using System.Collections.Generic;
using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>
    /// Central access to shared runtime assets: materials (cached per colour so the whole game
    /// uses a small set of shared, instancing-enabled materials), the UI font and procedural textures.
    ///
    /// Base materials are loaded from Resources/WF/Materials (created automatically by the editor
    /// setup script so the shaders are included in player builds). If missing (e.g. setup not yet run)
    /// it falls back to Shader.Find, which always works inside the editor.
    /// </summary>
    public static class GameAssets
    {
        static Material _litBase, _glowBase, _unlitBase, _particleAdd, _particleAlpha;
        static readonly Dictionary<long, Material> _litCache = new Dictionary<long, Material>();
        static readonly Dictionary<long, Material> _glowCache = new Dictionary<long, Material>();
        static readonly Dictionary<long, Material> _unlitCache = new Dictionary<long, Material>();
        static Font _font;
        static Texture2D _softCircle, _stripes, _hazard, _grid;

        public const string MaterialsPath = "WF/Materials/";

        static Material LoadBase(string name, params string[] shaderFallbacks)
        {
            var m = Resources.Load<Material>(MaterialsPath + name);
            if (m != null) return m;
            foreach (var s in shaderFallbacks)
            {
                var sh = Shader.Find(s);
                if (sh != null)
                {
                    var mat = new Material(sh) { name = name + " (runtime)" };
                    return mat;
                }
            }
            Debug.LogWarning("[GameAssets] No shader found for " + name + ". Run 'Weapon Factory/Setup Project' in the editor.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        static Material LitBase { get { return _litBase ? _litBase : (_litBase = LoadBase("WF_Lit", "Standard", "Mobile/Diffuse", "Legacy Shaders/Diffuse")); } }
        static Material GlowBase { get { return _glowBase ? _glowBase : (_glowBase = LoadBase("WF_Glow", "Standard", "Unlit/Color")); } }
        static Material UnlitBase { get { return _unlitBase ? _unlitBase : (_unlitBase = LoadBase("WF_Unlit", "Unlit/Color", "Standard")); } }

        public static Material ParticleAdditive
        {
            get
            {
                if (_particleAdd) return _particleAdd;
                _particleAdd = new Material(LoadBase("WF_ParticleAdd", "Legacy Shaders/Particles/Additive", "Particles/Standard Unlit", "Sprites/Default"));
                _particleAdd.mainTexture = SoftCircle;
                return _particleAdd;
            }
        }

        public static Material ParticleAlpha
        {
            get
            {
                if (_particleAlpha) return _particleAlpha;
                _particleAlpha = new Material(LoadBase("WF_ParticleAlpha", "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Sprites/Default"));
                _particleAlpha.mainTexture = SoftCircle;
                return _particleAlpha;
            }
        }

        static long Key(Color c, float extra = 0f)
        {
            Color32 c32 = c;
            return ((long)c32.r << 32) | ((long)c32.g << 24) | ((long)c32.b << 16) | ((long)c32.a << 8) | (long)(Mathf.Clamp(extra, 0f, 25.5f) * 10f);
        }

        /// <summary>Shared opaque lit material for a colour.</summary>
        public static Material Lit(Color color, float smoothness = 0.45f, float metallic = 0.2f)
        {
            long key = Key(color, smoothness * 10f + metallic);
            Material m;
            if (_litCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LitBase) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(color) };
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            m.enableInstancing = true;
            _litCache[key] = m;
            return m;
        }

        /// <summary>Shared emissive material (neon gates, lights, glowing parts).</summary>
        public static Material Glow(Color color, float intensity = 1.6f)
        {
            long key = Key(color, intensity);
            Material m;
            if (_glowCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(GlowBase) { name = "Glow_" + ColorUtility.ToHtmlStringRGB(color) };
            m.color = color;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * intensity);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.enableInstancing = true;
            _glowCache[key] = m;
            return m;
        }

        /// <summary>Unlit flat colour (health bars, laser beams).</summary>
        public static Material Unlit(Color color)
        {
            long key = Key(color);
            Material m;
            if (_unlitCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(UnlitBase) { name = "Unlit_" + ColorUtility.ToHtmlStringRGB(color) };
            m.color = color;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color);
            }
            m.enableInstancing = true;
            _unlitCache[key] = m;
            return m;
        }

        static readonly Dictionary<long, Material> _addCache = new Dictionary<long, Material>();

        /// <summary>Shared additive (glass/neon panel) material without texture.</summary>
        public static Material Additive(Color color)
        {
            long key = Key(color);
            Material m;
            if (_addCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LoadBase("WF_ParticleAdd", "Legacy Shaders/Particles/Additive", "Particles/Standard Unlit", "Sprites/Default")) { name = "Add_" + ColorUtility.ToHtmlStringRGB(color) };
            m.mainTexture = null;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            _addCache[key] = m;
            return m;
        }

        static Material _hazardMat;
        /// <summary>Shared yellow/black hazard-stripe material.</summary>
        public static Material HazardMaterial
        {
            get { return _hazardMat ? _hazardMat : (_hazardMat = Textured(HazardStripes, Color.white, new Vector2(2f, 1f))); }
        }

        /// <summary>Creates a new (non-shared) lit material with a texture, e.g. scrolling conveyor belts.</summary>
        public static Material Textured(Texture texture, Color tint, Vector2 tiling)
        {
            var m = new Material(LitBase) { name = "Tex_" + (texture ? texture.name : "none") };
            m.mainTexture = texture;
            m.mainTextureScale = tiling;
            m.color = tint;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.3f);
            return m;
        }

        public static Font UIFont
        {
            get
            {
                if (_font) return _font;
                // Unity 2022.2+ ships "LegacyRuntime.ttf"; older versions use "Arial.ttf".
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex[0] == '#' ? hex : "#" + hex, out c)) return c;
            return Color.white;
        }

        // ---------- procedural textures ----------

        public static Texture2D SoftCircle
        {
            get
            {
                if (_softCircle) return _softCircle;
                const int size = 64;
                _softCircle = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SoftCircle", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a);
                        byte b = (byte)(a * 255);
                        px[y * size + x] = new Color32(255, 255, 255, b);
                    }
                _softCircle.SetPixels32(px);
                _softCircle.Apply(false, true);
                return _softCircle;
            }
        }

        /// <summary>Dark conveyor belt stripes (tiles along V).</summary>
        public static Texture2D ConveyorStripes
        {
            get
            {
                if (_stripes) return _stripes;
                const int w = 32, h = 64;
                _stripes = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Conveyor", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool ridge = (y % 16) < 3;
                        bool edge = x < 2 || x > w - 3;
                        byte v = (byte)(ridge ? 120 : (edge ? 70 : 92));
                        px[y * w + x] = new Color32(v, v, v, 255);
                    }
                _stripes.SetPixels32(px);
                _stripes.Apply(true, true);
                return _stripes;
            }
        }

        /// <summary>Yellow/black hazard stripes.</summary>
        public static Texture2D HazardStripes
        {
            get
            {
                if (_hazard) return _hazard;
                const int s = 64;
                _hazard = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Hazard", wrapMode = TextureWrapMode.Repeat };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        bool yellow = ((x + y) / 16) % 2 == 0;
                        px[y * s + x] = yellow ? new Color32(255, 196, 0, 255) : new Color32(30, 30, 34, 255);
                    }
                _hazard.SetPixels32(px);
                _hazard.Apply(true, true);
                return _hazard;
            }
        }

        /// <summary>Metal floor plate grid.</summary>
        public static Texture2D FloorGrid
        {
            get
            {
                if (_grid) return _grid;
                const int s = 64;
                _grid = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "FloorGrid", wrapMode = TextureWrapMode.Repeat };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        bool line = x < 2 || y < 2;
                        bool rivet = (x - 8) * (x - 8) + (y - 8) * (y - 8) < 5 || (x - 56) * (x - 56) + (y - 56) * (y - 56) < 5;
                        float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 18f;
                        byte v = (byte)(line ? 150 : (rivet ? 235 : 200 + noise));
                        px[y * s + x] = new Color32(v, v, v, 255);
                    }
                _grid.SetPixels32(px);
                _grid.Apply(true, true);
                return _grid;
            }
        }
    }
}
