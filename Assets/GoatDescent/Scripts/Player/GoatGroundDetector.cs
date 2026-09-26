using UnityEngine;

namespace GoatDescent
{
    [DefaultExecutionOrder(-100)]
    public sealed class GoatGroundDetector : MonoBehaviour
    {
        [SerializeField] private float probeRadius = .42f;
        [SerializeField] private float probeDistance = .22f;
        [SerializeField] private LayerMask groundMask = ~0;
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float SlopeAngle { get; private set; }
        public RaycastHit GroundHit { get; private set; }

        private void FixedUpdate()
        {
            Vector3 origin = transform.position + Vector3.up * .55f;
            IsGrounded = Physics.SphereCast(origin, probeRadius, Vector3.down, out var hit, probeDistance + .12f, groundMask, QueryTriggerInteraction.Ignore);
            if (IsGrounded) { GroundHit = hit; GroundNormal = hit.normal; SlopeAngle = Vector3.Angle(hit.normal, Vector3.up); }
            else { GroundNormal = Vector3.up; SlopeAngle = 90f; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .55f + Vector3.down * probeDistance, probeRadius);
        }
    }
}
