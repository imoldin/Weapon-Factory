using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Toggles a renderer on/off (warning lights).</summary>
    public class Blinker : MonoBehaviour
    {
        public float interval = 0.5f;
        public float phase;
        Renderer _r;

        void Awake() { _r = GetComponent<Renderer>(); }

        void Update()
        {
            if (_r == null) return;
            _r.enabled = Mathf.Repeat(Time.time + phase, interval * 2f) < interval;
        }
    }
}
