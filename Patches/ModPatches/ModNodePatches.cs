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
        private static List<Modification> Modifications => HollowZeroCore.CollectedMods;
        private static List<Corruption> Corruptions => HollowZeroCore.CollectedCorruptions;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Computer),nameof(Computer.connect))]
        public static bool CheckModNodeConnection(Computer __instance, string ipFrom, ref bool __result)
        {
            if (ipFrom != OS.currentInstance.thisComputer.ip) return true;
            bool okayToRun = true;

            foreach (var mod in Modifications.Where(m => m.Trigger == Modification.ModTriggers.EnterNode))
            {
                mod.Effect(__instance);
            }

            foreach(var cor in Corruptions.Where(c => c.Trigger == Modification.ModTriggers.EnterNode))
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
                            OS.currentInstance.terminal.writeLine("<!!!> Connection failed due to a network malfunction.");
                        });
                        __result = false;
                        okayToRun = false;
                    }
                } else
                {
                    cor.CorruptionEffect();
                }
            }

            return okayToRun;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Computer),nameof(Computer.giveAdmin))]
        public static bool CheckModAdminGained(Computer __instance, string ipFrom)
        {
            if (ipFrom != OS.currentInstance.thisComputer.ip) return true;
            bool okayToRun = true;

            foreach (var mod in Modifications.Where(m => m.Trigger == Modification.ModTriggers.GainAdminAccess)) {
                mod.LaunchEffect(__instance, -1);
            }

            foreach (var cor in Corruptions.Where(c => c.Trigger == Modification.ModTriggers.GainAdminAccess))
            {
                if (cor.IsBlocker && cor.ChanceEffect != null)
                {
                    if (!cor.ChanceEffect(__instance)) okayToRun = false;
                }
                else
                {
                    cor.CorruptionEffect();
                }
            }

            return okayToRun;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TraceTracker),nameof(TraceTracker.start))]
        public static bool CheckTraceMod(TraceTracker __instance, float t)
        {
            OS os = __instance.os;
            bool okayToRun = true;

            foreach(var mod in Modifications.Where(m => m.Trigger == Modification.ModTriggers.OnTraceTrigger))
            {
                if(mod.IsBlocker && mod.ChanceEffect != null)
                {
                    if (mod.ChanceEffect(os.thisComputer))
                    {
                        okayToRun = false;
                        continue;
                    }
                } else if(mod.IsBlocker)
                {
                    mod.TraceEffect(t);
                    okayToRun = false;
                    continue;
                } else
                {
                    mod.TraceEffect(t);
                }
            }

            foreach(var cor in Corruptions.Where(c => c.Trigger == Modification.ModTriggers.OnTraceTrigger))
            {
                cor.CorruptionEffect();

                if (cor.IsBlocker) okayToRun = false;
            }

            return okayToRun;
        }
    }
}
