using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Faces the main camera (health bars, damage numbers).</summary>
    public class Billboard : MonoBehaviour
    {
        static Camera _cam;

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.rotation = _cam.transform.rotation;
        }
    }
}
