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
        internal static Dictionary<string, string> loadedPacks = new Dictionary<string, string>();

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
                HZLog($"Attempting to load Hollow Pack {packAsm.GetName()}...");

                if (RegisterHollowPack(packAsm, out string packID, out string author))
                {
                    loadedPacks.Add(packID, author);
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
        }

        private enum RegisterFailures
        {
            NOT_HOLLOW,
            MISSING_ONREGISTER
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

            var onRegisterMethod = registerClass.GetMethod("OnRegister");
            var packIDProperty = registerClass.GetProperty("PackID");
            if(onRegisterMethod == null || packIDProperty == null)
            {
                FailLog(asmName, RegisterFailures.MISSING_ONREGISTER);
                return false;
            }

            var packInstance = Activator.CreateInstance(registerClass);
            if(packIDProperty.GetValue(packInstance) == null)
            {
                FailLog(asmName, RegisterFailures.MISSING_ONREGISTER);
                return false;
            }

            onRegisterMethod.Invoke(packInstance, null);
            packID = packIDProperty.GetValue(packInstance) as string;
            packAuthor = registerClass.GetProperty("PackAuthor").GetValue(packInstance) as string;
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
                        reason = "The HollowPack class is missing the OnRegister method or PackID property";
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
}
