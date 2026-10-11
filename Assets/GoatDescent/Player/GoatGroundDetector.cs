using UnityEngine;

namespace GoatDescent
{
    [DefaultExecutionOrder(-100)]
    public sealed class GoatGroundDetector : MonoBehaviour
    {
        [SerializeField] private float probeRadius = .3f;
        [SerializeField] private float probeDistance = .22f;
        [SerializeField] private LayerMask groundMask = ~0;
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float SlopeAngle { get; private set; }
        public RaycastHit GroundHit { get; private set; }
        private readonly RaycastHit[] probeHits = new RaycastHit[24];

        private void FixedUpdate()
        {
            Vector3 origin = transform.position + Vector3.up * .55f;
            int count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, probeHits,
                probeDistance + .12f, groundMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = probeHits[i];
                if (!hit.collider || hit.collider.attachedRigidbody == GetComponent<Rigidbody>()) continue;
                var part = hit.collider.GetComponentInParent<GoatPhysicsPart>();
                if (part && part.Owner && part.Owner.gameObject == gameObject) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                GroundHit = hit;
                IsGrounded = true;
            }
            if (IsGrounded) { GroundNormal = GroundHit.normal; SlopeAngle = Vector3.Angle(GroundNormal, Vector3.up); }
            else { GroundHit = default; GroundNormal = Vector3.up; SlopeAngle = 90f; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .55f + Vector3.down * probeDistance, probeRadius);
        }
    }
}
