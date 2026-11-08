using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>
    /// Simple component pool. Objects are created lazily with the factory function, deactivated
    /// on release and re-activated on Get. No allocations after warm-up.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        readonly Stack<T> _free;
        readonly List<T> _all;
        readonly Func<T> _factory;
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;

        public int CountAll { get { return _all.Count; } }
        /// <summary>All pooled items (active and inactive). Do not modify.</summary>
        public List<T> All { get { return _all; } }
        public int CountActive { get { return _all.Count - _free.Count; } }

        public ObjectPool(Func<T> factory, int prewarm = 0, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory;
            _onGet = onGet;
            _onRelease = onRelease;
            _free = new Stack<T>(Mathf.Max(8, prewarm));
            _all = new List<T>(Mathf.Max(8, prewarm));
            for (int i = 0; i < prewarm; i++)
            {
                var item = _factory();
                item.gameObject.SetActive(false);
                _free.Push(item);
                _all.Add(item);
            }
        }

        public T Get()
        {
            T item = null;
            while (_free.Count > 0 && item == null) item = _free.Pop(); // skip destroyed entries
            if (item == null)
            {
                item = _factory();
                _all.Add(item);
            }
            item.gameObject.SetActive(true);
            if (_onGet != null) _onGet(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf) return; // already released
            if (_onRelease != null) _onRelease(item);
            item.gameObject.SetActive(false);
            _free.Push(item);
        }

        /// <summary>Releases every active item (e.g. on level end).</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null && _all[i].gameObject.activeSelf) Release(_all[i]);
        }

        public void ForEachActive(Action<T> action)
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null && _all[i].gameObject.activeSelf) action(_all[i]);
        }
    }

    /// <summary>Cached number strings so HUD/damage numbers do not allocate every frame.</summary>
    public static class NumberCache
    {
        const int Size = 10000;
        static readonly string[] _cache = new string[Size];

        public static string Get(int n)
        {
            if (n >= 0 && n < Size)
            {
                var s = _cache[n];
                if (s == null) _cache[n] = s = n.ToString();
                return s;
            }
            return n.ToString();
        }

        /// <summary>1234 -> "1234", 12345 -> "12.3K", 1234567 -> "1.2M".</summary>
        public static string Compact(int n)
        {
            if (n < Size) return Get(n);
            if (n < 1000000) return (n / 1000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K";
            return (n / 1000000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "M";
        }
    }
}
