// Unit tests + balance simulation for the pure gameplay rules.
// Run from this folder:  dotnet run
using System;
using System.Collections.Generic;
using WeaponFactory.Logic;

static class Program
{
    static int _passed, _failed;

    static void Check(bool condition, string name)
    {
        if (condition) { _passed++; }
        else { _failed++; Console.WriteLine("  FAIL: " + name); }
    }

    static int Main()
    {
        Console.WriteLine("== Weapon Factory logic tests ==");
        TestEvolution();
        TestStats();
        TestGates();
        TestEconomy();
        TestSave();
        TestLevels();
        BalanceSimulation();
        Console.WriteLine($"Passed: {_passed}  Failed: {_failed}");
        return _failed == 0 ? 0 : 1;
    }

    static void TestEvolution()
    {
        Check(WeaponEvolution.TierForPower(0) == 1, "power 0 is tier 1");
        Check(WeaponEvolution.TierForPower(3) == 2, "power 3 is tier 2");
        Check(WeaponEvolution.TierForPower(31) == 7, "power 31 is tier 7");
        Check(WeaponEvolution.TierForPower(999) == 8, "max tier 8");
        Check(WeaponEvolution.Tiers.Count == 8, "8 tiers");
        for (int i = 1; i < 8; i++)
            Check(WeaponEvolution.Tiers[i].powerRequired > WeaponEvolution.Tiers[i - 1].powerRequired, "tier thresholds increase " + i);

        var build = new WeaponBuild();
        build.AddComponent(ComponentType.Barrel);
        Check(build.GetLevel(ComponentType.Barrel) == 1, "barrel level 1");
        Check(build.Power == 2, "barrel gives 2 power");
        build.AddComponent(ComponentType.DoubleBarrel);
        Check(build.Power == 6 && build.Tier == 3, "barrel + double barrel = power 6, tier 3");
        // overflow converts to power
        build.AddComponent(ComponentType.DoubleBarrel);
        int before = build.Power;
        build.AddComponent(ComponentType.DoubleBarrel); // already at max level 2
        Check(build.GetLevel(ComponentType.DoubleBarrel) == 2, "double barrel capped at 2");
        Check(build.Power > before, "maxed pickup converts to power");

        var rng = new Random(1);
        var b2 = new WeaponBuild();
        Check(!b2.RemoveRandomComponent(rng, out _), "cannot remove from empty build");
        b2.AddComponent(ComponentType.Scope);
        Check(b2.RemoveRandomComponent(rng, out var removed) && removed == ComponentType.Scope, "removes owned component");
        Check(b2.ComponentCount == 0, "build empty after removal");
    }

    static void TestStats()
    {
        var empty = StatCalculator.Calculate(new WeaponBuild(), new RunModifiers(), new PermanentBonuses());
        Check(Math.Abs(empty.damage - 10f) < 0.01f, "base pistol damage 10");
        Check(empty.magazine == 8, "base magazine 8");
        Check(empty.projectilesPerShot == 1, "1 projectile");

        var b = new WeaponBuild();
        b.AddComponent(ComponentType.Barrel);
        var s = StatCalculator.Calculate(b, new RunModifiers(), new PermanentBonuses());
        Check(s.damage > empty.damage, "barrel increases damage");
        Check(s.range > empty.range, "barrel increases range");

        b.AddComponent(ComponentType.Magazine);
        s = StatCalculator.Calculate(b, new RunModifiers(), new PermanentBonuses());
        Check(s.magazine > empty.magazine, "magazine increases capacity");

        var run = new RunModifiers();
        new GateEffect(GateEffectType.DamagePercent, 50).Apply(run, b);
        var s2 = StatCalculator.Calculate(b, run, new PermanentBonuses());
        Check(Math.Abs(s2.damage / s.damage - 1.5f) < 0.01f, "+50% gate multiplies damage by 1.5");

        new GateEffect(GateEffectType.DamagePercent, -30).Apply(run, b);
        var s3 = StatCalculator.Calculate(b, run, new PermanentBonuses());
        Check(s3.damage < s2.damage, "-30% gate reduces damage");

        // permanent damage multiplier
        var perm = new PermanentBonuses { damageMultiplier = 1.24f };
        var s4 = StatCalculator.Calculate(new WeaponBuild(), new RunModifiers(), perm);
        Check(Math.Abs(s4.damage - 12.4f) < 0.01f, "permanent multiplier applied");

        // barrel gates never drop below 1 projectile
        var run2 = new RunModifiers();
        for (int i = 0; i < 5; i++) new GateEffect(GateEffectType.BarrelAdd, -1).Apply(run2, null);
        var s5 = StatCalculator.Calculate(new WeaponBuild(), run2, new PermanentBonuses());
        Check(s5.projectilesPerShot == 1, "projectiles never below 1");

        // floors on negative gates
        var run3 = new RunModifiers();
        for (int i = 0; i < 20; i++) new GateEffect(GateEffectType.DamagePercent, -30).Apply(run3, null);
        Check(run3.damageMultiplier >= RunModifiers.MinDamageMultiplier - 1e-4f, "damage multiplier floored");

        // Each tier is stronger (sustained DPS) than the previous one.
        float prev = 0f;
        foreach (var t in WeaponEvolution.Tiers)
        {
            var bb = new WeaponBuild(t.powerRequired);
            var st = StatCalculator.Calculate(bb, new RunModifiers(), new PermanentBonuses());
            Check(st.SustainedDps > prev, "tier " + t.tier + " stronger than previous");
            prev = st.SustainedDps;
        }
    }

