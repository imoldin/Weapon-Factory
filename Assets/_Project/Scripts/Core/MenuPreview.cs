using UnityEngine;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.UI;
using WeaponFactory.Utilities;
using WeaponFactory.Weapons;

namespace WeaponFactory.Core
{
    /// <summary>Rotating weapon showcase on a glowing pedestal.</summary>
    public class MenuPreview : MonoBehaviour
    {
        WeaponController _weapon;
        Transform _spin;
        Transform _modelParent;
        public int ShownTier { get; private set; }

        public static MenuPreview Create(Vector3 position)
        {
            var root = new GameObject("WeaponPreview");
            root.transform.position = position;
            var p = root.AddComponent<MenuPreview>();
            // pedestal
            Shapes.CylinderY(root.transform, new Vector3(0f, -1.35f, 0f), 2.2f, 0.5f, GameAssets.Lit(new Color(0.15f, 0.16f, 0.2f), 0.6f, 0.7f), "Pedestal");
            var ring = Shapes.CylinderY(root.transform, new Vector3(0f, -1.08f, 0f), 2.3f, 0.06f, GameAssets.Glow(new Color(0.2f, 0.85f, 1f), 2f), "PedestalRing");
            ring.AddComponent<Pulser>().amount = 0.04f;
            Shapes.CylinderY(root.transform, new Vector3(0f, -1.06f, 0f), 1.9f, 0.06f, GameAssets.Lit(new Color(0.1f, 0.11f, 0.14f)), "PedestalTop");
            var spin = Shapes.Empty(root.transform, "Spin").transform;
            var sp = spin.gameObject.AddComponent<Spinner>();
            sp.speed = 28f;
            var bob = spin.gameObject.AddComponent<Bobber>();
            bob.amplitude = 0.08f; bob.frequency = 0.5f;
            p._spin = spin;
            p._modelParent = Shapes.Empty(spin, "ModelParent").transform;
            p._weapon = root.AddComponent<WeaponController>();
            return p;
        }

        public void Show(int tier, WeaponSkinDefinition skin)
        {
            ShownTier = Mathf.Clamp(tier, 1, WeaponEvolution.MaxTier);
            var model = WeaponEvolution.GetTier(ShownTier).model;
            // bigger weapons are scaled down to fit the pedestal
            float scale = model >= WeaponModel.Minigun ? 1.05f : (model >= WeaponModel.AssaultRifle ? 1.15f : 1.6f);
            _modelParent.localScale = Vector3.one * scale;
            _weapon.InitShowcase(_modelParent, ShownTier, skin, true);
            // centre the model on the pedestal
            if (_weapon.Rig != null) _weapon.Rig.root.localPosition = new Vector3(0f, 0f, -(_weapon.Rig.length * 0.5f - 0.35f) * 0.6f);
            Tween.Punch(_spin, 0.15f, 0.3f, true);
            VFX.Play(VFXType.Ring, transform.position + Vector3.down * 0.9f, new Color(0.3f, 0.9f, 1f), 30);
        }

        public string ShownName { get { return WeaponEvolution.GetTier(ShownTier).displayName; } }
    }
}
