using UnityEngine;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Gates
{
    /// <summary>
    /// Builds gate pairs. Authored pairs are sanitised and "randomize" pairs are generated with
    /// GateGenerator, which guarantees a fair choice (never two negatives, never duplicates).
    /// </summary>
    public static class GateManager
    {
        public static readonly Color PositiveColor = new Color(0.15f, 0.75f, 1f);
        public static readonly Color StrongColor = new Color(1f, 0.78f, 0.1f);
        public static readonly Color NegativeColor = new Color(1f, 0.18f, 0.25f);

        public const float TrackHalfWidth = 4.5f;

        public static GatePair BuildPair(Transform parent, float z, GatePairDefinition def, System.Random rng, float difficulty)
        {
            var fair = GateGenerator.Sanitize(def, rng, difficulty);
            var root = new GameObject("GatePair");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0f, 0f, z);
            var pair = root.AddComponent<GatePair>();

            float halfW = TrackHalfWidth * 0.5f;
            pair.left = BuildGate(root.transform, new Vector3(-halfW, 0f, 0f), TrackHalfWidth - 0.15f, fair.left, pair);
            pair.right = BuildGate(root.transform, new Vector3(halfW, 0f, 0f), TrackHalfWidth - 0.15f, fair.right, pair);

            // centre pillar shared by both gates
            var frame = GameAssets.Lit(new Color(0.18f, 0.19f, 0.23f), 0.6f, 0.7f);
            Shapes.Box(root.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.35f, 3.4f, 0.45f), frame, "CenterPillar", true);
            Shapes.Box(root.transform, new Vector3(0f, 3.45f, 0f), new Vector3(0.5f, 0.2f, 0.55f), GameAssets.Glow(Color.white, 1f), "CenterCap");
            return pair;
        }

        public static Color ColorFor(GateEffect e)
        {
            if (!e.IsPositive) return NegativeColor;
            if (e.type == GateEffectType.DamageMultiply || e.type == GateEffectType.BarrelAdd || (e.type == GateEffectType.PowerAdd && e.value >= 5))
                return StrongColor;
            return PositiveColor;
        }

        static UpgradeGate BuildGate(Transform parent, Vector3 localPos, float width, GateEffect effect, GatePair pair)
        {
            var go = new GameObject("Gate_" + effect.FullLabel);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var gate = go.AddComponent<UpgradeGate>();
            gate.effect = effect;
            gate.pair = pair;
            Color c = ColorFor(effect);
            gate.color = c;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.5f, 0f);
            col.size = new Vector3(width, 3f, 0.6f);

            var frame = GameAssets.Lit(new Color(0.18f, 0.19f, 0.23f), 0.6f, 0.7f);
            var glow = GameAssets.Glow(c, 2f);
            float side = width * 0.5f;
            // outer pillar (the inner one is the shared centre pillar)
            float outerX = localPos.x < 0 ? -side : side;
            Shapes.Box(go.transform, new Vector3(outerX, 1.7f, 0f), new Vector3(0.3f, 3.4f, 0.4f), frame, "Pillar", true);
            Shapes.Box(go.transform, new Vector3(outerX, 1.7f, -0.21f), new Vector3(0.1f, 3.2f, 0.02f), glow, "PillarGlow");
            Shapes.Box(go.transform, new Vector3(0f, 3.35f, 0f), new Vector3(width, 0.25f, 0.4f), frame, "TopBeam");
            Shapes.Box(go.transform, new Vector3(0f, 3.35f, -0.21f), new Vector3(width - 0.2f, 0.08f, 0.02f), glow, "BeamGlow");

            // translucent neon panel
            var panel = Shapes.Create(PrimitiveType.Quad, go.transform, new Vector3(0f, 1.62f, 0.02f), new Vector3(width - 0.3f, 3.05f, 1f),
                GameAssets.Additive(new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 0.6f)), "Panel");
            panel.transform.localRotation = Quaternion.identity;
            gate.panel = panel.transform;
            // bottom glow strip on the floor
            Shapes.Box(go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(width - 0.2f, 0.04f, 0.5f), glow, "FloorStrip");

            Color textCol = Color.white;
            gate.valueText = Shapes.Text3D(go.transform, effect.ValueLabel, new Vector3(0f, 2.05f, -0.05f), 0.12f, textCol);
            gate.captionText = Shapes.Text3D(go.transform, effect.Caption, new Vector3(0f, 1.15f, -0.05f), 0.055f, textCol);
            // drop shadows for readability
            var sh1 = Shapes.Text3D(go.transform, effect.ValueLabel, new Vector3(0.05f, 2.0f, -0.04f), 0.12f, new Color(0f, 0f, 0f, 0.6f));
            sh1.GetComponent<MeshRenderer>().sortingOrder = -1;
            var sh2 = Shapes.Text3D(go.transform, effect.Caption, new Vector3(0.03f, 1.12f, -0.04f), 0.055f, new Color(0f, 0f, 0f, 0.6f));
            sh2.GetComponent<MeshRenderer>().sortingOrder = -1;
            gate.valueText.GetComponent<MeshRenderer>().sortingOrder = 1;
            gate.captionText.GetComponent<MeshRenderer>().sortingOrder = 1;

            var pulse = panel.AddComponent<Pulser>();
            pulse.amount = 0.015f;
            return gate;
        }
    }
}
