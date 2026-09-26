using UnityEngine;

namespace GoatDescent
{
    // Adapted from the four-hoof support and projected centre-of-mass idea in balance.
    // This version reads the real mountain collider and applies forces to the playable Rigidbody.
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Rigidbody), typeof(GoatGroundDetector))]
    public sealed class GoatSlopeBalance : MonoBehaviour
    {
        private readonly Vector3[] hoofPoints = new Vector3[4];
        private readonly bool[] hoofHolding = new bool[4];
        private Rigidbody body;
        private GoatGroundDetector ground;
        private float fallTime;
        private float catchCooldown;
        private float slipUntil;
        private float lean;

        public float Balance01 { get; private set; } = 1f;
        public int HoovesHolding { get; private set; }
        public float LeanDegrees => lean;
        public bool IsSlipping => Time.time < slipUntil;
        public bool HoofHolds(int index) => index >= 0 && index < hoofHolding.Length && hoofHolding[index];
        public void CatchWithSuperHooves()
        {
            slipUntil = -100f;
            fallTime = 0f;
            catchCooldown = Time.time + .8f;
            Balance01 = Mathf.Max(Balance01, .65f);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ground = GetComponent<GoatGroundDetector>();
        }

        public void ResetBalance()
        {
            Balance01 = 1f;
            HoovesHolding = 0;
            fallTime = 0f;
            catchCooldown = 0f;
            slipUntil = -100f;
            lean = 0f;
            for (int i = 0; i < hoofHolding.Length; i++) hoofHolding[i] = false;
        }

        private void FixedUpdate()
        {
            if (GetComponent<GoatGripController>()?.SuperActive == true)
            {
                Balance01 = Mathf.MoveTowards(Balance01, .8f, Time.fixedDeltaTime * 3f);
                fallTime = 0f;
                return;
            }
            if (IsSlipping)
            {
                HoovesHolding = 0;
                Balance01 = .16f;
                return;
            }
            if (!body || !ground || body.isKinematic || !ground.IsGrounded ||
                GetComponent<GoatWallJumpController>()?.IsAiming == true)
            {
                Balance01 = Mathf.MoveTowards(Balance01, 1f, Time.fixedDeltaTime * 1.6f);
                HoovesHolding = 0;
                fallTime = 0f;
                lean = Mathf.MoveTowards(lean, 0f, Time.fixedDeltaTime * 65f);
                return;
            }
            Vector3 normal = ground.GroundNormal;
            Vector3 forward = Vector3.ProjectOnPlane(body.linearVelocity, normal);
            if (forward.sqrMagnitude < 1f) forward = Vector3.ProjectOnPlane(Vector3.forward, normal);
            forward.Normalize();
            Vector3 right = Vector3.Cross(normal, forward).normalized;
            Vector3 center = ground.GroundHit.point;
            Vector3 supportCenter = Vector3.zero;
            HoovesHolding = 0;
            float left = float.PositiveInfinity, rightEdge = float.NegativeInfinity;
            int mask = ~(1 << gameObject.layer);
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -.31f : .31f;
                float end = i < 2 ? .43f : -.43f;
                Vector3 foot = center + right * side + forward * end;
                bool holding = Physics.Raycast(foot + normal * .85f, -normal, out var hit,
                    1.45f, mask, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponent<MountainSlopeSurface>() && hit.normal.y > .52f;
                hoofHolding[i] = holding;
                if (!holding) continue;
                hoofPoints[i] = hit.point;
                supportCenter += hit.point;
                HoovesHolding++;
                float span = Vector3.Dot(hit.point - center, right);
                left = Mathf.Min(left, span);
                rightEdge = Mathf.Max(rightEdge, span);
            }

            if (HoovesHolding == 0)
            {
                Balance01 = Mathf.MoveTowards(Balance01, .05f, Time.fixedDeltaTime * 3f);
                fallTime += Time.fixedDeltaTime;
                if (fallTime > .3f && Time.time >= catchCooldown)
                    Slip(right, Vector3.ProjectOnPlane(Vector3.down, normal), body.linearVelocity.x);
                return;
            }
            supportCenter /= HoovesHolding;
            float steering = (Input.GetKey(KeyCode.D) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) ? 1f : 0f);
            // Momentum and the rider's steer move the projected weight across the hoof span.
            float offset = Vector3.Dot(body.position - supportCenter, right)
                + Vector3.Dot(body.linearVelocity, right) * .022f - steering * .10f;
            float midpoint = (left + rightEdge) * .5f;
            float halfSpan = Mathf.Max(.19f, (rightEdge - left) * .5f);
            float outside = Mathf.Max(0f, Mathf.Abs(offset - midpoint) / halfSpan - .75f);
            float slopeCost = Mathf.InverseLerp(40f, 78f, ground.SlopeAngle) * .11f;
            float speedCost = Mathf.InverseLerp(5f, 14f, body.linearVelocity.magnitude) * .1f;
            float missingCost = (4 - HoovesHolding) * .22f;
            bool braking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float recklessTurn = Mathf.Abs(steering) * Mathf.InverseLerp(4f, 11f, body.linearVelocity.magnitude)
                * (braking ? .2f : 1f) * .23f;
            float target = Mathf.Clamp01(1f - outside * .7f - slopeCost - speedCost
                - missingCost - recklessTurn);
            Balance01 = Mathf.MoveTowards(Balance01, target, Time.fixedDeltaTime * 2.8f);
            lean = Mathf.MoveTowards(lean, Mathf.Clamp(-(offset - midpoint) * 38f, -22f, 22f),
                Time.fixedDeltaTime * 70f);

            if (Balance01 < .28f && Time.time >= catchCooldown)
            {
                float direction = Mathf.Sign(offset - midpoint);
                body.AddForce(right * direction * (.28f - Balance01) * 10f, ForceMode.Acceleration);
            }
            fallTime = Balance01 < .28f ? fallTime + Time.fixedDeltaTime : 0f;
            if (fallTime > .35f && Time.time >= catchCooldown)
            {
                Slip(right, Vector3.ProjectOnPlane(Vector3.down, normal), offset - midpoint);
            }
        }

        private void Slip(Vector3 right, Vector3 downhill, float side)
        {
            float direction = Mathf.Abs(side) > .02f ? Mathf.Sign(side) : 1f;
            body.AddForce(Vector3.down * 1.2f + right * direction * 2.8f
                + downhill.normalized * 1.6f, ForceMode.VelocityChange);
            GetComponent<GoatGripController>()?.ReleaseForJump();
            SlopeRun.Instance?.Notify("КОПЫТА СОРВАЛИСЬ! ЛОВИ СЛЕДУЮЩУЮ ПОЛКУ");
            fallTime = 0f;
            slipUntil = Time.time + .48f;
            catchCooldown = Time.time + 1.6f;
            Balance01 = .16f;
        }
    }
}
