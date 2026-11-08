using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>Ground shockwave sweeping toward the player with one safe gap.</summary>
    public class BossShockwave : MonoBehaviour
    {
        float _speed, _damage, _gapX, _life = 6f;
        const float GapHalf = 1.4f;
        bool _hit;

        public static void Spawn(Transform parent, float z, float gapX, float speed, float damage)
        {
            var go = new GameObject("Shockwave");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, z);
            var w = go.AddComponent<BossShockwave>();
            w._speed = speed;
            w._damage = damage;
            w._gapX = gapX;
            var mat = GameAssets.Glow(new Color(1f, 0.25f, 0.7f), 2.5f);
            float half = 4.6f;
            float l = gapX - GapHalf, r = gapX + GapHalf;
            if (l > -half) Shapes.Box(go.transform, new Vector3((-half + l) * 0.5f, 0.35f, 0f), new Vector3(l + half, 0.7f, 0.35f), mat, "WaveL");
            if (r < half) Shapes.Box(go.transform, new Vector3((r + half) * 0.5f, 0.35f, 0f), new Vector3(half - r, 0.7f, 0.35f), mat, "WaveR");
            Shapes.Box(go.transform, new Vector3(gapX, 0.03f, 0f), new Vector3(GapHalf * 2f, 0.04f, 0.6f), GameAssets.Glow(new Color(0.2f, 1f, 0.5f), 1.5f), "SafeGap");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += Vector3.back * _speed * dt;
            _life -= dt;
            var player = PlayerController.Instance;
            if (!_hit && player != null && player.IsAlive)
            {
                float dz = Mathf.Abs(transform.position.z - player.transform.position.z);
                if (dz < 0.5f)
                {
                    float px = player.transform.position.x;
                    if (px < _gapX - GapHalf + 0.3f || px > _gapX + GapHalf - 0.3f)
                    {
                        _hit = true;
                        player.TakeHit(_damage, "Crushed by the boss's shockwave", true);
                    }
                }
            }
            if (_life <= 0f || (player != null && transform.position.z < player.transform.position.z - 4f)) Destroy(gameObject);
        }
    }
}
