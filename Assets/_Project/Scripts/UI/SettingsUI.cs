using System;
using UnityEngine;
using UnityEngine.UI;
using WeaponFactory.Core;

namespace WeaponFactory.UI
{
    /// <summary>
    /// Settings modal shared by the main menu and the pause menu: music / SFX volume, mute,
    /// vibration, screen shake, graphics quality, plus development-only tools (reset save, coins).
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _panel;
        Slider _music, _sfx;
        Button _mute, _vibration, _shake, _quality, _reset;
        Action _onClose;
        bool _resetArmed;

        public static SettingsUI Build(Transform parent)
        {
            var host = UIFactory.Rect(parent, "Settings");
            UIFactory.Stretch(host);
            var ui = host.gameObject.AddComponent<SettingsUI>();
            ui.Construct(host);
            host.gameObject.SetActive(false);
            return ui;
        }

        void Construct(RectTransform host)
        {
            bool dev = Debug.isDebugBuild || Application.isEditor;
            float h = dev ? 1500f : 1260f;
            _panel = UIFactory.Modal(host, "Modal", new Vector2(900f, h), out _group);
            var title = UIFactory.Label(_panel, "SETTINGS", 80, Color.white);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 120f), new Vector2(0f, -90f));

            var save = GameManager.Instance.Save;
            float y = -230f;
            _music = SliderRow("MUSIC", y, save.musicVolume, v => { GameManager.Instance.Save.musicVolume = v; GameManager.Instance.ApplySettings(); });
            y -= 150f;
            _sfx = SliderRow("SOUND FX", y, save.sfxVolume, v => { GameManager.Instance.Save.sfxVolume = v; GameManager.Instance.ApplySettings(); });
            y -= 150f;
            _mute = ToggleRow("MUTE ALL", y, () => { var s = GameManager.Instance.Save; s.muted = !s.muted; Changed(); });
            y -= 130f;
            _vibration = ToggleRow("VIBRATION", y, () => { var s = GameManager.Instance.Save; s.vibration = !s.vibration; Changed(); });
            y -= 130f;
            _shake = ToggleRow("SCREEN SHAKE", y, () => { var s = GameManager.Instance.Save; s.screenShake = !s.screenShake; Changed(); });
            y -= 130f;
            _quality = ToggleRow("GRAPHICS", y, () => { var s = GameManager.Instance.Save; s.highQuality = !s.highQuality; Changed(); });
            y -= 150f;

            if (dev)
            {
                var devLabel = UIFactory.Label(_panel, "DEVELOPER", 34, UITheme.TextMuted);
                UIFactory.Place(devLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 50f), new Vector2(0f, y + 20f));
                _reset = UIFactory.Button(_panel, "RESET SAVE", UITheme.Danger, new Vector2(380f, 110f), OnReset, null, 38);
                UIFactory.Place((RectTransform)_reset.transform, new Vector2(0.5f, 1f), new Vector2(380f, 110f), new Vector2(-200f, y - 50f));
                var coins = UIFactory.Button(_panel, "+1000 COINS", UITheme.Purple, new Vector2(380f, 110f), () => GameManager.Instance.AddCoins(1000), null, 38);
                UIFactory.Place((RectTransform)coins.transform, new Vector2(0.5f, 1f), new Vector2(380f, 110f), new Vector2(200f, y - 50f));
            }

            var close = UIFactory.Button(_panel, "CLOSE", UITheme.Primary, new Vector2(520f, 140f), Close, Icon.Check);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(520f, 140f), new Vector2(0f, 110f));
            Refresh();
        }

        Slider SliderRow(string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            var t = UIFactory.Label(_panel, label, 46, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(340f, 80f), new Vector2(-240f, y));
            var s = UIFactory.Slider(_panel, 420f, value, onChange, UITheme.Blue);
            UIFactory.Place((RectTransform)s.transform, new Vector2(0.5f, 1f), new Vector2(420f, 70f), new Vector2(170f, y));
            return s;
        }

        Button ToggleRow(string label, float y, UnityEngine.Events.UnityAction onClick)
        {
            var t = UIFactory.Label(_panel, label, 46, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(420f, 80f), new Vector2(-200f, y));
            var b = UIFactory.Button(_panel, "ON", UITheme.Primary, new Vector2(260f, 100f), onClick, null, 42);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(260f, 100f), new Vector2(240f, y));
            return b;
        }

        void Changed()
        {
            GameManager.Instance.SaveSettings();
            Refresh();
        }

        void SetToggle(Button b, bool on, string onText = "ON", string offText = "OFF")
        {
            UIFactory.ButtonText(b).text = on ? onText : offText;
            UIFactory.SetButtonColor(b, on ? UITheme.Primary : UITheme.Grey);
        }

        void Refresh()
        {
            var s = GameManager.Instance.Save;
            SetToggle(_mute, s.muted, "MUTED", "OFF");
            if (s.muted) UIFactory.SetButtonColor(_mute, UITheme.Danger);
            SetToggle(_vibration, s.vibration);
            SetToggle(_shake, s.screenShake);
            SetToggle(_quality, s.highQuality, "HIGH", "LOW");
            if (_music != null) _music.SetValueWithoutNotify(s.musicVolume);
            if (_sfx != null) _sfx.SetValueWithoutNotify(s.sfxVolume);
            if (_reset != null) UIFactory.ButtonText(_reset).text = _resetArmed ? "TAP TO CONFIRM" : "RESET SAVE";
        }

        void OnReset()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                Refresh();
                return;
            }
            _resetArmed = false;
            GameManager.Instance.ResetSave();
            Refresh();
            GameManager.Instance.GoToMenu();
        }

        public void Open(Action onClose = null)
        {
            _onClose = onClose;
            _resetArmed = false;
            Refresh();
            UIFactory.Show(gameObject, _group, _panel);
        }

        public void Close()
        {
            GameManager.Instance.SaveSettings();
            UIFactory.Hide(gameObject, _group, _onClose);
        }
    }
}
