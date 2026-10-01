using UnityEngine;

namespace GoatDescent
{
    /// <summary>Shared state for the main mountain, without a second level generator.</summary>
    public sealed class MountainSessionState : MonoBehaviour
    {
        private CrumblingPlatform[] platforms;
        private MountainHazardDirector hazards;
        private SkyPredatorEpisode eagles;
        public string MapSignature { get; private set; }

        public void Initialize()
        {
            platforms = FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None);
            hazards = MountainHazardDirector.Current;
            eagles = SkyPredatorEpisode.Current;
            var route = MountainGenerator.CurrentRoute;
            uint hash = 2166136261u;
            if (route != null)
            {
                foreach (var point in route)
                {
                    hash = Fold(hash, Mathf.RoundToInt(point.Center.x * 10f));
                    hash = Fold(hash, Mathf.RoundToInt(point.Center.y * 10f));
                    hash = Fold(hash, Mathf.RoundToInt(point.Center.z * 10f));
                    hash = Fold(hash, (int)point.Type);
                }
            }
            MapSignature = (route?.Count ?? 0) + ":" + hash.ToString("X8");
        }

        private static uint Fold(uint hash, int value)
            => unchecked((hash ^ (uint)value) * 16777619u);

        public MountainSnapshot CaptureSnapshot(int sequence)
        {
            if (platforms == null) Initialize();
            var snapshot = new MountainSnapshot
            {
                sequence = sequence, schema = 7, mapSignature = MapSignature,
                hostTime = Time.time, scenario = LocalGoatPair.Instance?.ScenarioCode ?? 0,
                ledges = new LedgeState[platforms.Length],
                stones = hazards ? hazards.CaptureStones() : new StoneState[0],
                loose = hazards ? hazards.CaptureSurfaces() : new LooseState[0],
                birds = eagles ? eagles.CaptureState() : new BirdState[0]
            };
            for (int i = 0; i < platforms.Length; i++)
                snapshot.ledges[i] = platforms[i].CaptureState();
            var pair = LocalGoatPair.Instance;
            snapshot.goats = pair ? new[] { CaptureGoat(pair.Primary), CaptureGoat(pair.Secondary) }
                : new GoatState[0];
            snapshot.players = new PlayerMountainProgress[0];
            snapshot.bellsState = new BellState[0];
            return snapshot;
        }

        private static GoatState CaptureGoat(GoatController goat)
        {
            if (!goat) return default;
            var body = goat.GetComponent<Rigidbody>();
            var visual = goat.GetComponent<GoatVisualController>();
            var interaction = goat.GetComponent<GoatInteraction>();
            return new GoatState
            {
                id = goat.GetComponent<GoatLocalControl>()?.Label ?? goat.name,
                position = body.position, rotation = body.rotation,
                velocity = body.linearVelocity,
                dead = goat.GetComponent<RespawnController>()?.IsDead ?? false,
                linked = interaction?.IsLinked ?? false,
                holding = interaction?.IsHolding ?? false,
                interactionStatus = interaction?.Status ?? "",
                animation = visual ? visual.CurrentClip : "Goat_Idle",
                facing = visual ? visual.Facing : goat.transform.forward,
                predatorCarried = goat.IsPredatorCarried,
                parts = goat.GetComponent<GoatPhysicalBody>()?.CaptureNetworkPose()
            };
        }

        public void ApplySnapshot(MountainSnapshot snapshot)
        {
            if (snapshot == null || MountainAuthority.IsHost) return;
            if (platforms == null) Initialize();
            if (snapshot.ledges != null)
                foreach (var state in snapshot.ledges)
                    foreach (var platform in platforms)
                        if (platform.StableId == state.id) { platform.ApplyState(state); break; }
            hazards?.ApplyState(snapshot);
            eagles?.ApplyState(snapshot.birds);
            LocalGoatPair.Instance?.ApplyNetworkScenario(snapshot.scenario);
            if (snapshot.goats == null) return;
            var pair = LocalGoatPair.Instance;
            foreach (var state in snapshot.goats)
            {
                var goat = state.id == "A" ? pair?.Primary : state.id == "B" ? pair?.Secondary : null;
                if (!goat) continue;
                var body = goat.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.position = state.position;
                body.rotation = state.rotation;
                goat.SetNetworkVelocity(state.velocity);
                goat.SetPredatorCarried(state.predatorCarried);
                goat.GetComponent<GoatPhysicalBody>()?.ApplyNetworkPose(state.parts);
                goat.GetComponent<GoatVisualController>()?.ApplyNetworkPose(state.animation, state.facing);
                goat.GetComponent<GoatInteraction>()?.ApplyNetworkState(state.linked,
                    state.holding, state.interactionStatus);
                goat.GetComponent<RespawnController>()?.ApplyNetworkState(state.dead, false);
            }
        }
    }
}
