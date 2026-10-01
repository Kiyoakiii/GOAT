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
        [SerializeField] private float steepGripAngle = 73f;
        [SerializeField] private float slideAngle = 86f;
        [SerializeField] private float steepSpeedMultiplier = .85f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private GoatCliffGrip cliffGrip;
        private Transform cameraTransform;
        private Vector2 input;
        private Vector3 moveDirection;
        private float externalImpulseUntil;
        private GoatInteraction interaction;
        private GoatGripController grip;
        private GoatSlopeBalance balance;
        private Vector3 remoteMove;
        private bool remoteBrake, remoteSuperHooves;
        private float remoteHorizontal, remoteVertical;
        private Vector3 remoteCameraRight;
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
            if (!enabled)
            {
                remoteMove = Vector3.zero;
                remoteBrake = remoteSuperHooves = false;
                remoteHorizontal = remoteVertical = 0f;
            }
        }
        public void SetRemoteMove(Vector3 worldDirection)
        { remoteMove = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(worldDirection, Vector3.up), 1f); }
        public void SetRemoteMountainInput(bool brake, bool superHooves, float horizontal,
            float vertical, Vector3 cameraRight)
        {
            remoteBrake = brake;
            remoteSuperHooves = superHooves;
            remoteHorizontal = horizontal;
            remoteVertical = vertical;
            remoteCameraRight = cameraRight;
        }
        public bool WantsBrake => NetworkOwnedByRemote ? remoteBrake
            : Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public bool WantsSuperHooves => NetworkOwnedByRemote ? remoteSuperHooves
            : Input.GetKey(KeyCode.LeftAlt);
        public float HorizontalInput => NetworkOwnedByRemote ? remoteHorizontal : input.x;
        public float VerticalInput => NetworkOwnedByRemote ? remoteVertical : input.y;
        public Vector3 InputCameraRight => NetworkOwnedByRemote ? remoteCameraRight
            : (Camera.main ? Camera.main.transform.right : Vector3.right);

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
            interaction ??= GetComponent<GoatInteraction>();
            grip ??= GetComponent<GoatGripController>();
            balance ??= GetComponent<GoatSlopeBalance>();
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
            if (cliffGrip && cliffGrip.IsGripping) return;
            if (body.isKinematic || (grip && grip.SuperActive) || (balance && balance.IsSlipping)) return;
            cameraTransform ??= Camera.main ? Camera.main.transform : null;
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
            Vector3 desired = NetworkOwnedByRemote ? remoteMove : forward * input.y + right * input.x;
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            moveDirection = Vector3.ProjectOnPlane(desired, Vector3.up).normalized;
            if (!ground.IsGrounded)
            {
                // Keep the mountain route's takeoff momentum while allowing steering.
                if (desired.sqrMagnitude > .01f)
                    body.AddForce(desired * airAcceleration * (interaction && interaction.IsLinked ? .12f : 1f),
                        ForceMode.Acceleration);
                return;
            }
            Vector3 surfaceNormal = ground.IsGrounded ? ground.GroundNormal : Vector3.up;
            desired = Vector3.ProjectOnPlane(desired, surfaceNormal).normalized;
            bool braking = WantsBrake;
            float downhillProgress = Mathf.InverseLerp(66f, 89f, ground.SlopeAngle);
            float downhillSpeed = Mathf.SmoothStep(0f, 1f, downhillProgress) * 7f;
            if (braking) downhillSpeed *= .28f;
            if (balance && balance.HoovesHolding >= 3 && VerticalInput <= 0f && body.linearVelocity.magnitude < 2.5f)
                downhillSpeed *= .2f;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
            float topSpeed = maxGroundSpeed * (ground.IsGrounded && ground.SlopeAngle > steepGripAngle ? steepSpeedMultiplier : 1f) * (IsBracingForJump && ground.IsGrounded ? .42f : 1f);
            Vector3 velocityOnSurface = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            Vector3 wantedVelocity = desired * topSpeed + downhill * downhillSpeed;
            float acceleration = braking ? 32f : Mathf.Lerp(groundAcceleration, 12f,
                Mathf.InverseLerp(28f, 65f, ground.SlopeAngle));
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
                float excess = Mathf.InverseLerp(slideAngle, 89f, ground.SlopeAngle);
                body.AddForce(downhill * (excess * 2.6f + .5f), ForceMode.Acceleration);
                if (excess > .55f) body.AddForce(Vector3.down * .2f, ForceMode.Acceleration);
            }
            else if (ground.SlopeAngle > steepGripAngle && !braking)
            {
                body.AddForce(downhill * (ground.SlopeAngle - steepGripAngle) * .6f,
                    ForceMode.Acceleration);
            }
        }

        public void Jump(float jumpVelocity, float forwardBoost = 0f)
        {
            if (!body || IsPredatorCarried) return;
            grip?.ReleaseForJump();
            body.linearVelocity = new Vector3(body.linearVelocity.x * .76f,
                Mathf.Max(0, body.linearVelocity.y), body.linearVelocity.z * .76f);
            Vector3 impulse = Vector3.up * jumpVelocity;
            if (moveDirection.sqrMagnitude > .01f) impulse += moveDirection * forwardBoost;
            body.AddForce(impulse, ForceMode.VelocityChange);
            GetComponent<GoatPhysicalBody>()?.BeginJump(impulse);
        }

        public void Stomp(float fallVelocity)
        {
            if (!body) return;
            grip?.ReleaseForJump();
            Vector3 velocity = body.linearVelocity;
            velocity.y = -fallVelocity;
            body.linearVelocity = velocity;
        }

        public void WallJump(Vector3 wallNormal, float upwardVelocity, float outwardVelocity)
        {
            if (!body) return;
            Vector3 velocity = Vector3.ProjectOnPlane(body.linearVelocity, wallNormal);
            velocity.y = Mathf.Max(0f, velocity.y);
            Vector3 launch = Vector3.up * upwardVelocity + wallNormal * outwardVelocity;
            body.linearVelocity = velocity + launch;
            GetComponent<GoatPhysicalBody>()?.BeginJump(launch);
        }
    }
}
