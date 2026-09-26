using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Rigidbody), typeof(GoatGroundDetector))]
    public sealed class GoatController : MonoBehaviour
    {
        [Header("Heavy, springy movement")]
        [SerializeField] private float maxGroundSpeed = 5.35f;
        [SerializeField] private float groundAcceleration = 19f;
        [SerializeField] private float airAcceleration = 5f;
        [SerializeField] private float steepGripAngle = 60f;
        [SerializeField] private float slideAngle = 65f;
        [SerializeField] private float steepSpeedMultiplier = .7f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private Transform cameraTransform;
        private Vector2 input;
        private GoatGripController grip;
        private GoatWallJumpController wall;

        public Vector3 Velocity => body ? body.linearVelocity : Vector3.zero;
        public bool Grounded => ground && ground.IsGrounded;
        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround) { body = targetBody; ground = targetGround; }
        private void Awake() => CacheComponents();
        private void OnEnable() => CacheComponents();
        private void CacheComponents()
        {
            // Runtime-created objects survive an Editor domain reload, while private references do not.
            // Reacquire them so movement does not silently stop after a script recompilation in Play Mode.
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
        }

        private void Start() { cameraTransform = Camera.main ? Camera.main.transform : null; Cursor.lockState = CursorLockMode.Locked; }
        private void Update()
        {
            // Read named keys directly: this does not depend on an Input Manager axis being configured in the project.
            input = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
            if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
            else if (Input.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;
        }
        private void FixedUpdate()
        {
            CacheComponents();
            if (!body || !ground) return;
            grip ??= GetComponent<GoatGripController>();
            wall ??= GetComponent<GoatWallJumpController>();
            if (body.isKinematic || (grip && grip.IsGripping) || (wall && wall.IsAiming)
                || GetComponent<GoatSlopeBalance>()?.IsSlipping == true) return;
            cameraTransform ??= Camera.main ? Camera.main.transform : null;
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
            Vector3 desired = (forward * input.y + right * input.x); if (desired.sqrMagnitude > 1f) desired.Normalize();
            if (!ground.IsGrounded)
            {
                // Keep takeoff momentum; keys steer the flight instead of automatically braking it.
                if (desired.sqrMagnitude > .01f) body.AddForce(desired * airAcceleration, ForceMode.Acceleration);
                return;
            }
            Vector3 surfaceNormal = ground.IsGrounded ? ground.GroundNormal : Vector3.up;
            if (ground.IsGrounded) desired = Vector3.ProjectOnPlane(desired, surfaceNormal).normalized;
            bool braking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (grip && grip.Exhausted && ground.SlopeAngle >= 35f) braking = false;
            float downhillSpeed = ground.IsGrounded ? Mathf.InverseLerp(12f, 70f, ground.SlopeAngle) * 22f : 0f;
            if (braking) downhillSpeed *= .28f;
            Vector3 downhill = ground.IsGrounded ? Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized : Vector3.zero;
            float topSpeed = maxGroundSpeed * (ground.IsGrounded && ground.SlopeAngle > steepGripAngle ? steepSpeedMultiplier : 1f);
            Vector3 velocityOnSurface = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            Vector3 wantedVelocity = desired * topSpeed + downhill * downhillSpeed;
            float acceleration = ground.IsGrounded
                ? (braking ? 32f : Mathf.Lerp(groundAcceleration, 12f, Mathf.InverseLerp(28f, 65f, ground.SlopeAngle)))
                : airAcceleration;
            Vector3 change = Vector3.ClampMagnitude(wantedVelocity - velocityOnSurface, acceleration * Time.fixedDeltaTime);
            // Write the controlled tangential velocity directly. ForceMode.VelocityChange can be swallowed by a
            // freshly-resting contact in PhysX, which looks like WASD has stopped working on a ledge.
            body.linearVelocity += change;
            if (ground.IsGrounded && ground.SlopeAngle > slideAngle && !braking)
            {
                Vector3 slide = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
                body.AddForce(slide * (ground.SlopeAngle - slideAngle) * 1.7f, ForceMode.Acceleration);
            }
        }

        public void Jump(float jumpVelocity)
        {
            if (!body) return;
            GetComponent<GoatGripController>()?.ReleaseForJump();
            body.linearVelocity = new Vector3(body.linearVelocity.x * .76f,
                Mathf.Max(0, body.linearVelocity.y), body.linearVelocity.z * .76f);
            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }

        public void Stomp(float fallVelocity)
        {
            if (!body) return;
            GetComponent<GoatGripController>()?.ReleaseForJump();
            var velocity = body.linearVelocity;
            velocity.y = -fallVelocity;
            body.linearVelocity = velocity;
        }

        public void WallJump(Vector3 wallNormal, float upwardVelocity, float outwardVelocity)
        {
            if (!body) return;
            Vector3 velocity = Vector3.ProjectOnPlane(body.linearVelocity, wallNormal);
            velocity.y = Mathf.Max(0f, velocity.y);
            body.linearVelocity = velocity + Vector3.up * upwardVelocity + wallNormal * outwardVelocity;
        }
    }
}
