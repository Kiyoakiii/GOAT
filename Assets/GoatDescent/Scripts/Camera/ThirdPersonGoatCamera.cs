using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonGoatCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float minDistance = 3.25f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float height = 2.2f;
        [SerializeField] private float sensitivity = 150f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private float collisionRadius = .22f;
        [SerializeField] private float lookAheadDistance = 5f;
        [SerializeField] private float lookDownOffset = 1f;
        [SerializeField] private Transform target;
        // The first safe ledge is below and slightly left of the summit; open the game looking at the decision.
        private float yaw = -27f, pitch = 20f;
        private bool hasInitialPosition;

        public void Configure(Transform newTarget) { target = newTarget; }
        public void Configure(Transform newTarget, float initialYaw)
        {
            target = newTarget;
            yaw = initialYaw;
            hasInitialPosition = false;
        }

        private void Start()
        {
            GetComponent<Camera>().fieldOfView = 68f;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void LateUpdate()
        {
            target ??= FindFirstObjectByType<GoatController>()?.transform;
            if (!target) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime, minPitch, maxPitch);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > .0001f)
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pivot = target.position + Vector3.up * height;
            float actualDistance = distance;
            // Ignore the goat's own capsule: otherwise the camera collides at zero distance and hides traversal.
            int playerLayer = LayerMask.NameToLayer("Player");
            int worldMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            if (Physics.SphereCast(pivot, collisionRadius, orbit * Vector3.back, out var hit, distance, worldMask, QueryTriggerInteraction.Ignore))
                actualDistance = Mathf.Max(.55f, hit.distance - .08f);

            Vector3 desiredPosition = pivot + orbit * Vector3.back * actualDistance;
            Vector3 viewForward = Vector3.ProjectOnPlane(orbit * Vector3.forward, Vector3.up).normalized;
            Vector3 lookAt = pivot + viewForward * lookAheadDistance - Vector3.up * lookDownOffset;
            Quaternion viewRotation = Quaternion.LookRotation(lookAt - desiredPosition, Vector3.up);

            if (!hasInitialPosition)
            {
                // A runtime-created camera starts at world origin. Do not make the player wait for it to cross a 130 m mountain.
                transform.SetPositionAndRotation(desiredPosition, viewRotation);
                hasInitialPosition = true;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-14f * Time.deltaTime));
                transform.rotation = viewRotation;
            }
        }
    }
}