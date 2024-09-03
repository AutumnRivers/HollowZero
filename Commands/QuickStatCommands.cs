using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Hacknet;

using HollowZero.Managers;

namespace HollowZero.Commands
{
    public class QuickStatCommands
    {
        public const string TERM_SEPERATOR = "- - - - - - - - - -";

        public static Dictionary<MethodInfo, string> Aliases = new Dictionary<MethodInfo, string>()
        {
            { FindMethod("ShowInfection"), "infection" },
            { FindMethod("ListMalware"), "malware" },
            { FindMethod("ListQuickStats"), "stats" }
        };

        private static MethodInfo FindMethod(string name)
        {
            return typeof(QuickStatCommands).GetMethod(name);
        }

        public static void ShowInfection(OS os, string[] args)
        {
            WriteToTerminal($"[!] CURRENT INFECTION LEVEL: {PlayerManager.InfectionLevel} / MALWARE COUNT: {HollowZeroCore.CollectedMalware.Count}");
        }

        public static void ListMalware(OS os, string[] args)
        {
            if(HollowZeroCore.CollectedMalware.Count == 0)
            {
                WriteToTerminal(":) You haven't collected any malware!");
            } else
            {
                foreach(var malware in HollowZeroCore.CollectedMalware)
                {
                    StringBuilder message = new StringBuilder(TERM_SEPERATOR);
                    message.Append($"\nMALWARE: {malware.DisplayName}\n");
                    message.Append($"{malware.Description}\n");
                    message.Append(TERM_SEPERATOR);
                    WriteToTerminal(message.ToString());
                }
            }
        }

        public static void ListQuickStats(OS os, string[] args)
        {
            StringBuilder message = new StringBuilder(TERM_SEPERATOR);
            message.Append("\n");

            message.Append($"CURRENT INFECTION: {PlayerManager.InfectionLevel}%\n");
            message.Append($"MODIFICATION COUNT: {HollowZeroCore.CollectedMods.Count}\n");
            message.Append($"CORRUPTION COUNT: {HollowZeroCore.CollectedCorruptions.Count}\n");
            message.Append($"CREDITS: ${PlayerManager.PlayerCredits}\n");

            message.Append(TERM_SEPERATOR);
            WriteToTerminal(message.ToString());
        }

        public static void WriteToTerminal(string message)
        {
            OS.currentInstance.terminal.writeLine(message);
        }
    }
}
