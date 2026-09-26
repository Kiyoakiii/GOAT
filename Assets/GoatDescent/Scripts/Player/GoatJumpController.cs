using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatJumpController : MonoBehaviour
    {
        [SerializeField] private float jumpVelocity = 5.15f;
        [SerializeField] private float rocketJumpVelocity = 8.2f;
        [SerializeField] private float airJumpVelocity = 4.5f;
        [SerializeField] private float stompFallVelocity = 13f;
        [SerializeField] private float stompBounceVelocity = 6.5f;
        [SerializeField] private float coyoteTime = .12f;
        [SerializeField] private float jumpBuffer = .15f;

        private GoatController controller;
        private GoatGroundDetector ground;
        private float lastGrounded = -100f;
        private float pressedAt = -100f;
        private bool rocketRequested;
        private bool groundJumpUsed;
        private bool airJumpUsed;
        private bool wasAirborne;
        private bool stomping;
        private float trickUntil;

        public string CurrentTrick { get; private set; } = "";
        public bool IsTrickShowing => Time.time < trickUntil;

        public void Configure(GoatController c, GoatGroundDetector g) { controller = c; ground = g; }
        private void Awake() => CacheComponents();
        private void OnEnable() => CacheComponents();
        private void CacheComponents() { controller ??= GetComponent<GoatController>(); ground ??= GetComponent<GoatGroundDetector>(); }

        public void ResetJumpState()
        {
            pressedAt = -100f;
            lastGrounded = -100f;
            groundJumpUsed = false;
            airJumpUsed = false;
            wasAirborne = false;
            stomping = false;
            CurrentTrick = "";
            trickUntil = 0f;
        }

        private void Update()
        {
            CacheComponents();
            if (!controller || !ground) return;
            if (GetComponent<GoatWallJumpController>()?.IsAiming == true) return;

            bool grounded = ground.IsGrounded;
            if (grounded) lastGrounded = Time.time;

            // The probe may still read grounded briefly after takeoff.
            if (!grounded) wasAirborne = true;

            if (stomping && grounded)
            {
                stomping = false;
                controller.Jump(stompBounceVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(.24f);
                GetComponent<GoatSpectacle>()?.StompBounce();
                groundJumpUsed = true;
                airJumpUsed = true;
                wasAirborne = false;
                ShowTrick("BOING! STOMP BOUNCE");
            }
            else if (grounded && wasAirborne)
            {
                groundJumpUsed = false;
                airJumpUsed = false;
                wasAirborne = false;
            }

            if (Input.GetKeyDown(KeyCode.C) && !grounded && !stomping)
            {
                stomping = true;
                pressedAt = -100f;
                controller.Stomp(stompFallVelocity);
                GetComponent<GoatSpectacle>()?.Stomp();
                ShowTrick("HOOVES OF DOOM!");
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                pressedAt = Time.time;
                rocketRequested = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }

            if (stomping || Time.time - pressedAt > jumpBuffer) return;

            if (!groundJumpUsed && Time.time - lastGrounded <= coyoteTime)
            {
                controller.Jump(rocketRequested ? rocketJumpVelocity : jumpVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(rocketRequested ? .24f : .14f);
                GetComponent<GoatSpectacle>()?.Jump(rocketRequested);
                groundJumpUsed = true;
                ShowTrick(rocketRequested ? "ROCKET GOAT!" : "BOOP!");
                pressedAt = -100f;
            }
            else if (!grounded && !airJumpUsed)
            {
                controller.Jump(airJumpVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(.18f);
                GetComponent<GoatSpectacle>()?.AirJump();
                airJumpUsed = true;
                ShowTrick("ME-E-E! AIR HOP");
                pressedAt = -100f;
            }
        }

        private void ShowTrick(string name)
        {
            CurrentTrick = name;
            trickUntil = Time.time + 1.1f;
        }

        public void AnnounceTrick(string name) => ShowTrick(name);
    }
}
