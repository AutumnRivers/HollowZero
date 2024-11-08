﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

using BepInEx;
using BepInEx.Hacknet;

using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;

using Pathfinder.Daemon;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using HollowZero.Daemons;
using HollowZero.Daemons.Event;

using HollowZero.Commands;

using HollowZero.Packs;

using HollowZero.Patches;
using HollowZero.Managers;

using Pathfinder.Event.Loading;
using Pathfinder.Event.Gameplay;
using Pathfinder.Event;
using Pathfinder.Command;

using MonoMod.Utils;

using BepInEx.Logging;

using static HollowZero.HollowLogger;
using static HollowZero.Managers.HollowGlobalManager;

namespace HollowZero
{
    [BepInDependency("autumnrivers.stuxnet")]
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class HollowZeroCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.hollowzero";
        public const string ModName = "Hollow Zero";
        public const string ModVer = "1.0.0";

        internal const string HZLOG_PREFIX = "[Hollow Zero] ";

        public const string DEFAULT_CONFIG_PATH = "/Plugins/HZConfig";
        public const string DEFAULT_PACKS_FOLDER = DEFAULT_CONFIG_PATH + "/Packs/";

        public const float BASE_PROXY_SPEED = 0.5f;
        public const float BASE_FIREWALL_ADD = -0.5f;
        public const float PROXY_MULTIPLIER = 1.25f;

        public const int MAX_MALWARE = 4;

        internal static List<Malware> CollectedMalware { get; set; } = new();
        internal static List<Modification> CollectedMods { get; set; } = new();
        internal static List<Corruption> CollectedCorruptions { get; set; } = new();

        public static List<string> SeenEvents { get; internal set; } = new();

        public static bool ShowInfecTracker = true;

        internal static Dictionary<string, string> knownPacks = new();
        internal static List<Assembly> knownPackAsms = new();
        internal static Dictionary<string, string> loadedPacks = new();

        internal enum UIState
        {
            Game, Guidebook, Inventory
        }

        internal static UIState CurrentUIState { get; set; }
        internal static string Mode { get; set; }

        public static float ForkbombMultiplier { get; internal set; } = 1.0f;

        public override bool Load()
        {
            HollowLogSource = Log;

            PlayerManager.InfectionLevel = 0;
            PlayerManager.PlayerCredits = 0;

            HZLog("Initializing...");
            HarmonyInstance.PatchAll(typeof(HollowZeroCore).Assembly);
            ChoiceEventDaemon.ReadChoiceEventsFileRewrite();

            var possiblePacks = Directory.GetFiles(GetExtensionFilePath(DEFAULT_PACKS_FOLDER));
            foreach (var pck in possiblePacks)
            {
                if (!File.Exists(pck)) continue;
                var packAsm = HollowPFManager.LoadAssemblyThroughPF(pck);
                HZLog($"Attempting to get metadata of Hollow Pack {packAsm.GetName()}...");

                if (GetHollowPackDetails(packAsm, out string packID, out string author))
                {
                    knownPacks.Add(packID, author);
                }
            }

            //HZLog("Adding actions...");

            HZLog("Adding daemons...");
            RegisterDaemons();

            HZLog("Adding commands...");
            // Quick Stats
            RegisterCommands(typeof(QuickStatCommands), QuickStatCommands.Aliases, false);

            // Node Commands
            CommandManager.RegisterCommand("listnodes", NodeCommands.ListAvailableNodes);

            // UI Commands
            CommandManager.RegisterCommand("guidebook", GuidebookCommands.ActivateGuidebook);
            CommandManager.RegisterCommand("inventory", InventoryCommands.ShowInventory);

            // Rob Store Command
            CommandManager.RegisterCommand("steal", RobCommand.RobStore);

            // Debug
            RegisterCommands(typeof(DebugCommands), DebugCommands.Aliases, true);

            HZLog("Registering events...");
            EventManager<OSLoadedEvent>.AddHandler(delegate (OSLoadedEvent osl)
            {
                ExtensionInit(osl);
            });
            EventManager<OSUpdateEvent>.AddHandler(delegate (OSUpdateEvent osu)
            {
                MalwareEffects.ApplyPersistentMalwareEffects(osu);
                HollowTimer.DecreaseTimers(osu);
                RunPersistentModsAndCorruptions(osu);
                ForkbombSpeedFix.AddToNewForkbombRamCost(osu);
            });
            return true;
        }

