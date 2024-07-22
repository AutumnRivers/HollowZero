using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private static List<Malware> possibleMalware = new List<Malware>();

        internal static List<Malware> CollectedMalware { get; set; }
        internal static List<Modification> CollectedMods { get; set; }
        internal static List<Corruption> CollectedCorruption { get; set; }

        public static List<Malware> PossibleMalware
        {
            get { return possibleMalware; }
            internal set { possibleMalware = value; }
        }

        public static int InfectionLevel { get; internal set; }
        public static uint PlayerCredits { get; internal set; }

        public override bool Load()
        {
            CollectedMalware = new List<Malware>()
            {
                new Malware()
                {
                    DisplayName = "Test Malware",
                    Description = "Doesn't actually do anything."
                }
            };
            CollectedMods = new List<Modification>();
            CollectedCorruption = new List<Corruption>();

            InfectionLevel = 0;
            PlayerCredits = 0;

            HZLog("Initializing...");
            HarmonyInstance.PatchAll(typeof(HollowZeroCore).Assembly);
            ChoiceEventDaemon.ReadChoiceEventsFile();

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

        public static void RemovePlayerCredits(int amount)
        {
            if(PlayerCredits - amount < 0)
            {
                PlayerCredits = 0;
            } else
            {
                PlayerCredits -= (uint)amount;
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
}
