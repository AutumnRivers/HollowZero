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
        internal readonly static string[] badCommands = { "probe", "login", "reboot" };

        // os.terminal.writeLine($"The command '{arguments[0]}' is disabled by your local administrator.");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Terminal),nameof(Terminal.executeLine))]
        public static bool DisableCommands(Terminal __instance)
        {
            string command = __instance.currentLine.Split(' ')[0];
            OS os = OS.currentInstance;

            if (badCommands.Contains(command)) {
                os.terminal.writeLine($"The command '{command}' is disabled by your local administrator.");
                __instance.currentLine = "";
                TextBox.cursorPosition = 0;
                TextBox.textDrawOffsetPosition = 0;
                return false;
            }

            return true;
        }
    }
}
