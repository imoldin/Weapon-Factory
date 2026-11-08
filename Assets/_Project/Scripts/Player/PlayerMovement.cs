using UnityEngine;

namespace WeaponFactory.Player
{
    /// <summary>
    /// Automatic forward movement + smooth horizontal steering. The visual child tilts
    /// (roll/yaw) with lateral speed for a juicy feel. Uses a kinematic Rigidbody so trigger
    /// interactions with the track are reliable.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        public float forwardSpeed = 9f;
        public float laneLimit = 3.6f;
        /// <summary>World units moved per full-screen-width drag.</summary>
        public float dragSensitivity = 9.5f;
        public float keyboardSpeed = 9f;
        public float steerSharpness = 18f;
        public Transform tiltTarget;

        public bool MoveForward { get; set; }
        public bool AllowSteer { get; set; }
        public float LateralVelocity { get; private set; }
        public float SpeedMultiplier { get; set; }

        PlayerInputHandler _input;
        Rigidbody _body;
        float _targetX;
        float _knockback;

        void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _body = GetComponent<Rigidbody>();
            SpeedMultiplier = 1f;
        }

        public void SetLimits(float limit)
        {
            laneLimit = limit;
            _targetX = Mathf.Clamp(_targetX, -limit, limit);
        }

        public void Teleport(Vector3 position)
        {
            transform.position = position;
            _targetX = position.x;
            if (_body != null) _body.position = position;
        }

        /// <summary>Pushes the player back a little and slows down briefly (obstacle hit).</summary>
        public void Knockback(float distance)
        {
            _knockback = distance;
            SpeedMultiplier = 0.4f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (AllowSteer)
            {
                _targetX += _input.DragDelta * dragSensitivity;
                _targetX += _input.KeyAxis * keyboardSpeed * dt;
                _targetX = Mathf.Clamp(_targetX, -laneLimit, laneLimit);
            }

            Vector3 p = transform.position;
            float prevX = p.x;
            p.x = Mathf.Lerp(p.x, _targetX, 1f - Mathf.Exp(-steerSharpness * dt));

            SpeedMultiplier = Mathf.MoveTowards(SpeedMultiplier, 1f, dt * 0.9f);
            if (MoveForward) p.z += forwardSpeed * SpeedMultiplier * dt;
            if (_knockback > 0f)
            {
                float step = Mathf.Min(_knockback, dt * 12f);
                p.z -= step;
                _knockback -= step;
            }

            // Kinematic body: moving the transform is synced to physics before the next step.
            transform.position = p;

            LateralVelocity = (p.x - prevX) / dt;
            if (tiltTarget != null)
            {
                float yaw = Mathf.Clamp(LateralVelocity * 1.6f, -18f, 18f);
                float roll = Mathf.Clamp(-LateralVelocity * 2.2f, -22f, 22f);
                var target = Quaternion.Euler(0f, yaw, roll);
                tiltTarget.localRotation = Quaternion.Slerp(tiltTarget.localRotation, target, 1f - Mathf.Exp(-10f * dt));
            }
        }
    }
}
