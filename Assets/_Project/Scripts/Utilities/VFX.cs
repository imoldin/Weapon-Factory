using UnityEngine;

namespace WeaponFactory.Utilities
{
    public enum VFXType
    {
        Sparks = 0,     // bullet impacts
        Explosion = 1,  // fiery burst
        Smoke = 2,      // dark puffs
        Pickup = 3,     // collectible sparkle
        Confetti = 4,   // victory
        Debris = 5,     // metal chunks
        Ring = 6,       // gate activation / evolution
    }

    /// <summary>
    /// One world-space ParticleSystem per effect type; effects are emitted with EmitParams at a
    /// position, so any number of simultaneous effects costs a single system (very mobile friendly).
    /// Lives in the current scene and is recreated lazily after scene changes.
    /// </summary>
    public class VFX : MonoBehaviour
    {
        static VFX _instance;
        ParticleSystem[] _systems;
        public static float QualityScale = 1f; // lowered on Low graphics quality

        static VFX Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[VFX]");
                    _instance = go.AddComponent<VFX>();
                    _instance.Build();
                }
                return _instance;
            }
        }

        void OnDestroy() { if (_instance == this) _instance = null; }

        void Build()
        {
            int n = System.Enum.GetValues(typeof(VFXType)).Length;
            _systems = new ParticleSystem[n];
            _systems[(int)VFXType.Sparks] = Create("Sparks", GameAssets.ParticleAdditive, 0.25f, 0.5f, 4f, 9f, 0.08f, 0.18f, 1.2f, 0.1f, true, 400);
            _systems[(int)VFXType.Explosion] = Create("Explosion", GameAssets.ParticleAdditive, 0.35f, 0.7f, 2f, 6f, 0.6f, 1.4f, -0.1f, 0.4f, false, 300);
            _systems[(int)VFXType.Smoke] = Create("Smoke", GameAssets.ParticleAlpha, 0.6f, 1.2f, 0.5f, 1.8f, 0.6f, 1.3f, -0.15f, 0.3f, false, 200);
            _systems[(int)VFXType.Pickup] = Create("Pickup", GameAssets.ParticleAdditive, 0.35f, 0.7f, 2f, 5f, 0.12f, 0.3f, -0.3f, 0.3f, false, 300);
            _systems[(int)VFXType.Confetti] = Create("Confetti", GameAssets.ParticleAlpha, 1.6f, 2.6f, 5f, 10f, 0.12f, 0.25f, 1.0f, 0.5f, false, 400);
            _systems[(int)VFXType.Debris] = Create("Debris", GameAssets.ParticleAlpha, 0.7f, 1.2f, 3f, 7f, 0.15f, 0.35f, 2.2f, 0.3f, false, 250);
            _systems[(int)VFXType.Ring] = Create("Ring", GameAssets.ParticleAdditive, 0.3f, 0.5f, 6f, 8f, 0.25f, 0.4f, 0f, 0.2f, false, 300);

            // ring emits on a flat circle
            var shape = _systems[(int)VFXType.Ring].shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(90f, 0f, 0f);
        }

        ParticleSystem Create(string name, Material mat, float lifeMin, float lifeMax, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity, float radius, bool stretch, int maxParticles)
        {
            var go = new GameObject("VFX_" + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (stretch) { r.velocityScale = 0.06f; r.lengthScale = 1.5f; }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();
            return ps;
        }

        public static void Play(VFXType type, Vector3 position, Color color, int count, float sizeMultiplier = 1f)
        {
            var inst = Instance;
            var ps = inst._systems[(int)type];
            if (ps == null) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(count * QualityScale));
            var p = new ParticleSystem.EmitParams();
            p.position = position;
            p.applyShapeToPosition = true;
            p.startColor = color;
            if (sizeMultiplier != 1f)
            {
                var main = ps.main;
                p.startSize = Random.Range(main.startSize.constantMin, main.startSize.constantMax) * sizeMultiplier;
            }
            if (type == VFXType.Confetti)
            {
                // multicoloured confetti
                for (int i = 0; i < n; i++)
                {
                    p.startColor = Color.HSVToRGB(Random.value, 0.8f, 1f);
                    ps.Emit(p, 1);
                }
                return;
            }
            ps.Emit(p, n);
        }

        /// <summary>Full explosion: flash + fire + smoke + debris + camera shake.</summary>
        public static void Explosion(Vector3 position, float scale, Color tint)
        {
            Play(VFXType.Explosion, position, Color.Lerp(new Color(1f, 0.6f, 0.2f), tint, 0.3f), Mathf.RoundToInt(18 * scale), scale);
            Play(VFXType.Sparks, position, new Color(1f, 0.85f, 0.4f), Mathf.RoundToInt(16 * scale));
            Play(VFXType.Smoke, position, new Color(0.25f, 0.25f, 0.28f, 0.8f), Mathf.RoundToInt(8 * scale), scale);
            Play(VFXType.Debris, position, new Color(0.35f, 0.37f, 0.4f), Mathf.RoundToInt(8 * scale));
            ScreenShake.Add(Mathf.Clamp(0.12f * scale, 0.05f, 0.45f));
        }
    }
}
