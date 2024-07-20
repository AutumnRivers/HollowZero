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
using Pathfinder.Event.Loading;
using Pathfinder.Action;
using Pathfinder.Event;

namespace HollowZero
{
    [BepInDependency("autumnrivers.stuxnet")]
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class HollowZeroCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.hollowzero";
        public const string ModName = "Hollow Zero";
        public const string ModVer = "1.0.0";

        private const string HZLOG_PREFIX = "[Hollow Zero] ";

        private static List<Malware> CollectedMalware { get; set; }
        private static List<Modification> CollectedMods { get; set; }
        private static List<Corruption> CollectedCorruption { get; set; }

        public static int InfectionLevel { get; internal set; }
        public static uint PlayerCredits { get; internal set; }

        public override bool Load()
        {
            CollectedMalware = new List<Malware>();
            CollectedMods = new List<Modification>();
            CollectedCorruption = new List<Corruption>();

            InfectionLevel = 0;
            PlayerCredits = 0;

            HZLog("Initializing...");
            HarmonyInstance.PatchAll(typeof(HollowZeroCore).Assembly);

            HZLog("Adding daemons...");
            DaemonManager.RegisterDaemon<DialogueEventDaemon>();
            DaemonManager.RegisterDaemon<ChoiceEventDaemon>();

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
            var placeOnNetMap = new Stuxnet_HN.Actions.Nodes.PlaceOnNetMap();
            placeOnNetMap.StartingPosition = "topleft";
            placeOnNetMap.Offset = "0.5,0.5";
            placeOnNetMap.TargetCompID = "playerComp";
            placeOnNetMap.Trigger(os_event.Os);
        }
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
