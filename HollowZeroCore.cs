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

using Pathfinder;
using Pathfinder.Daemon;

using Microsoft.Xna.Framework.Graphics;

using HollowZero.Daemons;
using HollowZero.Daemons.Event;
using HollowZero.Executables;
using HollowZero.Actions;
using HollowZero.Commands;
using HollowZero.Packs;

using Pathfinder.Event.Loading;
using Pathfinder.Action;
using Pathfinder.Event;
using Pathfinder.Command;
using Pathfinder.Executable;

using HarmonyLib;

using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using BepInEx.Bootstrap;
using HollowZero.Patches;

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
        internal static Dictionary<string, string> loadedPacks = new Dictionary<string, string>();

        internal static bool GuidebookIsActive { get; set; }

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
                var packAsm = Assembly.LoadFrom(pck);
                //var packAsm = HollowPFManager.LoadAssemblyThroughPF(pck);
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

            HZLog("Adding commands...");
            // Quick Stats
            CommandManager.RegisterCommand("infection", QuickStatCommands.ShowInfection);
            CommandManager.RegisterCommand("malware", QuickStatCommands.ListMalware);
            CommandManager.RegisterCommand("stats", QuickStatCommands.ListQuickStats);

            // Guidebook
            CommandManager.RegisterCommand("guidebook", GuidebookCommands.ActivateGuidebook);

            // Debug
            CommandManager.RegisterCommand("upinf", DebugCommands.IncreaseInfection, false, true);
            CommandManager.RegisterCommand("downinf", DebugCommands.DecreaseInfection, false, true);

            Action<OSLoadedEvent> extInit = ExtensionInit;
            EventManager<OSLoadedEvent>.AddHandler(extInit);

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

            var possiblePacks = Directory.GetFiles(GetExtensionFilePath(DEFAULT_PACKS_FOLDER));
            foreach (var pck in possiblePacks)
            {
                if (!File.Exists(pck)) continue;
                var packAsm = Assembly.LoadFrom(pck);
                //var packAsm = HollowPFManager.LoadAssemblyThroughPF(pck);
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
            foreach(var t in hollowPackAsm.GetTypes())
            {
                Console.WriteLine(t.Name + $" / Base Type: {t.BaseType} / Is Hollow Pack: {t.BaseType.Equals(typeof(HollowPack))}");
            }
            var registerClass = hollowPackAsm.GetTypes().FirstOrDefault(t => t.BaseType.Equals(typeof(HollowPack)));
            if (registerClass == default)
            {
                FailLog(asmName, RegisterFailures.NOT_HOLLOW);
                return false;
            }

            var metadataClass = registerClass.GetCustomAttribute<HollowPackMetadata>();
            if (metadataClass == default)
            {
                FailLog(asmName, RegisterFailures.BROKEN_METADATA);
                return false;
            }

            if(metadataClass.PackID == null || metadataClass.PackAuthor == null)
            {
                FailLog(asmName, RegisterFailures.BROKEN_METADATA);
                return false;
            }
            packID = metadataClass.PackID;
            packAuthor = metadataClass.PackAuthor;
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
            var registerClass = hollowPackAsm.GetTypes().FirstOrDefault(t => t.BaseType == typeof(HollowPack));
            if (registerClass == default)
            {
                FailLog(asmName, RegisterFailures.NOT_HOLLOW);
                return false;
            }

            var metadataClass = registerClass.GetCustomAttribute<HollowPackMetadata>();
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
            packID = metadataClass.PackID;
            packAuthor = metadataClass.PackAuthor;
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

        public static void IncreaseInfection(int amount)
        {
            if(InfectionLevel + amount >= 100)
            {
                // add malware...
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
            
        }

        public static void RemoveMalware(Malware malware = null)
        {
            
        }

        public static void AddModification(Modification mod = null)
        {

        }

        public static void AddCorruption(Corruption corruption = null)
        {

        }

        //private Malware GetRandomMalware() { }
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
        public Action Action { get; set; }
    }

    public class Modification
    {
        public Modification() { }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<int> PowerLevels { get; set; }
        public bool Upgraded { get; set; }
    }

    public class Corruption : Modification { }

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

            var hollowPackDll = Assembly.LoadFile(filepath + filename);

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
            var renamedAssemblyResolver = typeof(HacknetChainloader).Assembly.GetType("RenamedAssemblyResolver");
            var chainloaderFix = typeof(HacknetChainloader).Assembly.GetType("ChainloaderFix");
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

            return loaded;
        }
    }
}
