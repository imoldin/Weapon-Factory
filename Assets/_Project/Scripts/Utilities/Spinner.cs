using UnityEngine;

namespace WeaponFactory.Utilities
{
    /// <summary>Rotates continuously around a local axis.</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float speed = 90f; // degrees per second
        public bool unscaled;

        void Update()
        {
            transform.Rotate(axis, speed * (unscaled ? Time.unscaledDeltaTime : Time.deltaTime), Space.Self);
        }
    }
}
