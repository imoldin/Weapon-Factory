using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Combat;
using WeaponFactory.Core;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.UI;
using WeaponFactory.Utilities;

namespace WeaponFactory.Levels
{
    public enum RunPhase { Intro, Running, ArenaTransition, Combat, Victory, Defeat }

    /// <summary>
    /// Orchestrates one level: builds environment + track + player + UI from the LevelDefinition,
    /// runs the phases (intro → running → arena → combat → victory/defeat), spawns enemy waves,
    /// computes rewards and persists progress.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public LevelDefinition Definition { get; private set; }
        public PlayerController Player { get; private set; }
        public RunPhase Phase { get; private set; }
        public int RunCoins { get; private set; }
        public float ArenaTimeLeft { get; private set; }
        public BossController Boss { get; private set; }
        public string LevelLabel { get; private set; }
        public string WaveLabel { get; private set; }

        public float Progress
        {
            get
            {
                if (Player == null || _arenaZ <= 0f) return 0f;
                return Mathf.Clamp01(Player.transform.position.z / _arenaZ);
            }
        }

        CameraFollow _cam;
        CombatManager _combat;
        GameplayHUD _hud;
        PauseMenuUI _pause;
        VictoryUI _victory;
        DefeatUI _defeat;
        SettingsUI _settings;
        EnvironmentBuilder.Result _env;
        Transform _track, _enemyRoot;
        float _arenaZ;
        int _waveIndex = -1;
        float _waveTimer;
        bool _waitingForWave;
        bool _paused;
        readonly List<EnemyController> _enemies = new List<EnemyController>();
        float _slowMoTimer;
        int _peakTier;

        const float ArenaLength = 34f;

