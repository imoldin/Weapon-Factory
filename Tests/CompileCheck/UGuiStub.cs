// COMPILE-CHECK ONLY. Minimal signature stub of Unity's uGUI package (com.unity.ugui), mirroring the
// members this project uses (verified against the uGUI source). Never shipped inside the Unity project.
#pragma warning disable 0067, 0649, 0169
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public abstract class AbstractEventData { }
    public class BaseEventData : AbstractEventData { }
    public class PointerEventData : BaseEventData { public Vector2 position { get; set; } }
    public abstract class UIBehaviour : MonoBehaviour { }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public bool IsPointerOverGameObject() { return false; }
        public bool IsPointerOverGameObject(int pointerId) { return false; }
    }
    public abstract class BaseInputModule : UIBehaviour { }
    public abstract class PointerInputModule : BaseInputModule { }
    public class StandaloneInputModule : PointerInputModule { }
    public abstract class BaseRaycaster : UIBehaviour { }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;

    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
    }
    public abstract class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public FillMethod fillMethod { get; set; }
        public int fillOrigin { get; set; }
        public float fillAmount { get; set; }
        public bool preserveAspect { get; set; }
    }

    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public virtual string text { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public FontStyle fontStyle { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public float lineSpacing { get; set; }
    }

    public abstract class BaseMeshEffect : UIBehaviour { }
    public class Shadow : BaseMeshEffect
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
    }
    public class Outline : Shadow { }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color disabledColor { get; set; }
        public float fadeDuration { get; set; }
    }

    public class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public Transition transition { get; set; }
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
        public bool interactable { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; }
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public virtual float value { get; set; }
        public SliderEvent onValueChanged { get; set; }
        public virtual void SetValueWithoutNotify(float input) { }
    }

    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : BaseRaycaster { }
}
