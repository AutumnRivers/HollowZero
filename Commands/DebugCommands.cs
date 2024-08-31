using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Hacknet;

using HollowZero.Nodes.LayerSystem;

using static HollowZero.HollowLogger;

namespace HollowZero.Commands
{
    public class DebugCommands
    {
        public static Dictionary<MethodInfo, string> Aliases = new Dictionary<MethodInfo, string>()
        {
            { FindMethod("IncreaseInfection"), "upinf" },
            { FindMethod("DecreaseInfection"), "downinf" },
            { FindMethod("AddRandomMalware"), "addmal" },
            { FindMethod("ClearMalware"), "clearmal" },
            { FindMethod("AddCredits"), "addcreds" },
            { FindMethod("RemoveCredits"), "takecreds" },
            { FindMethod("AddMod"), "addmod" },
            { FindMethod("UpgradeMod"), "upmod" },
            { FindMethod("AddCorruption"), "addcorr" },
            { FindMethod("ListTimers"), "timers" },
            { FindMethod("SetForkbombSpeed"), "setfbs" },
            { FindMethod("GenerateRandomLayer"), "grlayer" },
            { FindMethod("GenerateSolvableLayer"), "gslayer" }
        };

        private static MethodInfo FindMethod(string name)
        {
            return typeof(DebugCommands).GetMethod(name);
        }

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

        public static void ListTimers(OS os, string[] args)
        {
            foreach(var timer in HollowTimer.timers)
            {
                bool active = timer.IsActive;
#pragma warning disable IDE0071
                os.write($"Timer ID: {timer.ID}, {timer.SecondsLeft.ToString("n2")}s left. " +
                    "(" + (active ? "ACTIVE" : "INACTIVE") + ")");
#pragma warning restore IDE0071
            }
        }

        public static void SetForkbombSpeed(OS os, string[] args)
        {
            if(args.Length < 2)
            {
                os.write("no");
                os.validCommand = false;
                return;
            }

            if (!float.TryParse(args[1], out float mult))
            {
                os.write("that's not a float, IDIOT!");
                os.validCommand = false;
                return;
            }

            HollowZeroCore.ForkbombMultiplier = mult;
        }

        public static void GenerateRandomLayer(OS os, string[] args)
        {
            var randomLayer = LayerGenerator.GenerateTrueRandomLayer();
            LogImportant(randomLayer.ToString());
            os.write("Sent layer details to actual terminal");
        }

        public static void GenerateSolvableLayer(OS os, string[] args)
        {
            var solvableLayer = LayerGenerator.GenerateSolvableLayer();
            if(!solvableLayer.Solvable)
            {
                LogWarning("--- FOLLOWING LAYER IS NOT SOLVABLE! SOMEONE MESSED UP, AND IT'S PROBABLY YOU ---");
                os.write("<!!!> Note: the following layer is NOT solvable! <!!!>");
            }
            LogImportant(solvableLayer.ToString());
            os.write("Sent layer details to actual terminal");
        }
    }
}
