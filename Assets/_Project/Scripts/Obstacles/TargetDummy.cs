using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Combat;
using WeaponFactory.Levels;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Obstacles
{
    /// <summary>Shooting-range target. Harmless; gives coins and shows off the weapon's power.</summary>
    public class TargetDummy : Damageable
    {
        int _coins;
        Transform _board;

        public static TargetDummy Build(Transform parent, Vector3 pos, int hp)
        {
            var go = new GameObject("Target");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var t = go.AddComponent<TargetDummy>();
            t.InitHealth(hp);
            t.HitRadius = 0.7f;
            t.AimOffset = new Vector3(0f, 1.3f, 0f);
            t._coins = Mathf.Clamp(hp / 8, 2, 15);
            Shapes.Box(go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.15f, 1.1f, 0.15f), GameAssets.Lit(new Color(0.3f, 0.3f, 0.34f)), "Pole");
            var board = Shapes.Empty(go.transform, "Board", new Vector3(0f, 1.3f, 0f)).transform;
            t._board = board;
            Shapes.CylinderZ(board, Vector3.zero, 1.1f, 0.1f, GameAssets.Lit(Color.white, 0.3f, 0f));
            Shapes.CylinderZ(board, new Vector3(0f, 0f, -0.02f), 0.8f, 0.1f, GameAssets.Lit(new Color(0.95f, 0.2f, 0.2f), 0.3f, 0f));
            Shapes.CylinderZ(board, new Vector3(0f, 0f, -0.04f), 0.5f, 0.1f, GameAssets.Lit(Color.white, 0.3f, 0f));
            Shapes.CylinderZ(board, new Vector3(0f, 0f, -0.06f), 0.22f, 0.1f, GameAssets.Glow(new Color(1f, 0.2f, 0.2f), 1.5f));
            return t;
        }

        protected override void OnDamaged(float amount, bool crit, Vector3 hitPoint)
        {
            if (_board != null) Tween.Punch(_board, 0.12f, 0.15f);
        }

        protected override void OnDeath()
        {
            VFX.Play(VFXType.Debris, AimPoint, Color.white, 14);
            VFX.Play(VFXType.Pickup, AimPoint, new Color(1f, 0.85f, 0.2f), 14);
            AudioManager.Play(SfxId.EnemyDie, 0.5f, 0.1f, 1.3f);
            var lm = LevelManager.Instance;
            if (lm != null) lm.AddRunCoins(_coins, AimPoint);
            gameObject.SetActive(false);
        }
    }
}
