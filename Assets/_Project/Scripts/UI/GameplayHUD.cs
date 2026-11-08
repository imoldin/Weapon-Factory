using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Combat;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>
    /// In-game HUD: level + progress bar, coins, weapon name/tier/evolution progress, damage and
    /// fire-rate chips, armour and ammo bars, boss health bar, arena timer, floating notifications,
    /// evolution banner and the start/tutorial overlay. Text is only rebuilt when values change.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        LevelManager _lm;
        Text _level, _coins, _weaponName, _dmg, _rate, _timer, _wave, _reload, _bossName, _armorText;
        Image _progress, _evolve, _armor, _ammo, _boss;
        RectTransform _coinPill, _weaponCard, _bossRoot, _timerRoot, _intro, _hand, _progressRoot, _dmgChip, _rateChip;
        CanvasGroup _introGroup;
        readonly List<Text> _notes = new List<Text>();
        readonly List<float> _noteTimers = new List<float>();
        int _noteIndex;
        Text _banner, _bannerSub;
        RectTransform _bannerRoot;
        Image _damageFlash;

        // change tracking
        int _shownCoins = -1, _shownDmg = -1, _shownTier = -1, _shownTimer = -1, _shownArmor = -1;
        float _shownRate = -1f;

        public Button PauseButton { get; private set; }

        public static GameplayHUD Build(LevelManager lm, Transform parentRoot)
        {
            var host = UIFactory.Rect(parentRoot, "HUD");
            UIFactory.Stretch(host);
            var hud = host.gameObject.AddComponent<GameplayHUD>();
            hud._lm = lm;
            hud.Construct(host);
            return hud;
        }

        void Construct(RectTransform root)
        {
            // damage flash (full screen red vignette-ish)
            _damageFlash = UIFactory.Panel(root, "DamageFlash", new Color(1f, 0.1f, 0.1f, 0f), false);
            UIFactory.Stretch(_damageFlash.rectTransform, -300, -300, -300, -300);

            // ---- top bar ----
            PauseButton = UIFactory.Button(root, "", new Color(0f, 0f, 0f, 0.45f), new Vector2(120f, 120f), () => _lm.Pause(), Icon.Pause);
            UIFactory.Place((RectTransform)PauseButton.transform, new Vector2(0f, 1f), new Vector2(120f, 120f), new Vector2(30f, -30f), new Vector2(0f, 1f));

            _coins = UIFactory.CoinPill(root, out _coinPill);
            UIFactory.Place(_coinPill, new Vector2(1f, 1f), new Vector2(270f, 90f), new Vector2(-30f, -45f), new Vector2(1f, 1f));

            _level = UIFactory.Label(root, "LEVEL 1", 50, Color.white);
            UIFactory.Place(_level.rectTransform, new Vector2(0.5f, 1f), new Vector2(500f, 70f), new Vector2(0f, -60f));
            _progress = UIFactory.ProgressBar(root, "Progress", new Vector2(460f, 40f), new Color(0f, 0f, 0f, 0.5f), UITheme.Primary);
            _progressRoot = (RectTransform)_progress.transform.parent;
            UIFactory.Place(_progressRoot, new Vector2(0.5f, 1f), new Vector2(460f, 40f), new Vector2(0f, -120f));
            var flag = UIFactory.Icon(_progressRoot, Icon.Skull, Color.white, 50f);
            UIFactory.Place(flag.rectTransform, new Vector2(1f, 0.5f), new Vector2(50f, 50f), new Vector2(34f, 0f));

            // arena timer (replaces the progress bar in combat)
            _timerRoot = UIFactory.Rect(root, "Timer");
            UIFactory.Place(_timerRoot, new Vector2(0.5f, 1f), new Vector2(300f, 80f), new Vector2(0f, -125f));
            _timer = UIFactory.Label(_timerRoot, "45", 60, Color.white);
            UIFactory.Stretch(_timer.rectTransform);
            _timerRoot.gameObject.SetActive(false);
            _wave = UIFactory.Label(root, "", 34, UITheme.TextMuted);
            UIFactory.Place(_wave.rectTransform, new Vector2(0.5f, 1f), new Vector2(400f, 50f), new Vector2(0f, -175f));

            // ---- boss bar ----
            var bossBg = UIFactory.Panel(root, "BossBar", new Color(0f, 0f, 0f, 0.6f));
            _bossRoot = bossBg.rectTransform;
            UIFactory.Place(_bossRoot, new Vector2(0.5f, 1f), new Vector2(900f, 120f), new Vector2(0f, -260f));
            _bossName = UIFactory.Label(_bossRoot, "BOSS", 38, new Color(1f, 0.5f, 0.85f));
            UIFactory.Place(_bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(860f, 50f), new Vector2(0f, -30f));
            _boss = UIFactory.ProgressBar(_bossRoot, "Bar", new Vector2(860f, 46f), new Color(0.2f, 0.05f, 0.15f, 0.9f), new Color(0.9f, 0.2f, 0.65f));
            UIFactory.Place((RectTransform)_boss.transform.parent, new Vector2(0.5f, 0f), new Vector2(860f, 46f), new Vector2(0f, 32f));
            _bossRoot.gameObject.SetActive(false);

            // ---- weapon card ----
            var card = UIFactory.Panel(root, "WeaponCard", new Color(0f, 0f, 0f, 0.42f));
            _weaponCard = card.rectTransform;
            UIFactory.Place(_weaponCard, new Vector2(0.5f, 0f), new Vector2(620f, 150f), new Vector2(0f, 330f));
            _weaponName = UIFactory.Label(_weaponCard, "", 40, Color.white);
            UIFactory.Place(_weaponName.rectTransform, new Vector2(0.5f, 1f), new Vector2(600f, 60f), new Vector2(0f, -38f));
            _evolve = UIFactory.ProgressBar(_weaponCard, "Evolve", new Vector2(520f, 34f), new Color(0f, 0f, 0f, 0.5f), UITheme.Gold);
            UIFactory.Place((RectTransform)_evolve.transform.parent, new Vector2(0.5f, 0f), new Vector2(520f, 34f), new Vector2(0f, 38f));
            var evoLabel = UIFactory.Label(_evolve.transform.parent, "EVOLUTION", 22, Color.white);
            UIFactory.Stretch(evoLabel.rectTransform);

            _dmgChip = Chip(root, "DMG", UITheme.Danger, new Vector2(-425f, 330f), out _dmg);
            _rateChip = Chip(root, "RATE", UITheme.Accent, new Vector2(425f, 330f), out _rate);

            // ---- armour + ammo ----
            var armorBar = UIFactory.ProgressBar(root, "Armor", new Vector2(620f, 52f), new Color(0f, 0f, 0f, 0.55f), UITheme.Primary);
            _armor = armorBar;
            UIFactory.Place((RectTransform)armorBar.transform.parent, new Vector2(0.5f, 0f), new Vector2(620f, 52f), new Vector2(0f, 200f));
            var heart = UIFactory.Icon(armorBar.transform.parent, Icon.Heart, new Color(1f, 0.4f, 0.45f), 60f);
            UIFactory.Place(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, 60f), new Vector2(-40f, 0f));
            _armorText = UIFactory.Label(armorBar.transform.parent, "100", 30, Color.white);
            UIFactory.Stretch(_armorText.rectTransform);
            _ammo = UIFactory.ProgressBar(root, "Ammo", new Vector2(620f, 26f), new Color(0f, 0f, 0f, 0.55f), UITheme.Blue);
            UIFactory.Place((RectTransform)_ammo.transform.parent, new Vector2(0.5f, 0f), new Vector2(620f, 26f), new Vector2(0f, 150f));
            _reload = UIFactory.Label(root, "RELOADING", 30, UITheme.Blue);
            UIFactory.Place(_reload.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 40f), new Vector2(0f, 110f));
            _reload.enabled = false;

            // ---- notifications ----
            for (int i = 0; i < 5; i++)
            {
                var t = UIFactory.Label(root, "", 56, Color.white);
                UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 80f), new Vector2(0f, 200f));
                t.enabled = false;
                _notes.Add(t);
                _noteTimers.Add(0f);
            }

            // ---- evolution banner ----
            _bannerRoot = UIFactory.Rect(root, "Banner");
            UIFactory.Place(_bannerRoot, new Vector2(0.5f, 0.5f), new Vector2(1000f, 260f), new Vector2(0f, 420f));
            var bannerBg = UIFactory.Panel(_bannerRoot, "BG", new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Stretch(bannerBg.rectTransform);
            _banner = UIFactory.Label(_bannerRoot, "EVOLVED!", 96, UITheme.Gold);
            UIFactory.Place(_banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(980f, 120f), new Vector2(0f, -80f));
            _bannerSub = UIFactory.Label(_bannerRoot, "", 54, Color.white);
            UIFactory.Place(_bannerSub.rectTransform, new Vector2(0.5f, 0f), new Vector2(980f, 80f), new Vector2(0f, 60f));
            _bannerRoot.gameObject.SetActive(false);

            // ---- intro / tutorial overlay ----
            _intro = UIFactory.Rect(root, "Intro");
            UIFactory.Stretch(_intro);
            _introGroup = _intro.gameObject.AddComponent<CanvasGroup>();
            _introGroup.blocksRaycasts = false;
            _introGroup.interactable = false;
            var lvlTitle = UIFactory.Label(_intro, "", 110, Color.white);
            UIFactory.Place(lvlTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 140f), new Vector2(0f, 420f));
            lvlTitle.text = "LEVEL " + _lm.Definition.levelNumber;
            var lvlName = UIFactory.Label(_intro, _lm.Definition.displayName, 64, UITheme.Accent);
            UIFactory.Place(lvlName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 90f), new Vector2(0f, 320f));
            var introText = UIFactory.Label(_intro, _lm.Definition.introText, 42, UITheme.TextMuted);
            UIFactory.Place(introText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 70f), new Vector2(0f, 240f));
            var drag = UIFactory.Label(_intro, "HOLD & DRAG TO START", 54, Color.white);
            UIFactory.Place(drag.rectTransform, new Vector2(0.5f, 0f), new Vector2(1000f, 80f), new Vector2(0f, 560f));
            drag.gameObject.AddComponent<Pulser>().amount = 0.06f;
            // animated hand (finger dot + track line)
            var track = UIFactory.Panel(_intro, "SwipeTrack", new Color(1f, 1f, 1f, 0.25f));
            UIFactory.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(520f, 16f), new Vector2(0f, 680f));
            var hand = UIFactory.Icon(_intro, Icon.Circle, new Color(1f, 1f, 1f, 0.9f), 110f);
            _hand = hand.rectTransform;
            UIFactory.Place(_hand, new Vector2(0.5f, 0f), new Vector2(110f, 110f), new Vector2(0f, 680f));
            var mover = hand.gameObject.AddComponent<PingPongMover>();
            mover.offset = new Vector3(230f, 0f, 0f);
            mover.period = 1.6f;
        }

        RectTransform Chip(Transform root, string label, Color color, Vector2 pos, out Text value)
        {
            var bg = UIFactory.Panel(root, "Chip_" + label, new Color(0f, 0f, 0f, 0.42f));
            UIFactory.Place(bg.rectTransform, new Vector2(0.5f, 0f), new Vector2(190f, 150f), pos);
            var l = UIFactory.Label(bg.rectTransform, label, 30, color);
            UIFactory.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, 50f), new Vector2(0f, -32f));
            value = UIFactory.Label(bg.rectTransform, "0", 52, Color.white);
            UIFactory.Place(value.rectTransform, new Vector2(0.5f, 0f), new Vector2(200f, 70f), new Vector2(0f, 45f));
            return bg.rectTransform;
        }

        // ---------------- public API ----------------

        public void HideIntro()
        {
            if (_intro == null || !_intro.gameObject.activeSelf) return;
            Tween.Fade(_introGroup, 0f, 0.3f, 0f, () => _intro.gameObject.SetActive(false));
        }

        public void Notify(string text, Color color, bool big)
        {
            var t = _notes[_noteIndex];
            _noteTimers[_noteIndex] = 1.1f;
            _noteIndex = (_noteIndex + 1) % _notes.Count;
            t.enabled = true;
            t.text = text;
            t.color = color;
            t.fontSize = big ? 74 : 54;
            t.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            t.rectTransform.localScale = Vector3.one * 0.6f;
            Tween.Scale(t.rectTransform, Vector3.one, 0.25f, Ease.OutBack, 0f, true);
            // push older notes up
            for (int i = 0; i < _notes.Count; i++)
            {
                if (_notes[i] == t || !_notes[i].enabled) continue;
                var rt = _notes[i].rectTransform;
                rt.anchoredPosition += new Vector2(0f, 80f);
            }
        }

        public void ShowEvolution(string weaponName, bool upgrade)
        {
            _bannerRoot.gameObject.SetActive(true);
            _banner.text = upgrade ? "EVOLVED!" : "DOWNGRADED";
            _banner.color = upgrade ? UITheme.Gold : UITheme.Danger;
            _bannerSub.text = weaponName;
            _bannerRoot.localScale = Vector3.one * 0.3f;
            Tween.Kill(_bannerRoot);
            Tween.Scale(_bannerRoot, Vector3.one, 0.4f, Ease.OutElastic, 0f, true);
            Tween.Delay(1.5f, () => { if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false); }, true, this);
            Tween.Punch(_weaponCard, 0.15f, 0.35f, true);
        }

        public void ShowBoss(string name)
        {
            _bossRoot.gameObject.SetActive(true);
            _bossName.text = name;
            _bossRoot.localScale = new Vector3(1f, 0.01f, 1f);
            Tween.Scale(_bossRoot, Vector3.one, 0.4f, Ease.OutBack, 0f, true);
        }

        public void HideBoss() { if (_bossRoot != null) _bossRoot.gameObject.SetActive(false); }

        public void EnterCombat()
        {
            _progressRoot.gameObject.SetActive(false);
            _timerRoot.gameObject.SetActive(true);
        }

        public void PunchCoins() { Tween.Punch(_coinPill, 0.15f, 0.2f, true); }

        public void FlashDamage()
        {
            Tween.Kill(_damageFlash);
            Tween.Run(_damageFlash, 0.4f, p => _damageFlash.color = new Color(1f, 0.1f, 0.1f, 0.35f * (1f - p)), Ease.OutQuad, null, 0f, true);
        }

        public void SetVisible(bool visible) { gameObject.SetActive(visible); }

        // ---------------- per-frame update ----------------

        void Update()
        {
            if (_lm == null) return;
            var player = _lm.Player;
            float udt = Time.unscaledDeltaTime;

            _level.text = _lm.LevelLabel;
            _progress.fillAmount = _lm.Progress;

            int coins = _lm.RunCoins;
            if (coins != _shownCoins) { _shownCoins = coins; _coins.text = NumberCache.Compact(coins); }

            if (player != null)
            {
                var w = player.Weapon;
                var s = w.Stats;
                if (s.tier != _shownTier)
                {
                    _shownTier = s.tier;
                    _weaponName.text = w.WeaponName + "  " + MainMenuUI.Stars(s.tier);
                }
                _evolve.fillAmount = Mathf.Lerp(_evolve.fillAmount, w.TierProgress, 1f - Mathf.Exp(-8f * udt));
                int dmg = s.DisplayDamage;
                if (dmg != _shownDmg)
                {
                    bool up = dmg > _shownDmg && _shownDmg >= 0;
                    _shownDmg = dmg;
                    _dmg.text = NumberCache.Get(dmg);
                    if (_shownDmg >= 0) Tween.Punch(_dmgChip, up ? 0.15f : 0.08f, 0.25f, true);
                }
                float rate = Mathf.Round(s.fireRate * 10f) / 10f;
                if (Mathf.Abs(rate - _shownRate) > 0.01f)
                {
                    _shownRate = rate;
                    _rate.text = rate.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "/s";
                    Tween.Punch(_rateChip, 0.12f, 0.25f, true);
                }
                _armor.fillAmount = Mathf.Lerp(_armor.fillAmount, player.IntegrityFraction, 1f - Mathf.Exp(-10f * udt));
                _armor.color = Color.Lerp(UITheme.Danger, UITheme.Primary, player.IntegrityFraction);
                int armor = Mathf.CeilToInt(player.Integrity);
                if (armor != _shownArmor) { _shownArmor = armor; _armorText.text = NumberCache.Get(armor); }
                var f = player.Firing;
                _ammo.fillAmount = f.IsReloading ? f.ReloadProgress : (s.magazine > 0 ? f.Ammo / (float)s.magazine : 0f);
                _reload.enabled = f.IsReloading;
            }

            if (_timerRoot.gameObject.activeSelf)
            {
                int t = Mathf.CeilToInt(_lm.ArenaTimeLeft);
                if (t != _shownTimer)
                {
                    _shownTimer = t;
                    _timer.text = NumberCache.Get(Mathf.Max(0, t));
                    _timer.color = t <= 10 ? UITheme.Danger : Color.white;
                    if (t <= 10) Tween.Punch(_timerRoot, 0.15f, 0.2f, true);
                }
            }
            _wave.text = _lm.WaveLabel;

            var boss = _lm.Boss;
            if (boss != null && _bossRoot.gameObject.activeSelf)
                _boss.fillAmount = Mathf.Lerp(_boss.fillAmount, boss.Health.HealthFraction, 1f - Mathf.Exp(-10f * udt));

            for (int i = 0; i < _notes.Count; i++)
            {
                if (!_notes[i].enabled) continue;
                _noteTimers[i] -= udt;
                var rt = _notes[i].rectTransform;
                rt.anchoredPosition += new Vector2(0f, 60f * udt);
                float a = Mathf.Clamp01(_noteTimers[i] / 0.35f);
                var c = _notes[i].color;
                c.a = a;
                _notes[i].color = c;
                if (_noteTimers[i] <= 0f) _notes[i].enabled = false;
            }
        }
    }
}
