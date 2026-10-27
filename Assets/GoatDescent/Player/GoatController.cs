using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Rigidbody), typeof(GoatGroundDetector))]
    [RequireComponent(typeof(GoatWeatherResponse))]
    public sealed class GoatController : MonoBehaviour
    {
        private GoatMechanicsSettings tuning;
        private GoatMechanicsSettings Tuning => tuning ? tuning : tuning=GoatMechanics.For(this);
        private float maxGroundSpeed => Tuning.maxGroundSpeed;
        private float groundAcceleration => Tuning.groundAcceleration;
        private float airAcceleration => Tuning.airAcceleration;
        private float steepGripAngle => Tuning.steepGripAngle;
        private float slideAngle => Tuning.slideAngle;
        private float steepSpeedMultiplier => Tuning.steepSpeedMultiplier;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private GoatCliffGrip cliffGrip;
        private Transform cameraTransform;
        private Vector2 input;
        private Vector3 moveDirection;
        private float externalImpulseUntil;
        private Vector3 windDrift;
        private float windUntil;
        public void ApplyWind(Vector3 drift)
        {
            windDrift = Vector3.ClampMagnitude(drift, 1f);
            windUntil = Time.time + .1f;
        }
        private GoatInteraction interaction;
        private GoatGripController grip;
        private GoatSlopeBalance balance;
        private GoatWeatherResponse weatherResponse;
        private Vector3 remoteMove;
        private bool remoteBrake, remoteSuperHooves, remoteFirstPerson;
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
                remoteFirstPerson = false;
                remoteHorizontal = remoteVertical = 0f;
            }
        }
        public void SetRemoteMove(Vector3 worldDirection)
        { remoteMove = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(worldDirection, Vector3.up), 1f); }
        public void SetRemoteMountainInput(bool brake, bool superHooves, float horizontal,
            float vertical, Vector3 cameraRight, bool firstPerson = false)
        {
            remoteBrake = brake;
            remoteSuperHooves = superHooves;
            remoteHorizontal = horizontal;
            remoteVertical = vertical;
            remoteCameraRight = cameraRight;
            remoteFirstPerson = firstPerson;
        }
        public bool TryGetFirstPersonFacing(out Vector3 facing)
        {
            facing = Vector3.zero;
            if (NetworkOwnedByRemote)
            {
                if (!remoteFirstPerson || remoteCameraRight.sqrMagnitude < .1f) return false;
                facing = Vector3.Cross(remoteCameraRight, Vector3.up).normalized;
                return true;
            }
            var local = GetComponent<GoatLocalControl>();
            if (local && !local.IsControlled) return false;
            var camera = Camera.main ? Camera.main.GetComponent<ThirdPersonGoatCamera>() : null;
            if (!camera || !camera.IsFirstPerson || !camera.Follows(transform)) return false;
            facing = camera.PlanarLook;
            return true;
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
        public bool IsBackingUp => HasMovementInput && VerticalInput < -.1f;
        // Travel and facing are different when retreating. Keep MoveDirection
        // as the actual travel direction for jumps and the mountain mechanics.
        public Vector3 MovementFacing => IsBackingUp ? -moveDirection : moveDirection;
        public bool HasMovementInput => NetworkOwnedByRemote
            ? remoteMove.sqrMagnitude > .0001f : input.sqrMagnitude > .0001f;
        public bool IsBracingForJump { get; set; }
        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround) { body = targetBody; ground = targetGround; }
        public void ClearInput() { input = Vector2.zero; moveDirection = Vector3.zero; IsBracingForJump = false; }
        public void ReceiveImpulse(Vector3 impulse, float recoverySeconds)
        {
            CacheComponents();
            if (impulse.magnitude > 8f) GetComponent<GoatEmotionController>()?.ReactToImpact(impulse.magnitude / 45f);
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
            weatherResponse ??= GetComponent<GoatWeatherResponse>();
        }

        private void Start() { cameraTransform = Camera.main ? Camera.main.transform : null; Cursor.lockState = CursorLockMode.Locked; }
        private void Update()
        {
            if (GetComponent<GoatWaterDrinking>()?.Active == true) { ClearInput(); SetRemoteMove(Vector3.zero); return; }
            if(GetComponent<GoatToiletAction>()?.Active==true) {ClearInput();return;}
            if(GoatSkinMenu.IsOpen || GoatSettingsMenu.BlocksInput) { ClearInput(); return; }
            if (Input.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;
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
            if (GetComponent<GoatDescentTricks>()?.MadnessActive == true
                || GetComponent<GoatToiletAction>()?.Active == true
                || GetComponent<GoatComedyActions>()?.BlocksMovement == true
                || GetComponent<GoatPunchBuff>()?.PerformingPair == true) return;
            bool canDrive = NetworkOwnedByRemote || GoatLocalControl.AllowsInput(this);
            Vector3 wind = Time.time < windUntil ? windDrift : Vector3.zero;
            if (weatherResponse) wind += weatherResponse.Wind;
            if (!canDrive && wind.sqrMagnitude < .001f) return;
            if (cliffGrip && cliffGrip.IsGripping) return;
            if (body.isKinematic || (grip && grip.SuperActive) || (balance && balance.IsSlipping)) return;
            cameraTransform ??= Camera.main ? Camera.main.transform : null;
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
            Vector3 desired = NetworkOwnedByRemote ? remoteMove : forward * input.y + right * input.x;
            if (!canDrive) desired = Vector3.zero;
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            moveDirection = Vector3.ProjectOnPlane(desired, Vector3.up).normalized;
            if (!ground.IsGrounded)
            {
                // Keep the mountain route's takeoff momentum while allowing steering.
                if (desired.sqrMagnitude > .01f)
                    body.AddForce(desired * airAcceleration * (interaction && interaction.IsLinked ? .12f : 1f),
                        ForceMode.Acceleration);
                body.AddForce(wind, ForceMode.Acceleration);
                return;
            }
            Vector3 surfaceNormal = ground.IsGrounded ? ground.GroundNormal : Vector3.up;
            desired = Vector3.ProjectOnPlane(desired, surfaceNormal).normalized;
            // Let the hooves turn the body before accelerating across it. This
            // also prevents a newly chosen camera direction from sliding the
            // goat sideways while the physical root is still facing elsewhere.
            if (desired.sqrMagnitude > .01f && GetComponent<GoatPhysicalBody>()?.RootIsTumbling != true)
            {
                Vector3 facing = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up);
                Vector3 travel = Vector3.ProjectOnPlane(desired, Vector3.up);
                if (IsBackingUp) travel = -travel;
                float turnAngle = Vector3.Angle(facing, travel);
                float traction = 1f - Mathf.Clamp01((turnAngle - 15f) / 55f);
                desired *= traction;
            }
            bool braking = WantsBrake;
            float downhillProgress = Mathf.InverseLerp(66f, 89f, ground.SlopeAngle);
            float downhillSpeed = Mathf.SmoothStep(0f, 1f, downhillProgress) * 7f;
            if (braking) downhillSpeed *= .28f;
            if (balance && balance.HoovesHolding >= 3 && VerticalInput <= 0f && body.linearVelocity.magnitude < 2.5f)
                downhillSpeed *= .2f;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
            float topSpeed = maxGroundSpeed * (ground.IsGrounded && ground.SlopeAngle > steepGripAngle ? steepSpeedMultiplier : 1f) * (IsBracingForJump && ground.IsGrounded ? .42f : 1f);
            if (IsBackingUp) topSpeed *= .45f;
            Vector3 velocityOnSurface = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            Vector3 wantedVelocity = desired * topSpeed + downhill * downhillSpeed;
            wantedVelocity += Vector3.ProjectOnPlane(wind, surfaceNormal) * (braking ? .2f : 1f);
            float acceleration = braking ? 32f : Mathf.Lerp(groundAcceleration, 12f,
                Mathf.InverseLerp(28f, 65f, ground.SlopeAngle));
            float wetness = weatherResponse ? weatherResponse.Wetness : 0f;
            acceleration *= Mathf.Lerp(1f, braking ? .35f : .2f, wetness);
            // Rain also makes gentle inclines slick, not just the steep cliff zone.
            wantedVelocity += downhill * (wetness * Mathf.InverseLerp(5f, 50f, ground.SlopeAngle)
                * (braking ? .22f : 1.05f));
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
