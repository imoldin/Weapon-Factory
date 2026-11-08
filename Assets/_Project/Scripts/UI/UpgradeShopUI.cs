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
    /// Permanent upgrade shop: one card per upgrade with level pips, current/next benefit,
    /// price and a BUY button that is disabled (and buzzes) without enough coins.
    /// </summary>
    public class UpgradeShopUI : MonoBehaviour
    {
        class Card
        {
            public UpgradeType type;
            public RectTransform root;
            public Text benefit, price;
            public Button buy;
            public List<Image> pips = new List<Image>();
            public Image coinIcon;
        }

        CanvasGroup _group;
        RectTransform _panel;
        Text _coins;
        RectTransform _coinPill;
        readonly List<Card> _cards = new List<Card>();
        Action _onClose;

        public static UpgradeShopUI Build(Transform parent, Action onClose)
        {
            var host = UIFactory.Rect(parent, "UpgradeShop");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<UpgradeShopUI>();
            ui._onClose = onClose;
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void OnEnable() { GameEvents.CoinsChanged += OnCoins; }
        void OnDisable() { GameEvents.CoinsChanged -= OnCoins; }

        void Construct(RectTransform host)
        {
            _panel = UIFactory.Modal(host, "Modal", new Vector2(980f, 1720f), out _group);
            var title = UIFactory.Label(_panel, "UPGRADES", 84, UITheme.Accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 120f), new Vector2(0f, -85f));
            _coins = UIFactory.CoinPill(_panel, out _coinPill);
            UIFactory.Place(_coinPill, new Vector2(0.5f, 1f), new Vector2(300f, 90f), new Vector2(0f, -195f));

            float y = -310f;
            foreach (var def in UpgradeCatalog.Defaults)
            {
                _cards.Add(BuildCard(def, y));
                y -= 205f;
            }

            var close = UIFactory.Button(_panel, "BACK", UITheme.Grey, new Vector2(480f, 130f), Close, Icon.Home, 50);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(480f, 130f), new Vector2(0f, 95f));
        }

        Card BuildCard(UpgradeDefinition def, float y)
        {
            var card = new Card { type = def.type };
            var bg = UIFactory.Panel(_panel, "Card_" + def.type, UITheme.PanelLight);
            card.root = bg.rectTransform;
            UIFactory.Place(card.root, new Vector2(0.5f, 1f), new Vector2(920f, 190f), new Vector2(0f, y - 80f));
            Color c = GameAssets.Hex(def.colorHex);

            var iconBg = UIFactory.Panel(card.root, "IconBg", c);
            UIFactory.Place(iconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(140f, 140f), new Vector2(90f, 0f));
            var icon = UIFactory.Icon(iconBg.rectTransform, IconFor(def.type), Color.white, 96f);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), Vector2.zero);

            var name = UIFactory.Label(card.root, def.displayName, 46, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(420f, 60f), new Vector2(180f, -42f), new Vector2(0f, 0.5f));
            card.benefit = UIFactory.Label(card.root, "", 30, UITheme.TextMuted, TextAnchor.MiddleLeft, false, false);
            UIFactory.Place(card.benefit.rectTransform, new Vector2(0f, 0.5f), new Vector2(440f, 50f), new Vector2(180f, -4f), new Vector2(0f, 0.5f));

            for (int i = 0; i < def.MaxLevel; i++)
            {
                var pip = UIFactory.Panel(card.root, "Pip", new Color(1f, 1f, 1f, 0.15f));
                pip.sprite = ProceduralSprites.RoundedSmall;
                UIFactory.Place(pip.rectTransform, new Vector2(0f, 0f), new Vector2(64f, 22f), new Vector2(180f + i * 76f, 36f), new Vector2(0f, 0.5f));
                card.pips.Add(pip);
            }

            var type = def.type;
            card.buy = UIFactory.Button(card.root, "100", UITheme.Primary, new Vector2(250f, 120f), () => OnBuy(type), null, 46);
            UIFactory.Place((RectTransform)card.buy.transform, new Vector2(1f, 0.5f), new Vector2(250f, 120f), new Vector2(-145f, 0f));
            card.price = UIFactory.ButtonText(card.buy);
            UIFactory.Stretch(card.price.rectTransform, 70f, 14f, 0, 0);
            card.coinIcon = UIFactory.Icon(card.buy.transform.Find("Face"), Icon.Coin, Color.white, 54f);
            UIFactory.Place(card.coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 54f), new Vector2(48f, 2f));
            return card;
        }

        static Icon IconFor(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.StartingDamage: return Icon.Skull;
                case UpgradeType.StartingFireRate: return Icon.Bolt;
                case UpgradeType.StartingMagazine: return Icon.ArrowUp;
                case UpgradeType.StartingWeaponLevel: return Icon.Gun;
                case UpgradeType.CoinBonus: return Icon.Coin;
                default: return Icon.Star;
            }
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            _coins.text = NumberCache.Compact(gm.Coins);
            foreach (var card in _cards)
            {
                var def = UpgradeCatalog.Get(card.type);
                int lvl = UpgradeShop.LevelOf(card.type);
                int price = def.PriceForNextLevel(lvl);
                Color c = GameAssets.Hex(def.colorHex);
                for (int i = 0; i < card.pips.Count; i++) card.pips[i].color = i < lvl ? c : new Color(1f, 1f, 1f, 0.15f);
                if (price < 0)
                {
                    card.benefit.text = def.BenefitText(lvl) + "  (MAX)";
                    card.price.text = "MAX";
                    card.coinIcon.enabled = false;
                    UIFactory.Stretch(card.price.rectTransform, 14f, 14f, 0, 0);
                    card.buy.interactable = false;
                    UIFactory.SetButtonColor(card.buy, UITheme.Grey);
                }
                else
                {
                    card.benefit.text = (lvl > 0 ? def.BenefitText(lvl) + "  >  " : "") + "<color=#9CFF9C>" + def.BenefitText(lvl + 1) + "</color>";
                    card.price.text = NumberCache.Compact(price);
                    card.coinIcon.enabled = true;
                    UIFactory.Stretch(card.price.rectTransform, 70f, 14f, 0, 0);
                    bool afford = gm.Coins >= price;
                    card.buy.interactable = afford;
                    UIFactory.SetButtonColor(card.buy, afford ? UITheme.Primary : UITheme.Grey);
                }
            }
        }

        void OnBuy(UpgradeType type)
        {
            var result = UpgradeShop.Buy(type);
            var card = _cards.Find(c => c.type == type);
            if (result == Purchases.Result.Success)
            {
                Tween.Punch(card.root, 0.08f, 0.3f, true);
                Flash(card.root);
            }
            else if (card != null)
            {
                Shake(card.root);
            }
            Refresh();
        }

        static void Flash(RectTransform rt)
        {
            var flash = UIFactory.Panel(rt, "Flash", new Color(1f, 1f, 1f, 0.6f));
            UIFactory.Stretch(flash.rectTransform);
            Tween.Run(flash, 0.4f, p => flash.color = new Color(1f, 1f, 1f, 0.6f * (1f - p)), Ease.OutQuad, () => Destroy(flash.gameObject), 0f, true);
        }

        static void Shake(RectTransform rt)
        {
            Vector2 basePos = rt.anchoredPosition;
            Tween.Run(rt, 0.3f, p => rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(p * 40f) * 14f * (1f - p), 0f), Ease.Linear,
                () => rt.anchoredPosition = basePos, 0f, true);
        }

        void OnCoins(int coins)
        {
            if (gameObject.activeInHierarchy) Refresh();
        }

        public void Open()
        {
            Refresh();
            UIFactory.Show(gameObject, _group, _panel);
        }

        public void Close()
        {
            UIFactory.Hide(gameObject, _group, _onClose);
        }
    }
}
