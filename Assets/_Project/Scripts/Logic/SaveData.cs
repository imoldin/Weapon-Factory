using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    /// <summary>
    /// Everything persisted between sessions. Serialised to JSON by SaveManager.
    /// Fields are public for JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>Level the PLAY button starts (1-based).</summary>
        public int currentLevel = 1;
        public int highestLevelCompleted;
        public int coins;
        public int[] upgradeLevels = new int[6];
        /// <summary>Highest weapon evolution tier ever reached (unlocks entries on the WEAPONS screen).</summary>
        public int highestTierUnlocked = 1;
        public List<string> ownedSkins = new List<string> { "factory" };
        public string equippedSkin = "factory";
        public List<int> completedLevels = new List<int>();

        // statistics
        public int totalEnemiesDefeated;
        public int totalBossesDefeated;
        public int totalRuns;

        // settings
        public float musicVolume = 0.6f;
        public float sfxVolume = 0.85f;
        public bool muted;
        public bool vibration = true;
        public bool screenShake = true;
        public bool highQuality = true;
        public bool tutorialSeen;

        public int GetUpgradeLevel(UpgradeType t)
        {
            int i = (int)t;
            return upgradeLevels != null && i < upgradeLevels.Length ? upgradeLevels[i] : 0;
        }

        public void SetUpgradeLevel(UpgradeType t, int level)
        {
            EnsureArrays();
            upgradeLevels[(int)t] = level;
        }

        public bool IsLevelCompleted(int level) { return completedLevels != null && completedLevels.Contains(level); }

        public void MarkLevelCompleted(int level)
        {
            EnsureArrays();
            if (!completedLevels.Contains(level)) completedLevels.Add(level);
            if (level > highestLevelCompleted) highestLevelCompleted = level;
            if (currentLevel <= level) currentLevel = level + 1;
        }

        void EnsureArrays()
        {
            int n = Enum.GetValues(typeof(UpgradeType)).Length;
            if (upgradeLevels == null) upgradeLevels = new int[n];
            if (upgradeLevels.Length != n)
            {
                var arr = new int[n];
                for (int i = 0; i < Math.Min(n, upgradeLevels.Length); i++) arr[i] = upgradeLevels[i];
                upgradeLevels = arr;
            }
            if (completedLevels == null) completedLevels = new List<int>();
            if (ownedSkins == null) ownedSkins = new List<string>();
        }

        /// <summary>
        /// Repairs any out-of-range or missing values (e.g. from a hand-edited or older save).
        /// Returns true if something had to be fixed.
        /// </summary>
        public bool Sanitize()
        {
            bool fixedSomething = false;
            int n = Enum.GetValues(typeof(UpgradeType)).Length;
            if (upgradeLevels == null || upgradeLevels.Length != n || completedLevels == null || ownedSkins == null)
            {
                EnsureArrays();
                fixedSomething = true;
            }
            for (int i = 0; i < upgradeLevels.Length; i++)
            {
                int max = UpgradeCatalog.Get((UpgradeType)i).MaxLevel;
                int clamped = Math.Max(0, Math.Min(max, upgradeLevels[i]));
                if (clamped != upgradeLevels[i]) { upgradeLevels[i] = clamped; fixedSomething = true; }
            }
            if (coins < 0) { coins = 0; fixedSomething = true; }
            if (currentLevel < 1) { currentLevel = 1; fixedSomething = true; }
            if (currentLevel > 9999) { currentLevel = 9999; fixedSomething = true; }
            if (highestLevelCompleted < 0) { highestLevelCompleted = 0; fixedSomething = true; }
            if (highestTierUnlocked < 1 || highestTierUnlocked > WeaponEvolution.MaxTier)
            {
                highestTierUnlocked = Math.Max(1, Math.Min(WeaponEvolution.MaxTier, highestTierUnlocked));
                fixedSomething = true;
            }
            if (!ownedSkins.Contains("factory")) { ownedSkins.Add("factory"); fixedSomething = true; }
            if (string.IsNullOrEmpty(equippedSkin) || !ownedSkins.Contains(equippedSkin)) { equippedSkin = "factory"; fixedSomething = true; }
            float mv = Clamp01(musicVolume), sv = Clamp01(sfxVolume);
            if (mv != musicVolume || sv != sfxVolume || float.IsNaN(musicVolume) || float.IsNaN(sfxVolume))
            {
                musicVolume = float.IsNaN(mv) ? 0.6f : mv;
                sfxVolume = float.IsNaN(sv) ? 0.85f : sv;
                fixedSomething = true;
            }
            version = CurrentVersion;
            return fixedSomething;
        }

        static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }

    /// <summary>Pure purchase rules shared by the shop UI and tests.</summary>
    public static class Purchases
    {
        public enum Result { Success, NotEnoughCoins, MaxLevel, AlreadyOwned }

        public static Result TryBuyUpgrade(SaveData save, UpgradeType type)
        {
            var def = UpgradeCatalog.Get(type);
            int lvl = save.GetUpgradeLevel(type);
            int price = def.PriceForNextLevel(lvl);
            if (price < 0) return Result.MaxLevel;
            if (save.coins < price) return Result.NotEnoughCoins;
            save.coins -= price;
            save.SetUpgradeLevel(type, lvl + 1);
            return Result.Success;
        }

        public static Result TryBuySkin(SaveData save, string skinId)
        {
            if (save.ownedSkins.Contains(skinId)) return Result.AlreadyOwned;
            var skin = SkinCatalog.Get(skinId);
            if (save.coins < skin.price) return Result.NotEnoughCoins;
            save.coins -= skin.price;
            save.ownedSkins.Add(skinId);
            save.equippedSkin = skinId;
            return Result.Success;
        }
    }
}
