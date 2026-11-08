using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WeaponFactory.Utilities
{
    /// <summary>
    /// Builds visuals from Unity's primitive meshes WITHOUT colliders (cheaper than
    /// GameObject.CreatePrimitive + Destroy). All placeholder models in the game use this.
    /// </summary>
    public static class Shapes
    {
        static readonly Dictionary<PrimitiveType, Mesh> _meshes = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>
        /// Procedural meshes with the same dimensions as Unity's primitives (cube 1x1x1, sphere d=1,
        /// cylinder d=1 h=2, quad 1x1 facing -Z). Generated in code instead of GameObject.CreatePrimitive,
        /// which can fail in stripped IL2CPP builds when collider types are not referenced.
        /// </summary>
        public static Mesh GetMesh(PrimitiveType type)
        {
            Mesh mesh;
            if (_meshes.TryGetValue(type, out mesh) && mesh != null) return mesh;
            switch (type)
            {
                case PrimitiveType.Sphere: mesh = BuildSphere(16, 12); break;
                case PrimitiveType.Cylinder:
                case PrimitiveType.Capsule: mesh = BuildCylinder(20); break;
                case PrimitiveType.Quad:
                case PrimitiveType.Plane: mesh = BuildQuad(); break;
                default: mesh = BuildCube(); break;
            }
            mesh.name = "WF_" + type;
            mesh.RecalculateBounds();
            _meshes[type] = mesh;
            return mesh;
        }

        static Mesh BuildCube()
        {
            var v = new List<Vector3>(24); var n = new List<Vector3>(24); var uv = new List<Vector2>(24); var tri = new List<int>(36);
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            foreach (var d in dirs)
            {
                Vector3 up = Mathf.Abs(d.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(up, d);
                int b = v.Count;
                Vector3 c = d * 0.5f;
                v.Add(c - right * 0.5f - up * 0.5f); v.Add(c + right * 0.5f - up * 0.5f);
                v.Add(c + right * 0.5f + up * 0.5f); v.Add(c - right * 0.5f + up * 0.5f);
                for (int i = 0; i < 4; i++) n.Add(d);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2); tri.Add(b); tri.Add(b + 2); tri.Add(b + 3); // normal = Cross(v1-v0, v2-v0) = d
            }
            return Finish(v, n, uv, tri);
        }

        static Mesh BuildQuad()
        {
            var v = new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            var tri = new List<int> { 0, 2, 1, 0, 3, 2 };
            return Finish(v, n, uv, tri);
        }

        static Mesh BuildSphere(int lon, int lat)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float a = Mathf.PI * y / lat;
                for (int x = 0; x <= lon; x++)
                {
                    float b = 2f * Mathf.PI * x / lon;
                    var dir = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b));
                    v.Add(dir * 0.5f); n.Add(dir); uv.Add(new Vector2(x / (float)lon, 1f - y / (float)lat));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                    tri.Add(i0); tri.Add(i1); tri.Add(i2);
                    tri.Add(i1); tri.Add(i3); tri.Add(i2);
                }
            return Finish(v, n, uv, tri);
        }

        static Mesh BuildCylinder(int seg)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            // side
            for (int i = 0; i <= seg; i++)
            {
                float a = 2f * Mathf.PI * i / seg;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(dir * 0.5f + Vector3.down); n.Add(dir); uv.Add(new Vector2(i / (float)seg, 0f));
                v.Add(dir * 0.5f + Vector3.up); n.Add(dir); uv.Add(new Vector2(i / (float)seg, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
                tri.Add(b + 1); tri.Add(b + 3); tri.Add(b + 2);
            }
            // caps
            foreach (float yCap in new[] { 1f, -1f })
            {
                int center = v.Count;
                Vector3 normal = yCap > 0 ? Vector3.up : Vector3.down;
                v.Add(new Vector3(0f, yCap, 0f)); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= seg; i++)
                {
                    float a = 2f * Mathf.PI * i / seg;
                    v.Add(new Vector3(Mathf.Cos(a) * 0.5f, yCap, Mathf.Sin(a) * 0.5f)); n.Add(normal);
                    uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < seg; i++)
                {
                    if (yCap > 0) { tri.Add(center); tri.Add(center + i + 2); tri.Add(center + i + 1); }
                    else { tri.Add(center); tri.Add(center + i + 1); tri.Add(center + i + 2); }
                }
            }
            return Finish(v, n, uv, tri);
        }

        static Mesh Finish(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri)
        {
            var m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            return m;
        }

        public static GameObject Create(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
            Material material, string name = null, bool castShadows = false)
        {
            var go = new GameObject(name ?? type.ToString());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(type);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Material mat, string name = null, bool shadows = false)
        {
            return Create(PrimitiveType.Cube, parent, pos, scale, mat, name, shadows);
        }

        public static GameObject Sphere(Transform parent, Vector3 pos, float diameter, Material mat, string name = null)
        {
            return Create(PrimitiveType.Sphere, parent, pos, Vector3.one * diameter, mat, name);
        }

        /// <summary>Cylinder lying along local Z (barrels, pipes). Length is the full length.</summary>
        public static GameObject CylinderZ(Transform parent, Vector3 pos, float diameter, float length, Material mat, string name = null, bool shadows = false)
        {
            var go = Create(PrimitiveType.Cylinder, parent, pos, new Vector3(diameter, length * 0.5f, diameter), mat, name, shadows);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return go;
        }

        /// <summary>Cylinder lying along local X.</summary>
        public static GameObject CylinderX(Transform parent, Vector3 pos, float diameter, float length, Material mat, string name = null, bool shadows = false)
        {
            var go = Create(PrimitiveType.Cylinder, parent, pos, new Vector3(diameter, length * 0.5f, diameter), mat, name, shadows);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return go;
        }

        /// <summary>Upright cylinder (Y axis). Height is the full height.</summary>
        public static GameObject CylinderY(Transform parent, Vector3 pos, float diameter, float height, Material mat, string name = null, bool shadows = false)
        {
            return Create(PrimitiveType.Cylinder, parent, pos, new Vector3(diameter, height * 0.5f, diameter), mat, name, shadows);
        }

        public static GameObject Empty(Transform parent, string name, Vector3 localPos = default(Vector3))
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>3D text label (gates, signs). Uses the built-in font.</summary>
        public static TextMesh Text3D(Transform parent, string text, Vector3 localPos, float charSize, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = GameAssets.UIFont;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = style;
            tm.color = color;
            var r = go.GetComponent<MeshRenderer>();
            if (tm.font != null) r.sharedMaterial = tm.font.material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return tm;
        }

        /// <summary>Sets material on every renderer below root.</summary>
        public static void SetMaterialRecursive(Transform root, Material mat)
        {
            var rs = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rs.Length; i++) rs[i].sharedMaterial = mat;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
        }
    }
}
