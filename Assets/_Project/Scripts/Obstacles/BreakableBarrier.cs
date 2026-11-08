using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Combat;
using WeaponFactory.Levels;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Obstacles
{
    /// <summary>
    /// Wall with hit points blocking one side of the track. Shoot it down for coins or dodge it;
    /// crashing into it damages the weapon. Shows its remaining HP.
    /// </summary>
    public class BreakableBarrier : Damageable, ITrackInteractable
    {
        TextMesh _label;
        Transform _body;
        int _coinReward;
        int _shownHp = -1;

        public static BreakableBarrier Build(Transform parent, Vector3 pos, int hp)
        {
            var go = new GameObject("Barrier");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var b = go.AddComponent<BreakableBarrier>();
            b.InitHealth(hp);
            b.HitRadius = 1.4f;
            b.AimOffset = new Vector3(0f, 1.0f, 0f);
            b._coinReward = Mathf.Clamp(hp / 6, 3, 40);

            var body = Shapes.Empty(go.transform, "Body").transform;
            b._body = body;
            var crate = GameAssets.Lit(new Color(0.35f, 0.38f, 0.45f), 0.4f, 0.5f);
            var edge = GameAssets.HazardMaterial;
            Shapes.Box(body, new Vector3(0f, 1.0f, 0f), new Vector3(4.0f, 2.0f, 0.7f), crate, "Wall", true);
            Shapes.Box(body, new Vector3(0f, 2.05f, 0f), new Vector3(4.1f, 0.15f, 0.75f), edge, "Top");
            Shapes.Box(body, new Vector3(0f, 0.08f, 0f), new Vector3(4.1f, 0.15f, 0.75f), edge, "Bottom");
            for (int i = -1; i <= 1; i++)
                Shapes.Box(body, new Vector3(i * 1.3f, 1.0f, -0.36f), new Vector3(0.1f, 1.7f, 0.02f), GameAssets.Lit(new Color(0.25f, 0.27f, 0.32f)), "Rib");
            b._label = Shapes.Text3D(go.transform, "", new Vector3(0f, 1.05f, -0.4f), 0.1f, Color.white);
            b.UpdateLabel();

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1f, 0f);
            col.size = new Vector3(4.0f, 2f, 0.7f);
            return b;
        }

        void UpdateLabel()
        {
            int hp = Mathf.CeilToInt(Health);
            if (hp == _shownHp || _label == null) return;
            _shownHp = hp;
            _label.text = NumberCache.Get(hp);
        }

        protected override void OnDamaged(float amount, bool crit, Vector3 hitPoint)
        {
            UpdateLabel();
            if (_body != null) Tween.Punch(_body, 0.04f, 0.12f);
        }

        protected override void OnDeath()
        {
            VFX.Play(VFXType.Debris, transform.position + Vector3.up, new Color(0.4f, 0.42f, 0.5f), 24);
            VFX.Play(VFXType.Sparks, transform.position + Vector3.up, new Color(1f, 0.8f, 0.4f), 20);
            AudioManager.Play(SfxId.EnemyDie, 0.7f);
            ScreenShake.Add(0.12f);
            var lm = LevelManager.Instance;
            if (lm != null) lm.AddRunCoins(_coinReward, transform.position + Vector3.up * 1.5f);
            gameObject.SetActive(false);
        }

        public void OnPlayerEnter(PlayerController player)
        {
            if (!IsAlive) return;
            player.TakeHit(20f, "Crashed into a barrier", true, true);
            ApplyDamage(Health + 1f, false, transform.position);
        }
    }
}
