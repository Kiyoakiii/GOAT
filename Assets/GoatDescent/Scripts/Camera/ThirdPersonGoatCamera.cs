using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonGoatCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 6.5f;
        [SerializeField] private float height = 1.65f;
        [SerializeField] private float sensitivity = 150f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private float collisionRadius = .22f;
        [SerializeField] private float lookAheadDistance = 1.8f;
        [SerializeField] private float lookDownOffset = .25f;
        [SerializeField] private Transform target;
        private float yaw, pitch = 18f;
        private bool hasInitialPosition;
        private float shake;
        private float fieldOfViewKick;
        private GoatController goat;
        private GoatGroundDetector ground;
        private float slopePitch;
        private GoatWallJumpController wall;
        public void Snap() { hasInitialPosition = false; shake = 0f; fieldOfViewKick = 0f; slopePitch = 0f; }
        public void Kick(float amount, float extraFieldOfView)
        {
            shake = Mathf.Max(shake, amount);
            fieldOfViewKick = Mathf.Max(fieldOfViewKick, extraFieldOfView);
        }
        public void Configure(Transform newTarget) { target = newTarget; }
        public void SetPitch(float degrees) { pitch = Mathf.Clamp(degrees, minPitch, maxPitch); Snap(); }
        public void SetTestCliffView()
        {
            distance = 6.4f;
            height = 2.1f;
            lookAheadDistance = 3.5f;
            lookDownOffset = .7f;
            pitch = 14f;
            Snap();
        }
        public void Configure(Transform newTarget, float initialYaw)
        {
            target = newTarget;
            yaw = initialYaw;
            hasInitialPosition = false;
        }
        private void Start() { GetComponent<Camera>().fieldOfView = 66f; Cursor.lockState = CursorLockMode.Locked; }
        private void LateUpdate()
        {
            target ??= FindFirstObjectByType<GoatController>()?.transform;
            if (!target) return;
            if (!goat) goat = target.GetComponent<GoatController>();
            if (!ground) ground = target.GetComponent<GoatGroundDetector>();
            if (!wall) wall = target.GetComponent<GoatWallJumpController>();
            float speed = goat ? goat.Velocity.magnitude : 0f;
            if (Cursor.lockState == CursorLockMode.Locked && !(wall && wall.IsAiming)) { yaw += Input.GetAxis("Mouse X") * sensitivity * Time.unscaledDeltaTime; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity * Time.unscaledDeltaTime, minPitch, maxPitch); }
            float wantedPitch = ground && ground.IsGrounded ? Mathf.Lerp(0f, Mathf.Min(80f, ground.SlopeAngle + 10f), Mathf.InverseLerp(30f, 62f, ground.SlopeAngle)) : slopePitch;
            slopePitch = Mathf.Lerp(slopePitch, wantedPitch, 1f - Mathf.Exp(-4f * Time.deltaTime));
            Quaternion orbit = Quaternion.Euler(Mathf.Max(pitch, slopePitch), yaw, 0); Vector3 pivot = target.position + Vector3.up * height;
            float followDistance = distance + Mathf.InverseLerp(6f, 20f, speed) * 1.6f;
            float actualDistance = followDistance;
            // Ignore the goat's own capsule: otherwise the camera collides at zero distance and hides traversal.
            int playerLayer = LayerMask.NameToLayer("Player");
            int worldMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            if (Physics.SphereCast(pivot, collisionRadius, orbit * Vector3.back, out var hit, followDistance, worldMask, QueryTriggerInteraction.Ignore)) actualDistance = Mathf.Max(.55f, hit.distance - .08f);
            Vector3 desiredPosition = pivot + orbit * Vector3.back * actualDistance;
            Vector3 viewForward = Vector3.ProjectOnPlane(orbit * Vector3.forward, Vector3.up).normalized;
            Vector3 lookAt = pivot + viewForward * lookAheadDistance - Vector3.up * lookDownOffset;
            Quaternion viewRotation = Quaternion.LookRotation(lookAt - desiredPosition, Vector3.up);
            if (!hasInitialPosition)
            {
                // Snap to the goat on the first frame, then follow smoothly.
                transform.SetPositionAndRotation(desiredPosition, viewRotation);
                hasInitialPosition = true;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-14f * Time.deltaTime));
                transform.rotation = viewRotation;
            }
            transform.position += Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * .8f);
            Camera view = GetComponent<Camera>();
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, 66f + Mathf.InverseLerp(5f, 20f, speed) * 9f + fieldOfViewKick, 1f - Mathf.Exp(-8f * Time.deltaTime));
            fieldOfViewKick = Mathf.MoveTowards(fieldOfViewKick, 0f, Time.deltaTime * 18f);
        }
    }
}
