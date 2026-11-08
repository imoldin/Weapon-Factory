using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Scrolls a material's main texture (conveyor belts). Uses its own material instance.</summary>
    public class TextureScroller : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0f, -0.6f);
        Material _mat;

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) _mat = r.sharedMaterial;
        }

        void Update()
        {
            if (_mat == null) return;
            // absolute time: idempotent when several belts share one material
            float t = Time.time % 1000f;
            _mat.mainTextureOffset = new Vector2(Mathf.Repeat(speed.x * t, 1f), Mathf.Repeat(speed.y * t, 1f));
        }
    }
}
