// Pure C# gameplay data. This folder must NOT reference UnityEngine so that the
// rules can be unit-tested outside the Unity editor (see Tests/LogicTests).
using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    /// <summary>The ten collectible weapon component types.</summary>
    public enum ComponentType
    {
        Barrel = 0,
        ExtendedBarrel = 1,
        DoubleBarrel = 2,
        Magazine = 3,
        ExtendedMagazine = 4,
        Stock = 5,
        Scope = 6,
        LaserSight = 7,
        Grip = 8,
        Muzzle = 9,
    }

    public enum ProjectileKind
    {
        Bullet = 0,
        HeavyBullet = 1,
        Explosive = 2,
        Rocket = 3,
    }

    /// <summary>Which procedural base model the assembler builds for a tier.</summary>
    public enum WeaponModel
    {
        BasicPistol = 0,
        ModifiedPistol = 1,
        DoubleBarrelPistol = 2,
        AssaultRifle = 3,
        AdvancedRifle = 4,
        Minigun = 5,
        HeavyCannon = 6,
        RocketLauncher = 7,
    }

    /// <summary>
    /// Static description of a collectible weapon component. All multipliers are
    /// applied once per upgrade level (level 1..maxLevel).
    /// </summary>
    [Serializable]
    public class ComponentDefinition
    {
        public string id;
        public string displayName;
        public ComponentType type;
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public int magazineBonus;
        public float reloadMultiplier = 1f;
        public float accuracyBonus;
        public float rangeBonus;
        public float critBonus;
        public int extraProjectiles;
        /// <summary>Evolution power granted per level.</summary>
        public int powerValue = 2;
        public int maxLevel = 3;
        /// <summary>Hex colour used for the pickup and notification (e.g. "FFB300").</summary>
        public string colorHex = "FFFFFF";
        public string description;

        public ComponentDefinition Clone() { return (ComponentDefinition)MemberwiseClone(); }
    }

    /// <summary>One stage of the weapon evolution path.</summary>
    [Serializable]
    public class WeaponTierDefinition
    {
        public int tier;
        public string displayName;
        public WeaponModel model;
        public int powerRequired;
        public float damage;
        public float fireRate;
        public int magazine;
        public float reloadTime;
        public float accuracy;
        public float range;
        public float critChance;
        public int projectilesPerShot = 1;
        public ProjectileKind projectile;
        public float splashRadius;
        public string colorHex = "9EA7B3";

        public WeaponTierDefinition Clone() { return (WeaponTierDefinition)MemberwiseClone(); }
    }

    public static class ComponentCatalog
    {
        static List<ComponentDefinition> _defaults;

        public static List<ComponentDefinition> CreateDefaults()
        {
            return new List<ComponentDefinition>
            {
                new ComponentDefinition { id = "barrel", displayName = "BARREL", type = ComponentType.Barrel,
                    damageMultiplier = 1.10f, rangeBonus = 2f, powerValue = 2, colorHex = "FFB300",
                    description = "Longer barrel: more damage and range." },
                new ComponentDefinition { id = "barrel_ext", displayName = "EXTENDED BARREL", type = ComponentType.ExtendedBarrel,
                    damageMultiplier = 1.18f, accuracyBonus = 0.04f, rangeBonus = 3f, powerValue = 3, colorHex = "FF8F00",
                    description = "Shrouded long barrel: big damage boost." },
                new ComponentDefinition { id = "barrel_double", displayName = "DOUBLE BARREL", type = ComponentType.DoubleBarrel,
                    damageMultiplier = 1.0f, extraProjectiles = 1, powerValue = 4, maxLevel = 2, colorHex = "FF5252",
                    description = "Adds another barrel: one more projectile per shot." },
                new ComponentDefinition { id = "mag", displayName = "MAGAZINE", type = ComponentType.Magazine,
                    magazineBonus = 6, powerValue = 2, colorHex = "40C4FF",
                    description = "More rounds before reloading." },
                new ComponentDefinition { id = "mag_ext", displayName = "DRUM MAGAZINE", type = ComponentType.ExtendedMagazine,
                    magazineBonus = 14, reloadMultiplier = 0.9f, powerValue = 3, colorHex = "00B0FF",
                    description = "Huge capacity and faster reloads." },
                new ComponentDefinition { id = "stock", displayName = "STOCK", type = ComponentType.Stock,
                    accuracyBonus = 0.08f, fireRateMultiplier = 1.05f, powerValue = 2, colorHex = "8D6E63",
                    description = "Steadier aim and slightly faster fire." },
                new ComponentDefinition { id = "scope", displayName = "SCOPE", type = ComponentType.Scope,
                    critBonus = 0.07f, rangeBonus = 5f, accuracyBonus = 0.03f, powerValue = 3, colorHex = "69F0AE",
                    description = "Critical hits and long range." },
                new ComponentDefinition { id = "laser", displayName = "LASER SIGHT", type = ComponentType.LaserSight,
                    accuracyBonus = 0.12f, critBonus = 0.04f, damageMultiplier = 1.04f, powerValue = 3, colorHex = "FF1744",
                    description = "Pinpoint accuracy." },
                new ComponentDefinition { id = "grip", displayName = "GRIP", type = ComponentType.Grip,
                    fireRateMultiplier = 1.12f, reloadMultiplier = 0.9f, powerValue = 2, colorHex = "B388FF",
                    description = "Faster fire rate and reload." },
                new ComponentDefinition { id = "muzzle", displayName = "MUZZLE BRAKE", type = ComponentType.Muzzle,
                    damageMultiplier = 1.12f, fireRateMultiplier = 1.05f, powerValue = 3, colorHex = "FFEA00",
                    description = "Punchier shots." },
            };
        }

        public static List<ComponentDefinition> Defaults
        {
            get { return _defaults ?? (_defaults = CreateDefaults()); }
        }

        /// <summary>Allows the Unity layer to replace definitions with ScriptableObject data.</summary>
        public static void Override(List<ComponentDefinition> definitions)
        {
            if (definitions != null && definitions.Count == Enum.GetValues(typeof(ComponentType)).Length)
                _defaults = definitions;
        }

        public static ComponentDefinition Get(ComponentType type)
        {
            var list = Defaults;
            for (int i = 0; i < list.Count; i++)
                if (list[i].type == type) return list[i];
            throw new ArgumentException("Unknown component " + type);
        }
    }

    public static class WeaponEvolution
    {
        static List<WeaponTierDefinition> _tiers;

        public const int MaxTier = 8;

        public static List<WeaponTierDefinition> CreateDefaults()
        {
            return new List<WeaponTierDefinition>
            {
                new WeaponTierDefinition { tier = 1, displayName = "BASIC PISTOL", model = WeaponModel.BasicPistol, powerRequired = 0,
                    damage = 10, fireRate = 2.0f, magazine = 8, reloadTime = 1.0f, accuracy = 0.80f, range = 18, critChance = 0.05f,
                    projectile = ProjectileKind.Bullet, colorHex = "9EA7B3" },
                new WeaponTierDefinition { tier = 2, displayName = "MODIFIED PISTOL", model = WeaponModel.ModifiedPistol, powerRequired = 3,
                    damage = 13, fireRate = 2.6f, magazine = 10, reloadTime = 0.95f, accuracy = 0.83f, range = 20, critChance = 0.06f,
                    projectile = ProjectileKind.Bullet, colorHex = "5C9DFF" },
                new WeaponTierDefinition { tier = 3, displayName = "DOUBLE BARREL", model = WeaponModel.DoubleBarrelPistol, powerRequired = 6,
                    damage = 13, fireRate = 2.8f, magazine = 12, reloadTime = 1.0f, accuracy = 0.82f, range = 20, critChance = 0.06f,
                    projectilesPerShot = 2, projectile = ProjectileKind.Bullet, colorHex = "26C6DA" },
                new WeaponTierDefinition { tier = 4, displayName = "ASSAULT RIFLE", model = WeaponModel.AssaultRifle, powerRequired = 10,
                    damage = 14, fireRate = 6.5f, magazine = 30, reloadTime = 1.3f, accuracy = 0.85f, range = 24, critChance = 0.07f,
                    projectile = ProjectileKind.Bullet, colorHex = "66BB6A" },
                new WeaponTierDefinition { tier = 5, displayName = "ADVANCED RIFLE", model = WeaponModel.AdvancedRifle, powerRequired = 14,
                    damage = 18, fireRate = 7.5f, magazine = 36, reloadTime = 1.2f, accuracy = 0.92f, range = 28, critChance = 0.10f,
                    projectile = ProjectileKind.Bullet, colorHex = "AB47BC" },
                new WeaponTierDefinition { tier = 6, displayName = "MINIGUN", model = WeaponModel.Minigun, powerRequired = 19,
                    damage = 15, fireRate = 16f, magazine = 120, reloadTime = 2.0f, accuracy = 0.78f, range = 26, critChance = 0.08f,
                    projectile = ProjectileKind.HeavyBullet, colorHex = "FFA726" },
                new WeaponTierDefinition { tier = 7, displayName = "HEAVY CANNON", model = WeaponModel.HeavyCannon, powerRequired = 25,
                    damage = 60, fireRate = 4.5f, magazine = 24, reloadTime = 1.6f, accuracy = 0.88f, range = 28, critChance = 0.10f,
                    projectile = ProjectileKind.Explosive, splashRadius = 2.5f, colorHex = "EF5350" },
                new WeaponTierDefinition { tier = 8, displayName = "ROCKET LAUNCHER", model = WeaponModel.RocketLauncher, powerRequired = 32,
                    damage = 150, fireRate = 2.2f, magazine = 8, reloadTime = 1.6f, accuracy = 0.95f, range = 32, critChance = 0.12f,
                    projectile = ProjectileKind.Rocket, splashRadius = 4f, colorHex = "FFD54F" },
            };
        }

        public static List<WeaponTierDefinition> Tiers
        {
            get { return _tiers ?? (_tiers = CreateDefaults()); }
        }

        public static void Override(List<WeaponTierDefinition> tiers)
        {
            if (tiers == null || tiers.Count != MaxTier) return;
            for (int i = 1; i < tiers.Count; i++)
                if (tiers[i].powerRequired <= tiers[i - 1].powerRequired) return; // must be strictly increasing
            _tiers = tiers;
        }

        public static WeaponTierDefinition GetTier(int tier)
        {
            if (tier < 1) tier = 1;
            if (tier > MaxTier) tier = MaxTier;
            return Tiers[tier - 1];
        }

        public static int TierForPower(int power)
        {
            var tiers = Tiers;
            int result = 1;
            for (int i = 0; i < tiers.Count; i++)
                if (power >= tiers[i].powerRequired) result = tiers[i].tier;
            return result;
        }

        /// <summary>0..1 progress from the current tier toward the next tier.</summary>
        public static float ProgressToNextTier(int power)
        {
            int tier = TierForPower(power);
            if (tier >= MaxTier) return 1f;
            int from = GetTier(tier).powerRequired;
            int to = GetTier(tier + 1).powerRequired;
            return (power - from) / (float)(to - from);
        }
    }
}
