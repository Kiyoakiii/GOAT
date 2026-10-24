using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    [System.Serializable]
    public struct GoatHerdState
    {
        public bool isolated, grounding;
        public float charge, aloneSeconds, bleatAge, shockRemaining;
        public int bleatSerial, shockSerial;
        public byte bleatKind;
        public Vector3 groundPoint;
    }

    [DisallowMultipleComponent, DefaultExecutionOrder(30)]
    public sealed class GoatHerdBehaviour : MonoBehaviour
    {
        private static readonly HashSet<GoatHerdBehaviour> herd = new HashSet<GoatHerdBehaviour>();
        private GoatController motor;
        private GoatPhysicalBody physical;
        private GoatHerdSettings settings;
        private float separatedAt = -1f, nextDistanceScan, nextBleatAt, inhaleAt, loudAt;
        private float bleatAt = -100f, shockUntil;
        private int bleatSerial, shockSerial;
        private byte bleatKind;
        private Transform mouth, head;
        private Material mouthMaterial, electricMaterial;
        private AudioSource voice;
        private AudioClip quietClip, inhaleClip, loudClip, shockClip;
        private readonly LineRenderer[] arcs = new LineRenderer[5];
        private readonly Vector3[] arcPoints = new Vector3[7];
        public bool Isolated { get; private set; }
        public bool Grounding { get; private set; }
        public float Charge { get; private set; }
        private Vector3 groundPoint;
        public float ShockPose => Time.time < shockUntil ? Mathf.Sin(Mathf.Clamp01((shockUntil-Time.time) / .8f) * Mathf.PI) : 0;
        public float AloneSeconds => separatedAt < 0 ? 0 : Mathf.Max(0, Time.time - separatedAt);
        public float BleatPose
        {
            get
            {
                if (GetComponent<GoatWaterDrinking>()?.Active == true) return 0;
                float age = Time.time - bleatAt;
                float duration = bleatKind == 3 ? 1.8f : bleatKind == 2 ? settings.inhaleSeconds : .65f;
                return age >= 0 && age < duration ? Mathf.Sin(Mathf.PI * age / duration) * (bleatKind == 3 ? 1f : .3f) : 0;
            }
        }
        private bool Alive => motor && GetComponent<RespawnController>()?.IsDead != true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHerd() => herd.Clear();
        private void Awake()
        {
            settings = GoatHerdSettings.Get(); motor = GetComponent<GoatController>();
            physical = GetComponent<GoatPhysicalBody>();
            quietClip = Resources.Load<AudioClip>("GoatHerdAudio/QuietBleat");
            inhaleClip = Resources.Load<AudioClip>("GoatHerdAudio/Inhale");
            loudClip = Resources.Load<AudioClip>("GoatHerdAudio/LoudBleat");
            shockClip = Resources.Load<AudioClip>("GoatHerdAudio/Discharge");
        }
        private void OnEnable() { if (!settings || !motor) Awake(); herd.Add(this); }
        private void OnDisable() { herd.Remove(this); if (voice) voice.Stop(); }

        private void ScanDistance()
        {
            float nearest = float.PositiveInfinity;
            foreach (var other in herd)
                if (other && other != this && other.Alive)
                    nearest = Mathf.Min(nearest, Vector3.Distance(transform.position, other.transform.position));
            if (!Alive || float.IsInfinity(nearest) || nearest < Mathf.Min(settings.reunionDistance, settings.separationDistance))
            { separatedAt = -1; Isolated = false; nextBleatAt = inhaleAt = loudAt = 0; return; }
            if (separatedAt < 0 && nearest > settings.separationDistance) separatedAt = Time.time;
            bool lonely = separatedAt >= 0 && AloneSeconds >= settings.graceSeconds;
            if (lonely && !Isolated) { nextBleatAt = Time.time; inhaleAt = loudAt = 0; }
            Isolated = lonely;
        }
        private void Update()
        {
            if (!MountainAuthority.IsHost) return;
            if (Time.time >= nextDistanceScan) { nextDistanceScan = Time.time + .2f; ScanDistance(); }
            if (!Alive) { Charge = 0; Isolated = Grounding = false; separatedAt = -1; return; }
            var inventory = GetComponent<GoatDescentInventory>();
            Grounding = inventory && inventory.AxeGrounded;
            if (Grounding) groundPoint = inventory.AxeContactPoint;
            float delta = Grounding ? -1f / Mathf.Max(.1f, settings.axeGroundingSeconds)
                : Isolated ? 1f / Mathf.Max(1f, settings.fullChargeSeconds) : -settings.calmDischargePerSecond;
            Charge = Mathf.Clamp01(Charge + delta * Time.deltaTime);
            if (!Isolated || GetComponent<GoatWaterDrinking>()?.Active == true) return;
            if (Time.time >= nextBleatAt && inhaleAt == 0 && loudAt == 0)
            {
                Bleat(1); inhaleAt = Time.time + settings.quietToInhaleSeconds;
                nextBleatAt = Time.time + settings.repeatBleatSeconds;
            }
            if (inhaleAt > 0 && Time.time >= inhaleAt)
            { inhaleAt = 0; Bleat(2); loudAt = Time.time + settings.inhaleSeconds; }
            if (loudAt > 0 && Time.time >= loudAt) { loudAt = 0; Bleat(3); }
        }
        private void Bleat(byte kind)
        {
            bleatKind = kind; bleatSerial++; bleatAt = Time.time;
            PlayVoice(kind);
            if (kind == 3) GetComponent<GoatJumpController>()?.AnnounceTrick("БЕ-Е-Е-Е!");
        }
        private void PlayVoice(byte kind)
        {
            EnsureEffects();
            var clip = kind == 3 ? loudClip : kind == 2 ? inhaleClip : quietClip;
            if (clip) voice.PlayOneShot(clip, kind == 3 ? .9f : .32f);
        }
        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost || !Alive || Time.time < shockUntil) return;
            var interaction = GetComponent<GoatInteraction>();
            if (interaction && interaction.Partner) TryShock(interaction.Partner.GetComponent<GoatHerdBehaviour>());
        }
        private void OnCollisionEnter(Collision collision) => Contact(collision);
        private void OnCollisionStay(Collision collision) => Contact(collision);
        private void Contact(Collision collision)
        {
            var other = collision.rigidbody ? collision.rigidbody.GetComponentInParent<GoatHerdBehaviour>() : null;
            if (other && other != this) TryShock(other);
        }
        public bool TryShock(GoatHerdBehaviour other)
        {
            if (!MountainAuthority.IsHost || !other || other == this || !Alive || !other.Alive
                || Time.time < shockUntil || Time.time < other.shockUntil) return false;
            float strength = Mathf.Max(Charge, other.Charge);
            if (strength < settings.minimumShockCharge) return false;
            Vector3 direction = Vector3.ProjectOnPlane(other.transform.position - transform.position, Vector3.up).normalized;
            if (direction.sqrMagnitude < .1f) direction = transform.right;
            Charge = other.Charge = 0;
            shockUntil = other.shockUntil = Time.time + Mathf.Max(.8f, settings.gripInterruptSeconds);
            shockSerial++; other.shockSerial++;
            GetComponent<GoatInteraction>()?.InterruptElectricGrip(settings.gripInterruptSeconds);
            other.GetComponent<GoatInteraction>()?.InterruptElectricGrip(settings.gripInterruptSeconds);
            GetComponent<GoatWaterDrinking>()?.Cancel(); other.GetComponent<GoatWaterDrinking>()?.Cancel();
            float impulse = settings.shockImpulse * Mathf.Lerp(.45f, 1f, strength);
            if (GetComponent<Rigidbody>()?.isKinematic == false) motor.ReceiveImpulse((-direction + Vector3.up * .1f) * impulse, .45f);
            if (other.GetComponent<Rigidbody>()?.isKinematic == false) other.motor.ReceiveImpulse((direction + Vector3.up * .1f) * impulse, .45f);
            PlayShock(); other.PlayShock();
            GetComponent<GoatJumpController>()?.AnnounceTrick("СТАТИЧЕСКИЙ РАЗРЯД!");
            return true;
        }
        private void PlayShock()
        {
            EnsureEffects();
            if (shockClip) voice.PlayOneShot(shockClip, .65f);
        }
        private void EnsureEffects()
        {
            if (voice) return;
            var node = new GameObject("Herd voice"); node.transform.SetParent(transform, false);
            voice = node.AddComponent<AudioSource>(); voice.playOnAwake = false;
            voice.spatialBlend = 1; voice.minDistance = 2; voice.maxDistance = 55;
            voice.dopplerLevel = 0; voice.rolloffMode = AudioRolloffMode.Linear;
            voice.pitch = GetComponent<GoatLocalControl>()?.Label == "B" ? .91f : 1.03f;
            electricMaterial = new Material(Resources.Load<Shader>("GoatBuffSpark"));
            for (int i = 0; i < arcs.Length; i++)
            {
                var spark = new GameObject("Static arc " + i); spark.transform.SetParent(transform, false);
                var line = spark.AddComponent<LineRenderer>(); arcs[i] = line;
                line.sharedMaterial = electricMaterial; line.positionCount = arcPoints.Length;
                line.useWorldSpace = true; line.widthMultiplier = .014f; line.numCapVertices = 2;
                line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.enabled = false;
            }
        }
        private void LateUpdate()
        {
            EnsureEffects();
            if (!head)
                foreach (var bone in GetComponentsInChildren<Transform>()) if (bone.name == "Head") { head = bone; break; }
            Vector3 nose = physical ? physical.NosePosition : transform.position + Vector3.up * 1.2f;
            voice.transform.position = nose;
            if (head && !mouth)
            {
                mouth = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform; mouth.name = "Bleat mouth";
                Destroy(mouth.GetComponent<Collider>()); mouth.SetParent(head, false);
                mouthMaterial = new Material(Shader.Find("Standard")); mouthMaterial.color = new Color(.045f, .017f, .022f);
                mouth.GetComponent<Renderer>().sharedMaterial = mouthMaterial;
                mouth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (mouth && head)
            {
                float opening = BleatPose;
                mouth.position = nose - head.up * .055f + head.forward * .012f;
                mouth.rotation = head.rotation;
                Vector3 scale = head.lossyScale;
                mouth.localScale = new Vector3(.115f / scale.x, .085f / scale.y, .03f / scale.z) * opening;
            }
            float flash = Mathf.Clamp01((shockUntil - Time.time) / .45f);
            float amount = Mathf.Max(Charge, flash);
            Vector3 center = physical ? physical.HarnessChest : transform.position + Vector3.up;
            Quaternion facing = Quaternion.LookRotation(GetComponent<GoatVisualController>()?.Facing ?? transform.forward, Vector3.up);
            for (int i = 0; i < arcs.Length; i++)
            {
                var line = arcs[i];
                bool toGround = Grounding && i == arcs.Length - 1;
                bool visible = Alive && amount > (toGround ? .015f : .08f)
                    && (toGround || flash > 0 || Mathf.Sin(Time.time * 16f + i * 2f) > 1f - amount * .8f);
                line.enabled = visible; if (!visible) continue;
                float angle = i * 1.2566f + Time.time * .23f;
                Vector3 origin = center + facing * new Vector3(Mathf.Cos(angle) * .34f, Mathf.Sin(angle) * .22f, -.16f * i);
                if (toGround) origin = GetComponent<GoatDescentInventory>()?.AxeTipPosition ?? center;
                for (int p = 0; p < arcPoints.Length; p++)
                {
                    float t = p / (float)(arcPoints.Length - 1);
                    arcPoints[p] = origin + facing * new Vector3(Mathf.Sin(p * 9 + Time.time * 43) * .035f,
                        t * (.13f + amount * .22f), t * .22f + Mathf.Sin(p * 11 + Time.time * 37) * .04f);
                    if (toGround) arcPoints[p] = Vector3.Lerp(origin, groundPoint, t)
                        + facing * new Vector3(Mathf.Sin(p * 13 + Time.time * 35) * .025f, Mathf.Sin(p * 7) * .018f, 0) * Mathf.Sin(t * Mathf.PI);
                }
                line.SetPositions(arcPoints);
                line.startColor = new Color(.52f, .82f, 1f, amount); line.endColor = new Color(.9f, .98f, 1f, amount * .2f);
            }
        }
        public GoatHerdState Capture() => new GoatHerdState { isolated = Isolated, grounding = Grounding,
            charge = Charge, groundPoint = groundPoint, aloneSeconds = AloneSeconds, bleatSerial = bleatSerial, bleatKind = bleatKind,
            bleatAge = Time.time - bleatAt, shockSerial = shockSerial, shockRemaining = Mathf.Max(0, shockUntil - Time.time) };
        public void Apply(GoatHerdState state)
        {
            if (MountainAuthority.IsHost) return;
            Isolated = state.isolated; Grounding = state.grounding; Charge = Mathf.Clamp01(state.charge);
            groundPoint = state.groundPoint;
            separatedAt = state.aloneSeconds > 0 ? Time.time - state.aloneSeconds : -1;
            bleatKind = state.bleatKind; bleatAt = Time.time - state.bleatAge;
            if (bleatSerial != state.bleatSerial && state.bleatAge < .25f) PlayVoice(state.bleatKind);
            bleatSerial = state.bleatSerial;
            if (shockSerial != state.shockSerial && state.shockRemaining > .45f) PlayShock();
            shockSerial = state.shockSerial; shockUntil = Time.time + state.shockRemaining;
        }
        public void ResetNerves()
        { Isolated = Grounding = false; Charge = 0; separatedAt = -1; nextBleatAt = inhaleAt = loudAt = 0; bleatAt = -100; }
        private void OnDestroy()
        {
            herd.Remove(this);
            if (mouth) Destroy(mouth.gameObject);
            if (mouthMaterial) Destroy(mouthMaterial); if (electricMaterial) Destroy(electricMaterial);
        }
    }
}
