using Hacknet;

using System;
using System.Collections.Generic;

using static HollowZero.HollowLogger;

namespace HollowZero
{
    public class Modification
    {
        public Modification(string name, string id)
        {
            DisplayName = name;
            ID = id;
        }

        public enum ModTriggers
        {
            EnterNode, ExitNode, GainAdminAccess,
            OnForkbomb, OnOverload, OnInfectionGain,
            OnTraceTrigger, None, Always
        }

        public string ID { get; set; }
        public string DisplayName { get; set; }
        public virtual string Description { get; set; }
        public List<int> PowerLevels { get; set; }
        public ModTriggers Trigger { get; set; }
        public bool Upgraded = false;
        public Modification UpgradedModification { get; set; }

        public List<string> affectedCompIDs = new List<string>();
        public virtual Action<Computer> Effect { get; set; }
        public virtual Func<Computer, bool> ChanceEffect { get; set; }
        public Action<int> AltEffect { get; set; }
        public Action<float> TraceEffect { get; set; }

        public bool IsBlocker = false;
        public const bool IsCorruption = false;

        public int MinimumLayer = 0;

        public bool AddEffectToComp(Computer comp)
        {
            if (affectedCompIDs.Contains(comp.idName)) return false;

            affectedCompIDs.Add(comp.idName);
            return true;
        }

        public void OnLayerChange()
        {
            affectedCompIDs.Clear();
        }

        public virtual void Discard()
        {
            if (HollowZeroCore.CollectedMods.Contains(this))
            {
                HollowZeroCore.CollectedMods.Remove(this);
            }
        }

        public virtual void Upgrade()
        {
            if (Upgraded) return;
            Upgraded = true;
        }

        public void LaunchEffect(Computer comp = null, int alt = default)
        {
            if (Effect != null)
            {
                Effect(comp);
                return;
            }

            if (AltEffect != null)
            {
                AltEffect(alt);
                return;
            }

            LogError(HollowZeroCore.HZLOG_PREFIX +
                $"Couldn't determine effect for modification with ID of {ID}");
        }
    }

    public class Corruption : Modification
    {
        public Corruption(string name, string id) : base(name, id) { }

        public new const bool IsCorruption = true;

        public int StepsLeft = 5;
        public List<string> visitedNodeIDs = new List<string>();

        public Action CorruptionEffect { get; set; }

        public override void Discard()
        {
            if (HollowZeroCore.CollectedCorruptions.Contains(this))
            {
                HollowZeroCore.CollectedCorruptions.Remove(this);
            }
        }

        public void TakeStep()
        {
            if (StepsLeft-- <= 0)
            {
                Discard();
            }
        }
    }
}
