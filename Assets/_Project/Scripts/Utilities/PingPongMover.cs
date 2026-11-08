using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Moves back and forth between two local positions (moving platforms, background machinery).</summary>
    public class PingPongMover : MonoBehaviour
    {
        public Vector3 offset = new Vector3(2f, 0f, 0f);
        public float period = 2f;
        public float phase;
        Vector3 _base;
        RectTransform _rect;

        void Awake()
        {
            _rect = transform as RectTransform;
            _base = _rect != null ? (Vector3)_rect.anchoredPosition : transform.localPosition;
        }

        void Update()
        {
            float s = Mathf.Sin((Time.time / Mathf.Max(0.05f, period)) * 6.2832f + phase);
            if (_rect != null) _rect.anchoredPosition = (Vector2)(_base + offset * s);
            else transform.localPosition = _base + offset * s;
        }
    }
}