        public void Init(int levelNumber)
        {
            Instance = this;
            var gm = GameManager.Instance;
            Definition = DataRegistry.GetLevel(levelNumber);
            LevelLabel = "LEVEL " + Definition.levelNumber;
            WaveLabel = "";

            float trackLength = Definition.TrackLength;
            _env = EnvironmentBuilder.BuildLevel(Definition.theme, trackLength, ArenaLength, Definition.seed);
            var palette = ThemePalette.For(Definition.theme);

            _combat = gameObject.AddComponent<CombatManager>();
            _track = new GameObject("Track").transform;
            _arenaZ = LevelGenerator.Build(Definition, _track);
            _enemyRoot = new GameObject("Enemies").transform;

            Player = PlayerController.Create(new Vector3(0f, 1.15f, 0f), gm.Bonuses, gm.EquippedSkin);
            Player.Movement.forwardSpeed = Definition.runSpeed;
            Player.Movement.SetLimits(LevelGenerator.LaneLimit);
            Player.Movement.MoveForward = false;
            Player.Movement.AllowSteer = false;
            Player.Firing.FiringEnabled = false;
            Player.Died += OnPlayerDied;
            Player.Damaged += OnPlayerDamaged;
            Player.ComponentLost += t => Notify("LOST " + ComponentCatalog.Get(t).displayName + "!", UITheme.Danger);
            Player.Weapon.TierChanged += OnTierChanged;
            _peakTier = Player.Weapon.Tier;

            _cam = CameraFollow.Create(palette.background);
            _cam.target = Player.transform;
            _cam.SnapToTarget();

            // UI
            Canvas canvas;
            var uiRoot = UIFactory.CreateCanvas("GameplayCanvas", 10, out canvas);
            _hud = GameplayHUD.Build(this, uiRoot);
            _settings = SettingsUI.Build(uiRoot);
            _pause = PauseMenuUI.Build(uiRoot, this, _settings);
            _victory = VictoryUI.Build(uiRoot, this);
            _defeat = DefeatUI.Build(uiRoot, this);

            gm.Save.totalRuns++;
            Phase = RunPhase.Intro;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        // =====================================================================================
        // Phase flow
        // =====================================================================================

        void Update()
        {
            float dt = Time.deltaTime;
            if (_slowMoTimer > 0f && !_paused)
            {
                _slowMoTimer -= Time.unscaledDeltaTime;
                if (_slowMoTimer <= 0f) Time.timeScale = 1f;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && (Phase == RunPhase.Running || Phase == RunPhase.Combat || Phase == RunPhase.ArenaTransition))
            {
                if (_paused) Resume(); else Pause();
            }
            if (_paused) return;

            switch (Phase)
            {
                case RunPhase.Intro:
                    if (Player.Input.PressedThisFrame && !SceneLoader.IsLoading) StartRun();
                    break;

                case RunPhase.Running:
                    if (Player.transform.position.z >= _arenaZ + 1.5f) EnterArena();
                    break;

                case RunPhase.Combat:
                    ArenaTimeLeft -= dt;
                    UpdateWaves(dt);
                    if (ArenaTimeLeft <= 0f && Phase == RunPhase.Combat) Fail("Time ran out! The robots held the line.");
                    break;
            }
        }

        void StartRun()
        {
            Phase = RunPhase.Running;
            _hud.HideIntro();
            Player.Movement.MoveForward = true;
            Player.Movement.AllowSteer = true;
            Player.Firing.FiringEnabled = true;
            EnvironmentBuilder.OpenDoor(_env.startDoorLeft, _env.startDoorRight, 0.6f);
            AudioManager.Play(SfxId.Whoosh, 0.8f);
            if (Definition.tutorial) Tween.Delay(1.2f, () => Notify("COLLECT WEAPON PARTS!", UITheme.Accent, true), false, this);
        }

        void EnterArena()
        {
            Phase = RunPhase.ArenaTransition;
            Player.Movement.MoveForward = false;
            Player.Movement.SetLimits(EnvironmentBuilder.ArenaHalfWidth - 1.2f);
            _cam.SetArena(new Vector3(0f, 0f, _arenaZ + ArenaLength * 0.7f));
            EnvironmentBuilder.OpenDoor(_env.arenaDoorLeft, _env.arenaDoorRight, 1.0f);
            Notify(Definition.isBossLevel ? "BOSS FIGHT!" : "FIGHT!", UITheme.Danger, true);
            AudioManager.Play(SfxId.Whoosh, 0.8f);
            _hud.EnterCombat();
            ArenaTimeLeft = Definition.arenaTimeLimit;
            Tween.Delay(1.0f, () =>
            {
                if (Phase != RunPhase.ArenaTransition) return;
                Phase = RunPhase.Combat;
                _waveIndex = -1;
                _waitingForWave = true;
                _waveTimer = 0.2f;
            }, false, this);
        }

        void UpdateWaves(float dt)
        {
            if (_waitingForWave)
            {
                _waveTimer -= dt;
                if (_waveTimer <= 0f)
                {
                    _waitingForWave = false;
                    _waveIndex++;
                    SpawnWave(_waveIndex);
                }
                return;
            }
            _enemies.RemoveAll(IsGone);
            if (_enemies.Count == 0)
            {
                if (_waveIndex >= Definition.waves.Count - 1) Win();
                else
                {
                    _waitingForWave = true;
                    _waveTimer = Definition.waves[_waveIndex + 1].delay;
                    Notify("WAVE CLEARED!", UITheme.Primary);
                }
            }
        }

        static readonly System.Predicate<EnemyController> IsGone = e => e == null || e.IsDead;

        void SpawnWave(int index)
        {
            if (index < 0 || index >= Definition.waves.Count) return;
            var wave = Definition.waves[index];
            WaveLabel = Definition.waves.Count > 1 ? "WAVE " + (index + 1) + "/" + Definition.waves.Count : "";
            float baseZ = _arenaZ + 20f;
            int slot = 0;
            foreach (var spawn in wave.spawns)
            {
                for (int i = 0; i < spawn.count; i++)
                {
                    Vector3 pos;
                    if (spawn.type == EnemyType.Boss) pos = new Vector3(0f, 0f, _arenaZ + 24f);
                    else
                    {
                        float x = Mathf.Lerp(-4f, 4f, ((slot * 0.618f) % 1f));
                        pos = new Vector3(x, 0f, baseZ + (slot % 3) * 2.5f + Random.Range(0f, 1.5f));
                        if (EnemyCatalog.Get(spawn.type).flying) pos.y = 1.3f;
                    }
                    slot++;
                    SpawnEnemy(spawn.type, pos);
                }
            }
        }

        public EnemyController SpawnEnemy(EnemyType type, Vector3 position)
        {
            var def = EnemyCatalog.Get(type);
            float hp = Definition.enemyHealthMultiplier;
            var e = EnemyFactory.Spawn(_enemyRoot, def, position, hp, Definition.bossName);
            _enemies.Add(e);
            if (type == EnemyType.Boss) Boss = (BossController)e;
            return e;
        }

        public void KillAllEnemies()
        {
            foreach (var e in _enemies.ToArray())
                if (e != null && !e.IsDead && e.Definition.type != EnemyType.Boss)
                    e.Health.ApplyDamage(e.Health.Health + 1f, false, e.transform.position);
        }

        public void OnBossAppeared(BossController boss)
        {
            Boss = boss;
            _hud.ShowBoss(boss.BossName);
            Notify(boss.BossName, new Color(1f, 0.5f, 0.85f), true);
        }

        // =====================================================================================
        // Results
        // =====================================================================================

        void Win()
        {
            if (Phase == RunPhase.Victory || Phase == RunPhase.Defeat) return;
            Phase = RunPhase.Victory;
            Player.Firing.FiringEnabled = false;
            Player.Movement.AllowSteer = false;
            _hud.HideBoss();
            _combat.ClearProjectiles();
            SlowMo(0.35f, 0.8f);
            AudioManager.Play(SfxId.Victory, 1f);
            if (GameManager.Instance.Audio != null) GameManager.Instance.Audio.DuckMusic(1.5f);
            VFX.Play(VFXType.Confetti, Player.transform.position + new Vector3(0f, 3f, 4f), Color.white, 80);

            var gm = GameManager.Instance;
            var save = gm.Save;
            var stats = Player.Weapon.Stats;
            var r = RewardCalculator.Calculate(Definition.levelNumber, Definition.baseReward, _combat.EnemyRewardSum, _combat.BossDefeated,
                EnemyCatalog.Get(EnemyType.Boss).reward, stats.tier, Player.IntegrityFraction, RunCoins, gm.Bonuses.coinMultiplier);

            save.MarkLevelCompleted(Definition.levelNumber);
            save.totalEnemiesDefeated += _combat.EnemiesDefeated;
            if (_combat.BossDefeated) save.totalBossesDefeated++;
            save.highestTierUnlocked = Mathf.Max(save.highestTierUnlocked, _peakTier);
            if (Definition.tutorial) save.tutorialSeen = true;
            gm.AddCoins(r.total); // also saves

            int totalEnemies = Mathf.Max(Definition.TotalEnemies, _combat.EnemiesDefeated);
            Tween.Delay(1.3f, () => _victory.Show(r, _combat.EnemiesDefeated, totalEnemies, Player.Weapon.WeaponName, stats.tier, Definition.isBossLevel), true, this);
        }

        void Fail(string reason)
        {
            if (Phase == RunPhase.Victory || Phase == RunPhase.Defeat) return;
            Phase = RunPhase.Defeat;
            Player.Firing.FiringEnabled = false;
            Player.Movement.MoveForward = false;
            Player.Movement.AllowSteer = false;
            _hud.HideBoss();
            AudioManager.Play(SfxId.Defeat, 1f);
            if (GameManager.Instance.Audio != null) GameManager.Instance.Audio.DuckMusic(1.5f);
            SlowMo(0.3f, 0.6f);

            var gm = GameManager.Instance;
            gm.Save.highestTierUnlocked = Mathf.Max(gm.Save.highestTierUnlocked, _peakTier);
            int kept = RunCoins; // coins picked up on the track are kept
            gm.AddCoins(kept);
            if (kept == 0) gm.SaveSystem.Save();
            Tween.Delay(1.2f, () => _defeat.Show(reason, kept), true, this);
        }

        void OnPlayerDied(string reason)
        {
            Fail(string.IsNullOrEmpty(reason) ? "Your weapon was destroyed." : reason + "!");
        }

        void OnPlayerDamaged(float amount, bool lostComponent)
        {
            _hud.FlashDamage();
        }

        void OnTierChanged(int oldTier, int newTier)
        {
            bool up = newTier > oldTier;
            _hud.ShowEvolution(Player.Weapon.WeaponName, up);
            if (up)
            {
                _peakTier = Mathf.Max(_peakTier, newTier);
                AudioManager.Play(SfxId.Evolve, 1f);
                VFX.Play(VFXType.Ring, Player.HitCenter, UITheme.Gold, 50);
                VFX.Play(VFXType.Pickup, Player.HitCenter, UITheme.Gold, 40);
                ScreenShake.Add(0.2f);
                SlowMo(0.5f, 0.35f);
                GameManager.Instance.Vibrate();
            }
            else
            {
                AudioManager.Play(SfxId.GateNegative, 1f);
            }
        }

        void SlowMo(float scale, float realSeconds)
        {
            if (_paused) return;
            Time.timeScale = scale;
            _slowMoTimer = realSeconds;
        }

        // =====================================================================================
        // API used by gameplay objects and UI
        // =====================================================================================

        public void Notify(string text, Color color, bool big = false)
        {
            if (_hud != null) _hud.Notify(text, color, big);
        }

        public void AddRunCoins(int amount, Vector3? worldPos = null)
        {
            if (amount <= 0) return;
            RunCoins += amount;
            if (_hud != null) _hud.PunchCoins();
            if (worldPos.HasValue)
            {
                FloatingText.Spawn(worldPos.Value, "+" + NumberCache.Get(amount), new Color(1f, 0.85f, 0.2f), 1.2f, 0.9f);
                AudioManager.Play(SfxId.Coin, 0.5f);
            }
        }

        public void Pause()
        {
            if (_paused || Phase == RunPhase.Victory || Phase == RunPhase.Defeat || Phase == RunPhase.Intro) return;
            _paused = true;
            Time.timeScale = 0f;
            _pause.Show();
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = 1f;
            _pause.Hide();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            GameManager.Instance.PlayLevel(Definition.levelNumber);
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            GameManager.Instance.PlayLevel(GameManager.Instance.Save.currentLevel);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            GameManager.Instance.GoToMenu();
        }

        void OnApplicationPause(bool pausedByOs)
        {
            if (pausedByOs && (Phase == RunPhase.Running || Phase == RunPhase.Combat)) Pause();
        }
    }
}
