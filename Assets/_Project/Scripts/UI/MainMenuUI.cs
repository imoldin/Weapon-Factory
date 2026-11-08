using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Core;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>
    /// Main menu: logo, coins, current level, weapon name under the 3D preview, PLAY, UPGRADES,
    /// WEAPONS and SETTINGS. Opens the shop / weapons / settings panels.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        MainMenuBootstrap _menu;
        Text _coins, _level, _weaponName, _playSub;
        RectTransform _coinPill, _main;
        CanvasGroup _mainGroup;
        SettingsUI _settings;
        UpgradeShopUI _shop;
        WeaponsUI _weapons;

        public static MainMenuUI Build(MainMenuBootstrap menu)
        {
            Canvas canvas;
            var root = UIFactory.CreateCanvas("MainMenuCanvas", 10, out canvas);
            var ui = canvas.gameObject.AddComponent<MainMenuUI>();
            ui._menu = menu;
            ui.Construct(root);
            return ui;
        }

        void OnEnable() { GameEvents.CoinsChanged += OnCoins; }
        void OnDisable() { GameEvents.CoinsChanged -= OnCoins; }

        void Construct(RectTransform root)
        {
            var gm = GameManager.Instance;
            _main = UIFactory.Rect(root, "Main");
            UIFactory.Stretch(_main);
            _mainGroup = _main.gameObject.AddComponent<CanvasGroup>();

            // ---- top bar ----
            var levelPill = UIFactory.Panel(_main, "LevelPill", new Color(0f, 0f, 0f, 0.5f));
            UIFactory.Place(levelPill.rectTransform, new Vector2(0f, 1f), new Vector2(320f, 96f), new Vector2(40f, -40f), new Vector2(0f, 1f));
            var star = UIFactory.Icon(levelPill.rectTransform, Icon.Star, UITheme.Gold, 64f);
            UIFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 64f), new Vector2(50f, 0f));
            _level = UIFactory.Label(levelPill.rectTransform, "LEVEL 1", 46, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_level.rectTransform, 96f, 16f, 0, 0);

            _coins = UIFactory.CoinPill(_main, out _coinPill);
            UIFactory.Place(_coinPill, new Vector2(1f, 1f), new Vector2(300f, 96f), new Vector2(-40f, -40f), new Vector2(1f, 1f));

            // ---- logo ----
            var logo = UIFactory.Rect(_main, "Logo");
            UIFactory.Place(logo, new Vector2(0.5f, 1f), new Vector2(1000f, 330f), new Vector2(0f, -330f));
            var w = UIFactory.Label(logo, "WEAPON", 168, UITheme.Accent);
            UIFactory.Place(w.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 180f), new Vector2(0f, -80f));
            var f = UIFactory.Label(logo, "FACTORY", 128, Color.white);
            UIFactory.Place(f.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 150f), new Vector2(0f, -220f));
            var g1 = UIFactory.Icon(logo, Icon.Gear, new Color(1f, 0.7f, 0.1f, 0.9f), 110f);
            UIFactory.Place(g1.rectTransform, new Vector2(0f, 1f), new Vector2(110f, 110f), new Vector2(40f, -230f));
            g1.gameObject.AddComponent<Spinner>().axis = Vector3.forward;
            var g2 = UIFactory.Icon(logo, Icon.Gear, new Color(1f, 0.7f, 0.1f, 0.9f), 90f);
            UIFactory.Place(g2.rectTransform, new Vector2(1f, 1f), new Vector2(90f, 90f), new Vector2(-40f, -235f));
            var sp2 = g2.gameObject.AddComponent<Spinner>();
            sp2.axis = Vector3.forward;
            sp2.speed = -120f;
            foreach (var sp in logo.GetComponentsInChildren<Spinner>()) sp.unscaled = true;
            var logoPulse = w.gameObject.AddComponent<Pulser>();
            logoPulse.amount = 0.02f;
            logoPulse.frequency = 0.5f;

            // ---- weapon name under preview ----
            var namePanel = UIFactory.Panel(_main, "WeaponName", new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Place(namePanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(620f, 90f), new Vector2(0f, 720f));
            _weaponName = UIFactory.Label(namePanel.rectTransform, "", 44, Color.white);
            UIFactory.Stretch(_weaponName.rectTransform);

            // ---- PLAY ----
            var playHolder = UIFactory.Rect(_main, "PlayHolder");
            UIFactory.Place(playHolder, new Vector2(0.5f, 0f), new Vector2(680f, 210f), new Vector2(0f, 480f));
            var play = UIFactory.Button(playHolder, "PLAY", UITheme.Primary, new Vector2(680f, 210f), OnPlay, Icon.Play, 96);
            UIFactory.Stretch((RectTransform)play.transform);
            var pulse = playHolder.gameObject.AddComponent<Pulser>();
            pulse.amount = 0.035f;
            pulse.frequency = 1.1f;
            _playSub = UIFactory.Label(_main, "", 36, UITheme.TextMuted);
            UIFactory.Place(_playSub.rectTransform, new Vector2(0.5f, 0f), new Vector2(680f, 60f), new Vector2(0f, 345f));

            // ---- bottom row ----
            var up = UIFactory.Button(_main, "UPGRADES", UITheme.Accent, new Vector2(400f, 160f), OpenShop, Icon.ArrowUp, 44);
            UIFactory.Place((RectTransform)up.transform, new Vector2(0.5f, 0f), new Vector2(400f, 160f), new Vector2(-300f, 170f));
            var wp = UIFactory.Button(_main, "WEAPONS", UITheme.Blue, new Vector2(400f, 160f), OpenWeapons, Icon.Gun, 44);
            UIFactory.Place((RectTransform)wp.transform, new Vector2(0.5f, 0f), new Vector2(400f, 160f), new Vector2(130f, 170f));
            var st = UIFactory.Button(_main, "", UITheme.Grey, new Vector2(160f, 160f), OpenSettings, Icon.Gear);
            UIFactory.Place((RectTransform)st.transform, new Vector2(0.5f, 0f), new Vector2(160f, 160f), new Vector2(430f, 170f));

            // ---- panels ----
            _shop = UpgradeShopUI.Build(root, OnPanelClosed);
            _weapons = WeaponsUI.Build(root, _menu, OnPanelClosed);
            _settings = SettingsUI.Build(root);

            // entrance animation
            logo.localScale = Vector3.one * 0.6f;
            Tween.Scale(logo, Vector3.one, 0.6f, Ease.OutBack, 0.1f, true);
            var playRt = playHolder;
            Vector2 target = playRt.anchoredPosition;
            playRt.anchoredPosition = target + new Vector2(0f, -500f);
            Tween.Anchored(playRt, target, 0.55f, Ease.OutBack, 0.25f);

            Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            int lvl = Mathf.Max(1, gm.Save.currentLevel);
            _level.text = "LEVEL " + lvl;
            _coins.text = NumberCache.Compact(gm.Coins);
            var def = DataRegistry.GetLevel(lvl);
            _playSub.text = def.displayName + (def.isBossLevel ? "  -  BOSS FIGHT" : "");
            if (_menu != null && _menu.Preview != null)
                _weaponName.text = _menu.Preview.ShownName + "   " + Stars(_menu.Preview.ShownTier);
        }

        public static string Stars(int tier)
        {
            return "<color=#FFD54F>" + new string('*', Mathf.Clamp(tier, 1, 8)) + "</color>";
        }

        void OnCoins(int coins)
        {
            if (_coins == null) return;
            _coins.text = NumberCache.Compact(coins);
            Tween.Punch(_coinPill, 0.12f, 0.25f, true);
        }

        void OnPlay()
        {
            var gm = GameManager.Instance;
            gm.PlayLevel(Mathf.Max(1, gm.Save.currentLevel));
        }

        void SetMainVisible(bool visible)
        {
            _mainGroup.interactable = visible;
            _mainGroup.blocksRaycasts = visible;
            Tween.Fade(_mainGroup, visible ? 1f : 0f, 0.15f);
        }

        void OpenShop() { SetMainVisible(false); _shop.Open(); }
        void OpenWeapons() { SetMainVisible(false); _weapons.Open(); }
        void OpenSettings() { SetMainVisible(false); _settings.Open(OnPanelClosed); }

        void OnPanelClosed()
        {
            SetMainVisible(true);
            Refresh();
        }
    }
}
