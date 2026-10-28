using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    public enum RockfallPhase : byte { Waiting, Warning, Falling, Settling, Complete }

    /// <summary>A bounded, host-simulated rockfall at the middle resting ledge.</summary>
    public sealed class RockfallEpisode : MonoBehaviour
    {
        private sealed class Stone
        {
            public Transform view;
            public Vector3 velocity, spin;
            public float radius, age;
            public int bounces;
            public bool active;
        }
        private const int PoolSize = 28;
        private readonly Stone[] stones = new Stone[PoolSize];
        private readonly RaycastHit[] hits = new RaycastHit[48];
        private readonly Dictionary<GoatController, float> nextHit = new Dictionary<GoatController, float>();
        private readonly System.Random random = new System.Random(61429);
        private GoatController[] goats;
        private Mesh stoneMesh;
        private Material dustMaterial;
        private ParticleSystem dust;
        private AudioSource rumble;
        private AudioClip rumbleClip;
        private float phaseEnd, nextStone, nextDust, resetTime;
        private bool initialized;
        public static RockfallEpisode Current { get; private set; }
        public RockfallPhase Phase { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Outward { get; private set; }
        public int RouteIndex { get; private set; }
        public int Spawned { get; private set; }
        public int HitsApplied { get; private set; }
        public bool IsDangerous => Phase == RockfallPhase.Warning || Phase == RockfallPhase.Falling || Phase == RockfallPhase.Settling;
        public float SecondsLeft => Mathf.Max(0, phaseEnd - Time.time);
        public int ActiveStones
        {
            get { int count = 0; foreach (var stone in stones) if (stone != null && stone.active) count++; return count; }
        }

        private void Awake() => Current = this;

        public void Initialize(IReadOnlyList<LandingPlatform> route, Material rock)
        {
            if (initialized || route == null || route.Count < 8) return;
            RouteIndex = route.Count / 2;
            // A resting shelf gives the pair solid footing while they wait.
            for (int offset = 0; offset <= 5; offset++)
            {
                int a = Mathf.Clamp(route.Count / 2 + offset, 1, route.Count - 2);
                int b = Mathf.Clamp(route.Count / 2 - offset, 1, route.Count - 2);
                if (route[a].Type == LandingPlatformType.Rest) { RouteIndex = a; break; }
                if (route[b].Type == LandingPlatformType.Rest) { RouteIndex = b; break; }
            }
            Center = route[RouteIndex].Center;
            Outward = FindCliffNormal(Center + Vector3.up * 1.3f);
            stoneMesh = MakeStoneMesh();
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Rockfall fragment " + (i + 1));
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = stoneMesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = rock;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                stones[i] = new Stone { view = go.transform };
                go.SetActive(false);
            }
            goats = FindObjectsByType<GoatController>(FindObjectsSortMode.None);
            CreateFeedback(); initialized = true;
        }

        public bool InArea(Vector3 point, float vertical = 12)
            => Mathf.Abs(point.y - Center.y) < vertical && Vector3.ProjectOnPlane(point - Center, Vector3.up).sqrMagnitude < 18 * 18;

        public bool Begin()
        {
            if (!initialized || !MountainAuthority.IsHost || IsDangerous) return false;
            nextHit.Clear(); Spawned = HitsApplied = 0;
            SetPhase(RockfallPhase.Warning, 6); return true;
        }

        private void SetPhase(RockfallPhase phase, float duration)
        {
            Phase = phase; phaseEnd = Time.time + duration;
            nextStone = Time.time; nextDust = Time.time;
        }

        private void Update()
        {
            if (!initialized) return;
            if (MountainAuthority.IsHost)
            {
                if (Phase == RockfallPhase.Waiting)
                {
                    foreach (var goat in goats)
                        if (goat && goat.Grounded && goat.GetComponent<RespawnController>()?.IsDead != true
                            && InArea(goat.transform.position, 5)) { Begin(); break; }
                }
                else if (Phase == RockfallPhase.Warning && SecondsLeft <= 0) SetPhase(RockfallPhase.Falling, 16);
                else if (Phase == RockfallPhase.Falling)
                {
                    if (SecondsLeft <= 0) SetPhase(RockfallPhase.Settling, 7);
                    else if (Time.time >= nextStone)
                    {
                        SpawnStone();
                        nextStone = Time.time + Mathf.Lerp(.22f, .5f, .5f + .5f * Mathf.Sin(SecondsLeft * 1.8f));
                    }
                }
                else if (Phase == RockfallPhase.Settling && ActiveStones == 0) SetPhase(RockfallPhase.Complete, 4);
                else if (Phase == RockfallPhase.Complete)
                {
                    bool returnedToSummit = goats.Length > 0;
                    foreach (var goat in goats) if (goat && goat.transform.position.y < Center.y + 40) returnedToSummit = false;
                    resetTime = returnedToSummit ? resetTime + Time.deltaTime : 0;
                    if (resetTime > 5) { SetPhase(RockfallPhase.Waiting, 0); resetTime = 0; }
                }
            }
            if (rumble)
            {
                float volume = Phase == RockfallPhase.Warning ? .25f : Phase == RockfallPhase.Falling ? .6f : 0;
                rumble.volume = Mathf.MoveTowards(rumble.volume, volume, Time.deltaTime * .5f);
                if (volume > 0 && !rumble.isPlaying) rumble.Play();
                if (rumble.volume <= 0 && rumble.isPlaying) rumble.Stop();
            }
            if ((Phase == RockfallPhase.Warning || Phase == RockfallPhase.Falling) && Time.time >= nextDust)
            {
                Puff(Center + Vector3.up * 8 + Side * Range(-4, 4), 10);
                nextDust = Time.time + .3f;
            }
        }

        private Vector3 Side => Vector3.Cross(Vector3.up, Outward);
        private float Range(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());

        private void SpawnStone()
        {
            Stone free = null;
            foreach (var stone in stones) if (!stone.active) { free = stone; break; }
            if (free == null) return;
            float radius = Range(.22f, .48f);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 target = Center + Side * Range(-3.4f, 3.4f) + Outward * Range(-.6f, 2.3f);
                Vector3 source = target + Vector3.up * 2;
                float height = Range(11, 18);
                int count = Physics.RaycastNonAlloc(source, Vector3.up, hits, height, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++) height = Mathf.Min(height, hits[i].distance - radius - .15f);
                if (height < 3.5f) continue;
                source += Vector3.up * height;
                if (Physics.CheckSphere(source, radius, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore)) continue;
                free.radius = radius; free.age = 0; free.bounces = 0; free.active = true;
                free.velocity = Vector3.down * Range(2, 4) + Outward * Range(-.15f, .2f);
                free.spin = new Vector3(Range(-170, 170), Range(-170, 170), Range(-170, 170));
                free.view.SetPositionAndRotation(source, Quaternion.Euler(free.spin));
                free.view.localScale = Vector3.one * radius * 2;
                free.view.gameObject.SetActive(true); Spawned++; return;
            }
        }

        private void FixedUpdate()
        {
            if (!initialized || !MountainAuthority.IsHost) return;
            foreach (var stone in stones)
            {
                if (!stone.active) continue;
                float dt = Time.fixedDeltaTime; stone.age += dt;
                if (stone.age > 6 || stone.view.position.y < Center.y - 25) { Retire(stone); continue; }
                stone.velocity += Physics.gravity * dt;
                Vector3 step = stone.velocity * dt;
                int count = Physics.SphereCastNonAlloc(stone.view.position, stone.radius, step.normalized,
                    hits, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
                int nearest = -1; float distance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                    if (hits[i].distance < distance) { nearest = i; distance = hits[i].distance; }
                if (nearest >= 0)
                {
                    var hit = hits[nearest];
                    var goat = hit.collider.GetComponentInParent<GoatController>();
                    if (!goat)
                    {
                        var part = hit.collider.GetComponentInParent<GoatPhysicsPart>();
                        if (part && part.Owner) goat = part.Owner.GetComponent<GoatController>();
                    }
                    Puff(hit.point + hit.normal * .1f, 8);
                    if (goat) { HitGoat(goat, stone.velocity, stone.radius); Retire(stone); continue; }
                    stone.view.position += step.normalized * distance + hit.normal * .04f;
                    stone.velocity = Vector3.Reflect(stone.velocity, hit.normal) * .38f + Outward * .7f;
                    if (++stone.bounces >= 2) { Retire(stone); continue; }
                }
                else stone.view.position += step;
                stone.view.Rotate(stone.spin * dt, Space.World);
            }
        }

        private void HitGoat(GoatController goat, Vector3 velocity, float radius)
        {
            if (goat.GetComponent<RespawnController>()?.IsDead == true) return;
            if (nextHit.TryGetValue(goat, out float readyAt) && Time.time < readyAt) return;
            nextHit[goat] = Time.time + .8f;
            float protection = goat.GetComponent<GoatRockfallShelter>()?.Protection ?? 0;
            float strength = Mathf.InverseLerp(3, 18, velocity.magnitude);
            float damage = Mathf.Lerp(10, 25, strength) * Mathf.Lerp(.8f, 1.15f, Mathf.InverseLerp(.22f, .48f, radius));
            goat.GetComponent<GoatHealth>()?.Damage(damage * (1 - protection * .96f));
            Vector3 shove = (Outward + Vector3.up * .12f).normalized;
            goat.ReceiveImpulse(shove * Mathf.Lerp(100, 220, strength) * (1 - protection * .96f), .4f);
            HitsApplied++;
        }

        private static void Retire(Stone stone) { stone.active = false; stone.view.gameObject.SetActive(false); }

        public RockfallState Capture()
        {
            if (!initialized) return default;
            var state = new RockfallState { active = true, phase = (byte)Phase, secondsLeft = SecondsLeft,
                center = Center, outward = Outward, stones = new RockfallStoneState[PoolSize] };
            for (int i = 0; i < PoolSize; i++)
                state.stones[i] = new RockfallStoneState { active = stones[i].active, position = stones[i].view.position,
                    rotation = stones[i].view.rotation, radius = stones[i].radius };
            return state;
        }

        public void Apply(RockfallState state)
        {
            if (MountainAuthority.IsHost || !initialized || !state.active) return;
            Phase = (RockfallPhase)state.phase; phaseEnd = Time.time + state.secondsLeft;
            Center = state.center; Outward = state.outward;
            for (int i = 0; i < PoolSize; i++)
            {
                var incoming = state.stones != null && i < state.stones.Length ? state.stones[i] : default;
                var stone = stones[i]; stone.active = incoming.active; stone.view.gameObject.SetActive(incoming.active);
                if (!incoming.active) continue;
                stone.view.SetPositionAndRotation(incoming.position, incoming.rotation);
                stone.view.localScale = Vector3.one * incoming.radius * 2;
            }
        }

        private Vector3 FindCliffNormal(Vector3 point)
        {
            float distance = 7; Vector3 normal = Vector3.forward;
            for (int ray = 0; ray < 24; ray++)
            {
                Vector3 direction = Quaternion.Euler(0, ray * 15, 0) * Vector3.forward;
                if (Physics.Raycast(point, direction, out var hit, distance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(hit.normal.y) < .5f)
                { distance = hit.distance; normal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized; }
            }
            return normal;
        }

        private void CreateFeedback()
        {
            var node = new GameObject("Rockfall dust and grit"); node.transform.SetParent(transform, false);
            dust = node.AddComponent<ParticleSystem>(); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180; main.startLifetime = .8f; main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.06f, .22f);
            main.startColor = new Color(.57f, .51f, .42f, .48f); main.gravityModifier = .3f;
            var emission = dust.emission; emission.enabled = false; var shape = dust.shape; shape.enabled = false;
            var fade = dust.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            dustMaterial = new Material(Resources.Load<Shader>("WeatherRain")); dustMaterial.SetFloat("_Splash", 1);
            var renderer = dust.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = dustMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; dust.Play();
            var audioNode = new GameObject("Rockfall warning rumble"); audioNode.transform.SetParent(transform, false);
            audioNode.transform.position = Center + Vector3.up * 8;
            rumble = audioNode.AddComponent<AudioSource>(); rumble.spatialBlend = 1; rumble.minDistance = 8; rumble.maxDistance = 65;
            rumble.loop = true; rumble.playOnAwake = false; rumble.volume = 0;
            const int rate = 22050; var samples = new float[rate * 2];
            var noise = new System.Random(1729); float low = 0;
            for (int i = 0; i < samples.Length; i++)
            { low = low * .92f + ((float)noise.NextDouble() * 2 - 1) * .08f; samples[i] = low * .8f + Mathf.Sin(i * 2 * Mathf.PI * 48 / rate) * .12f; }
            rumbleClip = AudioClip.Create("Cliff rockfall rumble", samples.Length, 1, rate, false);
            rumbleClip.SetData(samples, 0); rumble.clip = rumbleClip;
        }

        private void Puff(Vector3 point, int amount)
        {
            if (!dust) return;
            for (int i = 0; i < amount; i++) dust.Emit(new ParticleSystem.EmitParams
            { position = point, velocity = new Vector3(Range(-1, 1), Range(.3f, 1.5f), Range(-1, 1)) }, 1);
        }

        private static Mesh MakeStoneMesh()
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            Vector3[] v = { new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),
                new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),
                new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1) };
            int[] triangles = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var flat = new Vector3[triangles.Length]; var indices = new int[flat.Length]; var colors = new Color[flat.Length];
            for (int i = 0; i < flat.Length; i++)
            { flat[i] = v[triangles[i]].normalized * (.44f + .055f * Mathf.Sin(triangles[i] * 3.7f)); indices[i] = i; colors[i] = new Color(.48f,.44f,.39f,1); }
            var mesh = new Mesh { name = "Reusable angular rockfall fragment", vertices = flat, triangles = indices, colors = colors };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (stoneMesh) Destroy(stoneMesh); if (dustMaterial) Destroy(dustMaterial); if (rumbleClip) Destroy(rumbleClip);
        }
    }
}