    static void TestGates()
    {
        var rng = new Random(42);
        for (int i = 0; i < 2000; i++)
        {
            var p = GateGenerator.Generate(rng, (i % 11) / 10f);
            Check(GateGenerator.IsFair(p), "generated pair fair #" + i);
        }
        var bad = new GatePairDefinition { left = new GateEffect(GateEffectType.DamagePercent, -30), right = new GateEffect(GateEffectType.FireRatePercent, -20) };
        Check(GateGenerator.IsFair(GateGenerator.Sanitize(bad, rng, 0.5f)), "sanitize fixes double negative");
        Check(new GateEffect(GateEffectType.DamageMultiply, 2).ValueLabel == "x2", "x2 label");
        Check(new GateEffect(GateEffectType.DamagePercent, 50).FullLabel == "+50% DAMAGE", "+50% DAMAGE label");
        Check(new GateEffect(GateEffectType.BarrelAdd, -1).FullLabel == "-1 BARREL", "-1 BARREL label");
        Check(!new GateEffect(GateEffectType.FireRatePercent, -20).IsPositive, "negative detection");
    }

    static void TestEconomy()
    {
        var dmg = UpgradeCatalog.Get(UpgradeType.StartingDamage);
        Check(dmg.PriceForNextLevel(0) == 100 && dmg.PriceForNextLevel(4) == 1500, "damage prices match spec");
        Check(dmg.PriceForNextLevel(5) == -1, "maxed returns -1");
        foreach (var u in UpgradeCatalog.Defaults)
            for (int i = 1; i < u.MaxLevel; i++)
                Check(u.prices[i] > u.prices[i - 1], u.displayName + " prices increase");

        var save = new SaveData { coins = 99 };
        Check(Purchases.TryBuyUpgrade(save, UpgradeType.StartingDamage) == Purchases.Result.NotEnoughCoins, "cannot buy without coins");
        Check(save.coins == 99 && save.GetUpgradeLevel(UpgradeType.StartingDamage) == 0, "failed purchase changes nothing");
        save.coins = 100;
        Check(Purchases.TryBuyUpgrade(save, UpgradeType.StartingDamage) == Purchases.Result.Success, "buy with enough coins");
        Check(save.coins == 0 && save.GetUpgradeLevel(UpgradeType.StartingDamage) == 1, "purchase applied");

        var bonuses = UpgradeCatalog.BuildBonuses(save.GetUpgradeLevel);
        Check(Math.Abs(bonuses.damageMultiplier - 1.12f) < 1e-4f, "bonus from level 1 damage");

        save.coins = 5000;
        Check(Purchases.TryBuySkin(save, "toxic") == Purchases.Result.Success, "buy skin");
        Check(Purchases.TryBuySkin(save, "toxic") == Purchases.Result.AlreadyOwned, "skin already owned");

        var r = RewardCalculator.Calculate(1, 60, 6, false, 0, 2, 1f, 20, 1.1f);
        Check(r.total > 0 && r.bonusObjective > 0, "reward with flawless bonus");
        var r2 = RewardCalculator.Calculate(5, 120, 20, true, 150, 5, 0.5f, 30, 1f);
        Check(r2.boss == 150 && r2.bonusObjective == 0, "boss reward counted, no flawless");
    }

    static void TestSave()
    {
        var s = new SaveData { coins = -50, currentLevel = 0, musicVolume = 5f, equippedSkin = "missing", upgradeLevels = new[] { 99, -3 } };
        Check(s.Sanitize(), "sanitize reports fixes");
        Check(s.coins == 0 && s.currentLevel == 1 && s.musicVolume == 1f, "sanitize clamps values");
        Check(s.upgradeLevels.Length == 6 && s.upgradeLevels[0] == 5 && s.upgradeLevels[1] == 0, "upgrade array repaired");
        Check(s.equippedSkin == "factory", "invalid skin reset");
        s.MarkLevelCompleted(1);
        Check(s.currentLevel == 2 && s.IsLevelCompleted(1), "level completion advances");
        s.MarkLevelCompleted(1);
        Check(s.completedLevels.Count == 1, "no duplicate completion");
    }

