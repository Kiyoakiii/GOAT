using UnityEngine;

namespace GoatDescent
{
    [DefaultExecutionOrder(250)]
    [RequireComponent(typeof(Camera), typeof(GoatFirstPersonView))]
    public sealed class ThirdPersonGoatCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 6.5f;
        [SerializeField] private float minDistance = 2.4f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float height = 1.65f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float collisionRadius = .28f;
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private float orbitSharpness = 30f;
        [SerializeField] private float zoomSharpness = 8f;
        [SerializeField] private Transform target;

        private readonly RaycastHit[] obstructionHits = new RaycastHit[64];
        private float yaw, pitch = 18f;
        private float viewYaw, viewPitch, resolvedDistance;
        private Vector3 followPivot;
        private bool hasInitialPosition;
        private float shake, fieldOfViewKick;
        private GoatController goat;
        private GoatWallJumpController wall;
        private Camera view;
        private GoatFirstPersonView firstPersonBody;
        private const string PerspectiveKey = "Goat.Camera.FirstPerson";
        [Header("First person")]
        [SerializeField] private float firstPersonLookSharpness = 55f;
        [SerializeField] private float firstPersonEyeSharpness = 16f;
        [SerializeField] private float firstPersonCollisionRadius = .095f;
        private Vector3 eyeOffset, viewInertia, inertiaVelocity, previousViewVelocity;
        private float viewRoll, previousAirSpeed;
        private bool wasViewGrounded;
        public bool IsFirstPerson { get; private set; }
        public bool Follows(Transform subject) => target == subject;
        public Vector3 PlanarLook => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

