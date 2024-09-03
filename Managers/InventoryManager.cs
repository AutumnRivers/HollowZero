using Hacknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static HollowZero.Managers.HollowGlobalManager;

namespace HollowZero.Managers
{
    public static class InventoryManager
    {
        public static void AddModification(Modification mod = null)
        {
            if (HollowZeroCore.CollectedMods.Any(m => m.ID == mod?.ID)) return;

            Modification GetModification()
            {
                var modf = PossibleModifications.GetRandom();
                if (HollowZeroCore.CollectedMods.Any(m => m.ID == modf.ID)) return GetModification();
                return modf;
            }

            var modification = mod ??= GetModification();

            HollowZeroCore.CollectedMods.Add(modification);
            if (modification.Trigger == Modification.ModTriggers.None)
            {
                modification.Effect(null);
            }
        }

        public static void UpgradeModification(Modification mod = null)
        {
            if (mod == null && !HollowZeroCore.CollectedMods.Any(m => !m.Upgraded)) return;
            var modf = mod ??= HollowZeroCore.CollectedMods.Where(m => !m.Upgraded).GetRandom();

            if (HollowZeroCore.CollectedMods.TryFind(m => m.ID == modf.ID, out var modification))
            {
                int index = HollowZeroCore.CollectedMods.IndexOf(modification);
                HollowZeroCore.CollectedMods[index].Upgrade();
            }
            else
            {
                modf.Upgrade();
                HollowZeroCore.CollectedMods.Add(modf);
            }
        }

        public static void AddCorruption(Corruption corruption = null)
        {
            if (HollowZeroCore.CollectedCorruptions.Any(c => c.ID == corruption?.ID)) return;

            Corruption GetCorruption()
            {
                var cor = DefaultCorruptions.Corruptions.GetRandom();
                if (HollowZeroCore.CollectedCorruptions.Any(c => c.ID == cor.ID)) return GetCorruption();
                return cor;
            }

            var corr = corruption ??= GetCorruption();

            HollowZeroCore.CollectedCorruptions.Add(corr);
            if (corr.Trigger == Modification.ModTriggers.None)
            {
                corr.CorruptionEffect();
            }
        }

        public static void UpgradeCorruption(Corruption corruption)
        {
            if (HollowZeroCore.CollectedCorruptions.TryFind(c => c.ID == corruption.ID, out var corr))
            {
                int index = HollowZeroCore.CollectedCorruptions.IndexOf(corr);
                HollowZeroCore.CollectedCorruptions[index].Upgrade();
            }
        }

        public static void AddMalware(Malware malware = null)
        {
            foreach (var mod in HollowZeroCore.CollectedMods.Where(m => m.Trigger == Modification.ModTriggers.OnOverload))
            {
                if (mod.IsBlocker && mod.ChanceEffect != null)
                {
                    if (mod.ChanceEffect(OS.currentInstance.thisComputer)) return;
                }
                else if (mod.IsBlocker)
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, PlayerManager.InfectionLevel);
                    return;
                }
                else
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, PlayerManager.InfectionLevel);
                }
            }

            static Malware GetMalware()
            {
                Malware m = GetRandomMalware();
                if (HollowZeroCore.CollectedMalware.Contains(m))
                {
                    return GetMalware();
                }
                return m;
            }

            malware ??= GetMalware();

            HollowZeroCore.CollectedMalware.Add(malware);
            if (malware.SetTimer)
            {
                MalwareEffects.AddMalwareTimer(malware, malware.PowerLevel);
            }

            MalwareOverlay.CurrentMalware = malware;
        }

        public static void RemoveMalware(Malware malware = null)
        {
            malware ??= HollowZeroCore.CollectedMalware.GetRandom();

            List<Computer> affectedComps = new List<Computer>();
            if (MalwareEffects.AffectedComps.Exists(c => c.AppliedEffects.Contains(malware.DisplayName)))
            {
                foreach (var comp in MalwareEffects.AffectedComps.Where(c => c.AppliedEffects.Contains(malware.DisplayName)))
                {
                    comp.AppliedEffects.Remove(malware.DisplayName);
                    var affectedComp = OS.currentInstance.netMap.nodes.First(c => c.idName == comp.CompID);
                    affectedComps.Add(affectedComp);
                }
            }

            if (malware.RemoveAction != null)
            {
                malware.RemoveAction(malware.PowerLevel, affectedComps);
            }
            HollowZeroCore.CollectedMalware.Remove(malware);
        }

        public static Malware GetRandomMalware()
        {
            return PossibleMalware.GetRandom();
        }
    }
}
