using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>LEVEL COMPLETED screen: reward breakdown with counting coins, kills, final weapon, CONTINUE.</summary>
    public class VictoryUI : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _panel;
        LevelManager _lm;
        Text _total, _rows, _weapon, _kills, _title;
        Image _rays;

        public static VictoryUI Build(Transform parent, LevelManager lm)
        {
            var host = UIFactory.Rect(parent, "Victory");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<VictoryUI>();
            ui._lm = lm;
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void Construct(RectTransform host)
        {
            _panel = UIFactory.Modal(host, "Modal", new Vector2(900f, 1500f), out _group);
            _rays = UIFactory.Panel(_panel, "Glow", new Color(1f, 0.8f, 0.2f, 0.25f), false);
            _rays.sprite = ProceduralSprites.Glow;
            UIFactory.Place(_rays.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 700f), new Vector2(0f, -120f));
            _rays.transform.SetAsFirstSibling();
            _rays.gameObject.AddComponent<Pulser>().amount = 0.06f;

            _title = UIFactory.Label(_panel, "LEVEL\nCOMPLETED!", 96, UITheme.Gold);
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(860f, 260f), new Vector2(0f, -170f));
            for (int i = 0; i < 3; i++)
            {
                var star = UIFactory.Icon(_panel, Icon.Star, UITheme.Gold, i == 1 ? 130f : 100f);
                UIFactory.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(i == 1 ? 130f : 100f, i == 1 ? 130f : 100f), new Vector2((i - 1) * 140f, i == 1 ? -370f : -385f));
                star.gameObject.AddComponent<Pulser>().amount = 0.05f;
            }

            _rows = UIFactory.Label(_panel, "", 38, Color.white, TextAnchor.UpperLeft, true, false);
            UIFactory.Place(_rows.rectTransform, new Vector2(0.5f, 1f), new Vector2(720f, 420f), new Vector2(0f, -680f));
            _rows.lineSpacing = 1.15f;

            var totalBg = UIFactory.Panel(_panel, "TotalBg", new Color(0f, 0f, 0f, 0.4f));
            UIFactory.Place(totalBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(640f, 130f), new Vector2(0f, 470f));
            var coin = UIFactory.Icon(totalBg.rectTransform, Icon.Coin, Color.white, 96f);
            UIFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(96f, 96f), new Vector2(80f, 0f));
            _total = UIFactory.Label(totalBg.rectTransform, "+0", 78, UITheme.Gold);
            UIFactory.Stretch(_total.rectTransform, 140f, 20f, 0, 0);

            _kills = UIFactory.Label(_panel, "", 36, UITheme.TextMuted);
            UIFactory.Place(_kills.rectTransform, new Vector2(0.5f, 0f), new Vector2(820f, 50f), new Vector2(0f, 380f));
            _weapon = UIFactory.Label(_panel, "", 40, Color.white);
            UIFactory.Place(_weapon.rectTransform, new Vector2(0.5f, 0f), new Vector2(820f, 56f), new Vector2(0f, 325f));

            var cont = UIFactory.Button(_panel, "CONTINUE", UITheme.Primary, new Vector2(620f, 170f), () => _lm.NextLevel(), Icon.Play, 62);
            UIFactory.Place((RectTransform)cont.transform, new Vector2(0.5f, 0f), new Vector2(620f, 170f), new Vector2(0f, 175f));
            var menu = UIFactory.Button(_panel, "MENU", UITheme.Grey, new Vector2(300f, 100f), () => _lm.QuitToMenu(), Icon.Home, 40);
            UIFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(300f, 100f), new Vector2(0f, 55f));
        }

        public void Show(RewardBreakdown r, int enemies, int totalEnemies, string weaponName, int tier, bool bossLevel)
        {
            _title.text = bossLevel ? "BOSS\nDEFEATED!" : "LEVEL\nCOMPLETED!";
            string rows = "";
            rows += Row("Level completed", r.completion);
            rows += Row("Enemies defeated", r.enemies);
            if (r.boss > 0) rows += Row("Boss bonus", r.boss);
            rows += Row("Weapon strength", r.weaponStrength);
            rows += Row("Coins collected", r.collected);
            if (r.bonusObjective > 0) rows += Row("<color=#9CFF9C>FLAWLESS (no damage)</color>", r.bonusObjective);
            if (r.multiplier > 1.001f) rows += "<color=#FFD54F>Coin bonus  x" + r.multiplier.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + "</color>\n";
            _rows.text = rows;
            _kills.text = "Enemies defeated: " + enemies + " / " + totalEnemies;
            _weapon.text = "Final weapon: " + weaponName + "  " + MainMenuUI.Stars(tier);
            UIFactory.Show(gameObject, _group, _panel);

            int total = r.total;
            _total.text = "+0";
            Tween.Run(_total, 1.1f, p =>
            {
                _total.text = "+" + Mathf.RoundToInt(total * p);
                if (Random.value < 0.25f) AudioManager.Play(SfxId.Coin, 0.35f);
            }, Ease.OutCubic, () => Tween.Punch(_total.rectTransform, 0.2f, 0.3f, true), 0.35f, true);
        }

        static string Row(string label, int value)
        {
            return label + "  <color=#FFD54F>+" + value + "</color>\n";
        }
    }
}
