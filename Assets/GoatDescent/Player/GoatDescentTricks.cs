using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(GoatController), typeof(Rigidbody))]
    public sealed class GoatDescentTricks : MonoBehaviour
    {
        private GoatController motor;
        private GoatVisualController visual;
        private Rigidbody body;
        private float kickAt = -1f, kickReadyAt, birdSafeUntil;
        private float actionUntil;
        private float spinUntil, spinStarted;
        private Vector3 spinFacing;
        private float anchorAt = -1f, anchorUntil, anchorReadyAt;
        private float anchorDuration = 12f, ropeLength = 3.5f;
        private SpringJoint tether;
        private Collider anchorSurface;
        private Vector3 anchorLocal, anchorWorld;
        private LineRenderer rope;
        private LineRenderer ropeBraid,anchorEye;
        private Material anchorMetal;
        private Vector3 anchorNormal=Vector3.up;
        private GameObject peg;
        private float madnessUntil, madnessStarted;
        private Vector3 madnessDirection;
        private string feedback;
        private float feedbackUntil;
        public bool BirdsAvoid => Time.time < birdSafeUntil;
        public float BirdSafeRemaining => Mathf.Max(0f, birdSafeUntil - Time.time);
        public bool Anchored => Time.time < anchorUntil;
        public float AnchorRemaining => Mathf.Max(0f, anchorUntil - Time.time);
        public float RopeLength => ropeLength;
        public Vector3 AnchorPosition => anchorWorld;
        public bool MadnessActive => Time.time < madnessUntil;
        public float MadnessRemaining => Mathf.Max(0f, madnessUntil - Time.time);
        public Vector3 MadnessFacing => -madnessDirection;
        public Vector3 MadnessDirection => madnessDirection;
        public bool Busy => MadnessActive || Time.time < actionUntil || kickAt >= 0f || anchorAt >= 0f;
        public bool Spinning => Time.time < spinUntil;
        public Vector3 SpinFacing => Quaternion.AngleAxis(Mathf.Clamp01((Time.time-spinStarted)/1.21f)*360f,Vector3.up)*spinFacing;

        private void Awake()
        {
            motor = GetComponent<GoatController>();
            body = GetComponent<Rigidbody>();
            visual = GetComponent<GoatVisualController>();
        }
        private bool Ready(string clip)
        {
            var animation = GetComponentInChildren<Animation>();
            return !Busy && GetComponent<GoatComedyActions>()?.Busy != true
                && GetComponent<GoatPunchBuff>()?.PerformingPair != true && GetComponent<RespawnController>()?.IsDead != true
                && GetComponent<GoatInteraction>()?.IsBusy != true && animation && animation[clip] != null;
        }
        private void Message(string text) { feedback = text; feedbackUntil = Time.time + 2f; }
        public bool TryKick()
        {
            if (!MountainAuthority.IsHost || Time.time < kickReadyAt || !Ready("Goat_EagleRoundhouse")) return false;
            visual.PlayInteraction("Goat_EagleRoundhouse", 1.21f, visual.Facing);
            spinFacing = Vector3.ProjectOnPlane(visual.Facing,Vector3.up).normalized;
            spinStarted = Time.time; spinUntil = Time.time+1.21f;
            actionUntil = spinUntil;
            kickAt = Time.time + .54f;
            kickReadyAt = Time.time + 2f;
            return true;
        }
        public bool TryAnchor(float duration = 12f, float length = 3.5f)
        {
            if (!MountainAuthority.IsHost) return false;
            if (Anchored) { ReleaseAnchor(); Message("СТРАХОВКА СНЯТА"); return true; }
            if (Time.time < anchorReadyAt || motor.IsPredatorCarried || !Ready("Goat_AnchorPlant")) return false;
            var ground = GetComponent<GoatGroundDetector>();
            RaycastHit hit = ground.GroundHit;
            if (!ground.IsGrounded)
            {
                bool found = false;
                foreach (var candidate in Physics.RaycastAll(body.position + Vector3.up * .6f,
                    visual.Facing, 2.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (candidate.collider.GetComponentInParent<GoatController>()
                        || candidate.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                    if (!found || candidate.distance < hit.distance) { hit = candidate; found = true; }
                }
                if (!found) { Message("НУЖНА СКАЛА ИЛИ ОПОРА РЯДОМ"); return false; }
            }
            if (!hit.collider || hit.collider.GetComponentInParent<GoatController>()) return false;
            anchorDuration = duration; ropeLength = length;
            anchorSurface = hit.collider;
            anchorLocal = anchorSurface.transform.InverseTransformPoint(hit.point + hit.normal * .08f);
            anchorWorld = anchorSurface.transform.TransformPoint(anchorLocal);
            anchorNormal=hit.normal;
            visual.PlayInteraction("Goat_AnchorPlant", 1.04f, visual.Facing);
            anchorAt = Time.time + .54f;
            actionUntil = Time.time + 1.04f;
            return true;
        }
        private void AttachAnchor()
        {
            anchorAt = -1f;
            if (!anchorSurface) return;
            anchorUntil = Time.time + anchorDuration;
            anchorReadyAt = Time.time + 16f;
            tether = gameObject.AddComponent<SpringJoint>();
            tether.autoConfigureConnectedAnchor = false;
            tether.anchor = body.transform.InverseTransformPoint(GetComponent<GoatPhysicalBody>().HarnessPosition);
            tether.connectedBody = anchorSurface.attachedRigidbody;
            tether.connectedAnchor = tether.connectedBody
                ? tether.connectedBody.transform.InverseTransformPoint(anchorWorld) : anchorWorld;
            tether.maxDistance = ropeLength;
            tether.spring = 8500f;
            tether.damper = 450f;
            tether.tolerance = .05f;
            Message($"СТРАХОВКА: {anchorDuration:F0} СЕКУНД · {ropeLength:F1} М · H — СНЯТЬ");
        }
        private void ReleaseAnchor()
        {
            anchorAt = -1f; anchorUntil = 0f;
            if (tether) { tether.connectedBody = null; tether.spring = tether.damper = 0f; Destroy(tether); tether = null; }
            if (peg) Destroy(peg);
            if (rope) { Destroy(rope.sharedMaterial); Destroy(rope.gameObject); }
            if(ropeBraid) Destroy(ropeBraid.gameObject);
            if(anchorEye) Destroy(anchorEye.gameObject);
            if(anchorMetal) Destroy(anchorMetal);
            ropeBraid=anchorEye=null; anchorMetal=null;
            peg = null; rope = null;
        }
        public void ReleaseForComedy() => ReleaseAnchor();
        public bool StartMadness(Vector3 awayFromChest)
        {
            if (!MountainAuthority.IsHost || !motor.Grounded || motor.IsPredatorCarried || !Ready("Goat_ChestRise")) return false;
            ReleaseAnchor();
            kickAt = -1f;
            motor.ClearInput();
            madnessDirection = Vector3.ProjectOnPlane(awayFromChest, Vector3.up).normalized;
            if (madnessDirection.sqrMagnitude < .1f) madnessDirection = -visual.Facing;
            madnessStarted = Time.time;
            madnessUntil = Time.time + 4.2f;
            visual.PlayInteraction("Goat_ChestRise", 1f, MadnessFacing);
            Message("ААААА! ЧТО БЫЛО В СУНДУКЕ?!");
            return true;
        }
        public void ApplyNetwork(float birds, float anchor, Vector3 point, float madness, Vector3 direction, float length = 3.5f)
        {
            if (MountainAuthority.IsHost) return;
            birdSafeUntil = Time.time + Mathf.Max(0f, birds);
            anchorUntil = Time.time + Mathf.Max(0f, anchor);
            anchorWorld = point;
            ropeLength = length;
            madnessUntil = Time.time + Mathf.Max(0f, madness);
            madnessDirection = direction;
        }
        private void Update()
        {
            if (GetComponent<RespawnController>()?.IsDead == true) { Clear(); return; }
            if (MountainAuthority.IsHost)
            {
                if (Spinning) visual.FaceInteraction(SpinFacing,.2f);
                if (!motor.NetworkOwnedByRemote && GoatLocalControl.AllowsInput(this))
                {
                    if (Input.GetKeyDown(KeyCode.G)) TryKick();
                    if (Input.GetKeyDown(KeyCode.H)) TryAnchor();
                }
                if (kickAt >= 0f && Time.time >= kickAt)
                {
                    kickAt = -1f;
                    if (SkyPredatorEpisode.Current && SkyPredatorEpisode.Current.RepelNearby(motor, 6f))
                    {
                        birdSafeUntil = Time.time + 45f;
                        kickReadyAt = birdSafeUntil;
                        ComicImpactRing.Create(body.position + Vector3.up, new Color(1f,.6f,.1f), 1f);
                        Message("ОРЛЫ ЗАПОМНИЛИ: 45 СЕКУНД ПОКОЯ");
                    }
                    else Message("ОРЛА НЕТ РЯДОМ — ПОДПУСТИ БЛИЖЕ");
                }
                if (anchorAt >= 0f && Time.time >= anchorAt) AttachAnchor();
                if (tether && (!Anchored || !anchorSurface)) ReleaseAnchor();
                if (MadnessActive)
                {
                    visual.FaceInteraction(MadnessFacing, .2f);
                    if (Time.time - madnessStarted > .95f && visual.CurrentClip != "Goat_PanicRun")
                        visual.PlayInteraction("Goat_PanicRun", MadnessRemaining, MadnessFacing);
                }
            }
            if (Anchored) DrawRope();
            else if (rope) ReleaseAnchor();
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            if (Anchored && anchorSurface)
            {
                anchorWorld = anchorSurface.transform.TransformPoint(anchorLocal);
                if (tether && !tether.connectedBody) tether.connectedAnchor = anchorWorld;
            }
            if (!MadnessActive || Time.time - madnessStarted < .85f || motor.IsPredatorCarried) return;
            var ground = GetComponent<GoatGroundDetector>();
            if (!ground.IsGrounded) return;
            Vector3 direction = Vector3.ProjectOnPlane(madnessDirection, ground.GroundNormal).normalized;
            Vector3 tangent = Vector3.ProjectOnPlane(body.linearVelocity, ground.GroundNormal);
            body.linearVelocity += Vector3.ClampMagnitude(direction * 4.5f - tangent, 16f * Time.fixedDeltaTime);
        }
        private void DrawRope()
        {
            if (!rope)
            {
                rope = new GameObject("Safety rope").AddComponent<LineRenderer>();
                rope.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                rope.positionCount = 49; rope.widthMultiplier = .04f;
                rope.numCapVertices=6; rope.numCornerVertices=4;
                rope.startColor = rope.endColor = new Color(1f,.55f,.08f);
                ropeBraid=GoatClimbingEquipment.Line(rope.transform,"Braided rope highlight",rope.sharedMaterial,.009f,new Color(1f,.95f,.66f),97);
                peg=new GameObject("Steel rock piton");
                anchorMetal=new Material(Shader.Find("Standard")) { color=new Color(.65f,.73f,.8f) };
                anchorMetal.SetFloat("_Metallic",.85f); anchorMetal.SetFloat("_Glossiness",.5f);
                GoatClimbingEquipment.Segment(peg.transform,"Piton shaft",new Vector3(0,0,-.12f),new Vector3(0,0,.1f),.035f,anchorMetal);
                GoatClimbingEquipment.Segment(peg.transform,"Piton crossbar",new Vector3(-.12f,0,.04f),new Vector3(.12f,0,.04f),.035f,anchorMetal);
                anchorEye=GoatClimbingEquipment.Line(peg.transform,"Piton rope eye",rope.sharedMaterial,.03f,new Color(.8f,.86f,.9f),25);
            }
            peg.transform.position = anchorWorld;
            Vector3 normal=MountainAuthority.IsHost ? anchorNormal : (GetComponent<GoatPhysicalBody>().HarnessPosition-anchorWorld).normalized;
            if(normal.sqrMagnitude<.1f) normal=Vector3.up;
            peg.transform.rotation=Quaternion.LookRotation(normal);
            GoatClimbingEquipment.Loop(anchorEye,anchorWorld+normal*.11f,peg.transform.right,peg.transform.up,.065f,.09f);
            var inventory=GetComponent<GoatDescentInventory>();
            Vector3 end=inventory && inventory.SafetyClipPosition!=Vector3.zero ? inventory.SafetyClipPosition : GetComponent<GoatPhysicalBody>().HarnessPosition;
            Vector3 start=anchorWorld+normal*.11f;
            float slack = Mathf.Clamp01(1f - Vector3.Distance(anchorWorld,end)/Mathf.Max(.1f,ropeLength))*.45f;
            for (int i=0;i<49;i++)
            {
                float t=i/48f;
                rope.SetPosition(i,Vector3.Lerp(start,end,t) + Vector3.down * Mathf.Sin(t*Mathf.PI)*slack);
            }
            Vector3 direction=(end-start).normalized;
            Vector3 side=Vector3.Cross(direction,Vector3.up).normalized;
            if(side.sqrMagnitude<.1f) side=Vector3.right;
            Vector3 around=Vector3.Cross(direction,side).normalized;
            for(int i=0;i<97;i++) { float t=i/96f, angle=t*48*Mathf.PI;
                ropeBraid.SetPosition(i,Vector3.Lerp(start,end,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*slack
                    +(side*Mathf.Cos(angle)+around*Mathf.Sin(angle))*.022f); }
        }
        public void Clear()
        {
            ReleaseAnchor(); kickAt=-1f; spinUntil=0f; actionUntil=0f; birdSafeUntil=0f; madnessUntil=0f;
        }
        private void OnDisable() => Clear();
        private void OnGUI()
        {
            if(GoatSkinMenu.IsOpen) return;
            if (!GoatLocalControl.AllowsInput(this)) return;
            string text = BirdsAvoid ? $"Орлы обходят {BirdSafeRemaining:F0} с" : "";
            if (Anchored) text += (text.Length > 0 ? " · " : "") + $"Верёвка {AnchorRemaining:F1} с";
            if (text.Length > 0) GUI.Label(new Rect(25,172,850,24),text);
            if (Time.time < feedbackUntil) GUI.Box(new Rect(25,203,420,28),feedback);
        }
    }
}
