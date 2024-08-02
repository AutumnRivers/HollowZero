﻿using Hacknet;
using Pathfinder.Port;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero
{
    public static class DefaultModifications
    {
        private static readonly List<Modification> modifications = new List<Modification>()
        {
            /*new Modification("QuikStrike Mk.I")
            {
                Description = "On any node with a proxy, immediately calls upon (1) shell to overload when connecting.",
                PowerLevels = new List<int>() { 1 },
                Upgraded = false,
                UpgradedModification = new Modification("QuikStrike Mk.I-rev.1")
                {
                    Description = "On any node with a proxy, immediately calls upon (1) shell to overload when connecting at 1.1x speed.",
                    PowerLevels = new List<int>() { 1 },
                    Upgraded = true,
                }
            },*/
            new WaterfallModifier(),
            new CloudbleedModifier(),
            new CloverModifier(),
            new VaccineModifier(),
            new ElModifier(),
            new CSECModifier(),
            new CoelModifier(),
            new MaskrModifier()
        };

        public static ReadOnlyCollection<Modification> Mods {
            get {
                return new ReadOnlyCollection<Modification>(modifications);
            }
        }

        private class WaterfallModifier : Modification
        {
            public WaterfallModifier() : base("Waterfall")
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
            public CloudbleedModifier() : base("Cloudbleed")
            {
                PowerLevels = new List<int>() { 10 };
                Trigger = ModTriggers.OnForkbomb;
                Effect = delegate (Computer comp)
                {
                    return; // The patch handles this
                };
            }

            public override string Description
            {
                get
                {
                    return $"Slows incoming forkbomb speeds by {PowerLevels[0]}%.";
                }
            }

            public override void Upgrade()
            {
                if (Upgraded) return;
                base.Upgrade();

                DisplayName = "Cloudgash";
                PowerLevels = new List<int>() { 25 };
            }
        }

        private class CloverModifier : Modification
        {
            public CloverModifier() : base("Cl0v3r's Gambit")
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
            public VaccineModifier() : base("Vaccine Shot")
            {
                PowerLevels = new List<int>() { 10 };
                Trigger = ModTriggers.OnInfectionGain;
                IsBlocker = true;
                ChanceEffect = comp =>
                {
                    Random random = new Random();
                    int chance = random.Next(0, 100);

                    if (chance <= PowerLevels[0]) return true;
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
            public ElModifier() : base("/el's Praise")
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
            public CSECModifier() : base("CSEC's Gift")
            {
                Trigger = ModTriggers.OnOverload;
                Description = "Protects you from gaining Malware (1) time before discarding itself. " +
                "If protecting from an Overload, lowers your Infection to 95% before discarding itself.";
                IsBlocker = true;
                AltEffect = delegate (int infectionLevel)
                {
                    if (infectionLevel > 99)
                    {
                        HollowZeroCore.InfectionLevel = 95;
                    }
                    Discard();
                };
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
                    Discard();
                };
                Upgraded = true;
            }
        }

        private class CoelModifier : Modification
        {
            public CoelModifier() : base("Coel's Aid")
            {
                Trigger = ModTriggers.GainAdminAccess;
                Effect = delegate (Computer comp)
                {
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
            public MaskrModifier() : base("maskr_1.0")
            {
                PowerLevels = new List<int>() { 10 };
                Trigger = ModTriggers.OnTraceTrigger;
                IsBlocker = true;
                TraceEffect = delegate (float traceTime)
                {
                    float newTime = traceTime * (PowerLevels[0] / 100.0f);
                    OS.currentInstance.traceTracker.start(newTime);
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