        public override bool Unload()
        {
            HZLog("Resetting cracker values...");
            PortHackExe.CRACK_TIME = 6f;

            HZLog("Clearing timers...");
            HollowTimer.ClearTimers();

            return base.Unload();
        }

        private static void RegisterCommands(Type rootType, Dictionary<MethodInfo, string> aliases, bool isDebug = false)
        {
            if (aliases == null) return;
            foreach(var mthd in rootType.GetMethods())
            {
                if (!mthd.HasTypes(typeof(OS), typeof(string[]))) return;
                Action<OS, string[]> cmd = mthd.CreateDelegate<Action<OS, string[]>>();
                string alias = aliases[mthd];

                if(!isDebug)
                {
                    CommandManager.RegisterCommand(alias, cmd);
                } else if(isDebug && OS.DEBUG_COMMANDS)
                {
                    CommandManager.RegisterCommand(alias, cmd, false, true);
                }
            }
        }

        private static void RegisterDaemons()
        {
            var hollowDaemons = typeof(HollowZeroCore).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(HollowDaemon)));
            List<Type> registerableDaemons = new();

            foreach(var daemon in hollowDaemons)
            {
                try
                {
                    var register = daemon.GetProperty("Registerable", BindingFlags.Static | BindingFlags.Public);
                    if (register == null) continue;
                    if ((bool)register.GetValue(null))
                    {
                        LogDebug($"Registering Hollow Daemon {daemon.Name}...");
                        registerableDaemons.Add(daemon);
                    }
                } catch(Exception e)
                {
                    LogError($"Error while attempting to convert {daemon.Name} to Hollow Daemon, skipping:");
                    LogError(e.ToString());
                }
            }

            foreach(var daemon in registerableDaemons)
            {
                try
                {
                    DaemonManager.RegisterDaemon(daemon);
                    LogDebug($"Successfully registered Hollow Daemon {daemon.Name} with Pathfinder");
                } catch(Exception e)
                {
                    LogError($"Error while attempting to register Hollow Daemon {daemon.Name} with Pathfinder:");
                    LogError(e.ToString());
                }
            }
        }

        private void HZLog(string message)
        {
            LogDebug(HZLOG_PREFIX + message);
        }

        public static string GetExtensionFilePath(string relativePath)
        {
            return ExtensionLoader.ActiveExtensionInfo.FolderPath + relativePath;
        }

        private static void RunPersistentModsAndCorruptions(OSUpdateEvent updateEvent)
        {
            foreach(var mod in CollectedMods.Where(m => m.Trigger == Modification.ModTriggers.Always))
            {
                mod.AltEffect(mod.PowerLevels[0]);
            }

            foreach(var cor in CollectedCorruptions.Where(c => c.Trigger == Modification.ModTriggers.Always))
            {
                cor.CorruptionEffect();
            }
        }

        private static void ExtensionInit(OSLoadedEvent os_event)
        {
            if(ReadExtensionConfigIfAny(out var config))
            {
                Mode = config.mode;
                ShowInfecTracker = config.mode == "Endless" || config.launchInfecTracker;
            }

            var placeOnNetMap = new Stuxnet_HN.Actions.Nodes.PlaceOnNetMap
            {
                StartingPosition = "topleft",
                Offset = "0.5,0.5",
                TargetCompID = "playerComp"
            };
            placeOnNetMap.Trigger(os_event.Os);

            GuidebookPatch.GuidebookEntries = GuidebookPatch.GuidebookEntries.OrderBy(x => x.ShortTitle).ToList();
            List<string> GuidebookTitles = new List<string>();
            foreach(var entry in GuidebookPatch.GuidebookEntries)
            {
                GuidebookTitles.Add(entry.ShortTitle);
            }
            GuidebookPatch.GuidebookEntryTitles = GuidebookTitles;

            PossibleMalware.AddRange(DefaultMalware.MalwareCollection);
            PossibleCorruptions.AddRange(DefaultCorruptions.Corruptions);
            PossibleModifications.AddRange(DefaultModifications.Mods);
        }

        private static bool ReadExtensionConfigIfAny(out HollowConfig config)
        {
            config = null;
            if (!File.Exists(DEFAULT_CONFIG_PATH + "/extension_config.json")) return false;

            StreamReader configStream = new StreamReader(DEFAULT_CONFIG_PATH + "/extension_config.json");
            var configString = configStream.ReadToEnd();
            configStream.Close();

            config = JsonConvert.DeserializeObject<HollowConfig>(configString);
            return true;
        }

        private enum RegisterFailures
        {
            NOT_HOLLOW,
            MISSING_ONREGISTER,
            BROKEN_METADATA
        }

        internal static bool RegisterHollowPacks()
        {
            bool allLoaded = true;

            foreach (var packAsm in knownPackAsms)
            {
                if (RegisterHollowPack(packAsm, out string packID, out string author))
                {
                    loadedPacks.Add(packID, author);
                } else
                {
                    allLoaded = false;
                }
            }

            return allLoaded;
        }

        internal static bool GetHollowPackDetails(Assembly hollowPackAsm, out string packID, out string packAuthor)
        {
            string asmName = hollowPackAsm.GetName().Name;
            var registerClass = hollowPackAsm.GetTypes().FirstOrDefault(t => t.BaseType.Name == "HollowPack");
            packID = null;
            packAuthor = null;
            if (registerClass == default)
            {
                FailLog(asmName, RegisterFailures.NOT_HOLLOW);
                return false;
            }

            var metadataClass = registerClass.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == "HollowPackMetadata");
            if (metadataClass == default)
            {
                FailLog(asmName, RegisterFailures.BROKEN_METADATA);
                return false;
            }

            if(metadataClass.ConstructorArguments.Count < 2)
            {
                FailLog(asmName, RegisterFailures.BROKEN_METADATA);
                return false;
            }
            packID = metadataClass.ConstructorArguments[0].Value as string;
            packAuthor = metadataClass.ConstructorArguments[1].Value as string;
            return true;

            void FailLog(string title, RegisterFailures failureType)
            {
                string reason = "Unknown Error";

                switch (failureType)
                {
                    case RegisterFailures.NOT_HOLLOW:
                        reason = "There are no classes within the DLL that inherit from HollowPack";
                        break;
                    case RegisterFailures.BROKEN_METADATA:
                        reason = "The HollowPack class' HollowPackMetadata is broken";
                        break;
                    default:
                        break;
                }

                LogError(HZLOG_PREFIX + $"Failed to get metadata of Hollow Pack {title} with reason: {reason}.");
            }
        }

        internal static bool RegisterHollowPack(Assembly hollowPackAsm, out string packID, out string packAuthor)
        {
            string asmName = hollowPackAsm.GetName().Name;
            var registerClass = hollowPackAsm.GetTypes().FirstOrDefault(t => t.BaseType.Name == "HollowPack");
            packID = null;
            packAuthor = null;
            if (registerClass == default)
            {
                FailLog(asmName, RegisterFailures.NOT_HOLLOW);
                return false;
            }

            var metadataClass = registerClass.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == "HollowPackMetadata");
            if (metadataClass == default)
            {
                FailLog(asmName, RegisterFailures.BROKEN_METADATA);
                return false;
            }

            var packInstance = Activator.CreateInstance(registerClass);

            packID = metadataClass.ConstructorArguments[0].Value as string;
            packAuthor = metadataClass.ConstructorArguments[1].Value as string;

            var customMalware = registerClass.GetProperty("CustomMalware");
            if(customMalware != null && customMalware?.GetValue(packInstance) != null)
            {
                if(customMalware.GetValue(packInstance) is List<Malware> cMalwareList)
                {
                    foreach(var malware in cMalwareList)
                    {
                        PossibleMalware.Add(malware);
                    }
                }
            }

            var customMods = registerClass.GetProperty("CustomModifications");
            if(customMods != null && customMods?.GetValue(packInstance) != null)
            {
                if(customMods.GetValue(packInstance) is List<Modification> cModsList)
                {
                    foreach(var mod in cModsList)
                    {
                        PossibleModifications.Add(mod);
                    }
                }
            }

            var customCorruptions = registerClass.GetProperty("CustomCorruptions");
            if(customCorruptions != null && customCorruptions?.GetValue(packInstance) != null)
            {
                if(customCorruptions.GetValue(packInstance) is List<Corruption> cors)
                {
                    foreach(var corruption in cors)
                    {
                        PossibleCorruptions.Add(corruption);
                    }
                }
            }

            return true;

            void FailLog(string title, RegisterFailures failureType)
            {
                string reason = "Unknown Error";

                switch(failureType)
                {
                    case RegisterFailures.NOT_HOLLOW:
                        reason = "There are no classes within the DLL that inherit from HollowPack";
                        break;
                    case RegisterFailures.BROKEN_METADATA:
                        reason = "The HollowPack class' HollowPackMetadata is broken";
                        break;
                    default:
                        reason = "Unknown Error";
                        break;
                }

                LogError(HZLOG_PREFIX + $"Failed to load Hollow Pack {title} with reason: {reason}.");
            }
        }

        internal static void Overload(int newInfection, bool overflow)
        {
            foreach (var mod in CollectedMods.Where(m => m.Trigger == Modification.ModTriggers.OnOverload))
            {
                if (mod.IsBlocker && mod.ChanceEffect != null)
                {
                    if (mod.ChanceEffect(OS.currentInstance.thisComputer)) return;
                }
                else if (mod.IsBlocker)
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, newInfection);
                    return;
                }
                else
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, newInfection);
                }
            }

            PlayerManager.InfectionLevel = overflow ? newInfection - 100 : 0;

            OS os = OS.currentInstance;
            os.IncConnectionOverlay.sound1.Play();

            CustomEffects.CurrentEffect = DrawOverload;
            CustomEffects.EffectsActive = true;
        }

        internal static void DrawOverload()
        {
            int stage = CustomEffects.CurrentStage;
            Action stageUpper = delegate ()
            {
                CustomEffects.CurrentStage++;
            };
            Rectangle playerBounds = GuiData.spriteBatch.GraphicsDevice.Viewport.Bounds;
            double lastGameTime = OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;

            switch(stage)
            {
                case 0:
                    RenderedRectangle.doRectangle(playerBounds.X, playerBounds.Y, playerBounds.Width, playerBounds.Height, Color.White);
                    HollowTimer.AddTimer("overload_upper", 0.3f, stageUpper);
                    break;
                case 1:
                    RenderedRectangle.doRectangle(playerBounds.X, playerBounds.Y, playerBounds.Width, playerBounds.Height, Color.Black);
                    HollowDaemon.DrawTrueCenteredText(playerBounds, "-- CRITICAL ERROR - RECOVERING --", GuiData.font, Color.Red);
                    HollowTimer.AddTimer("overload_upper_1", 3f, stageUpper);
                    break;
                case 2:
                    RenderedRectangle.doRectangle(playerBounds.X, playerBounds.Y, playerBounds.Width, playerBounds.Height,
                        Color.Black * (CustomEffects.RectOpacity / 100));
                    CustomEffects.RectOpacity -= (float)lastGameTime * 25;
                    if(CustomEffects.RectOpacity <= 0.0f)
                    {
                        CustomEffects.CurrentStage++;
                    }
                    break;
                case 3:
                    CustomEffects.ResetEffect();
                    InventoryManager.AddMalware();
                    break;
            }
        }
    }

    internal static class HollowLogger
    {
        internal static ManualLogSource HollowLogSource;

        internal static void LogImportant(string msg) => HollowLogSource.Log(LogLevel.Message, msg);
        internal static void LogError(string msg) => HollowLogSource.LogError(msg);
        internal static void LogDebug(string msg) => HollowLogSource.LogDebug(msg);
        internal static void LogWarning(string msg) => HollowLogSource.LogWarning(msg);
        internal static void LogCustom(LogLevel level, string msg) => HollowLogSource.Log(level, msg);

        internal static void LogDebug(string msg, bool onlyIfDebugModeIsEnabled)
        {
            if (!OS.DEBUG_COMMANDS && onlyIfDebugModeIsEnabled) return;
            HollowLogSource.LogDebug(msg);
        }
    }

    public class HollowConfig
    {
        /*
         * Endless - Endless randomly generated layers
         * Story - Set amount of pre-defined layers
         * StoryWithEndless - Set amount of pre-defined layers followed by endless randomly generated layers
         */
        public string mode = "Endless";

        /*
         * Whether or not to launch InfecTracker on extension start.
         * If "mode" is set to "Endless," then this is ignored.
         */
        public bool launchInfecTracker = false;

        /*
         * If true, disables default malware, corruptions, events, etc... making it only possible for any
         * of these things to be propogated by HZConfig or Hollow Packs.
         */
        public bool disableBuiltInAssets = false;
    }
}
