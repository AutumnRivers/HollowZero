using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Hacknet;
using Hacknet.Screens;
using Hacknet.Extensions;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class ListLoadedPacks
    {
        public static bool hasListed = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionsMenuScreen),nameof(ExtensionsMenuScreen.DrawExtensionInfoDetail))]
        public static bool AddLoadedPacks(Vector2 drawpos, Rectangle dest, SpriteBatch sb, ScreenManager screenMan, ref ExtensionInfo info)
        {
            if (HollowZeroCore.knownPacks.Count == 0 || hasListed) return true;

            StringBuilder packList = new StringBuilder("\n\nCurrently loaded Hollow Packs:\n");
            foreach(var pack in HollowZeroCore.knownPacks)
            {
                packList.Append($"* \"{pack.Key}\" by {pack.Value}\n");
            }
            info.Description += packList.ToString();
            hasListed = true;

            return true;
        }
    }
}
