using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeaponFactory.Utilities
{
    public enum Ease { Linear, OutQuad, InQuad, InOutQuad, OutBack, OutElastic, OutCubic }

    /// <summary>
    /// Tiny tweening engine (no external packages). Tweens bound to a UnityEngine.Object
    /// target stop automatically when the target is destroyed.
    /// </summary>
    public class Tween : MonoBehaviour
    {
        class TweenData
        {
            public UnityEngine.Object target;
            public bool hasTarget;
            public float time, duration, delay;
            public bool unscaled;
            public Ease ease;
            public Action<float> update;
            public Action complete;
            public bool alive;
            public int id;
        }

        static Tween _instance;
        static int _nextId = 1;
        readonly List<TweenData> _tweens = new List<TweenData>(64);
        readonly Stack<TweenData> _free = new Stack<TweenData>();

        static Tween Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Tween]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Tween>();
                }
                return _instance;
            }
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InQuad: return t * t;
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                    {
                        const float c1 = 1.70158f, c3 = c1 + 1f;
                        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                    }
                case Ease.OutElastic:
                    {
                        if (t <= 0f) return 0f;
                        if (t >= 1f) return 1f;
                        const float c4 = (2f * Mathf.PI) / 3f;
                        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                    }
                default: return t;
            }
        }

        /// <summary>Generic tween. update receives the eased 0..1 progress.</summary>
        public static int Run(UnityEngine.Object target, float duration, Action<float> update, Ease ease = Ease.OutQuad,
            Action complete = null, float delay = 0f, bool unscaled = false)
        {
            var inst = Instance;
            var d = inst._free.Count > 0 ? inst._free.Pop() : new TweenData();
            d.target = target;
            d.hasTarget = target != null;
            d.time = 0f;
            d.duration = Mathf.Max(0.0001f, duration);
            d.delay = delay;
            d.unscaled = unscaled;
            d.ease = ease;
            d.update = update;
            d.complete = complete;
            d.alive = true;
            d.id = _nextId++;
            inst._tweens.Add(d);
            return d.id;
        }

        /// <summary>Stops every tween bound to target (without calling complete).</summary>
        public static void Kill(UnityEngine.Object target)
        {
            if (_instance == null || target == null) return;
            var list = _instance._tweens;
            for (int i = 0; i < list.Count; i++)
                if (list[i].alive && list[i].target == target) list[i].alive = false;
        }

        public static void Kill(int id)
        {
            if (_instance == null) return;
            var list = _instance._tweens;
            for (int i = 0; i < list.Count; i++)
                if (list[i].id == id) list[i].alive = false;
        }

        // ---------- convenience helpers ----------

        public static int Scale(Transform t, Vector3 to, float duration, Ease ease = Ease.OutBack, float delay = 0f, bool unscaled = false, Action complete = null)
        {
            Vector3 from = t.localScale;
            return Run(t, duration, p => t.localScale = Vector3.LerpUnclamped(from, to, p), ease, complete, delay, unscaled);
        }

        public static int MoveLocal(Transform t, Vector3 to, float duration, Ease ease = Ease.OutQuad, float delay = 0f, bool unscaled = false, Action complete = null)
        {
            Vector3 from = t.localPosition;
            return Run(t, duration, p => t.localPosition = Vector3.LerpUnclamped(from, to, p), ease, complete, delay, unscaled);
        }

        public static int Move(Transform t, Vector3 to, float duration, Ease ease = Ease.OutQuad, float delay = 0f, Action complete = null)
        {
            Vector3 from = t.position;
            return Run(t, duration, p => t.position = Vector3.LerpUnclamped(from, to, p), ease, complete, delay);
        }

        public static int Anchored(RectTransform t, Vector2 to, float duration, Ease ease = Ease.OutCubic, float delay = 0f, Action complete = null)
        {
            Vector2 from = t.anchoredPosition;
            return Run(t, duration, p => t.anchoredPosition = Vector2.LerpUnclamped(from, to, p), ease, complete, delay, true);
        }

        public static int Fade(CanvasGroup g, float to, float duration, float delay = 0f, Action complete = null)
        {
            float from = g.alpha;
            return Run(g, duration, p => g.alpha = Mathf.Lerp(from, to, p), Ease.Linear, complete, delay, true);
        }

        /// <summary>Quick "pop" — scales up then back to the original scale.</summary>
        static readonly Dictionary<Transform, Vector3> _punchRest = new Dictionary<Transform, Vector3>();

        public static int Punch(Transform t, float amount = 0.2f, float duration = 0.25f, bool unscaled = false)
        {
            Kill(t);
            Transform key = t; // keyed by reference (GetInstanceID is obsolete in Unity 6.x)
            Vector3 baseScale;
            // re-punching mid-animation must not inflate the rest scale
            if (!_punchRest.TryGetValue(key, out baseScale)) { baseScale = t.localScale; _punchRest[key] = baseScale; }
            return Run(t, duration, p =>
            {
                float s = 1f + Mathf.Sin(p * Mathf.PI) * amount * (1f - p * 0.5f);
                t.localScale = baseScale * s;
            }, Ease.Linear, () => { t.localScale = baseScale; _punchRest.Remove(key); }, 0f, unscaled);
        }

        /// <summary>Calls action after a delay. Pass an owner so it is cancelled if the owner is destroyed.</summary>
        public static int Delay(float seconds, Action action, bool unscaled = false, UnityEngine.Object owner = null)
        {
            return Run(owner, seconds, null, Ease.Linear, action, 0f, unscaled);
        }

        void Update()
        {
            float dt = Time.deltaTime, udt = Time.unscaledDeltaTime;
            // iterate by index: tweens may start new tweens in callbacks
            for (int i = 0; i < _tweens.Count; i++)
            {
                var d = _tweens[i];
                if (!d.alive) continue;
                if (d.hasTarget && d.target == null) { d.alive = false; continue; }
                float step = d.unscaled ? udt : dt;
                if (d.delay > 0f) { d.delay -= step; continue; }
                d.time += step;
                float p = Mathf.Clamp01(d.time / d.duration);
                try
                {
                    if (d.update != null) d.update(Evaluate(d.ease, p));
                    if (p >= 1f)
                    {
                        d.alive = false;
                        if (d.complete != null) d.complete();
                    }
                }
                catch (Exception e)
                {
                    d.alive = false;
                    Debug.LogException(e);
                }
            }
            // compact
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                if (_tweens[i].alive) continue;
                var d = _tweens[i];
                d.update = null; d.complete = null; d.target = null;
                _free.Push(d);
                _tweens.RemoveAt(i);
            }
        }
    }
}
