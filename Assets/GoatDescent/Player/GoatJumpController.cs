using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatJumpController : MonoBehaviour
    {
        private GoatMechanicsSettings tuning;
        private GoatMechanicsSettings Tuning => tuning ? tuning : tuning=GoatMechanics.For(this);
        private float jumpVelocity => Tuning.jumpVelocity;
        private float airJumpVelocity => Tuning.airJumpVelocity;
        private float chargedVerticalBonus => Tuning.chargedVerticalBonus;
        private float chargedForwardBoost => Tuning.chargedForwardBoost;
        private float fullChargeSeconds => Tuning.fullChargeSeconds;
        private float coyoteTime => Tuning.coyoteTime;
        private float jumpBuffer => Tuning.jumpBuffer;
        private GoatController controller;
        private GoatGroundDetector ground;
        private GoatCliffGrip cliffGrip;
        private float lastGrounded = -100f;
        private float pressedAt = -100f;
        private float chargeStarted;
        private bool remoteSpaceDown, remoteSpaceUp;
        private bool groundJumpUsed, airJumpUsed, wasAirborne;
        private float trickUntil;
        public string CurrentTrick { get; private set; } = "";
        public bool IsTrickShowing => Time.time < trickUntil;
        public void AnnounceTrick(string name) => ShowTrick(name);

        public void NetworkSpaceDown() => remoteSpaceDown = true;
        public void NetworkSpaceUp() => remoteSpaceUp = true;

        public bool IsCharging { get; private set; }
        public float Charge01 => IsCharging ? Mathf.Clamp01((Time.time - chargeStarted) / fullChargeSeconds) : 0f;
        private bool HasFooting => ground && ground.IsGrounded && ground.SlopeAngle < 80f;
        public void Configure(GoatController c, GoatGroundDetector g) { controller = c; ground = g; }
        public void ResetInput() { CancelCharge(); pressedAt = -100f; lastGrounded = -100f; }
        public void ResetJumpState()
        {
            ResetInput();
            groundJumpUsed = airJumpUsed = wasAirborne = false;
            CurrentTrick = "";
            trickUntil = 0f;
        }
        private void Awake() => CacheComponents();
        private void OnEnable() { CacheComponents(); CancelCharge(); lastGrounded = -100f; pressedAt = -100f; }
        private void OnDisable() { CancelCharge(); pressedAt = -100f; }
        private void CacheComponents()
        {
            controller ??= GetComponent<GoatController>();
            ground ??= GetComponent<GoatGroundDetector>();
            cliffGrip ??= GetComponent<GoatCliffGrip>();
        }
        private void Update()
        {
            CacheComponents();
            if (!MountainAuthority.IsHost || !controller || !ground) return;
            if (controller.IsPredatorCarried) { ResetInput(); return; }
            var toilet=GetComponent<GoatToiletAction>();
            if(toilet && toilet.Active)
            {
                bool cancel=controller.NetworkOwnedByRemote ? remoteSpaceDown
                    : GoatLocalControl.AllowsInput(this) && Input.GetKeyDown(KeyCode.Space);
                remoteSpaceDown=remoteSpaceUp=false;
                if(cancel) toilet.Cancel();
                ResetInput();return;
            }
            if (GetComponent<GoatDescentTricks>()?.MadnessActive == true) { ResetInput(); return; }
            if (GetComponent<GoatSlopeBalance>()?.IsSlipping == true)
            { ResetInput(); return; }
            bool remote = controller.NetworkOwnedByRemote;
            if (!remote && !GoatLocalControl.AllowsInput(this)) { ResetInput(); return; }
            bool pressed = remote ? remoteSpaceDown : Input.GetKeyDown(KeyCode.Space);
            bool released = remote ? remoteSpaceUp : Input.GetKeyUp(KeyCode.Space);
            remoteSpaceDown = remoteSpaceUp = false;
            var comedy = GetComponent<GoatComedyActions>();
            if (comedy && comedy.Busy)
            {
                if (pressed && comedy.IsMounted) comedy.Dismount(true);
                else if (pressed && comedy.PlayingDead) comedy.BreakPretence();
                ResetInput(); return;
            }
            if (HasFooting && !groundJumpUsed) lastGrounded = Time.time;
            if (!ground.IsGrounded) wasAirborne = true;
            if (HasFooting && wasAirborne)
            {
                groundJumpUsed = airJumpUsed = wasAirborne = false;
                lastGrounded = Time.time;
            }
            // Space belongs to the cliff grip while the goat is hanging on rock.
            if (cliffGrip && (cliffGrip.IsGripping || cliffGrip.LastWallKickFrame == Time.frameCount)) { CancelCharge(); return; }
            if (pressed && !groundJumpUsed && (HasFooting || Time.time - lastGrounded <= coyoteTime))
            {
                IsCharging = true;
                chargeStarted = Time.time;
                controller.IsBracingForJump = true;
                GetComponent<GoatVisualController>()?.SetJumpPreparation(true);
            }
            else if (pressed && wasAirborne && !airJumpUsed)
            {
                CancelCharge();
                controller.Jump(airJumpVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(.1f);
                GetComponent<GoatSpectacle>()?.AirJump();
                airJumpUsed = true;
                ShowTrick("ДВОЙНОЙ!");
            }
            else if (pressed) pressedAt = Time.time;
            if (IsCharging)
            {
                bool steppedOff = !HasFooting && Time.time - lastGrounded > .02f;
                if (released || steppedOff) LaunchCharge();
            }
            else if (!groundJumpUsed && Time.time - pressedAt <= jumpBuffer && HasFooting)
            {
                controller.Jump(jumpVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(.14f);
                GetComponent<GoatSpectacle>()?.Jump(false);
                groundJumpUsed = true;
                ShowTrick("ПРЫГ!");
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
            GetComponent<GoatVisualController>()?.PlayTakeoff(.14f);
            GetComponent<GoatSpectacle>()?.Jump(charge > .5f);
            groundJumpUsed = true;
            ShowTrick(charge > .5f ? "ЗАРЯЖЕННЫЙ!" : "ПРЫГ!");
            pressedAt = -100f;
            lastGrounded = -100f;
        }
        private void CancelCharge()
        {
            IsCharging = false;
            if (controller) controller.IsBracingForJump = false;
            GetComponent<GoatVisualController>()?.SetJumpPreparation(false);
        }
        private void ShowTrick(string name)
        {
            CurrentTrick = name;
            trickUntil = Time.time + 1.1f;
        }
    }
}
