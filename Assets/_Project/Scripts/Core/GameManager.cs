using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeaponFactory.Audio;
using WeaponFactory.Logic;
using WeaponFactory.Save;
using WeaponFactory.Utilities;

namespace WeaponFactory.Core
{
    /// <summary>
    /// Persistent root of the game (DontDestroyOnLoad). Created automatically before the first
    /// scene loads, so pressing Play in ANY scene works. Owns save data, audio, settings and the
    /// coin wallet, and makes sure every scene gets the right bootstrap.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveManager SaveSystem { get; private set; }
        public SaveData Save { get { return SaveSystem.Data; } }
        public AudioManager Audio { get; private set; }

        /// <summary>Level number the Gameplay scene will load.</summary>
        public int LevelToPlay { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            Input.multiTouchEnabled = true;

            SaveSystem = new SaveManager();
            SaveSystem.Load();
            DataRegistry.Load();
            LevelToPlay = Save.currentLevel;

            Audio = AudioManager.Create(transform);
            gameObject.AddComponent<SceneLoader>();
            ApplySettings();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            // The first scene was loaded before we subscribed, so check it now.
            SceneBootstrapGuard.Ensure(SceneManager.GetActiveScene());
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) SceneBootstrapGuard.Ensure(scene);
        }

        // ---------------- coins ----------------

        public int Coins { get { return Save.coins; } }

        public void AddCoins(int amount, bool save = true)
        {
            if (amount == 0) return;
            Save.coins = Mathf.Max(0, Save.coins + amount);
            GameEvents.RaiseCoinsChanged(Save.coins);
            if (save) SaveSystem.Save();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Save.coins < amount) return false;
            Save.coins -= amount;
            GameEvents.RaiseCoinsChanged(Save.coins);
            SaveSystem.Save();
            return true;
        }

        public void NotifyCoinsChanged() { GameEvents.RaiseCoinsChanged(Save.coins); }

        public PermanentBonuses Bonuses { get { return UpgradeCatalog.BuildBonuses(Save.GetUpgradeLevel); } }

        public WeaponSkinDefinition EquippedSkin { get { return SkinCatalog.Get(Save.equippedSkin); } }

        // ---------------- settings ----------------

        public void ApplySettings()
        {
            var s = Save;
            if (Audio != null) Audio.ApplySettings(s.musicVolume, s.sfxVolume, s.muted);
            ScreenShake.Enabled = s.screenShake;
            VFX.QualityScale = s.highQuality ? 1f : 0.55f;
            QualitySettings.shadows = s.highQuality ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            QualitySettings.shadowDistance = s.highQuality ? 45f : 0f;
            QualitySettings.antiAliasing = s.highQuality ? 2 : 0;
            GameEvents.RaiseSettingsChanged();
        }

        public void SaveSettings()
        {
            ApplySettings();
            SaveSystem.Save();
        }

        public void Vibrate()
        {
            if (!Save.vibration) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>Development-only: wipes all progress.</summary>
        public void ResetSave()
        {
            SaveSystem.ResetAll();
            LevelToPlay = Save.currentLevel;
            ApplySettings();
            GameEvents.RaiseCoinsChanged(Save.coins);
        }

        void OnApplicationPause(bool paused) { if (paused && SaveSystem != null) SaveSystem.Save(); }
        void OnApplicationQuit() { if (SaveSystem != null) SaveSystem.Save(); }

        // ---------------- flow ----------------

        public void PlayLevel(int levelNumber)
        {
            LevelToPlay = Mathf.Max(1, levelNumber);
            SceneLoader.Load(GameScene.Gameplay);
        }

        public void GoToMenu() { SceneLoader.Load(GameScene.MainMenu); }
    }

    /// <summary>Global events for loosely coupled UI updates.</summary>
    public static class GameEvents
    {
        public static event Action<int> CoinsChanged;
        public static event Action SettingsChanged;

        public static void RaiseCoinsChanged(int coins) { if (CoinsChanged != null) CoinsChanged(coins); }
        public static void RaiseSettingsChanged() { if (SettingsChanged != null) SettingsChanged(); }
    }
}
