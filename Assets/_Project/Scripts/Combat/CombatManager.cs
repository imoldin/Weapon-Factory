using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Scene-level combat hub: keeps the list of shootable targets, owns projectile pools,
    /// performs hit tests (segment vs sphere — cheap and tunnelling-free) and splash damage,
    /// and tracks kill statistics for rewards.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }
        static readonly List<Damageable> _pending = new List<Damageable>();

        readonly List<Damageable> _targets = new List<Damageable>(64);
        ObjectPool<Projectile> _projectiles;
        ObjectPool<EnemyProjectile> _enemyProjectiles;

        public int EnemiesDefeated { get; private set; }
        public int EnemyRewardSum { get; private set; }
        public bool BossDefeated { get; private set; }
        public int ActiveEnemyCount { get; private set; }

        public System.Action<EnemyController> EnemyKilled;

        void Awake()
        {
            Instance = this;
            _projectiles = new ObjectPool<Projectile>(() => Projectile.Create(transform), 32);
            _enemyProjectiles = new ObjectPool<EnemyProjectile>(() => EnemyProjectile.Create(transform), 16);
            foreach (var d in _pending) if (d != null && !_targets.Contains(d)) _targets.Add(d);
            _pending.Clear();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Register(Damageable d)
        {
            if (Instance == null) { if (!_pending.Contains(d)) _pending.Add(d); return; }
            if (!Instance._targets.Contains(d)) Instance._targets.Add(d);
        }

        public static void Unregister(Damageable d)
        {
            _pending.Remove(d);
            if (Instance != null) Instance._targets.Remove(d);
        }

        /// <summary>Best target ahead of the shooter within range (higher priority, then nearest).</summary>
        public Damageable FindTarget(Vector3 from, float range, float maxLateral = 7f)
        {
            Damageable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null || !t.IsAlive || !t.Targetable) continue;
                Vector3 p = t.AimPoint;
                float dz = p.z - from.z;
                if (dz < 0.5f || dz > range) continue;
                float dx = Mathf.Abs(p.x - from.x);
                if (dx > maxLateral) continue;
                float score = dz + dx * 1.5f - t.Priority * 100f;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            return best;
        }

        /// <summary>First alive target whose sphere intersects segment a→b (expanded by radius).</summary>
        public Damageable SegmentHit(Vector3 a, Vector3 b, float radius, out Vector3 hitPoint)
        {
            hitPoint = b;
            Damageable best = null;
            float bestT = float.MaxValue;
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null || !t.IsAlive) continue;
                Vector3 c = t.AimPoint;
                float u = len2 > 0f ? Mathf.Clamp01(Vector3.Dot(c - a, ab) / len2) : 0f;
                Vector3 closest = a + ab * u;
                float r = t.HitRadius + radius;
                if ((c - closest).sqrMagnitude <= r * r && u < bestT)
                {
                    bestT = u;
                    best = t;
                    hitPoint = closest;
                }
            }
            return best;
        }

        readonly List<Damageable> _splashBuffer = new List<Damageable>(32);

        public void Explode(Vector3 position, float radius, float damage, bool crit, Damageable exclude)
        {
            _splashBuffer.Clear();
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null || !t.IsAlive || t == exclude) continue;
                float r = radius + t.HitRadius;
                if ((t.AimPoint - position).sqrMagnitude <= r * r) _splashBuffer.Add(t);
            }
            for (int i = 0; i < _splashBuffer.Count; i++)
            {
                var t = _splashBuffer[i];
                float dealt = t.ApplyDamage(damage * 0.6f, crit, t.AimPoint);
                DamageNumbers.Show(t.AimPoint, dealt, crit);
            }
            VFX.Explosion(position, Mathf.Clamp(radius * 0.5f, 0.8f, 2.5f), new Color(1f, 0.5f, 0.2f));
            AudioManager.Play(SfxId.Explosion, 0.8f);
        }

        // ---------------- projectiles ----------------

        public Projectile SpawnProjectile(Vector3 pos, Vector3 dir, WeaponStats stats, float damage, bool crit, Damageable homingTarget)
        {
            var p = _projectiles.Get();
            p.Launch(this, pos, dir, stats.projectile, damage, crit, stats.splashRadius, stats.range + 6f, homingTarget);
            return p;
        }

        public void ReleaseProjectile(Projectile p) { _projectiles.Release(p); }

        public EnemyProjectile SpawnEnemyProjectile(Vector3 pos, Vector3 velocity, float damage, float size, Color color)
        {
            var p = _enemyProjectiles.Get();
            p.Launch(this, pos, velocity, damage, size, color);
            return p;
        }

        public void ReleaseEnemyProjectile(EnemyProjectile p) { _enemyProjectiles.Release(p); }

        public void ClearProjectiles()
        {
            _projectiles.ReleaseAll();
            _enemyProjectiles.ReleaseAll();
        }

        // ---------------- statistics ----------------

        public void OnEnemySpawned() { ActiveEnemyCount++; }

        public void OnEnemyDied(EnemyController enemy)
        {
            ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
            EnemiesDefeated++;
            EnemyRewardSum += enemy.Definition.reward;
            if (enemy.Definition.type == EnemyType.Boss) BossDefeated = true;
            if (EnemyKilled != null) EnemyKilled(enemy);
        }
    }

    /// <summary>Damage number helper (white normal, yellow crits).</summary>
    public static class DamageNumbers
    {
        static readonly Color Normal = new Color(1f, 1f, 1f);
        static readonly Color Crit = new Color(1f, 0.85f, 0.1f);

        public static void Show(Vector3 pos, float amount, bool crit)
        {
            if (amount <= 0f) return;
            int v = Mathf.Max(1, Mathf.RoundToInt(amount));
            FloatingText.Spawn(pos + Vector3.up * 0.4f, crit ? NumberCache.Get(v) + "!" : NumberCache.Get(v), crit ? Crit : Normal, crit ? 1.5f : 1f, 0.7f);
        }
    }
}
