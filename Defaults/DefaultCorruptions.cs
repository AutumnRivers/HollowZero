﻿using Hacknet;
using HollowZero.Patches;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero
{
    public class DefaultCorruptions
    {
        private static readonly List<Corruption> corruptions = new List<Corruption>()
        {
            new TerminalCorruption(),
            new CommandCorruption(),
            new PortHackCorruption(),
            new ShellCorruption(),
            new NetworkCorruption(),
            new ForkbombCorruption()
        };

        public static ReadOnlyCollection<Corruption> Corruptions
        {
            get
            {
                return new ReadOnlyCollection<Corruption>(corruptions);
            }
        }

        private class TerminalCorruption : Corruption
        {
            public TerminalCorruption() : base("Display Module Corruption")
            {
                Trigger = ModTriggers.Always;
                Effect = delegate (Computer comp)
                {
                    OS.currentInstance.terminalOnlyMode = true;
                };
                Description = "Corrupts your display module files, forcing you into terminal only mode. Sucks to suck! " +
                    "View visible nodes with the 'listnodes' command.";
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Display Module Termination";
                Description += " Lasts for five extra steps.";
                StepsLeft += 5;
            }

            public override void Discard()
            {
                base.Discard();
                OS.currentInstance.terminalOnlyMode = false;
                OS.currentInstance.terminal.writeLine("< :) > Display Module Restored");
            }
        }

        private class CommandCorruption : Corruption
        {
            private const string TIMER_ID = "commandcorrupter";
            private const int TIMER_SECONDS = 30;

            public CommandCorruption() : base("Command Runner Corruption")
            {
                Trigger = ModTriggers.None;
                PowerLevels = new List<int>() { 1 };
                CorruptionEffect = delegate ()
                {
                    var commands = ProgramList.programs;

                    Action corruptActionCommand = delegate ()
                    {
                        CommandDisabler.corruptedCommands.Clear();
                        OS.currentInstance.warningFlash();
                        for (int i = 0; i < PowerLevels[0]; i++)
                        {
                            var cmd = commands.GetRandom();
                            CommandDisabler.corruptedCommands.Add(cmd);
                            OS.currentInstance.terminal.writeLine($"<X> Command '{cmd}' disabled due to system instability!");
                        }
                    };

                    HollowTimer.AddTimer(TIMER_ID, TIMER_SECONDS, corruptActionCommand, true);
                };
            }

            public override string Description
            {
                get
                {
                    var message = $"Every {TIMER_SECONDS} seconds, disables {PowerLevels[0]} random command(s).";
                    if(OS.currentInstance.terminalOnlyMode) { message += " God help you."; }
                    return message;
                }
            }

            public override void Discard()
            {
                base.Discard();
                HollowTimer.RemoveTimer(TIMER_ID);
                CommandDisabler.corruptedCommands.Clear();
                OS.currentInstance.terminal.writeLine("< :) > Commands Restored");
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Command Module Corruption";
                PowerLevels = new List<int>() { 3 };
            }
        }

        private class PortHackCorruption : Corruption
        {
            public const int DEFAULT_STEPS_LEFT = 10;

            public PortHackCorruption() : base("Core Services Corruption")
            {
                Trigger = ModTriggers.None;
                PowerLevels = new List<int>() { 50 };
                CorruptionEffect = delegate ()
                {
                    PortHackExe.CRACK_TIME *= 100.0f / PowerLevels[0];
                };
                StepsLeft = DEFAULT_STEPS_LEFT;
            }

            public override string Description
            {
                get
                {
                    return $"Increases the time it takes for PortHack to run by {PowerLevels[0]}%.";
                }
            }

            public override void Discard()
            {
                base.Discard();
                PortHackExe.CRACK_TIME = 6.0f;
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Heart Corruption";
                PowerLevels = new List<int>() { 100 };
                StepsLeft += DEFAULT_STEPS_LEFT / 2;
            }
        }

        private class ShellCorruption : Corruption
        {
            public int CloseTimerMin = 120;
            public int CloseTimerMax = 300;

            public const string TIMER_ID = "shellcorrupter";

            public ShellCorruption() : base("Shell Services Corruption")
            {
                Trigger = ModTriggers.None;
                CorruptionEffect = delegate ()
                {
                    Action shellCorruptionAction = delegate ()
                    {
                        Random random = new Random();
                        int newSeconds = random.Next(CloseTimerMin, CloseTimerMax);
                        HollowTimer.ChangeTimer(TIMER_ID, newSeconds);

                        OS.currentInstance.shells.Clear();
                        foreach(var exe in OS.currentInstance.exes.Where(e => e.GetType() == typeof(ShellExe))) {
                            exe.needsRemoval = true;
                        }
                    };
                    HollowTimer.AddTimer(TIMER_ID, CloseTimerMax, shellCorruptionAction, true);
                };
                Description = "Closes all of your currently open shells at a random interval.";
                StepsLeft = 6;
            }

            public override void Discard()
            {
                base.Discard();
                HollowTimer.RemoveTimer(TIMER_ID);
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Shell Program Corruption";
                CloseTimerMin = 90;
                CloseTimerMax = 240;
                StepsLeft += 3;
            }
        }

        private class NetworkCorruption : Corruption
        {
            public const int DEFAULT_STEPS = 10;

            public NetworkCorruption() : base("Networking Services Corruption")
            {
                Trigger = ModTriggers.EnterNode;
                PowerLevels = new List<int>() { 15 };
                Description = "Randomly causes you to fail a connection when attempting to connect to a node.";
                ChanceEffect = comp =>
                {
                    Random random = new Random();
                    int chance = random.Next(0, 100);
                    if (chance < PowerLevels[0]) return false;
                    return true;
                };
                IsBlocker = true;
                StepsLeft = DEFAULT_STEPS;
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Network Corruption";
                PowerLevels = new List<int>() { 35 };
                StepsLeft += DEFAULT_STEPS / 2;
            }
        }

        private class ForkbombCorruption : Corruption
        {
            public int MinTimerSeconds = 120;
            public int MaxTimerSeconds = 600;

            public const string TIMER_ID = "forkbombcorruption";
            public const int DEFAULT_STEPS = 3; // Considering crashes are an instant run-ender, this can have a low value.

            public ForkbombCorruption() : base("Hardware Malfunction")
            {
                Trigger = ModTriggers.None;
                Description = "Causes your PC to be forkbombed at random intervals.";
                PowerLevels = new List<int>() { 15 };
                StepsLeft = DEFAULT_STEPS;
                MinimumLayer = 20;
                CorruptionEffect = delegate ()
                {
                    Random random = new Random();
                    int newTime = random.Next(MinTimerSeconds, MaxTimerSeconds);
                    HollowTimer.ChangeTimer(TIMER_ID, newTime);

                    void Warn()
                    {
                        OS.currentInstance.warningFlash();
                        OS.currentInstance.beepSound.Play();
                    }

                    int chance = random.Next(0, 100);
                    if(chance < PowerLevels[0])
                    {
                        OS.currentInstance.terminal.writeLine("<!> HARDWARE FAILURE IMMINENT <!>");
                        OS.currentInstance.terminal.writeLine("<!> PLEASE PREPARE FOR ANY CORE MALFUNCTIONS <!>");

                        Action countdown3 = delegate ()
                        {
                            Warn();
                            OS.currentInstance.terminal.writeLine("<!> 3... <!>");
                        };

                        Action countdown2 = delegate ()
                        {
                            Warn();
                            OS.currentInstance.terminal.writeLine("<!> 2... <!>");
                        };

                        Action countdown1 = delegate ()
                        {
                            Warn();
                            OS.currentInstance.terminal.writeLine("<!> 1... <!>");
                        };

                        Action final = delegate ()
                        {
                            Warn();
                            OS.currentInstance.delayer.Post(ActionDelayer.Wait(0.1), Warn);
                            OS.currentInstance.delayer.Post(ActionDelayer.Wait(0.2), Warn);
                            OS.currentInstance.terminal.writeLine("<!> MAJOR MALFUNCTION :: MAJOR MALFUNCTION <!>");
                            HollowManager.ForkbombComputer(OS.currentInstance.thisComputer);
                        };

                        HollowTimer.AddTimer("majormalfunction_3", 2.0f, countdown3);
                        HollowTimer.AddTimer("majormalfunction_2", 4.0f, countdown2);
                        HollowTimer.AddTimer("majormalfunction_1", 6.0f, countdown1);
                        HollowTimer.AddTimer("majormalfunction_f", 10.0f, final);
                    };
                };
            }

            public override void Discard()
            {
                base.Discard();
                HollowTimer.RemoveTimer(TIMER_ID);
                HollowTimer.RemoveTimer("majormalfunction_3");
                HollowTimer.RemoveTimer("majormalfunction_2");
                HollowTimer.RemoveTimer("majormalfunction_1");
                HollowTimer.RemoveTimer("majormalfunction_f");

                if(OS.currentInstance.exes.Any(e => e is ForkBombExe))
                {
                    ForkBombExe fb = (ForkBombExe)OS.currentInstance.exes.First(e => e is ForkBombExe);
                    fb.isExiting = true;
                }

                OS.currentInstance.terminal.writeLine("< :) > Hardware Stability Restored");
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Major Hardware Malfunction";
                StepsLeft += DEFAULT_STEPS - 1;
                MinTimerSeconds = 90;
                PowerLevels = new List<int>() { 20 };
            }
        }
    }
}
