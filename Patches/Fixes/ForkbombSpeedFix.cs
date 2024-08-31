using System;
using System.Linq;

using Hacknet;

using HarmonyLib;

using MonoMod.Cil;
using Mono.Cecil.Cil;

using Pathfinder.Event.Gameplay;

using static HollowZero.HollowLogger;

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
            try
            {
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
            } catch
            {
                LogCustom(BepInEx.Logging.LogLevel.Fatal,
                    $"[Forkbomb Patch] FAILED TO LOAD FORKBOMB PATCH!\n" +
                    "This happens randomly. This is often fixed by restarting Hacknet.\n" +
                    "DO NOT MAKE A BUG REPORT! I CAN'T DO ANYTHING ABOUT IT!!!");
            }
        }

        public const float DEFAULT_FORKBOMB_SPEED = 150f;

        public static void AddToNewForkbombRamCost(OSUpdateEvent osu)
        {
            if (!Active && newRamCost > 0.0f) { newRamCost = 0.0f; }
            if (!Active) return;

            var gameTime = (float)osu.GameTime.ElapsedGameTime.TotalSeconds;
            newRamCost += (gameTime * HollowZeroCore.ForkbombMultiplier) * DEFAULT_FORKBOMB_SPEED;
        }
    }
}
