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
            MapSignature = GetComponent<MountainSceneBuilder>()?.useGreenPillars == true
                ? "green-pillars-28471-level1-cave-v1:"+(route?.Count??0)+":"+hash.ToString("X8")
                : (route?.Count ?? 0) + ":" + hash.ToString("X8");
        }

        private static uint Fold(uint hash, int value)
            => unchecked((hash ^ (uint)value) * 16777619u);

        public MountainSnapshot CaptureSnapshot(int sequence)
        {
            if (platforms == null) Initialize();
            var snapshot = new MountainSnapshot
            {
                sequence = sequence, schema = MountainSnapshot.CurrentSchema, mapSignature = MapSignature,
                hostTime = Time.time, scenario = LocalGoatPair.Instance?.ScenarioCode ?? 0,
                ledges = new LedgeState[platforms.Length],
                stones = hazards ? hazards.CaptureStones() : new StoneState[0],
                loose = hazards ? hazards.CaptureSurfaces() : new LooseState[0],
                birds = eagles ? eagles.CaptureState() : new BirdState[0],
                weather = WorldWeather.Current ? WorldWeather.Current.Capture() : default
            };
            snapshot.chest = MadnessChest.Current ? MadnessChest.Current.Capture() : default;
            snapshot.supplies = DescentSupplyChest.Current ? DescentSupplyChest.Current.Capture() : default;
            snapshot.puddles = BeerPuddle.CaptureAll();
            for (int i = 0; i < platforms.Length; i++)
                snapshot.ledges[i] = platforms[i].CaptureState();
            var pair = LocalGoatPair.Instance;
            snapshot.goats = pair ? new[] { CaptureGoat(pair.Primary), CaptureGoat(pair.Secondary) }
                : new GoatState[0];
            snapshot.players = LevelOneCave.Current ? LevelOneCave.Current.CapturePlayers() : new PlayerMountainProgress[0];
            snapshot.completed = LevelOneCave.Current && LevelOneCave.Current.Completed;
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
                herd = goat.GetComponent<GoatHerdBehaviour>()?.Capture() ?? default,
                coat = goat.GetComponent<GoatFurEffect>()?.Capture() ?? default,
                drinkingRemaining = goat.GetComponent<GoatWaterDrinking>()?.Remaining ?? 0,
                health=goat.GetComponent<GoatHealth>()?.Current??100f,
                beerRemaining=goat.GetComponent<GoatBeerEffect>()?.Remaining??0f,
                toilet=goat.GetComponent<GoatToiletAction>()?.Capture()??default,
                skinNumber=goat.GetComponent<GoatSkin>()?.Number??0,
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
                punchBuffRemaining = goat.GetComponent<GoatPunchBuff>()?.Remaining ?? 0f,
                inventory = goat.GetComponent<GoatDescentInventory>()?.Capture() ?? default,
                anchorLength = goat.GetComponent<GoatDescentTricks>()?.RopeLength ?? 3.5f,
                birdSafeRemaining = goat.GetComponent<GoatDescentTricks>()?.BirdSafeRemaining ?? 0f,
                anchorRemaining = goat.GetComponent<GoatDescentTricks>()?.AnchorRemaining ?? 0f,
                anchorPosition = goat.GetComponent<GoatDescentTricks>()?.AnchorPosition ?? Vector3.zero,
                madnessRemaining = goat.GetComponent<GoatDescentTricks>()?.MadnessRemaining ?? 0f,
                madnessDirection = goat.GetComponent<GoatDescentTricks>()?.MadnessDirection ?? Vector3.zero,
                playingDead = goat.GetComponent<GoatComedyActions>()?.PlayingDead ?? false,
                gettingUp = goat.GetComponent<GoatComedyActions>()?.Recovering ?? false,
                ridingOn = goat.GetComponent<GoatComedyActions>()?.CarrierId ?? "",
                rideLean = goat.GetComponent<GoatComedyActions>()?.RideLean ?? 0f,
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
            MadnessChest.Apply(snapshot.chest);
            DescentSupplyChest.Apply(snapshot.supplies);
            BeerPuddle.ApplyAll(snapshot.puddles);
            eagles?.ApplyState(snapshot.birds);
            if (snapshot.weather.active) WorldWeather.Ensure().Apply(snapshot.weather);
            LocalGoatPair.Instance?.ApplyNetworkScenario(snapshot.scenario);
            LevelOneCave.Current?.Apply(snapshot.players,snapshot.completed);
            if (snapshot.goats == null) return;
            var pair = LocalGoatPair.Instance;
            foreach (var state in snapshot.goats)
            {
                var goat = state.id == "A" ? pair?.Primary : state.id == "B" ? pair?.Secondary : null;
                if (!goat) continue;
                if(!(goat.GetComponent<GoatLocalControl>()?.IsControlled??false))
                    goat.GetComponent<GoatSkin>()?.Set(state.skinNumber);
                var body = goat.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.position = state.position;
                body.rotation = state.rotation;
                goat.SetNetworkVelocity(state.velocity);
                goat.SetPredatorCarried(state.predatorCarried);
                goat.GetComponent<GoatHealth>()?.ApplyNetwork(state.health);
                goat.GetComponent<GoatBeerEffect>()?.ApplyNetwork(state.beerRemaining);
                goat.GetComponent<GoatHerdBehaviour>()?.Apply(state.herd);
                goat.GetComponent<GoatFurEffect>()?.Apply(state.coat);
                goat.GetComponent<GoatWaterDrinking>()?.Apply(state.drinkingRemaining);
                goat.GetComponent<GoatToiletAction>()?.Apply(state.toilet);
                goat.GetComponent<GoatPunchBuff>()?.ApplyNetworkRemaining(state.punchBuffRemaining);
                goat.GetComponent<GoatDescentInventory>()?.Apply(state.inventory);
                goat.GetComponent<GoatDescentTricks>()?.ApplyNetwork(state.birdSafeRemaining,
                    state.anchorRemaining, state.anchorPosition, state.madnessRemaining, state.madnessDirection,state.anchorLength);
                goat.GetComponent<GoatComedyActions>()?.ApplyNetwork(state.playingDead,state.gettingUp,state.ridingOn,state.rideLean);
                goat.GetComponent<GoatPhysicalBody>()?.ApplyNetworkPose(state.parts);
                goat.GetComponent<GoatVisualController>()?.ApplyNetworkPose(state.animation, state.facing);
                goat.GetComponent<GoatInteraction>()?.ApplyNetworkState(state.linked,
                    state.holding, state.interactionStatus);
                goat.GetComponent<RespawnController>()?.ApplyNetworkState(state.dead, false);
            }
        }
    }
}
