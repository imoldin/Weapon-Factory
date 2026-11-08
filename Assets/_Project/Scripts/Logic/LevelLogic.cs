using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    public enum EnvironmentTheme
    {
        IndustrialFactory = 0,
        TechLaboratory = 1,
        RobotFactory = 2,
    }

    public enum EnemyType
    {
        BasicRobot = 0,
        ArmoredRobot = 1,
        Drone = 2,
        HeavySecurity = 3,
        Boss = 4,
    }

    public enum PickupKind
    {
        Component = 0,
        Coin = 1,
        UpgradeModule = 2,   // +power
        DamageBooster = 3,   // +damage %
        FireRateBooster = 4, // +fire rate %
        UpgradeBox = 5,      // random component + power + coins
        RepairKit = 6,       // restores integrity
        // negatives
        BrokenPart = 20,     // lose a component level
        RustedPart = 21,     // -damage
        DamageReducer = 22,  // -damage (bigger)
        WeightPenalty = 23,  // -fire rate
        ExplosiveHazard = 24 // integrity damage + lose a component
    }

    public enum SegmentType
    {
        Empty = 0,
        Coins = 1,           // line of coins at x
        ComponentItem = 2,   // single component pickup at x
        ComponentChoice = 3, // component (left) vs component2 (right)
        GatePair = 4,
        Saw = 5,             // static spinning saw at x
        MovingSaw = 6,       // saw sliding left-right
        Crusher = 7,         // piston slamming down at x
        LaserWall = 8,       // laser fence with a gap at x
        Barrier = 9,         // breakable wall with HP covering one side
        ExplosiveBarrels = 10,
        Pickup = 11,         // generic pickup (pickup kind) at x
        Mixed = 12,          // positive pickup at x, negative pickup at -x
        Targets = 13,        // shootable targets, give coins
        Hammer = 14,         // swinging hammer
    }

    [Serializable]
    public class SegmentDefinition
    {
        public SegmentType type;
        public float x;
        public ComponentType component;
        public ComponentType component2;
        public PickupKind pickup;
        public PickupKind pickup2;
        public int count = 1;
        public float value;
        public GatePairDefinition gate;

        public float Length
        {
            get
            {
                switch (type)
                {
                    case SegmentType.Coins: return 4f + 1.6f * Math.Max(1, count);
                    case SegmentType.GatePair: return 16f;
                    case SegmentType.LaserWall:
                    case SegmentType.Crusher:
                    case SegmentType.Hammer:
                    case SegmentType.MovingSaw: return 14f;
                    case SegmentType.Barrier: return 14f;
                    case SegmentType.Targets: return 12f;
                    case SegmentType.Empty: return Math.Max(4f, value);
                    default: return 11f;
                }
            }
        }
    }

    [Serializable]
    public class EnemySpawn
    {
        public EnemyType type;
        public int count = 1;
        public EnemySpawn() { }
        public EnemySpawn(EnemyType t, int c) { type = t; count = c; }
    }

    [Serializable]
    public class WaveDefinition
    {
        public List<EnemySpawn> spawns = new List<EnemySpawn>();
        /// <summary>Seconds after the previous wave is cleared.</summary>
        public float delay = 0.8f;

        public int TotalCount { get { int c = 0; foreach (var s in spawns) c += s.count; return c; } }
    }

    [Serializable]
    public class LevelDefinition
    {
        public int levelNumber;
        public string displayName;
        public EnvironmentTheme theme;
        public float runSpeed = 9f;
        public float enemyHealthMultiplier = 1f;
        public float gateDifficulty = 0.3f;
        public bool isBossLevel;
        public string bossName = "FACTORY BOSS";
        public int baseReward = 60;
        public float arenaTimeLimit = 45f;
        public int seed = 1;
        public bool tutorial;
        public string introText = "";
        public List<SegmentDefinition> segments = new List<SegmentDefinition>();
        public List<WaveDefinition> waves = new List<WaveDefinition>();

        public const float StartPadding = 16f;
        public const float EndPadding = 10f;

        public float TrackLength
        {
            get
            {
                float len = StartPadding + EndPadding;
                foreach (var s in segments) len += s.Length;
                return len;
            }
        }

        public int TotalEnemies { get { int c = 0; foreach (var w in waves) c += w.TotalCount; return c; } }
    }

    [Serializable]
    public class EnemyDefinition
    {
        public EnemyType type;
        public string displayName;
        public float maxHealth;
        /// <summary>Fraction of damage ignored (0..0.8).</summary>
        public float armor;
        public float moveSpeed;
        public float contactDamage;
        public float rangedDamage;
        public float attackInterval;
        public float preferredDistance; // ranged units stop at this z-distance from the player
        public int reward;
        public float scale = 1f;
        public float hitRadius = 0.8f;
        public bool flying;
        public string colorHex = "FFFFFF";

        public EnemyDefinition Clone() { return (EnemyDefinition)MemberwiseClone(); }

        public float DamageTaken(float rawDamage)
        {
            float a = armor < 0f ? 0f : (armor > 0.8f ? 0.8f : armor);
            return rawDamage * (1f - a);
        }
    }

    public static class EnemyCatalog
    {
        static List<EnemyDefinition> _defaults;

        public static List<EnemyDefinition> CreateDefaults()
        {
            return new List<EnemyDefinition>
            {
                new EnemyDefinition { type = EnemyType.BasicRobot, displayName = "FACTORY ROBOT", maxHealth = 40, armor = 0f, moveSpeed = 1.6f,
                    contactDamage = 20, reward = 6, scale = 1f, hitRadius = 0.8f, colorHex = "FFB74D" },
                new EnemyDefinition { type = EnemyType.ArmoredRobot, displayName = "ARMORED ROBOT", maxHealth = 100, armor = 0.3f, moveSpeed = 1.1f,
                    contactDamage = 30, reward = 12, scale = 1.25f, hitRadius = 1.0f, colorHex = "78909C" },
                new EnemyDefinition { type = EnemyType.Drone, displayName = "SECURITY DRONE", maxHealth = 28, armor = 0f, moveSpeed = 3.5f,
                    contactDamage = 10, rangedDamage = 7, attackInterval = 2.2f, preferredDistance = 13f, reward = 8, scale = 0.8f,
                    hitRadius = 0.7f, flying = true, colorHex = "4DD0E1" },
                new EnemyDefinition { type = EnemyType.HeavySecurity, displayName = "HEAVY SECURITY", maxHealth = 260, armor = 0.25f, moveSpeed = 0.7f,
                    contactDamage = 40, rangedDamage = 12, attackInterval = 3.0f, preferredDistance = 9f, reward = 22, scale = 1.6f,
                    hitRadius = 1.3f, colorHex = "E57373" },
                new EnemyDefinition { type = EnemyType.Boss, displayName = "FACTORY BOSS", maxHealth = 2400, armor = 0.15f, moveSpeed = 0.6f,
                    contactDamage = 50, rangedDamage = 10, attackInterval = 2.6f, preferredDistance = 15f, reward = 150, scale = 2.6f,
                    hitRadius = 2.4f, colorHex = "BA68C8" },
            };
        }

        public static List<EnemyDefinition> Defaults { get { return _defaults ?? (_defaults = CreateDefaults()); } }

        public static void Override(List<EnemyDefinition> list)
        {
            if (list != null && list.Count == Enum.GetValues(typeof(EnemyType)).Length) _defaults = list;
        }

        public static EnemyDefinition Get(EnemyType type)
        {
            foreach (var e in Defaults) if (e.type == type) return e;
            throw new ArgumentException("Unknown enemy " + type);
        }
    }
}
