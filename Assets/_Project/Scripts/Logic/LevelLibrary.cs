using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    /// <summary>
    /// The 10 hand-designed default levels plus a procedural generator for levels 11+.
    /// Levels are pure data; LevelGenerator (Unity side) turns them into a 3D track.
    /// ScriptableObject LevelData assets, if present in Resources/Levels, override these.
    /// </summary>
    public static class LevelLibrary
    {
        public const int HandmadeLevelCount = 10;

        // ---------- segment helpers ----------
        static SegmentDefinition Coins(float x, int count) { return new SegmentDefinition { type = SegmentType.Coins, x = x, count = count }; }
        static SegmentDefinition Item(ComponentType c, float x = 0f) { return new SegmentDefinition { type = SegmentType.ComponentItem, component = c, x = x }; }
        static SegmentDefinition Choice(ComponentType left, ComponentType right) { return new SegmentDefinition { type = SegmentType.ComponentChoice, component = left, component2 = right }; }
        static SegmentDefinition Gate(GateEffectType lt, float lv, GateEffectType rt, float rv)
        {
            return new SegmentDefinition { type = SegmentType.GatePair, gate = new GatePairDefinition { left = new GateEffect(lt, lv), right = new GateEffect(rt, rv) } };
        }
        static SegmentDefinition RandomGate() { return new SegmentDefinition { type = SegmentType.GatePair, gate = new GatePairDefinition { randomize = true } }; }
        static SegmentDefinition Saw(float x) { return new SegmentDefinition { type = SegmentType.Saw, x = x }; }
        static SegmentDefinition MovingSaw(float speed = 1f) { return new SegmentDefinition { type = SegmentType.MovingSaw, value = speed }; }
        static SegmentDefinition Crusher(float x) { return new SegmentDefinition { type = SegmentType.Crusher, x = x }; }
        static SegmentDefinition Laser(float gapX) { return new SegmentDefinition { type = SegmentType.LaserWall, x = gapX }; }
        static SegmentDefinition Barrier(float x, int hp) { return new SegmentDefinition { type = SegmentType.Barrier, x = x, value = hp }; }
        static SegmentDefinition Barrels(float x) { return new SegmentDefinition { type = SegmentType.ExplosiveBarrels, x = x }; }
        static SegmentDefinition Pickup(PickupKind k, float x = 0f, float value = 0f) { return new SegmentDefinition { type = SegmentType.Pickup, pickup = k, x = x, value = value }; }
        static SegmentDefinition Mixed(PickupKind good, float x, PickupKind bad) { return new SegmentDefinition { type = SegmentType.Mixed, pickup = good, x = x, pickup2 = bad }; }
        static SegmentDefinition MixedComponent(ComponentType good, float x, PickupKind bad)
        {
            return new SegmentDefinition { type = SegmentType.Mixed, pickup = PickupKind.Component, component = good, x = x, pickup2 = bad };
        }
        static SegmentDefinition Targets(int count, int hp) { return new SegmentDefinition { type = SegmentType.Targets, count = count, value = hp }; }
        static SegmentDefinition Hammer(float speed = 1f) { return new SegmentDefinition { type = SegmentType.Hammer, value = speed }; }

        static WaveDefinition Wave(float delay, params EnemySpawn[] spawns)
        {
            return new WaveDefinition { delay = delay, spawns = new List<EnemySpawn>(spawns) };
        }
        static EnemySpawn E(EnemyType t, int c) { return new EnemySpawn(t, c); }

        const GateEffectType DMG = GateEffectType.DamagePercent;
        const GateEffectType DMGX = GateEffectType.DamageMultiply;
        const GateEffectType FR = GateEffectType.FireRatePercent;
        const GateEffectType MAG = GateEffectType.MagazineAdd;
        const GateEffectType MAGP = GateEffectType.MagazinePercent;
        const GateEffectType BAR = GateEffectType.BarrelAdd;
        const GateEffectType CRIT = GateEffectType.CritPercent;
        const GateEffectType PWR = GateEffectType.PowerAdd;

        public static List<LevelDefinition> CreateDefaults()
        {
            var levels = new List<LevelDefinition>();

            // LEVEL 1 — tutorial: barrels, one positive gate, one robot.
            levels.Add(new LevelDefinition
            {
                levelNumber = 1, displayName = "FIRST SHIFT", theme = EnvironmentTheme.IndustrialFactory, runSpeed = 8.5f,
                enemyHealthMultiplier = 1f, gateDifficulty = 0f, baseReward = 60, tutorial = true, seed = 101,
                introText = "DRAG TO MOVE - COLLECT PARTS",
                segments = new List<SegmentDefinition>
                {
                    Coins(0f, 5),
                    Item(ComponentType.Barrel, 0f),
                    Coins(-2f, 4),
                    Item(ComponentType.Barrel, 2.2f),
                    Gate(DMG, 50, FR, 30),
                    Targets(2, 12),
                    Item(ComponentType.Barrel, -2.2f),
                    Coins(0f, 6),
                },
                waves = new List<WaveDefinition> { Wave(0.5f, E(EnemyType.BasicRobot, 1)) },
            });

            // LEVEL 2 — magazines and negative gates.
            levels.Add(new LevelDefinition
            {
                levelNumber = 2, displayName = "AMMO SUPPLY", theme = EnvironmentTheme.IndustrialFactory, runSpeed = 9f,
                enemyHealthMultiplier = 1.15f, gateDifficulty = 0.2f, baseReward = 70, seed = 202,
                introText = "AVOID RED GATES",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.Magazine, 0f),
                    Coins(2f, 5),
                    Gate(MAG, 20, FR, -20),
                    Item(ComponentType.Barrel, -2f),
                    Barrier(2f, 25),
                    MixedComponent(ComponentType.Magazine, -2.2f, PickupKind.RustedPart),
                    Gate(BAR, 1, DMG, -30),
                    Coins(0f, 6),
                    Item(ComponentType.Barrel, 0f),
                },
                waves = new List<WaveDefinition> { Wave(0.5f, E(EnemyType.BasicRobot, 2)) },
            });

            // LEVEL 3 — scopes and saws.
            levels.Add(new LevelDefinition
            {
                levelNumber = 3, displayName = "EAGLE EYE", theme = EnvironmentTheme.IndustrialFactory, runSpeed = 9.5f,
                enemyHealthMultiplier = 1.35f, gateDifficulty = 0.3f, baseReward = 80, seed = 303,
                introText = "WATCH OUT FOR SAWS",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.Scope, 0f),
                    Saw(-2f),
                    Choice(ComponentType.Barrel, ComponentType.Grip),
                    RandomGate(),
                    MovingSaw(0.8f),
                    Coins(0f, 6),
                    Item(ComponentType.ExtendedBarrel, 2f),
                    Barrier(-2f, 40),
                    Targets(3, 20),
                    Gate(DMG, 30, CRIT, 15),
                    Mixed(PickupKind.UpgradeModule, 2f, PickupKind.WeightPenalty),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.BasicRobot, 3)),
                    Wave(1.0f, E(EnemyType.Drone, 1)),
                },
            });

            // LEVEL 4 — multiple upgrade choices, stronger enemies, laboratory.
            levels.Add(new LevelDefinition
            {
                levelNumber = 4, displayName = "LAB ACCESS", theme = EnvironmentTheme.TechLaboratory, runSpeed = 10f,
                enemyHealthMultiplier = 1.55f, gateDifficulty = 0.4f, baseReward = 95, seed = 404,
                introText = "CHOOSE YOUR UPGRADES",
                segments = new List<SegmentDefinition>
                {
                    Choice(ComponentType.Stock, ComponentType.Muzzle),
                    Gate(DMGX, 2, BAR, 1),
                    Laser(2.2f),
                    Item(ComponentType.ExtendedMagazine, -2f),
                    Crusher(0f),
                    Choice(ComponentType.LaserSight, ComponentType.Grip),
                    RandomGate(),
                    Barrier(2f, 60),
                    Pickup(PickupKind.UpgradeBox, -2f),
                    Coins(0f, 8),
                    Mixed(PickupKind.DamageBooster, -2f, PickupKind.BrokenPart),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.BasicRobot, 2), E(EnemyType.ArmoredRobot, 1)),
                    Wave(1.0f, E(EnemyType.Drone, 2)),
                },
            });

            // LEVEL 5 — first boss.
            levels.Add(new LevelDefinition
            {
                levelNumber = 5, displayName = "THE GUARDIAN", theme = EnvironmentTheme.TechLaboratory, runSpeed = 10f,
                enemyHealthMultiplier = 1.0f, gateDifficulty = 0.35f, baseReward = 120, isBossLevel = true,
                bossName = "GUARDIAN MK-I", arenaTimeLimit = 60f, seed = 505,
                introText = "BOSS AHEAD! POWER UP!",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.DoubleBarrel, 0f),
                    Coins(-2f, 5),
                    Item(ComponentType.Scope, 2f),
                    RandomGate(),
                    MovingSaw(1f),
                    Choice(ComponentType.Magazine, ComponentType.Muzzle),
                    Gate(DMG, 50, FR, 30),
                    Crusher(-2f),
                    Pickup(PickupKind.UpgradeBox, 2f),
                    Item(ComponentType.Grip, -2f),
                    Pickup(PickupKind.RepairKit, 0f),
                    Gate(PWR, 3, MAG, 20),
                    Pickup(PickupKind.FireRateBooster, 0f),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.BasicRobot, 2)),
                    Wave(1.2f, E(EnemyType.Boss, 1)),
                },
            });

            // LEVEL 6 — advanced parts, heavy robots.
            levels.Add(new LevelDefinition
            {
                levelNumber = 6, displayName = "CLEAN ROOM", theme = EnvironmentTheme.TechLaboratory, runSpeed = 10.5f,
                enemyHealthMultiplier = 1.9f, gateDifficulty = 0.5f, baseReward = 130, seed = 606,
                introText = "HEAVY SECURITY DETECTED",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.LaserSight, 0f),
                    Item(ComponentType.ExtendedMagazine, 2.2f),
                    RandomGate(),
                    Laser(-2.2f),
                    Hammer(1f),
                    Choice(ComponentType.DoubleBarrel, ComponentType.Scope),
                    Barrels(2f),
                    RandomGate(),
                    Barrier(-2f, 90),
                    Item(ComponentType.Grip, 2f),
                    MixedComponent(ComponentType.Muzzle, -2f, PickupKind.DamageReducer),
                    Targets(4, 40),
                    Gate(DMGX, 2, CRIT, 25),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.BasicRobot, 3), E(EnemyType.ArmoredRobot, 1)),
                    Wave(1.0f, E(EnemyType.ArmoredRobot, 1), E(EnemyType.HeavySecurity, 1)),
                },
            });

            // LEVEL 7 — robot factory, drones swarm.
            levels.Add(new LevelDefinition
            {
                levelNumber = 7, displayName = "ROBOT WORKS", theme = EnvironmentTheme.RobotFactory, runSpeed = 11f,
                enemyHealthMultiplier = 2.2f, gateDifficulty = 0.55f, baseReward = 145, seed = 707,
                introText = "DRONE SWARM INCOMING",
                segments = new List<SegmentDefinition>
                {
                    Choice(ComponentType.ExtendedBarrel, ComponentType.Stock),
                    MovingSaw(1.2f),
                    Item(ComponentType.Barrel, -2f),
                    RandomGate(),
                    Crusher(2f),
                    Crusher(-2f),
                    Pickup(PickupKind.UpgradeBox, 0f),
                    Laser(0f),
                    Choice(ComponentType.Scope, ComponentType.ExtendedMagazine),
                    Mixed(PickupKind.UpgradeModule, 2f, PickupKind.ExplosiveHazard),
                    RandomGate(),
                    Barrier(2f, 120),
                    Coins(-2f, 8),
                    Gate(BAR, 1, FR, 30),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.Drone, 3), E(EnemyType.ArmoredRobot, 1)),
                    Wave(1.0f, E(EnemyType.ArmoredRobot, 1), E(EnemyType.HeavySecurity, 1), E(EnemyType.Drone, 1)),
                },
            });

            // LEVEL 8 — assembly line, dense hazards.
            levels.Add(new LevelDefinition
            {
                levelNumber = 8, displayName = "ASSEMBLY LINE", theme = EnvironmentTheme.RobotFactory, runSpeed = 11.5f,
                enemyHealthMultiplier = 2.5f, gateDifficulty = 0.65f, baseReward = 160, seed = 808,
                introText = "STAY SHARP",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.Muzzle, 0f),
                    Hammer(1.2f),
                    Choice(ComponentType.DoubleBarrel, ComponentType.LaserSight),
                    RandomGate(),
                    Barrels(-2f),
                    Saw(2f),
                    Item(ComponentType.ExtendedBarrel, -2f),
                    Laser(2.2f),
                    RandomGate(),
                    Pickup(PickupKind.RepairKit, 0f),
                    Targets(4, 60),
                    MixedComponent(ComponentType.Grip, 2f, PickupKind.BrokenPart),
                    MovingSaw(1.3f),
                    Gate(DMG, 50, PWR, 3),
                    Barrier(-2f, 160),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.BasicRobot, 4), E(EnemyType.Drone, 1)),
                    Wave(1.0f, E(EnemyType.ArmoredRobot, 2), E(EnemyType.HeavySecurity, 1)),
                    Wave(1.0f, E(EnemyType.HeavySecurity, 1), E(EnemyType.Drone, 2)),
                },
            });

            // LEVEL 9 — overdrive.
            levels.Add(new LevelDefinition
            {
                levelNumber = 9, displayName = "OVERDRIVE", theme = EnvironmentTheme.RobotFactory, runSpeed = 12f,
                enemyHealthMultiplier = 2.4f, gateDifficulty = 0.75f, baseReward = 175, seed = 909,
                introText = "MAXIMUM SPEED",
                segments = new List<SegmentDefinition>
                {
                    Choice(ComponentType.ExtendedMagazine, ComponentType.Muzzle),
                    Crusher(0f),
                    RandomGate(),
                    MovingSaw(1.5f),
                    Pickup(PickupKind.UpgradeBox, 2f),
                    Laser(-2.2f),
                    Choice(ComponentType.Scope, ComponentType.Grip),
                    Hammer(1.4f),
                    RandomGate(),
                    Barrels(2f),
                    Item(ComponentType.Barrel, -2f),
                    Barrier(2f, 200),
                    Mixed(PickupKind.DamageBooster, -2f, PickupKind.WeightPenalty),
                    RandomGate(),
                    Coins(0f, 10),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)),
                    Wave(1.0f, E(EnemyType.HeavySecurity, 2), E(EnemyType.BasicRobot, 3)),
                    Wave(1.0f, E(EnemyType.HeavySecurity, 1), E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)),
                },
            });

            // LEVEL 10 — second boss.
            levels.Add(new LevelDefinition
            {
                levelNumber = 10, displayName = "CORE MELTDOWN", theme = EnvironmentTheme.RobotFactory, runSpeed = 12f,
                enemyHealthMultiplier = 2.6f, gateDifficulty = 0.6f, baseReward = 220, isBossLevel = true,
                bossName = "OVERSEER MK-II", arenaTimeLimit = 75f, seed = 1010,
                introText = "DESTROY THE OVERSEER",
                segments = new List<SegmentDefinition>
                {
                    Item(ComponentType.DoubleBarrel, 0f),
                    Choice(ComponentType.ExtendedBarrel, ComponentType.LaserSight),
                    RandomGate(),
                    Crusher(2f),
                    Pickup(PickupKind.UpgradeBox, -2f),
                    MovingSaw(1.4f),
                    Gate(DMGX, 2, PWR, 5),
                    Choice(ComponentType.Muzzle, ComponentType.ExtendedMagazine),
                    Laser(0f),
                    Item(ComponentType.Scope, 2f),
                    Hammer(1.3f),
                    RandomGate(),
                    Pickup(PickupKind.RepairKit, 0f),
                    Targets(4, 80),
                    Gate(FR, 30, BAR, 1),
                },
                waves = new List<WaveDefinition>
                {
                    Wave(0.5f, E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)),
                    Wave(1.5f, E(EnemyType.Boss, 1)),
                },
            });

            return levels;
        }

        /// <summary>Endless levels after the hand-made set: deterministic per level number.</summary>
        public static LevelDefinition GenerateProcedural(int levelNumber)
        {
            var rng = new Random(levelNumber * 7919 + 17);
            int cycle = levelNumber - HandmadeLevelCount; // 1, 2, 3...
            bool boss = levelNumber % 5 == 0;
            var def = new LevelDefinition
            {
                levelNumber = levelNumber,
                displayName = "SECTOR " + levelNumber,
                theme = (EnvironmentTheme)(levelNumber % 3),
                runSpeed = Math.Min(13f, 11.5f + 0.1f * cycle),
                enemyHealthMultiplier = (boss ? 2.1f : 2.8f) * (1f + 0.12f * cycle),
                gateDifficulty = Math.Min(0.9f, 0.6f + 0.03f * cycle),
                isBossLevel = boss,
                bossName = boss ? "OVERSEER MK-" + (levelNumber / 5) : "",
                baseReward = 220 + 15 * cycle,
                arenaTimeLimit = boss ? 80f : 50f,
                seed = levelNumber * 31,
                introText = boss ? "BOSS AHEAD!" : "SECTOR " + levelNumber,
            };

            var components = (ComponentType[])Enum.GetValues(typeof(ComponentType));
            int segCount = Math.Min(20, 13 + cycle / 2);
            float[] lanes = { -2.2f, 0f, 2.2f };
            for (int i = 0; i < segCount; i++)
            {
                int roll = rng.Next(100);
                float x = lanes[rng.Next(lanes.Length)];
                if (i % 4 == 2) def.segments.Add(RandomGate());
                else if (roll < 28) def.segments.Add(Item(components[rng.Next(components.Length)], x));
                else if (roll < 40) def.segments.Add(Choice(components[rng.Next(components.Length)], components[rng.Next(components.Length)]));
                else if (roll < 48) def.segments.Add(MovingSaw(1.2f + 0.05f * cycle));
                else if (roll < 56) def.segments.Add(Crusher(x));
                else if (roll < 63) def.segments.Add(Laser(x));
                else if (roll < 69) def.segments.Add(Hammer(1.3f));
                else if (roll < 75) def.segments.Add(Barrier(x, 150 + 20 * cycle));
                else if (roll < 81) def.segments.Add(Pickup(PickupKind.UpgradeBox, x));
                else if (roll < 87) def.segments.Add(Mixed(PickupKind.UpgradeModule, x, PickupKind.ExplosiveHazard));
                else if (roll < 92) def.segments.Add(Barrels(x));
                else def.segments.Add(Coins(x, 8));
            }

            if (boss)
            {
                def.waves.Add(Wave(0.5f, E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)));
                def.waves.Add(Wave(1.5f, E(EnemyType.Boss, 1)));
            }
            else
            {
                def.waves.Add(Wave(0.5f, E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)));
                def.waves.Add(Wave(1.0f, E(EnemyType.HeavySecurity, 2), E(EnemyType.BasicRobot, 3)));
                def.waves.Add(Wave(1.0f, E(EnemyType.HeavySecurity, 1 + cycle / 4), E(EnemyType.ArmoredRobot, 2), E(EnemyType.Drone, 2)));
            }
            return def;
        }
    }
}
