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
        [System.Serializable]
        private sealed class Part
        {
            public Transform Bone;
            public Rigidbody Body;
            public Rigidbody ParentBody;
            public ConfigurableJoint Joint;
            public Vector3 RestPosition;
            public Vector3 TipLocal;
            public Quaternion RestRotation;
            public Quaternion RestRelativeRotation;
            public Quaternion RestBoneLocalRotation;
            public Quaternion WalkRelativeRotation;
            public Quaternion WalkNeutralRotation;
            public int LastWalkSampleFrame = -100;
            public Quaternion ActionRelativeRotation;
            public string ActionClip;
            public bool IsLeg;
            public bool IsElastic;
            public float Spring;
            public List<Rigidbody> Descendants = new List<Rigidbody>(14);
        }

        // Preserve the live solver links across an Editor script reload. Keeping
        // only the scene bodies leaves an unowned ragdoll and stale initialized flag.
        [SerializeField] private List<Part> parts = new List<Part>(16);
        [SerializeField] private List<Collider> colliders = new List<Collider>(16);
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
        private float walkPhase;
        private Vector3 previousMotionVelocity;
        private bool motionWasSupported;
        private float motionPitch;
        private float motionRoll;
        private float motionLift;
        private float landingCompression;
        private float turnStepUntil;
        private float turnWalkTime;
        private float turnWalkDirection = 1f;
        private float turnStartedAt = -100f;
        private float turnHeadLead;
        private float turnChestLead;
        private bool turningInPlace;
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
        private CapsuleCollider contactHull;
        private Part contactRear, contactFront;
        private CapsuleCollider neckContactHull, headContactHull;
        private Part contactHead;
        private GoatPhysicalBody contactPartner;
        private GameObject walkSampler;
        private AnimationClip walkClip;
        private GameObject walkAnimationRoot;
        private Dictionary<string, Transform> walkBones;

        private void PrepareWalkSampler()
        {
            if(!walkSampler)
            {
                var prefab=Resources.Load<GameObject>("GoatDuoRefined");
                if(!prefab) return;
                walkSampler=Instantiate(prefab,simulationRoot);
                walkSampler.name="Isolated walk animation sampler";
                walkSampler.hideFlags=HideFlags.HideInHierarchy;
                foreach(var renderer in walkSampler.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
                var animation=walkSampler.GetComponentInChildren<Animation>();
                if(!animation || animation["Goat_Walk"]==null) return;
                walkAnimationRoot=animation.gameObject;walkClip=animation["Goat_Walk"].clip;
                animation.Stop();animation.enabled=false;
                if(animation["Goat_Idle"]!=null) animation["Goat_Idle"].clip.SampleAnimation(walkAnimationRoot,0f);
                CacheWalkBones();
                foreach(var part in parts)
                    if(walkBones.TryGetValue(part.Bone.name,out var bone)) part.WalkNeutralRotation=bone.localRotation;
            }
            else if(walkBones==null) CacheWalkBones();
        }

        private void CacheWalkBones()
        {
            walkBones=new Dictionary<string,Transform>();
            foreach(var rig in walkSampler.GetComponentsInChildren<Transform>(true))
                if(rig.name=="GoatRig_A")
                {
                    foreach(var bone in rig.GetComponentsInChildren<Transform>(true)) walkBones.TryAdd(bone.name,bone);
                    break;
                }
        }

        public bool RootIsTumbling => rootBody && rootBody.constraints == RigidbodyConstraints.None;
        public Vector3 HarnessPosition => contactRear != null && contactRear.Body
            ? contactRear.Body.position : transform.TransformPoint(Vector3.up * 1.2f);
        public Rigidbody HarnessBody => contactRear != null && contactRear.Body ? contactRear.Body : GetComponent<Rigidbody>();
        public Vector3 HarnessChest => contactFront != null && contactFront.Body ? contactFront.Body.position : HarnessPosition+transform.forward*.8f;
        public Vector3 DroppingsPosition => BodyAttachment(true);
        public Vector3 UrinePosition => BodyAttachment(false);
        private Vector3 BodyAttachment(bool droppings)
        {
            var attachments=GetComponent<GoatSkin>()?.BodyAttachments;
            if(attachments && contactRear!=null && contactRear.Body)
                return contactRear.Body.transform.TransformPoint(droppings ? attachments.droppingsOffset : attachments.urineOffset);
            Vector3 forward=(HarnessChest-HarnessPosition).normalized;
            return droppings ? HarnessPosition-forward*.34f+Vector3.down*.17f
                : HarnessPosition+forward*.16f+Vector3.down*.28f+Vector3.Cross(Vector3.up,forward).normalized*.12f;
        }
        public Vector3 NosePosition
        {
            get
            {
                if(contactHead==null || !contactHead.Body) return HarnessChest+transform.forward*.55f+Vector3.up*.35f;
                var attachments=GetComponent<GoatSkin>()?.HeadAttachments;
                return attachments ? contactHead.Body.transform.TransformPoint(attachments.noseOffset)
                    : contactHead.Body.position+contactHead.Body.rotation*new Vector3(0f,-.015f,.34f);
            }
        }
        public bool TryGetRightHoof(out Vector3 point,out Quaternion rotation)
        {
            var leg=parts.Find(part=>part.Bone.name=="FrontLower.R");
            point=leg!=null ? leg.Body.position+leg.Body.rotation*leg.TipLocal : HarnessChest+transform.right*.3f;
            rotation=leg!=null ? leg.Body.rotation : transform.rotation;
            return leg!=null;
        }
        public Vector3 RestPelvisOffset => contactRear != null ? contactRear.RestPosition : Vector3.up * 1.2f;
        public float TotalSimulatedMass
        {
            get
            {
                float mass = rootBody ? rootBody.mass : GetComponent<Rigidbody>().mass;
                foreach (var part in parts) if (part.Body) mass += part.Body.mass;
                return mass;
            }
        }
        public void ApplyCatchVelocityChange(Vector3 change)
        {
            foreach (var part in parts)
                if (part.Body) part.Body.AddForce(change,ForceMode.VelocityChange);
        }
        public Collider[] GetRideColliders()
        {
            var result = new List<Collider>(GetComponentsInChildren<Collider>());
            // The simulated skeleton is a scene sibling, not a child of the goat.
            if (simulationRoot) result.AddRange(simulationRoot.GetComponentsInChildren<Collider>());
            return result.ToArray();
        }
        public int SimulatedParts => parts.Count;
        public bool IsEagleStrained => Time.time < eagleTugUntil;
        public GoatPartState[] CaptureNetworkPose()
        {
            if (!initialized) TryInitialize();
            var result = new GoatPartState[parts.Count];
            for (int i = 0; i < parts.Count; i++)
                result[i] = new GoatPartState
                {
                    position = parts[i].Body.position,
                    rotation = parts[i].Body.rotation
                };
            return result;
        }

        public void ApplyNetworkPose(GoatPartState[] state)
        {
            if (MountainAuthority.IsHost || state == null) return;
            if (!initialized) TryInitialize();
            if (state.Length != parts.Count) return;
            SetSimulationAuthority(false);
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Body.position = state[i].position;
                parts[i].Body.rotation = state[i].rotation;
            }
        }

        private void SetSimulationAuthority(bool host)
        {
            foreach (var part in parts)
                if (part.Body.isKinematic == host)
                {
                    part.Body.isKinematic = !host;
                    if (host) part.Body.WakeUp();
                }
        }

        public void BeginJump(Vector3 launchVelocity)
        {
            if (!initialized) TryInitialize();
            if (!initialized) return;
            jumpStartedAt = Time.time;
            var tuning=GoatMechanics.For(this);
            var beer=GetComponent<GoatBeerEffect>();
            bool onSpot=motor && motor.MoveDirection.sqrMagnitude<.01f
                && Vector3.ProjectOnPlane(rootBody.linearVelocity,Vector3.up).magnitude<tuning.stationaryJumpSpeed;
            beer?.BeginJump(onSpot);
            float leadScale=onSpot && beer?.Intoxicated!=true ? tuning.stationaryJumpFrontLead : 1f;
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
                part.Body.AddForce(launchVelocity + Vector3.up * frontLead * leadScale,
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
            var attachments=GetComponent<GoatSkin>()?.HeadAttachments;
            if(attachments) contact=headBody.transform.TransformPoint(attachments.hornOffset)+forward*.08f;
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
            Add(byName, "Pelvis", null, "ElasticMid", 2f, .22f, 440f, 32f);
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
            CreateContactHull();
            Debug.Log($"PHYSICAL_GOAT_READY goat={name} dynamicParts={parts.Count} gravity={parts.TrueForAll(p => p.Body.useGravity)}");
        }

        private void CreateContactHull()
        {
            contactRear = parts.Find(part => part.Bone.name == "Pelvis");
            contactFront = parts.Find(part => part.Bone.name == "ElasticChest");
            int playerLayer = LayerMask.NameToLayer("Player");
            if (contactRear == null || contactFront == null || playerLayer < 0) return;
            var hull = new GameObject("Goat body contact");
            hull.layer = playerLayer;
            hull.transform.SetParent(transform, false);
            contactHull = hull.AddComponent<CapsuleCollider>();
            contactHull.direction = 2;
            contactHull.radius = .36f;
            // Only the gameplay bodies collide here. Keep terrain contact on
            // the existing locomotion capsule and individual airborne parts.
            contactHull.excludeLayers = ~(1 << playerLayer);
            contactHead = parts.Find(part => part.Bone.name == "Head");
            if (contactHead != null)
            {
                neckContactHull = CreateContactCapsule("Goat neck contact", .18f, playerLayer);
                headContactHull = CreateContactCapsule("Goat head contact", .2f, playerLayer);
            }
            UpdateContactHull();
        }

        private CapsuleCollider CreateContactCapsule(string label, float radius, int playerLayer)
        {
            var shape = new GameObject(label);
            shape.layer = playerLayer;
            shape.transform.SetParent(transform, false);
            var capsule = shape.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = radius;
            capsule.excludeLayers = ~(1 << playerLayer);
            return capsule;
        }

        private static void FitContactCapsule(CapsuleCollider capsule, Vector3 start, Vector3 end)
        {
            Vector3 span = end - start;
            if (!capsule || span.sqrMagnitude < .001f) return;
            capsule.transform.SetPositionAndRotation((start + end) * .5f,
                Quaternion.FromToRotation(Vector3.forward, span.normalized));
            capsule.height = span.magnitude + capsule.radius * 2f;
        }

        private void SetHeadContactWith(GoatPhysicalBody other, bool ignore)
        {
            if (!other) return;
            SetContactPair(headContactHull, other.headContactHull, ignore);
            SetContactPair(headContactHull, other.neckContactHull, ignore);
            SetContactPair(neckContactHull, other.headContactHull, ignore);
            SetContactPair(neckContactHull, other.neckContactHull, ignore);
        }

        private static void SetContactPair(Collider own, Collider other, bool ignore)
        {
            if (own && other) Physics.IgnoreCollision(own, other, ignore);
        }

        private void UpdateContactHull()
        {
            if (!contactHull || contactRear == null || contactFront == null || !contactRear.Body || !contactFront.Body) return;
            Vector3 rear = contactRear.Body.position;
            Vector3 front = contactFront.Body.position;
            FitContactCapsule(contactHull, rear, front);
            if (contactHead != null && contactHead.Body)
            {
                Vector3 head = contactHead.Body.position;
                var attachments=GetComponent<GoatSkin>()?.HeadAttachments;
                if(attachments) head=contactHead.Body.transform.TransformPoint(attachments.headCollisionOffset);
                FitContactCapsule(neckContactHull, front, head);
                FitContactCapsule(headContactHull, head,
                    attachments ? NosePosition : head + contactHead.Body.rotation * Vector3.forward * .28f);
            }
            GoatPhysicalBody linkedPartner = interaction && interaction.Partner
                ? interaction.Partner.GetComponent<GoatPhysicalBody>() : null;
            if (linkedPartner != contactPartner)
            {
                SetHeadContactWith(contactPartner, false);
                contactPartner = linkedPartner;
                // Horns must be able to interlock. Even while linked, each
                // head still collides with the other goat's torso.
                SetHeadContactWith(contactPartner, true);
            }
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
                TipLocal = proxy.transform.InverseTransformPoint(end),
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
            SetSimulationAuthority(MountainAuthority.IsHost);
            UpdateContactHull();
            if (!MountainAuthority.IsHost) return;
            if (!ignoredPartnerColliders) IgnorePartnerColliders();
            if (Vector3.Distance(lastRootPosition, rootBody.position) > 6f)
                ResetPose();
            lastRootPosition = rootBody.position;

            var tuning=GoatMechanics.For(this);
            var beer=GetComponent<GoatBeerEffect>();
            var comedy = GetComponent<GoatComedyActions>();
            bool riding = comedy && comedy.IsMounted;
            bool pretending = comedy && (comedy.PlayingDead || comedy.Recovering);
            bool supported = riding || (ground && ground.IsGrounded);
            bool madness = GetComponent<GoatDescentTricks>()?.MadnessActive == true;
            if (pelvisJoint)
            {
                float limit = pretending ? 95f : madness ? 85f : tuning.pelvisSwingDegrees;
                pelvisJoint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
                pelvisJoint.highAngularXLimit = new SoftJointLimit { limit = limit };
                pelvisJoint.angularZLimit = new SoftJointLimit { limit = pretending ? 95f : riding ? 50f : tuning.pelvisSwingDegrees };
                pelvisJoint.angularYLimit = new SoftJointLimit { limit = pretending ? 95f : tuning.pelvisSwingDegrees };
            }
            bool linked = interaction && interaction.IsLinked;
            bool carried = motor && motor.IsPredatorCarried;
            float axePose = GetComponent<GoatDescentInventory>()?.AxePoseBlend ?? 0f;
            float jumpAge = Time.time - jumpStartedAt;
            if (supported && jumpAge > .18f && rootBody.linearVelocity.y <= .1f)
            {
                jumpStartedAt = -100f;
                jumpAge = float.PositiveInfinity;
            }
            bool jumping = !linked && !carried && jumpAge >= 0f && jumpAge < tuning.jumpStabilizationSeconds;
            float jumpPose = jumping && jumpAge < tuning.jumpPoseSeconds
                ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(jumpAge / tuning.jumpPoseSeconds)) : 0f;
            if(jumping && beer?.UseCalmJump==true) jumpPose*=tuning.stationaryJumpPose;
            float braceTarget = supported && !linked && !carried
                && motor && motor.IsBracingForJump ? 1f : 0f;
            jumpBrace = Mathf.MoveTowards(jumpBrace, braceTarget, Time.fixedDeltaTime * 8f);
            float takeoffPose = Mathf.Max(jumpPose, jumpBrace * .65f);
            string clip = visual ? visual.CurrentClip : null;
            Vector3 horizontalVelocity = motor ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) : Vector3.zero;
            bool normalTurning = supported && !linked && !carried && !madness && !pretending && !riding
                && GetComponent<GoatPunchBuff>()?.PerformingPair != true
                && GetComponent<GoatToiletAction>()?.Active != true
                && GetComponent<GoatDescentTricks>()?.Busy != true
                && GetComponent<GoatComedyActions>()?.Busy != true
                && (clip == "Goat_Idle" || clip == "Goat_Walk" || clip == "Goat_Jump");
            Vector3 locomotionFacing = Vector3.zero;
            bool firstPersonTurning = false;
            if (supported && motor)
            {
                firstPersonTurning = normalTurning && motor.TryGetFirstPersonFacing(out locomotionFacing);
                if (!firstPersonTurning)
                    locomotionFacing = motor.HasMovementInput
                        ? Vector3.ProjectOnPlane(motor.MovementFacing, Vector3.up) : Vector3.zero;
                if (locomotionFacing.sqrMagnitude > .01f) locomotionFacing.Normalize();
            }
            float signedTurn = locomotionFacing.sqrMagnitude > .01f
                ? Vector3.SignedAngle(rootBody.rotation * Vector3.forward, locomotionFacing, Vector3.up) : 0f;
            bool stagedTurn = normalTurning && !jumping && Mathf.Abs(signedTurn) > 2f;
            if (stagedTurn)
            {
                float direction = Mathf.Sign(signedTurn);
                if (turnStartedAt < 0f || direction != turnWalkDirection)
                    turnStartedAt = Time.time;
                turnWalkDirection = direction;
            }
            else turnStartedAt = -100f;
            float turnAge = stagedTurn ? Time.time - turnStartedAt : 0f;
            // The gaze moves first, then the shoulders; the weight-bearing rear
            // follows after the front of the animal has committed to the turn.
            float headTarget = stagedTurn ? Mathf.Clamp(signedTurn, -35f, 35f)
                * Mathf.SmoothStep(0f, 1f, turnAge / (firstPersonTurning ? .06f : .12f)) : 0f;
            float chestTarget = stagedTurn ? Mathf.Clamp(signedTurn, -13f, 13f)
                * Mathf.SmoothStep(0f, 1f,
                    (turnAge - (firstPersonTurning ? .02f : .07f))
                    / (firstPersonTurning ? .08f : .16f)) : 0f;
            turnHeadLead = Mathf.MoveTowards(turnHeadLead, headTarget,
                Time.fixedDeltaTime * (firstPersonTurning ? 500f : 260f));
            turnChestLead = Mathf.MoveTowards(turnChestLead, chestTarget,
                Time.fixedDeltaTime * (firstPersonTurning ? 350f : 170f));
            if (normalTurning && horizontalVelocity.magnitude < .8f && Mathf.Abs(signedTurn) > 2f)
            {
                turnStepUntil = Time.time + .12f;
            }
            turningInPlace = normalTurning && horizontalVelocity.magnitude < .8f && Time.time < turnStepUntil;
            if (turningInPlace) turnWalkTime += turnWalkDirection * Time.fixedDeltaTime * .85f;
            bool walking = supported && !carried && !linked && clip == "Goat_Walk"
                && horizontalVelocity.sqrMagnitude > .16f;
            float walkTarget = walking ? Mathf.Lerp(.5f, 1f, Mathf.InverseLerp(.4f, 4.5f, horizontalVelocity.magnitude))
                : turningInPlace ? .65f : 0f;
            walkBlend = Mathf.MoveTowards(walkBlend, walkTarget, Time.fixedDeltaTime * 6f);
            UpdateMotionPosture(normalTurning && !jumping && !turningInPlace,
                supported, rootBody.linearVelocity);
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
            float liftTarget = comedy && comedy.PlayingDead ? -.55f : madness ? -.18f : 0f;
            var toilet=GetComponent<GoatToiletAction>();
            if (supported && !linked && !carried && !madness && !pretending && !riding)
                GetGroundPosture(out leanTarget, out rollTarget, out liftTarget);
            if(toilet && toilet.Active) liftTarget-=toilet.Crouch01*.36f;
            if(supported && !linked && !carried && !pretending && !riding && beer?.Intoxicated==true)
                rollTarget+=Mathf.Sin(Time.time*2.4f+(GetComponent<GoatLocalControl>()?.Label=="B" ? 1.8f : 0f))*beer.SwayDegrees;
            groundLean = Mathf.MoveTowards(groundLean, leanTarget, Time.fixedDeltaTime * 35f);
            groundRoll = Mathf.MoveTowards(groundRoll, rollTarget, Time.fixedDeltaTime * 35f);
            groundLift = Mathf.MoveTowards(groundLift, liftTarget, Time.fixedDeltaTime * .9f);
            eagleLift = Mathf.MoveTowards(eagleLift,
                linked && Time.time < eagleTugUntil ? .2f : 0f, Time.fixedDeltaTime * .8f);
            if (pelvisJoint)
                pelvisJoint.connectedAnchor = pelvisBaseAnchor + Vector3.up * (groundLift + eagleLift + motionLift);
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
            bool dynamicRoot = carried || (!riding && !pretending) && (!supported && !linked && !jumping
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
                if (GetComponent<GoatPunchBuff>()?.PerformingPair == true)
                    stableFacing = GetComponent<GoatPunchBuff>().PairFacing;
                else if (riding) stableFacing = comedy.RideFacing;
                else if (supported && GetComponent<GoatDescentTricks>()?.Spinning == true)
                    stableFacing = GetComponent<GoatDescentTricks>().SpinFacing;
                else if (supported && madness)
                    stableFacing = GetComponent<GoatDescentTricks>().MadnessFacing;
                else if (supported && linked && interaction && interaction.Partner)
                {
                    Vector3 towardPartner = Vector3.ProjectOnPlane(
                        interaction.Partner.transform.position - rootBody.position, Vector3.up);
                    if (towardPartner.sqrMagnitude > .04f)
                        stableFacing = towardPartner.normalized;
                }
                else if (locomotionFacing.sqrMagnitude > .01f)
                {
                    stableFacing = locomotionFacing;
                }
                else if (normalTurning)
                {
                    // Third-person camera orbit does not command the goat on
                    // its own. Releasing movement cancels the pending turn.
                    stableFacing = Vector3.ProjectOnPlane(
                        rootBody.rotation * Vector3.forward, Vector3.up).normalized;
                }
                // The visual is a child of this Rigidbody: using its Facing
                // as the target made the target turn again with every root
                // rotation, so an idle goat spun forever.
                if (stableFacing.sqrMagnitude < .01f)
                    stableFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                if (stableFacing.sqrMagnitude < .01f) stableFacing = Vector3.forward;
                float turnSpeed = GetComponent<GoatDescentTricks>()?.Spinning == true ? 720f
                    : stagedTurn ? firstPersonTurning
                        ? Mathf.Lerp(260f, 600f, Mathf.SmoothStep(0f, 1f, turnAge / .08f))
                        : Mathf.Lerp(110f, 300f, Mathf.SmoothStep(0f, 1f, turnAge / .11f))
                        : 150f;
                rootBody.MoveRotation(Quaternion.RotateTowards(rootBody.rotation,
                    Quaternion.LookRotation(stableFacing, Vector3.up), turnSpeed * Time.fixedDeltaTime));
            }

            // Active ragdoll muscles apply physical torque, not bone rotations.
            // Gravity, impulses, contacts and joint limits still decide the pose.
            float unsupportedAge = Mathf.Max(0f, Time.time - lastSupportedAt);
            float compliance = carried ? .09f : jumping ? tuning.jumpMuscleStrength : supported ? tuning.groundedMuscleStrength
                : Mathf.Lerp(.7f, .17f, Mathf.Clamp01(unsupportedAge / .75f));
            compliance*=tuning.muscleStrength;
            foreach (Part part in parts)
            {
                // Hips can swing, but shins cannot twist freely like ball joints.
                // Wide limits on every axis fold the skin through itself in a fall.
                bool authoredPose = ActionStrength(clip) > .01f || madness || pretending || riding || linked || carried;
                float authoredSide = part.IsLeg ? (part.Bone.name.Contains("Lower") ? 70f : 60f)
                    : part.Bone.name == "Head" ? 45f : part.Bone.name == "Neck" ? 35f : 22f;
                float sideLimit = authoredPose ? authoredSide
                    : part.IsLeg ? (part.Bone.name.Contains("Lower") ? tuning.lowerLegSideDegrees : tuning.upperLegSideDegrees) : tuning.spineBendDegrees;
                if(clip=="Goat_Pee" && part.Bone.name=="HindUpper.R") sideLimit=100f;
                if(part.Bone.name != "Pelvis")
                {
                    part.Joint.angularYLimit = new SoftJointLimit { limit = sideLimit };
                    part.Joint.angularZLimit = new SoftJointLimit { limit = sideLimit };
                }
                if (part.IsElastic)
                {
                    float bendLimit = authoredPose ? 22f : tuning.elasticBendDegrees;
                    part.Joint.lowAngularXLimit = new SoftJointLimit { limit = -bendLimit };
                    part.Joint.highAngularXLimit = new SoftJointLimit { limit = bendLimit };
                }
                if(part.Bone.name=="Spine" || part.Bone.name=="Head" || part.Bone.name.Contains("Lower"))
                {
                    float original = part.Bone.name=="Spine" ? 22f : part.Bone.name=="Head" ? 45f : 70f;
                    float limit = authoredPose ? original : part.Bone.name=="Spine" ? tuning.spineBendDegrees : part.Bone.name=="Head" ? tuning.headBendDegrees : 60f;
                    if(clip=="Goat_Poop" && part.Bone.name.StartsWith("HindLower")) limit=95f;
                    part.Joint.lowAngularXLimit=new SoftJointLimit {limit=-limit};
                    part.Joint.highAngularXLimit=new SoftJointLimit {limit=limit};
                }
                if (part.Bone.name == "Neck")
                {
                    float limit = riding ? 50f : authoredPose ? 35f : tuning.neckBendDegrees;
                    if(clip=="Goat_EatGrass" && GetComponent<GoatSkin>()?.HeadAttachments is GoatSkinDefinition attachments)
                        limit=Mathf.Max(limit,attachments.grassNeckBendDegrees);
                    part.Joint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
                    part.Joint.highAngularXLimit = new SoftJointLimit { limit = limit };
                }
                if (part.Bone.name == "FrontUpper.R")
                {
                    float punchLimit = madness ? 85f : (axePose>.01f || supported && clip == "Goat_RightPunch") ? 82f : authoredPose ? 60f : 45f;
                    part.Joint.lowAngularXLimit = new SoftJointLimit { limit = -punchLimit };
                    part.Joint.highAngularXLimit = new SoftJointLimit { limit = punchLimit };
                }
                if (part.Bone.name == "FrontUpper.L")
                {
                    float limit = madness ? 85f : authoredPose ? 60f : 45f;
                    part.Joint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
                    part.Joint.highAngularXLimit = new SoftJointLimit { limit = limit };
                }
                if (part.Bone.name.StartsWith("HindUpper"))
                {
                    float limit = madness ? 90f : authoredPose ? 60f : 45f;
                    if(clip=="Goat_Poop") limit=85f;
                    // Let the striking thigh extend above the hip during the authored kick.
                    // Ordinary walking retains its tighter joint range.
                    if (clip == "Goat_EagleRoundhouse" && part.Bone.name == "HindUpper.R")
                        limit = 170f;
                    part.Joint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
                    part.Joint.highAngularXLimit = new SoftJointLimit { limit = limit };
                }
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
                float legWalkBlend = walkBlend;
                if (turningInPlace && part.Bone.name.StartsWith("Hind"))
                    legWalkBlend *= Mathf.SmoothStep(0f, 1f, (turnAge - .16f) / .22f);
                if (part.IsLeg && legWalkBlend > .001f && Time.frameCount - part.LastWalkSampleFrame <= 2)
                    relativeTarget = Quaternion.SlerpUnclamped(relativeTarget, part.WalkRelativeRotation,
                        legWalkBlend * tuning.walkingAnimationAmplitude);
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
                if (axePose > .001f)
                {
                    Vector3 angles = part.Bone.name switch
                    {
                        "FrontUpper.R" => new Vector3(-68f,0,-8f),
                        "FrontLower.R" => new Vector3(22f,0,0),
                        "Spine" or "ElasticChest" => new Vector3(-5f,0,-3f),
                        "Head" => new Vector3(4f,-6f,0),
                        _ => Vector3.zero
                    };
                    if (angles!=Vector3.zero)
                        relativeTarget=Quaternion.Slerp(relativeTarget,part.RestRelativeRotation*Quaternion.Euler(angles),axePose);
                }
                if (part.Bone.name == "Pelvis" && riding)
                    relativeTarget = Quaternion.AngleAxis(comedy.RideLean * 42f, Vector3.forward) * relativeTarget;
                if (part.Bone.name == "Pelvis")
                    relativeTarget = Quaternion.AngleAxis(groundRoll, Vector3.forward)
                        * Quaternion.AngleAxis(groundLean, Vector3.right) * relativeTarget;
                Quaternion desired = part.ParentBody.rotation * relativeTarget;
                bool walkingStance = supported && !linked && !carried && !jumping
                    && !madness && !pretending && !riding && actionBlend < .01f;
                bool supportedTorso = walkingStance && !part.IsLeg;
                if (supportedTorso)
                {
                    // Each segment has a different yaw target. The joints and
                    // muscles transmit the head's lead down the flexible back.
                    float yawLead = part.Bone.name switch
                    {
                        "ElasticMid" => turnChestLead * .25f,
                        "Spine" => turnChestLead * .6f,
                        "ElasticChest" => turnChestLead,
                        "Neck" => Mathf.Lerp(turnChestLead, turnHeadLead, .55f),
                        "Head" => turnHeadLead,
                        _ => 0f
                    };
                    float lean = part.Bone.name == "Head" || part.Bone.name == "Neck" ? .45f : 1f;
                    Quaternion walkingSway = Quaternion.identity;
                    if (walkBlend > .001f && (part.Bone.name == "Head" || part.Bone.name == "Neck"))
                    {
                        float amplitude = (part.Bone.name == "Head" ? tuning.walkingHeadSwayDegrees
                            : tuning.walkingNeckSwayDegrees) * walkBlend * (1f - axePose);
                        float phase = walkPhase + (part.Bone.name == "Head" ? -.3f : 0f);
                        walkingSway = Quaternion.Euler(Mathf.Sin(phase * 2f) * amplitude * .45f,
                            Mathf.Sin(phase) * amplitude * .5f, Mathf.Cos(phase) * amplitude * .35f);
                    }
                    else if (walking && !turningInPlace)
                    {
                        // The shoulders and haunches settle a fraction apart,
                        // instead of rocking the whole torso as one rigid block.
                        float phaseOffset = part.Bone.name == "Pelvis" ? .65f
                            : part.Bone.name == "ElasticMid" ? .35f : 0f;
                        walkingSway = Quaternion.Euler(
                            Mathf.Sin(walkPhase * 2f + phaseOffset) * 1.3f * walkBlend,
                            0f, Mathf.Sin(walkPhase + phaseOffset) * .6f * walkBlend);
                    }
                    desired = Quaternion.AngleAxis(yawLead, Vector3.up) * rootBody.rotation
                        * Quaternion.AngleAxis((groundRoll + motionRoll) * lean, Vector3.forward)
                        * Quaternion.AngleAxis((groundLean + motionPitch) * lean, Vector3.right)
                        * walkingSway
                        * part.RestRotation;
                }
                if (supported && !linked && !carried && !jumping
                    && (part.Bone.name.StartsWith("FrontUpper") || part.Bone.name.StartsWith("HindUpper")))
                {
                    // Hips follow the sloping torso, but the legs hang along
                    // gravity. Keep the authored gait/action offset around an
                    // upright rest pose rather than inheriting the spine tilt.
                    Quaternion poseOffset = Quaternion.Inverse(part.RestRelativeRotation) * relativeTarget;
                    float frontLead = part.Bone.name.StartsWith("FrontUpper") ? turnChestLead * .6f : 0f;
                    Quaternion upright = Quaternion.AngleAxis(frontLead, Vector3.up)
                        * rootBody.rotation * part.RestRotation * poseOffset;
                    float footing = 1f - Mathf.Max(actionBlend,part.Bone.name=="FrontUpper.R" ? axePose : 0f);
                    desired = Quaternion.Slerp(desired, upright, footing);
                }
                var nerves = GetComponent<GoatHerdBehaviour>();
                if (nerves && supported && !linked && !carried && !riding && !pretending
                    && GetComponent<GoatToiletAction>()?.Active != true)
                {
                    float bleat = nerves.BleatPose;
                    if (part.Bone.name == "Neck") desired *= Quaternion.Euler(-6f * bleat + nerves.ShockPose * 5f, 0, 0);
                    else if (part.Bone.name == "Head") desired *= Quaternion.Euler(-8f * bleat + nerves.ShockPose * 8f,
                        Mathf.Sin(Time.time * 24) * bleat * 2.5f, nerves.ShockPose * 4f);
                }
                // Feed expression accents through the muscles so the rendered head
                // stays attached to its physical body and the staged turn is preserved.
                var emotion = GetComponent<GoatEmotionController>();
                if (emotion && supported && !linked && !carried && !riding && !pretending)
                {
                    if (part.Bone.name == "Head") desired *= Quaternion.Euler(emotion.HeadEuler);
                    else if (part.Bone.name == "Neck") desired *= Quaternion.Euler(emotion.HeadEuler * .3f);
                }
                Quaternion difference = desired * Quaternion.Inverse(part.Body.rotation);
                difference.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                if (axis.sqrMagnitude < .1f) continue;
                float stiffness = part.Spring * compliance
                    * (part.IsLeg && supported ? Mathf.Lerp(1f, 1.6f, walkBlend) : 1f)
                    * (supported ? Mathf.Lerp(1f, part.IsLeg ? 1.7f : 1.2f, actionBlend) : 1f);
                // Faster steps need quicker muscles as well as a faster clip;
                // otherwise the damped legs barely lift at a trot.
                if (part.IsLeg && walking && !turningInPlace)
                    stiffness *= Mathf.Lerp(1f, 2.1f, Mathf.InverseLerp(1.5f, 5.35f,
                        visual ? visual.LocomotionSpeed : horizontalVelocity.magnitude));
                if (supportedTorso && stagedTurn)
                    stiffness *= firstPersonTurning ? 3f : 1.5f;
                float damping = (jumping && beer ? beer.JumpDamping : walkingStance ? tuning.groundedDamping : tuning.airborneDamping) * Mathf.Sqrt(stiffness);
                Vector3 relativeSpin = part.Body.angularVelocity
                    - (supportedTorso ? rootBody.angularVelocity : part.ParentBody.angularVelocity);
                Vector3 angularAcceleration = axis.normalized * (angle * Mathf.Deg2Rad * stiffness)
                    - relativeSpin * damping;
                part.Body.AddTorque(Vector3.ClampMagnitude(angularAcceleration,
                    tuning.maxMuscleAcceleration * (supportedTorso && stagedTurn && firstPersonTurning ? 1.5f : 1f)),
                    ForceMode.Acceleration);
            }
        }

        private void UpdateMotionPosture(bool locomotion, bool supported, Vector3 velocity)
        {
            float dt = Time.fixedDeltaTime;
            Vector3 acceleration = (velocity - previousMotionVelocity) / Mathf.Max(dt, .001f);
            if (supported && !motionWasSupported && locomotion)
                landingCompression = Mathf.Max(landingCompression,
                    Mathf.InverseLerp(1.5f, 9f, -previousMotionVelocity.y));
            previousMotionVelocity = velocity;
            motionWasSupported = supported;
            landingCompression = Mathf.MoveTowards(landingCompression, 0f, dt * 3.5f);
            float pitch = 0f, roll = 0f, lift = 0f;
            if (locomotion)
            {
                Vector3 localAcceleration = Quaternion.Inverse(rootBody.rotation) * acceleration;
                // Haunches take the push-off; shoulders absorb braking. The head
                // receives less of this tilt, keeping the gaze steady.
                pitch = Mathf.Clamp(-localAcceleration.z * .24f, -4.5f, 5.5f);
                roll = Mathf.Clamp(-localAcceleration.x * .3f, -5f, 5f);
                float moving = Mathf.InverseLerp(.2f, 2f,
                    Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude) * walkBlend;
                pitch += Mathf.Sin(walkPhase * 2f - .45f) * .9f * moving;
                roll += Mathf.Sin(walkPhase) * 1.2f * moving;
                lift = (1f - Mathf.Cos(walkPhase * 2f)) * .012f * moving
                    - landingCompression * .055f;
            }
            float response = 1f - Mathf.Exp(-12f * dt);
            motionPitch = Mathf.Lerp(motionPitch, pitch, response);
            motionRoll = Mathf.Lerp(motionRoll, roll, response);
            motionLift = Mathf.Lerp(motionLift, lift, response);
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
                lean = Mathf.Clamp(slope * .7f, -28f, 28f);
            }
            else if (!front && rear) lean = 12f;
            else if (front && !rear) lean = -18f;
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
                // The pelvis is the rear attachment: let it settle below the
                // front support on an ascent instead of lifting both ends to
                // the height of the highest hoof. Limit this to a small shift
                // so uneven footing cannot collapse the body into the slope.
                if (front && rear)
                    lift += Mathf.Clamp((rearY - frontY) * .25f, -.22f, 0f);
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
                case "Goat_RightPunch":
                case "Goat_EagleRoundhouse":
                case "Goat_AnchorPlant":
                case "Goat_ChestRise":
                case "Goat_PanicRun":
                case "Goat_PlayDeadStart":
                case "Goat_PlayDeadHold":
                case "Goat_GetUp":
                case "Goat_BackRide":
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
                || clip == "GoatB_Duo_Performance" || clip == "Goat_RightPunch"
                || clip == "Goat_EagleRoundhouse" || clip == "Goat_AnchorPlant"
                || clip == "Goat_ChestRise" || clip == "Goat_PanicRun"
                || clip == "Goat_PlayDeadStart" || clip == "Goat_PlayDeadHold" || clip == "Goat_GetUp" || clip == "Goat_BackRide";
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
            bool sampleWalk = visual && (visual.CurrentClip == "Goat_Walk" || turningInPlace
                || visual.CurrentClip == "Goat_Idle" && walkBlend > .001f);
            if(sampleWalk)
            {
                PrepareWalkSampler();
                var animation=visual.GetComponentInChildren<Animation>();
                if(walkClip && walkAnimationRoot && animation && animation["Goat_Walk"]!=null)
                {
                    float walkTime=turningInPlace && visual.CurrentClip != "Goat_Walk"
                        ? Mathf.Repeat(turnWalkTime,walkClip.length)
                        : visual.WalkCycleTime * walkClip.length;
                    walkPhase=walkTime/walkClip.length*Mathf.PI*2f;
                    walkClip.SampleAnimation(walkAnimationRoot,walkTime);
                }
            }
            string actionClip = visual ? visual.CurrentClip : null;
            bool sampleAction = ActionStrength(actionClip) > 0f;
            bool toiletAction=actionClip=="Goat_Pee" || actionClip=="Goat_Poop";
            if(sampleAction && toiletAction)
            {
                PrepareWalkSampler();
                var animation=visual.GetComponentInChildren<Animation>();
                if(animation && animation[actionClip]!=null && walkAnimationRoot)
                    animation[actionClip].clip.SampleAnimation(walkAnimationRoot,
                        Mathf.Clamp(animation[actionClip].time,0f,animation[actionClip].length));
            }
            if (sampleAction)
            {
                sampledActionClip = actionClip;
                lastActionSampleFrame = Time.frameCount;
            }
            foreach (Part part in parts)
            {
                if (sampleWalk && part.IsLeg && part.Bone)
                {
                    if(walkBones!=null && walkBones.TryGetValue(part.Bone.name,out var authoredBone))
                    {
                        Quaternion delta=Quaternion.Inverse(part.WalkNeutralRotation)*authoredBone.localRotation;
                        part.WalkRelativeRotation=part.RestRelativeRotation*delta;
                        part.LastWalkSampleFrame=Time.frameCount;
                    }
                }
                if (sampleAction && part.Bone && (!part.IsElastic || HasElasticCurves(actionClip)))
                {
                    // The clip writes local bone rotations before this
                    // LateUpdate. Convert each authored change to the rest
                    // frame of its physical joint; some simulated torso
                    // links have different parents from the Blender rig.
                    Quaternion localDelta=toiletAction && walkBones!=null && walkBones.TryGetValue(part.Bone.name,out var authoredBone)
                        ? Quaternion.Inverse(part.WalkNeutralRotation)*authoredBone.localRotation
                        : Quaternion.Inverse(part.RestBoneLocalRotation)*part.Bone.localRotation;
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
                // With interpolation, Transform can still show the pre-teleport
                // pose. Reset the skeleton against the solver's current root.
                part.Body.position = rootBody.position + rootBody.rotation * Vector3.Scale(part.RestPosition, transform.lossyScale);
                part.Body.rotation = rootBody.rotation * part.RestRotation;
                part.Body.linearVelocity = Vector3.zero;
                part.Body.angularVelocity = Vector3.zero;
                part.Body.WakeUp();
            }
            lastRootPosition = rootBody.position;
            stableFacing = Vector3.ProjectOnPlane(rootBody.rotation * Vector3.forward, Vector3.up).normalized;
            lastSupportedAt = Time.time;
            walkBlend = 0f;
            walkPhase = 0f;
            previousMotionVelocity = rootBody.linearVelocity;
            motionWasSupported = false;
            motionPitch = motionRoll = motionLift = landingCompression = 0f;
            turnStepUntil = 0f;
            turnWalkTime = 0f;
            turnStartedAt = -100f;
            turnHeadLead = 0f;
            turnChestLead = 0f;
            turningInPlace = false;
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
