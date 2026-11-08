using UnityEngine;
using UnityEngine.EventSystems;

namespace WeaponFactory.Player
{
    /// <summary>
    /// One-finger drag input. Touch on mobile, mouse drag on PC, A/D or arrow keys for testing.
    /// Produces a horizontal delta normalised to screen width (resolution independent).
    /// Drags that start on a UI element (e.g. the pause button) are ignored.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        /// <summary>Horizontal drag this frame, as a fraction of screen width.</summary>
        public float DragDelta { get; private set; }
        /// <summary>Keyboard axis -1..1.</summary>
        public float KeyAxis { get; private set; }
        /// <summary>True on the frame a press/touch begins outside the UI.</summary>
        public bool PressedThisFrame { get; private set; }
        public bool IsHolding { get; private set; }

        bool _dragging;
        int _fingerId = -1;
        Vector2 _last;

        void Update()
        {
            DragDelta = 0f;
            PressedThisFrame = false;
            float width = Mathf.Max(1f, Screen.width);

            if (Input.touchCount > 0)
            {
                HandleTouches(width);
            }
            else
            {
                if (_fingerId >= 0) { _fingerId = -1; _dragging = false; }
                HandleMouse(width);
            }

            float k = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k += 1f;
            KeyAxis = k;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                PressedThisFrame = true;
            IsHolding = _dragging || k != 0f;
        }

        void HandleTouches(float width)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (_fingerId < 0 && t.phase == TouchPhase.Began)
                {
                    if (IsOverUI(t.fingerId)) continue;
                    _fingerId = t.fingerId;
                    _last = t.position;
                    _dragging = true;
                    PressedThisFrame = true;
                }
                else if (t.fingerId == _fingerId)
                {
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        DragDelta += (t.position.x - _last.x) / width;
                        _last = t.position;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        DragDelta += (t.position.x - _last.x) / width;
                        _fingerId = -1;
                        _dragging = false;
                    }
                }
            }
        }

        void HandleMouse(float width)
        {
            Vector2 pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsOverUI(-1))
                {
                    _dragging = true;
                    _last = pos;
                    PressedThisFrame = true;
                }
            }
            else if (_dragging && Input.GetMouseButton(0))
            {
                DragDelta += (pos.x - _last.x) / width;
                _last = pos;
            }
            if (Input.GetMouseButtonUp(0)) _dragging = false;
        }

        static bool IsOverUI(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return pointerId >= 0 ? es.IsPointerOverGameObject(pointerId) : es.IsPointerOverGameObject();
        }
    }
}
