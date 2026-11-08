using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaponFactory.Audio;
using WeaponFactory.Utilities;

namespace WeaponFactory.UI
{
    /// <summary>Keeps content inside the device safe area (notches, rounded corners, home indicator).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect _last;
        Vector2Int _lastScreen;

        void Awake() { Apply(); }
        void Update()
        {
            if (Screen.safeArea != _last || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            Rect safe = Screen.safeArea;
            _last = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = safe.position, max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
