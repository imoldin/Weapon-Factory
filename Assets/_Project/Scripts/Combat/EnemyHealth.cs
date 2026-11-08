using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Enemy hit points with readable feedback: white hit flash, punch-scale, floating health bar.
    /// </summary>
    public class EnemyHealth : Damageable
    {
        public EnemyController Controller { get; set; }
        Renderer[] _renderers;
        Material[] _originals;
        float _flash;
        Transform _barRoot, _barFill;
        float _shownFraction = 1f;
        static Material _flashMat;

        public void Setup(float maxHealth, float armor, float hitRadius, Vector3 aimOffset, bool showBar, float barWidth, float barHeight)
        {
            InitHealth(maxHealth, armor);
            HitRadius = hitRadius;
            AimOffset = aimOffset;
            Priority = 1;
            CacheRenderers();
            if (showBar) BuildBar(barWidth, barHeight);
        }

        public void CacheRenderers()
        {
            var list = new List<Renderer>(GetComponentsInChildren<MeshRenderer>(true));
            if (_barRoot != null) list.RemoveAll(r => r.transform.IsChildOf(_barRoot));
            _renderers = list.ToArray();
            _originals = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _originals[i] = _renderers[i].sharedMaterial;
        }

        void BuildBar(float width, float height)
        {
            _barRoot = Shapes.Empty(transform, "HealthBar", new Vector3(0f, height, 0f)).transform;
            _barRoot.gameObject.AddComponent<Billboard>();
            var bg = Shapes.Create(PrimitiveType.Quad, _barRoot, Vector3.zero, new Vector3(width + 0.08f, 0.2f, 1f), GameAssets.Unlit(new Color(0.08f, 0.08f, 0.1f)), "BarBG");
            bg.transform.localRotation = Quaternion.identity;
            var pivot = Shapes.Empty(_barRoot, "FillPivot", new Vector3(-width * 0.5f, 0f, -0.01f)).transform;
            var fill = Shapes.Create(PrimitiveType.Quad, pivot, new Vector3(width * 0.5f, 0f, 0f), new Vector3(width, 0.13f, 1f), GameAssets.Unlit(new Color(1f, 0.25f, 0.25f)), "BarFill");
            fill.transform.localRotation = Quaternion.identity;
            _barFill = pivot;
            _barRoot.gameObject.SetActive(false); // shown after the first hit
        }

        protected override void OnDamaged(float amount, bool crit, Vector3 hitPoint)
        {
            _flash = 0.07f;
            SetFlash(true);
            if (_barRoot != null && !_barRoot.gameObject.activeSelf && IsAlive) _barRoot.gameObject.SetActive(true);
            if (Controller != null) Controller.OnHit(amount, crit);
        }

        protected override void OnDeath()
        {
            SetFlash(false);
            if (_barRoot != null) _barRoot.gameObject.SetActive(false);
            if (Controller != null) Controller.Die();
            AudioManager.Play(SfxId.EnemyDie, 0.8f);
        }

        void SetFlash(bool on)
        {
            if (_renderers == null) return;
            if (_flashMat == null) _flashMat = GameAssets.Glow(Color.white, 1.2f);
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].sharedMaterial = on ? _flashMat : _originals[i];
        }

        void Update()
        {
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                if (_flash <= 0f) SetFlash(false);
            }
            if (_barFill != null)
            {
                _shownFraction = Mathf.Lerp(_shownFraction, HealthFraction, 1f - Mathf.Exp(-12f * Time.deltaTime));
                _barFill.localScale = new Vector3(Mathf.Max(0.001f, _shownFraction), 1f, 1f);
            }
        }
    }
}
