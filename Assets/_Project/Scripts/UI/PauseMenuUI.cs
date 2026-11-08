using UnityEngine;
using WeaponFactory.Levels;

namespace WeaponFactory.UI
{
    /// <summary>Pause menu: Resume, Restart, Settings, Main Menu. Works at Time.timeScale = 0.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _panel;
        LevelManager _lm;
        SettingsUI _settings;

        public static PauseMenuUI Build(Transform parent, LevelManager lm, SettingsUI settings)
        {
            var host = UIFactory.Rect(parent, "Pause");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<PauseMenuUI>();
            ui._lm = lm;
            ui._settings = settings;
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void Construct(RectTransform host)
        {
            _panel = UIFactory.Modal(host, "Modal", new Vector2(820f, 1150f), out _group);
            var title = UIFactory.Label(_panel, "PAUSED", 96, Color.white);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 130f), new Vector2(0f, -110f));
            float y = -290f;
            AddButton("RESUME", UITheme.Primary, Icon.Play, ref y, () => _lm.Resume());
            AddButton("RESTART", UITheme.Accent, Icon.Restart, ref y, () => _lm.Restart());
            AddButton("SETTINGS", UITheme.Blue, Icon.Gear, ref y, OpenSettings);
            AddButton("MAIN MENU", UITheme.Grey, Icon.Home, ref y, () => _lm.QuitToMenu());
        }

        void AddButton(string label, Color color, Icon icon, ref float y, UnityEngine.Events.UnityAction action)
        {
            var b = UIFactory.Button(_panel, label, color, new Vector2(620f, 170f), action, icon, 58);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(620f, 170f), new Vector2(0f, y));
            y -= 200f;
        }

        void OpenSettings()
        {
            UIFactory.Hide(gameObject, _group);
            _settings.Open(() => Show());
        }

        public void Show() { UIFactory.Show(gameObject, _group, _panel); }
        public void Hide() { UIFactory.Hide(gameObject, _group); }
    }
}