    static void TestLevels()
    {
        var levels = LevelLibrary.CreateDefaults();
        Check(levels.Count == 10, "10 handmade levels");
        float prevLen = 0f;
        for (int i = 0; i < levels.Count; i++)
        {
            var l = levels[i];
            Check(l.levelNumber == i + 1, "level numbering " + (i + 1));
            Check(l.waves.Count > 0 && l.TotalEnemies > 0, "level has enemies " + l.levelNumber);
            bool hasBoss = false;
            foreach (var w in l.waves) foreach (var sp in w.spawns) if (sp.type == EnemyType.Boss) hasBoss = true;
            Check(hasBoss == l.isBossLevel, "boss flag matches waves " + l.levelNumber);
            Check(l.isBossLevel == (l.levelNumber % 5 == 0), "every 5th level is a boss " + l.levelNumber);
            Check(l.TrackLength >= prevLen - 20f, "track length roughly increasing " + l.levelNumber);
            prevLen = Math.Max(prevLen, l.TrackLength);
            foreach (var seg in l.segments)
                if (seg.type == SegmentType.GatePair && !seg.gate.randomize)
                    Check(GateGenerator.IsFair(seg.gate), "authored gate fair L" + l.levelNumber);
        }
        Check(levels[0].tutorial, "level 1 is tutorial");
        for (int n = 11; n <= 30; n++)
        {
            var p = LevelLibrary.GenerateProcedural(n);
            Check(p.TotalEnemies > 0 && p.segments.Count > 10, "procedural level " + n + " valid");
            Check(p.isBossLevel == (n % 5 == 0), "procedural boss cadence " + n);
        }
    }

    /// <summary>
    /// Simulates an "average" player (picks up ~70% of positive items, picks the better gate,
    /// never buys permanent upgrades) and checks the arena can be cleared well inside the time limit.
    /// </summary>
    static void BalanceSimulation()
    {
        Console.WriteLine();
        Console.WriteLine(" Lvl  Name             Len   Tier  DPS     EnemyHP   KillTime  Limit");
        foreach (var level in LevelLibrary.CreateDefaults())
        {
            var rng = new Random(level.seed);
            var build = new WeaponBuild();
            var run = new RunModifiers();
            var perm = new PermanentBonuses();
            int itemIndex = 0;
            foreach (var seg in level.segments)
            {
                bool take = (itemIndex++ % 10) < 7; // 70% pickup rate
                switch (seg.type)
                {
                    case SegmentType.ComponentItem:
                        if (take) build.AddComponent(seg.component); break;
                    case SegmentType.ComponentChoice:
                        if (take) build.AddComponent(seg.component); break;
                    case SegmentType.Mixed:
                        if (take && seg.pickup == PickupKind.Component) build.AddComponent(seg.component);
                        else if (take && seg.pickup == PickupKind.UpgradeModule) build.AddPower(2);
                        else if (take && seg.pickup == PickupKind.DamageBooster) run.damageMultiplier *= 1.2f;
                        break;
                    case SegmentType.Pickup:
                        if (!take) break;
                        if (seg.pickup == PickupKind.UpgradeBox) { build.AddPower(4); build.AddComponent((ComponentType)rng.Next(10)); }
                        else if (seg.pickup == PickupKind.UpgradeModule) build.AddPower(2);
                        else if (seg.pickup == PickupKind.DamageBooster) run.damageMultiplier *= 1.2f;
                        else if (seg.pickup == PickupKind.FireRateBooster) run.fireRateMultiplier *= 1.15f;
                        break;
                    case SegmentType.GatePair:
                        var pair = GateGenerator.Sanitize(seg.gate, rng, level.gateDifficulty);
                        // choose better gate by resulting DPS
                        var runA = Copy(run); pair.left.Apply(runA, null);
                        var runB = Copy(run); pair.right.Apply(runB, null);
                        float dA = StatCalculator.Calculate(build, runA, perm).SustainedDps;
                        float dB = StatCalculator.Calculate(build, runB, perm).SustainedDps;
                        var chosen = dA >= dB ? pair.left : pair.right;
                        chosen.Apply(run, build);
                        break;
                }
            }
            var stats = StatCalculator.Calculate(build, run, perm);
            float hp = 0f;
            foreach (var w in level.waves)
                foreach (var sp in w.spawns)
                {
                    var e = EnemyCatalog.Get(sp.type);
                    hp += sp.count * e.maxHealth * level.enemyHealthMultiplier / (1f - e.armor);
                }
            float effectiveDps = stats.SustainedDps * 0.8f; // 80% hit rate
            float killTime = hp / effectiveDps;
            Console.WriteLine($" {level.levelNumber,3}  p{build.Power,2}  {level.displayName,-15} {level.TrackLength,5:0}  {stats.tier,4}  {effectiveDps,6:0}  {hp,8:0}  {killTime,7:0.0}s  {level.arenaTimeLimit,4:0}s");
            Check(killTime < level.arenaTimeLimit * 0.65f, "level " + level.levelNumber + " beatable by average player without upgrades");
            Check(killTime > 0.3f, "level " + level.levelNumber + " not trivial");
        }
        Console.WriteLine();
    }

    static RunModifiers Copy(RunModifiers r)
    {
        return new RunModifiers
        {
            damageMultiplier = r.damageMultiplier, fireRateMultiplier = r.fireRateMultiplier, magazineMultiplier = r.magazineMultiplier,
            magazineBonus = r.magazineBonus, extraProjectiles = r.extraProjectiles, critBonus = r.critBonus,
        };
    }
}
