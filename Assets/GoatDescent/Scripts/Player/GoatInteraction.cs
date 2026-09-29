using UnityEngine;

namespace GoatDescent
{
    /// <summary>Tap/hold arbitration, timed shove and a two-body horn grip.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(GoatController))]
    public sealed class GoatInteraction : MonoBehaviour
    {
        [SerializeField] private float holdThreshold = .25f;
        [SerializeField] private float reach = 2.55f;
        [SerializeField] private float halfAngle = 65f;
        [SerializeField] private float shoveImpulse = 205f;
        [SerializeField] private float shoveCooldown = .75f;
        [SerializeField] private float shoveContactTime = .25f;
        [SerializeField] private float gripSpring = 2800f;
        [SerializeField] private float gripDamping = 230f;
        [SerializeField] private float gripBreakForce = 5400f;
        [SerializeField] private float hornReach = .99f;
        private Rigidbody body;
        private GoatController motor;
        private GoatVisualController visual;
        private RespawnController life;
        private ConfigurableJoint joint;
        private Rigidbody ownHornBody, otherHornBody;
        private Vector3 ownHornAnchor, otherHornAnchor;
        private GoatInteraction held, heldBy, pendingTarget;
        private RollingStone pendingStone;
        private float pressedAt, nextShove, contactAt, grabAt, linkedAt, obstructionSince = -1f;
        private bool pressing, holdConsumed, shovePending, grabPending;
        private Vector3 actionDirection;
        private float initialLimit;
        private float liftAnchor;
        private bool networkLinked, networkHolding;
        public GoatInteraction Partner => held ? held : heldBy;
        public bool IsLinked => held || heldBy || (!MountainAuthority.IsHost && networkLinked);
        public bool IsHolding => held || (!MountainAuthority.IsHost && networkHolding);
        public bool IsBusy => IsLinked || shovePending || grabPending;
        public bool IsPulling => held && motor.MoveDirection.sqrMagnitude > .05f;
        public string Status { get; private set; } = "Подойди к напарнику";
        public void ApplyNetworkState(bool linked, bool holding, string status)
        {
            if (MountainAuthority.IsHost) return;
            networkLinked = linked;
            networkHolding = holding;
            Status = status ?? "";
        }
        public int ShovesApplied { get; private set; }
        public int GrabsStarted { get; private set; }
        public float GripForce => joint ? joint.currentForce.magnitude : heldBy && heldBy.joint ? heldBy.joint.currentForce.magnitude : 0f;
        public Vector3 ForceOnBody => joint ? joint.currentForce : heldBy && heldBy.joint ? -heldBy.joint.currentForce : Vector3.zero;
        public Vector3 GripPoint => joint ? transform.TransformPoint(joint.anchor)
            : heldBy && heldBy.joint ? transform.TransformPoint(heldBy.joint.connectedAnchor) : transform.position;
        public Vector3 Facing => visual ? visual.Facing : transform.forward;
        public bool Ready => isActiveAndEnabled && body && !body.isKinematic && (!life || !life.IsDead)
            && !(GetComponent<GoatHornVault>()?.IsVaulting ?? false);

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); motor = GetComponent<GoatController>();
            visual = GetComponent<GoatVisualController>(); life = GetComponent<RespawnController>();
        }
        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            if (!Ready) { CancelAll(); return; }
            if (held && !joint) ReleaseGrip();
            if (pressing && !holdConsumed && Time.time - pressedAt >= holdThreshold)
            { holdConsumed = true; TryGrab(); }
            if (!GoatLocalControl.AllowsInput(this)) return;
            if (Input.GetKeyDown(KeyCode.F)) Press();
            if (Input.GetKeyUp(KeyCode.F)) ReleaseButton();
            if (!IsBusy && !pressing)
            {
                var candidate = FindTarget();
                Status = candidate ? "F — толкнуть · удерживать F — сцепить рога" : "Подойди и повернись к напарнику";
            }
        }
        public void Press() { if (!Ready || pressing) return; pressing = true; holdConsumed = false; pressedAt = Time.time; }
        public void ReleaseButton()
        {
            if (!pressing) return;
            pressing = false;
            if (holdConsumed) { grabPending = false; pendingTarget = null; ReleaseGrip(); }
            else TryShove();
        }
        public GoatInteraction FindTarget()
        {
            GoatInteraction best = null;
            float bestDistance = reach;
            foreach (var other in FindObjectsByType<GoatInteraction>(FindObjectsSortMode.None))
            {
                if (other == this || !CanReach(other)) continue;
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance < bestDistance) { best = other; bestDistance = distance; }
            }
            return best;
        }
        private bool CanReach(GoatInteraction other)
        {
            if (!other || !other.Ready || other.IsLinked) return false;
            Vector3 delta = other.transform.position - transform.position;
            if (delta.magnitude > reach || Mathf.Abs(delta.y) > 1.8f) return false;
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (flat.sqrMagnitude > .01f && Vector3.Angle(Vector3.ProjectOnPlane(Facing, Vector3.up), flat) > halfAngle) return false;
            return HasClearPath(other);
        }
        private bool HasClearPath(GoatInteraction other)
        {
            Vector3 start = body.position + Vector3.up * 1.25f;
            Vector3 delta = other.body.position + Vector3.up * 1.25f - start;
            foreach (var hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.rigidbody != body && hit.rigidbody != other.body
                    && !hit.collider.GetComponentInParent<GoatPhysicsPart>()) return false;
            return true;
        }
        public bool TryShove()
        {
            if (!Ready || IsBusy || Time.time < nextShove) return false;
            var other = FindTarget();
            var stone = other ? null : FindStoneTarget();
            if (!other && !stone) { Status = "Не дотянуться"; return false; }
            pendingTarget = other; shovePending = true;
            pendingStone = stone;
            actionDirection = Vector3.ProjectOnPlane((other ? other.body.position : stone.transform.position) - body.position, Vector3.up).normalized;
            contactAt = Time.time + shoveContactTime; nextShove = Time.time + shoveCooldown;
            visual?.PlayInteraction("Goat_Push", .68f, actionDirection);
            Status = "ТОЛЧОК";
            return true;
        }

        private RollingStone FindStoneTarget()
        {
            RollingStone best = null;
            float bestDistance = reach;
            foreach (var stone in FindObjectsByType<RollingStone>(FindObjectsSortMode.None))
            {
                Vector3 delta = stone.transform.position - transform.position;
                float distance = delta.magnitude;
                if (distance > bestDistance || Mathf.Abs(delta.y) > 1.8f) continue;
                if (Vector3.Angle(Vector3.ProjectOnPlane(Facing, Vector3.up),
                    Vector3.ProjectOnPlane(delta, Vector3.up)) > halfAngle) continue;
                Vector3 from = body.position + Vector3.up * 1.1f;
                Vector3 to = stone.transform.position - from;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(from, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.rigidbody != body && hit.collider.gameObject != stone.gameObject
                        && !hit.collider.GetComponentInParent<GoatPhysicsPart>()) { blocked = true; break; }
                if (blocked) continue;
                best = stone; bestDistance = distance;
            }
            return best;
        }
        public bool TryGrab()
        {
            if (!Ready || IsBusy) return false;
            var other = FindTarget();
            if (!other) { Status = "Не дотянуться до рогов"; return false; }
            pendingTarget = other; grabPending = true; grabAt = Time.time + .22f;
            actionDirection = Vector3.ProjectOnPlane(other.body.position - body.position, Vector3.up).normalized;
            visual?.PlayInteraction("Goat_GrabStart", .42f, actionDirection);
            other.visual?.FaceInteraction(-actionDirection, .45f);
            Status = "ТЯНЕТСЯ К НАПАРНИКУ";
            return true;
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost || !Ready) return;
            if (shovePending && Time.time >= contactAt)
            {
                shovePending = false;
                var other = pendingTarget; pendingTarget = null;
                var stone = pendingStone; pendingStone = null;
                if (CanReach(other))
                {
                    var impulse = (actionDirection + Vector3.up * .13f).normalized * shoveImpulse;
                    other.motor.ReceiveImpulse(impulse, .55f);
                    motor.ReceiveImpulse(-impulse * .17f, .25f);
                    other.GetComponent<GoatCliffGrip>()?.BreakGrip(.5f);
                    other.visual?.PlayInteraction("Goat_PushReact", .68f, other.Facing);
                    ShovesApplied++;
                }
                else if (stone && Vector3.Distance(body.position, stone.transform.position) < reach + .2f)
                {
                    var impulse = (actionDirection + Vector3.up * .15f).normalized * shoveImpulse;
                    if (stone.ReceiveImpulse(impulse, name + " толкнул рогами")) ShovesApplied++;
                }
            }
            if (grabPending && Time.time >= grabAt)
            {
                grabPending = false;
                var other = pendingTarget; pendingTarget = null;
                if (CanReach(other)) Attach(other);
            }
            if (!held) return;
            if (!held.Ready || !joint || Vector3.Distance(body.position, held.body.position) > 3.8f)
            { ReleaseGrip(); return; }
            if (!HasClearPath(held))
            {
                if (obstructionSince < 0f) obstructionSince = Time.time;
                if (Time.time - obstructionSince > .2f) { ReleaseGrip(); return; }
            }
            else obstructionSince = -1f;
            Vector3 direction = Vector3.ProjectOnPlane(held.body.position - body.position, Vector3.up).normalized;
            // The rescuer lifts its head while stepping back. The joint carries
            // this motion as force to both bodies, including the rescuer's recoil.
            bool hauling = Vector3.Dot(motor.MoveDirection, -direction) > .25f;
            liftAnchor = Mathf.MoveTowards(liftAnchor, hauling ? .6f : 0f, Time.fixedDeltaTime * .65f);
            joint.anchor = transform.InverseTransformVector(Vector3.up * (1.25f + liftAnchor) + direction * hornReach);
            joint.connectedAnchor = held.transform.InverseTransformVector(Vector3.up * 1.25f - direction * hornReach);
            var limit = joint.linearLimit;
            limit.limit = Mathf.Lerp(initialLimit, .18f, Mathf.Clamp01((Time.time - linkedAt) / .65f));
            joint.linearLimit = limit;
            PullHornsTogether();
            visual?.FaceInteraction(direction, .15f);
            held.visual?.FaceInteraction(-direction, .15f);
            Status = IsPulling ? "ТЯНЕТ НАПАРНИКА" : "РОГА СЦЕПЛЕНЫ — отпусти F, чтобы разжать";
            if (!IsPulling && (GetComponent<GoatGripBalance>()?.Unbalance01 ?? 0f) > .1f)
                Status = "ТЯНЕТ К КРАЮ — отходи назад или отпусти F";
        }
        private void Attach(GoatInteraction other)
        {
            if (other.IsBusy) return;
            held = other; other.heldBy = this; linkedAt = Time.time; liftAnchor = 0f;
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = other.body;
            joint.anchor = transform.InverseTransformVector(Vector3.up * 1.25f + actionDirection * hornReach);
            joint.connectedAnchor = other.transform.InverseTransformVector(Vector3.up * 1.25f - actionDirection * hornReach);
            initialLimit = Mathf.Max(.18f, Vector3.Distance(transform.TransformPoint(joint.anchor), other.transform.TransformPoint(joint.connectedAnchor)));
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.linearLimit = new SoftJointLimit { limit = initialLimit, bounciness = 0f, contactDistance = .025f };
            joint.linearLimitSpring = new SoftJointLimitSpring { spring = gripSpring, damper = gripDamping };
            joint.breakForce = gripBreakForce; joint.breakTorque = Mathf.Infinity;
            joint.enableCollision = true; joint.enablePreprocessing = false;
            joint.projectionMode = JointProjectionMode.None;
            body.solverIterations = other.body.solverIterations = 12;
            body.solverVelocityIterations = other.body.solverVelocityIterations = 8;
            body.WakeUp(); other.body.WakeUp();
            var ownSkeleton = GetComponent<GoatPhysicalBody>();
            var otherSkeleton = other.GetComponent<GoatPhysicalBody>();
            if (ownSkeleton && otherSkeleton
                && ownSkeleton.TryGetHornConnection(actionDirection, out Rigidbody ownHead,
                    out Vector3 ownHornAnchor)
                && otherSkeleton.TryGetHornConnection(-actionDirection, out Rigidbody otherHead,
                    out Vector3 otherHornAnchor))
            {
                ownHornBody = ownHead;
                otherHornBody = otherHead;
                this.ownHornAnchor = ownHornAnchor;
                this.otherHornAnchor = otherHornAnchor;
                ownHead.WakeUp(); otherHead.WakeUp();
            }
            other.visual?.PlayInteraction("Goat_GrabbedStart", .38f, -actionDirection);
            GrabsStarted++;
        }
        public void ReleaseGrip()
        {
            if (heldBy) { var owner = heldBy; heldBy = null; owner.ReleaseGrip(); return; }
            ownHornBody = otherHornBody = null;
            if (joint) { joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free; joint.connectedBody = null; Destroy(joint); joint = null; }
            if (held)
            {
                held.heldBy = null;
                held.visual?.PlayInteraction("Goat_Release", .35f, held.Facing);
                held = null;
                visual?.PlayInteraction("Goat_Release", .35f, Facing);
            }
            obstructionSince = -1f;
        }

        private void PullHornsTogether()
        {
            if (!ownHornBody || !otherHornBody) return;
            Vector3 ownPoint = ownHornBody.transform.TransformPoint(ownHornAnchor);
            Vector3 otherPoint = otherHornBody.transform.TransformPoint(otherHornAnchor);
            Vector3 separation = otherPoint - ownPoint;
            if (separation.magnitude < .045f) return;
            Vector3 relativeVelocity = otherHornBody.GetPointVelocity(otherPoint)
                - ownHornBody.GetPointVelocity(ownPoint);
            float engage = Mathf.Clamp01((Time.time - linkedAt) / .55f);
            float maximumForce = held.GetComponent<GoatPhysicalBody>()?.IsEagleStrained == true
                ? 220f : 150f;
            Vector3 force = Vector3.ClampMagnitude(separation * 260f + relativeVelocity * 18f,
                maximumForce)
                * engage;
            ownHornBody.AddForceAtPosition(force, ownPoint, ForceMode.Force);
            otherHornBody.AddForceAtPosition(-force, otherPoint, ForceMode.Force);
        }
        private void OnJointBreak(float force)
        { joint = null; ReleaseGrip(); Status = "Рога соскользнули"; }
        public void CancelAll()
        {
            pressing = holdConsumed = shovePending = grabPending = false; pendingTarget = null; pendingStone = null;
            ReleaseGrip();
        }
        private void OnDisable() => CancelAll();
        private void OnApplicationFocus(bool focused)
        { if (!focused) CancelAll(); }
    }
}
