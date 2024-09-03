using Hacknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Managers
{
    public static class HollowGlobalManager
    {
        public static List<Modification> PossibleModifications { get; internal set; } = new();
        public static List<Malware> PossibleMalware { get; internal set; } = new();
        public static List<Corruption> PossibleCorruptions { get; internal set; } = new();

        public static Action<string, string> StartNewGameAction { get; internal set; }

        public static string LastCustomThemePath { get; internal set; }
        public static OSTheme LastOSTheme { get; internal set; }
        public static OSTheme TargetTheme { get; internal set; } = OSTheme.HacknetBlue;
    }
}
