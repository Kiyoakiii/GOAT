using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Fixed placements and physical event trail for the shared mountain.</summary>
    public sealed class MountainHazardDirector : MonoBehaviour
    {
        public static MountainHazardDirector Current { get; private set; }
        public IReadOnlyList<string> RecentChains => chains;
        public int ActiveStones { get; private set; }
        private readonly List<string> chains = new List<string>();
        private readonly List<RollingStone> stones = new List<RollingStone>();
        private readonly List<LooseSurface> surfaces = new List<LooseSurface>();
        private const int MaximumActiveStones = 4;

        private void Awake() => Current = this;
        private void OnDestroy() { if (Current == this) Current = null; }

        public void Initialize(IReadOnlyList<LandingPlatform> route, Material rock,
            Transform routeProps)
        {
            int[] stoneSites = { 3, 6, 14, 22, 34, 46, 54, 62 };
            for (int i = 0; i < stoneSites.Length; i++)
            {
                if (stoneSites[i] >= route.Count) continue;
                var platform = route[stoneSites[i]];
                var objectStone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                objectStone.name = $"STONE-{i + 1:00}";
                objectStone.transform.SetParent(transform);
                float diameter = i % 3 == 0 ? 1.25f : .85f;
                objectStone.transform.position = platform.Center + new Vector3(
                    (i % 2 == 0 ? -1f : 1f) * 1.15f, diameter * .5f + .15f, .45f);
                objectStone.transform.localScale = Vector3.one * diameter;
                objectStone.GetComponent<Renderer>().sharedMaterial = rock;
                var body = objectStone.AddComponent<Rigidbody>();
                body.mass = i % 3 == 0 ? 84f : 28f;
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var stone = objectStone.AddComponent<RollingStone>();
                Vector3 downhill = stoneSites[i] + 1 < route.Count
                    ? Vector3.ProjectOnPlane(route[stoneSites[i] + 1].Center - platform.Center,
                        Vector3.up).normalized : Vector3.forward;
                stone.Initialize(this, $"STONE-{i + 1:00}", downhill, body);
                stones.Add(stone);
            }
            if (!routeProps) return;
            int looseIndex = 0;
            foreach (var collider in routeProps.GetComponentsInChildren<MeshCollider>())
            {
                bool snow = collider.name == "Ice Landing";
                if (!snow && collider.name != "Crumbling Landing") continue;
                var surface = collider.gameObject.AddComponent<LooseSurface>();
                surface.Initialize($"{(snow ? "SNOW" : "GRAVEL")}-{++looseIndex:00}", snow);
                surfaces.Add(surface);
            }
        }

        public StoneState[] CaptureStones()
        {
            var states = new StoneState[stones.Count];
            for (int i = 0; i < states.Length; i++) states[i] = stones[i].CaptureState();
            return states;
        }

        public LooseState[] CaptureSurfaces()
        {
            var states = new LooseState[surfaces.Count];
            for (int i = 0; i < states.Length; i++) states[i] = surfaces[i].CaptureState();
            return states;
        }

        public void ApplyState(MountainSnapshot snapshot)
        {
            if (MountainAuthority.IsHost) return;
            if (snapshot.stones != null)
                foreach (var state in snapshot.stones)
                    foreach (var stone in stones)
                        if (stone.StableId == state.id) { stone.ApplyState(state); break; }
            if (snapshot.loose != null)
                foreach (var state in snapshot.loose)
                    foreach (var surface in surfaces)
                        if (surface.StableId == state.id) { surface.ApplyState(state); break; }
        }

        public bool TryActivate(RollingStone stone, string cause)
        {
            if (!MountainAuthority.IsHost || !stone || stone.IsMoving || ActiveStones >= MaximumActiveStones) return false;
            ActiveStones++;
            Record($"{stone.StableId} сорван: {cause}");
            return true;
        }

        public void Settled(RollingStone stone)
        {
            ActiveStones = Mathf.Max(0, ActiveStones - 1);
            Record($"{stone.StableId} остановился после {stone.Cause}");
        }

        public void OnPlatformBroken(CrumblingPlatform platform)
        {
            foreach (var stone in stones)
                if (stone && Vector3.Distance(stone.transform.position,
                    platform.transform.position) < 3f)
                    stone.ReleaseFromSupport(platform.name);
        }

        public void Record(string entry)
        {
            string line = $"{Time.time:0.00} {entry}";
            chains.Add(line);
            if (chains.Count > 64) chains.RemoveAt(0);
            Debug.Log("MOUNTAIN_CHAIN " + line);
        }
    }

    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class RollingStone : MonoBehaviour
    {
        public string StableId { get; private set; }
        public Vector3 Downhill { get; private set; }
        public bool IsMoving { get; private set; }
        public string Cause { get; private set; }
        private MountainHazardDirector director;
        private Rigidbody body;
        private AudioSource rollAudio;
        private static AudioClip rollClip;
        private float releasedAt, slowSince;

        public void Initialize(MountainHazardDirector owner, string id, Vector3 downhill,
            Rigidbody stoneBody)
        {
            director = owner; StableId = id; Downhill = downhill; body = stoneBody; Cause = "на месте";
            if (!rollClip) rollClip = MakeRollClip();
            rollAudio = gameObject.AddComponent<AudioSource>();
            rollAudio.clip = rollClip; rollAudio.loop = true; rollAudio.playOnAwake = false;
            rollAudio.spatialBlend = 1f; rollAudio.minDistance = 3f; rollAudio.maxDistance = 85f;
        }

        public bool ReceiveImpulse(Vector3 impulse, string source)
        {
            if (!IsMoving && !director.TryActivate(this, source)) return false;
            if (!IsMoving)
            {
                IsMoving = true; Cause = source; releasedAt = Time.time;
                body.isKinematic = false; body.WakeUp();
                rollAudio.Play();
            }
            body.AddForce(impulse, ForceMode.Impulse);
            return true;
        }

        public StoneState CaptureState() => new StoneState
        { id = StableId, active = gameObject.activeSelf, moving = IsMoving,
          position = transform.position, rotation = transform.rotation,
          velocity = IsMoving ? body.linearVelocity : Vector3.zero, cause = Cause };

        public void ApplyState(StoneState state)
        {
            if (MountainAuthority.IsHost || state.id != StableId) return;
            gameObject.SetActive(state.active);
            IsMoving = state.moving; Cause = state.cause;
            body.isKinematic = true;
            body.position = state.position; body.rotation = state.rotation;
            if (state.moving && !rollAudio.isPlaying) rollAudio.Play();
            if (!state.moving && rollAudio.isPlaying) rollAudio.Stop();
            rollAudio.volume = Mathf.Clamp01(state.velocity.magnitude / 8f) * .75f;
        }

        public void ReleaseFromSupport(string ledgeId)
            => ReceiveImpulse(Downhill * 3f, ledgeId + " обрушился");

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost || !IsMoving) return;
            if (rollAudio) rollAudio.volume = Mathf.Clamp01(body.linearVelocity.magnitude / 8f) * .75f;
            if (body.linearVelocity.sqrMagnitude > .09f) slowSince = 0f;
            else if (slowSince == 0f) slowSince = Time.time;
            bool stopped = slowSince > 0f && Time.time - slowSince > 3f;
            bool old = Time.time - releasedAt > 28f;
            if (!stopped && !old) return;
            foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
                if (goat && Vector3.Distance(goat.transform.position, transform.position) < 10f) return;
            IsMoving = false;
            if (rollAudio) rollAudio.Stop();
            director.Settled(this);
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!MountainAuthority.IsHost) return;
            var goat = collision.rigidbody ? collision.rigidbody.GetComponent<GoatController>() : null;
            if (!IsMoving)
            {
                if (goat && collision.relativeVelocity.magnitude > 1.2f)
                    ReceiveImpulse(goat.Velocity * goat.GetComponent<Rigidbody>().mass * .18f,
                        goat.name + " коснулся камня");
                else if (collision.rigidbody && collision.rigidbody.GetComponent<RollingStone>()
                    && collision.relativeVelocity.magnitude > 1f)
                    ReceiveImpulse(collision.relativeVelocity * body.mass * .2f,
                        collision.rigidbody.name + " ударил камень");
                return;
            }
            if (goat && collision.relativeVelocity.magnitude > 1.7f)
            {
                float impulse = Mathf.Min(280f, body.mass * collision.relativeVelocity.magnitude * .32f);
                goat.ReceiveImpulse((goat.transform.position - transform.position).normalized * impulse, .35f);
                director.Record($"{StableId} после {Cause} сбил {goat.name}, сила {impulse:0} Н·с");
            }
            var platform = collision.collider.GetComponent<CrumblingPlatform>();
            if (platform && collision.relativeVelocity.magnitude > 2f)
            {
                platform.RegisterRockImpact(body.mass * collision.relativeVelocity.magnitude);
                director.Record($"{StableId} после {Cause} ударил {platform.name}");
            }
        }

        private static AudioClip MakeRollClip()
        {
            const int rate = 22050, length = 11025;
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                samples[i] = .15f * (Mathf.Sin(t * 2f * Mathf.PI * 69f)
                    + .47f * Mathf.Sin(t * 2f * Mathf.PI * 137f)
                    + .21f * Mathf.Sin(i * 1.77f));
            }
            var clip = AudioClip.Create("Rolling stone warning", length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>Marked gravel that temporarily loses traction under measured force or motion.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class LooseSurface : MonoBehaviour
    {
        public string StableId { get; private set; }
        public bool IsLoose { get; private set; }
        public float SecondsLeft => IsLoose ? Mathf.Max(0f, recoverAt - Time.time) : 0f;
        public float TractionMultiplier => IsLoose ? .16f : 1f;
        private Collider support;
        private PhysicsMaterial originalMaterial, looseMaterial;
        private MeshRenderer stone;
        private Color originalColor;
        private string colorProperty;
        private bool snow;
        private MaterialPropertyBlock tint;
        private float recoverAt, slideTime;

        public void Initialize(string id, bool snowSurface)
        {
            StableId = id; snow = snowSurface;
            support = GetComponent<Collider>(); originalMaterial = support.sharedMaterial;
            looseMaterial = new PhysicsMaterial("Loose gravel traction")
            { staticFriction = .05f, dynamicFriction = .03f, frictionCombine = PhysicsMaterialCombine.Minimum };
            stone = GetComponent<MeshRenderer>();
            colorProperty = stone.sharedMaterial.HasProperty("_StoneColor") ? "_StoneColor" : "_Color";
            originalColor = stone.sharedMaterial.GetColor(colorProperty);
            tint = new MaterialPropertyBlock();
            SetTint(snow ? new Color(.9f, .94f, .98f) : new Color(.76f, .68f, .47f));
        }

        public LooseState CaptureState() => new LooseState
        { id = StableId, active = IsLoose, secondsLeft = SecondsLeft };

        public void ApplyState(LooseState state)
        {
            if (MountainAuthority.IsHost || state.id != StableId) return;
            IsLoose = state.active;
            recoverAt = Time.time + state.secondsLeft;
            support.sharedMaterial = IsLoose ? looseMaterial : originalMaterial;
            SetTint(IsLoose ? snow ? new Color(.53f, .66f, .77f) : new Color(.48f, .4f, .28f)
                : snow ? new Color(.9f, .94f, .98f) : new Color(.76f, .68f, .47f));
        }

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            int goats = 0; float moving = 0f;
            foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
            {
                var ground = goat.GetComponent<GoatGroundDetector>();
                if (!ground || !ground.IsGrounded || ground.GroundHit.collider != support) continue;
                goats++;
                moving = Mathf.Max(moving, goat.Velocity.magnitude);
            }
            slideTime = moving > 3.5f ? slideTime + Time.fixedDeltaTime : Mathf.Max(0f, slideTime - Time.fixedDeltaTime);
            if (!IsLoose && (goats >= 2 || slideTime >= .5f)) Activate(goats >= 2 ? "нагрузка двух козлов" : "скольжение");
            if (IsLoose && Time.time >= recoverAt && goats == 0) Restore();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!MountainAuthority.IsHost) return;
            if (collision.rigidbody && collision.rigidbody.GetComponent<GoatController>()
                && Mathf.Abs(collision.relativeVelocity.y) > 6f)
                Activate("сильное приземление");
        }

        private void Activate(string reason)
        {
            if (IsLoose) return;
            IsLoose = true; recoverAt = Time.time + 6f;
            support.sharedMaterial = looseMaterial;
            SetTint(snow ? new Color(.53f, .66f, .77f) : new Color(.48f, .4f, .28f));
            MountainHazardDirector.Current?.Record($"{StableId} осыпался: {reason}");
        }

        private void Restore()
        {
            IsLoose = false; slideTime = 0f; support.sharedMaterial = originalMaterial;
            SetTint(snow ? new Color(.9f, .94f, .98f) : new Color(.76f, .68f, .47f));
            MountainHazardDirector.Current?.Record($"{StableId} восстановил сцепление");
        }

        private void SetTint(Color color)
        {
            if (!stone) return;
            stone.GetPropertyBlock(tint);
            tint.SetColor(colorProperty, Color.Lerp(originalColor, color, .65f));
            stone.SetPropertyBlock(tint);
        }

        private void OnDestroy() { if (support) support.sharedMaterial = originalMaterial; if (looseMaterial) Destroy(looseMaterial); }
    }
}
