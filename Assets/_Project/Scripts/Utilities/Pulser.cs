using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Pulsing scale (glows, buttons, arrows).</summary>
    public class Pulser : MonoBehaviour
    {
        public float amount = 0.08f;
        public float frequency = 1.2f;
        public bool unscaled = true;
        Vector3 _base;

        void Awake() { _base = transform.localScale; }

        void Update()
        {
            float t = unscaled ? Time.unscaledTime : Time.time;
            transform.localScale = _base * (1f + Mathf.Sin(t * frequency * 6.2832f) * amount);
        }
    }
}
