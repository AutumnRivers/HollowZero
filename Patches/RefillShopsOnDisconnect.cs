using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;

using HollowZero.Daemons.Shop;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    internal class RefillShopsOnDisconnect
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Computer),nameof(Computer.disconnecting))]
        public static void CheckForPlayerDisconnectOnShop(Computer __instance, string ipFrom)
        {
            if (ipFrom != OS.currentInstance.thisComputer.ip) return;
            if (!__instance.daemons.Any(d => d.name == "Program Shop")) return;

            ProgramShopDaemon programShop = (ProgramShopDaemon)__instance.daemons.First(d => d.name == "Program Shop");
            programShop.CreateSaleFilesIfMissing();
        }
    }
}
