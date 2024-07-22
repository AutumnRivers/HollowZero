using System.Text;
using Hacknet;

namespace HollowZero.Commands
{
    public class DebugCommands
    {
        public static void IncreaseInfection(OS os, string[] args)
        {
            if(!OS.DEBUG_COMMANDS)
            {
                os.validCommand = false;
                return;
            }

            if(args.Length < 2)
            {
                os.write("no");
                os.validCommand = false;
                return;
            }

            int amount = int.Parse(args[1]);

            HollowZeroCore.IncreaseInfection(amount);
        }

        public static void DecreaseInfection(OS os, string[] args)
        {
            if (!OS.DEBUG_COMMANDS)
            {
                os.validCommand = false;
                return;
            }

            if (args.Length < 2)
            {
                os.write("no");
                os.validCommand = false;
                return;
            }

            int amount = int.Parse(args[1]);

            HollowZeroCore.DecreaseInfection(amount);
        }
    }
}
