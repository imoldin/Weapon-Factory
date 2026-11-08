using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeaponFactory.UI
{
    public enum Icon { Coin, Play, Pause, Gear, Lock, Star, ArrowUp, Gun, Home, Restart, Close, Check, Speaker, Circle, Skull, Bolt, Heart }

    /// <summary>
    /// Generates all UI sprites at runtime with anti-aliased signed-distance shapes:
    /// 9-sliced rounded panels and a set of flat icons. No texture assets required.
    /// </summary>
    public static class ProceduralSprites
    {
        static Sprite _rounded, _roundedSmall, _circle, _glow;
        static readonly Dictionary<Icon, Sprite> _icons = new Dictionary<Icon, Sprite>();

        /// <summary>White rounded rectangle, 9-sliced (use Image.Type.Sliced).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded) return _rounded;
                _rounded = MakeRounded(96, 30f, "Rounded");
                return _rounded;
            }
        }

        public static Sprite RoundedSmall
        {
            get
            {
                if (_roundedSmall) return _roundedSmall;
                _roundedSmall = MakeRounded(48, 12f, "RoundedSmall");
                return _roundedSmall;
            }
        }

        public static Sprite CircleSprite
        {
            get { return _circle ? _circle : (_circle = GetIcon(Icon.Circle)); }
        }

        /// <summary>Soft radial glow (for highlights behind items).</summary>
        public static Sprite Glow
        {
            get
            {
                if (_glow) return _glow;
                _glow = Make(128, (x, y) =>
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    float a = Mathf.Clamp01(1f - d);
                    return new Color(1f, 1f, 1f, a * a);
                }, "Glow", Vector4.zero);
                return _glow;
            }
        }

        static Sprite MakeRounded(int size, float radius, string name)
        {
            float half = size * 0.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px0 = Mathf.Abs(x + 0.5f - half), py0 = Mathf.Abs(y + 0.5f - half);
                    float qx = Mathf.Max(px0 - (half - radius), 0f), qy = Mathf.Max(py0 - (half - radius), 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        /// <summary>Builds a sprite from a colour function over normalised coordinates (-1..1).</summary>
        static Sprite Make(int size, Func<float, float, Color> f, string name, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    px[y * size + x] = f(nx, ny);
                }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        // ---------- SDF helpers (coordinates in -1..1) ----------
        const float AA = 0.035f; // anti-alias width in normalised units

        static float Fill(float d) { return Mathf.Clamp01(0.5f - d / AA); }
        static float Circle(float x, float y, float cx, float cy, float r) { return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r; }
        static float Box(float x, float y, float cx, float cy, float hw, float hh)
        {
            float dx = Mathf.Abs(x - cx) - hw, dy = Mathf.Abs(y - cy) - hh;
            float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }
        static float Segment(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }
        static float Tri(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            // signed distance approximation to a triangle (negative inside); works for either winding
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (cross < 0f) { var t = b; b = c; c = t; }
            float d1 = Edge(x, y, a, b), d2 = Edge(x, y, b, c), d3 = Edge(x, y, c, a);
            return Mathf.Max(d1, Mathf.Max(d2, d3));
        }
        static float Edge(float x, float y, Vector2 a, Vector2 b)
        {
            Vector2 e = b - a;
            Vector2 n = new Vector2(e.y, -e.x).normalized;
            return (x - a.x) * n.x + (y - a.y) * n.y;
        }
        static readonly Vector2[] StarPoints = BuildStar(0.92f, 0.4f);

        static Vector2[] BuildStar(float outer, float inner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? outer : inner;
                pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.05f);
            }
            return pts;
        }

        /// <summary>Signed distance to a simple polygon (negative inside).</summary>
        static float Polygon(float x, float y, Vector2[] v)
        {
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                float wx = x - v[i].x, wy = y - v[i].y;
                float h = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float dx = wx - ex * h, dy = wy - ey * h;
                d = Mathf.Min(d, dx * dx + dy * dy);
                if (((v[i].y > y) != (v[j].y > y)) && (x < (v[j].x - v[i].x) * (y - v[i].y) / (v[j].y - v[i].y) + v[i].x)) inside = !inside;
            }
            d = Mathf.Sqrt(d);
            return inside ? -d : d;
        }

        static float Union(float a, float b) { return Mathf.Min(a, b); }
        static float Subtract(float a, float b) { return Mathf.Max(a, -b); }

        public static Sprite GetIcon(Icon icon)
        {
            Sprite s;
            if (_icons.TryGetValue(icon, out s) && s != null) return s;
            s = Make(96, (x, y) => IconPixel(icon, x, y), "Icon_" + icon, Vector4.zero);
            _icons[icon] = s;
            return s;
        }

        static Color White(float a) { return new Color(1f, 1f, 1f, a); }

        static Color IconPixel(Icon icon, float x, float y)
        {
            switch (icon)
            {
                case Icon.Circle: return White(Fill(Circle(x, y, 0, 0, 0.92f)));
                case Icon.Coin:
                    {
                        float outer = Fill(Circle(x, y, 0, 0, 0.88f));
                        float inner = Fill(Circle(x, y, 0, 0, 0.62f));
                        float mark = Fill(Box(x, y, 0f, 0f, 0.12f, 0.36f));
                        Color rim = new Color(1f, 0.67f, 0.05f);
                        Color face = new Color(1f, 0.84f, 0.2f);
                        Color c = Color.Lerp(rim, face, inner);
                        c = Color.Lerp(c, new Color(1f, 0.95f, 0.6f), mark * inner);
                        c.a = outer;
                        return c;
                    }
                case Icon.Play:
                    return White(Fill(Tri(x, y, new Vector2(-0.45f, -0.65f), new Vector2(-0.45f, 0.65f), new Vector2(0.7f, 0f)) - 0.05f));
                case Icon.Pause:
                    return White(Fill(Union(Box(x, y, -0.3f, 0, 0.16f, 0.62f), Box(x, y, 0.3f, 0, 0.16f, 0.62f)) - 0.04f));
                case Icon.Gear:
                    {
                        float ang = Mathf.Atan2(y, x);
                        float r = Mathf.Sqrt(x * x + y * y);
                        float teeth = Mathf.Cos(ang * 8f) > 0.2f ? 0.86f : 0.66f;
                        float d = r - teeth;
                        d = Subtract(d, Circle(x, y, 0, 0, 0.28f));
                        return White(Fill(d));
                    }
                case Icon.Lock:
                    {
                        float body = Box(x, y, 0f, -0.25f, 0.55f, 0.42f) - 0.08f;
                        float ring = Mathf.Abs(Circle(x, y, 0f, 0.22f, 0.36f)) - 0.1f;
                        ring = Mathf.Max(ring, -(y - 0.15f)); // only the top half of the shackle
                        float d = Union(body, ring);
                        d = Subtract(d, Circle(x, y, 0f, -0.2f, 0.1f));
                        return White(Fill(d));
                    }
                case Icon.Star:
                    return White(Fill(Polygon(x, y, StarPoints)));
                case Icon.ArrowUp:
                    {
                        float head = Tri(x, y, new Vector2(-0.7f, 0.05f), new Vector2(0f, 0.8f), new Vector2(0.7f, 0.05f));
                        float shaft = Box(x, y, 0f, -0.35f, 0.22f, 0.45f);
                        return White(Fill(Union(head, shaft) - 0.03f));
                    }
                case Icon.Gun:
                    {
                        float slide = Box(x, y, 0.05f, 0.25f, 0.72f, 0.2f) - 0.05f;
                        float grip = Box(x * 0.95f + y * 0.25f, y, -0.38f, -0.3f, 0.17f, 0.42f) - 0.05f;
                        float guard = Mathf.Abs(Circle(x, y, -0.02f, -0.05f, 0.2f)) - 0.05f;
                        guard = Mathf.Max(guard, y - 0.02f);
                        return White(Fill(Union(Union(slide, grip), guard)));
                    }
                case Icon.Home:
                    {
                        float roof = Tri(x, y, new Vector2(-0.8f, 0.1f), new Vector2(0f, 0.8f), new Vector2(0.8f, 0.1f));
                        float house = Box(x, y, 0f, -0.3f, 0.55f, 0.45f);
                        float door = Box(x, y, 0f, -0.45f, 0.15f, 0.3f);
                        return White(Fill(Subtract(Union(roof, house) - 0.03f, door)));
                    }
                case Icon.Restart:
                    {
                        float ang = Mathf.Atan2(y, x);
                        float ring = Mathf.Abs(Circle(x, y, 0, 0, 0.58f)) - 0.13f;
                        if (ang > 0.2f && ang < 1.3f) ring = 1f; // gap
                        // arrow head at the ring end, pointing clockwise (down)
                        float head = Tri(x, y, new Vector2(0.3f, 0.22f), new Vector2(0.88f, 0.22f), new Vector2(0.59f, -0.2f)) - 0.02f;
                        return White(Fill(Union(ring, head)));
                    }
                case Icon.Close:
                    return White(Fill(Union(Segment(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.15f), Segment(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.15f))));
                case Icon.Check:
                    return White(Fill(Union(Segment(x, y, -0.6f, 0f, -0.15f, -0.5f, 0.15f), Segment(x, y, -0.15f, -0.5f, 0.65f, 0.55f, 0.15f))));
                case Icon.Speaker:
                    {
                        float body = Box(x, y, -0.5f, 0f, 0.18f, 0.22f);
                        float cone = Tri(x, y, new Vector2(-0.45f, -0.22f), new Vector2(-0.45f, 0.22f), new Vector2(0.05f, 0f));
                        cone = Mathf.Min(cone, Box(x, y, -0.2f, 0f, 0.25f, 0.45f) + 0.0f);
                        cone = Mathf.Max(cone, Tri(x, y, new Vector2(-0.5f, 0f), new Vector2(0.1f, 0.65f), new Vector2(0.1f, -0.65f)));
                        float wave1 = Mathf.Abs(Circle(x, y, 0.05f, 0f, 0.38f)) - 0.06f;
                        float wave2 = Mathf.Abs(Circle(x, y, 0.05f, 0f, 0.65f)) - 0.06f;
                        float waves = Mathf.Max(Union(wave1, wave2), -(x - 0.15f));
                        waves = Mathf.Max(waves, Mathf.Abs(y) - 0.55f * (x + 0.2f));
                        return White(Fill(Union(Union(body, cone), waves)));
                    }
                case Icon.Skull:
                    {
                        float head = Circle(x, y, 0f, 0.12f, 0.62f);
                        float jaw = Box(x, y, 0f, -0.5f, 0.32f, 0.2f) - 0.05f;
                        float d = Union(head, jaw);
                        d = Subtract(d, Circle(x, y, -0.24f, 0.1f, 0.16f));
                        d = Subtract(d, Circle(x, y, 0.24f, 0.1f, 0.16f));
                        d = Subtract(d, Box(x, y, 0f, -0.55f, 0.03f, 0.2f));
                        return White(Fill(d));
                    }
                case Icon.Bolt:
                    {
                        float a = Tri(x, y, new Vector2(0.15f, 0.9f), new Vector2(-0.55f, -0.05f), new Vector2(0.05f, -0.05f));
                        float b = Tri(x, y, new Vector2(-0.15f, -0.9f), new Vector2(0.55f, 0.05f), new Vector2(-0.05f, 0.05f));
                        float c = Box(x, y, 0f, 0f, 0.25f, 0.08f);
                        return White(Fill(Union(Union(a, b), c) - 0.02f));
                    }
                case Icon.Heart:
                    {
                        float l = Circle(x, y, -0.33f, 0.25f, 0.4f);
                        float r = Circle(x, y, 0.33f, 0.25f, 0.4f);
                        float t = Tri(x, y, new Vector2(-0.72f, 0.12f), new Vector2(0f, -0.8f), new Vector2(0.72f, 0.12f));
                        return White(Fill(Union(Union(l, r), t)));
                    }
            }
            return White(0f);
        }
    }
}
