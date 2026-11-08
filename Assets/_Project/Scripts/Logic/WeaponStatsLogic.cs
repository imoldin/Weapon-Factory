using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    /// <summary>Final, calculated weapon statistics used by the firing system and HUD.</summary>
    [Serializable]
    public struct WeaponStats
    {
        public float damage;          // per projectile, before crits
        public float fireRate;        // shots per second
        public int magazine;          // shots before reload
        public float reloadTime;      // seconds
        public float accuracy;        // 0..1, 1 = perfect
        public float range;           // world units
        public float critChance;      // 0..1
        public int projectilesPerShot;
        public ProjectileKind projectile;
        public float splashRadius;
        public int tier;

        /// <summary>Expected damage per second while not reloading (crit = x2).</summary>
        public float BurstDps
        {
            get { return damage * projectilesPerShot * fireRate * (1f + critChance); }
        }

        /// <summary>Expected sustained DPS including reload downtime.</summary>
        public float SustainedDps
        {
            get
            {
                if (fireRate <= 0f || magazine <= 0) return 0f;
                float magTime = magazine / fireRate;
                return BurstDps * magTime / (magTime + reloadTime);
            }
        }

        /// <summary>Single number shown in the HUD as "DMG".</summary>
        public int DisplayDamage { get { return (int)Math.Round(damage * projectilesPerShot); } }
    }

    /// <summary>Temporary modifiers collected from gates and items during one run.</summary>
    [Serializable]
    public class RunModifiers
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float magazineMultiplier = 1f;
        public int magazineBonus;
        public int extraProjectiles;
        public float critBonus;

        public const float MinDamageMultiplier = 0.35f;
        public const float MinFireRateMultiplier = 0.4f;
        public const float MinMagazineMultiplier = 0.4f;

        public void Clamp()
        {
            if (damageMultiplier < MinDamageMultiplier) damageMultiplier = MinDamageMultiplier;
            if (fireRateMultiplier < MinFireRateMultiplier) fireRateMultiplier = MinFireRateMultiplier;
            if (magazineMultiplier < MinMagazineMultiplier) magazineMultiplier = MinMagazineMultiplier;
            if (extraProjectiles < -1) extraProjectiles = -1; // tier minimum is still enforced later
            if (extraProjectiles > 5) extraProjectiles = 5;
            if (critBonus < -0.2f) critBonus = -0.2f;
        }
    }

    /// <summary>Bonuses coming from the permanent upgrade shop.</summary>
    [Serializable]
    public class PermanentBonuses
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float magazineMultiplier = 1f;
        public int startingPower;
        public float coinMultiplier = 1f;
        public float critBonus;
    }

    /// <summary>
    /// Which components the weapon currently has and at which level, plus bonus
    /// evolution power from upgrade modules. Mutated during a run.
    /// </summary>
    public class WeaponBuild
    {
        readonly int[] _levels = new int[Enum.GetValues(typeof(ComponentType)).Length];
        public int BonusPower { get; private set; }

        public WeaponBuild(int startingPower = 0)
        {
            BonusPower = Math.Max(0, startingPower);
        }

        public int GetLevel(ComponentType type) { return _levels[(int)type]; }

        public int ComponentCount
        {
            get { int c = 0; for (int i = 0; i < _levels.Length; i++) if (_levels[i] > 0) c++; return c; }
        }

        public int Power
        {
            get
            {
                int p = BonusPower;
                for (int i = 0; i < _levels.Length; i++)
                    if (_levels[i] > 0) p += ComponentCatalog.Get((ComponentType)i).powerValue * _levels[i];
                return Math.Max(0, p);
            }
        }

        public int Tier { get { return WeaponEvolution.TierForPower(Power); } }

        /// <summary>
        /// Adds a component (or levels it up). When already at max level the pickup
        /// converts into bonus power so it is never wasted. Returns the new level.
        /// </summary>
        public int AddComponent(ComponentType type)
        {
            var def = ComponentCatalog.Get(type);
            int idx = (int)type;
            if (_levels[idx] < def.maxLevel)
            {
                _levels[idx]++;
            }
            else
            {
                BonusPower += Math.Max(1, def.powerValue);
            }
            return _levels[idx];
        }

        public void AddPower(int amount)
        {
            BonusPower = Math.Max(0, BonusPower + amount);
        }

        /// <summary>Removes one level from a random owned component. Returns false if none owned.</summary>
        public bool RemoveRandomComponent(Random rng, out ComponentType removed)
        {
            removed = ComponentType.Barrel;
            int owned = ComponentCount;
            if (owned == 0) return false;
            int pick = rng.Next(owned);
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i] <= 0) continue;
                if (pick-- == 0)
                {
                    _levels[i]--;
                    removed = (ComponentType)i;
                    return true;
                }
            }
            return false;
        }

        public bool RemoveComponentLevel(ComponentType type)
        {
            int idx = (int)type;
            if (_levels[idx] <= 0) return false;
            _levels[idx]--;
            return true;
        }

        public IEnumerable<KeyValuePair<ComponentType, int>> OwnedComponents()
        {
            for (int i = 0; i < _levels.Length; i++)
                if (_levels[i] > 0) yield return new KeyValuePair<ComponentType, int>((ComponentType)i, _levels[i]);
        }
    }

    public static class StatCalculator
    {
        public const float MaxFireRate = 30f;
        public const float MaxCrit = 0.75f;
        public const int MaxProjectiles = 6;

        /// <summary>
        /// Final Damage = Tier Base x Component Multipliers x Gate Multipliers x Permanent Multiplier,
        /// with a soft penalty per extra projectile so multi-barrel builds do not explode.
        /// </summary>
        public static WeaponStats Calculate(WeaponBuild build, RunModifiers run, PermanentBonuses perm)
        {
            if (run == null) run = new RunModifiers();
            if (perm == null) perm = new PermanentBonuses();
            var tierDef = WeaponEvolution.GetTier(build.Tier);

            float dmgMul = 1f, frMul = 1f, reloadMul = 1f;
            float acc = tierDef.accuracy, range = tierDef.range, crit = tierDef.critChance;
            int magBonus = 0, extraProj = 0;

            // Magazine bonuses scale with the base magazine so they matter for every tier.
            float magScale = tierDef.magazine / 12f;
            if (magScale < 0.5f) magScale = 0.5f;
            if (magScale > 4f) magScale = 4f;

            foreach (var pair in build.OwnedComponents())
            {
                var c = ComponentCatalog.Get(pair.Key);
                int lvl = pair.Value;
                dmgMul *= (float)Math.Pow(c.damageMultiplier, lvl);
                frMul *= (float)Math.Pow(c.fireRateMultiplier, lvl);
                reloadMul *= (float)Math.Pow(c.reloadMultiplier, lvl);
                acc += c.accuracyBonus * lvl;
                range += c.rangeBonus * lvl;
                crit += c.critBonus * lvl;
                magBonus += (int)Math.Round(c.magazineBonus * lvl * magScale);
                extraProj += c.extraProjectiles * lvl;
            }

            var s = new WeaponStats();
            s.tier = tierDef.tier;
            s.projectile = tierDef.projectile;
            s.splashRadius = tierDef.splashRadius;

            s.projectilesPerShot = Clamp(tierDef.projectilesPerShot + extraProj + run.extraProjectiles, 1, MaxProjectiles);
            float spreadPenalty = 1f / (1f + 0.15f * (s.projectilesPerShot - 1));

            s.damage = tierDef.damage * dmgMul * run.damageMultiplier * perm.damageMultiplier * spreadPenalty;
            s.fireRate = Math.Min(MaxFireRate, tierDef.fireRate * frMul * run.fireRateMultiplier * perm.fireRateMultiplier);
            float mag = (tierDef.magazine + magBonus + run.magazineBonus) * run.magazineMultiplier * perm.magazineMultiplier;
            s.magazine = Math.Max(1, (int)Math.Round(mag));
            s.reloadTime = Math.Max(0.3f, tierDef.reloadTime * reloadMul);
            s.accuracy = Clamp01(acc);
            s.range = range;
            s.critChance = Math.Max(0f, Math.Min(MaxCrit, crit + run.critBonus + perm.critBonus));
            return s;
        }

        static int Clamp(int v, int min, int max) { return v < min ? min : (v > max ? max : v); }
        static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }
}
