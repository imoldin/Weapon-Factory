using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Enemy AI for the arena. Melee robots march toward the weapon and explode on contact;
    /// drones hover, strafe and shoot; heavy security robots stop at range, fire bursts and
    /// eventually advance. Includes procedural walk/hover animation, hit reactions and a
    /// break-apart death.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyHealth Health { get; private set; }
        protected EnemyParts Parts;
        protected bool Dead;
        protected float SpawnTimer = 0.8f;
        protected float AttackTimer;
        protected float AliveTime;

        float _anim, _strafePhase, _hitPush;
        bool _moving;
        Vector3 _baseScale;
        int _burstLeft;
        float _burstTimer;

        public bool IsDead { get { return Dead; } }

        public virtual void Init(EnemyDefinition def, EnemyHealth health, EnemyParts parts)
        {
            Definition = def;
            Health = health;
            Parts = parts;
            health.Controller = this;
            health.Targetable = false;
            AttackTimer = def.attackInterval > 0f ? def.attackInterval * Random.Range(0.5f, 1f) : 0f;
            _strafePhase = Random.value * 6.28f;
            _baseScale = transform.localScale;
            if (CombatManager.Instance != null) CombatManager.Instance.OnEnemySpawned();

            // spawn animation: drop in (flying) or pop up through a floor hatch
            transform.localScale = _baseScale * 0.01f;
            Tween.Scale(transform, _baseScale, 0.55f, Ease.OutBack);
            VFX.Play(VFXType.Ring, transform.position + Vector3.up * 0.2f, GameAssets.Hex(def.colorHex), 24);
        }

        protected virtual void Update()
        {
            if (Dead) return;
            float dt = Time.deltaTime;
            AliveTime += dt;
            if (SpawnTimer > 0f)
            {
                SpawnTimer -= dt;
                if (SpawnTimer <= 0f) Health.Targetable = true;
                Animate(dt, false);
                return;
            }
            var player = PlayerController.Instance;
            _moving = false;
            if (player != null && player.IsAlive) Behave(dt, player);
            if (_hitPush > 0f)
            {
                float step = Mathf.Min(_hitPush, dt * 4f);
                transform.position += new Vector3(0f, 0f, step);
                _hitPush -= step;
            }
            Animate(dt, _moving);
        }

        protected virtual void Behave(float dt, PlayerController player)
        {
            var d = Definition;
            Vector3 pos = transform.position;
            Vector3 pp = player.transform.position;
            bool ranged = d.rangedDamage > 0f;
            float preferred = ranged ? d.preferredDistance : 1.0f;
            if (d.type == EnemyType.HeavySecurity && AliveTime > 14f) preferred = 1.0f; // eventually charges

            float targetZ = pp.z + preferred;
            float targetX = d.type == EnemyType.Drone ? pp.x + Mathf.Sin(AliveTime * 1.3f + _strafePhase) * 2.6f : pp.x;
            targetX = Mathf.Clamp(targetX, -4f, 4f);

            float speed = d.moveSpeed;
            float nz = Mathf.MoveTowards(pos.z, targetZ, speed * dt);
            float nx = Mathf.MoveTowards(pos.x, targetX, speed * (d.type == EnemyType.Drone ? 1f : 0.55f) * dt);
            _moving = Mathf.Abs(nz - pos.z) > 0.001f || Mathf.Abs(nx - pos.x) > 0.001f;
            pos.z = nz;
            pos.x = nx;
            if (d.flying) pos.y = 1.3f + Mathf.Sin(AliveTime * 2.2f + _strafePhase) * 0.25f;
            transform.position = pos;

            // ranged attack
            if (ranged && pos.z - pp.z <= preferred + 2f && preferred > 1.5f)
            {
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = d.attackInterval;
                    _burstLeft = d.type == EnemyType.HeavySecurity ? 3 : 1;
                    _burstTimer = 0f;
                }
            }
            if (_burstLeft > 0)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    _burstTimer = 0.22f;
                    _burstLeft--;
                    Shoot(player, d.type == EnemyType.HeavySecurity ? 9f : 8f, 0f);
                }
            }

            // melee contact
            if (Mathf.Abs(pos.z - pp.z) < 1.4f + d.hitRadius * 0.3f && Mathf.Abs(pos.x - pp.x) < 1.1f + d.hitRadius * 0.4f)
            {
                player.TakeHit(d.contactDamage, "Overrun by a " + d.displayName.ToLowerInvariant(), true);
                Health.ApplyDamage(Health.Health + 1f, false, transform.position); // self-destructs on impact
            }
        }

        /// <summary>Fires one projectile from each cannon (or the body) toward the player.</summary>
        protected void Shoot(PlayerController player, float speed, float angleOffset)
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;
            Color c = Definition.type == EnemyType.Boss ? new Color(1f, 0.25f, 0.75f) : new Color(1f, 0.25f, 0.3f);
            int n = Mathf.Max(1, Parts.cannons.Count);
            for (int i = 0; i < n; i++)
            {
                Vector3 origin = Parts.cannons.Count > 0 ? Parts.cannons[i].position + Vector3.back * 0.4f : Health.AimPoint;
                Vector3 dir = player.HitCenter - origin;
                dir.y *= 0.6f;
                dir = Quaternion.Euler(0f, angleOffset, 0f) * dir.normalized;
                cm.SpawnEnemyProjectile(origin, dir * speed, Definition.rangedDamage, Definition.type == EnemyType.Boss ? 0.45f : 0.35f, c);
                VFX.Play(VFXType.Sparks, origin, c, 4);
                if (Parts.cannons.Count > 0) Tween.Punch(Parts.cannons[i], 0.2f, 0.15f);
            }
            AudioManager.Play(SfxId.Shoot, 0.3f, 0.08f, 0.6f);
        }

        void Animate(float dt, bool moving)
        {
            _anim += dt * (moving ? 1f : 0.25f);
            float swing = Mathf.Sin(_anim * 9f) * (moving ? 32f : 4f);
            if (Parts.legL != null) Parts.legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (Parts.legR != null) Parts.legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (Parts.armL != null && Definition.type != EnemyType.Boss) Parts.armL.localRotation = Quaternion.Euler(-swing * 0.7f, 0f, 0f);
            if (Parts.armR != null && Definition.type != EnemyType.Boss) Parts.armR.localRotation = Quaternion.Euler(swing * 0.7f, 0f, 0f);
            if (Parts.body != null && !Definition.flying)
            {
                var lp = Parts.body.localPosition;
                lp.y = Mathf.Lerp(lp.y, BaseBodyY() + Mathf.Abs(Mathf.Sin(_anim * 9f)) * (moving ? 0.06f : 0.01f), 0.5f);
                Parts.body.localPosition = lp;
            }
            for (int i = 0; i < Parts.rotors.Count; i++) Parts.rotors[i].Rotate(0f, 1500f * dt, 0f, Space.Self);
            if (Definition.flying && Parts.body != null)
                Parts.body.localRotation = Quaternion.Euler(Mathf.Sin(AliveTime * 2f) * 8f, 0f, Mathf.Sin(AliveTime * 1.3f + _strafePhase) * 12f);
        }

        float BaseBodyY()
        {
            switch (Definition.type)
            {
                case EnemyType.HeavySecurity: return 1.0f;
                case EnemyType.Boss: return 1.25f;
                default: return 0.95f;
            }
        }

        public virtual void OnHit(float amount, bool crit)
        {
            if (Dead) return;
            _hitPush = Mathf.Min(0.6f, _hitPush + (Definition.type == EnemyType.Boss ? 0.01f : 0.08f));
            if (Parts.body != null) Tween.Punch(Parts.body, crit ? 0.12f : 0.06f, 0.12f);
        }

        public virtual void Die()
        {
            if (Dead) return;
            Dead = true;
            Health.Targetable = false;
            var cm = CombatManager.Instance;
            if (cm != null) cm.OnEnemyDied(this);

            float s = Definition.scale;
            VFX.Explosion(Health.AimPoint, Mathf.Clamp(s, 0.8f, 3f), GameAssets.Hex(Definition.colorHex));
            FloatingText.Spawn(Health.AimPoint + Vector3.up * 0.8f, "+" + NumberCache.Get(Definition.reward), new Color(1f, 0.85f, 0.2f), 1.3f, 1f);
            BreakApart();
            Destroy(gameObject, 1.3f);
        }

        /// <summary>Death animation: limbs and plates fly off and shrink.</summary>
        void BreakApart()
        {
            var pieces = new System.Collections.Generic.List<Transform>();
            if (Parts.head != null && Parts.head != Parts.body) pieces.Add(Parts.head);
            if (Parts.armL != null) pieces.Add(Parts.armL);
            if (Parts.armR != null) pieces.Add(Parts.armR);
            if (Parts.legL != null) pieces.Add(Parts.legL);
            if (Parts.legR != null) pieces.Add(Parts.legR);
            if (Parts.body != null) pieces.Add(Parts.body);
            foreach (var piece in pieces)
            {
                var t = piece;
                t.SetParent(transform.parent, true);
                Destroy(t.gameObject, 1.3f);
                Vector3 start = t.position;
                Vector3 vel = new Vector3(Random.Range(-4f, 4f), Random.Range(4f, 8f), Random.Range(1f, 5f));
                Vector3 spin = new Vector3(Random.Range(-400f, 400f), Random.Range(-400f, 400f), Random.Range(-400f, 400f));
                Vector3 scale = t.localScale;
                Quaternion rot = t.rotation;
                Tween.Run(t, 1.1f, p =>
                {
                    float time = p * 1.1f;
                    Vector3 pos = start + vel * time + Vector3.down * 9f * time * time;
                    if (pos.y < 0.1f) pos.y = 0.1f;
                    t.position = pos;
                    t.rotation = rot * Quaternion.Euler(spin * time);
                    t.localScale = scale * Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((p - 0.6f) / 0.4f));
                }, Ease.Linear);
            }
        }
    }
}
