using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>
    /// A small active ragdoll for the skinned goat. Every visible body section is
    /// driven by a simulated mass and a joint, never by an edge/fall pose clip.
    /// The gameplay Rigidbody remains the locomotion and interaction body.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GoatPhysicalBody : MonoBehaviour
    {
        private sealed class Part
        {
            public Transform Bone;
            public Rigidbody Body;
            public Rigidbody ParentBody;
            public ConfigurableJoint Joint;
            public Vector3 RestPosition;
            public Quaternion RestRotation;
            public Quaternion RestRelativeRotation;
            public Quaternion RestBoneLocalRotation;
            public Quaternion WalkRelativeRotation;
            public int LastWalkSampleFrame = -100;
            public Quaternion ActionRelativeRotation;
            public string ActionClip;
            public bool IsLeg;
            public bool IsElastic;
            public float Spring;
            public readonly List<Rigidbody> Descendants = new List<Rigidbody>(14);
        }

        private readonly List<Part> parts = new List<Part>(16);
        private readonly List<Collider> colliders = new List<Collider>(16);
        private readonly RaycastHit[] footingHits = new RaycastHit[16];
        private Rigidbody rootBody;
        private GoatGroundDetector ground;
        private GoatController motor;
        private GoatInteraction interaction;
        private GoatVisualController visual;
        private ConfigurableJoint pelvisJoint;
        private Vector3 pelvisBaseAnchor;
        private Transform simulationRoot;
        private Vector3 lastRootPosition;
        private Vector3 stableFacing;
        private float lastSupportedAt;
        private float walkBlend;
        private float groundLean;
        private float groundRoll;
        private float groundLift;
        private float eagleLift;
        private float eagleTugUntil;
        private float jumpStartedAt = -100f;
        private float jumpBrace;
        private float actionBlend;
        private string lastActionClip;
        private string sampledActionClip;
        private int lastActionSampleFrame = -100;
        private bool initialized;
        private bool ignoredPartnerColliders;

        public bool RootIsTumbling => rootBody && rootBody.constraints == RigidbodyConstraints.None;
        public int SimulatedParts => parts.Count;
        public bool IsEagleStrained => Time.time < eagleTugUntil;

        public void BeginJump(Vector3 launchVelocity)
        {
            if (!initialized) TryInitialize();
            if (!initialized) return;
            jumpStartedAt = Time.time;
            foreach (Part part in parts)
            {
                // The gameplay body and the articulated rig must leave the
                // ground together. Otherwise the joint drags the pelvis up
                // while the heavy head stays behind and dives downward.
                float frontLead = part.Bone.name switch
                {
                    "Head" or "Neck" => .45f,
                    "ElasticChest" or "Spine" => .3f,
                    "FrontUpper.L" or "FrontUpper.R" or "FrontLower.L" or "FrontLower.R" => .2f,
                    _ => 0f
                };
                part.Body.AddForce(launchVelocity + Vector3.up * frontLead,
                    ForceMode.VelocityChange);
                part.Body.WakeUp();
            }
        }

        public bool TryGetHornConnection(Vector3 towardPartner, out Rigidbody headBody,
            out Vector3 localAnchor)
        {
            Part head = parts.Find(part => part.Bone && part.Bone.name == "Head");
            headBody = head?.Body;
            localAnchor = Vector3.zero;
            if (!initialized || !headBody) return false;
            Vector3 forward = Vector3.ProjectOnPlane(towardPartner, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = transform.forward;
            // The contact sits between the horn bases, above and slightly in
            // front of the skull. Its local anchor follows the simulated head.
            Vector3 contact = headBody.position + Vector3.up * .27f + forward * .15f;
            localAnchor = headBody.transform.InverseTransformPoint(contact);
            return true;
        }

        public void ApplyEagleTug(Vector3 birdPosition, Vector3 outward)
        {
            if (!initialized || !interaction || !interaction.IsLinked) return;
            eagleTugUntil = Time.time + .12f;
            Part chest = parts.Find(part => part.Bone && part.Bone.name == "ElasticChest");
            if (chest == null || !chest.Body) return;
            // The talons lift the upper torso. The head stays at the horn
            // connection while the spine and neck bend under the load.
            Vector3 target = birdPosition - Vector3.up * .18f;
            Vector3 force = (target - chest.Body.worldCenterOfMass) * 185f
                - chest.Body.linearVelocity * 18f + Vector3.up * 75f;
            force += Vector3.ProjectOnPlane(outward, Vector3.up).normalized * 15f;
            chest.Body.AddForce(Vector3.ClampMagnitude(force, 210f), ForceMode.Force);
        }

        private void Start() => TryInitialize();

        private void TryInitialize()
        {
            if (initialized || simulationRoot) return;
            rootBody = GetComponent<Rigidbody>();
            ground = GetComponent<GoatGroundDetector>();
            motor = GetComponent<GoatController>();
            interaction = GetComponent<GoatInteraction>();
            visual = GetComponent<GoatVisualController>();
            Transform model = transform.Find("VisualRoot — replaceable goat model");
            if (!model) return;
            var bones = model.GetComponentsInChildren<Transform>(true);
            var byName = new Dictionary<string, Transform>();
            foreach (Transform bone in bones)
                if (!byName.ContainsKey(bone.name)) byName.Add(bone.name, bone);
            if (!byName.ContainsKey("ElasticMid") || !byName.ContainsKey("ElasticChest"))
            {
                Debug.LogError($"PHYSICAL_GOAT_MISSING_BONES goat={name}");
                return;
            }

            simulationRoot = new GameObject($"{name} physical skeleton").transform;
            // Anatomical order matters: the connection is to the previous mass.
            // ElasticMid/ElasticChest carry smooth skin weights between the larger bones.
            // The gameplay body already owns most of the goat's 72 kg. These
            // masses add only about 10 kg so the visual rig cannot double the
            // load on the existing horn rescue joint.
            Add(byName, "Pelvis", null, "ElasticMid", 2f, .22f, 440f, 18f);
            Add(byName, "ElasticMid", "Pelvis", "Spine", 1.2f, .21f, 330f, 22f);
            Add(byName, "Spine", "ElasticMid", "ElasticChest", 1.6f, .23f, 320f, 22f);
            Add(byName, "ElasticChest", "Spine", "Neck", 1.2f, .18f, 260f, 22f);
            Add(byName, "Neck", "ElasticChest", "Head", .5f, .13f, 400f, 35f);
            Add(byName, "Head", "Neck", null, .8f, .19f, 300f, 45f);
            foreach (string side in new[] { ".L", ".R" })
            {
                Add(byName, "HindUpper" + side, "Pelvis", "HindLower" + side, .4f, .085f, 155f, 60f);
                Add(byName, "HindLower" + side, "HindUpper" + side, null, .25f, .072f, 105f, 70f);
                Add(byName, "FrontUpper" + side, "Spine", "FrontLower" + side, .4f, .085f, 155f, 60f);
                Add(byName, "FrontLower" + side, "FrontUpper" + side, null, .25f, .072f, 105f, 70f);
            }
            foreach (Part ancestor in parts)
            foreach (Part candidate in parts)
            {
                Part cursor = candidate;
                while (cursor != null)
                {
                    if (cursor == ancestor) { ancestor.Descendants.Add(candidate.Body); break; }
                    cursor = parts.Find(p => p.Body == cursor.ParentBody);
                }
            }
            var own = GetComponent<Collider>();
            foreach (Collider collider in colliders)
            {
                if (own) Physics.IgnoreCollision(collider, own);
                foreach (Collider other in colliders)
                    if (other != collider) Physics.IgnoreCollision(collider, other);
            }
            lastRootPosition = rootBody.position;
            stableFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            lastSupportedAt = Time.time;
            initialized = parts.Count == 14;
            Debug.Log($"PHYSICAL_GOAT_READY goat={name} dynamicParts={parts.Count} gravity={parts.TrueForAll(p => p.Body.useGravity)}");
        }

        private void Add(Dictionary<string, Transform> bones, string name, string parentName,
            string endName, float mass, float radius, float spring, float angle)
        {
            if (!bones.TryGetValue(name, out Transform bone)) return;
            Rigidbody parent = rootBody;
            if (parentName != null)
            {
                Part parentPart = parts.Find(p => p.Bone.name == parentName);
                if (parentPart == null) return;
                parent = parentPart.Body;
            }
            var proxy = new GameObject($"Physics {name}");
            proxy.transform.SetParent(simulationRoot, true);
            proxy.transform.SetPositionAndRotation(bone.position, bone.rotation);
            var owner = proxy.AddComponent<GoatPhysicsPart>();
            owner.Owner = this;
            var body = proxy.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.maxAngularVelocity = 18f;

            Vector3 end = endName != null && bones.TryGetValue(endName, out Transform next)
                ? next.position : bone.position + (name == "Head" ? bone.forward * .25f : Vector3.down * .39f);
            Vector3 direction = end - bone.position;
            if (name == "Head")
            {
                var sphere = proxy.AddComponent<SphereCollider>();
                sphere.radius = radius;
                sphere.center = proxy.transform.InverseTransformPoint(bone.position + direction * .45f);
                colliders.Add(sphere);
                body.centerOfMass = sphere.center;
            }
            else
            {
                var shape = new GameObject("Physical volume");
                shape.transform.SetParent(proxy.transform, false);
                shape.transform.position = bone.position + direction * .5f;
                shape.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                var capsule = shape.AddComponent<CapsuleCollider>();
                capsule.radius = radius;
                capsule.height = Mathf.Max(radius * 2.05f, direction.magnitude + radius);
                colliders.Add(capsule);
                body.centerOfMass = proxy.transform.InverseTransformPoint(shape.transform.position);
            }

            var joint = proxy.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = parent;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = parent.transform.InverseTransformPoint(bone.position);
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            // Keep a physical range of motion around the bind pose. Free angular
            // axes let the spine fold through itself during an ordinary turn.
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -angle };
            joint.highAngularXLimit = new SoftJointLimit { limit = angle };
            joint.angularYLimit = new SoftJointLimit { limit = angle };
            joint.angularZLimit = new SoftJointLimit { limit = angle };
            // Muscle torque below supplies the active rest posture;
            // targetRotation=identity is not the FBX bind orientation.
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive = new JointDrive { positionSpring = 0f, positionDamper = 0f, maximumForce = 0f };
            joint.enableCollision = false;
            joint.enablePreprocessing = false;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = .08f;
            joint.projectionAngle = 10f;
            parts.Add(new Part
            {
                Bone = bone, Body = body, ParentBody = parent, Joint = joint, Spring = spring,
                IsLeg = name.StartsWith("Front") || name.StartsWith("Hind"),
                IsElastic = name.StartsWith("Elastic"),
                RestPosition = transform.InverseTransformPoint(bone.position),
                RestRotation = Quaternion.Inverse(transform.rotation) * bone.rotation,
                RestRelativeRotation = Quaternion.Inverse(parent.rotation) * bone.rotation,
                RestBoneLocalRotation = bone.localRotation
            });
            if (name == "Pelvis")
            {
                pelvisJoint = joint;
                pelvisBaseAnchor = joint.connectedAnchor;
            }
        }

        private void FixedUpdate()
        {
            if (!initialized) TryInitialize();
            if (!initialized || !rootBody) return;
            if (!ignoredPartnerColliders) IgnorePartnerColliders();
            if (Vector3.Distance(lastRootPosition, rootBody.position) > 6f)
                ResetPose();
            lastRootPosition = rootBody.position;

            bool supported = ground && ground.IsGrounded;
            bool linked = interaction && interaction.IsLinked;
            bool carried = motor && motor.IsPredatorCarried;
            float jumpAge = Time.time - jumpStartedAt;
            if (supported && jumpAge > .18f && rootBody.linearVelocity.y <= .1f)
            {
                jumpStartedAt = -100f;
                jumpAge = float.PositiveInfinity;
            }
            bool jumping = !linked && !carried && jumpAge >= 0f && jumpAge < 1.25f;
            float jumpPose = jumping && jumpAge < .72f
                ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(jumpAge / .72f)) : 0f;
            float braceTarget = supported && !linked && !carried
                && motor && motor.IsBracingForJump ? 1f : 0f;
            jumpBrace = Mathf.MoveTowards(jumpBrace, braceTarget, Time.fixedDeltaTime * 8f);
            float takeoffPose = Mathf.Max(jumpPose, jumpBrace * .65f);
            string clip = visual ? visual.CurrentClip : null;
            Vector3 horizontalVelocity = motor ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) : Vector3.zero;
            bool walking = supported && !carried && !linked && clip == "Goat_Walk"
                && horizontalVelocity.sqrMagnitude > .16f;
            float walkTarget = walking ? Mathf.InverseLerp(.4f, 2.5f, horizontalVelocity.magnitude) : 0f;
            walkBlend = Mathf.MoveTowards(walkBlend, walkTarget, Time.fixedDeltaTime * 6f);
            float actionStrength = ActionStrength(clip);
            if (clip != lastActionClip)
            {
                if (actionStrength > 0f) actionBlend = 0f;
                lastActionClip = clip;
            }
            bool actionSampleReady = clip == sampledActionClip && Time.frameCount - lastActionSampleFrame <= 2;
            float actionTarget = supported && !carried && actionSampleReady ? actionStrength : 0f;
            actionBlend = Mathf.MoveTowards(actionBlend, actionTarget, Time.fixedDeltaTime * 9f);
            float leanTarget = 0f;
            float rollTarget = 0f;
            float liftTarget = 0f;
            if (supported && !linked && !carried)
                GetGroundPosture(out leanTarget, out rollTarget, out liftTarget);
            groundLean = Mathf.MoveTowards(groundLean, leanTarget, Time.fixedDeltaTime * 35f);
            groundRoll = Mathf.MoveTowards(groundRoll, rollTarget, Time.fixedDeltaTime * 35f);
            groundLift = Mathf.MoveTowards(groundLift, liftTarget, Time.fixedDeltaTime * .9f);
            eagleLift = Mathf.MoveTowards(eagleLift,
                linked && Time.time < eagleTugUntil ? .2f : 0f, Time.fixedDeltaTime * .8f);
            if (pelvisJoint)
                pelvisJoint.connectedAnchor = pelvisBaseAnchor + Vector3.up * (groundLift + eagleLift);
            if (supported) lastSupportedAt = Time.time;
            // While the gameplay capsule is supported, proxy colliders can
            // hook into the terrain during a turn and flip the whole rig.
            // Airborne parts still collide with rocks during a fall.
            bool partContactsEnabled = !supported || carried;
            foreach (Collider collider in colliders)
                if (collider.isTrigger == partContactsEnabled)
                    collider.isTrigger = !partContactsEnabled;
            // The existing horn joint is anchored in the gameplay body. Keep
            // that body rotationally stable while linked so the anchor does not
            // spin away and break the rescue. The visible articulated body still
            // simulates each segment under gravity.
            // A single missed ground probe must not turn ordinary walking into
            // a tumble. After a real step off the ledge, release the capsule
            // and let gravity rotate the whole animal.
            bool dynamicRoot = carried || (!supported && !linked && !jumping
                && Time.time - lastSupportedAt > .3f);
            if (dynamicRoot)
            {
                if (rootBody.constraints != RigidbodyConstraints.None)
                    rootBody.constraints = RigidbodyConstraints.None;
            }
            else
            {
                if (rootBody.constraints != RigidbodyConstraints.FreezeRotation)
                    rootBody.constraints = RigidbodyConstraints.FreezeRotation;
                if (supported && linked && interaction && interaction.Partner)
                {
                    Vector3 towardPartner = Vector3.ProjectOnPlane(
                        interaction.Partner.transform.position - rootBody.position, Vector3.up);
                    if (towardPartner.sqrMagnitude > .04f)
                        stableFacing = towardPartner.normalized;
                }
                else if (supported && motor)
                {
                    Vector3 movement = Vector3.ProjectOnPlane(motor.MoveDirection, Vector3.up);
                    if (movement.sqrMagnitude > .02f)
                        stableFacing = movement.normalized;
                }
                // The visual is a child of this Rigidbody: using its Facing
                // as the target made the target turn again with every root
                // rotation, so an idle goat spun forever.
                if (stableFacing.sqrMagnitude < .01f)
                    stableFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                if (stableFacing.sqrMagnitude < .01f) stableFacing = Vector3.forward;
                rootBody.MoveRotation(Quaternion.RotateTowards(rootBody.rotation,
                    Quaternion.LookRotation(stableFacing, Vector3.up), 180f * Time.fixedDeltaTime));
            }

            // Active ragdoll muscles apply physical torque, not bone rotations.
            // Gravity, impulses, contacts and joint limits still decide the pose.
            float unsupportedAge = Mathf.Max(0f, Time.time - lastSupportedAt);
            float compliance = carried ? .09f : jumping ? 1.05f : supported ? 1f
                : Mathf.Lerp(.7f, .17f, Mathf.Clamp01(unsupportedAge / .75f));
            foreach (Part part in parts)
            {
                if (supported && !carried)
                {
                    // Muscles carry most, but not all, of the downstream
                    // weight. The remainder bends the spine under gravity.
                    Vector3 anchor = part.Body.transform.TransformPoint(part.Joint.anchor);
                    Vector3 weightMoment = Vector3.zero;
                    foreach (Rigidbody segment in part.Descendants)
                        weightMoment += Vector3.Cross(segment.worldCenterOfMass - anchor,
                            Physics.gravity * segment.mass);
                    Vector3 supportMoment = -weightMoment * .8f;
                    part.Body.AddTorque(supportMoment, ForceMode.Force);
                    if (!part.ParentBody.isKinematic)
                        part.ParentBody.AddTorque(-supportMoment, ForceMode.Force);
                }
                // The authored walk clip is sampled on the skinned bones before
                // LateUpdate copies the simulated pose over them. Use that
                // sample as a muscle target, never as a direct bone override.
                Quaternion relativeTarget = part.RestRelativeRotation;
                if (part.IsLeg && walkBlend > .001f && Time.frameCount - part.LastWalkSampleFrame <= 2)
                    relativeTarget = Quaternion.SlerpUnclamped(relativeTarget, part.WalkRelativeRotation,
                        walkBlend * 1.8f);
                if (actionBlend > .001f && part.ActionClip == sampledActionClip)
                    relativeTarget = Quaternion.Slerp(relativeTarget, part.ActionRelativeRotation,
                        actionBlend * (part.IsElastic ? .7f : 1f));
                if (takeoffPose > .001f)
                {
                    string bone = part.Bone.name;
                    if (bone == "Pelvis")
                        relativeTarget = Quaternion.AngleAxis(-6f * takeoffPose, Vector3.right)
                            * relativeTarget;
                    else if (bone == "Spine" || bone == "ElasticChest")
                        relativeTarget = Quaternion.AngleAxis(-8f * takeoffPose, Vector3.right)
                            * relativeTarget;
                    else if (bone == "FrontUpper.L" || bone == "FrontUpper.R")
                    {
                        float side = bone.EndsWith(".L") ? 1f : -1f;
                        relativeTarget *= Quaternion.AngleAxis(side * 15f * takeoffPose,
                            Vector3.forward);
                    }
                }
                if (part.Bone.name == "Pelvis")
                    relativeTarget = Quaternion.AngleAxis(groundRoll, Vector3.forward)
                        * Quaternion.AngleAxis(groundLean, Vector3.right) * relativeTarget;
                Quaternion desired = part.ParentBody.rotation * relativeTarget;
                Quaternion difference = desired * Quaternion.Inverse(part.Body.rotation);
                difference.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                if (axis.sqrMagnitude < .1f) continue;
                float stiffness = part.Spring * compliance
                    * (part.IsLeg && supported ? Mathf.Lerp(1f, 1.6f, walkBlend) : 1f)
                    * (supported ? Mathf.Lerp(1f, part.IsLeg ? 1.7f : 1.2f, actionBlend) : 1f);
                float damping = 1.25f * Mathf.Sqrt(stiffness);
                Vector3 relativeSpin = part.Body.angularVelocity - part.ParentBody.angularVelocity;
                Vector3 angularAcceleration = axis.normalized * (angle * Mathf.Deg2Rad * stiffness)
                    - relativeSpin * damping;
                part.Body.AddTorque(Vector3.ClampMagnitude(angularAcceleration, 900f), ForceMode.Acceleration);
            }
        }

        private void GetGroundPosture(out float lean, out float roll, out float lift)
        {
            Vector3 forward = Vector3.ProjectOnPlane(rootBody.rotation * Vector3.forward, Vector3.up).normalized;
            lean = 0f;
            roll = 0f;
            lift = 0f;
            if (forward.sqrMagnitude < .1f) return;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 frontPoint = rootBody.position + forward * .25f;
            Vector3 rearPoint = rootBody.position - forward * .7f;
            bool frontLeft = TryGroundHeight(frontPoint - right * .21f, out float frontLeftY);
            bool frontRight = TryGroundHeight(frontPoint + right * .21f, out float frontRightY);
            bool rearLeft = TryGroundHeight(rearPoint - right * .21f, out float rearLeftY);
            bool rearRight = TryGroundHeight(rearPoint + right * .21f, out float rearRightY);
            int frontCount = (frontLeft ? 1 : 0) + (frontRight ? 1 : 0);
            int rearCount = (rearLeft ? 1 : 0) + (rearRight ? 1 : 0);
            bool front = frontCount > 0;
            bool rear = rearCount > 0;
            float frontY = front ? ((frontLeft ? frontLeftY : 0f) + (frontRight ? frontRightY : 0f)) / frontCount : 0f;
            float rearY = rear ? ((rearLeft ? rearLeftY : 0f) + (rearRight ? rearRightY : 0f)) / rearCount : 0f;
            if (front && rear)
            {
                float slope = Mathf.Atan2(rearY - frontY, .95f) * Mathf.Rad2Deg;
                lean = Mathf.Clamp(2f + slope * .8f, -10f, 14f);
            }
            else if (!front && rear) lean = 12f;
            else if (front && !rear) lean = -6f;
            int leftCount = (frontLeft ? 1 : 0) + (rearLeft ? 1 : 0);
            int rightCount = (frontRight ? 1 : 0) + (rearRight ? 1 : 0);
            if (leftCount > 0 && rightCount > 0)
            {
                float leftY = ((frontLeft ? frontLeftY : 0f) + (rearLeft ? rearLeftY : 0f)) / leftCount;
                float rightY = ((frontRight ? frontRightY : 0f) + (rearRight ? rearRightY : 0f)) / rightCount;
                roll = Mathf.Clamp(Mathf.Atan2(rightY - leftY, .42f) * Mathf.Rad2Deg * .9f, -12f, 12f);
            }
            if (front || rear)
            {
                float highest = front && rear ? Mathf.Max(frontY, rearY) : front ? frontY : rearY;
                lift = Mathf.Clamp(highest - rootBody.position.y, 0f, .24f);
            }
        }

        private bool TryGroundHeight(Vector3 horizontalPoint, out float height)
        {
            Vector3 origin = horizontalPoint + Vector3.up * 1.5f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, footingHits, 3.7f,
                ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            height = 0f;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = footingHits[i];
                if (!hit.collider || hit.collider.GetComponentInParent<GoatController>()) continue;
                if (hit.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                height = hit.point.y;
            }
            return nearest < float.PositiveInfinity;
        }

        private static float ActionStrength(string clip)
        {
            switch (clip)
            {
                case "Goat_EatGrass":
                case "Goat_Pee":
                case "Goat_Poop":
                case "Goat_Sequence":
                case "GoatA_Duo_Performance":
                case "GoatB_Duo_Performance":
                case "Goat_Push":
                    return 1f;
                case "Goat_PushReact":
                case "Goat_GrabStart":
                case "Goat_GrabbedStart":
                case "Goat_Release":
                    return .7f;
                case "Goat_GrabHold":
                case "Goat_GrabbedHold":
                case "Goat_Pull":
                case "Goat_RescueBrace":
                case "Goat_GripStrain":
                    return .35f;
                default:
                    return 0f;
            }
        }

        private static bool HasElasticCurves(string clip)
        {
            return clip == "Goat_EatGrass" || clip == "Goat_Pee" || clip == "Goat_Poop"
                || clip == "Goat_Sequence" || clip == "GoatA_Duo_Performance"
                || clip == "GoatB_Duo_Performance";
        }

        private void IgnorePartnerColliders()
        {
            foreach (GoatPhysicalBody other in FindObjectsByType<GoatPhysicalBody>(FindObjectsSortMode.None))
            {
                if (other == this || !other.initialized) continue;
                Collider otherRoot = other.GetComponent<Collider>();
                Collider ownRoot = GetComponent<Collider>();
                foreach (Collider ownPart in colliders)
                {
                    if (otherRoot) Physics.IgnoreCollision(ownPart, otherRoot);
                    foreach (Collider otherPart in other.colliders)
                        Physics.IgnoreCollision(ownPart, otherPart);
                }
                foreach (Collider otherPart in other.colliders)
                    if (ownRoot) Physics.IgnoreCollision(otherPart, ownRoot);
                ignoredPartnerColliders = true;
            }
        }

        private void LateUpdate()
        {
            if (!initialized) TryInitialize();
            if (!initialized) return;
            // Animation may still move ears/tail/facial bones. The weighted body
            // bones always take their final positions from the physics solver.
            bool sampleWalk = visual && visual.CurrentClip == "Goat_Walk";
            string actionClip = visual ? visual.CurrentClip : null;
            bool sampleAction = ActionStrength(actionClip) > 0f;
            if (sampleAction)
            {
                sampledActionClip = actionClip;
                lastActionSampleFrame = Time.frameCount;
            }
            foreach (Part part in parts)
            {
                if (sampleWalk && part.IsLeg && part.Bone)
                {
                    part.WalkRelativeRotation = part.Bone.localRotation;
                    part.LastWalkSampleFrame = Time.frameCount;
                }
                if (sampleAction && part.Bone && (!part.IsElastic || HasElasticCurves(actionClip)))
                {
                    // The clip writes local bone rotations before this
                    // LateUpdate. Convert each authored change to the rest
                    // frame of its physical joint; some simulated torso
                    // links have different parents from the Blender rig.
                    Quaternion localDelta = Quaternion.Inverse(part.RestBoneLocalRotation)
                        * part.Bone.localRotation;
                    part.ActionRelativeRotation = part.RestRelativeRotation * localDelta;
                    part.ActionClip = actionClip;
                }
                if (part.Bone && part.Body)
                    part.Bone.SetPositionAndRotation(part.Body.transform.position, part.Body.transform.rotation);
            }
        }

        public void ResetPose()
        {
            if (!initialized) return;
            foreach (Part part in parts)
            {
                part.Body.position = transform.TransformPoint(part.RestPosition);
                part.Body.rotation = transform.rotation * part.RestRotation;
                part.Body.linearVelocity = Vector3.zero;
                part.Body.angularVelocity = Vector3.zero;
                part.Body.WakeUp();
            }
            lastRootPosition = rootBody.position;
            stableFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            lastSupportedAt = Time.time;
            walkBlend = 0f;
            groundLean = 0f;
            groundRoll = 0f;
            groundLift = 0f;
            eagleLift = 0f;
            eagleTugUntil = 0f;
            jumpStartedAt = -100f;
            jumpBrace = 0f;
            if (pelvisJoint) pelvisJoint.connectedAnchor = pelvisBaseAnchor;
            actionBlend = 0f;
            lastActionClip = null;
            sampledActionClip = null;
        }

        private void OnDestroy()
        {
            if (simulationRoot) Destroy(simulationRoot.gameObject);
        }
    }

    /// <summary>Identifies a proxy collider so gameplay raycasts can ignore its owner.</summary>
    public sealed class GoatPhysicsPart : MonoBehaviour
    {
        public GoatPhysicalBody Owner { get; set; }
    }
}
