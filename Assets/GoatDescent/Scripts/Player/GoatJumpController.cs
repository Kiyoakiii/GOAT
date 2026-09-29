using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatJumpController : MonoBehaviour
    {
        [SerializeField] private float jumpVelocity = 5.15f;
        [SerializeField] private float chargedVerticalBonus = 1.15f;
        [SerializeField] private float chargedForwardBoost = 2.4f;
        [SerializeField] private float fullChargeSeconds = .72f;
        [SerializeField] private float coyoteTime = .12f;
        [SerializeField] private float jumpBuffer = .15f;
        private GoatController controller;
        private GoatGroundDetector ground;
        private GoatCliffGrip cliffGrip;
        private GoatHornVault hornVault;
        private float lastGrounded = -100f;
        private float pressedAt = -100f;
        private float chargeStarted;
        private bool remoteSpaceDown, remoteSpaceUp;

        public void NetworkSpaceDown() => remoteSpaceDown = true;
        public void NetworkSpaceUp() => remoteSpaceUp = true;

        public bool IsCharging { get; private set; }
        public float Charge01 => IsCharging ? Mathf.Clamp01((Time.time - chargeStarted) / fullChargeSeconds) : 0f;
        private bool HasFooting => ground && ground.IsGrounded && ground.SlopeAngle < 55f;
        public void Configure(GoatController c, GoatGroundDetector g) { controller = c; ground = g; }
        public void ResetInput() { CancelCharge(); pressedAt = -100f; lastGrounded = -100f; }
        private void Awake() => CacheComponents();
        private void OnEnable() { CacheComponents(); CancelCharge(); lastGrounded = -100f; pressedAt = -100f; }
        private void OnDisable() { CancelCharge(); pressedAt = -100f; }
        private void CacheComponents()
        {
            controller ??= GetComponent<GoatController>();
            ground ??= GetComponent<GoatGroundDetector>();
            cliffGrip ??= GetComponent<GoatCliffGrip>();
            hornVault ??= GetComponent<GoatHornVault>();
        }
        private void Update()
        {
            CacheComponents();
            if (!MountainAuthority.IsHost || !controller || !ground) return;
            if (controller.IsPredatorCarried) { ResetInput(); return; }
            bool remote = controller.NetworkOwnedByRemote;
            if (!remote && !GoatLocalControl.AllowsInput(this)) { ResetInput(); return; }
            bool pressed = remote ? remoteSpaceDown : Input.GetKeyDown(KeyCode.Space);
            bool released = remote ? remoteSpaceUp : Input.GetKeyUp(KeyCode.Space);
            remoteSpaceDown = remoteSpaceUp = false;
            if (hornVault && hornVault.IsVaulting) { CancelCharge(); return; }
            if (HasFooting) lastGrounded = Time.time;
            // Space belongs to the cliff grip while the goat is hanging on rock.
            if (cliffGrip && (cliffGrip.IsGripping || cliffGrip.LastWallKickFrame == Time.frameCount)) { CancelCharge(); return; }
            if (pressed)
            {
                if (Time.time - lastGrounded <= coyoteTime)
                {
                    IsCharging = true;
                    chargeStarted = Time.time;
                    controller.IsBracingForJump = true;
                }
                else pressedAt = Time.time;
            }
            if (IsCharging)
            {
                bool steppedOff = !HasFooting && Time.time - lastGrounded > .02f;
                if (released || steppedOff) LaunchCharge();
            }
            else if (Time.time - pressedAt <= jumpBuffer && HasFooting)
            {
                controller.Jump(jumpVelocity);
                pressedAt = -100f;
                lastGrounded = -100f;
            }
        }
        private void LaunchCharge()
        {
            float charge = Charge01;
            CancelCharge();
            if (Time.time - lastGrounded > coyoteTime) return;
            controller.Jump(jumpVelocity + chargedVerticalBonus * charge, chargedForwardBoost * charge);
            pressedAt = -100f;
            lastGrounded = -100f;
        }
        private void CancelCharge()
        {
            IsCharging = false;
            if (controller) controller.IsBracingForJump = false;
        }
    }
}
