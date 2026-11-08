using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Pooled world-space floating text: damage numbers, "+5" coins, etc.</summary>
    public class FloatingText : MonoBehaviour
    {
        static FloatingText _root;
        ObjectPool<FloatingTextItem> _pool;

        static FloatingText Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("[FloatingText]");
                    _root = go.AddComponent<FloatingText>();
                    _root._pool = new ObjectPool<FloatingTextItem>(_root.CreateItem, 24);
                }
                return _root;
            }
        }

        void OnDestroy() { if (_root == this) _root = null; }

        FloatingTextItem CreateItem()
        {
            var tm = Shapes.Text3D(transform, "", Vector3.zero, 0.05f, Color.white);
            tm.gameObject.AddComponent<Billboard>();
            var e = tm.gameObject.AddComponent<FloatingTextItem>();
            e.text = tm;
            return e;
        }

        public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float life = 0.8f)
        {
            var e = Root._pool.Get();
            e.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
            e.text.text = text;
            e.text.color = color;
            e.baseSize = 0.05f * size;
            e.text.characterSize = e.baseSize;
            e.life = e.maxLife = life;
            e.velocity = new Vector3(Random.Range(-0.4f, 0.4f), 2.6f, 0f);
            e.color = color;
        }

        void Update()
        {
            if (_pool == null) return;
            float dt = Time.deltaTime;
            var all = _pool.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || !e.gameObject.activeSelf) continue;
                e.life -= dt;
                if (e.life <= 0f) { _pool.Release(e); continue; }
                float t = 1f - e.life / e.maxLife;
                e.transform.position += e.velocity * dt;
                e.velocity.y -= 3.5f * dt;
                float pop = t < 0.15f ? Mathf.Lerp(1.6f, 1f, t / 0.15f) : 1f; // pop in
                e.text.characterSize = e.baseSize * pop;
                var c = e.color;
                c.a = t > 0.6f ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f; // fade out
                e.text.color = c;
            }
        }
    }
}
