using Hacknet;

namespace HollowZero.Commands
{
    public class InventoryCommands
    {
        public static void ShowInventory(OS os, string[] args)
        {
            HollowZeroCore.CurrentUIState = HollowZeroCore.UIState.Inventory;
        }
    }
}
