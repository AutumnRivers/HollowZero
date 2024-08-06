using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;
using HollowZero.Daemons;
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

            if (__instance.daemons.Any(d => d.name == "Program Shop"))
            {
                ProgramShopDaemon programShop = (ProgramShopDaemon)__instance.daemons.First(d => d.name == "Program Shop");
                programShop.CreateSaleFilesIfMissing();
            }

            if(__instance.daemons.Any(d => d.name == "Gacha Shop"))
            {
                GachaShopDaemon gachaShop = (GachaShopDaemon)__instance.daemons.First(d => d.name == "Gacha Shop");
                gachaShop.RecreateChanceFilesIfMissing();
            }

            if(__instance.daemons.Any(d => d.GetType().IsSubclassOf(typeof(HollowDaemon))))
            {
                foreach(var daemon in __instance.daemons.Where(d => d.GetType().IsSubclassOf(typeof(HollowDaemon))))
                {
                    HollowDaemon h = (HollowDaemon)daemon;
                    h.OnDisconnect();
                }
            }
        }
    }
}
