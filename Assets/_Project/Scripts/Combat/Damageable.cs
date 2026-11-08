using System;
using UnityEngine;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Anything the player's weapon can shoot: enemies, breakable barriers, targets, explosive barrels.
    /// Registers itself with the CombatManager so projectiles can find it without physics queries.
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        public float MaxHealth { get; protected set; }
        public float Health { get; protected set; }
        /// <summary>Fraction of incoming damage ignored (0..0.8).</summary>
        public float Armor { get; set; }
        public float HitRadius = 0.8f;
        public Vector3 AimOffset = new Vector3(0f, 0.8f, 0f);
        /// <summary>Higher priority targets are preferred by the auto-aim.</summary>
        public int Priority;
        public bool Targetable = true;

        public bool IsAlive { get { return Health > 0f; } }
        public Vector3 AimPoint { get { return transform.position + AimOffset; } }
        public float HealthFraction { get { return MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f; } }

        public event Action<Damageable, float, bool> Damaged;
        public event Action<Damageable> Died;

        public virtual void InitHealth(float maxHealth, float armor = 0f)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            Armor = Mathf.Clamp(armor, 0f, 0.8f);
        }

        protected virtual void OnEnable() { CombatManager.Register(this); }
        protected virtual void OnDisable() { CombatManager.Unregister(this); }

        /// <summary>Applies damage after armour. Returns the damage actually dealt.</summary>
        public float ApplyDamage(float rawDamage, bool crit, Vector3 hitPoint)
        {
            if (!IsAlive || rawDamage <= 0f) return 0f;
            float dmg = rawDamage * (1f - Armor);
            Health -= dmg;
            OnDamaged(dmg, crit, hitPoint);
            if (Damaged != null) Damaged(this, dmg, crit);
            if (Health <= 0f)
            {
                Health = 0f;
                OnDeath();
                if (Died != null) Died(this);
            }
            return dmg;
        }

        protected virtual void OnDamaged(float amount, bool crit, Vector3 hitPoint) { }
        protected virtual void OnDeath() { }
    }
}
