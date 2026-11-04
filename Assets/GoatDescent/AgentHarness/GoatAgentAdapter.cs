using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.Harness
{
    public sealed class GoatAgentAdapter : MonoBehaviour, IAgentGame
    {
        private GoatController primary;
        private GoatController secondary;
        private LevelGeneratedResources resources;
        private MountainSessionState session;
        private string currentScenario = "";
        private int currentSeed;
        private Vector3 walkDirection;
        private bool brakeHeld;
        private bool superHeld;
        private bool gripHeld;
        private float gripSide;
        private float gripClimb;
        private float jumpReleaseAt = -1f;
        private GoatController jumpGoat;

        public bool Ready
        {
            get
            {
                Resolve();
                return primary && secondary;
            }
        }

        public string MapSignature => session ? session.MapSignature : "";

        private void Update() => ReleasePendingJump();

        public void Reset(string scenarioId, int seed)
        {
            Resolve();
            if (!primary || !secondary)
                throw new System.InvalidOperationException("Сцена ещё не собрана: нет пары козлов");
            currentScenario = scenarioId ?? "";
            currentSeed = seed;
            Random.InitState(seed);
            ReleasePendingJump();
            brakeHeld = superHeld = gripHeld = false;
            gripSide = gripClimb = 0f;
            walkDirection = Vector3.zero;
            if (primary) primary.SetRemoteControl(true);
            if (secondary) secondary.SetRemoteControl(true);
            Stop(primary);
            Stop(secondary);
            ApplyStart(currentScenario);
            Physics.SyncTransforms();
        }

        public void Execute(GameCommand command)
        {
            if (command == null) throw new System.InvalidOperationException("Пустая команда");
            string type = (command.type ?? "").Trim().ToLowerInvariant();
            GoatController goat = Target(command.target);
            switch (type)
            {
                case "walk":
                    ApplyWalk(goat, new Vector3(command.x, command.y, command.z));
                    break;
                case "stop":
                    ApplyWalk(goat, Vector3.zero);
                    break;
                case "brake":
                    brakeHeld = command.value > .5f;
                    ApplyWalk(goat, walkDirection);
                    break;
                case "super":
                    superHeld = command.value > .5f;
                    ApplyWalk(goat, walkDirection);
                    break;
                case "grip":
                    gripHeld = command.value > .5f;
                    gripSide = command.x;
                    gripClimb = command.y;
                    goat.GetComponent<GoatCliffGrip>()?.SetRemoteInput(gripHeld, gripSide, gripClimb, CameraRight());
                    break;
                case "jump":
                    Jump(goat, command.seconds > 0f ? command.seconds : .15f);
                    break;
                case "shove":
                    if (!goat.GetComponent<GoatInteraction>().TryShove()) throw new System.InvalidOperationException("Толчок недоступен");
                    break;
                case "grab":
                    if (!goat.GetComponent<GoatInteraction>().TryGrab()) throw new System.InvalidOperationException("Сцепление недоступно");
                    break;
                case "release":
                    goat.GetComponent<GoatInteraction>()?.ReleaseGrip();
                    break;
                case "respawn":
                    goat.GetComponent<RespawnController>()?.Respawn();
                    break;
                case "teleport":
                    TeleportToPlatform(Mathf.RoundToInt(command.value));
                    break;
                case "wait":
                    break;
                default:
                    throw new System.InvalidOperationException("Неизвестная команда: " + command.type);
            }
        }

        public GameSnapshot Snapshot(int tick)
        {
            Resolve();
            var snapshot = new GameSnapshot
            {
                tick = tick,
                time = Time.time,
                seed = currentSeed,
                scenario = currentScenario,
                map = MapSignature,
                primary = Capture(primary, "A"),
                secondary = Capture(secondary, "B")
            };
            var route = resources ? resources.Route : null;
            if (route != null && route.Platforms != null) snapshot.platforms = route.Platforms.Length;
            var tracker = resources ? resources.GetComponent<LevelRunTracker>() : null;
            if (tracker)
            {
                snapshot.completed = tracker.Completed;
                snapshot.falls = tracker.Falls;
                snapshot.elapsed = tracker.ElapsedSeconds;
            }
            return snapshot;
        }

        private void Resolve()
        {
            var pair = LocalGoatPair.Instance;
            if (pair)
            {
                primary = pair.Primary;
                secondary = pair.Secondary;
            }
            if (!resources) resources = FindFirstObjectByType<LevelGeneratedResources>();
            if (!session) session = FindFirstObjectByType<MountainSessionState>();
        }

        private GoatController Target(string target)
        {
            return string.Equals(target, "b", System.StringComparison.OrdinalIgnoreCase) ? secondary : primary;
        }

        private void Stop(GoatController goat)
        {
            if (!goat) return;
            goat.SetRemoteMove(Vector3.zero);
            goat.SetRemoteMountainInput(false, false, 0f, 0f, CameraRight());
            goat.GetComponent<GoatJumpController>()?.ResetInput();
            goat.GetComponent<GoatCliffGrip>()?.ClearInput();
            goat.GetComponent<GoatInteraction>()?.CancelAll();
        }

        private void ApplyWalk(GoatController goat, Vector3 direction)
        {
            walkDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            goat.SetRemoteMove(walkDirection);
            Vector3 flat = walkDirection.sqrMagnitude > .0001f ? walkDirection.normalized : Vector3.zero;
            goat.SetRemoteMountainInput(brakeHeld, superHeld,
                Vector3.Dot(flat, CameraRight()), Vector3.Dot(flat, CameraForward()), CameraRight());
        }

        private void Jump(GoatController goat, float holdSeconds)
        {
            ReleasePendingJump();
            var jump = goat.GetComponent<GoatJumpController>();
            if (!jump) throw new System.InvalidOperationException("Нет GoatJumpController");
            jump.NetworkSpaceDown();
            jumpGoat = goat;
            jumpReleaseAt = Time.time + Mathf.Max(.05f, holdSeconds);
        }

        private void ReleasePendingJump()
        {
            if (jumpReleaseAt < 0f) return;
            jumpReleaseAt = -1f;
            GoatController goat = jumpGoat;
            jumpGoat = null;
            if (goat) goat.GetComponent<GoatJumpController>()?.NetworkSpaceUp();
        }

        private void ApplyStart(string scenarioId)
        {
            string id = string.IsNullOrEmpty(scenarioId) ? "summit" : scenarioId.ToLowerInvariant();
            if (id.StartsWith("platform:") || id.StartsWith("jump:"))
            {
                TeleportToPlatform(ParseIndex(id));
                return;
            }
            var pair = LocalGoatPair.Instance;
            if (!pair) return;
            switch (id)
            {
                case "pair_platform": pair.ResetScenario(0); return;
                case "pair_shove": pair.ResetScenario(1); return;
                case "pair_rescue": pair.ResetScenario(2); return;
                case "gap": pair.RemoveLowerSupport(); return;
                case "fragile": pair.ResetFragilePractice(); return;
                case "rock": pair.ResetRockPractice(); return;
                default: pair.ResetScenario(0); return;
            }
        }

        private static int ParseIndex(string id)
        {
            int separator = id.IndexOf(':');
            return separator < 0 ? 0 : int.Parse(id.Substring(separator + 1));
        }

        private void TeleportToPlatform(int index)
        {
            var route = resources ? resources.Route : null;
            if (route == null || route.Platforms == null || route.Platforms.Length == 0)
                throw new System.InvalidOperationException("Маршрут уровня недоступен");
            index = Mathf.Clamp(index, 0, route.Platforms.Length - 1);
            var platform = route.Platforms[index];
            Vector3 forward = index + 1 < route.Platforms.Length
                ? Vector3.ProjectOnPlane(route.Platforms[index + 1].Center - platform.Center, Vector3.up)
                : Vector3.forward;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            Teleport(primary, platform.Center + Vector3.up * 1.2f);
            Teleport(secondary, platform.Center + Vector3.up * 1.2f + side * 1.5f);
        }

        private static void Teleport(GoatController goat, Vector3 point)
        {
            if (!goat) return;
            var life = goat.GetComponent<RespawnController>();
            if (life) life.Teleport(point);
            else goat.transform.position = point;
        }

        private static GoatSnapshot Capture(GoatController goat, string label)
        {
            if (!goat) return null;
            var ground = goat.GetComponent<GoatGroundDetector>();
            var progress = goat.GetComponent<RouteProgressTracker>();
            var life = goat.GetComponent<RespawnController>();
            var cliffGrip = goat.GetComponent<GoatCliffGrip>();
            var grip = goat.GetComponent<GoatGripController>();
            var balance = goat.GetComponent<GoatSlopeBalance>();
            var interaction = goat.GetComponent<GoatInteraction>();
            Vector3 velocity = goat.Velocity;
            return new GoatSnapshot
            {
                label = label,
                position = goat.transform.position,
                velocity = velocity,
                speed = velocity.magnitude,
                grounded = goat.Grounded,
                dead = life && life.IsDead,
                slope = ground ? ground.SlopeAngle : 0f,
                progress = progress ? progress.CurrentProgress : 0f,
                gripping = (cliffGrip && cliffGrip.IsGripping) || (grip && grip.SuperActive),
                linked = interaction && interaction.IsLinked,
                slipping = balance && balance.IsSlipping,
                hooves = balance ? balance.HoovesHolding : 0
            };
        }

        private static Vector3 CameraRight()
        {
            var camera = Camera.main;
            return camera ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : Vector3.right;
        }

        private static Vector3 CameraForward()
        {
            var camera = Camera.main;
            return camera ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
        }
    }
}
