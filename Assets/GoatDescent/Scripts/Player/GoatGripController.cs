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
        private Rigidbody body;
        private GoatGroundDetector ground;
        private float superCharge = 1.15f, superReleasedUntil;
        private TrailRenderer leftTrace, rightTrace;
        private Material traceMaterial;
        public bool IsGripping { get; private set; }
        public bool SuperActive { get; private set; }
        public float SuperCharge01 => superCharge / 1.15f;
        public Vector3 SurfaceNormal { get; private set; }
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
        }
        public void ReleaseForJump()
        {
            IsGripping = false; SuperActive = false;
            superReleasedUntil = Time.time + .32f;
        }
        public void ResetGrip()
        {
            IsGripping = false;
            SuperActive = false; superCharge = 1.15f; superReleasedUntil = -100f;
            if(leftTrace){leftTrace.emitting=false;leftTrace.Clear();}if(rightTrace){rightTrace.emitting=false;rightTrace.Clear();}
        }
        private void FixedUpdate()
        {
            if (!body || body.isKinematic)
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
            IsGripping = false;
        }
        private void OnDisable() { IsGripping = false; SuperActive = false; }
        private void OnDestroy() { if(traceMaterial)Destroy(traceMaterial); }
    }
}
