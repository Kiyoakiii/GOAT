using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(GoatController))]
    public sealed class GoatPunchBuff : MonoBehaviour
    {
        public const float Duration = 10f;
        public const float GripBonus = 1.6f;
        private GoatController motor;
        private GoatVisualController visual;
        private RespawnController life;
        private float requestedUntil, strikeAt = -1f, animationAt = -1f, endsAt, poseUntil;
        private float lastGroundedAt = -100f;
        private GoatPunchBuff partner;
        private bool leader, effectActive;
        private GoatBuffEffect effect;
        private string feedback = "";
        private float feedbackUntil;
        public bool Active => Remaining > 0f;
        public float Remaining => Mathf.Max(0f, endsAt - Time.time);
        public bool PerformingPair => partner && Time.time < poseUntil;
        public Vector3 PairFacing { get; private set; }
        public float GripMultiplier => Active ? GripBonus : 1f;
        private bool HasFooting => motor && (motor.Grounded
            || (Time.time-lastGroundedAt < .2f && Mathf.Abs(motor.Velocity.y)<1.5f));
        private void Awake()
        { motor = GetComponent<GoatController>(); visual = GetComponent<GoatVisualController>(); }
        private void Message(string text)
        { feedback = text; feedbackUntil = Time.time + 2.5f; }
        private string UnavailableReason()
        {
            life ??= GetComponent<RespawnController>();
            var animation = GetComponentInChildren<Animation>();
            if (!motor || (life && life.IsDead)) return "нужно дождаться возрождения";
            if (motor.IsPredatorCarried) return "козла несут";
            if (PerformingPair) return "жест уже выполняется";
            if (Active) return $"бонус уже действует ещё {Remaining:F1} с";
            if (GetComponent<GoatInteraction>()?.IsBusy == true) return "сначала отпусти хват F";
            if (GetComponent<GoatComedyActions>()?.Busy == true) return "сначала встань или слезь со спины";
            if (GetComponent<GoatDescentTricks>()?.Busy == true) return "дождись окончания другого действия";
            if (!HasFooting)
                return "нужна опора под ногами";
            if (Vector3.ProjectOnPlane(motor.Velocity,Vector3.up).magnitude > 3.5f) return "остановись рядом с напарником";
            if (!animation || animation["Goat_RightPunch"] == null) return "не загружена анимация копыта";
            return null;
        }
        private bool Ready() => UnavailableReason() == null;
        private bool CanPair(GoatPunchBuff other)
        {
            Vector3 delta = other.transform.position - transform.position;
            if (delta.magnitude > 3f || delta.magnitude < .6f || Mathf.Abs(delta.y) > 1.1f) return false;
            Vector3 start = transform.position + Vector3.up * 1.1f;
            foreach (var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if (!hit.collider.GetComponentInParent<GoatController>()
                    && !hit.collider.GetComponentInParent<GoatPhysicsPart>()) return false;
            return true;
        }
        public bool TryPunch()
        {
            if (!MountainAuthority.IsHost) return false;
            string reason=UnavailableReason();
            if(reason!=null) { Message("ДАЙ КОПЫТО: "+reason); return false; }
            GoatPunchBuff nearest = null;
            GoatPunchBuff nearby = null;
            float distance = 3f, nearbyDistance=4f;
            foreach (var other in FindObjectsByType<GoatPunchBuff>(FindObjectsSortMode.None))
            {
                if (other == this) continue;
                float candidate = Vector3.Distance(transform.position,other.transform.position);
                if(candidate<nearbyDistance) { nearby=other; nearbyDistance=candidate; }
                if (!other.Ready() || !CanPair(other)) continue;
                if (candidate <= distance) { nearest=other; distance=candidate; }
            }
            if (!nearest)
            {
                string otherReason=nearby ? nearby.UnavailableReason() : null;
                Message(otherReason!=null ? "НАПАРНИК: "+otherReason
                    : nearbyDistance>3f ? "ДАЙ КОПЫТО: подойди ближе к напарнику"
                    : "ДАЙ КОПЫТО: встаньте рядом без скалы между вами");
                return false;
            }
            requestedUntil = Time.time + 5f;
            if (nearest.requestedUntil < Time.time)
            { Message("Теперь напарник тоже должен нажать B"); nearest.Message("Напарник предлагает дать копыто — нажми B"); return true; }
            partner=nearest; nearest.partner=this; leader=true; nearest.leader=false;
            requestedUntil=nearest.requestedUntil=0f;
            Vector3 toward=Vector3.ProjectOnPlane(nearest.transform.position-transform.position,Vector3.up).normalized;
            // Both right hooves meet slightly across the centre line.
            PairFacing=Quaternion.Euler(0,-15f,0)*toward;
            nearest.PairFacing=Quaternion.Euler(0,-15f,0)*-toward;
            animationAt=Time.time+1.1f; nearest.animationAt=-1f;
            poseUntil=nearest.poseUntil=Time.time+2.35f;
            strikeAt=Time.time+1.6f; nearest.strikeAt=-1f;
            motor.ClearInput(); nearest.motor.ClearInput();
            visual.FaceInteraction(PairFacing,1.1f);
            nearest.visual.FaceInteraction(nearest.PairFacing,1.1f);
            return true;
        }
        public void ApplyNetworkRemaining(float seconds)
        { if (!MountainAuthority.IsHost) endsAt=Time.time+Mathf.Clamp(seconds,0f,Duration); }
        private void CancelPair()
        {
            var other=partner; partner=null; strikeAt=animationAt=-1f; poseUntil=0; leader=false;
            if (other && other.partner==this)
            { other.partner=null; other.strikeAt=other.animationAt=-1f; other.poseUntil=0; other.leader=false; }
        }
        public void Clear()
        {
            CancelPair(); requestedUntil=0; endsAt=0;
            if(effect) effect.StopEffect(); effectActive=false;
        }
        private void Update()
        {
            if(motor && motor.Grounded) lastGroundedAt=Time.time;
            life ??= GetComponent<RespawnController>();
            if(life && life.IsDead) { Clear(); return; }
            if(MountainAuthority.IsHost)
            {
                if(!motor.NetworkOwnedByRemote && GoatLocalControl.AllowsInput(this) && Input.GetKeyDown(KeyCode.B)) TryPunch();
                if(partner && leader && strikeAt>=0)
                {
                    if(!HasFooting || !partner.HasFooting || motor.IsPredatorCarried || partner.motor.IsPredatorCarried
                        || GetComponent<GoatComedyActions>()?.Busy==true || partner.GetComponent<GoatComedyActions>()?.Busy==true
                        || GetComponent<GoatInteraction>()?.IsBusy==true || partner.GetComponent<GoatInteraction>()?.IsBusy==true
                        || !CanPair(partner)) { CancelPair(); Message("Копытами не встретились — попробуйте снова"); }
                    else if(animationAt>=0 && Time.time>=animationAt)
                    {
                        animationAt=-1;
                        visual.PlayInteraction("Goat_RightPunch",1.25f,PairFacing);
                        partner.visual.PlayInteraction("Goat_RightPunch",1.25f,partner.PairFacing);
                    }
                    else if(Time.time>=strikeAt)
                    {
                        strikeAt=-1; endsAt=partner.endsAt=Time.time+Duration;
                        Message("СРАБОТАЛИСЬ! Теперь хват рогами крепче"); partner.Message("СРАБОТАЛИСЬ! Теперь хват рогами крепче");
                    }
                }
                if(partner && Time.time>=poseUntil) CancelPair();
            }
            if(Active && !effectActive)
            {
                effect ??= GetComponent<GoatBuffEffect>(); if(!effect) effect=gameObject.AddComponent<GoatBuffEffect>();
                effect.StartEffect(); effectActive=true;
            }
            else if(!Active && effectActive) { effect.StopEffect(); effectActive=false; }
            if(effectActive) effect.SetRemaining(Remaining);
        }
        private void OnDisable()=>Clear();
        private void OnGUI()
        {
            if(GoatSkinMenu.IsOpen) return;
            if(!GoatLocalControl.AllowsInput(this)) return;
            if(Active) GUI.Box(new Rect(25,139,540,27),$"СРАБОТАЛИСЬ: совместный хват рогами крепче · {Remaining:F1} с");
            else if(Time.time<feedbackUntil) GUI.Box(new Rect(25,139,600,27),feedback);
        }
    }
}
