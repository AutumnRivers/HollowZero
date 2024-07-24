using Hacknet;

namespace HollowZero.Commands
{
    public class GuidebookCommands
    {
        public static void ActivateGuidebook(OS os, string[] args)
        {
            HollowZeroCore.GuidebookIsActive = true;
        }
    }
}
