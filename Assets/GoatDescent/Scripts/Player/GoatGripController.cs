using UnityEngine;

namespace GoatDescent
{
    // Reads the actual terrain collider, including sloping faces rather than only vertical walls.
    public static class GoatCliffProbe
    {
        public static bool Find(Transform goat, out RaycastHit best)
        {
            best = default;
            int layer = LayerMask.NameToLayer("Player");
            int mask = layer >= 0 ? ~(1 << layer) : ~0;
            float nearest = float.MaxValue;
            var ground = goat.GetComponent<GoatGroundDetector>();
            if (ground && ground.IsGrounded && ground.SlopeAngle >= 38f)
            { best = ground.GroundHit; nearest = .15f; }
            Vector3 origin = goat.position + Vector3.up * .6f;
            for (int i = 0; i < 12; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward;
                if (!Physics.SphereCast(origin, .12f, direction, out var hit, 1.15f, mask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider.transform.IsChildOf(goat) || hit.normal.y > .79f || hit.normal.y < -.2f || hit.distance >= nearest) continue;
                nearest = hit.distance; best = hit;
            }
            return best.collider;
        }
    }

    [DefaultExecutionOrder(-30)]
    public sealed class GoatGripController : MonoBehaviour
    {
        private const float Capacity = 2.2f;
        private Rigidbody body;
        private GoatGroundDetector ground;
        private GoatWallJumpController wall;
        private bool held;
        private float remaining = Capacity, releasedUntil;
        private float superCharge = 1.15f, superReleasedUntil;
        private TrailRenderer leftTrace, rightTrace;
        private Material traceMaterial;
        public bool IsGripping { get; private set; }
        public bool SuperActive { get; private set; }
        public float SuperCharge01 => superCharge / 1.15f;
        public bool Exhausted { get; private set; }
        public float Strength => remaining / Capacity;
        public Vector3 SurfaceNormal { get; private set; }
        public void SetGripHeld(bool value) { held = value; if (!value) IsGripping = false; }
        private void Awake() { body = GetComponent<Rigidbody>(); ground = GetComponent<GoatGroundDetector>(); }
        private void Start()
        {
            traceMaterial = new Material(Shader.Find("Sprites/Default"));
            leftTrace = Trace(-.27f); rightTrace = Trace(.27f);
        }
        private TrailRenderer Trace(float x)
        {
            var go = new GameObject("Hoof scrape"); go.transform.SetParent(transform,false); go.transform.localPosition = new Vector3(x,.12f,0f);
            var line = go.AddComponent<TrailRenderer>(); line.material = traceMaterial; line.time = .3f;
            line.minVertexDistance = .035f; line.startWidth = .065f; line.endWidth = 0f;
            line.startColor = new Color(.93f,.78f,.44f,.85f); line.endColor = new Color(.93f,.78f,.44f,0f);
            line.emitting = false; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
        private void Update()
        {
            if (leftTrace) leftTrace.emitting = IsGripping || SuperActive;
            if (rightTrace) rightTrace.emitting = IsGripping || SuperActive;
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) SetGripHeld(true);
            if ((Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
                && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) SetGripHeld(false);
        }
        public void ReleaseForJump()
        {
            IsGripping = false; SuperActive = false;
            releasedUntil = Time.time + .32f;
            superReleasedUntil = Time.time + .32f;
        }
        public void ResetGrip()
        {
            held = false; IsGripping = false; Exhausted = false; remaining = Capacity; releasedUntil = -100f;
            SuperActive = false; superCharge = 1.15f; superReleasedUntil = -100f;
            if(leftTrace){leftTrace.emitting=false;leftTrace.Clear();}if(rightTrace){rightTrace.emitting=false;rightTrace.Clear();}
        }
        private void FixedUpdate()
        {
            wall ??= GetComponent<GoatWallJumpController>();
            if (!body || body.isKinematic || (wall && wall.IsAiming))
            { IsGripping = false; SuperActive = false; return; }
            if (ground && ground.IsGrounded && ground.SlopeAngle < 30f && !SuperActive)
                superCharge = Mathf.Min(1.15f, superCharge + Time.fixedDeltaTime * .48f);
            bool superHeld = Input.GetKey(KeyCode.F) && Time.time >= superReleasedUntil && superCharge > .03f;
            if (superHeld && GoatCliffProbe.Find(transform, out var superHit))
            {
                if (!SuperActive)
                {
                    body.linearVelocity *= .38f;
                    body.AddForce(Vector3.up * 1.35f, ForceMode.VelocityChange);
                    GetComponent<GoatSlopeBalance>()?.CatchWithSuperHooves();
                    GetComponent<GoatSpectacle>()?.SuperHooves();
                }
                SuperActive = true;
                IsGripping = true;
                SurfaceNormal = superHit.normal;
                superCharge = Mathf.Max(0f, superCharge - Time.fixedDeltaTime);
                Vector3 superDownhill = Vector3.ProjectOnPlane(Vector3.down, superHit.normal).normalized;
                Vector3 superAcross = Vector3.Cross(superHit.normal, Vector3.up).normalized;
                var view = Camera.main;
                if (view && Vector3.Dot(superAcross, view.transform.right) < 0f) superAcross = -superAcross;
                float superSteer = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                Vector3 superVelocity = superDownhill * .18f + superAcross * superSteer * 1.3f - superHit.normal * .2f;
                body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, superVelocity, 75f * Time.fixedDeltaTime);
                body.AddForce(-Physics.gravity, ForceMode.Acceleration);
                return;
            }
            if (SuperActive) superReleasedUntil = Time.time + .3f;
            SuperActive = false;
            if (ground && ground.IsGrounded && ground.SlopeAngle < 35f && !held)
            {
                remaining = Mathf.Min(Capacity, remaining + Time.fixedDeltaTime * 1.4f);
                if (remaining > Capacity * .35f) Exhausted = false;
            }
            IsGripping = held && !Exhausted && Time.time >= releasedUntil && GoatCliffProbe.Find(transform, out _);
            if (!IsGripping || !GoatCliffProbe.Find(transform, out var hit)) return;
            SurfaceNormal = hit.normal;
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            remaining = Mathf.Max(0f, remaining - Time.fixedDeltaTime * Mathf.Lerp(.35f, 1f, Mathf.InverseLerp(38f, 78f, angle)));
            if (remaining <= 0f)
            {
                Exhausted = true; IsGripping = false;
                SlopeRun.Instance?.Notify("Копыта срываются! Прыгай или ищи полку.");
                return;
            }
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            Vector3 across = Vector3.Cross(hit.normal, Vector3.up).normalized;
            var camera = Camera.main;
            if (camera && Vector3.Dot(across, camera.transform.right) < 0f) across = -across;
            float steer = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            Vector3 target = downhill * .65f + across * steer * 2.5f - hit.normal * .3f;
            body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, target, 65f * Time.fixedDeltaTime);
            body.AddForce(-Physics.gravity, ForceMode.Acceleration);
        }
        private void OnDisable() { held = false; IsGripping = false; SuperActive = false; }
        private void OnDestroy() { if(traceMaterial)Destroy(traceMaterial); }
    }
}
