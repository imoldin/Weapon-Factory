using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>Animated parts of a procedurally built robot.</summary>
    public class EnemyParts
    {
        public Transform root, body, head, legL, legR, armL, armR, cannon;
        public readonly List<Transform> rotors = new List<Transform>();
        public readonly List<Transform> cannons = new List<Transform>();
        public Transform core;
    }

    /// <summary>
    /// Builds cartoon factory robots from primitives. Each type has a distinct silhouette and colour:
    /// orange worker robot, grey armoured robot with shield plates, cyan drone, red heavy security, purple boss.
    /// </summary>
    public static class EnemyFactory
    {
        public static EnemyController Spawn(Transform parent, EnemyDefinition def, Vector3 position, float healthMultiplier, string bossName = null)
        {
            var go = new GameObject("Enemy_" + def.type);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the player

            var parts = new EnemyParts { root = go.transform };
            Color main = GameAssets.Hex(def.colorHex);
            switch (def.type)
            {
                case EnemyType.BasicRobot: BuildWorker(parts, main); break;
                case EnemyType.ArmoredRobot: BuildArmored(parts, main); break;
                case EnemyType.Drone: BuildDrone(parts, main); break;
                case EnemyType.HeavySecurity: BuildHeavy(parts, main); break;
                case EnemyType.Boss: BuildBoss(parts, main); break;
            }
            go.transform.localScale = Vector3.one * def.scale;

            var health = go.AddComponent<EnemyHealth>();
            float aimY = def.flying ? 0f : 0.9f * def.scale;
            bool isBoss = def.type == EnemyType.Boss;
            health.Setup(def.maxHealth * healthMultiplier, def.armor, def.hitRadius, new Vector3(0f, aimY, 0f), !isBoss, 1.0f, def.flying ? 0.8f : 2.0f);

            EnemyController ctrl = isBoss ? go.AddComponent<BossController>() : go.AddComponent<EnemyController>();
            ctrl.Init(def, health, parts);
            if (isBoss && !string.IsNullOrEmpty(bossName)) ((BossController)ctrl).BossName = bossName;
            return ctrl;
        }

        static Material Lit(Color c, float s = 0.45f, float m = 0.25f) { return GameAssets.Lit(c, s, m); }
        static readonly Color DarkGrey = new Color(0.16f, 0.17f, 0.2f);

        static void Eyes(Transform head, float spacing, float y, float z, float size, Color color)
        {
            var glow = GameAssets.Glow(color, 2.5f);
            Shapes.Sphere(head, new Vector3(-spacing, y, z), size, glow, "EyeL");
            Shapes.Sphere(head, new Vector3(spacing, y, z), size, glow, "EyeR");
        }

        static void BuildWorker(EnemyParts p, Color main)
        {
            var r = p.root;
            var body = Shapes.Empty(r, "Body", new Vector3(0f, 0.95f, 0f)).transform;
            p.body = body;
            Shapes.Box(body, Vector3.zero, new Vector3(0.8f, 0.75f, 0.55f), Lit(main), "Torso", true);
            Shapes.Box(body, new Vector3(0f, -0.05f, 0.28f), new Vector3(0.45f, 0.3f, 0.04f), Lit(DarkGrey), "Chest");
            Shapes.Box(body, new Vector3(0f, -0.05f, 0.3f), new Vector3(0.3f, 0.06f, 0.02f), GameAssets.Glow(new Color(0.2f, 1f, 0.4f), 1.6f), "Status");
            var head = Shapes.Empty(body, "Head", new Vector3(0f, 0.6f, 0f)).transform;
            p.head = head;
            Shapes.Box(head, Vector3.zero, new Vector3(0.55f, 0.42f, 0.48f), Lit(main * 0.9f + Color.white * 0.1f), "HeadBox", true);
            Shapes.Box(head, new Vector3(0f, 0.02f, 0.24f), new Vector3(0.45f, 0.18f, 0.04f), Lit(DarkGrey), "Visor");
            Eyes(head, 0.11f, 0.02f, 0.27f, 0.11f, new Color(1f, 0.25f, 0.15f));
            Shapes.CylinderY(head, new Vector3(0f, 0.32f, 0f), 0.04f, 0.25f, Lit(DarkGrey), "Antenna");
            Shapes.Sphere(head, new Vector3(0f, 0.47f, 0f), 0.1f, GameAssets.Glow(new Color(1f, 0.8f, 0.2f), 2f), "AntennaTip").AddComponent<Blinker>().interval = 0.5f;
            p.armL = Limb(body, new Vector3(-0.5f, 0.2f, 0f), new Vector3(0.18f, 0.6f, 0.18f), Lit(DarkGrey), Lit(main), "ArmL");
            p.armR = Limb(body, new Vector3(0.5f, 0.2f, 0f), new Vector3(0.18f, 0.6f, 0.18f), Lit(DarkGrey), Lit(main), "ArmR");
            p.legL = Limb(r, new Vector3(-0.22f, 0.6f, 0f), new Vector3(0.22f, 0.6f, 0.22f), Lit(DarkGrey), Lit(main * 0.7f), "LegL");
            p.legR = Limb(r, new Vector3(0.22f, 0.6f, 0f), new Vector3(0.22f, 0.6f, 0.22f), Lit(DarkGrey), Lit(main * 0.7f), "LegR");
        }

        static void BuildArmored(EnemyParts p, Color main)
        {
            BuildWorker(p, main);
            var plate = Lit(new Color(0.42f, 0.46f, 0.52f), 0.7f, 0.85f);
            Shapes.Box(p.body, new Vector3(0f, 0.05f, 0.32f), new Vector3(0.9f, 0.65f, 0.08f), plate, "ChestPlate", true);
            Shapes.Box(p.body, new Vector3(-0.52f, 0.42f, 0f), new Vector3(0.38f, 0.18f, 0.5f), plate, "PauldronL");
            Shapes.Box(p.body, new Vector3(0.52f, 0.42f, 0f), new Vector3(0.38f, 0.18f, 0.5f), plate, "PauldronR");
            Shapes.Box(p.head, new Vector3(0f, 0.24f, 0f), new Vector3(0.62f, 0.12f, 0.55f), plate, "Helmet");
            // riot shield on the left arm
            var shield = Shapes.Box(p.armL, new Vector3(-0.05f, -0.35f, 0.25f), new Vector3(0.12f, 0.9f, 0.6f), plate, "Shield", true);
            shield.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Shapes.Box(shield.transform, new Vector3(0f, 0f, -0.55f), new Vector3(0.9f, 0.08f, 0.1f), GameAssets.Glow(new Color(0.3f, 0.7f, 1f), 2f), "ShieldGlow");
        }

        static void BuildDrone(EnemyParts p, Color main)
        {
            var r = p.root;
            var body = Shapes.Empty(r, "Body", new Vector3(0f, 0f, 0f)).transform;
            p.body = body;
            Shapes.Sphere(body, Vector3.zero, 0.8f, Lit(main, 0.7f, 0.5f), "Shell");
            Shapes.CylinderY(body, Vector3.zero, 0.95f, 0.12f, Lit(DarkGrey), "Ring");
            Shapes.Sphere(body, new Vector3(0f, -0.05f, 0.32f), 0.32f, GameAssets.Glow(new Color(1f, 0.15f, 0.2f), 2.6f), "Eye");
            p.head = body;
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 arm = new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * 0.75f;
                var armGo = Shapes.Box(body, arm * 0.5f + new Vector3(0f, 0.1f, 0f), new Vector3(0.1f, 0.06f, 0.75f), Lit(DarkGrey), "Arm");
                armGo.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                var rotor = Shapes.Empty(body, "Rotor", arm + new Vector3(0f, 0.12f, 0f)).transform;
                Shapes.Box(rotor, Vector3.zero, new Vector3(0.6f, 0.02f, 0.08f), Lit(new Color(0.85f, 0.88f, 0.9f)), "Blade");
                Shapes.Box(rotor, Vector3.zero, new Vector3(0.08f, 0.02f, 0.6f), Lit(new Color(0.85f, 0.88f, 0.9f)), "Blade");
                p.rotors.Add(rotor);
            }
            var gun = Shapes.CylinderZ(body, new Vector3(0f, -0.42f, 0.2f), 0.12f, 0.4f, Lit(DarkGrey), "Gun");
            p.cannons.Add(gun.transform);
        }

        static void BuildHeavy(EnemyParts p, Color main)
        {
            var r = p.root;
            var body = Shapes.Empty(r, "Body", new Vector3(0f, 1.0f, 0f)).transform;
            p.body = body;
            Shapes.Box(body, Vector3.zero, new Vector3(1.2f, 0.95f, 0.8f), Lit(main), "Torso", true);
            Shapes.Box(body, new Vector3(0f, 0f, 0.41f), new Vector3(1.0f, 0.5f, 0.04f), GameAssets.HazardMaterial, "Stripe");
            var head = Shapes.Empty(body, "Head", new Vector3(0f, 0.65f, 0.05f)).transform;
            p.head = head;
            Shapes.Box(head, Vector3.zero, new Vector3(0.6f, 0.35f, 0.5f), Lit(DarkGrey), "HeadBox", true);
            Shapes.Box(head, new Vector3(0f, 0f, 0.26f), new Vector3(0.5f, 0.1f, 0.02f), GameAssets.Glow(new Color(1f, 0.2f, 0.1f), 3f), "Visor");
            // shoulder cannons
            foreach (float side in new[] { -1f, 1f })
            {
                var mount = Shapes.Empty(body, side < 0 ? "CannonL" : "CannonR", new Vector3(side * 0.75f, 0.45f, 0f)).transform;
                Shapes.Box(mount, Vector3.zero, new Vector3(0.35f, 0.35f, 0.5f), Lit(DarkGrey), "Mount");
                Shapes.CylinderZ(mount, new Vector3(0f, 0f, 0.45f), 0.22f, 0.6f, Lit(new Color(0.5f, 0.52f, 0.56f), 0.7f, 0.8f), "Barrel");
                Shapes.CylinderZ(mount, new Vector3(0f, 0f, 0.76f), 0.18f, 0.04f, GameAssets.Glow(new Color(1f, 0.4f, 0.1f), 2.5f), "Muzzle");
                p.cannons.Add(mount);
            }
            p.armL = Limb(body, new Vector3(-0.7f, -0.1f, 0f), new Vector3(0.3f, 0.75f, 0.3f), Lit(DarkGrey), Lit(main), "ArmL");
            p.armR = Limb(body, new Vector3(0.7f, -0.1f, 0f), new Vector3(0.3f, 0.75f, 0.3f), Lit(DarkGrey), Lit(main), "ArmR");
            p.legL = Limb(r, new Vector3(-0.32f, 0.55f, 0f), new Vector3(0.34f, 0.55f, 0.36f), Lit(DarkGrey), Lit(main * 0.7f), "LegL");
            p.legR = Limb(r, new Vector3(0.32f, 0.55f, 0f), new Vector3(0.34f, 0.55f, 0.36f), Lit(DarkGrey), Lit(main * 0.7f), "LegR");
        }

        static void BuildBoss(EnemyParts p, Color main)
        {
            var r = p.root;
            var body = Shapes.Empty(r, "Body", new Vector3(0f, 1.25f, 0f)).transform;
            p.body = body;
            var armor = Lit(new Color(0.3f, 0.28f, 0.36f), 0.7f, 0.8f);
            Shapes.Box(body, Vector3.zero, new Vector3(1.6f, 1.2f, 1.0f), Lit(main, 0.6f, 0.5f), "Torso", true);
            Shapes.Box(body, new Vector3(0f, 0.2f, 0.45f), new Vector3(1.3f, 0.5f, 0.2f), armor, "ChestArmor", true);
            var core = Shapes.Sphere(body, new Vector3(0f, 0.05f, 0.55f), 0.45f, GameAssets.Glow(new Color(1f, 0.25f, 0.8f), 3f), "Core");
            core.AddComponent<Pulser>().amount = 0.12f;
            p.core = core.transform;
            var head = Shapes.Empty(body, "Head", new Vector3(0f, 0.85f, 0.1f)).transform;
            p.head = head;
            Shapes.Box(head, Vector3.zero, new Vector3(0.8f, 0.5f, 0.7f), armor, "HeadBox", true);
            Shapes.Box(head, new Vector3(0f, 0.05f, 0.36f), new Vector3(0.7f, 0.14f, 0.04f), GameAssets.Glow(new Color(1f, 0.15f, 0.3f), 3f), "Visor");
            Shapes.Box(head, new Vector3(-0.45f, 0.35f, 0f), new Vector3(0.1f, 0.4f, 0.1f), Lit(main), "HornL").transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            Shapes.Box(head, new Vector3(0.45f, 0.35f, 0f), new Vector3(0.1f, 0.4f, 0.1f), Lit(main), "HornR").transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            foreach (float side in new[] { -1f, 1f })
            {
                var shoulder = Shapes.Empty(body, side < 0 ? "ShoulderL" : "ShoulderR", new Vector3(side * 1.05f, 0.35f, 0f)).transform;
                Shapes.Box(shoulder, Vector3.zero, new Vector3(0.6f, 0.5f, 0.7f), armor, "Pad", true);
                // gatling arm
                var arm = Shapes.Empty(shoulder, "Arm", new Vector3(0f, -0.5f, 0.1f)).transform;
                Shapes.Box(arm, new Vector3(0f, -0.3f, 0f), new Vector3(0.4f, 0.7f, 0.45f), Lit(DarkGrey), "Forearm");
                var gun = Shapes.Empty(arm, "Gun", new Vector3(0f, -0.65f, 0.35f)).transform;
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * 0.5f;
                    Shapes.CylinderZ(gun, new Vector3(Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f, 0.3f), 0.08f, 0.8f, Lit(new Color(0.55f, 0.57f, 0.6f), 0.7f, 0.85f));
                }
                Shapes.CylinderZ(gun, new Vector3(0f, 0f, 0.72f), 0.3f, 0.05f, GameAssets.Glow(new Color(1f, 0.3f, 0.6f), 2.5f));
                p.cannons.Add(gun);
                if (side < 0) p.armL = shoulder; else p.armR = shoulder;
            }
            p.legL = Limb(r, new Vector3(-0.5f, 0.75f, 0f), new Vector3(0.45f, 0.75f, 0.5f), Lit(DarkGrey), armor, "LegL");
            p.legR = Limb(r, new Vector3(0.5f, 0.75f, 0f), new Vector3(0.45f, 0.75f, 0.5f), Lit(DarkGrey), armor, "LegR");
            Shapes.Box(r, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.05f, 1.6f), GameAssets.Glow(new Color(1f, 0.2f, 0.6f), 1.2f), "BossPad");
        }

        /// <summary>Limb pivoting from its top (shoulder/hip). Returns the pivot.</summary>
        static Transform Limb(Transform parent, Vector3 pivotPos, Vector3 size, Material joint, Material mat, string name)
        {
            var pivot = Shapes.Empty(parent, name, pivotPos).transform;
            Shapes.Sphere(pivot, Vector3.zero, size.x * 1.2f, joint, "Joint");
            Shapes.Box(pivot, new Vector3(0f, -size.y * 0.5f, 0f), size, mat, "Segment", true);
            Shapes.Box(pivot, new Vector3(0f, -size.y, 0.05f), new Vector3(size.x * 1.25f, size.x * 0.6f, size.z * 1.5f), joint, "Foot");
            return pivot;
        }
    }
}
