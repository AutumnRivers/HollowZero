using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class CommandDisabler
    {
        internal readonly static List<string> corruptedCommands = new List<string>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Terminal),nameof(Terminal.executeLine))]
        public static bool DisableCommands(Terminal __instance)
        {
            string command = __instance.currentLine.Split(' ')[0].ToLower();
            OS os = OS.currentInstance;

            if(corruptedCommands.Contains(command) ||
                ((corruptedCommands.Contains("disconnect") || corruptedCommands.Contains("dc")) &&
                (command == "disconnect" || command == "dc")))
            {
                os.terminal.writeLine($"The command '{command}' cannot be ran at this time due to system instability.");
                __instance.currentLine = "";
                TextBox.cursorPosition = 0;
                TextBox.textDrawOffsetPosition = 0;
                return false;
            }

            if(command == "forkbomb" && os.connectedComp == os.thisComputer)
            {
                if (OS.DEBUG_COMMANDS) return true;
                os.terminal.writeLine("But then you think to yourself, 'why would I forkbomb my own system? That's stupid.'");
                __instance.currentLine = "";
                TextBox.cursorPosition = 0;
                TextBox.textDrawOffsetPosition = 0;
                return false;
            }

            return true;
        }
    }
}
