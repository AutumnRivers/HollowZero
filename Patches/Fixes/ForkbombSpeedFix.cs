using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;

using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Pathfinder.Event.Gameplay;

using System.Reflection;

namespace HollowZero
{
    [HarmonyPatch]
    public class ForkbombSpeedFix
    {
        public static bool Active => OS.currentInstance.exes.Any(exe => exe.GetType() == typeof(ForkBombExe));
        public static float newRamCost = 0.0f;

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(ForkBombExe),nameof(ForkBombExe.Update))]
        public static void FixForkbombSpeedsToFloat(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchDup(),
                x => x.MatchLdfld(AccessTools.Field(typeof(ExeModule), nameof(ExeModule.ramCost))),
                x => x.MatchLdloc(0),
                x => x.MatchAdd(),
                x => x.MatchStfld(AccessTools.Field(typeof(ExeModule), nameof(ExeModule.ramCost)))
            );
            c.RemoveRange(4);
            c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(ForkbombSpeedFix), nameof(newRamCost)));
            c.EmitDelegate(int (float newRamCost) => (int)Math.Floor(newRamCost));
        }

        public static void AddToNewForkbombRamCost(OSUpdateEvent osu)
        {
            if (!Active && newRamCost > 0.0f) { newRamCost = 0.0f; }
            if (!Active) return;

            var gameTime = (float)osu.GameTime.ElapsedGameTime.TotalSeconds;
            newRamCost += (gameTime * HollowZeroCore.ForkbombMultiplier) * 150f;
        }
    }
}
