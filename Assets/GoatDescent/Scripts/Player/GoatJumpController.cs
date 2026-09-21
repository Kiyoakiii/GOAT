using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatJumpController : MonoBehaviour
    {
        [SerializeField] private float jumpVelocity = 5.15f;
        [SerializeField] private float coyoteTime = .12f;
        [SerializeField] private float jumpBuffer = .15f;
        private GoatController controller;
        private GoatGroundDetector ground;
        private float lastGrounded, pressedAt = -100f;
        public void Configure(GoatController c, GoatGroundDetector g) { controller = c; ground = g; }
        private void Awake() => CacheComponents();
        private void OnEnable() => CacheComponents();
        private void CacheComponents() { controller ??= GetComponent<GoatController>(); ground ??= GetComponent<GoatGroundDetector>(); }
        private void Update()
        {
            CacheComponents();
            if (Input.GetKeyDown(KeyCode.Space)) pressedAt = Time.time;
            if (ground && ground.IsGrounded) lastGrounded = Time.time;
            if (controller && Time.time - pressedAt <= jumpBuffer && Time.time - lastGrounded <= coyoteTime)
            {
                controller.Jump(jumpVelocity); pressedAt = -100f; lastGrounded = -100f;
            }
        }
    }
}
