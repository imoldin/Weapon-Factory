using UnityEngine;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;
using WeaponFactory.Weapons;

namespace WeaponFactory.Collectibles
{
    /// <summary>
    /// Builds collectible GameObjects (visual + trigger collider + Collectible component).
    /// Positive pickups glow with bright colours and a green/blue pedestal; negative pickups are
    /// dull/rusty with red warning rings so they are readable at a glance.
    /// </summary>
    public static class CollectibleSpawner
    {
        static readonly Color PositiveRing = new Color(0.25f, 0.9f, 1f);
        static readonly Color NegativeRing = new Color(1f, 0.15f, 0.2f);

        public static Collectible Spawn(Transform parent, PickupKind kind, Vector3 position, ComponentType component = ComponentType.Barrel, int value = 1)
        {
            var go = new GameObject(kind == PickupKind.Component ? "Pickup_" + component : "Pickup_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var c = go.AddComponent<Collectible>();
            c.kind = kind;
            c.component = component;
            c.value = value;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = kind == PickupKind.Coin ? new Vector3(1.0f, 1.2f, 1.0f) : new Vector3(1.5f, 1.6f, 1.2f);

            var vis = Shapes.Empty(go.transform, "Visual", new Vector3(0f, 0.75f, 0f)).transform;
            c.visual = vis;
            var spinRoot = Shapes.Empty(vis, "Spin").transform;
            var spinner = spinRoot.gameObject.AddComponent<Spinner>();
            spinner.speed = kind == PickupKind.Coin ? 180f : 70f;
            var bob = vis.gameObject.AddComponent<Bobber>();
            bob.amplitude = 0.12f;

            bool negative = (int)kind >= 20;
            if (kind != PickupKind.Coin) BuildPedestal(go.transform, negative ? NegativeRing : PositiveRing);

            switch (kind)
            {
                case PickupKind.Component:
                    {
                        var icon = WeaponAssembler.BuildComponentIcon(spinRoot, component).transform;
                        icon.localScale = Vector3.one * 1.1f;
                        icon.localRotation = Quaternion.Euler(0f, 35f, 0f);
                        var def = ComponentCatalog.Get(component);
                        Label(go.transform, def.displayName, GameAssets.Hex(def.colorHex), 1.95f);
                        break;
                    }
                case PickupKind.Coin:
                    {
                        var coin = Shapes.CylinderY(spinRoot, Vector3.zero, 0.6f, 0.1f, GameAssets.Glow(new Color(1f, 0.78f, 0.15f), 0.9f), "Coin");
                        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        Shapes.CylinderY(coin.transform, Vector3.zero, 0.65f, 1.02f, GameAssets.Lit(new Color(1f, 0.65f, 0.05f), 0.8f, 0.9f), "Inner");
                        Shapes.Box(coin.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.12f, 0.2f, 0.35f), GameAssets.Glow(new Color(1f, 0.95f, 0.6f), 1.2f), "Mark");
                        break;
                    }
                case PickupKind.UpgradeModule:
                    Shapes.Box(spinRoot, Vector3.zero, Vector3.one * 0.55f, GameAssets.Lit(new Color(0.15f, 0.2f, 0.18f), 0.7f, 0.7f));
                    Shapes.Box(spinRoot, Vector3.zero, new Vector3(0.6f, 0.14f, 0.14f), GameAssets.Glow(new Color(0.3f, 1f, 0.5f), 2.2f));
                    Shapes.Box(spinRoot, Vector3.zero, new Vector3(0.14f, 0.6f, 0.14f), GameAssets.Glow(new Color(0.3f, 1f, 0.5f), 2.2f));
                    Shapes.Box(spinRoot, Vector3.zero, new Vector3(0.14f, 0.14f, 0.6f), GameAssets.Glow(new Color(0.3f, 1f, 0.5f), 2.2f));
                    Label(go.transform, "+POWER", new Color(0.4f, 1f, 0.55f), 1.7f);
                    break;
                case PickupKind.DamageBooster:
                    Orb(spinRoot, new Color(1f, 0.3f, 0.25f));
                    Label(go.transform, "+DAMAGE", new Color(1f, 0.45f, 0.4f), 1.7f);
                    break;
                case PickupKind.FireRateBooster:
                    Orb(spinRoot, new Color(1f, 0.8f, 0.15f));
                    Label(go.transform, "+FIRE RATE", new Color(1f, 0.85f, 0.3f), 1.7f);
                    break;
                case PickupKind.UpgradeBox:
                    {
                        var crate = Shapes.Box(spinRoot, Vector3.zero, Vector3.one * 0.75f, GameAssets.Lit(new Color(0.95f, 0.65f, 0.15f), 0.4f, 0.2f), "Crate");
                        var edge = GameAssets.Glow(new Color(1f, 0.95f, 0.5f), 2f);
                        Shapes.Box(crate.transform, Vector3.zero, new Vector3(1.04f, 0.12f, 1.04f), edge);
                        Shapes.Box(crate.transform, Vector3.zero, new Vector3(0.12f, 1.04f, 1.04f), edge);
                        var q = Shapes.Text3D(spinRoot, "?", new Vector3(0f, 0f, -0.39f), 0.12f, new Color(0.3f, 0.15f, 0f));
                        q.transform.localRotation = Quaternion.identity;
                        Label(go.transform, "UPGRADE BOX", new Color(1f, 0.85f, 0.3f), 1.9f);
                        break;
                    }
                case PickupKind.RepairKit:
                    Shapes.Box(spinRoot, Vector3.zero, new Vector3(0.7f, 0.5f, 0.4f), GameAssets.Lit(Color.white, 0.6f, 0.1f));
                    Shapes.Box(spinRoot, new Vector3(0f, 0f, -0.21f), new Vector3(0.4f, 0.12f, 0.02f), GameAssets.Glow(new Color(0.2f, 1f, 0.4f), 2f));
                    Shapes.Box(spinRoot, new Vector3(0f, 0f, -0.21f), new Vector3(0.12f, 0.4f, 0.02f), GameAssets.Glow(new Color(0.2f, 1f, 0.4f), 2f));
                    Label(go.transform, "REPAIR", new Color(0.4f, 1f, 0.6f), 1.6f);
                    break;

                case PickupKind.BrokenPart:
                    BrokenIcon(spinRoot, new Color(0.45f, 0.45f, 0.48f));
                    Label(go.transform, "BROKEN PART", NegativeRing, 1.7f);
                    break;
                case PickupKind.RustedPart:
                    BrokenIcon(spinRoot, new Color(0.55f, 0.28f, 0.12f));
                    Label(go.transform, "RUST", new Color(0.95f, 0.5f, 0.25f), 1.7f);
                    break;
                case PickupKind.DamageReducer:
                    Orb(spinRoot, new Color(0.35f, 0.05f, 0.08f));
                    Shapes.Box(spinRoot, new Vector3(0f, 0f, -0.36f), new Vector3(0.45f, 0.12f, 0.02f), GameAssets.Glow(NegativeRing, 2f));
                    Label(go.transform, "-DAMAGE", NegativeRing, 1.7f);
                    break;
                case PickupKind.WeightPenalty:
                    {
                        var w = Shapes.Box(spinRoot, new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.45f, 0.5f), GameAssets.Lit(new Color(0.2f, 0.2f, 0.22f), 0.3f, 0.6f), "Weight");
                        Shapes.CylinderX(spinRoot, new Vector3(0f, 0.25f, 0f), 0.12f, 0.5f, GameAssets.Lit(new Color(0.3f, 0.3f, 0.32f)));
                        Shapes.Text3D(w.transform, "KG", new Vector3(0f, 0f, -0.52f), 0.15f, NegativeRing);
                        Label(go.transform, "HEAVY", NegativeRing, 1.7f);
                        break;
                    }
                case PickupKind.ExplosiveHazard:
                    {
                        spinner.speed = 0f;
                        var barrel = Shapes.CylinderY(spinRoot, new Vector3(0f, -0.05f, 0f), 0.65f, 0.95f, GameAssets.Lit(new Color(0.85f, 0.12f, 0.1f), 0.5f, 0.3f), "Barrel");
                        Shapes.CylinderY(spinRoot, new Vector3(0f, 0.1f, 0f), 0.67f, 0.18f, GameAssets.HazardMaterial, "Stripe");
                        var light = Shapes.Sphere(spinRoot, new Vector3(0f, 0.5f, 0f), 0.18f, GameAssets.Glow(new Color(1f, 0.2f, 0.1f), 3f), "Blink");
                        light.AddComponent<Blinker>().interval = 0.25f;
                        Label(go.transform, "DANGER", NegativeRing, 1.8f);
                        break;
                    }
            }
            return c;
        }

