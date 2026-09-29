using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Rigidbody), typeof(GoatGroundDetector))]
    public sealed class GoatController : MonoBehaviour
    {
        [Header("Heavy, springy movement")]
        [SerializeField] private float maxGroundSpeed = 5.35f;
        [SerializeField] private float groundAcceleration = 25f;
        [SerializeField] private float airAcceleration = 6.5f;
        [SerializeField] private float steepGripAngle = 60f;
        [SerializeField] private float slideAngle = 65f;
        [SerializeField] private float steepSpeedMultiplier = .7f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private GoatCliffGrip cliffGrip;
        private GoatHornVault hornVault;
        private Transform cameraTransform;
        private Vector2 input;
        private Vector3 moveDirection;
        private float externalImpulseUntil;
        private GoatInteraction interaction;
        private Vector3 remoteMove;
        private Vector3 networkVelocity;
        public void SetNetworkVelocity(Vector3 velocity) => networkVelocity = velocity;
        public bool NetworkOwnedByRemote { get; private set; }
        public bool IsPredatorCarried { get; private set; }
        public void SetPredatorCarried(bool carried)
        {
            IsPredatorCarried = carried;
            if (carried) ClearInput();
        }
        public void SetRemoteControl(bool enabled)
        {
            NetworkOwnedByRemote = enabled;
            if (!enabled) remoteMove = Vector3.zero;
        }
        public void SetRemoteMove(Vector3 worldDirection)
        { remoteMove = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(worldDirection, Vector3.up), 1f); }

        public Vector3 Velocity => !MountainAuthority.IsHost ? networkVelocity : body ? body.linearVelocity : Vector3.zero;
        public bool Grounded => ground && ground.IsGrounded;
        public Vector3 MoveDirection => moveDirection;
        public bool IsBracingForJump { get; set; }
        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround) { body = targetBody; ground = targetGround; }
        public void ClearInput() { input = Vector2.zero; moveDirection = Vector3.zero; IsBracingForJump = false; }
        public void ReceiveImpulse(Vector3 impulse, float recoverySeconds)
        {
            CacheComponents();
            externalImpulseUntil = Mathf.Max(externalImpulseUntil, Time.time + recoverySeconds);
            body.AddForce(impulse, ForceMode.Impulse);
        }
        private void Awake() => CacheComponents();
        private void OnEnable() => CacheComponents();
        private void CacheComponents()
        {
            // Runtime-created objects survive an Editor domain reload, while private references do not.
            // Reacquire them so movement does not silently stop after a script recompilation in Play Mode.
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
            cliffGrip ??= GetComponent<GoatCliffGrip>();
            hornVault ??= GetComponent<GoatHornVault>();
            interaction ??= GetComponent<GoatInteraction>();
        }

        private void Start() { cameraTransform = Camera.main ? Camera.main.transform : null; Cursor.lockState = CursorLockMode.Locked; }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
            else if (Input.GetMouseButtonDown(0))
            {
                Vector3 pointer = Input.mousePosition;
                bool overNetworkPanel = pointer.x >= Screen.width - 346f
                    && pointer.y >= Screen.height - 230f && pointer.y <= Screen.height - 114f;
                if (!overNetworkPanel) Cursor.lockState = CursorLockMode.Locked;
            }
            if (!MountainAuthority.IsHost || IsPredatorCarried) { ClearInput(); return; }
            if (NetworkOwnedByRemote) return;
            if (!GoatLocalControl.AllowsInput(this)) { ClearInput(); return; }
            // Read named keys directly: this does not depend on an Input Manager axis being configured in the project.
            input = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        }
        private void FixedUpdate()
        {
            CacheComponents();
            if (!MountainAuthority.IsHost || !body || !ground || IsPredatorCarried) return;
            if (!NetworkOwnedByRemote && !GoatLocalControl.AllowsInput(this)) return;
            if ((cliffGrip && cliffGrip.IsGripping) || (hornVault && hornVault.IsVaulting)) return;
            cameraTransform ??= Camera.main ? Camera.main.transform : null;
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
            Vector3 desired = NetworkOwnedByRemote ? remoteMove : forward * input.y + right * input.x;
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            moveDirection = Vector3.ProjectOnPlane(desired, Vector3.up).normalized;
            Vector3 surfaceNormal = ground.IsGrounded ? ground.GroundNormal : Vector3.up;
            if (ground.IsGrounded) desired = Vector3.ProjectOnPlane(desired, surfaceNormal).normalized;
            float topSpeed = maxGroundSpeed * (ground.IsGrounded && ground.SlopeAngle > steepGripAngle ? steepSpeedMultiplier : 1f) * (IsBracingForJump && ground.IsGrounded ? .42f : 1f);
            Vector3 velocityOnSurface = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            Vector3 wantedVelocity = desired * topSpeed;
            float acceleration = ground.IsGrounded ? groundAcceleration : airAcceleration;
            var loose = ground.IsGrounded && ground.GroundHit.collider
                ? ground.GroundHit.collider.GetComponent<LooseSurface>() : null;
            if (loose) acceleration *= loose.TractionMultiplier;
            if (Time.time < externalImpulseUntil) acceleration *= .08f;
            if (interaction && interaction.IsLinked)
            {
                // Braking may not erase the weight of the hanging partner.
                if (desired.sqrMagnitude < .001f) return;
                acceleration *= ground.IsGrounded ? .85f : .12f;
                wantedVelocity *= .4f;
            }
            Vector3 change = Vector3.ClampMagnitude(wantedVelocity - velocityOnSurface, acceleration * Time.fixedDeltaTime);
            // Write the controlled tangential velocity directly. ForceMode.VelocityChange can be swallowed by a
            // freshly-resting contact in PhysX, which looks like WASD has stopped working on a ledge.
            body.linearVelocity += change;
            if (ground.IsGrounded && ground.SlopeAngle > slideAngle)
            {
                Vector3 slide = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
                body.AddForce(slide * (ground.SlopeAngle - slideAngle) * 1.7f, ForceMode.Acceleration);
            }
        }

        public void Jump(float jumpVelocity, float forwardBoost = 0f)
        {
            if (!body || IsPredatorCarried) return;
            body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Max(0, body.linearVelocity.y), body.linearVelocity.z);
            Vector3 impulse = Vector3.up * jumpVelocity;
            if (moveDirection.sqrMagnitude > .01f) impulse += moveDirection * forwardBoost;
            body.AddForce(impulse, ForceMode.VelocityChange);
            GetComponent<GoatPhysicalBody>()?.BeginJump(impulse);
        }
    }
}
