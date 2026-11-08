using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Combat;
using WeaponFactory.Levels;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Obstacles
{
    /// <summary>Red explosive barrel cluster: explodes when shot (bonus coins) or when touched (damage).</summary>
    public class ExplosiveBarrel : Damageable, ITrackInteractable
    {
        public static ExplosiveBarrel Build(Transform parent, Vector3 pos)
        {
            var go = new GameObject("ExplosiveBarrels");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var b = go.AddComponent<ExplosiveBarrel>();
            b.InitHealth(15f);
            b.HitRadius = 1.0f;
            b.AimOffset = new Vector3(0f, 0.7f, 0f);
            var red = GameAssets.Lit(new Color(0.85f, 0.12f, 0.1f), 0.5f, 0.3f);
            Vector3[] offs = { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0.1f), new Vector3(0f, 0f, 0.6f) };
            foreach (var o in offs)
            {
                Shapes.CylinderY(go.transform, o + new Vector3(0f, 0.55f, 0f), 0.75f, 1.1f, red, "Barrel");
                Shapes.CylinderY(go.transform, o + new Vector3(0f, 0.7f, 0f), 0.77f, 0.22f, GameAssets.HazardMaterial, "Stripe");
            }
            Shapes.Sphere(go.transform, new Vector3(0f, 1.3f, 0.2f), 0.22f, GameAssets.Glow(new Color(1f, 0.2f, 0.1f), 3f), "Light").AddComponent<Blinker>().interval = 0.3f;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.6f, 0.2f);
            col.size = new Vector3(1.9f, 1.2f, 1.5f);
            return b;
        }

        protected override void OnDeath()
        {
            var cm = CombatManager.Instance;
            if (cm != null) cm.Explode(AimPoint, 3f, 60f, false, this);
            else VFX.Explosion(AimPoint, 1.5f, new Color(1f, 0.4f, 0.1f));
            var lm = LevelManager.Instance;
            if (lm != null) lm.AddRunCoins(8, AimPoint);
            gameObject.SetActive(false);
        }

        public void OnPlayerEnter(PlayerController player)
        {
            if (!IsAlive) return;
            player.TakeHit(25f, "Blown up by explosive barrels", true, true);
            ApplyDamage(Health + 1f, false, transform.position);
        }
    }
}
