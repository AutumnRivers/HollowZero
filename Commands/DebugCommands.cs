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

        public static void AddRandomMalware(OS os, string[] args)
        {
            if (!OS.DEBUG_COMMANDS)
            {
                os.validCommand = false;
                return;
            }

            HollowZeroCore.AddMalware();
        }

        public static void ClearMalware(OS os, string[] args)
        {
            if (!OS.DEBUG_COMMANDS)
            {
                os.validCommand = false;
                return;
            }

            foreach(var mal in HollowZeroCore.CollectedMalware.ToArray())
            {
                HollowZeroCore.RemoveMalware(mal);
            }
        }

        public static void AddCredits(OS os, string[] args)
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

            HollowZeroCore.AddPlayerCredits(int.Parse(args[1]));
        }

        public static void RemoveCredits(OS os, string[] args)
        {
            if (!OS.DEBUG_COMMANDS)
            {
                os.validCommand = false;
                return;
            }

            if (args.Length < 2)
            {
                os.write("woah now hold on there buddy you didnt put any args in. if you wanna remove all creds then do 'all' thanks");
                os.validCommand = false;
                return;
            }

            if (args[1] == "all")
            {
                HollowZeroCore.RemovePlayerCredits(10000);
            } else
            {
                HollowZeroCore.RemovePlayerCredits(int.Parse(args[1]));
            }
        }
    }
}