        public void TogglePerspective() => SetFirstPerson(!IsFirstPerson);
        public void SetFirstPerson(bool value)
        {
            IsFirstPerson = value;
            PlayerPrefs.SetInt(PerspectiveKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (value && target)
            {
                var visual = target.GetComponent<GoatVisualController>();
                Vector3 facing = visual ? visual.Facing : target.forward;
                yaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
                pitch = 4f;
            }
            Snap();
        }

        public void Snap()
        {
            hasInitialPosition = false;
            shake = fieldOfViewKick = 0f;
            viewInertia = inertiaVelocity = previousViewVelocity = Vector3.zero;
            viewRoll = previousAirSpeed = 0f;
            wasViewGrounded = false;
        }

        public void Kick(float amount, float extraFieldOfView)
        {
            shake = Mathf.Max(shake, amount);
            fieldOfViewKick = Mathf.Max(fieldOfViewKick, extraFieldOfView);
        }

        public void Configure(Transform newTarget)
        {
            target = newTarget;
            goat = newTarget ? newTarget.GetComponent<GoatController>() : null;
            wall = newTarget ? newTarget.GetComponent<GoatWallJumpController>() : null;
            Snap();
        }

        public void SetPitch(float degrees)
        {
            pitch = Mathf.Clamp(degrees, IsFirstPerson ? -85f : minPitch, IsFirstPerson ? 87f : maxPitch);
            Snap();
        }

        public void SetTestCliffView()
        {
            distance = 6.4f;
            height = 2.1f;
            pitch = 14f;
            Snap();
        }

        public void Configure(Transform newTarget, float initialYaw)
        {
            Configure(newTarget);
            yaw = initialYaw;
        }

        public void Configure(Transform newTarget, float initialYaw, float initialPitch)
        {
            Configure(newTarget, initialYaw);
            pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        }

        private void Start()
        {
            view = GetComponent<Camera>();
            view.fieldOfView = 66f;
            view.nearClipPlane = .08f;
            IsFirstPerson = PlayerPrefs.GetInt(PerspectiveKey, 0) != 0;
            firstPersonBody = GetComponent<GoatFirstPersonView>();
            if (IsFirstPerson) SetFirstPerson(true);
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ReadOrbitInput()
        {
            if (GoatSettingsMenu.BlocksInput || GoatSkinMenu.IsOpen
                || Cursor.lockState != CursorLockMode.Locked || (wall && wall.IsAiming)) return;
            // Legacy mouse axes already contain this frame's mouse displacement.
            // Multiplying by deltaTime makes identical mouse movements depend on FPS.
            ApplyLookDelta(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (!IsFirstPerson) distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        private void ApplyLookDelta(Vector2 delta)
        {
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity,
                IsFirstPerson ? -85f : minPitch, IsFirstPerson ? 87f : maxPitch);
        }

        private float FindClearDistance(Vector3 pivot, Vector3 direction, float length)
            => FindClearDistance(pivot, direction, length, collisionRadius, .08f, .12f);

        private float FindClearDistance(Vector3 pivot, Vector3 direction, float length, float radius,
            float margin, float minimum = .025f)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int worldMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            int count = Physics.SphereCastNonAlloc(pivot, radius, direction,
                obstructionHits, length, worldMask, QueryTriggerInteraction.Ignore);
            float clear = length;
            for (int i = 0; i < count; i++)
            {
                var hit = obstructionHits[i];
                if (!hit.collider || hit.collider.transform.IsChildOf(target)
                    || hit.collider.GetComponentInParent<GoatPhysicsPart>()
                    || hit.collider.GetComponentInParent<GoatController>()) continue;
                clear = Mathf.Min(clear, Mathf.Max(minimum, hit.distance - margin));
            }
            return clear;
        }

        private void UpdateFirstPersonMotion(float dt, bool snap)
        {
            Vector3 velocity = goat ? goat.Velocity : Vector3.zero;
            bool grounded = goat && goat.Grounded;
            if (snap)
            {
                previousViewVelocity = velocity;
                wasViewGrounded = grounded;
                previousAirSpeed = velocity.y;
                return;
            }
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, .04f);
            Vector3 acceleration = Vector3.ClampMagnitude((velocity - previousViewVelocity) / dt, 35f);
            previousViewVelocity = velocity;
            Vector3 forward = Quaternion.Euler(0f, viewYaw, 0f) * Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 targetInertia = new Vector3(-Vector3.Dot(acceleration, right), 0f,
                -Vector3.Dot(acceleration, forward)) * .0008f;
            if (grounded && !wasViewGrounded && previousAirSpeed < -2f)
                inertiaVelocity.y -= Mathf.Clamp(-previousAirSpeed * .045f, .08f, .42f);
            if (!grounded) previousAirSpeed = Mathf.Min(previousAirSpeed, velocity.y);
            else previousAirSpeed = 0f;
            wasViewGrounded = grounded;
            inertiaVelocity += ((targetInertia - viewInertia) * 100f - inertiaVelocity * 19f) * dt;
            viewInertia = Vector3.ClampMagnitude(viewInertia + inertiaVelocity * dt, .055f);
            float roll = Mathf.Clamp(-Vector3.Dot(acceleration, right) * .045f, -.8f, .8f);
            viewRoll = Mathf.Lerp(viewRoll, roll, 1f - Mathf.Exp(-9f * dt));
        }

        private void LateUpdate()
        {
            if (!target) Configure(FindFirstObjectByType<GoatController>()?.transform);
            if (!target) return;
            if (!view) view = GetComponent<Camera>();
            if (!goat) goat = target.GetComponent<GoatController>();
            if (!wall) wall = target.GetComponent<GoatWallJumpController>();
            if (!firstPersonBody) firstPersonBody = GetComponent<GoatFirstPersonView>();
            if (!GoatSkinMenu.IsOpen && !GoatSettingsMenu.BlocksInput && Input.GetKeyDown(KeyCode.F5)) TogglePerspective();
            ReadOrbitInput();

            float dt = Time.unscaledDeltaTime;
            if (firstPersonBody) firstPersonBody.Configure(goat, IsFirstPerson);
            Vector3 pivot = IsFirstPerson && firstPersonBody ? firstPersonBody.EyePosition
                : target.position + Vector3.up * height;
            bool snap = !hasInitialPosition || (pivot - followPivot).sqrMagnitude > 64f;
            if (snap)
            {
                followPivot = pivot;
                eyeOffset = pivot - target.position;
                viewYaw = yaw;
                viewPitch = pitch;
            }
            else
            {
                if (IsFirstPerson)
                {
                    eyeOffset = Vector3.Lerp(eyeOffset, pivot - target.position,
                        1f - Mathf.Exp(-firstPersonEyeSharpness * Mathf.Min(Time.deltaTime, .04f)));
                    followPivot = target.position + eyeOffset;
                }
                else followPivot = Vector3.Lerp(followPivot, pivot, 1f - Mathf.Exp(-followSharpness * dt));
                float orbitBlend = 1f - Mathf.Exp(-(IsFirstPerson ? firstPersonLookSharpness : orbitSharpness) * dt);
                viewYaw = Mathf.LerpAngle(viewYaw, yaw, orbitBlend);
                viewPitch = Mathf.Lerp(viewPitch, pitch, orbitBlend);
            }

            // Mouse-controlled orbit stays independent of slope, speed and collisions.
            if (IsFirstPerson) UpdateFirstPersonMotion(Time.deltaTime, snap);
            Quaternion orbit = Quaternion.Euler(viewPitch, viewYaw, IsFirstPerson ? viewRoll : 0f);
            Vector3 backward = orbit * Vector3.back;
            float clearDistance = IsFirstPerson ? 0f : FindClearDistance(followPivot, backward, distance);
            // Move in immediately to avoid clipping; ease out after the obstruction leaves.
            resolvedDistance = snap || clearDistance < resolvedDistance ? clearDistance
                : Mathf.Lerp(resolvedDistance, clearDistance, 1f - Mathf.Exp(-zoomSharpness * dt));
            Vector3 position = followPivot + backward * resolvedDistance;
            if (IsFirstPerson && firstPersonBody)
            {
                position += Quaternion.Euler(0f, viewYaw, 0f) * viewInertia;
                Vector3 from = firstPersonBody.CollisionAnchor;
                Vector3 offset = position - from;
                if (offset.sqrMagnitude > .0001f)
                    position = from + offset.normalized * FindClearDistance(from, offset.normalized,
                        offset.magnitude, firstPersonCollisionRadius, .025f);
            }
            Vector3 shakeOffset = Random.insideUnitSphere * shake;
            // The shake itself must not put the near plane into the rock.
            if (!IsFirstPerson && shake > .001f)
            {
                Vector3 offset = position + shakeOffset - followPivot;
                position = followPivot + offset.normalized *
                    FindClearDistance(followPivot, offset.normalized, offset.magnitude);
            }
            transform.SetPositionAndRotation(position, orbit);
            hasInitialPosition = true;
            shake = Mathf.MoveTowards(shake, 0f, dt * .8f);

            float speed = goat ? goat.Velocity.magnitude : 0f;
            float wantedFov = IsFirstPerson ? GoatGraphicsSettings.FirstPersonFov
                : 66f + Mathf.InverseLerp(5f, 20f, speed) * 4f + fieldOfViewKick;
            view.fieldOfView = snap ? wantedFov : Mathf.Lerp(view.fieldOfView, wantedFov, 1f - Mathf.Exp(-8f * dt));
            fieldOfViewKick = Mathf.MoveTowards(fieldOfViewKick, 0f, dt * 18f);
        }
    }
}
