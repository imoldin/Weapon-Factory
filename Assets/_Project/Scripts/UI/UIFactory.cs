using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaponFactory.Audio;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>Shared colours of the mobile UI.</summary>
    public static class UITheme
    {
        public static readonly Color Panel = new Color(0.09f, 0.11f, 0.17f, 0.94f);
        public static readonly Color PanelLight = new Color(0.16f, 0.19f, 0.28f, 0.96f);
        public static readonly Color Overlay = new Color(0.02f, 0.03f, 0.06f, 0.72f);
        public static readonly Color Primary = new Color(0.18f, 0.8f, 0.36f);
        public static readonly Color Accent = new Color(1f, 0.7f, 0.1f);
        public static readonly Color Blue = new Color(0.2f, 0.6f, 1f);
        public static readonly Color Purple = new Color(0.58f, 0.35f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.26f, 0.3f);
        public static readonly Color Grey = new Color(0.35f, 0.38f, 0.45f);
        public static readonly Color Text = Color.white;
        public static readonly Color TextMuted = new Color(0.72f, 0.76f, 0.85f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
    }

    /// <summary>
    /// Code-built uGUI: canvases with CanvasScaler (1080x1920 reference, portrait) and safe area,
    /// rounded panels, outlined text, juicy buttons, progress bars and sliders.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        public static void ClearCaches() { }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        /// <summary>Creates an overlay canvas; returns the safe-area root to put content into.</summary>
        public static RectTransform CreateCanvas(string name, int sortingOrder, out Canvas canvas)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.35f; // favour width on tall phones
            go.AddComponent<AdaptiveCanvasScaler>(); // switches to height matching on wide screens
            go.AddComponent<GraphicRaycaster>();
            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(go.transform, false);
            var rt = (RectTransform)safe.transform;
            Stretch(rt);
            safe.AddComponent<SafeArea>();
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors at a normalised point of the parent with a fixed size.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = ProceduralSprites.Rounded;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Icon(Transform parent, Icon icon, Color color, float size)
        {
            var rt = Rect(parent, "Icon_" + icon);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralSprites.GetIcon(icon);
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter,
            bool bold = true, bool outline = true)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GameAssets.UIFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.55f);
                o.effectDistance = new Vector2(2.5f, -2.5f);
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.35f);
                s.effectDistance = new Vector2(0f, -5f);
            }
            return t;
        }

        /// <summary>Large rounded button with optional icon, press animation and click sound.</summary>
        public static Button Button(Transform parent, string label, Color color, Vector2 size, UnityAction onClick,
            Icon? icon = null, int fontSize = 52)
        {
            var rt = Rect(parent, "Button_" + label);
            rt.sizeDelta = size;
            // drop shadow
            var shadow = Panel(rt, "Shadow", new Color(0f, 0f, 0f, 0.35f));
            Stretch(shadow.rectTransform, 0, 0, 10, -10);
            // darker bottom edge (3D look)
            var edge = Panel(rt, "Edge", Color.Lerp(color, Color.black, 0.35f));
            Stretch(edge.rectTransform, 0, 0, 6, -6);
            var face = Panel(rt, "Face", color, true, true);
            Stretch(face.rectTransform, 0, 0, 0, 4);
            // highlight
            var hi = Panel(face.rectTransform, "Highlight", new Color(1f, 1f, 1f, 0.16f));
            hi.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            hi.rectTransform.anchorMax = new Vector2(1f, 1f);
            hi.rectTransform.offsetMin = new Vector2(10f, 0f);
            hi.rectTransform.offsetMax = new Vector2(-10f, -8f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<ButtonFeedback>();

            var content = Rect(face.rectTransform, "Content");
            Stretch(content);
            float iconSize = Mathf.Min(size.y * 0.5f, 90f);
            if (icon.HasValue && !string.IsNullOrEmpty(label))
            {
                var ic = Icon(content, icon.Value, Color.white, iconSize);
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(iconSize, iconSize), new Vector2(size.y * 0.42f + 10f, 2f));
                var t = Label(content, label, fontSize, Color.white);
                Stretch(t.rectTransform, size.y * 0.42f + iconSize * 0.5f, 16f, 0, 0);
            }
            else if (icon.HasValue)
            {
                var ic = Icon(content, icon.Value, Color.white, iconSize);
                Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(iconSize, iconSize), new Vector2(0f, 2f));
            }
            else
            {
                var t = Label(content, label, fontSize, Color.white);
                Stretch(t.rectTransform, 12f, 12f, 0, 0);
            }
            return btn;
        }

        public static Text ButtonText(Button b)
        {
            return b.GetComponentInChildren<Text>(true);
        }

        public static void SetButtonColor(Button b, Color color)
        {
            var face = b.targetGraphic as Image;
            if (face != null) face.color = color;
            var edge = b.transform.Find("Edge");
            if (edge != null) edge.GetComponent<Image>().color = Color.Lerp(color, Color.black, 0.35f);
        }

        /// <summary>Horizontal progress bar. Returns the fill image (set fillAmount 0..1).</summary>
        public static Image ProgressBar(Transform parent, string name, Vector2 size, Color back, Color fill)
        {
            var bg = Panel(parent, name, back);
            bg.rectTransform.sizeDelta = size;
            var f = Panel(bg.rectTransform, "Fill", fill, false);
            Stretch(f.rectTransform, 6, 6, 6, 6);
            f.sprite = ProceduralSprites.RoundedSmall;
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillOrigin = (int)Image.OriginHorizontal.Left;
            f.fillAmount = 1f;
            return f;
        }

        /// <summary>Settings slider (0..1).</summary>
        public static Slider Slider(Transform parent, float width, float value, UnityAction<float> onChange, Color fillColor)
        {
            var rt = Rect(parent, "Slider");
            rt.sizeDelta = new Vector2(width, 70f);
            var slider = rt.gameObject.AddComponent<Slider>();
            var bg = Panel(rt, "Background", new Color(0f, 0f, 0f, 0.45f), true, true);
            Stretch(bg.rectTransform, 0, 0, 22, 22);
            var fillArea = Rect(rt, "FillArea");
            Stretch(fillArea, 10, 10, 22, 22);
            var fill = Panel(fillArea, "Fill", fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Rect(rt, "HandleArea");
            Stretch(handleArea, 30, 30, 0, 0);
            var handle = Panel(handleArea, "Handle", Color.white, false, true);
            handle.sprite = ProceduralSprites.CircleSprite;
            handle.rectTransform.sizeDelta = new Vector2(64f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        /// <summary>Full-screen dimmed overlay with a centred rounded panel; returns the panel.</summary>
        public static RectTransform Modal(Transform parent, string name, Vector2 panelSize, out CanvasGroup group)
        {
            var root = Rect(parent, name);
            Stretch(root, -400, -400, -400, -400); // extend under notches
            group = root.gameObject.AddComponent<CanvasGroup>();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = UITheme.Overlay;
            dim.raycastTarget = true;
            var panel = Panel(root, "Panel", UITheme.Panel, true, true);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), panelSize, Vector2.zero);
            var border = Panel(panel.rectTransform, "Border", new Color(1f, 1f, 1f, 0.06f));
            Stretch(border.rectTransform, -4, -4, -4, -4);
            border.transform.SetAsFirstSibling();
            return panel.rectTransform;
        }

        /// <summary>Animated show for screens (fade + pop).</summary>
        public static void Show(GameObject screen, CanvasGroup group, RectTransform pop)
        {
            screen.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = true;
                group.blocksRaycasts = true;
                Tween.Fade(group, 1f, 0.2f);
            }
            if (pop != null)
            {
                pop.localScale = Vector3.one * 0.85f;
                Tween.Scale(pop, Vector3.one, 0.35f, Ease.OutBack, 0f, true);
            }
        }

        public static void Hide(GameObject screen, CanvasGroup group, Action done = null)
        {
            if (group == null) { screen.SetActive(false); if (done != null) done(); return; }
            group.interactable = false;
            group.blocksRaycasts = false;
            Tween.Fade(group, 0f, 0.15f, 0f, () => { screen.SetActive(false); if (done != null) done(); });
        }

        /// <summary>Coin counter pill (coin icon + number). Returns the text.</summary>
        public static Text CoinPill(Transform parent, out RectTransform pill)
        {
            var bg = Panel(parent, "CoinPill", new Color(0f, 0f, 0f, 0.5f));
            pill = bg.rectTransform;
            pill.sizeDelta = new Vector2(300f, 96f);
            var ic = Icon(pill, WeaponFactory.UI.Icon.Coin, Color.white, 76f);
            Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(76f, 76f), new Vector2(52f, 0f));
            var t = Label(pill, "0", 52, UITheme.Gold, TextAnchor.MiddleRight);
            Stretch(t.rectTransform, 96f, 26f, 0, 0);
            return t;
        }
    }
}
