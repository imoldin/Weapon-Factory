using System;
using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;
using WeaponFactory.Weapons;

namespace WeaponFactory.Player
{
    /// <summary>Implemented by everything on the track the player can touch (pickups, gates, obstacles).</summary>
    public interface ITrackInteractable
    {
        void OnPlayerEnter(PlayerController player);
    }

    /// <summary>
    /// The player = the flying weapon. Owns weapon integrity (health), invulnerability frames after
    /// a hit, and routes trigger contacts to track objects.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public const float MaxIntegrity = 100f;
        public float Integrity { get; private set; }
        public bool IsAlive { get { return Integrity > 0f; } }
        public bool TookDamage { get; private set; }
        public float IntegrityFraction { get { return Integrity / MaxIntegrity; } }
        public Vector3 HitCenter { get { return transform.position + new Vector3(0f, 0.1f, 0.3f); } }

        public WeaponController Weapon { get; private set; }
        public WeaponFiring Firing { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerInputHandler Input { get; private set; }
        public Transform Visual { get; private set; }

        public event Action<float, bool> Damaged;   // amount, lostComponent
        public event Action<string> Died;           // reason
        public event Action<ComponentType> ComponentLost;

        float _invulnerable;
        string _deathReason;

        public static PlayerController Create(Vector3 position, PermanentBonuses perm, WeaponSkinDefinition skin)
        {
            var root = new GameObject("Player");
            root.transform.position = position;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.1f, 0.2f);
            col.size = new Vector3(1.1f, 1.0f, 1.8f);

            var input = root.AddComponent<PlayerInputHandler>();
            var tilt = Shapes.Empty(root.transform, "Tilt", new Vector3(0f, 0f, 0f)).transform;
            var hover = Shapes.Empty(tilt, "Hover").transform;
            var bob = hover.gameObject.AddComponent<Bobber>();
            bob.amplitude = 0.06f; bob.frequency = 0.8f;
            var aim = Shapes.Empty(hover, "Aim").transform;
            var modelParent = Shapes.Empty(aim, "ModelParent").transform;
            modelParent.localScale = Vector3.one * 1.45f;

            // hover glow pad under the weapon
            var pad = Shapes.CylinderY(root.transform, new Vector3(0f, -1.12f, 0.2f), 1.5f, 0.02f, GameAssets.Glow(new Color(0.2f, 0.85f, 1f), 1.2f), "HoverPad");
            pad.AddComponent<Pulser>().amount = 0.1f;

            var move = root.AddComponent<PlayerMovement>();
            move.tiltTarget = tilt;

            var weapon = root.AddComponent<WeaponController>();
            weapon.Init(modelParent, perm, skin);
            var firing = root.AddComponent<WeaponFiring>();
            firing.aimPivot = aim;
            firing.ResetAmmo();

            var pc = root.AddComponent<PlayerController>();
            pc.Weapon = weapon;
            pc.Firing = firing;
            pc.Movement = move;
            pc.Input = input;
            pc.Visual = hover;
            return pc;
        }

        void Awake()
        {
            Instance = this;
            Integrity = MaxIntegrity;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (_invulnerable > 0f)
            {
                _invulnerable -= Time.deltaTime;
                // blink while invulnerable
                if (Visual != null) Visual.gameObject.SetActive(_invulnerable <= 0f || Mathf.Repeat(_invulnerable, 0.16f) > 0.06f);
            }
            else if (Visual != null && !Visual.gameObject.activeSelf) Visual.gameObject.SetActive(true);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsAlive) return;
            var interactable = other.GetComponentInParent<ITrackInteractable>();
            if (interactable != null) interactable.OnPlayerEnter(this);
        }

        void OnTriggerStay(Collider other)
        {
            // Obstacles that become dangerous while overlapping (crushers, lasers) re-check here.
            if (!IsAlive || _invulnerable > 0f) return;
            var obstacle = other.GetComponentInParent<Obstacles.Obstacle>();
            if (obstacle != null) obstacle.OnPlayerEnter(this);
        }

        public bool IsInvulnerable { get { return _invulnerable > 0f; } }

        /// <summary>Damages weapon integrity. Optionally knocks a component off the weapon.</summary>
        public void TakeHit(float damage, string reason, bool loseComponent, bool knockback = false)
        {
            if (!IsAlive || _invulnerable > 0f || damage <= 0f) return;
            Integrity = Mathf.Max(0f, Integrity - damage);
            TookDamage = true;
            _invulnerable = 0.9f;
            _deathReason = reason;

            bool lost = false;
            ComponentType lostType;
            if (loseComponent && Weapon.LoseRandomComponent(out lostType))
            {
                lost = true;
                if (ComponentLost != null) ComponentLost(lostType);
            }
            if (knockback) Movement.Knockback(1.6f);

            AudioManager.Play(SfxId.PlayerHurt, 0.8f);
            ScreenShake.Add(0.35f);
            VFX.Play(VFXType.Sparks, HitCenter, new Color(1f, 0.5f, 0.3f), 18);
            if (Core.GameManager.Instance != null) Core.GameManager.Instance.Vibrate();
            if (Damaged != null) Damaged(damage, lost);

            if (Integrity <= 0f)
            {
                VFX.Explosion(HitCenter, 1.6f, new Color(1f, 0.4f, 0.2f));
                if (Visual != null) Visual.gameObject.SetActive(false);
                if (Died != null) Died(_deathReason);
            }
        }

        public void Repair(float amount)
        {
            if (!IsAlive) return;
            Integrity = Mathf.Min(MaxIntegrity, Integrity + amount);
        }
    }
}
