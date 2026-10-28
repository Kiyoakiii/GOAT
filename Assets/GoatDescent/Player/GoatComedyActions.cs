using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Feigning death and a physical, two-body back seat. The host owns both.</summary>
    [DefaultExecutionOrder(30)]
    [RequireComponent(typeof(GoatController), typeof(Rigidbody))]
    public sealed class GoatComedyActions : MonoBehaviour
    {
        private struct CollisionPair { public Collider a, b; public bool ignored; }
        private readonly List<CollisionPair> seatCollisions = new List<CollisionPair>();
        private GoatController motor;
        private Rigidbody body;
        private float passengerMassMultiplier => GoatMechanics.For(this).passengerMassMultiplier;
        private float unmountedMass;
        private bool passengerMassApplied;
        private GoatVisualController visual;
        private GoatComedyActions carrier, passenger;
        private ConfigurableJoint seat;
        private ConfigurableJoint hornGrip;
        private float rideStarted, hornInitialDistance;
        private Vector3 pretendPosition, pretendFacing;
        private float pretendStarted, recoveryUntil, cooldownUntil;
        private float lean, lastCarrierYaw, unsafeSince = -1f, restoreCollisionAt;
        private bool pretending, remoteBrace;
        private float drunkenPretenceUntil;
        private string networkCarrier = "", feedback = "";
        private float feedbackUntil;
        public bool PlayingDead => pretending;
        public bool Recovering => Time.time < recoveryUntil;
        public bool IsMounted => carrier || (!MountainAuthority.IsHost && !string.IsNullOrEmpty(networkCarrier));
        public bool HasPassenger => passenger;
        public bool Busy => PlayingDead || Recovering || IsMounted;
        public bool BlocksMovement => Busy;
        public float RideLean => lean;
        public Vector3 RideFacing => carrier ? carrier.transform.forward : transform.forward;
        public string CarrierId => carrier ? carrier.GetComponent<GoatLocalControl>()?.Label ?? carrier.name : networkCarrier;
        public string PoseClip => PlayingDead ? (Time.time - pretendStarted < 1f ? "Goat_PlayDeadStart" : "Goat_PlayDeadHold")
            : Recovering ? "Goat_GetUp" : IsMounted ? "Goat_BackRide" : null;

        private void Awake()
        { motor = GetComponent<GoatController>(); body = GetComponent<Rigidbody>(); visual = GetComponent<GoatVisualController>(); }
        private bool Available()
            => !Busy && !HasPassenger && !motor.IsPredatorCarried && GetComponent<GoatToiletAction>()?.Active!=true
                && GetComponent<RespawnController>()?.IsDead != true
                && GetComponent<GoatInteraction>()?.IsBusy != true
                && GetComponent<GoatDescentTricks>()?.Busy != true
                && GetComponent<GoatPunchBuff>()?.PerformingPair != true;
        private bool HasClip(string name)
        { var animation = GetComponentInChildren<Animation>(); return animation && animation[name] != null; }
        private void Message(string text) { feedback = text; feedbackUntil = Time.time + 3f; }

        public bool TryFeignDeath()
        {
            if (!MountainAuthority.IsHost) return false;
            if (PlayingDead) { BreakPretence(); return true; }
            if (!Available() || Time.time < cooldownUntil || !motor.Grounded || motor.Velocity.magnitude > 1.5f
                || GetComponent<GoatGroundDetector>().SlopeAngle > 45f || !HasClip("Goat_PlayDeadHold"))
            { Message("ДЛЯ ОБМАНА НУЖНО ОСТАНОВИТЬСЯ НА ОПОРЕ"); return false; }
            GetComponent<GoatDescentTricks>()?.ReleaseForComedy();
            GetComponent<GoatCliffGrip>()?.BreakGrip(1.1f);
            GetComponent<GoatGripController>()?.ResetGrip();
            GetComponent<GoatJumpController>()?.ResetInput();
            GetComponent<GoatSlopeBalance>()?.ResetBalance();
            motor.ClearInput();
            pretending = true; pretendStarted = Time.time; pretendPosition = body.position;
            pretendFacing = transform.forward;
            visual.PlayInteraction("Goat_PlayDeadStart", 1f, pretendFacing);
            SkyPredatorEpisode.Current?.ObservePretence(motor);
            Message(SkyPredatorEpisode.Current && SkyPredatorEpisode.Current.IsInspecting(motor)
                ? "НЕ ШЕВЕЛИСЬ! ОРЁЛ ПРОВЕРЯЕТ…" : "ЗАМРИ!");
            return true;
        }
        public bool TryDrunkenStumble(float duration)
        {
            if(!MountainAuthority.IsHost || !Available() || Time.time<cooldownUntil
                || !motor.Grounded || !HasClip("Goat_PlayDeadHold")) return false;
            GetComponent<GoatDescentTricks>()?.ReleaseForComedy();
            GetComponent<GoatCliffGrip>()?.BreakGrip(duration);
            GetComponent<GoatGripController>()?.ResetGrip();
            GetComponent<GoatJumpController>()?.ResetInput();
            motor.ClearInput();
            pretending=true; pretendStarted=Time.time; pretendPosition=body.position;
            pretendFacing=transform.forward; drunkenPretenceUntil=Time.time+duration;
            visual.PlayInteraction("Goat_PlayDeadStart",1f,pretendFacing);
            Message("ОЙ… КОПЫТА РАЗЪЕХАЛИСЬ!");
            return true;
        }
        public void BreakPretence()
        {
            if (!MountainAuthority.IsHost || !PlayingDead) return;
            if(drunkenPretenceUntil>0f && Time.time-pretendStarted<.75f) return;
            pretending = false; recoveryUntil = Time.time + 1.05f; cooldownUntil = Time.time + 3f;
            visual.PlayInteraction("Goat_GetUp", 1.05f, pretendFacing);
            bool caught = SkyPredatorEpisode.Current && SkyPredatorEpisode.Current.IsInspecting(motor);
            if(drunkenPretenceUntil<=0f) SkyPredatorEpisode.Current?.ExposePretence(motor);
            drunkenPretenceUntil=0f;
            Message(caught ? "ШЕВЕЛЬНУЛСЯ! ОБМАН РАСКРЫТ" : "ВСТАЛ — МОЖНО ИДТИ");
        }
        public void FooledEagle() => Message("ПОВЕРИЛ! ОРЁЛ УЛЕТАЕТ — МОЖНО ВСТАТЬ");
        public void SetRemoteInput(bool brace, Vector3 movement)
        {
            remoteBrace = brace;
            if (PlayingDead && movement.sqrMagnitude > .01f) BreakPretence();
        }

        public bool TryRide()
        {
            if (!MountainAuthority.IsHost) return false;
            if (IsMounted) { Dismount(false); return true; }
            if (!Available() || !motor.Grounded || Time.time < cooldownUntil || !HasClip("Goat_BackRide")) return false;
            GoatComedyActions nearest = null; float distance = 2.8f;
            foreach (var other in FindObjectsByType<GoatComedyActions>(FindObjectsSortMode.None))
            {
                if (other == this || !other.Available() || !other.motor.Grounded || other.motor.Velocity.magnitude > 2.5f) continue;
                float candidate = Vector3.Distance(body.position, other.body.position);
                if (candidate < distance) { distance = candidate; nearest = other; }
            }
            if (!nearest) { Message("ПОДОЙДИ К ОСТАНОВИВШЕМУСЯ НАПАРНИКУ"); return false; }
            var ownRig = GetComponent<GoatPhysicalBody>();
            var carrierRig = nearest.GetComponent<GoatPhysicalBody>();
            Quaternion seatRotation = Quaternion.LookRotation(nearest.transform.forward, Vector3.up);
            Vector3 seatPoint = carrierRig.HarnessPosition + Vector3.up * .85f
                - nearest.transform.forward * .16f;
            // Position the pelvis on the back, accounting for the rig's offset
            // from its gameplay capsule. A root position is not a sitting point.
            Vector3 entry = seatPoint - seatRotation * ownRig.RestPelvisOffset;
            // Do not mount through a wall or into an overhead rock.
            Vector3 path = entry - body.position;
            foreach (var hit in Physics.SphereCastAll(body.position + Vector3.up * .8f, .32f,
                path.normalized, path.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GoatController>() || hit.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                Message("НЕДОСТАТОЧНО МЕСТА ДЛЯ ПОСАДКИ"); return false;
            }
            carrier = nearest; nearest.passenger = this;
            IgnoreSeatCollisions(nearest);
            GetComponent<GoatInteraction>()?.CancelAll();
            GetComponent<GoatDescentTricks>()?.ReleaseForComedy();
            GetComponent<GoatCliffGrip>()?.BreakGrip(1.5f);
            GetComponent<GoatGripController>()?.ResetGrip();
            GetComponent<GoatJumpController>()?.ResetInput();
            motor.ClearInput();
            transform.SetPositionAndRotation(entry, seatRotation);
            body.position = entry; body.rotation = seatRotation;
            Physics.SyncTransforms();
            body.linearVelocity = nearest.body.linearVelocity; body.angularVelocity = Vector3.zero;
            ownRig.ResetPose();
            // Keep the articulated parts unchanged so their muscles and balance
            // retain the same response, but reduce the main carried mass.
            unmountedMass = body.mass;
            passengerMassApplied = true;
            body.mass = unmountedMass * passengerMassMultiplier;
            // Attach the actual seated pelvis, not the capsule beneath it.
            // Locked translation prevents gravity and horn tension from pulling
            // the passenger down beside the carrier; the articulated rig can bend.
            seat = ownRig.HarnessBody.gameObject.AddComponent<ConfigurableJoint>();
            seat.autoConfigureConnectedAnchor = false;
            seat.connectedBody = carrierRig.HarnessBody;
            seat.anchor = Vector3.zero;
            seat.connectedAnchor = seat.connectedBody.transform.InverseTransformPoint(seatPoint);
            seat.xMotion = seat.yMotion = seat.zMotion = ConfigurableJointMotion.Locked;
            seat.angularXMotion = seat.angularYMotion = seat.angularZMotion = ConfigurableJointMotion.Free;
            seat.projectionMode = JointProjectionMode.PositionAndRotation;
            seat.projectionDistance = .035f;
            seat.projectionAngle = 10f;
            seat.breakForce = Mathf.Infinity; seat.breakTorque = Mathf.Infinity; seat.enableCollision = false;
            seat.enablePreprocessing = false;
            lastCarrierYaw = nearest.transform.eulerAngles.y; lean = 0; unsafeSince = -1;
            rideStarted = Time.time;
            visual.PlayInteraction("Goat_BackRide", 1.2f, nearest.transform.forward);
            Message("ПАССАЖИР! ДЕРЖИ РАВНОВЕСИЕ!");
            nearest.Message("НАПАРНИК НА СПИНЕ — ОСТОРОЖНЕЕ НА ПОВОРОТАХ!");
            return true;
        }
        private void IgnoreSeatCollisions(GoatComedyActions other)
        {
            RestoreSeatCollisions();
            foreach (var a in GetComponent<GoatPhysicalBody>().GetRideColliders())
                foreach (var b in other.GetComponent<GoatPhysicalBody>().GetRideColliders())
                {
                    seatCollisions.Add(new CollisionPair { a=a, b=b, ignored=Physics.GetIgnoreCollision(a,b) });
                    Physics.IgnoreCollision(a,b,true);
                }
        }
        private void RestoreSeatCollisions()
        {
            foreach (var pair in seatCollisions) if (pair.a && pair.b) Physics.IgnoreCollision(pair.a,pair.b,pair.ignored);
            seatCollisions.Clear(); restoreCollisionAt = 0;
        }
        public void Dismount(bool jump)
        {
            if (!MountainAuthority.IsHost || !carrier) return;
            var previous = carrier; carrier = null; previous.passenger = null;
            RestorePassengerMass();
            if (hornGrip)
            {
                hornGrip.xMotion=hornGrip.yMotion=hornGrip.zMotion=ConfigurableJointMotion.Free;
                hornGrip.connectedBody=null; Destroy(hornGrip); hornGrip=null;
            }
            if (seat)
            {
                seat.xMotion=seat.yMotion=seat.zMotion=ConfigurableJointMotion.Free;
                seat.connectedBody=null; Destroy(seat); seat=null;
            }
            float side = Mathf.Abs(lean) > .1f ? Mathf.Sign(lean) : 1f;
            body.linearVelocity = previous.body.linearVelocity + previous.transform.right * side * 2.4f
                + Vector3.up * (jump ? 5f : 1.2f);
            body.WakeUp(); GetComponent<GoatJumpController>()?.ResetJumpState();
            visual.EndComedyPose();
            if (jump) GetComponent<GoatPhysicalBody>()?.BeginJump(Vector3.up * 5f);
            cooldownUntil = Time.time + 1f; restoreCollisionAt = Time.time + .6f;
            lean = 0; unsafeSince = -1; Message(jump ? "СПРЫГНУЛ!" : "СЛЕЗ СО СПИНЫ");
        }
        private void OnJointBreak(float force) { if (IsMounted) Dismount(false); }

        private void Update()
        {
            if (GetComponent<RespawnController>()?.IsDead == true || motor.IsPredatorCarried) { Clear(); return; }
            if (!MountainAuthority.IsHost) return;
            if(PlayingDead && drunkenPretenceUntil>0f && Time.time>=drunkenPretenceUntil) BreakPretence();
            if (restoreCollisionAt > 0 && Time.time >= restoreCollisionAt) RestoreSeatCollisions();
            bool input = !motor.NetworkOwnedByRemote && GoatLocalControl.AllowsInput(this);
            if (input)
            {
                if (Input.GetKeyDown(KeyCode.J)) TryFeignDeath();
                if (Input.GetKeyDown(KeyCode.K)) TryRide();
                if (PlayingDead && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S)
                    || Input.GetKey(KeyCode.D) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))) BreakPretence();
            }
            if (PlayingDead && Time.time - pretendStarted > .45f
                && (body.linearVelocity.magnitude > 1.45f || Vector3.Distance(pretendPosition,body.position) > .65f || !motor.Grounded))
                BreakPretence();
            string clip = PoseClip;
            if (clip != null)
            {
                Vector3 facing = IsMounted ? RideFacing : pretendFacing;
                visual.FaceInteraction(facing,.25f);
                if (visual.CurrentClip != clip) visual.PlayInteraction(clip,clip=="Goat_GetUp" ? Mathf.Max(.05f,recoveryUntil-Time.time) : 1.25f,facing);
            }
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost || !carrier) return;
            if (!seat || carrier.GetComponent<RespawnController>()?.IsDead == true || carrier.motor.IsPredatorCarried
                || Vector3.Distance(body.position,carrier.body.position) > 3.5f)
            { Dismount(false); return; }
            float turn = Mathf.DeltaAngle(lastCarrierYaw,carrier.transform.eulerAngles.y) * Mathf.Deg2Rad / Time.fixedDeltaTime;
            lastCarrierYaw = carrier.transform.eulerAngles.y;
            float speed = Vector3.ProjectOnPlane(carrier.body.linearVelocity,Vector3.up).magnitude;
            bool local = !motor.NetworkOwnedByRemote && GoatLocalControl.AllowsInput(this);
            bool brace = motor.NetworkOwnedByRemote ? remoteBrace : local && Input.GetKey(KeyCode.F);
            if (Time.time-rideStarted > .65f && !hornGrip) AttachRidingHorns();
            if (hornGrip)
            {
                float settle = Mathf.Clamp01((Time.time-rideStarted-.65f)/1.2f);
                hornGrip.linearLimit = new SoftJointLimit { limit=Mathf.Lerp(hornInitialDistance,.18f,settle) };
                hornGrip.linearLimitSpring = new SoftJointLimitSpring { spring=brace ? 1250 : 700, damper=brace ? 115 : 85 };
            }
            float counter = motor.NetworkOwnedByRemote || local ? motor.HorizontalInput : 0;
            lean += (-turn * Mathf.Min(speed,6f) * .09f + counter * 1.25f - lean * (brace ? 1.9f : .5f)) * Time.fixedDeltaTime;
            lean = Mathf.Clamp(lean,-1.2f,1.2f);
            body.AddForce(carrier.transform.right * (-turn * speed * .22f),ForceMode.Acceleration);
            if (Mathf.Abs(lean) > .98f)
            {
                if (unsafeSince < 0) unsafeSince = Time.time;
                if (Time.time-unsafeSince > .4f) { Dismount(false); Message("НЕ УДЕРЖАЛСЯ НА ПОВОРОТЕ!"); }
            }
            else unsafeSince=-1;
        }
        private void AttachRidingHorns()
        {
            var own = GetComponent<GoatPhysicalBody>(); var other = carrier.GetComponent<GoatPhysicalBody>();
            if (!own.TryGetHornConnection(carrier.transform.forward,out var head,out var anchor)
                || !other.TryGetHornConnection(carrier.transform.forward,out var otherHead,out var otherAnchor)) return;
            hornGrip=head.gameObject.AddComponent<ConfigurableJoint>();
            hornGrip.autoConfigureConnectedAnchor=false; hornGrip.connectedBody=otherHead;
            hornGrip.anchor=anchor; hornGrip.connectedAnchor=otherAnchor;
            hornGrip.xMotion=hornGrip.yMotion=hornGrip.zMotion=ConfigurableJointMotion.Limited;
            hornGrip.angularXMotion=hornGrip.angularYMotion=hornGrip.angularZMotion=ConfigurableJointMotion.Free;
            hornInitialDistance=Vector3.Distance(head.transform.TransformPoint(anchor),otherHead.transform.TransformPoint(otherAnchor));
            hornGrip.linearLimit=new SoftJointLimit {limit=hornInitialDistance};
            hornGrip.linearLimitSpring=new SoftJointLimitSpring {spring=700,damper=85};
            hornGrip.breakForce=Mathf.Infinity; hornGrip.breakTorque=Mathf.Infinity;
            hornGrip.enableCollision=false; hornGrip.enablePreprocessing=false;
        }
        public void ApplyNetwork(bool deadPose, bool recovering, string seatOwner, float seatLean)
        {
            if (MountainAuthority.IsHost) return;
            if (deadPose && !pretending) pretendStarted=Time.time-1f;
            pretending=deadPose; recoveryUntil=recovering ? Time.time+.3f : 0;
            networkCarrier=seatOwner ?? ""; lean=seatLean;
        }
        public void Clear()
        {
            if (passenger) passenger.Dismount(false);
            if (carrier) Dismount(false);
            RestorePassengerMass();
            pretending=false; recoveryUntil=0; networkCarrier=""; remoteBrace=false; drunkenPretenceUntil=0f;
            if (visual) visual.EndComedyPose();
            RestoreSeatCollisions();
        }
        private void OnDisable() => Clear();
        private void RestorePassengerMass()
        {
            if (!passengerMassApplied) return;
            if (body) body.mass = unmountedMass;
            passengerMassApplied = false;
        }
        private void OnGUI()
        {
            if(GoatSkinMenu.IsOpen) return;
            if (!GoatLocalControl.AllowsInput(this)) return;
            if (IsMounted) GUI.Box(new Rect(25,264,600,30),$"ПАССАЖИР · наклон {lean*42:F0}°");
            else if (Time.time < feedbackUntil) GUI.Box(new Rect(25,264,760,30),feedback);
        }
    }
}
