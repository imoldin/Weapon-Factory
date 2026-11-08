using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Weapons
{
    /// <summary>Result of building a weapon model: the points other systems need.</summary>
    public class WeaponRig
    {
        public Transform root;
        public readonly List<Transform> muzzles = new List<Transform>();
        public Transform spinner;      // minigun barrel cluster
        public LineRenderer laser;     // laser sight beam
        public Transform recoilPivot;  // moves back on each shot
        public float length;           // approximate model length (for colliders / camera)
    }

    /// <summary>
    /// Procedurally builds the weapon from primitives: one of 8 evolution base models plus every
    /// collected component as a visible attachment. The visible barrel count always equals the
    /// number of projectiles per shot, so "+1 BARREL" literally adds a barrel.
    /// </summary>
    public static class WeaponAssembler
    {
        struct Layout
        {
            public float muzzleZ;       // where the barrel currently ends
            public float barrelY;
            public float barrelDiameter;
            public float barrelStartZ;  // where extra barrels start
            public List<float> mainBarrelX;
            public float topY, topZ;    // top rail (scope)
            public float underY;        // bottom of the receiver (magazine)
            public float magZ;
            public float rearZ;         // back of receiver (stock)
            public float gripZ;         // foregrip position
            public float bodyWidth;
            public bool hasMagazineWell;
        }

        class Palette
        {
            public Material body, accent, metal, dark, glow, tierGlow;
            public Color glowColor, tierColor;
        }

        public static WeaponRig Build(Transform parent, WeaponModel model, WeaponBuild build, WeaponStats stats,
            RunModifiers run, WeaponSkinDefinition skin, ComponentType? highlight = null)
        {
            var rig = new WeaponRig();
            var root = Shapes.Empty(parent, "WeaponModel").transform;
            rig.root = root;
            var recoil = Shapes.Empty(root, "Recoil").transform;
            rig.recoilPivot = recoil;

            var tierDef = WeaponEvolution.GetTier(stats.tier);
            var pal = new Palette();
            Color bodyCol = GameAssets.Hex(skin.bodyHex);
            pal.tierColor = GameAssets.Hex(tierDef.colorHex);
            pal.glowColor = GameAssets.Hex(skin.glowHex);
            pal.body = GameAssets.Lit(bodyCol, 0.55f, 0.35f);
            pal.accent = GameAssets.Lit(GameAssets.Hex(skin.accentHex), 0.35f, 0.2f);
            pal.metal = GameAssets.Lit(new Color(0.62f, 0.65f, 0.7f), 0.75f, 0.85f);
            pal.dark = GameAssets.Lit(new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.3f);
            pal.glow = GameAssets.Glow(pal.glowColor, 1.8f);
            pal.tierGlow = GameAssets.Glow(pal.tierColor, 1.5f);

            bool hasMagPart = build.GetLevel(ComponentType.Magazine) > 0 || build.GetLevel(ComponentType.ExtendedMagazine) > 0;
            var L = BuildBase(recoil, model, pal, rig, hasMagPart);

            // ---- components (each visibly changes the weapon) ----
            int lvl;
            if ((lvl = build.GetLevel(ComponentType.Barrel)) > 0)
                AddBarrelExtension(recoil, ref L, lvl, pal, Mark(highlight, ComponentType.Barrel));
            if ((lvl = build.GetLevel(ComponentType.ExtendedBarrel)) > 0)
                AddShroud(recoil, ref L, lvl, pal, Mark(highlight, ComponentType.ExtendedBarrel));

            // Main barrels muzzles
            var muzzleXs = new List<float>(L.mainBarrelX);
            // Extra barrels so visible barrels == projectiles per shot
            int extra = stats.projectilesPerShot - L.mainBarrelX.Count;
            bool doubleHighlight = Mark(highlight, ComponentType.DoubleBarrel);
            for (int i = 0; i < extra; i++)
            {
                int side = (i % 2 == 0) ? 1 : -1;
                int ring = i / 2 + 1;
                float x = side * (L.bodyWidth * 0.5f + L.barrelDiameter * 0.75f * ring + 0.02f);
                float y = L.barrelY - 0.02f * ring;
                var b = Shapes.CylinderZ(recoil, new Vector3(x, y, (L.barrelStartZ + L.muzzleZ) * 0.5f), L.barrelDiameter * 0.9f,
                    L.muzzleZ - L.barrelStartZ, pal.metal, "ExtraBarrel");
                Shapes.Box(recoil, new Vector3(x * 0.75f, y, L.barrelStartZ + 0.1f), new Vector3(Mathf.Abs(x) * 0.6f, 0.06f, 0.12f), pal.accent, "BarrelMount");
                Shapes.CylinderZ(recoil, new Vector3(x, y, L.muzzleZ - 0.05f), L.barrelDiameter * 1.15f, 0.08f, pal.tierGlow, "ExtraBarrelRing");
                if (doubleHighlight) PopIn(b.transform);
                muzzleXs.Add(x);
            }

            if ((lvl = build.GetLevel(ComponentType.Muzzle)) > 0)
                AddMuzzleBrakes(recoil, ref L, muzzleXs, lvl, pal, Mark(highlight, ComponentType.Muzzle));
            if ((lvl = build.GetLevel(ComponentType.Magazine)) > 0 && build.GetLevel(ComponentType.ExtendedMagazine) == 0)
                AddMagazine(recoil, L, lvl, pal, Mark(highlight, ComponentType.Magazine));
            if ((lvl = build.GetLevel(ComponentType.ExtendedMagazine)) > 0)
                AddDrum(recoil, L, lvl, pal, Mark(highlight, ComponentType.ExtendedMagazine), build.GetLevel(ComponentType.Magazine));
            if ((lvl = build.GetLevel(ComponentType.Stock)) > 0)
                AddStock(recoil, L, lvl, pal, Mark(highlight, ComponentType.Stock));
            if ((lvl = build.GetLevel(ComponentType.Scope)) > 0)
                AddScope(recoil, L, lvl, pal, Mark(highlight, ComponentType.Scope));
            if ((lvl = build.GetLevel(ComponentType.Grip)) > 0)
                AddGrip(recoil, L, lvl, pal, Mark(highlight, ComponentType.Grip));
            if ((lvl = build.GetLevel(ComponentType.LaserSight)) > 0)
                AddLaser(recoil, L, lvl, pal, rig, stats.range, Mark(highlight, ComponentType.LaserSight));

            // Damage boosts from gates/boosters show as glowing power coils.
            float boost = run != null ? run.damageMultiplier : 1f;
            int coils = boost >= 2.5f ? 3 : boost >= 1.7f ? 2 : boost >= 1.25f ? 1 : 0;
            for (int i = 0; i < coils; i++)
            {
                float z = Mathf.Lerp(L.rearZ + 0.15f, L.barrelStartZ, (i + 1f) / (coils + 1f));
                var coil = Shapes.CylinderZ(recoil, new Vector3(0f, L.barrelY - 0.02f, z), L.bodyWidth * 1.25f, 0.05f, GameAssets.Glow(new Color(1f, 0.35f, 0.1f), 2.2f), "PowerCoil");
                coil.AddComponent<Pulser>().amount = 0.06f;
            }

            // Muzzle transforms (projectile spawn points)
            for (int i = 0; i < muzzleXs.Count; i++)
            {
                var m = Shapes.Empty(recoil, "Muzzle" + i, new Vector3(muzzleXs[i], L.barrelY, L.muzzleZ + 0.05f)).transform;
                rig.muzzles.Add(m);
            }
            rig.length = L.muzzleZ - L.rearZ;
            return rig;
        }

        static bool Mark(ComponentType? highlight, ComponentType t) { return highlight.HasValue && highlight.Value == t; }

        static void PopIn(Transform t)
        {
            Vector3 s = t.localScale;
            t.localScale = s * 0.01f;
            Tween.Scale(t, s, 0.35f, Ease.OutBack);
        }

        // =====================================================================================
        // Base models
        // =====================================================================================

        static Layout BuildBase(Transform r, WeaponModel model, Palette p, WeaponRig rig, bool hasMagPart)
        {
            var L = new Layout { mainBarrelX = new List<float> { 0f }, hasMagazineWell = true };
            switch (model)
            {
                case WeaponModel.BasicPistol:
                    Shapes.Box(r, new Vector3(0f, 0.1f, 0.05f), new Vector3(0.22f, 0.24f, 0.85f), p.body, "Slide", true);
                    Shapes.Box(r, new Vector3(0f, -0.02f, 0.05f), new Vector3(0.2f, 0.1f, 0.8f), p.accent, "Frame");
                    var g = Shapes.Box(r, new Vector3(0f, -0.28f, -0.25f), new Vector3(0.19f, 0.5f, 0.26f), p.dark, "Grip");
                    g.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
                    Shapes.Box(r, new Vector3(0f, -0.14f, 0.02f), new Vector3(0.05f, 0.12f, 0.18f), p.dark, "TriggerGuard");
                    Shapes.CylinderZ(r, new Vector3(0f, 0.1f, 0.52f), 0.11f, 0.14f, p.metal, "Barrel");
                    Shapes.Box(r, new Vector3(0f, 0.235f, 0.38f), new Vector3(0.04f, 0.04f, 0.05f), p.tierGlow, "Sight");
                    L.muzzleZ = 0.59f; L.barrelY = 0.1f; L.barrelDiameter = 0.11f; L.barrelStartZ = 0.3f;
                    L.topY = 0.24f; L.topZ = 0.05f; L.underY = -0.07f; L.magZ = -0.25f; L.rearZ = -0.38f; L.gripZ = 0.3f; L.bodyWidth = 0.22f;
                    break;

                case WeaponModel.ModifiedPistol:
                    Shapes.Box(r, new Vector3(0f, 0.11f, 0.12f), new Vector3(0.24f, 0.26f, 1.05f), p.body, "Slide", true);
                    Shapes.Box(r, new Vector3(0f, 0.11f, 0.12f), new Vector3(0.25f, 0.05f, 0.9f), p.tierGlow, "GlowStripe");
                    Shapes.Box(r, new Vector3(0f, -0.03f, 0.1f), new Vector3(0.21f, 0.11f, 0.95f), p.accent, "Frame");
                    var g2 = Shapes.Box(r, new Vector3(0f, -0.3f, -0.22f), new Vector3(0.2f, 0.52f, 0.28f), p.dark, "Grip");
                    g2.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
                    Shapes.Box(r, new Vector3(0f, 0.26f, 0.12f), new Vector3(0.08f, 0.04f, 0.8f), p.dark, "Rail");
                    Shapes.CylinderZ(r, new Vector3(0f, 0.11f, 0.72f), 0.12f, 0.2f, p.metal, "Barrel");
                    Shapes.Box(r, new Vector3(0f, 0.11f, 0.67f), new Vector3(0.27f, 0.16f, 0.12f), p.accent, "Compensator");
                    L.muzzleZ = 0.82f; L.barrelY = 0.11f; L.barrelDiameter = 0.12f; L.barrelStartZ = 0.4f;
                    L.topY = 0.28f; L.topZ = 0.12f; L.underY = -0.08f; L.magZ = -0.22f; L.rearZ = -0.4f; L.gripZ = 0.42f; L.bodyWidth = 0.24f;
                    break;

                case WeaponModel.DoubleBarrelPistol:
                    Shapes.Box(r, new Vector3(0f, 0.12f, 0.12f), new Vector3(0.38f, 0.28f, 1.05f), p.body, "Body", true);
                    Shapes.Box(r, new Vector3(0f, 0.27f, 0.12f), new Vector3(0.1f, 0.04f, 0.85f), p.tierGlow, "Rib");
                    var g3 = Shapes.Box(r, new Vector3(0f, -0.3f, -0.22f), new Vector3(0.22f, 0.54f, 0.3f), p.dark, "Grip");
                    g3.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
                    Shapes.CylinderZ(r, new Vector3(-0.09f, 0.12f, 0.78f), 0.13f, 0.32f, p.metal, "BarrelL");
                    Shapes.CylinderZ(r, new Vector3(0.09f, 0.12f, 0.78f), 0.13f, 0.32f, p.metal, "BarrelR");
                    Shapes.Box(r, new Vector3(0f, 0.12f, 0.66f), new Vector3(0.36f, 0.06f, 0.06f), p.accent, "Clamp");
                    L.mainBarrelX = new List<float> { -0.09f, 0.09f };
                    L.muzzleZ = 0.94f; L.barrelY = 0.12f; L.barrelDiameter = 0.13f; L.barrelStartZ = 0.45f;
                    L.topY = 0.3f; L.topZ = 0.1f; L.underY = -0.04f; L.magZ = -0.22f; L.rearZ = -0.42f; L.gripZ = 0.45f; L.bodyWidth = 0.38f;
                    break;

                case WeaponModel.AssaultRifle:
                case WeaponModel.AdvancedRifle:
                    {
                        bool adv = model == WeaponModel.AdvancedRifle;
                        float w = adv ? 0.28f : 0.25f;
                        Shapes.Box(r, new Vector3(0f, 0.08f, 0f), new Vector3(w, 0.32f, 1.25f), p.body, "Receiver", true);
                        Shapes.Box(r, new Vector3(0f, 0.08f, 0.85f), new Vector3(w * 0.85f, 0.24f, 0.6f), p.accent, "Handguard");
                        for (int i = 0; i < 3; i++)
                            Shapes.Box(r, new Vector3(0f, 0.08f, 0.66f + i * 0.17f), new Vector3(w * 0.9f, 0.05f, 0.05f), p.tierGlow, "Vent");
                        Shapes.Box(r, new Vector3(0f, 0.27f, 0.25f), new Vector3(0.09f, 0.05f, 1.2f), p.dark, "Rail");
                        Shapes.CylinderZ(r, new Vector3(0f, 0.1f, 1.45f), 0.09f, 0.55f, p.metal, "Barrel");
                        var pg = Shapes.Box(r, new Vector3(0f, -0.22f, -0.22f), new Vector3(0.17f, 0.42f, 0.22f), p.dark, "PistolGrip");
                        pg.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                        // built-in short stock
                        Shapes.Box(r, new Vector3(0f, 0.02f, -0.85f), new Vector3(w * 0.8f, 0.26f, 0.5f), p.accent, "StockBase");
                        if (adv)
                        {
                            Shapes.Box(r, new Vector3(w * 0.5f + 0.005f, 0.12f, 0.1f), new Vector3(0.02f, 0.06f, 1.0f), p.glow, "SideGlowR");
                            Shapes.Box(r, new Vector3(-w * 0.5f - 0.005f, 0.12f, 0.1f), new Vector3(0.02f, 0.06f, 1.0f), p.glow, "SideGlowL");
                            Shapes.Box(r, new Vector3(0f, 0.08f, 1.68f), new Vector3(0.2f, 0.14f, 0.12f), p.accent, "Brake");
                        }
                        L.muzzleZ = adv ? 1.75f : 1.72f; L.barrelY = 0.1f; L.barrelDiameter = 0.09f; L.barrelStartZ = 0.55f;
                        L.topY = 0.3f; L.topZ = 0.15f; L.underY = -0.08f; L.magZ = 0.25f; L.rearZ = -1.1f; L.gripZ = 0.9f; L.bodyWidth = w;
                        // rifles show a basic curved magazine until a magazine part replaces it
                        if (!hasMagPart)
                        {
                            var mag = Shapes.Box(r, new Vector3(0f, -0.3f, 0.28f), new Vector3(0.14f, 0.42f, 0.2f), p.dark, "BaseMag");
                            mag.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                        }
                        break;
                    }

                case WeaponModel.Minigun:
                    {
                        Shapes.CylinderZ(r, new Vector3(0f, 0.1f, -0.15f), 0.52f, 0.9f, p.body, "Housing");
                        Shapes.CylinderZ(r, new Vector3(0f, 0.1f, -0.15f), 0.56f, 0.12f, p.tierGlow, "HousingRing");
                        Shapes.Box(r, new Vector3(0f, 0.45f, -0.15f), new Vector3(0.08f, 0.2f, 0.5f), p.dark, "Handle");
                        Shapes.Box(r, new Vector3(0.38f, -0.05f, -0.2f), new Vector3(0.24f, 0.32f, 0.45f), p.accent, "AmmoBox");
                        Shapes.Box(r, new Vector3(0.24f, 0.05f, 0f), new Vector3(0.12f, 0.06f, 0.3f), p.metal, "Belt");
                        var spin = Shapes.Empty(r, "BarrelCluster", new Vector3(0f, 0.1f, 0.3f)).transform;
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * Mathf.PI / 3f;
                            Shapes.CylinderZ(spin, new Vector3(Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.13f, 0.65f), 0.08f, 1.3f, p.metal, "MiniBarrel");
                        }
                        Shapes.CylinderZ(spin, new Vector3(0f, 0f, 0.2f), 0.4f, 0.06f, p.accent, "Plate1");
                        Shapes.CylinderZ(spin, new Vector3(0f, 0f, 1.15f), 0.4f, 0.06f, p.accent, "Plate2");
                        rig.spinner = spin;
                        L.muzzleZ = 1.6f; L.barrelY = 0.1f; L.barrelDiameter = 0.16f; L.barrelStartZ = 0.4f;
                        L.topY = 0.38f; L.topZ = -0.15f; L.underY = -0.18f; L.magZ = -0.2f; L.rearZ = -0.62f; L.gripZ = 0.6f; L.bodyWidth = 0.52f;
                        L.hasMagazineWell = false;
                        break;
                    }

                case WeaponModel.HeavyCannon:
                    {
                        Shapes.Box(r, new Vector3(0f, 0.1f, -0.2f), new Vector3(0.48f, 0.44f, 1.0f), p.body, "Body", true);
                        Shapes.CylinderZ(r, new Vector3(0f, 0.12f, 0.85f), 0.36f, 1.2f, p.metal, "BigBarrel");
                        for (int i = 0; i < 3; i++)
                        {
                            var coil = Shapes.CylinderZ(r, new Vector3(0f, 0.12f, 0.5f + i * 0.3f), 0.44f, 0.07f, p.tierGlow, "Coil");
                            coil.AddComponent<Pulser>().frequency = 2f;
                        }
                        Shapes.CylinderZ(r, new Vector3(0f, 0.12f, 1.5f), 0.46f, 0.16f, p.accent, "MuzzleCap");
                        Shapes.Box(r, new Vector3(0f, 0.38f, -0.2f), new Vector3(0.1f, 0.12f, 0.7f), p.dark, "TopHandle");
                        var hg = Shapes.Box(r, new Vector3(0f, -0.28f, -0.35f), new Vector3(0.18f, 0.4f, 0.22f), p.dark, "Grip");
                        hg.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                        Shapes.Box(r, new Vector3(0.26f, 0.15f, -0.2f), new Vector3(0.04f, 0.18f, 0.8f), p.glow, "SideCell");
                        Shapes.Box(r, new Vector3(-0.26f, 0.15f, -0.2f), new Vector3(0.04f, 0.18f, 0.8f), p.glow, "SideCell");
                        L.muzzleZ = 1.6f; L.barrelY = 0.12f; L.barrelDiameter = 0.3f; L.barrelStartZ = 0.3f;
                        L.topY = 0.44f; L.topZ = -0.2f; L.underY = -0.12f; L.magZ = -0.15f; L.rearZ = -0.7f; L.gripZ = 0.55f; L.bodyWidth = 0.48f;
                        break;
                    }

                case WeaponModel.RocketLauncher:
                    {
                        Shapes.CylinderZ(r, new Vector3(0f, 0.15f, 0.2f), 0.46f, 1.9f, p.body, "Tube", true);
                        Shapes.CylinderZ(r, new Vector3(0f, 0.15f, 1.15f), 0.54f, 0.14f, p.accent, "FrontRing");
                        Shapes.CylinderZ(r, new Vector3(0f, 0.15f, -0.75f), 0.56f, 0.2f, p.accent, "RearRing");
                        Shapes.CylinderZ(r, new Vector3(0f, 0.15f, 0.2f), 0.48f, 0.08f, p.tierGlow, "MidStripe");
                        var tip = Shapes.Create(PrimitiveType.Sphere, r, new Vector3(0f, 0.15f, 1.12f), new Vector3(0.3f, 0.3f, 0.36f), GameAssets.Glow(new Color(1f, 0.3f, 0.2f), 1.4f), "RocketTip");
                        tip.AddComponent<Pulser>().amount = 0.05f;
                        Shapes.Box(r, new Vector3(0f, -0.22f, 0.05f), new Vector3(0.16f, 0.36f, 0.2f), p.dark, "Grip");
                        Shapes.Box(r, new Vector3(0f, -0.2f, 0.55f), new Vector3(0.14f, 0.3f, 0.16f), p.dark, "FrontGrip");
                        Shapes.Box(r, new Vector3(-0.3f, 0.32f, 0.1f), new Vector3(0.08f, 0.16f, 0.3f), p.dark, "SightBox");
                        Shapes.Box(r, new Vector3(-0.3f, 0.32f, 0.26f), new Vector3(0.06f, 0.1f, 0.02f), p.glow, "SightLens");
                        L.muzzleZ = 1.2f; L.barrelY = 0.15f; L.barrelDiameter = 0.4f; L.barrelStartZ = -0.6f;
                        L.topY = 0.4f; L.topZ = 0.1f; L.underY = -0.1f; L.magZ = -0.3f; L.rearZ = -0.85f; L.gripZ = 0.7f; L.bodyWidth = 0.46f;
                        L.hasMagazineWell = false;
                        break;
                    }
            }
            return L;
        }

        // =====================================================================================
        // Component attachments
        // =====================================================================================

        static void AddBarrelExtension(Transform r, ref Layout L, int lvl, Palette p, bool pop)
        {
            float len = 0.18f + 0.12f * lvl;
            foreach (float x in L.mainBarrelX)
            {
                var b = Shapes.CylinderZ(r, new Vector3(x, L.barrelY, L.muzzleZ + len * 0.5f), L.barrelDiameter * 0.95f, len, p.metal, "BarrelExt");
                Shapes.CylinderZ(r, new Vector3(x, L.barrelY, L.muzzleZ + 0.02f), L.barrelDiameter * 1.2f, 0.05f, GameAssets.Glow(GameAssets.Hex("FFB300"), 1.4f), "BarrelJoint");
                if (pop) PopIn(b.transform);
            }
            L.muzzleZ += len;
        }

        static void AddShroud(Transform r, ref Layout L, int lvl, Palette p, bool pop)
        {
            float len = 0.3f + 0.14f * lvl;
            float d = L.barrelDiameter * 1.9f;
            foreach (float x in L.mainBarrelX)
            {
                var s = Shapes.CylinderZ(r, new Vector3(x, L.barrelY, L.muzzleZ + len * 0.5f), d, len, p.accent, "Shroud");
                int vents = 2 + lvl;
                for (int i = 0; i < vents; i++)
                    Shapes.CylinderZ(r, new Vector3(x, L.barrelY, L.muzzleZ + len * (i + 0.5f) / vents), d * 1.08f, 0.03f, GameAssets.Glow(GameAssets.Hex("FF8F00"), 1.6f), "ShroudVent");
                if (pop) PopIn(s.transform);
            }
            L.muzzleZ += len;
            L.gripZ = Mathf.Max(L.gripZ, L.muzzleZ - len);
        }

        static void AddMuzzleBrakes(Transform r, ref Layout L, List<float> xs, int lvl, Palette p, bool pop)
        {
            float size = L.barrelDiameter * (1.5f + 0.25f * lvl);
            float len = 0.12f + 0.05f * lvl;
            var glow = GameAssets.Glow(GameAssets.Hex("FFEA00"), 1.8f);
            foreach (float x in xs)
            {
                var b = Shapes.Box(r, new Vector3(x, L.barrelY, L.muzzleZ + len * 0.5f), new Vector3(size, size * 0.8f, len), p.dark, "MuzzleBrake");
                Shapes.Box(r, new Vector3(x, L.barrelY, L.muzzleZ + len * 0.5f), new Vector3(size * 1.05f, size * 0.25f, len * 0.6f), glow, "BrakePort");
                if (pop) PopIn(b.transform);
            }
            L.muzzleZ += len;
        }

        static void AddMagazine(Transform r, Layout L, int lvl, Palette p, bool pop)
        {
            float h = 0.28f + 0.12f * lvl;
            var m = Shapes.Box(r, new Vector3(0f, L.underY - h * 0.5f, L.magZ), new Vector3(0.15f, h, 0.2f), p.dark, "Magazine");
            m.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Shapes.Box(m.transform, new Vector3(0f, -0.2f, 0f), new Vector3(1.08f, 0.15f, 1.08f), GameAssets.Glow(GameAssets.Hex("40C4FF"), 1.5f), "MagBand");
            if (lvl >= 3) Shapes.Box(m.transform, new Vector3(0f, 0.2f, 0f), new Vector3(1.08f, 0.1f, 1.08f), GameAssets.Glow(GameAssets.Hex("40C4FF"), 1.5f), "MagBand2");
            if (pop) PopIn(m.transform);
        }

        static void AddDrum(Transform r, Layout L, int lvl, Palette p, bool pop, int magLevel)
        {
            float d = 0.36f + 0.08f * lvl + 0.03f * magLevel;
            var drum = Shapes.CylinderX(r, new Vector3(0f, L.underY - d * 0.45f, L.magZ), d, 0.22f, p.dark, "DrumMag");
            Shapes.CylinderX(r, new Vector3(0f, L.underY - d * 0.45f, L.magZ), d * 0.55f, 0.24f, GameAssets.Glow(GameAssets.Hex("00B0FF"), 1.6f), "DrumCore");
            if (pop) PopIn(drum.transform);
        }

        static void AddStock(Transform r, Layout L, int lvl, Palette p, bool pop)
        {
            float len = 0.32f + 0.12f * lvl;
            var s = Shapes.Box(r, new Vector3(0f, L.barrelY - 0.06f, L.rearZ - len * 0.5f), new Vector3(0.12f, 0.12f, len), p.accent, "StockArm");
            Shapes.Box(r, new Vector3(0f, L.barrelY - 0.12f, L.rearZ - len), new Vector3(0.16f, 0.34f + 0.04f * lvl, 0.1f), p.dark, "ButtPlate");
            Shapes.Box(r, new Vector3(0f, L.barrelY - 0.06f, L.rearZ - len * 0.5f), new Vector3(0.13f, 0.03f, len * 0.8f), GameAssets.Glow(GameAssets.Hex("FFAB91"), 1.2f), "StockStripe");
            if (pop) PopIn(s.transform);
        }

        static void AddScope(Transform r, Layout L, int lvl, Palette p, bool pop)
        {
            float len = 0.38f + 0.1f * lvl;
            float d = 0.13f + 0.025f * lvl;
            float y = L.topY + d * 0.5f + 0.06f;
            var s = Shapes.CylinderZ(r, new Vector3(0f, y, L.topZ), d, len, p.dark, "Scope");
            Shapes.Box(r, new Vector3(0f, L.topY + 0.03f, L.topZ - len * 0.25f), new Vector3(0.06f, 0.07f, 0.05f), p.metal, "Mount");
            Shapes.Box(r, new Vector3(0f, L.topY + 0.03f, L.topZ + len * 0.25f), new Vector3(0.06f, 0.07f, 0.05f), p.metal, "Mount");
            Shapes.CylinderZ(r, new Vector3(0f, y, L.topZ + len * 0.5f), d * 0.85f, 0.02f, GameAssets.Glow(GameAssets.Hex("69F0AE"), 2.2f), "Lens");
            Shapes.CylinderZ(r, new Vector3(0f, y, L.topZ - len * 0.5f), d * 1.15f, 0.06f, p.accent, "EyePiece");
            if (pop) PopIn(s.transform);
        }

        static void AddGrip(Transform r, Layout L, int lvl, Palette p, bool pop)
        {
            float h = 0.22f + 0.07f * lvl;
            float z = Mathf.Min(L.gripZ, L.muzzleZ - 0.2f);
            var g = Shapes.Box(r, new Vector3(0f, L.barrelY - 0.12f - h * 0.5f, z), new Vector3(0.1f, h, 0.12f), p.dark, "ForeGrip");
            Shapes.Box(g.transform, new Vector3(0f, -0.35f, 0f), new Vector3(1.1f, 0.12f, 1.1f), GameAssets.Glow(GameAssets.Hex("B388FF"), 1.5f), "GripCap");
            if (pop) PopIn(g.transform);
        }

        static void AddLaser(Transform r, Layout L, int lvl, Palette p, WeaponRig rig, float range, bool pop)
        {
            float x = L.bodyWidth * 0.5f + 0.06f;
            float z = Mathf.Max(L.barrelStartZ, L.muzzleZ - 0.5f);
            var box = Shapes.Box(r, new Vector3(x, L.barrelY - 0.04f, z), new Vector3(0.08f, 0.08f, 0.22f), p.dark, "LaserModule");
            var red = new Color(1f, 0.1f, 0.15f);
            Shapes.Sphere(r, new Vector3(x, L.barrelY - 0.04f, z + 0.12f), 0.05f, GameAssets.Glow(red, 3f), "LaserDot");
            var beamGo = Shapes.Empty(r, "LaserBeam", new Vector3(x, L.barrelY - 0.04f, z + 0.12f));
            var lr = beamGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, new Vector3(0f, 0f, Mathf.Clamp(range * 0.5f, 4f, 14f)));
            float w = 0.025f + 0.01f * lvl;
            lr.startWidth = w;
            lr.endWidth = w * 0.3f;
            lr.sharedMaterial = GameAssets.ParticleAdditive;
            lr.startColor = new Color(1f, 0.15f, 0.2f, 0.9f);
            lr.endColor = new Color(1f, 0.15f, 0.2f, 0f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            rig.laser = lr;
            if (pop) PopIn(box.transform);
        }

        // =====================================================================================
        // Pickup visuals (shared with collectibles so pickups look like the actual part)
        // =====================================================================================

        public static GameObject BuildComponentIcon(Transform parent, ComponentType type)
        {
            var def = ComponentCatalog.Get(type);
            Color c = GameAssets.Hex(def.colorHex);
            var root = Shapes.Empty(parent, "Part_" + type).transform;
            var body = GameAssets.Lit(new Color(0.22f, 0.24f, 0.28f), 0.6f, 0.6f);
            var glow = GameAssets.Glow(c, 2f);
            var metal = GameAssets.Lit(new Color(0.7f, 0.72f, 0.78f), 0.8f, 0.9f);
            switch (type)
            {
                case ComponentType.Barrel:
                    Shapes.CylinderZ(root, Vector3.zero, 0.22f, 0.9f, metal);
                    Shapes.CylinderZ(root, new Vector3(0, 0, 0.42f), 0.3f, 0.08f, glow);
                    Shapes.CylinderZ(root, new Vector3(0, 0, -0.42f), 0.3f, 0.08f, glow);
                    break;
                case ComponentType.ExtendedBarrel:
                    Shapes.CylinderZ(root, Vector3.zero, 0.36f, 1.0f, body);
                    for (int i = 0; i < 4; i++) Shapes.CylinderZ(root, new Vector3(0, 0, -0.36f + i * 0.24f), 0.4f, 0.04f, glow);
                    break;
                case ComponentType.DoubleBarrel:
                    Shapes.CylinderZ(root, new Vector3(-0.14f, 0, 0), 0.22f, 0.9f, metal);
                    Shapes.CylinderZ(root, new Vector3(0.14f, 0, 0), 0.22f, 0.9f, metal);
                    Shapes.Box(root, Vector3.zero, new Vector3(0.5f, 0.1f, 0.12f), glow);
                    break;
                case ComponentType.Magazine:
                    Shapes.Box(root, Vector3.zero, new Vector3(0.26f, 0.7f, 0.36f), body);
                    Shapes.Box(root, new Vector3(0, -0.2f, 0), new Vector3(0.28f, 0.1f, 0.38f), glow);
                    Shapes.Box(root, new Vector3(0, 0.38f, 0.05f), new Vector3(0.14f, 0.06f, 0.2f), metal);
                    break;
                case ComponentType.ExtendedMagazine:
                    Shapes.CylinderX(root, Vector3.zero, 0.7f, 0.3f, body);
                    Shapes.CylinderX(root, Vector3.zero, 0.4f, 0.32f, glow);
                    break;
                case ComponentType.Stock:
                    Shapes.Box(root, new Vector3(0, 0.1f, 0.15f), new Vector3(0.18f, 0.18f, 0.6f), body);
                    Shapes.Box(root, new Vector3(0, -0.05f, -0.2f), new Vector3(0.22f, 0.5f, 0.14f), body);
                    Shapes.Box(root, new Vector3(0, 0.1f, 0.15f), new Vector3(0.2f, 0.04f, 0.5f), glow);
                    break;
                case ComponentType.Scope:
                    Shapes.CylinderZ(root, Vector3.zero, 0.26f, 0.8f, body);
                    Shapes.CylinderZ(root, new Vector3(0, 0, 0.41f), 0.22f, 0.03f, glow);
                    Shapes.CylinderZ(root, new Vector3(0, 0, -0.38f), 0.32f, 0.1f, metal);
                    break;
                case ComponentType.LaserSight:
                    Shapes.Box(root, Vector3.zero, new Vector3(0.24f, 0.24f, 0.5f), body);
                    Shapes.Sphere(root, new Vector3(0, 0, 0.28f), 0.14f, GameAssets.Glow(new Color(1f, 0.1f, 0.15f), 3f));
                    break;
                case ComponentType.Grip:
                    Shapes.Box(root, Vector3.zero, new Vector3(0.2f, 0.6f, 0.24f), body);
                    Shapes.Box(root, new Vector3(0, -0.32f, 0), new Vector3(0.24f, 0.1f, 0.28f), glow);
                    break;
                case ComponentType.Muzzle:
                    Shapes.Box(root, Vector3.zero, new Vector3(0.42f, 0.34f, 0.36f), body);
                    Shapes.Box(root, Vector3.zero, new Vector3(0.44f, 0.12f, 0.24f), glow);
                    Shapes.CylinderZ(root, new Vector3(0, 0, -0.3f), 0.18f, 0.25f, metal);
                    break;
            }
            return root.gameObject;
        }
    }
}
