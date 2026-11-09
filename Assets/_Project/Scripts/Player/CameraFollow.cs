using UnityEngine;
using WeaponFactory.Utilities;

namespace WeaponFactory.Player
{
    public enum CameraMode { Running, Arena, Showcase }

    /// <summary>
    /// Smooth third-person follow camera (behind and above the weapon). Field of view adapts to the
    /// screen aspect so the whole track width stays visible on tall phones and tablets alike.
    /// Hierarchy: Rig (this) -> Shake (ScreenShake) -> Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public CameraMode mode = CameraMode.Running;
        public Camera Cam { get; private set; }

        Vector3 _offset = new Vector3(0f, 6.6f, -7.2f);
        Vector3 _lookAhead = new Vector3(0f, 0f, 7f);
        float _xFollow = 0.55f;
        float _desiredHorizontalFov = 50f;
        Vector3 _arenaFocus;

        public static CameraFollow Create(Color background)
        {
            var rig = new GameObject("CameraRig");
            var follow = rig.AddComponent<CameraFollow>();
            var shake = new GameObject("Shake");
            shake.transform.SetParent(rig.transform, false);
            shake.AddComponent<ScreenShake>();
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(shake.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 160f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();
            follow.Cam = cam;
            follow.UpdateFov();
            return follow;
        }

        public void SetArena(Vector3 focus)
        {
            mode = CameraMode.Arena;
            _arenaFocus = focus;
        }

        void UpdateFov()
        {
            if (Cam == null) return;
            float aspect = Mathf.Max(0.3f, Cam.aspect);
            float hRad = _desiredHorizontalFov * Mathf.Deg2Rad;
            float v = 2f * Mathf.Atan(Mathf.Tan(hRad * 0.5f) / aspect) * Mathf.Rad2Deg;
            Cam.fieldOfView = Mathf.Clamp(v, 55f, 85f);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 pos, look;
            Compute(out pos, out look);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos);
        }

        void Compute(out Vector3 pos, out Vector3 look)
        {
            Vector3 tp = target.position;
            if (mode == CameraMode.Arena)
            {
                // further back and lower: stays under the ceiling girders and keeps the weapon above the HUD
                pos = new Vector3(tp.x * 0.35f, tp.y + 7.0f, tp.z - 10f);
                look = new Vector3(tp.x * 0.25f, 0f, _arenaFocus.z - 9f);
            }
            else if (mode == CameraMode.Showcase)
            {
                pos = tp + new Vector3(0f, 1.2f, -4.2f);
                look = tp;
            }
            else
            {
                pos = new Vector3(tp.x * _xFollow, tp.y + _offset.y, tp.z + _offset.z);
                look = new Vector3(tp.x * 0.7f, tp.y * 0.3f, tp.z) + _lookAhead;
            }
        }

        void LateUpdate()
        {
            UpdateFov();
            if (target == null) return;
            Vector3 pos, look;
            Compute(out pos, out look);
            float dt = Time.deltaTime;
            float posSharp = mode == CameraMode.Arena ? 3f : 9f;
            // Keep up with forward motion exactly, smooth only lateral/vertical motion.
            Vector3 cur = transform.position;
            Vector3 next = Vector3.Lerp(cur, pos, 1f - Mathf.Exp(-posSharp * dt));
            if (mode == CameraMode.Running) next.z = pos.z;
            transform.position = next;
            var rot = Quaternion.LookRotation(look - next);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-8f * dt));
        }
    }
}
