using UnityEngine;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Obstacles
{
    public enum ObstacleKind { Saw, MovingSaw, Crusher, LaserWall, Hammer }

    /// <summary>
    /// Hazards on the track. Touching a dangerous obstacle damages weapon integrity, knocks a
    /// component off the weapon and pushes the player back. Moving parts use kinematic bodies.
    /// </summary>
    public class Obstacle : MonoBehaviour, ITrackInteractable
    {
        public ObstacleKind kind;
        public float damage = 20f;
        public float speed = 1f;
        public string deathReason = "Destroyed by factory machinery";

        // animated parts
        public Transform movingPart;
        public Transform spinPart;
        public Renderer warningRenderer;
        public float phase;

        float _time;
        bool _dangerous = true;

        public bool IsDangerous { get { return _dangerous; } }

        public void OnPlayerEnter(PlayerController player)
        {
            if (!_dangerous || player.IsInvulnerable) return;
            player.TakeHit(damage, deathReason, true, true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _time += dt * speed;
            if (spinPart != null) spinPart.Rotate(0f, 0f, 720f * dt * Mathf.Max(0.5f, speed), Space.Self);

            switch (kind)
            {
                case ObstacleKind.MovingSaw:
                    if (movingPart != null)
                    {
                        float x = Mathf.Sin(_time * 1.6f + phase) * 3.2f;
                        movingPart.localPosition = new Vector3(x, movingPart.localPosition.y, 0f);
                    }
                    break;

                case ObstacleKind.Crusher:
                    if (movingPart != null)
                    {
                        // cycle: 1.2s up, 0.15s slam, 0.45s down, 0.6s rise (2.4 s)
                        float c = Mathf.Repeat(_time + phase, 2.4f);
                        float h;
                        if (c < 1.2f) h = 3.2f;
                        else if (c < 1.35f) h = Mathf.Lerp(3.2f, 0.55f, (c - 1.2f) / 0.15f);
                        else if (c < 1.8f) h = 0.55f;
                        else h = Mathf.Lerp(0.55f, 3.2f, (c - 1.8f) / 0.6f);
                        if (c >= 1.33f && c < 1.36f) ScreenShakeIfClose();
                        movingPart.localPosition = new Vector3(0f, h, 0f);
                        _dangerous = h < 1.5f;
                        if (warningRenderer != null)
                        {
                            float warn = c > 0.6f && c < 1.8f ? (Mathf.Repeat(_time * 6f, 1f) < 0.5f ? 1f : 0.3f) : 0.15f;
                            warningRenderer.enabled = warn > 0.5f;
                        }
                    }
                    break;

                case ObstacleKind.Hammer:
                    if (movingPart != null)
                    {
                        float angle = Mathf.Sin(_time * 1.8f + phase) * 62f;
                        movingPart.localRotation = Quaternion.Euler(0f, 0f, angle);
                    }
                    break;

                case ObstacleKind.LaserWall:
                    if (warningRenderer != null)
                        warningRenderer.enabled = Mathf.Repeat(_time * 8f, 1f) < 0.85f; // subtle flicker
                    break;
            }
        }

        void ScreenShakeIfClose()
        {
            var p = PlayerController.Instance;
            if (p != null && Mathf.Abs(p.transform.position.z - transform.position.z) < 12f) ScreenShake.Add(0.08f);
        }

        // =====================================================================================
        // Builders
        // =====================================================================================

        static Material Steel { get { return GameAssets.Lit(new Color(0.55f, 0.58f, 0.62f), 0.75f, 0.85f); } }
        static Material Dark { get { return GameAssets.Lit(new Color(0.14f, 0.15f, 0.18f), 0.5f, 0.5f); } }
        static Material Hazard { get { return GameAssets.HazardMaterial; } }
        static readonly Color Red = new Color(1f, 0.15f, 0.15f);

        static Obstacle Root(Transform parent, Vector3 pos, ObstacleKind kind, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var o = go.AddComponent<Obstacle>();
            o.kind = kind;
            o.phase = Random.value * 3f;
            return o;
        }

        static void AddTrigger(GameObject go, Vector3 center, Vector3 size, bool moving)
        {
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = center;
            col.size = size;
            if (moving)
            {
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        static Transform BuildSawBlade(Transform parent, Vector3 localPos, float diameter)
        {
            var holder = Shapes.Empty(parent, "Saw", localPos).transform;
            var spin = Shapes.Empty(holder, "Blade").transform;
            var disc = Shapes.CylinderZ(spin, Vector3.zero, diameter, 0.08f, Steel, "Disc");
            disc.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Shapes.CylinderZ(spin, Vector3.zero, diameter * 0.3f, 0.12f, Dark, "Hub");
            Shapes.CylinderZ(spin, Vector3.zero, diameter * 0.12f, 0.14f, GameAssets.Glow(Red, 2f), "HubLight");
            int teeth = 12;
            for (int i = 0; i < teeth; i++)
            {
                float a = i * Mathf.PI * 2f / teeth;
                var tooth = Shapes.Box(spin, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * diameter * 0.5f, new Vector3(0.18f, 0.18f, 0.06f), Steel, "Tooth");
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 45f);
            }
            return spin;
        }

        public static Obstacle BuildSaw(Transform parent, Vector3 pos)
        {
            var o = Root(parent, pos, ObstacleKind.Saw, "Obstacle_Saw");
            o.deathReason = "Cut in half by a saw blade";
            var t = o.transform;
            Shapes.Box(t, new Vector3(0f, 0.08f, 0f), new Vector3(2.1f, 0.16f, 0.6f), Hazard, "Slot");
            Shapes.Box(t, new Vector3(0f, 0.1f, 0f), new Vector3(1.9f, 0.05f, 0.15f), GameAssets.Glow(Red, 1.5f), "SlotGlow");
            o.spinPart = BuildSawBlade(t, new Vector3(0f, 0.75f, 0f), 1.7f);
            AddTrigger(o.gameObject, new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 1.5f, 0.5f), false);
            return o;
        }

        public static Obstacle BuildMovingSaw(Transform parent, Vector3 pos, float speed)
        {
            var o = Root(parent, pos, ObstacleKind.MovingSaw, "Obstacle_MovingSaw");
            o.speed = Mathf.Max(0.5f, speed);
            o.deathReason = "Caught by a moving saw";
            var t = o.transform;
            Shapes.Box(t, new Vector3(0f, 0.06f, 0f), new Vector3(8.6f, 0.12f, 0.5f), Hazard, "Rail");
            Shapes.Box(t, new Vector3(0f, 0.09f, 0f), new Vector3(8.4f, 0.04f, 0.12f), GameAssets.Glow(Red, 1.4f), "RailGlow");
            var mover = Shapes.Empty(t, "Mover").transform;
            o.movingPart = mover;
            Shapes.Box(mover, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.35f, 0.6f), Dark, "Carriage");
            o.spinPart = BuildSawBlade(mover, new Vector3(0f, 0.95f, 0f), 1.6f);
            AddTrigger(mover.gameObject, new Vector3(0f, 0.9f, 0f), new Vector3(1.5f, 1.6f, 0.5f), true);
            return o;
        }

        public static Obstacle BuildCrusher(Transform parent, Vector3 pos)
        {
            var o = Root(parent, pos, ObstacleKind.Crusher, "Obstacle_Crusher");
            o.damage = 30f;
            o.deathReason = "Flattened by a hydraulic press";
            var t = o.transform;
            float w = 3.2f;
            Shapes.Box(t, new Vector3(-w * 0.5f - 0.25f, 2.6f, 0f), new Vector3(0.35f, 5.2f, 0.6f), Dark, "FrameL", true);
            Shapes.Box(t, new Vector3(w * 0.5f + 0.25f, 2.6f, 0f), new Vector3(0.35f, 5.2f, 0.6f), Dark, "FrameR", true);
            Shapes.Box(t, new Vector3(0f, 5.3f, 0f), new Vector3(w + 0.9f, 0.4f, 0.8f), Dark, "FrameTop", true);
            var head = Shapes.Empty(t, "Head", new Vector3(0f, 3.2f, 0f)).transform;
            o.movingPart = head;
            Shapes.Box(head, new Vector3(0f, 0f, 0f), new Vector3(w, 0.7f, 1.4f), Hazard, "Plate", true);
            Shapes.Box(head, new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 2.0f, 0.5f), Steel, "Piston");
            var warn = Shapes.Box(t, new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.03f, 1.4f), GameAssets.Glow(Red, 1.8f), "Warning");
            o.warningRenderer = warn.GetComponent<MeshRenderer>();
            AddTrigger(o.gameObject, new Vector3(0f, 0.7f, 0f), new Vector3(w - 0.2f, 1.4f, 1.2f), false);
            return o;
        }

        public static Obstacle BuildLaserWall(Transform parent, Vector3 pos, float gapX)
        {
            var o = Root(parent, pos, ObstacleKind.LaserWall, "Obstacle_Laser");
            o.damage = 20f;
            o.deathReason = "Fried by a laser fence";
            var t = o.transform;
            float half = 4.5f, gap = 3.0f;
            gapX = Mathf.Clamp(gapX, -half + gap * 0.5f, half - gap * 0.5f);
            float gl = gapX - gap * 0.5f, gr = gapX + gap * 0.5f;
            var beamsRoot = Shapes.Empty(t, "Beams").transform;
            var beamMat = GameAssets.Glow(Red, 3f);
            foreach (float y in new[] { 0.35f, 0.85f, 1.35f })
            {
                if (gl > -half) Shapes.Box(beamsRoot, new Vector3((-half + gl) * 0.5f, y, 0f), new Vector3(gl + half, 0.06f, 0.06f), beamMat, "BeamL");
                if (gr < half) Shapes.Box(beamsRoot, new Vector3((gr + half) * 0.5f, y, 0f), new Vector3(half - gr, 0.06f, 0.06f), beamMat, "BeamR");
            }
            // emitter posts at both sides of the gap and at the walls
            foreach (float x in new[] { -half, gl, gr, half })
            {
                if (Mathf.Abs(x) > half + 0.01f) continue;
                Shapes.Box(t, new Vector3(x, 0.9f, 0f), new Vector3(0.25f, 1.8f, 0.25f), Dark, "Post", true);
                Shapes.Sphere(t, new Vector3(x, 1.9f, 0f), 0.22f, GameAssets.Glow(Red, 2.5f), "PostLight").AddComponent<Blinker>().interval = 0.4f;
            }
            // green safe-gap marker on the floor
            Shapes.Box(t, new Vector3(gapX, 0.03f, 0f), new Vector3(gap - 0.4f, 0.03f, 0.8f), GameAssets.Glow(new Color(0.2f, 1f, 0.4f), 1.4f), "SafeGap");
            o.warningRenderer = null;
            // two trigger volumes (left and right of the gap)
            if (gl > -half)
            {
                var l = Shapes.Empty(t, "TriggerL");
                AddTrigger(l, new Vector3((-half + gl) * 0.5f, 0.9f, 0f), new Vector3(gl + half, 1.6f, 0.4f), false);
            }
            if (gr < half)
            {
                var r = Shapes.Empty(t, "TriggerR");
                AddTrigger(r, new Vector3((gr + half) * 0.5f, 0.9f, 0f), new Vector3(half - gr, 1.6f, 0.4f), false);
            }
            return o;
        }

        public static Obstacle BuildHammer(Transform parent, Vector3 pos, float speed)
        {
            var o = Root(parent, pos, ObstacleKind.Hammer, "Obstacle_Hammer");
            o.speed = Mathf.Max(0.5f, speed);
            o.damage = 25f;
            o.deathReason = "Smashed by a swinging hammer";
            var t = o.transform;
            Shapes.Box(t, new Vector3(-5f, 3.4f, 0f), new Vector3(0.4f, 6.8f, 0.5f), Dark, "SupportL", true);
            Shapes.Box(t, new Vector3(5f, 3.4f, 0f), new Vector3(0.4f, 6.8f, 0.5f), Dark, "SupportR", true);
            Shapes.Box(t, new Vector3(0f, 6.9f, 0f), new Vector3(10.4f, 0.4f, 0.5f), Dark, "Beam", true);
            var pivot = Shapes.Empty(t, "Pivot", new Vector3(0f, 6.7f, 0f)).transform;
            o.movingPart = pivot;
            Shapes.Box(pivot, new Vector3(0f, -2.6f, 0f), new Vector3(0.2f, 5.2f, 0.2f), Steel, "Arm");
            var head = Shapes.CylinderZ(pivot, new Vector3(0f, -5.6f, 0f), 1.4f, 1.0f, Hazard, "HammerHead");
            head.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Shapes.CylinderZ(pivot, new Vector3(0f, -5.6f, 0f), 1.45f, 0.2f, GameAssets.Glow(Red, 1.8f), "HeadGlow");
            var trig = Shapes.Empty(pivot, "HeadTrigger", new Vector3(0f, -5.6f, 0f));
            AddTrigger(trig, Vector3.zero, new Vector3(1.4f, 1.4f, 1.0f), true);
            return o;
        }
    }
}
