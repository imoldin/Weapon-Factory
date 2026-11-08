using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Levels;

namespace WeaponFactory.UI
{
    /// <summary>LEVEL FAILED screen: reason, coins kept, RETRY and MAIN MENU.</summary>
    public class DefeatUI : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _panel;
        LevelManager _lm;
        Text _reason, _coins, _tip;

        public static DefeatUI Build(Transform parent, LevelManager lm)
        {
            var host = UIFactory.Rect(parent, "Defeat");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<DefeatUI>();
            ui._lm = lm;
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void Construct(RectTransform host)
        {
            _panel = UIFactory.Modal(host, "Modal", new Vector2(880f, 1250f), out _group);
            var skull = UIFactory.Icon(_panel, Icon.Skull, UITheme.Danger, 170f);
            UIFactory.Place(skull.rectTransform, new Vector2(0.5f, 1f), new Vector2(170f, 170f), new Vector2(0f, -140f));
            var title = UIFactory.Label(_panel, "LEVEL FAILED", 96, UITheme.Danger);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(840f, 130f), new Vector2(0f, -310f));
            _reason = UIFactory.Label(_panel, "", 44, Color.white);
            UIFactory.Place(_reason.rectTransform, new Vector2(0.5f, 1f), new Vector2(780f, 140f), new Vector2(0f, -440f));
            _coins = UIFactory.Label(_panel, "", 42, UITheme.Gold);
            UIFactory.Place(_coins.rectTransform, new Vector2(0.5f, 1f), new Vector2(780f, 70f), new Vector2(0f, -560f));
            _tip = UIFactory.Label(_panel, "", 34, UITheme.TextMuted, TextAnchor.MiddleCenter, false, false);
            UIFactory.Place(_tip.rectTransform, new Vector2(0.5f, 1f), new Vector2(780f, 120f), new Vector2(0f, -660f));

            var retry = UIFactory.Button(_panel, "RETRY", UITheme.Primary, new Vector2(620f, 170f), () => _lm.Restart(), Icon.Restart, 62);
            UIFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(620f, 170f), new Vector2(0f, 290f));
            var menu = UIFactory.Button(_panel, "MAIN MENU", UITheme.Grey, new Vector2(620f, 140f), () => _lm.QuitToMenu(), Icon.Home, 50);
            UIFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(620f, 140f), new Vector2(0f, 110f));
        }

        static readonly string[] Tips =
        {
            "Tip: blue gates are upgrades, red gates make your weapon weaker.",
            "Tip: shoot barriers down instead of crashing into them.",
            "Tip: buy permanent upgrades in the main menu with your coins.",
            "Tip: during boss shockwaves, move into the green gap!",
            "Tip: WEAPON LEVEL upgrades let you start every run evolved.",
        };

        public void Show(string reason, int coinsKept)
        {
            _reason.text = reason;
            _coins.text = coinsKept > 0 ? "Coins kept: +" + coinsKept : "";
            _tip.text = Tips[Random.Range(0, Tips.Length)];
            UIFactory.Show(gameObject, _group, _panel);
        }
    }
}
