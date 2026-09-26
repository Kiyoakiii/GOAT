using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatJumpController : MonoBehaviour
    {
        [SerializeField] private float jumpVelocity = 4.35f;
        [SerializeField] private float coyoteTime = .2f;
        [SerializeField] private float jumpBuffer = .25f;
        [SerializeField] private float pushOffDelay = .1f;

        private GoatController controller;
        private GoatGroundDetector ground;
        private float lastGrounded = -100f;
        private float pressedAt = -100f;
        private bool groundJumpUsed;
        private bool wasAirborne;
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
            wasAirborne = false;
            GetComponent<GoatVisualController>()?.SetJumpPreparation(false);
            CurrentTrick = "";
            trickUntil = 0f;
        }

        private void Update()
        {
            CacheComponents();
            if (!controller || !ground) return;
            if (GetComponent<GoatSlopeBalance>()?.IsSlipping == true)
            {
                pressedAt = -100f;
                GetComponent<GoatVisualController>()?.SetJumpPreparation(false);
                return;
            }

            bool grounded = ground.IsGrounded;
            if (grounded) lastGrounded = Time.time;

            // The probe may still read grounded briefly after takeoff.
            if (!grounded) wasAirborne = true;

            if (grounded && wasAirborne)
            {
                groundJumpUsed = false;
                wasAirborne = false;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                pressedAt = Time.time;
                if (grounded || Time.time - lastGrounded <= coyoteTime)
                    GetComponent<GoatVisualController>()?.SetJumpPreparation(true);
            }
            if (Time.time - pressedAt > jumpBuffer)
            {
                GetComponent<GoatVisualController>()?.SetJumpPreparation(false);
                return;
            }
            if (Time.time - pressedAt < pushOffDelay) return;

            if (!groundJumpUsed && Time.time - lastGrounded <= coyoteTime)
            {
                controller.Jump(jumpVelocity);
                GetComponent<GoatVisualController>()?.PlayTakeoff(.14f);
                GetComponent<GoatVisualController>()?.SetJumpPreparation(false);
                GetComponent<GoatSpectacle>()?.Jump(false);
                groundJumpUsed = true;
                ShowTrick("ПРЫГ!");
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
