using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Pathfinder.Replacements;

using MonoMod.Cil;
using Mono.Cecil.Cil;

using System.Reflection;

namespace HollowZero.Fixes
{
    [HarmonyPatch]
    public class DaemonContentFix
    {
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(ContentLoader), nameof(ContentLoader.LoadComputer))]
        public static void RecognizeDaemonContentPatch(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // This is my first ILManip written from scratch so it will look ugly
            c.GotoNext(MoveType.After,
                x => x.MatchLdstr("Computer."),
                x => x.MatchLdloc(6),
                x => x.MatchCallvirt<MemberInfo>("get_Name"),
                x => x.MatchCall<string>("Concat"),
                x => x.MatchLdsfld(out var _),
                x => x.MatchDup(),
                x => x.MatchBrtrue(out ILLabel _),
                x => x.MatchPop(),
                x => x.MatchLdsfld(out var _),
                x => x.MatchLdftn(out var _),
                x => x.MatchNewobj(out var _),
                x => x.MatchDup(),
                x => x.MatchStsfld(out var _),
                x => x.MatchLdcI4(0)
                );
            c.Previous.OpCode = OpCodes.Ldc_I4_1;
            // I am never touching IL again after this.
        }
    }
}
