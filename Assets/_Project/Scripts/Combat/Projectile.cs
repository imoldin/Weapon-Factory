using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Pooled player projectile. Bullets fly straight; explosive shells and rockets deal splash
    /// damage, rockets home in on their target and leave a smoke trail.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        CombatManager _owner;
        Vector3 _dir;
        float _speed, _damage, _splash, _maxDist, _travelled, _trailTimer;
        bool _crit;
        ProjectileKind _kind;
        Damageable _homing;
        GameObject _bullet, _heavy, _shell, _rocket;

        public static Projectile Create(Transform parent)
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<Projectile>();
            var t = go.transform;
            p._bullet = Shapes.Create(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.12f, 0.12f, 0.55f), GameAssets.Glow(new Color(1f, 0.85f, 0.3f), 2.5f), "Bullet");
            p._heavy = Shapes.Create(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.16f, 0.16f, 0.7f), GameAssets.Glow(new Color(1f, 0.55f, 0.15f), 2.5f), "Heavy");
            p._shell = Shapes.Create(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.32f, 0.32f, 0.45f), GameAssets.Glow(new Color(1f, 0.3f, 0.15f), 2.5f), "Shell");
            var rocket = Shapes.Empty(t, "Rocket").transform;
            Shapes.CylinderZ(rocket, Vector3.zero, 0.2f, 0.6f, GameAssets.Lit(new Color(0.85f, 0.85f, 0.9f), 0.6f, 0.5f));
            Shapes.Create(PrimitiveType.Sphere, rocket, new Vector3(0f, 0f, 0.3f), new Vector3(0.2f, 0.2f, 0.3f), GameAssets.Lit(new Color(0.9f, 0.2f, 0.15f)));
            Shapes.Box(rocket, new Vector3(0f, 0f, -0.25f), new Vector3(0.4f, 0.04f, 0.15f), GameAssets.Lit(new Color(0.9f, 0.2f, 0.15f)));
            Shapes.Box(rocket, new Vector3(0f, 0f, -0.25f), new Vector3(0.04f, 0.4f, 0.15f), GameAssets.Lit(new Color(0.9f, 0.2f, 0.15f)));
            Shapes.Sphere(rocket, new Vector3(0f, 0f, -0.38f), 0.2f, GameAssets.Glow(new Color(1f, 0.6f, 0.1f), 3f), "Flame");
            p._rocket = rocket.gameObject;
            return p;
        }

        public void Launch(CombatManager owner, Vector3 pos, Vector3 dir, ProjectileKind kind, float damage, bool crit,
            float splash, float maxDist, Damageable homing)
        {
            _owner = owner;
            transform.position = pos;
            _dir = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
            transform.rotation = Quaternion.LookRotation(_dir);
            _kind = kind;
            _damage = damage;
            _crit = crit;
            _splash = splash;
            _maxDist = maxDist;
            _travelled = 0f;
            _homing = kind == ProjectileKind.Rocket ? homing : null;
            _speed = kind == ProjectileKind.Rocket ? 18f : (kind == ProjectileKind.Explosive ? 34f : 48f);
            _bullet.SetActive(kind == ProjectileKind.Bullet);
            _heavy.SetActive(kind == ProjectileKind.HeavyBullet);
            _shell.SetActive(kind == ProjectileKind.Explosive);
            _rocket.SetActive(kind == ProjectileKind.Rocket);
            if (crit) transform.localScale = Vector3.one * 1.35f; else transform.localScale = Vector3.one;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_kind == ProjectileKind.Rocket)
            {
                _speed = Mathf.Min(42f, _speed + 50f * dt);
                if (_homing != null && _homing.IsAlive)
                {
                    Vector3 to = (_homing.AimPoint - transform.position).normalized;
                    _dir = Vector3.Slerp(_dir, to, 1f - Mathf.Exp(-6f * dt)).normalized;
                    transform.rotation = Quaternion.LookRotation(_dir);
                }
                _trailTimer -= dt;
                if (_trailTimer <= 0f)
                {
                    _trailTimer = 0.04f;
                    VFX.Play(VFXType.Smoke, transform.position - _dir * 0.4f, new Color(0.8f, 0.8f, 0.8f, 0.6f), 1, 0.6f);
                }
            }

            Vector3 a = transform.position;
            float step = _speed * dt;
            Vector3 b = a + _dir * step;
            Vector3 hitPoint;
            var target = _owner.SegmentHit(a, b, _kind == ProjectileKind.Rocket ? 0.35f : 0.15f, out hitPoint);
            if (target != null)
            {
                Hit(target, hitPoint);
                return;
            }
            transform.position = b;
            _travelled += step;
            if (_travelled > _maxDist || b.y < -1f) _owner.ReleaseProjectile(this);
        }

        void Hit(Damageable target, Vector3 point)
        {
            float dealt = target.ApplyDamage(_damage, _crit, point);
            DamageNumbers.Show(target.AimPoint, dealt, _crit);
            VFX.Play(VFXType.Sparks, point, _crit ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.7f, 0.3f), _crit ? 10 : 5);
            AudioManager.Play(_crit ? SfxId.Crit : SfxId.Hit, _crit ? 0.6f : 0.35f);
            if (_splash > 0f) _owner.Explode(point, _splash, _damage, _crit, target);
            _owner.ReleaseProjectile(this);
        }
    }
}
