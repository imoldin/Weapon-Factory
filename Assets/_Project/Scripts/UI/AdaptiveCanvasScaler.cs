using UnityEngine;
using UnityEngine.UI;

namespace WeaponFactory.UI
{
    /// <summary>
    /// Keeps the portrait-designed UI on screen for any aspect ratio: on tall/portrait screens the
    /// canvas scales mostly by width, on landscape screens (PC windows, tablets) it scales by height
    /// so tall panels and bottom buttons never fall off the screen.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        CanvasScaler _scaler;
        int _w, _h;

        void Awake() { _scaler = GetComponent<CanvasScaler>(); Apply(); }

        void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Apply();
        }

        void Apply()
        {
            _w = Screen.width;
            _h = Screen.height;
            if (_scaler == null || _h <= 0) return;
            float aspect = _w / (float)_h;
            float reference = UIFactory.ReferenceResolution.x / UIFactory.ReferenceResolution.y; // 0.5625
            // narrower than (or equal to) the reference: match width; wider: match height
            _scaler.matchWidthOrHeight = aspect <= reference ? 0.35f : 1f;
        }
    }
}
