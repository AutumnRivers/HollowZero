using Hacknet;

namespace HollowZero.Commands
{
    public class GuidebookCommands
    {
        public static void ActivateGuidebook(OS os, string[] args)
        {
            HollowZeroCore.CurrentUIState = HollowZeroCore.UIState.Guidebook;
        }
    }
}
