using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>
    /// Trauma-based camera shake (shake = trauma²) applied to the transform it sits on,
    /// which should be a child of the camera rig. Gentle limits keep it comfortable,
    /// and it can be disabled in Settings.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        public static bool Enabled = true;
        static ScreenShake _instance;

        public float maxOffset = 0.25f;
        public float maxRoll = 1.5f;
        public float decay = 1.8f;

        float _trauma;
        float _seed;

        void Awake()
        {
            _instance = this;
            _seed = Random.value * 100f;
        }

        void OnDestroy() { if (_instance == this) _instance = null; }

        /// <summary>Adds trauma (0..1). Small hits ~0.1, explosions ~0.3, boss slam ~0.5.</summary>
        public static void Add(float amount)
        {
            if (!Enabled || _instance == null) return;
            _instance._trauma = Mathf.Clamp01(_instance._trauma + amount);
        }

        void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }
            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * 25f;
            float x = (Mathf.PerlinNoise(_seed, t) * 2f - 1f) * maxOffset * shake;
            float y = (Mathf.PerlinNoise(_seed + 1f, t) * 2f - 1f) * maxOffset * shake;
            float r = (Mathf.PerlinNoise(_seed + 2f, t) * 2f - 1f) * maxRoll * shake;
            transform.localPosition = new Vector3(x, y, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, r);
            _trauma = Mathf.Max(0f, _trauma - decay * Time.unscaledDeltaTime);
        }
    }
}
