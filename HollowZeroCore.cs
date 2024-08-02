using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;

using BepInEx;
using BepInEx.Hacknet;

using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;

using Pathfinder;
using Pathfinder.Daemon;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;

using HollowZero.Daemons;
using HollowZero.Daemons.Event;
using HollowZero.Executables;
using HollowZero.Actions;
using HollowZero.Commands;
using HollowZero.Packs;
using HollowZero.Patches;

using Pathfinder.Event.Loading;
using Pathfinder.Event.Gameplay;
using Pathfinder.Action;
using Pathfinder.Event;
using Pathfinder.Command;
using Pathfinder.Executable;

using HarmonyLib;

using Mono.Cecil;
using HollowZero.Daemons.Shop;

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

        private static List<Malware> possibleMalware = new List<Malware>();

        internal static List<Malware> CollectedMalware { get; set; }
        internal static List<Modification> CollectedMods { get; set; }
        internal static List<Corruption> CollectedCorruption { get; set; }

        private static List<string> seenEvents = new List<string>();
        public static List<string> SeenEvents
        {
            get
            {
                return seenEvents;
            }
            internal set
            {
                seenEvents = value;
            }
        }

        public static List<Malware> PossibleMalware
        {
            get { return possibleMalware; }
            internal set { possibleMalware = value; }
        }

        public static int InfectionLevel { get; internal set; }
        public static uint PlayerCredits { get; internal set; }

        //internal static List<string> loadedPacks = new List<string>();
        internal static Dictionary<string, string> knownPacks = new Dictionary<string, string>();
        internal static List<Assembly> knownPackAsms = new List<Assembly>();
        internal static Dictionary<string, string> loadedPacks = new Dictionary<string, string>();

        internal static bool GuidebookIsActive { get; set; }
        internal static bool EnableTrinity { get; set; }
        internal static string Mode { get; set; }

        public override bool Load()
        {
            CollectedMalware = new List<Malware>();
            CollectedMods = new List<Modification>();
            CollectedCorruption = new List<Corruption>();

            InfectionLevel = 0;
            PlayerCredits = 0;

            HZLog("Initializing...");
            HarmonyInstance.PatchAll(typeof(HollowZeroCore).Assembly);
            ChoiceEventDaemon.ReadChoiceEventsFile();

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

            HZLog("Adding actions...");
            ActionManager.RegisterAction(typeof(LaunchInfecTrackerAction), "LaunchInfecTracker");

            HZLog("Adding daemons...");
            DaemonManager.RegisterDaemon<DialogueEventDaemon>();
            DaemonManager.RegisterDaemon<ChoiceEventDaemon>();
            DaemonManager.RegisterDaemon<ChanceEventDaemon>();
            DaemonManager.RegisterDaemon<RestStopDaemon>();
            DaemonManager.RegisterDaemon<ProgramShopDaemon>();

            HZLog("Adding commands...");
            // Quick Stats
            CommandManager.RegisterCommand("infection", QuickStatCommands.ShowInfection);
            CommandManager.RegisterCommand("malware", QuickStatCommands.ListMalware);
            CommandManager.RegisterCommand("stats", QuickStatCommands.ListQuickStats);

            // Guidebook
            CommandManager.RegisterCommand("guidebook", GuidebookCommands.ActivateGuidebook);

            // Debug
            if(OS.DEBUG_COMMANDS)
            {
                CommandManager.RegisterCommand("upinf", DebugCommands.IncreaseInfection, false, true);
                CommandManager.RegisterCommand("downinf", DebugCommands.DecreaseInfection, false, true);
                CommandManager.RegisterCommand("addmal", DebugCommands.AddRandomMalware, false, true);
                CommandManager.RegisterCommand("clearmal", DebugCommands.ClearMalware, false, true);
                CommandManager.RegisterCommand("addcreds", DebugCommands.AddCredits, false, true);
                CommandManager.RegisterCommand("delcreds", DebugCommands.RemoveCredits, false, true);
            }

            HZLog("Registering game events...");
            EventManager<OSLoadedEvent>.AddHandler(delegate (OSLoadedEvent osl)
            {
                ExtensionInit(osl);
            });
            EventManager<OSUpdateEvent>.AddHandler(delegate (OSUpdateEvent osu)
            {
                MalwareEffects.ApplyPersistentMalwareEffects(osu);
            });
            EventManager<OSUpdateEvent>.AddHandler(delegate (OSUpdateEvent osu)
            {
                HollowTimer.DecreaseTimers(osu);
            });
            return true;
        }

        private void HZLog(string message)
        {
            Log.LogDebug(HZLOG_PREFIX + message);
        }

        public static string GetExtensionFilePath(string relativePath)
        {
            return ExtensionLoader.ActiveExtensionInfo.FolderPath + relativePath;
        }

        private static void ExtensionInit(OSLoadedEvent os_event)
        {
            if(ReadExtensionConfigIfAny(out var config))
            {
                Mode = config.mode;
                EnableTrinity = config.enableTrinity;
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

                Console.WriteLine(HZLOG_PREFIX + $"Failed to get metadata of Hollow Pack {title} with reason: {reason}.");
            }
        }

        internal static bool RegisterHollowPack(Assembly hollowPackAsm, out string packID, out string packAuthor)
        {
            string asmName = hollowPackAsm.GetName().Name;
            var registerClass = hollowPackAsm.GetTypes().FirstOrDefault(t => t.BaseType.Name == "HollowPack");
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

            var onRegisterMethod = registerClass.GetMethod("OnRegister");
            if(onRegisterMethod == null)
            {
                FailLog(asmName, RegisterFailures.MISSING_ONREGISTER);
                return false;
            }

            var packInstance = Activator.CreateInstance(registerClass);

            onRegisterMethod.Invoke(packInstance, null);
            packID = metadataClass.ConstructorArguments[0].Value as string;
            packAuthor = metadataClass.ConstructorArguments[1].Value as string;
            return true;

            void FailLog(string title, RegisterFailures failureType)
            {
                string reason = "Unknown Error";

                switch(failureType)
                {
                    case RegisterFailures.NOT_HOLLOW:
                        reason = "There are no classes within the DLL that inherit from HollowPack";
                        break;
                    case RegisterFailures.MISSING_ONREGISTER:
                        reason = "The HollowPack class is missing the OnRegister method";
                        break;
                    case RegisterFailures.BROKEN_METADATA:
                        reason = "The HollowPack class' HollowPackMetadata is broken";
                        break;
                    default:
                        break;
                }

                Console.WriteLine(HZLOG_PREFIX + $"Failed to load Hollow Pack {title} with reason: {reason}.");
            }
        }

        private static void Overload()
        {
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
                    AddMalware();
                    break;
            }
        }

        public static void IncreaseInfection(int amount)
        {
            if(InfectionLevel + amount >= 100)
            {
                Overload();
                InfectionLevel = 0;
            } else
            {
                InfectionLevel += amount;
            }
        }

        public static void DecreaseInfection(int amount)
        {
            if(InfectionLevel - amount <= 0)
            {
                InfectionLevel = 0;
            } else
            {
                InfectionLevel -= amount;
            }
        }

        public static void ClearInfection()
        {
            InfectionLevel = 0;
        }

        public static void AddPlayerCredits(int amount)
        {
            if(PlayerCredits + amount > 9999)
            {
                PlayerCredits = 9999;
            } else
            {
                PlayerCredits += (uint)amount;
            }
        }

        public static bool RemovePlayerCredits(int amount)
        {
            if(PlayerCredits - amount < 0)
            {
                return false;
            } else
            {
                PlayerCredits -= (uint)amount;
                return true;
            }
        }

        public static void AddMalware(Malware malware = null)
        {
            Malware GetMalware()
            {
                Malware m = GetRandomMalware();
                if(CollectedMalware.Contains(m))
                {
                    return GetMalware();
                }
                return m;
            }

            malware ??= GetMalware();

            CollectedMalware.Add(malware);
            if(malware.SetTimer)
            {
                //MalwareEffects.MalwareTimers.Add(malware, malware.PowerLevel);
                MalwareEffects.AddMalwareTimer(malware, malware.PowerLevel);
            }

            MalwareOverlay.CurrentMalware = malware;
        }

        public static void RemoveMalware(Malware malware = null)
        {
            malware ??= CollectedMalware.GetRandom();

            List<Computer> affectedComps = new List<Computer>();
            if(MalwareEffects.AffectedComps.Exists(c => c.AppliedEffects.Contains(malware.DisplayName)))
            {
                foreach (var comp in MalwareEffects.AffectedComps.Where(c => c.AppliedEffects.Contains(malware.DisplayName)))
                {
                    comp.AppliedEffects.Remove(malware.DisplayName);
                    var affectedComp = OS.currentInstance.netMap.nodes.First(c => c.idName == comp.CompID);
                    affectedComps.Add(affectedComp);
                }
            }

            malware.RemoveAction(malware.PowerLevel, affectedComps);
            CollectedMalware.Remove(malware);
        }

        public static void AddModification(Modification mod = null)
        {

        }

        public static void UpgradeModification(Modification mod = null)
        {

        }

        public static void AddCorruption(Corruption corruption = null)
        {

        }

        public static Malware GetRandomMalware()
        {
            return PossibleMalware.GetRandom();
        }
    }

    public static class PlayerManager
    {
        public static void AddProgramToPlayerPC(string programName, string programContent)
        {
            FileEntry programFile = new FileEntry(programContent, $"{programName}.exe");
            Folder binFolder = OS.currentInstance.thisComputer.getFolderFromPath("bin");

            if (binFolder.containsFile($"{programName}.exe")) return;

            binFolder.files.Add(programFile);
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
         * Determines whether or not Trinity should be enabled, as she's technically one of my characters
         * and would therefore break immersion on any extension that isn't mine.
         * 
         * If Trinity is disabled, her chance shop branding is simply replaced with generic branding.
         */
        public bool enableTrinity = true;

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

        /*
         * If set to true, commands that can cheese HZ such as probe and reboot will be disabled.
         */
        public bool disableCheeseCommands = true;
    }

    public class Malware
    {
        public enum MalwareTrigger
        {
            ENTER_NODE,
            EXIT_NODE,
            ENTER_NETWORK,
            EXIT_NETWORK,
            EVERY_ACTION,
            PERSISTENT,
            ONE_SHOT
        }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int PowerLevel { get; set; }
        public MalwareTrigger Trigger { get; set; }

        public Action<int> PowerAction { get; set; }
        public Action<Computer> CompAction { get; set; }

        public Action<int, List<Computer>> RemoveAction { get; set; }

        public bool SetTimer = false;
    }

    public class Modification
    {
        public Modification(string name)
        {
            DisplayName = name;
        }

        public enum ModTriggers
        {
            EnterNode, ExitNode, GainAdminAccess,
            OnForkbomb, OnOverload, OnInfectionGain,
            OnTraceTrigger
        }

        public string DisplayName { get; set; }
        public virtual string Description { get; set; }
        public List<int> PowerLevels { get; set; }
        public ModTriggers Trigger { get; set; }
        public bool Upgraded = false;
        public Modification UpgradedModification { get; set; }

        public List<string> affectedCompIDs = new List<string>();
        public virtual Action<Computer> Effect { get; set; }
        public virtual Func<Computer, bool> ChanceEffect { get; set; }
        public Action<int> AltEffect { get; set; }
        public Action<float> TraceEffect { get; set; }

        public bool IsBlocker = false;
        public const bool IsCorruption = false;

        public bool AddEffectToComp(Computer comp)
        {
            if (affectedCompIDs.Contains(comp.idName)) return false;

            affectedCompIDs.Add(comp.idName);
            return true;
        }

        public void OnLayerChange()
        {
            affectedCompIDs.Clear();
        }

        public void Discard()
        {
            if(HollowZeroCore.CollectedMods.Contains(this))
            {
                HollowZeroCore.CollectedMods.Remove(this);
            }
        }

        public virtual void Upgrade()
        {
            if (Upgraded) return;
            Upgraded = true;
        }
    }

    public class Corruption : Modification
    {
        public Corruption(string name) : base(name) { }

        public new const bool IsCorruption = true;
    }

    public static class HollowGlobalManager
    {
        public static Action<string,string> StartNewGameAction { get; internal set; }
    }

    public static class HollowManager
    {
        public static bool ForceRegisterHollowPack(string filename, string filepath = null)
        {
            filepath ??= ExtensionLoader.ActiveExtensionInfo + "/Plugins/HZConfig/Packs/";
            if(!File.Exists(filepath + filename))
            {
                return false;
            }

            return true;
        }

        public static void AddChoiceEvent(ChoiceEvent ev)
        {
            ChoiceEventDaemon.PossibleEvents.Add(ev);
        }

        public static void AddChoiceEvent(IEnumerable<ChoiceEvent> evs)
        {
            ChoiceEventDaemon.PossibleEvents.AddRange(evs);
        }
    }

    public class HollowPFManager
    {
        public static Assembly LoadAssemblyThroughPF(string path)
        {
            var renamedAssemblyResolver = typeof(HacknetChainloader).Assembly.GetType("BepInEx.Hacknet.RenamedAssemblyResolver", true);
            var chainloaderFix = typeof(HacknetChainloader).Assembly.GetType("BepInEx.Hacknet.ChainloaderFix", true);
            var chFixRemaps = chainloaderFix.GetPrivateStaticField<Dictionary<string, Assembly>>("Remaps");
            var chFixRemapDefs = chainloaderFix.GetPrivateStaticField<Dictionary<string, AssemblyDefinition>>("RemapDefinitions");

            byte[] asmBytes;
            string name;

            var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters()
            {
                AssemblyResolver = (IAssemblyResolver)Activator.CreateInstance(renamedAssemblyResolver)
            });
            name = asm.Name.Name;
            asm.Name.Name = asm.Name.Name + "-" + DateTime.Now.Ticks;

            using (var ms = new MemoryStream())
            {
                asm.Write(ms);
                asmBytes = ms.ToArray();
            }

            var loaded = Assembly.Load(asmBytes);
            chFixRemaps[name] = loaded;
            chFixRemapDefs[name] = asm;

            chainloaderFix.SetPrivateStaticField("Remaps", chFixRemaps);
            chainloaderFix.SetPrivateStaticField("RemapDefinitions", chFixRemapDefs);

            HollowZeroCore.knownPackAsms.Add(loaded);

            return loaded;
        }
    }
}