        static void BuildPedestal(Transform root, Color color)
        {
            var ring = Shapes.CylinderY(root, new Vector3(0f, 0.03f, 0f), 1.3f, 0.04f, GameAssets.Glow(color, 1.6f), "Ring");
            ring.AddComponent<Pulser>().amount = 0.08f;
            Shapes.CylinderY(root, new Vector3(0f, 0.04f, 0f), 1.1f, 0.05f, GameAssets.Lit(new Color(0.12f, 0.13f, 0.16f)), "RingInner");
            // vertical light beam to make pickups readable from far away
            var beam = Shapes.CylinderY(root, new Vector3(0f, 1.2f, 0f), 0.06f, 2.4f, GameAssets.Glow(color, 1.1f), "Beam");
            beam.AddComponent<Pulser>().amount = 0.2f;
        }

        static void Orb(Transform parent, Color c)
        {
            Shapes.Sphere(parent, Vector3.zero, 0.65f, GameAssets.Glow(c, 1.6f), "Orb");
            Shapes.CylinderY(parent, Vector3.zero, 0.85f, 0.05f, GameAssets.Lit(new Color(0.15f, 0.15f, 0.18f), 0.7f, 0.8f), "OrbRing");
        }

        static void BrokenIcon(Transform parent, Color c)
        {
            var mat = GameAssets.Lit(c, 0.15f, 0.4f);
            var a = Shapes.CylinderZ(parent, new Vector3(-0.12f, 0f, 0f), 0.22f, 0.5f, mat);
            a.transform.localRotation = Quaternion.Euler(90f, 25f, 0f);
            var b = Shapes.CylinderZ(parent, new Vector3(0.15f, -0.05f, 0.05f), 0.22f, 0.4f, mat);
            b.transform.localRotation = Quaternion.Euler(70f, -40f, 0f);
            Shapes.Box(parent, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.08f, 0.08f), GameAssets.Glow(NegativeRing, 2f)).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Shapes.Box(parent, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.08f, 0.08f), GameAssets.Glow(NegativeRing, 2f)).transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        static void Label(Transform root, string text, Color color, float height)
        {
            var tm = Shapes.Text3D(root, text, new Vector3(0f, height, 0f), 0.045f, color);
            tm.gameObject.AddComponent<Billboard>();
        }
    }
}
