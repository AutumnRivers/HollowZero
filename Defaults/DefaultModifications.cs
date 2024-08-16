using Hacknet;

using Pathfinder.Port;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HollowZero
{
    public static class DefaultModifications
    {
        private static readonly List<Modification> modifications = new List<Modification>()
        {
            new WaterfallModifier(),
            new CloudbleedModifier(),
            new CloverModifier(),
            new VaccineModifier(),
            new ElModifier(),
            new CSECModifier(),
            new CoelModifier(),
            new MaskrModifier(),
            new QuikStrikeModifier()
        };

        public static ReadOnlyCollection<Modification> Mods {
            get {
                return new ReadOnlyCollection<Modification>(modifications);
            }
        }

        private class QuikStrikeModifier : Modification
        {
            public QuikStrikeModifier() : base("QuikStrike Mk.I", "quikstrike")
            {
                PowerLevels = new List<int>() { 1 };
                Trigger = ModTriggers.EnterNode;
                Effect = delegate (Computer comp)
                {
                    List<ShellExe> shells = new List<ShellExe>();

                    foreach(var exe in OS.currentInstance.exes)
                    {
                        if(exe is ShellExe shellExe)
                        {
                            shells.Add(shellExe);
                        }
                    }

                    if (!shells.Any() || !comp.hasProxy) return;
                    if (!AddEffectToComp(comp)) return;

                    for(var i = 0; i < PowerLevels[0]; i++)
                    {
                        if (i >= shells.Count) return;
                        var shell = shells[i];
                        shell.StartOverload();
                    }
                };
            }

            public override string Description
            {
                get
                {
                    return $"When connecting to a node with a proxy, immediately start an overload with {PowerLevels[0]} shell(s).";
                }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "QuikStrike Mk.II";
                PowerLevels = new List<int>() { 2 };
            }
        }

        private class WaterfallModifier : Modification
        {
            public WaterfallModifier() : base("Waterfall", "waterfall")
            {
                PowerLevels = new List<int>() { 1 };
                Trigger = ModTriggers.EnterNode;
                Effect = delegate (Computer comp)
                {
                    if (!AddEffectToComp(comp)) return;
                    if (comp.firewall == null) return;

                    comp.firewall.analysisPasses = PowerLevels[0];
                };
            }

            public override string Description
            {
                get { return $"Strips away ({PowerLevels[0]}) layer(s) of firewall on any node with a firewall. Triggers once per node."; }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                PowerLevels = new List<int>() { 3 };
                DisplayName = "Waterfall PRO";
            }
        }

        private class CloudbleedModifier : Modification
        {
            private float multiplier;

            public CloudbleedModifier() : base("Cloudbleed", "cloudbleed")
            {
                PowerLevels = new List<int>() { 20 };
                multiplier = 1.0f - (PowerLevels[0] * 0.01f);
                Trigger = ModTriggers.None;
                Effect = delegate (Computer comp)
                {
                    HollowZeroCore.ForkbombMultiplier = multiplier;
                };
            }

            public override string Description
            {
                get
                {
                    return $"Slows incoming forkbomb speeds by {PowerLevels[0]}%.";
                }
            }

            public override void Discard()
            {
                HollowZeroCore.ForkbombMultiplier += 1.0f - multiplier;
                base.Discard();
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Cloudgash";
                PowerLevels = new List<int>() { 35 };
                ResetMultiplier();
            }

            private void ResetMultiplier()
            {
                multiplier = 1.0f - (PowerLevels[0] * 0.01f);
            }
        }

        private class CloverModifier : Modification
        {
            public CloverModifier() : base("Cl0v3r's Gambit", "clover")
            {
                Description = "Clear all logs when leaving a node.";
                Trigger = ModTriggers.ExitNode;
                Effect = delegate (Computer comp)
                {
                    Folder logFolder = comp.getFolderFromPath("log");
                    logFolder.files.Clear();
                    comp.log($"LOG_CLEARED:CL0V3R");
                };
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Cl0v3r's Guarantee";
                Description += " Additionally, also forkbombs.";
                Effect = delegate (Computer comp)
                {
                    Folder logFolder = comp.getFolderFromPath("log");
                    logFolder.files.Clear();
                    comp.crash("CL0V3R");
                };
            }
        }

        private class VaccineModifier : Modification
        {
            public VaccineModifier() : base("Vaccine Shot", "vaccine")
            {
                PowerLevels = new List<int>() { 10 };
                Trigger = ModTriggers.OnInfectionGain;
                IsBlocker = true;
                ChanceEffect = comp =>
                {
                    Random random = new Random();
                    int chance = random.Next(0, 100);

                    if (chance <= PowerLevels[0])
                    {
                        OS.currentInstance.write($"Stay vaxxed, kids! (Infection Gain Nullified :: {DisplayName})");
                        return true;
                    }
                    return false;
                };
            }

            public override string Description
            {
                get
                {
                    return $"{PowerLevels[0]}% chance to nullify Infection gain.";
                }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Booster Shot";
                PowerLevels = new List<int>() { 25 };
            }
        }

        private class ElModifier : Modification
        {
            public ElModifier() : base("/el's Praise", "elcrack")
            {
                PowerLevels = new List<int>() { 50 };
                Trigger = ModTriggers.EnterNode;
                Effect = delegate (Computer comp)
                {
                    if (!AddEffectToComp(comp)) return;

                    if (PowerLevels[0] < 100)
                    {
                        Random random = new Random();
                        int chance = random.Next(0, 100);

                        if(chance <= PowerLevels[0])
                        {
                            comp.GetAllPortStates().GetRandom().SetCracked(true, EL_SIGNOFF);
                        }
                    } else
                    {
                        comp.GetAllPortStates().GetRandom().SetCracked(true, EL_SIGNOFF);
                    }
                };
            }

            private const string EL_SIGNOFF = "slashEl";

            public override string Description
            {
                get
                {
                    return $"{PowerLevels[0]}% chance for a random port to be cracked when entering a node. Triggers once per node.";
                }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "/el's Blessing";
                PowerLevels = new List<int>() { 100 };
                Upgraded = true;
            }
        }

        private class CSECModifier : Modification
        {
            public CSECModifier() : base("CSEC's Gift", "csecgift")
            {
                Trigger = ModTriggers.OnOverload;
                Description = "Protects you from gaining Malware (1) time before discarding itself. " +
                "If protecting from an Overload, lowers your Infection to 95% before discarding itself.";
                IsBlocker = true;
                AltEffect = delegate (int infectionLevel)
                {
                    if (infectionLevel > 95)
                    {
                        HollowZeroCore.InfectionLevel = 95;
                    }
                    LogCSECMessage();
                    Discard();
                };
            }

            private void LogCSECMessage()
            {
                OS.currentInstance.write("thank us later xoxo -csec");
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "CSEC's Benefit";
                Description = "Protects you from gaining Malware (1) time before discarding itself, and clears your Infection.";
                AltEffect = delegate (int _)
                {
                    HollowZeroCore.ClearInfection();
                    LogCSECMessage();
                    Discard();
                };
                Upgraded = true;
            }
        }

        private class CoelModifier : Modification
        {
            public CoelModifier() : base("Coel's Aid", "coelaid")
            {
                Trigger = ModTriggers.EnterNode;
                Effect = delegate (Computer comp)
                {
                    if (comp.adminIP != OS.currentInstance.thisComputer.ip) return;
                    if (comp.getDaemon(typeof(WhitelistConnectionDaemon)) == null) return;

                    OS.currentInstance.runCommand("ls");
                };
                Description = "Automatically shows the file list when gaining admin access to a WL source.";
            }

            public const string COEL_SIGNOFF = "coel";

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Coel's Assist";
                Description = "Automatically [ rm ] whitelist files when gaining admin access to a WL source.";
                Effect = delegate (Computer comp)
                {
                    if (comp.getDaemon(typeof(WhitelistConnectionDaemon)) == null) return;
                    Folder wlFolder = comp.getFolderFromPath("Whitelist");
                    if (wlFolder == null) return; // ??? this should NEVER fire
                    var path = comp.getFolderPath("Whitelist");

                    foreach(var file in wlFolder.files)
                    {
                        comp.deleteFile(COEL_SIGNOFF, file.name, path);
                    }
                };
            }
        }

        private class MaskrModifier : Modification
        {
            public MaskrModifier() : base("maskr_1.0", "maskr")
            {
                PowerLevels = new List<int>() { 10 };
                Trigger = ModTriggers.OnTraceTrigger;
                IsBlocker = true;
                TraceEffect = delegate (float traceTime)
                {
                    float newTime = traceTime + (traceTime * (PowerLevels[0] / 100.0f));
                    TraceManager.StartTrace(newTime);
                };
            }

            public override string Description
            {
                get
                {
                    return $"Slows traces by {PowerLevels[0]}%.";
                }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "maskr_2.0";
                PowerLevels = new List<int>() { 20 };
            }
        }
    }
}
