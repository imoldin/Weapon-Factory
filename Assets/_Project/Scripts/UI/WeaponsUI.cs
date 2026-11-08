using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Core;
using WeaponFactory.Economy;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>
    /// WEAPONS screen (bottom sheet so the 3D preview stays visible): the 8-stage evolution gallery
    /// (tiers unlock when first reached in a run) and purchasable weapon finishes (skins).
    /// </summary>
    public class WeaponsUI : MonoBehaviour
    {
        class TierCard { public int tier; public Image bg; public Text name; public Image lockIcon; public Button button; }
        class SkinCard { public string id; public Button button; public Text label; public Image swatch; }

        CanvasGroup _group;
        RectTransform _sheet;
        Text _coins, _info, _previewName;
        RectTransform _coinPill;
        readonly List<TierCard> _tiers = new List<TierCard>();
        readonly List<SkinCard> _skins = new List<SkinCard>();
        MainMenuBootstrap _menu;
        Action _onClose;
        int _selectedTier = 1;

        public static WeaponsUI Build(Transform parent, MainMenuBootstrap menu, Action onClose)
        {
            var host = UIFactory.Rect(parent, "Weapons");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<WeaponsUI>();
            ui._menu = menu;
            ui._onClose = onClose;
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void OnEnable() { GameEvents.CoinsChanged += OnCoins; }
        void OnDisable() { GameEvents.CoinsChanged -= OnCoins; }

        void Construct(RectTransform host)
        {
            _group = host.gameObject.AddComponent<CanvasGroup>();
            // title over the 3D preview
            var title = UIFactory.Label(host, "WEAPONS", 84, UITheme.Blue);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 120f), new Vector2(0f, -110f));
            _coins = UIFactory.CoinPill(host, out _coinPill);
            UIFactory.Place(_coinPill, new Vector2(1f, 1f), new Vector2(300f, 96f), new Vector2(-40f, -40f), new Vector2(1f, 1f));
            var nameBg = UIFactory.Panel(host, "PreviewName", new Color(0f, 0f, 0f, 0.5f));
            UIFactory.Place(nameBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(700f, 90f), new Vector2(0f, 1230f));
            _previewName = UIFactory.Label(nameBg.rectTransform, "", 44, Color.white);
            UIFactory.Stretch(_previewName.rectTransform);

            var sheetImg = UIFactory.Panel(host, "Sheet", UITheme.Panel, true, true);
            _sheet = sheetImg.rectTransform;
            _sheet.anchorMin = new Vector2(0f, 0f);
            _sheet.anchorMax = new Vector2(1f, 0f);
            _sheet.pivot = new Vector2(0.5f, 0f);
            _sheet.sizeDelta = new Vector2(-40f, 1170f);
            _sheet.anchoredPosition = new Vector2(0f, 20f);

            var evo = UIFactory.Label(_sheet, "EVOLUTION", 44, Color.white);
            UIFactory.Place(evo.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 70f), new Vector2(0f, -55f));

            // 4 x 2 tier grid
            for (int i = 0; i < WeaponEvolution.MaxTier; i++)
            {
                int tier = i + 1;
                int col = i % 4, row = i / 4;
                var card = new TierCard { tier = tier };
                card.button = UIFactory.Button(_sheet, "", UITheme.PanelLight, new Vector2(230f, 170f), () => SelectTier(tier), null, 30);
                UIFactory.Place((RectTransform)card.button.transform, new Vector2(0.5f, 1f), new Vector2(230f, 170f), new Vector2(-375f + col * 250f, -190f - row * 190f));
                card.bg = (Image)card.button.targetGraphic;
                var face = card.button.transform.Find("Face");
                var num = UIFactory.Label(face, NumberCache.Get(tier), 64, new Color(1f, 1f, 1f, 0.9f));
                UIFactory.Place(num.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, 80f), new Vector2(0f, -50f));
                card.name = UIFactory.Label(face, "", 24, Color.white);
                UIFactory.Place(card.name.rectTransform, new Vector2(0.5f, 0f), new Vector2(220f, 60f), new Vector2(0f, 38f));
                card.lockIcon = UIFactory.Icon(face, Icon.Lock, new Color(1f, 1f, 1f, 0.85f), 60f);
                UIFactory.Place(card.lockIcon.rectTransform, new Vector2(1f, 1f), new Vector2(46f, 46f), new Vector2(-30f, -30f));
                _tiers.Add(card);
            }

            _info = UIFactory.Label(_sheet, "", 30, UITheme.TextMuted, TextAnchor.MiddleCenter, false, false);
            UIFactory.Place(_info.rectTransform, new Vector2(0.5f, 1f), new Vector2(920f, 80f), new Vector2(0f, -560f));

            var fin = UIFactory.Label(_sheet, "FINISHES", 44, Color.white);
            UIFactory.Place(fin.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 70f), new Vector2(0f, -640f));
            for (int i = 0; i < SkinCatalog.All.Count; i++)
            {
                var skin = SkinCatalog.All[i];
                var sc = new SkinCard { id = skin.id };
                string id = skin.id;
                sc.button = UIFactory.Button(_sheet, "", UITheme.PanelLight, new Vector2(230f, 210f), () => OnSkin(id), null, 28);
                UIFactory.Place((RectTransform)sc.button.transform, new Vector2(0.5f, 1f), new Vector2(230f, 210f), new Vector2(-375f + i * 250f, -790f));
                var face = sc.button.transform.Find("Face");
                sc.swatch = UIFactory.Panel(face, "Swatch", GameAssets.Hex(skin.bodyHex));
                UIFactory.Place(sc.swatch.rectTransform, new Vector2(0.5f, 1f), new Vector2(120f, 80f), new Vector2(0f, -55f));
                var glow = UIFactory.Panel(sc.swatch.rectTransform, "Glow", GameAssets.Hex(skin.glowHex));
                UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120f, 18f), Vector2.zero);
                var n = UIFactory.Label(face, skin.displayName, 24, Color.white);
                UIFactory.Place(n.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220f, 40f), new Vector2(0f, -15f));
                sc.label = UIFactory.Label(face, "", 28, UITheme.Gold);
                UIFactory.Place(sc.label.rectTransform, new Vector2(0.5f, 0f), new Vector2(220f, 50f), new Vector2(0f, 36f));
                _skins.Add(sc);
            }

            var back = UIFactory.Button(_sheet, "BACK", UITheme.Grey, new Vector2(480f, 130f), Close, Icon.Home, 50);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(480f, 130f), new Vector2(0f, 95f));
        }

        void SelectTier(int tier)
        {
            var save = GameManager.Instance.Save;
            if (tier > save.highestTierUnlocked)
            {
                _info.text = "Reach the " + WeaponEvolution.GetTier(tier).displayName + " in a run to unlock it!";
                Tween.Punch(_info.rectTransform, 0.1f, 0.25f, true);
                return;
            }
            _selectedTier = tier;
            if (_menu != null && _menu.Preview != null) _menu.Preview.Show(tier, GameManager.Instance.EquippedSkin);
            Refresh();
        }

        void OnSkin(string id)
        {
            var save = GameManager.Instance.Save;
            if (save.ownedSkins.Contains(id)) UpgradeShop.EquipSkin(id);
            else
            {
                var r = UpgradeShop.BuySkin(id);
                if (r != Purchases.Result.Success) { _info.text = "Not enough coins for " + SkinCatalog.Get(id).displayName + "."; return; }
            }
            if (_menu != null && _menu.Preview != null) _menu.Preview.Show(_selectedTier, GameManager.Instance.EquippedSkin);
            Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            var save = gm.Save;
            _coins.text = NumberCache.Compact(gm.Coins);
            foreach (var t in _tiers)
            {
                bool unlocked = t.tier <= save.highestTierUnlocked;
                var def = WeaponEvolution.GetTier(t.tier);
                t.name.text = unlocked ? def.displayName : "???";
                t.lockIcon.enabled = !unlocked;
                Color c = unlocked ? Color.Lerp(UITheme.PanelLight, GameAssets.Hex(def.colorHex), 0.45f) : new Color(0.12f, 0.13f, 0.17f);
                if (t.tier == _selectedTier) c = Color.Lerp(c, Color.white, 0.25f);
                UIFactory.SetButtonColor(t.button, c);
            }
            foreach (var s in _skins)
            {
                var def = SkinCatalog.Get(s.id);
                bool owned = save.ownedSkins.Contains(s.id);
                bool equipped = save.equippedSkin == s.id;
                s.label.text = equipped ? "EQUIPPED" : (owned ? "EQUIP" : NumberCache.Compact(def.price) + " COINS");
                s.label.color = equipped ? UITheme.Primary : (owned ? Color.white : (gm.Coins >= def.price ? UITheme.Gold : UITheme.Danger));
                UIFactory.SetButtonColor(s.button, equipped ? Color.Lerp(UITheme.PanelLight, UITheme.Primary, 0.35f) : UITheme.PanelLight);
            }
            if (_menu != null && _menu.Preview != null)
                _previewName.text = _menu.Preview.ShownName + "   " + MainMenuUI.Stars(_menu.Preview.ShownTier);
            if (string.IsNullOrEmpty(_info.text))
                _info.text = "Unlocked " + save.highestTierUnlocked + "/8. Buy WEAPON LEVEL upgrades to start runs evolved.";
        }

        void OnCoins(int c) { if (gameObject.activeInHierarchy) Refresh(); }

        public void Open()
        {
            _info.text = "";
            if (_menu != null && _menu.Preview != null) _selectedTier = _menu.Preview.ShownTier;
            Refresh();
            gameObject.SetActive(true);
            _group.alpha = 0f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            Tween.Fade(_group, 1f, 0.2f);
            Vector2 target = new Vector2(0f, 20f);
            _sheet.anchoredPosition = new Vector2(0f, -1200f);
            Tween.Anchored(_sheet, target, 0.4f, Ease.OutCubic);
        }

        public void Close()
        {
            UIFactory.Hide(gameObject, _group, _onClose);
        }
    }
}
