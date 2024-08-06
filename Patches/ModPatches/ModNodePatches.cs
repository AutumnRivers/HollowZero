using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;

namespace HollowZero
{
    [HarmonyPatch]
    public class ModNodePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Computer),nameof(Computer.connect))]
        public static bool CheckModNodeConnection(Computer __instance, string ipFrom, ref bool __result)
        {
            if (ipFrom != OS.currentInstance.thisComputer.ip) return true;

            foreach(var mod in HollowZeroCore.CollectedMods.Where(m => m.Trigger == Modification.ModTriggers.EnterNode))
            {
                mod.Effect(__instance);
            }

            foreach(var cor in HollowZeroCore.CollectedCorruptions.Where(c => c.Trigger == Modification.ModTriggers.EnterNode))
            {
                if(cor.IsBlocker)
                {
                    if(!cor.ChanceEffect(__instance))
                    {
                        OS.currentInstance.execute("disconnect");
                        OS.currentInstance.display.doDisconnectForcedDisplay();
                        OS.currentInstance.delayer.Post(ActionDelayer.NextTick(), delegate
                        {
                            OS.currentInstance.display.command = "connectiondenied";
                        });
                        __result = false;
                        return false;
                    }
                } else
                {
                    cor.CorruptionEffect();
                }
            }

            return true;
        }
    }
}
