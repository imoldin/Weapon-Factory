using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Combat;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Weapons
{
    /// <summary>
    /// Automatic firing: picks the best target in range, aims, fires one projectile per barrel at the
    /// weapon's fire rate, handles magazine + reload (with a tilt animation), recoil, muzzle flashes
    /// and the minigun's rotating barrels.
    /// </summary>
    [RequireComponent(typeof(WeaponController))]
    public class WeaponFiring : MonoBehaviour
    {
        public Transform aimPivot;      // rotated toward the target
        public bool FiringEnabled { get; set; }

        public int Ammo { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadProgress { get { return IsReloading ? Mathf.Clamp01(_reloadTimer / Mathf.Max(0.01f, _weapon.Stats.reloadTime)) : 1f; } }
        public Damageable CurrentTarget { get; private set; }
        public int ShotsFired { get; private set; }

        WeaponController _weapon;
        float _cooldown, _reloadTimer, _idleTimer, _recoil, _spinSpeed;
        int _lastMagazine;
        readonly List<GameObject> _flashes = new List<GameObject>();
        float _flashTimer;
        int _muzzleIndex;

        void Awake()
        {
            _weapon = GetComponent<WeaponController>();
        }

        void OnEnable()
        {
            if (_weapon == null) _weapon = GetComponent<WeaponController>();
            _weapon.RigRebuilt += OnRigRebuilt;
            _weapon.StatsChanged += OnStatsChanged;
        }

        void OnDisable()
        {
            _weapon.RigRebuilt -= OnRigRebuilt;
            _weapon.StatsChanged -= OnStatsChanged;
        }

        public void ResetAmmo()
        {
            Ammo = _weapon.Stats.magazine;
            _lastMagazine = Ammo;
            IsReloading = false;
            if (_weapon.Rig != null) OnRigRebuilt(_weapon.Rig);
        }

        void OnStatsChanged(WeaponStats s)
        {
            // Bigger magazines are filled immediately (feels rewarding); smaller ones clamp.
            int delta = s.magazine - _lastMagazine;
            if (delta > 0) Ammo += delta;
            Ammo = Mathf.Clamp(Ammo, 0, s.magazine);
            _lastMagazine = s.magazine;
        }

        void OnRigRebuilt(WeaponRig rig)
        {
            _flashes.Clear();
            var flashMat = GameAssets.Glow(new Color(1f, 0.8f, 0.35f), 3f);
            foreach (var m in rig.muzzles)
            {
                var f = Shapes.Empty(m, "Flash");
                Shapes.Create(PrimitiveType.Sphere, f.transform, new Vector3(0f, 0f, 0.1f), new Vector3(0.3f, 0.3f, 0.45f), flashMat);
                Shapes.Box(f.transform, new Vector3(0f, 0f, 0.1f), new Vector3(0.5f, 0.06f, 0.06f), flashMat);
                Shapes.Box(f.transform, new Vector3(0f, 0f, 0.1f), new Vector3(0.06f, 0.5f, 0.06f), flashMat);
                f.SetActive(false);
                _flashes.Add(f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var stats = _weapon.Stats;
            var rig = _weapon.Rig;
            var cm = CombatManager.Instance;

            CurrentTarget = (FiringEnabled && cm != null) ? cm.FindTarget(transform.position, stats.range) : null;

            // Aim toward target (limited yaw so it still reads as a runner).
            if (aimPivot != null)
            {
                Quaternion want = Quaternion.identity;
                if (CurrentTarget != null)
                {
                    Vector3 to = CurrentTarget.AimPoint - aimPivot.position;
                    to.y *= 0.5f;
                    if (to.sqrMagnitude > 0.01f)
                    {
                        var world = Quaternion.LookRotation(to.normalized);
                        var local = Quaternion.Inverse(aimPivot.parent.rotation) * world;
                        Vector3 e = local.eulerAngles;
                        float yaw = Mathf.Clamp(Mathf.DeltaAngle(0f, e.y), -35f, 35f);
                        float pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, e.x), -15f, 15f);
                        want = Quaternion.Euler(pitch, yaw, 0f);
                    }
                }
                // reload tilt
                if (IsReloading)
                {
                    float t = ReloadProgress;
                    want *= Quaternion.Euler(-Mathf.Sin(t * Mathf.PI) * 35f, 0f, Mathf.Sin(t * Mathf.PI) * 25f);
                }
                aimPivot.localRotation = Quaternion.Slerp(aimPivot.localRotation, want, 1f - Mathf.Exp(-12f * dt));
            }

            // Reload
            if (IsReloading)
            {
                _reloadTimer += dt;
                if (_reloadTimer >= stats.reloadTime)
                {
                    IsReloading = false;
                    Ammo = stats.magazine;
                }
            }
            else if (CurrentTarget != null)
            {
                _idleTimer = 0f;
                _cooldown -= dt;
                int safety = 0;
                while (_cooldown <= 0f && Ammo > 0 && safety++ < 4)
                {
                    Fire(CurrentTarget, stats, rig);
                    _cooldown += 1f / Mathf.Max(0.1f, stats.fireRate);
                    Ammo--;
                }
                if (Ammo <= 0) StartReload();
            }
            else
            {
                _cooldown = Mathf.Max(0f, _cooldown - dt);
                _idleTimer += dt;
                if (_idleTimer > 1.2f && Ammo < stats.magazine && FiringEnabled) StartReload();
            }

            // Recoil + minigun spin + flash
            _recoil = Mathf.Lerp(_recoil, 0f, 1f - Mathf.Exp(-18f * dt));
            if (rig != null && rig.recoilPivot != null)
            {
                rig.recoilPivot.localPosition = new Vector3(0f, _recoil * 0.15f, -_recoil);
                rig.recoilPivot.localRotation = Quaternion.Euler(-_recoil * 25f, 0f, 0f);
            }
            float wantSpin = (CurrentTarget != null && !IsReloading) ? 1440f : 90f;
            _spinSpeed = Mathf.Lerp(_spinSpeed, wantSpin, 1f - Mathf.Exp(-3f * dt));
            if (rig != null && rig.spinner != null) rig.spinner.Rotate(0f, 0f, _spinSpeed * dt, Space.Self);
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) for (int i = 0; i < _flashes.Count; i++) if (_flashes[i] != null) _flashes[i].SetActive(false);
            }
        }

        void StartReload()
        {
            if (IsReloading) return;
            IsReloading = true;
            _reloadTimer = 0f;
            AudioManager.Play(SfxId.Reload, 0.6f);
        }

        void Fire(Damageable target, WeaponStats stats, WeaponRig rig)
        {
            var cm = CombatManager.Instance;
            if (cm == null || rig == null || rig.muzzles.Count == 0) return;
            ShotsFired++;
            int count = stats.projectilesPerShot;
            float spreadDeg = (1f - stats.accuracy) * 10f;

            for (int i = 0; i < count; i++)
            {
                // minigun alternates its single muzzle; multi-barrel weapons fire from every barrel
                Transform muzzle = rig.muzzles[(i + (rig.spinner != null ? _muzzleIndex++ : 0)) % rig.muzzles.Count];
                Vector3 origin = muzzle.position;
                Vector3 dir = (target.AimPoint - origin).normalized;
                dir = Quaternion.Euler(Random.Range(-spreadDeg, spreadDeg) * 0.5f, Random.Range(-spreadDeg, spreadDeg), 0f) * dir;
                bool crit = Random.value < stats.critChance;
                float dmg = stats.damage * (crit ? 2f : 1f);
                cm.SpawnProjectile(origin, dir, stats, dmg, crit, target);
            }

            for (int i = 0; i < _flashes.Count; i++)
                if (_flashes[i] != null)
                {
                    _flashes[i].SetActive(true);
                    _flashes[i].transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                }
            _flashTimer = 0.045f;

            switch (stats.projectile)
            {
                case ProjectileKind.Rocket:
                    _recoil = 0.45f;
                    AudioManager.Play(SfxId.Rocket, 0.8f);
                    ScreenShake.Add(0.08f);
                    break;
                case ProjectileKind.Explosive:
                    _recoil = 0.32f;
                    AudioManager.Play(SfxId.ShootHeavy, 0.7f);
                    ScreenShake.Add(0.05f);
                    break;
                case ProjectileKind.HeavyBullet:
                    _recoil = Mathf.Max(_recoil, 0.08f);
                    AudioManager.Play(SfxId.Shoot, 0.4f, 0.1f, 0.85f);
                    break;
                default:
                    _recoil = Mathf.Max(_recoil, 0.16f);
                    AudioManager.Play(SfxId.Shoot, 0.45f);
                    break;
            }
        }
    }
}
