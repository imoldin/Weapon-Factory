using UnityEngine;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>Pooled enemy shot. The player can dodge it by moving sideways.</summary>
    public class EnemyProjectile : MonoBehaviour
    {
        CombatManager _owner;
        Vector3 _velocity;
        float _damage, _life, _radius;
        MeshRenderer _renderer;

        public static EnemyProjectile Create(Transform parent)
        {
            var go = Shapes.Sphere(parent, Vector3.zero, 1f, GameAssets.Glow(new Color(1f, 0.2f, 0.4f), 2.5f), "EnemyShot");
            var p = go.AddComponent<EnemyProjectile>();
            p._renderer = go.GetComponent<MeshRenderer>();
            go.AddComponent<Pulser>().amount = 0.15f;
            return p;
        }

        public void Launch(CombatManager owner, Vector3 pos, Vector3 velocity, float damage, float size, Color color)
        {
            _owner = owner;
            transform.position = pos;
            transform.localScale = Vector3.one * size;
            _velocity = velocity;
            _damage = damage;
            _radius = size * 0.5f + 0.45f;
            _life = 6f;
            _renderer.sharedMaterial = GameAssets.Glow(color, 2.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += _velocity * dt;
            _life -= dt;
            var player = PlayerController.Instance;
            if (player != null && player.IsAlive)
            {
                Vector3 d = player.HitCenter - transform.position;
                if (d.sqrMagnitude < _radius * _radius)
                {
                    player.TakeHit(_damage, "Shot down by robots", false);
                    VFX.Play(VFXType.Sparks, transform.position, new Color(1f, 0.3f, 0.4f), 10);
                    _owner.ReleaseEnemyProjectile(this);
                    return;
                }
                // passed behind the player
                if (transform.position.z < player.transform.position.z - 3f) _life = 0f;
            }
            if (_life <= 0f) _owner.ReleaseEnemyProjectile(this);
        }
    }
}
