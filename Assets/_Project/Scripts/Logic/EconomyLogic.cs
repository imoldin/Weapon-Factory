using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    public enum UpgradeType
    {
        StartingDamage = 0,
        StartingFireRate = 1,
        StartingMagazine = 2,
        StartingWeaponLevel = 3,
        CoinBonus = 4,
        CriticalChance = 5,
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType type;
        public string displayName;
        public string description;
        /// <summary>Price of level 1, 2, ... The array length is the maximum level.</summary>
        public int[] prices;
        /// <summary>Bonus per level (meaning depends on type: percent, power, ...).</summary>
        public float[] valuePerLevel;
        public string colorHex = "FFFFFF";

        public int MaxLevel { get { return prices == null ? 0 : prices.Length; } }

        /// <summary>Price to buy the next level, or -1 if maxed.</summary>
        public int PriceForNextLevel(int currentLevel)
        {
            if (currentLevel < 0) currentLevel = 0;
            if (currentLevel >= MaxLevel) return -1;
            return prices[currentLevel];
        }

        /// <summary>Total value at a given level (sum of per-level values).</summary>
        public float ValueAtLevel(int level)
        {
            float v = 0f;
            for (int i = 0; i < level && i < valuePerLevel.Length; i++) v += valuePerLevel[i];
            return v;
        }

        public string BenefitText(int level)
        {
            float v = ValueAtLevel(level);
            switch (type)
            {
                case UpgradeType.StartingWeaponLevel:
                    return "Start as " + WeaponEvolution.GetTier(WeaponEvolution.TierForPower((int)v)).displayName;
                case UpgradeType.CriticalChance:
                    return "+" + Math.Round(v * 100) + "% crit chance";
                default:
                    return "+" + Math.Round(v * 100) + "% " + ShortStat;
            }
        }

        string ShortStat
        {
            get
            {
                switch (type)
                {
                    case UpgradeType.StartingDamage: return "damage";
                    case UpgradeType.StartingFireRate: return "fire rate";
                    case UpgradeType.StartingMagazine: return "magazine";
                    case UpgradeType.CoinBonus: return "coins";
                }
                return "";
            }
        }
    }

    public static class UpgradeCatalog
    {
        static List<UpgradeDefinition> _defaults;

        public static List<UpgradeDefinition> CreateDefaults()
        {
            return new List<UpgradeDefinition>
            {
                new UpgradeDefinition { type = UpgradeType.StartingDamage, displayName = "DAMAGE", description = "Every shot hits harder.",
                    prices = new[] { 100, 250, 500, 900, 1500 }, valuePerLevel = new[] { 0.12f, 0.12f, 0.12f, 0.12f, 0.12f }, colorHex = "FF5252" },
                new UpgradeDefinition { type = UpgradeType.StartingFireRate, displayName = "FIRE RATE", description = "Shoot faster from the start.",
                    prices = new[] { 120, 300, 600, 1000, 1700 }, valuePerLevel = new[] { 0.08f, 0.08f, 0.08f, 0.08f, 0.08f }, colorHex = "FFB300" },
                new UpgradeDefinition { type = UpgradeType.StartingMagazine, displayName = "MAGAZINE", description = "Bigger magazines, fewer reloads.",
                    prices = new[] { 80, 200, 400, 700, 1200 }, valuePerLevel = new[] { 0.15f, 0.15f, 0.15f, 0.15f, 0.15f }, colorHex = "40C4FF" },
                new UpgradeDefinition { type = UpgradeType.StartingWeaponLevel, displayName = "WEAPON LEVEL", description = "Begin each run with an evolved weapon.",
                    prices = new[] { 300, 800, 1600, 3000 }, valuePerLevel = new[] { 3f, 4f, 5f, 6f }, colorHex = "69F0AE" },
                new UpgradeDefinition { type = UpgradeType.CoinBonus, displayName = "COIN BONUS", description = "Earn more coins from every level.",
                    prices = new[] { 150, 350, 700, 1200, 2000 }, valuePerLevel = new[] { 0.10f, 0.10f, 0.10f, 0.10f, 0.10f }, colorHex = "FFEA00" },
                new UpgradeDefinition { type = UpgradeType.CriticalChance, displayName = "CRITICAL", description = "Higher chance of double-damage hits.",
                    prices = new[] { 150, 350, 700, 1200, 2000 }, valuePerLevel = new[] { 0.03f, 0.03f, 0.03f, 0.03f, 0.03f }, colorHex = "B388FF" },
            };
        }

        public static List<UpgradeDefinition> Defaults { get { return _defaults ?? (_defaults = CreateDefaults()); } }

        public static void Override(List<UpgradeDefinition> list)
        {
            if (list != null && list.Count == Enum.GetValues(typeof(UpgradeType)).Length) _defaults = list;
        }

        public static UpgradeDefinition Get(UpgradeType type)
        {
            foreach (var u in Defaults) if (u.type == type) return u;
            throw new ArgumentException("Unknown upgrade " + type);
        }

        public static PermanentBonuses BuildBonuses(Func<UpgradeType, int> levelOf)
        {
            var b = new PermanentBonuses();
            b.damageMultiplier = 1f + Get(UpgradeType.StartingDamage).ValueAtLevel(levelOf(UpgradeType.StartingDamage));
            b.fireRateMultiplier = 1f + Get(UpgradeType.StartingFireRate).ValueAtLevel(levelOf(UpgradeType.StartingFireRate));
            b.magazineMultiplier = 1f + Get(UpgradeType.StartingMagazine).ValueAtLevel(levelOf(UpgradeType.StartingMagazine));
            b.startingPower = (int)Math.Round(Get(UpgradeType.StartingWeaponLevel).ValueAtLevel(levelOf(UpgradeType.StartingWeaponLevel)));
            b.coinMultiplier = 1f + Get(UpgradeType.CoinBonus).ValueAtLevel(levelOf(UpgradeType.CoinBonus));
            b.critBonus = Get(UpgradeType.CriticalChance).ValueAtLevel(levelOf(UpgradeType.CriticalChance));
            return b;
        }
    }

    /// <summary>Cosmetic weapon finishes, bought with coins on the WEAPONS screen.</summary>
    [Serializable]
    public class WeaponSkinDefinition
    {
        public string id;
        public string displayName;
        public int price;
        public string bodyHex;
        public string accentHex;
        public string glowHex;
    }

    public static class SkinCatalog
    {
        public static readonly List<WeaponSkinDefinition> All = new List<WeaponSkinDefinition>
        {
            new WeaponSkinDefinition { id = "factory", displayName = "FACTORY STEEL", price = 0, bodyHex = "5B6573", accentHex = "2B3038", glowHex = "38E1FF" },
            new WeaponSkinDefinition { id = "toxic", displayName = "TOXIC", price = 400, bodyHex = "2E7D32", accentHex = "1B1F1A", glowHex = "B2FF59" },
            new WeaponSkinDefinition { id = "royal", displayName = "ROYAL GOLD", price = 1200, bodyHex = "C9A227", accentHex = "3B2A12", glowHex = "FFF176" },
            new WeaponSkinDefinition { id = "neon", displayName = "NEON PULSE", price = 2500, bodyHex = "311B92", accentHex = "120A2A", glowHex = "FF2BD6" },
        };

        public static WeaponSkinDefinition Get(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return All[0];
        }
    }

    public struct RewardBreakdown
    {
        public int completion;
        public int enemies;
        public int boss;
        public int weaponStrength;
        public int collected;
        public int bonusObjective;
        public float multiplier;
        public int total;
    }

    public static class RewardCalculator
    {
        /// <param name="integrityFraction">Remaining weapon integrity 0..1 (bonus objective: flawless &gt;= 0.999).</param>
        public static RewardBreakdown Calculate(int levelNumber, int baseReward, int enemyRewardSum, bool bossDefeated,
            int bossReward, int weaponTier, float integrityFraction, int coinsCollected, float coinMultiplier)
        {
            var r = new RewardBreakdown();
            r.completion = baseReward + 10 * Math.Max(0, levelNumber - 1);
            r.enemies = Math.Max(0, enemyRewardSum);
            r.boss = bossDefeated ? Math.Max(0, bossReward) : 0;
            r.weaponStrength = Math.Max(0, weaponTier - 1) * 8;
            r.collected = Math.Max(0, coinsCollected);
            r.bonusObjective = integrityFraction >= 0.999f ? (int)Math.Round(r.completion * 0.5f) : 0;
            r.multiplier = Math.Max(1f, coinMultiplier);
            int subtotal = r.completion + r.enemies + r.boss + r.weaponStrength + r.collected + r.bonusObjective;
            r.total = (int)Math.Round(subtotal * r.multiplier);
            return r;
        }
    }
}
