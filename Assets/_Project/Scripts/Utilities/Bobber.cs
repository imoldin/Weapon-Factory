using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Sine bob along local Y (collectibles, drones, menu props).</summary>
    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.15f;
        public float frequency = 1.5f;
        public float phase;
        Vector3 _base;

        void Awake() { _base = transform.localPosition; phase += Random.value * 6.28f; }

        public void ResetBase() { _base = transform.localPosition; }

        void Update()
        {
            transform.localPosition = _base + Vector3.up * (Mathf.Sin(Time.time * frequency * 6.2832f + phase) * amplitude);
        }
    }
}
