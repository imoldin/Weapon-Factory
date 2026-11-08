using System;
using UnityEngine;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Weapons
{
    /// <summary>
    /// Owns the weapon's run state (components, gate modifiers, permanent bonuses), recalculates
    /// statistics, rebuilds the visual model when anything changes and broadcasts events
    /// (stats changed, component added/lost, evolution tier changed).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public WeaponBuild Build { get; private set; }
        public RunModifiers Run { get; private set; }
        public PermanentBonuses Perm { get; private set; }
        public WeaponStats Stats { get; private set; }
        public WeaponSkinDefinition Skin { get; private set; }
        public WeaponRig Rig { get; private set; }
        public Transform ModelParent { get; private set; }

        public event Action<WeaponStats> StatsChanged;
        /// <summary>(oldTier, newTier)</summary>
        public event Action<int, int> TierChanged;
        public event Action<WeaponRig> RigRebuilt;

        public int Tier { get { return Stats.tier; } }
        public int Power { get { return Build.Power; } }
        public float TierProgress { get { return WeaponEvolution.ProgressToNextTier(Build.Power); } }
        public string WeaponName { get { return WeaponEvolution.GetTier(Stats.tier).displayName; } }
        public WeaponModel Model { get { return WeaponEvolution.GetTier(Stats.tier).model; } }

        readonly System.Random _rng = new System.Random();

        public void Init(Transform modelParent, PermanentBonuses perm, WeaponSkinDefinition skin)
        {
            ModelParent = modelParent;
            Perm = perm ?? new PermanentBonuses();
            Skin = skin ?? SkinCatalog.All[0];
            Build = new WeaponBuild(Perm.startingPower);
            Run = new RunModifiers();
            Stats = StatCalculator.Calculate(Build, Run, Perm);
            RebuildModel(null);
        }

        /// <summary>For menu previews: show a specific tier with a set of components.</summary>
        public void InitShowcase(Transform modelParent, int tier, WeaponSkinDefinition skin, bool withParts)
        {
            ModelParent = modelParent;
            Perm = new PermanentBonuses();
            Skin = skin ?? SkinCatalog.All[0];
            Build = new WeaponBuild(WeaponEvolution.GetTier(tier).powerRequired);
            if (withParts)
            {
                if (tier >= 2) Build.AddComponent(ComponentType.Barrel);
                if (tier >= 3) Build.AddComponent(ComponentType.Magazine);
                if (tier >= 4) Build.AddComponent(ComponentType.Scope);
                if (tier >= 5) Build.AddComponent(ComponentType.LaserSight);
                if (tier >= 5) Build.AddComponent(ComponentType.Stock);
                if (tier >= 6) Build.AddComponent(ComponentType.Muzzle);
            }
            // components add power too — keep the requested tier
            while (Build.Tier > tier && Build.BonusPower > 0) Build.AddPower(-1);
            Run = new RunModifiers();
            Stats = StatCalculator.Calculate(Build, Run, Perm);
            RebuildModel(null);
        }

        public void SetSkin(WeaponSkinDefinition skin)
        {
            Skin = skin;
            RebuildModel(null);
        }

        // ---------------- mutations ----------------

        public int AddComponent(ComponentType type)
        {
            int lvl = Build.AddComponent(type);
            Refresh(type);
            return lvl;
        }

        public bool LoseRandomComponent(out ComponentType lost)
        {
            bool ok = Build.RemoveRandomComponent(_rng, out lost);
            if (ok) Refresh(null);
            return ok;
        }

        public void AddPower(int amount)
        {
            Build.AddPower(amount);
            Refresh(null);
        }

        public void ApplyGate(GateEffect effect)
        {
            effect.Apply(Run, Build);
            Refresh(null);
        }

        /// <summary>Multiplies run damage (boosters: +x, penalties: -x as fraction, e.g. 0.2 = +20%).</summary>
        public void ModifyDamage(float fraction)
        {
            Run.damageMultiplier *= 1f + fraction;
            Run.Clamp();
            Refresh(null);
        }

        public void ModifyFireRate(float fraction)
        {
            Run.fireRateMultiplier *= 1f + fraction;
            Run.Clamp();
            Refresh(null);
        }

        void Refresh(ComponentType? highlight)
        {
            int oldTier = Stats.tier;
            var old = Stats;
            Stats = StatCalculator.Calculate(Build, Run, Perm);
            RebuildModel(highlight);
            if (StatsChanged != null) StatsChanged(Stats);
            if (Stats.tier != oldTier && TierChanged != null) TierChanged(oldTier, Stats.tier);
            if (old.projectilesPerShot != Stats.projectilesPerShot || Stats.tier != oldTier)
                Tween.Punch(ModelParent, 0.25f, 0.3f);
        }

        public void RebuildModel(ComponentType? highlight)
        {
            if (Rig != null && Rig.root != null) Destroy(Rig.root.gameObject);
            Rig = WeaponAssembler.Build(ModelParent, Model, Build, Stats, Run, Skin, highlight);
            if (RigRebuilt != null) RigRebuilt(Rig);
        }
    }
}
