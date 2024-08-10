using System.Linq;
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

        public static void AddMod(OS os, string[] args)
        {
            if(args.Length < 2)
            {
                os.write("alright buddy ol pal im gonna assume you want a random modification");
                HollowZeroCore.AddModification();
                return;
            }

            if(HollowZeroCore.PossibleModifications.TryFind(m => m.ID.ToLower() == args[1].ToLower(), out var mod))
            {
                os.write($"Adding Modficiation with ID of {mod.ID}...");
                HollowZeroCore.AddModification(mod);
            } else
            {
                os.write("what the heckarino. there's no modification with that id. terrible fortune, ancestor cry");
                os.validCommand = false;
                return;
            }
        }

        public static void UpgradeMod(OS os, string[] args)
        {
            var mod = HollowZeroCore.CollectedMods.Where(m => !m.Upgraded);

            if(!mod.Any())
            {
                os.write("there's no mods to upgrade. horrible fortune");
                os.validCommand = false;
                return;
            }

            var rMod = mod.GetRandom();
            os.write($"Upgrading {rMod.DisplayName}...");
            HollowZeroCore.UpgradeModification();
        }

        public static void AddCorruption(OS os, string[] args)
        {
            if (args.Length < 2)
            {
                os.write("alright buddy ol pal im gonna assume you want a random corruption");
                HollowZeroCore.AddCorruption();
                return;
            }

            if (HollowZeroCore.PossibleCorruptions.TryFind(m => m.ID.ToLower() == args[1].ToLower(), out var cor))
            {
                os.write($"Adding Corruption with ID of {cor.ID}...");
                HollowZeroCore.AddCorruption(cor);
            }
            else
            {
                os.write("what the heckarino. there's no corruption with that id. terrible fortune, ancestor cry");
                os.validCommand = false;
                return;
            }
        }
    }
}
