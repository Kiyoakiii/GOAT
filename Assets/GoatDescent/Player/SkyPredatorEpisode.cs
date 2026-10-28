using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Five circling birds; the host rolls once every thirty seconds for a hunt.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class SkyPredatorEpisode : MonoBehaviour
    {
        private enum FlightPhase : byte { Circling, Approaching, Diving, Struggling, Looping, Carrying, Returning, KnockedBack, Inspecting, WindApproaching, WindGusting }

        private sealed class Bird
        {
            public string Id;
            public Transform Root;
            public Transform LeftWing;
            public Transform RightWing;
            public Transform Visual;
            public EagleWindRibbons Wind;
            public EagleHoverHead HoverHead;
            public float HoverBlend, WindBlend;
            public Vector3 SnapshotPosition;
            public Quaternion SnapshotRotation;
            public bool ImpactVisible;
            public Vector3 PatrolCenter, PatrolOutward;
            public bool OrbitPlaced;
        }

        private sealed class HuntAttention { public int Started, WaitingRolls; }

        public static SkyPredatorEpisode Current { get; private set; }
        public string AlertText
        {
            get
            {
                string hunter = $"ОРЁЛ {activeBirdIndex + 1}";
                switch (phase)
                {
                    case FlightPhase.Approaching: return $"{hunter} ЛЕТИТ К КОЗЛУ {targetId} — заход {attemptNumber + 1}/{MaximumAttempts}";
                    case FlightPhase.Diving: return $"{hunter} ПИКИРУЕТ НА КОЗЛА {targetId} — заход {attemptNumber}/{MaximumAttempts}!";
                    case FlightPhase.Struggling:
                        float seconds = MountainAuthority.IsHost ? Mathf.Max(0f, phaseUntil - Time.time) : networkSecondsLeft;
                        return $"{hunter} ТЯНЕТ {targetId}, НО НАПАРНИК ДЕРЖИТ F! {seconds:0.0} с";
                    case FlightPhase.Looping: return $"{hunter} ОТПУСТИЛ КОЗЛА {targetId} И ДЕЛАЕТ НОВЫЙ КРУГ!";
                    case FlightPhase.Carrying: return $"{hunter} УНОСИТ КОЗЛА {targetId} В НЕБО!";
                    case FlightPhase.KnockedBack: return $"{hunter} ПОЛУЧИЛ КОПЫТОМ — ПЕРЬЯ ЛЕТЯТ!";
                    case FlightPhase.Inspecting: return $"{hunter} ПРОВЕРЯЕТ КОЗЛА {targetId} — НЕ ШЕВЕЛИСЬ!";
                    case FlightPhase.WindApproaching: return $"{hunter} ГОТОВИТ ПОРЫВ ВЕТРА!";
                    case FlightPhase.WindGusting: return $"{hunter} СДУВАЕТ КОЗЛА {targetId} — УПРИСЬ КОПЫТАМИ!";
                    case FlightPhase.Returning: return $"{hunter} ВОЗВРАЩАЕТСЯ К СТАЕ";
                    default: return "";
                }
            }
        }

        private const float DiveDuration = 1.55f;
        private const float StruggleDuration = 2.5f;
        private const float LoopDuration = 3.2f;
        private const float CarryDuration = 3.7f;
        private const float FlightSpeed = 25f;
        private const float FlightTurnSpeed = 540f;
        private const int MaximumAttempts = 3;
        private const float HuntRollInterval = 30f;
        private readonly Bird[] birds = new Bird[5];
        private readonly Material[] materials = new Material[7];
        private readonly List<GoatController> huntCandidates=new List<GoatController>();
        private readonly Dictionary<GoatController,HuntAttention> huntAttention=new Dictionary<GoatController,HuntAttention>();
        private static readonly RaycastHit[] flightHits=new RaycastHit[64];
        private static readonly float[] attackAngles={0f,30f,-30f,60f,-60f};
        private bool initialized;
        private float summitHeight;
        private Vector3 center, outward, pillarOrigin;
        private FlightPhase phase;
        private GoatController target, carried;
        private Rigidbody carryAnchorBody;
        private ConfigurableJoint carryJoint;
        private bool manualAttack;
        private int activeBirdIndex, attemptNumber, approachStage, returnStage;
        private string targetId = "";
        private float phaseStarted, phaseUntil, nextHuntRollAt, networkSecondsLeft;
        private float flightCeilingY, loopStartAngle;
        private Vector3 attackOutward, diveStart, carryStart, carryEnd;
        private Vector3 recoilStart, recoilDirection;
        private Quaternion recoilRotation;
        private Vector3 inspectionPoint;
        private bool inspectionLanded;
        private const float RecoilDuration = 1.65f;
        private const float WindDuration = 4.6f;
        private Vector3 windHoverPoint, windDirection;

        private void Awake() => Current = this;

        private void Update()
        {
            if (GoatSkinMenu.IsOpen || GoatSettingsMenu.BlocksInput
                || !MountainAuthority.IsHost || !Input.GetKeyDown(KeyCode.F12)) return;
            var pair = LocalGoatPair.Instance;
            if (pair && pair.Active && (!pair.NetworkMode || pair.Active == pair.Primary))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ForceWindAttack(pair.Active);
                else ForceAttack(pair.Active);
            }
        }

        public bool ForceWindAttack(GoatController goat)
        {
            if (!MountainAuthority.IsHost || !initialized || !ValidTarget(goat)
                || carried || phase == FlightPhase.KnockedBack) return false;
            return BeginWindAttack(goat, ClosestBirdTo(goat.transform.position), Time.time);
        }

        private bool BeginWindAttack(GoatController goat, int index, float now)
        {
            Vector3 front = Vector3.ProjectOnPlane(goat.transform.forward, Vector3.up).normalized;
            Vector3 hover = goat.transform.position + front * 4.2f + Vector3.up * 1.9f;
            if (!ClearFlightPath(hover, goat.transform.position + Vector3.up * 1.2f)) return false;
            target = goat; targetId = GoatId(goat); activeBirdIndex = index;
            attackOutward = front; windHoverPoint = hover; manualAttack = false; attemptNumber = 0;
            flightCeilingY = Mathf.Max(summitHeight + 14f, hover.y + 10f, birds[index].Root.position.y + 4f);
            approachStage = ClearFlightPath(birds[index].Root.position, hover) ? 2 : 0;
            phase = FlightPhase.WindApproaching; phaseStarted = now; phaseUntil = now + 12f;
            return true;
        }

        private static float WindEnvelope(float age)
            => Mathf.SmoothStep(0, 1, (age - .55f) / .45f)
                * (1f - Mathf.SmoothStep(0, 1, (age - WindDuration + .65f) / .65f));

        private void AdvanceWind(float now)
        {
            if (!ValidTarget(target) || target.GetComponent<GoatComedyActions>()?.PlayingDead == true)
            { BeginReturn(now); return; }
            var bird = birds[activeBirdIndex];
            if (phase == FlightPhase.WindApproaching)
            {
                if (now >= phaseUntil) { BeginReturn(now); return; }
                Vector3 point = target.transform.position + attackOutward * 4.2f + Vector3.up * 1.9f;
                if (!ClearFlightPath(point, target.transform.position + Vector3.up * 1.2f))
                { BeginReturn(now); return; }
                if (approachStage == 0)
                {
                    if (FlyToward(new Vector3(bird.Root.position.x, flightCeilingY, bird.Root.position.z), FlightSpeed)) approachStage = 1;
                    return;
                }
                if (approachStage == 1)
                {
                    if (FlyToward(new Vector3(point.x, flightCeilingY, point.z), FlightSpeed)) approachStage = 2;
                    return;
                }
                if (!ClearFlightPath(bird.Root.position, point)) { BeginReturn(now); return; }
                if (!FlyToward(point, FlightSpeed * .7f)) return;
                windHoverPoint = point;
                windDirection = Vector3.ProjectOnPlane(target.transform.position - point, Vector3.up).normalized;
                phase = FlightPhase.WindGusting; phaseStarted = now; phaseUntil = now + WindDuration;
            }
            float age = now - phaseStarted;
            SetBirdPose(activeBirdIndex, windHoverPoint + Vector3.up * (Mathf.Sin(age * 10.5f) * .055f), windDirection);
            // Commit to a lane: the goat can step sideways out of the stream.
            Vector3 chest = target.transform.position + Vector3.up * 1.2f;
            Vector3 offset = chest - bird.Root.position;
            float distance = Vector3.Dot(offset, windDirection);
            Vector3 radial = offset - windDirection * distance;
            if (distance > .5f && distance < 7f && radial.magnitude < 1.8f
                && ClearPath(bird.Root.position, chest))
            {
                float pulse = .65f + .35f * Mathf.Max(0, Mathf.Cos(age * 10.5f));
                float falloff = 1f - Mathf.SmoothStep(0, 1, (distance - 5f) / 2f);
                target.ApplyWind(windDirection * (.75f * pulse * WindEnvelope(age) * falloff));
            }
            if (now >= phaseUntil) BeginReturn(now);
        }

        /// <summary>Debug shortcut: launch a hunt from the flock toward the specified goat.</summary>
        public bool ForceAttack(GoatController goat)
        {
            if (!MountainAuthority.IsHost || !initialized || birds[0] == null || !goat
                || phase == FlightPhase.Carrying || phase == FlightPhase.KnockedBack) return false;
            var interaction = goat.GetComponent<GoatInteraction>();
            if (interaction && interaction.IsHolding && interaction.Partner)
                goat = interaction.Partner.GetComponent<GoatController>();
            if (!ValidTarget(goat)) return false;
            if (goat.GetComponent<GoatComedyActions>()?.PlayingDead == true)
            { ObservePretence(goat); return true; }
            if (phase == FlightPhase.Circling)
                activeBirdIndex = ClosestBirdTo(goat.transform.position);
            BeginHunt(goat, activeBirdIndex, true, Time.time);
            Debug.Log($"SKY_PREDATOR_MANUAL bird={birds[activeBirdIndex].Id} target={targetId}");
            return true;
        }

        public void Initialize(Vector3 flockCenter, Vector3 outwardDirection, float summitY,Transform pillar=null)
        {
            initialized = true;
            center = flockCenter;
            summitHeight = summitY;
            pillarOrigin=pillar ? pillar.position : transform.parent ? transform.parent.position : Vector3.zero;
            outward = Vector3.ProjectOnPlane(outwardDirection, Vector3.up).normalized;
            if (outward.sqrMagnitude < .01f) outward = Vector3.forward;
            materials[0] = MakeMaterial("Predator feathers", new Color(.19f, .12f, .09f));
            materials[1] = MakeMaterial("Predator wing tips", new Color(.08f, .075f, .07f));
            materials[2] = MakeMaterial("Predator beak", new Color(.96f, .65f, .17f));
            materials[3] = MakeMaterial("Predator ivory plumage", new Color(.94f, .9f, .74f));
            materials[4] = MakeMaterial("Predator eye whites", Color.white);
            materials[5] = MakeMaterial("Predator warm feather edges", new Color(.38f, .23f, .13f));
            materials[6] = new Material(Resources.Load<Shader>("EaglePlumage"))
                { name = "Blender eagle vertex plumage", color = Color.white };
            for (int i = 0; i < birds.Length; i++) birds[i] = CreateBird(i);
            nextHuntRollAt = Time.time + HuntRollInterval;
            UpdateOrbit(Time.time);
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            return material;
        }

        private Bird CreateBird(int index)
        {
            var root = new GameObject($"SKY-{index + 1:00} bird");
            root.transform.SetParent(transform);
            var bird = new Bird { Id = $"SKY-{index + 1:00}", Root = root.transform,
                PatrolCenter=center,PatrolOutward=outward };
            bird.Visual = new GameObject("Articulated hover posture").transform;
            bird.Visual.SetParent(root.transform, false);
            CartoonEagleVisual.Build(bird.Visual, materials, index, out bird.LeftWing, out bird.RightWing);
            bird.HoverHead = bird.Visual.gameObject.AddComponent<EagleHoverHead>();
            var wind = new GameObject("Eagle wind stream");
            wind.transform.SetParent(root.transform, false);
            bird.Wind = wind.AddComponent<EagleWindRibbons>();
            return bird;
        }

        private void FixedUpdate()
        {
            if (!initialized || birds[0] == null || !MountainAuthority.IsHost) return;
            float now = Time.time;
            UpdatePatrol();
            if (now >= nextHuntRollAt)
            {
                nextHuntRollAt = now + HuntRollInterval;
                RollForHunt(now);
            }
            UpdateOrbit(now);
            if (target && target.GetComponent<GoatComedyActions>()?.PlayingDead == true
                && (phase == FlightPhase.Approaching || phase == FlightPhase.Diving || phase == FlightPhase.Looping))
                BeginInspection(target, activeBirdIndex, now);
            switch (phase)
            {
                case FlightPhase.Circling:
                    break;
                case FlightPhase.Approaching:
                    if (!CanContinueHunt()) { BeginReturn(now); break; }
                    AdvanceApproach(now);
                    break;
                case FlightPhase.Diving:
                    if (!CanContinueHunt()) { BeginReturn(now); break; }
                    float diveT = Mathf.Clamp01((now - phaseStarted) / DiveDuration);
                    Vector3 aim = target.transform.position + Vector3.up * 1.35f;
                    Vector3 divePoint = Vector3.Lerp(diveStart, aim, diveT * diveT * (3f - 2f * diveT));
                    divePoint += Vector3.up * Mathf.Sin(diveT * Mathf.PI) * 1.2f;
                    SetBirdPose(activeBirdIndex, divePoint, aim - divePoint);
                    if (diveT > .55f && Vector3.Distance(divePoint, aim) < 1.5f
                        && (manualAttack || (IsExposed(target) && ClearPath(divePoint, aim))))
                    {
                        var interaction = target.GetComponent<GoatInteraction>();
                        if (interaction && interaction.IsLinked && interaction.Partner)
                            BeginStruggle(now);
                        else CaptureGoat(now);
                        break;
                    }
                    if (diveT >= 1f) RetryOrReturn(now);
                    break;
                case FlightPhase.Struggling:
                    if (!CanContinueHunt()) { BeginReturn(now); break; }
                    var grip = target.GetComponent<GoatInteraction>();
                    Vector3 targetHead = target.transform.position + Vector3.up * 1.35f;
                    float separation = Vector3.Distance(birds[activeBirdIndex].Root.position, targetHead);
                    if (!grip || !grip.IsLinked || !grip.Partner)
                    {
                        CaptureGoat(now);
                        break;
                    }
                    if (now - phaseStarted > .25f && separation > 3.4f)
                    { RetryOrReturn(now); break; }
                    Vector3 tugPoint = target.transform.position + Vector3.up * 1.55f
                        + attackOutward * .45f
                        + Vector3.Cross(Vector3.up, attackOutward) * (Mathf.Sin((now - phaseStarted) * 9f) * .35f);
                    FlyToward(tugPoint, 18f);
                    target.GetComponent<GoatPhysicalBody>()?.ApplyEagleTug(
                        birds[activeBirdIndex].Root.position, attackOutward);
                    if (now >= phaseUntil)
                    {
                        Debug.Log($"SKY_PREDATOR_RESISTED bird={birds[activeBirdIndex].Id} target={targetId} attempt={attemptNumber}");
                        RetryOrReturn(now);
                    }
                    break;
                case FlightPhase.Looping:
                    if (!CanContinueHunt()) { BeginReturn(now); break; }
                    float angle = loopStartAngle + (now - phaseStarted) * (Mathf.PI * 2f / LoopDuration);
                    Vector3 loopPoint = target.transform.position + attackOutward * (10f + Mathf.Sin(angle) * 4f)
                        + Vector3.up * 8f
                        + Vector3.Cross(Vector3.up, attackOutward) * (Mathf.Cos(angle) * 5f);
                    FlyToward(loopPoint, FlightSpeed);
                    if (now >= phaseUntil)
                    {
                        attackOutward = OutwardFromPillar(target.transform.position);
                        approachStage = 2;
                        phase = FlightPhase.Approaching;
                        phaseStarted = now;
                    }
                    break;
                case FlightPhase.Carrying:
                    if (!carried) { BeginReturn(now); break; }
                    if (carried.GetComponent<RespawnController>()?.IsDead == true)
                    { ReleaseGoat(carried, false); break; }
                    float carryT = Mathf.Clamp01((now - phaseStarted) / CarryDuration);
                    Vector3 carryPoint = Vector3.Lerp(carryStart, carryEnd, carryT * carryT * (3f - 2f * carryT));
                    SetBirdPose(activeBirdIndex, carryPoint, carryEnd - carryPoint);
                    if (carryAnchorBody) carryAnchorBody.MovePosition(carryPoint);
                    if (carryT >= 1f) ReleaseGoat(carried, true);
                    break;
                case FlightPhase.KnockedBack:
                    AdvanceRecoil(now);
                    break;
                case FlightPhase.Inspecting:
                    AdvanceInspection(now);
                    break;
                case FlightPhase.WindApproaching:
                case FlightPhase.WindGusting:
                    AdvanceWind(now);
                    break;
                case FlightPhase.Returning:
                    AdvanceReturn(now);
                    break;
            }
        }

        private void BeginHunt(GoatController goat, int birdIndex, bool forced, float now)
        {
            target = goat;
            targetId = GoatId(goat);
            activeBirdIndex = birdIndex;
            manualAttack = forced;
            attemptNumber = 0;
            attackOutward = OutwardFromPillar(goat.transform.position);
            if(TryFindAttackLane(goat,out var launchPoint,out var laneOutward)) attackOutward=laneOutward;
            else launchPoint=goat.transform.position+attackOutward*7f+Vector3.up*8f;
            flightCeilingY = Mathf.Max(summitHeight + 14f, goat.transform.position.y + 11f,
                birds[birdIndex].Root.position.y + 6f);
            // A bird already patrolling this goat can approach at its current
            // altitude. Only an obstructed transfer takes the route above the summit.
            approachStage=ClearFlightPath(birds[birdIndex].Root.position,launchPoint) ? 2 : 0;
            phase = FlightPhase.Approaching;
            phaseStarted = now;
            phaseUntil = 0f;
            Debug.Log($"SKY_PREDATOR_HUNT_BEGIN bird={birds[birdIndex].Id} target={targetId} forced={forced}");
        }

        public void ObservePretence(GoatController goat)
        {
            if (!MountainAuthority.IsHost || !initialized || !goat || carried
                || phase == FlightPhase.KnockedBack || !ValidTarget(goat)) return;
            if (phase != FlightPhase.Circling && phase != FlightPhase.Returning && target != goat) return;
            int index = target == goat ? activeBirdIndex : ClosestBirdTo(goat.transform.position);
            BeginInspection(goat,index,Time.time);
        }
        public bool IsInspecting(GoatController goat) => phase == FlightPhase.Inspecting && target == goat;

        private void BeginInspection(GoatController goat, int index, float now)
        {
            target=goat; targetId=GoatId(goat); activeBirdIndex=index;
            phase=FlightPhase.Inspecting; phaseStarted=now; phaseUntil=now+15f;
            inspectionLanded=false; manualAttack=false;
            attackOutward=OutwardFromPillar(goat.transform.position);
            inspectionPoint=goat.transform.position+attackOutward*1.9f+Vector3.up*1.3f;
            // Prefer a real patch of ground for the bird's feet. At a cliff edge it hovers instead.
            float nearest=float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(inspectionPoint+Vector3.up*2f,Vector3.down,5f,~0,QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GoatController>() || hit.collider.GetComponentInParent<GoatPhysicsPart>()
                    || hit.normal.y<.6f || hit.distance>=nearest) continue;
                nearest=hit.distance; inspectionPoint=hit.point+Vector3.up*.68f;
            }
            Debug.Log("EAGLE_INSPECTION_BEGIN target="+targetId);
        }

        private void AdvanceInspection(float now)
        {
            if (!target || target.GetComponent<RespawnController>()?.IsDead==true) { BeginReturn(now); return; }
            var comedy=target.GetComponent<GoatComedyActions>();
            if (!comedy || !comedy.PlayingDead) { ExposePretence(target); return; }
            if (!inspectionLanded)
            {
                if (FlyToward(inspectionPoint,FlightSpeed*.6f))
                { inspectionLanded=true; phaseUntil=now+2.8f; }
                else if (now>=phaseUntil) BeginReturn(now);
                return;
            }
            Vector3 direction=target.transform.position+Vector3.up*.7f-inspectionPoint;
            SetBirdPose(activeBirdIndex,inspectionPoint,direction);
            if (now>=phaseUntil) { comedy.FooledEagle(); BeginReturn(now); }
        }

        public void ExposePretence(GoatController goat)
        {
            if (!MountainAuthority.IsHost || phase!=FlightPhase.Inspecting || target!=goat) return;
            // Attack from the bird's actual checking position; never teleport above the goat.
            phase=FlightPhase.Diving; phaseStarted=Time.time; phaseUntil=Time.time+DiveDuration;
            diveStart=birds[activeBirdIndex].Root.position; manualAttack=true; attemptNumber=1;
            Debug.Log("EAGLE_PRETENCE_EXPOSED target="+targetId);
        }

        private bool CanContinueHunt()
            => ValidTarget(target) && (manualAttack || IsExposed(target));

        private void AdvanceApproach(float now)
        {
            var bird = birds[activeBirdIndex];
            if(TryFindAttackLane(target,out var launchPoint,out var laneOutward)) attackOutward=laneOutward;
            else launchPoint=target.transform.position+attackOutward*7f+Vector3.up*8f;
            if (approachStage == 0)
            {
                Vector3 climbPoint = new Vector3(bird.Root.position.x, flightCeilingY, bird.Root.position.z);
                if (FlyToward(climbPoint, FlightSpeed)) approachStage = 1;
                return;
            }
            if (approachStage == 1)
            {
                Vector3 highPoint = new Vector3(launchPoint.x, flightCeilingY, launchPoint.z);
                if (FlyToward(highPoint, FlightSpeed)) approachStage = 2;
                return;
            }
            if (!FlyToward(launchPoint, FlightSpeed) || now - phaseStarted < 1f) return;
            attemptNumber++;
            phase = FlightPhase.Diving;
            phaseStarted = now;
            phaseUntil = now + DiveDuration;
            diveStart = bird.Root.position;
            Debug.Log($"SKY_PREDATOR_DIVE bird={bird.Id} target={targetId} attempt={attemptNumber}/{MaximumAttempts}");
        }

        private bool FlyToward(Vector3 destination, float speed)
        {
            var bird = birds[activeBirdIndex];
            Vector3 from = bird.Root.position;
            Vector3 next = Vector3.MoveTowards(from, destination, speed * Time.fixedDeltaTime);
            SetBirdPose(activeBirdIndex, next, destination - from);
            return (next - destination).sqrMagnitude < .04f;
        }

        private void BeginStruggle(float now)
        {
            phase = FlightPhase.Struggling;
            phaseStarted = now;
            phaseUntil = now + StruggleDuration;
            Debug.Log($"SKY_PREDATOR_STRUGGLE bird={birds[activeBirdIndex].Id} target={targetId} attempt={attemptNumber}");
        }

        private void RetryOrReturn(float now)
        {
            if (attemptNumber >= MaximumAttempts || !CanContinueHunt())
            { BeginReturn(now); return; }
            phase = FlightPhase.Looping;
            phaseStarted = now;
            phaseUntil = now + LoopDuration;
            Vector3 loopCenter = target.transform.position + attackOutward * 10f + Vector3.up * 8f;
            Vector3 offset = birds[activeBirdIndex].Root.position - loopCenter;
            Vector3 attackSideways = Vector3.Cross(Vector3.up, attackOutward);
            loopStartAngle = Mathf.Atan2(Vector3.Dot(offset, attackOutward) / 4f,
                Vector3.Dot(offset, attackSideways) / 5f);
            Debug.Log($"SKY_PREDATOR_LOOP bird={birds[activeBirdIndex].Id} target={targetId} nextAttempt={attemptNumber + 1}");
        }

        private void BeginReturn(float now)
        {
            phase = FlightPhase.Returning;
            phaseStarted = now;
            phaseUntil = 0f;
            returnStage=ClearFlightPath(birds[activeBirdIndex].Root.position,OrbitPoint(activeBirdIndex,now)) ? 2 : 0;
            target = null;
            targetId = "";
            manualAttack = false;
            flightCeilingY = Mathf.Max(flightCeilingY, summitHeight + 14f,
                birds[activeBirdIndex].Root.position.y + 3f);
            Debug.Log($"SKY_PREDATOR_RETURN bird={birds[activeBirdIndex].Id} attempts={attemptNumber}");
        }

        private void AdvanceReturn(float now)
        {
            var bird = birds[activeBirdIndex];
            Vector3 flockPoint = OrbitPoint(activeBirdIndex, now);
            if (returnStage == 0)
            {
                Vector3 climbPoint = new Vector3(bird.Root.position.x, flightCeilingY, bird.Root.position.z);
                if (FlyToward(climbPoint, FlightSpeed)) returnStage = 1;
                return;
            }
            if (returnStage == 1)
            {
                Vector3 highPoint = new Vector3(flockPoint.x, flightCeilingY, flockPoint.z);
                if (FlyToward(highPoint, FlightSpeed)) returnStage = 2;
                return;
            }
            if (FlyToward(flockPoint, FlightSpeed)) phase = FlightPhase.Circling;
        }

        private int ClosestBirdTo(Vector3 point)
        {
            int best = 0;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < birds.Length; i++)
            {
                float candidate = (birds[i].Root.position - point).sqrMagnitude;
                if (candidate >= distance) continue;
                best = i;
                distance = candidate;
            }
            return best;
        }

        private Vector3 OutwardFromPillar(Vector3 position)
        {
            Vector3 radial = position-pillarOrigin;
            radial.y = 0f;
            return radial.sqrMagnitude > .1f ? radial.normalized : outward;
        }

        private void RollForHunt(float now)
        {
            if (phase != FlightPhase.Circling)
            {
                Debug.Log("SKY_PREDATOR_ROLL hunt=false reason=bird_busy");
                return;
            }
            CollectHuntCandidates();
            foreach(var entry in huntAttention)
                if(!huntCandidates.Contains(entry.Key)) entry.Value.WaitingRolls=0;
            foreach(var goat in huntCandidates) AttentionFor(goat).WaitingRolls++;
            GoatController candidate = FindExposedTarget();
            if (!candidate)
            {
                Debug.Log("SKY_PREDATOR_ROLL hunt=false reason=no_exposed_goat");
                return;
            }
            var settings = GoatHerdSettings.Get();
            bool lonely = candidate.GetComponent<GoatHerdBehaviour>()?.Isolated == true;
            float chance = lonely ? settings.lonelyHuntChance : settings.normalHuntChance;
            var attention=AttentionFor(candidate);
            bool overdue=settings.maxHuntRollsWithoutAttack>0 && attention.WaitingRolls>=settings.maxHuntRollsWithoutAttack;
            float roll = Random.value;
            if (!overdue && roll >= chance)
            {
                Debug.Log($"SKY_PREDATOR_ROLL target={GoatId(candidate)} eligible={huntCandidates.Count} waiting={attention.WaitingRolls} chance={chance:0.00} value={roll:0.000} lonely={lonely} hunt=false");
                return;
            }
            activeBirdIndex=ClosestBirdTo(candidate.transform.position);
            attention.Started++;attention.WaitingRolls=0;
            // Alternate the new harassment with the existing capture attack.
            if (attention.Started % 2 == 0 || !BeginWindAttack(candidate, activeBirdIndex, now))
                BeginHunt(candidate, activeBirdIndex, false, now);
            Debug.Log($"SKY_PREDATOR_ROLL chance={chance:0.00} value={roll:0.000} lonely={lonely} overdue={overdue} hunt=true bird={birds[activeBirdIndex].Id} target={targetId}");
        }

        private HuntAttention AttentionFor(GoatController goat)
        {
            if(!huntAttention.TryGetValue(goat,out var attention))
                huntAttention.Add(goat,attention=new HuntAttention());
            return attention;
        }

        private void CollectHuntCandidates()
        {
            huntCandidates.Clear();
            foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
                if(Eligible(goat)) huntCandidates.Add(goat);
            huntCandidates.Sort((a,b)=>string.CompareOrdinal(GoatId(a),GoatId(b)));
        }

        private GoatController FindExposedTarget()
        {
            var settings=GoatHerdSettings.Get();
            int mostWaiting=0;
            if(settings.maxHuntRollsWithoutAttack>0)
                foreach(var goat in huntCandidates)
                    mostWaiting=Mathf.Max(mostWaiting,AttentionFor(goat).WaitingRolls);
            bool overdue=mostWaiting>=settings.maxHuntRollsWithoutAttack && settings.maxHuntRollsWithoutAttack>0;
            GoatController chosen=null;
            float leastAttention=float.PositiveInfinity,totalWeight=0;
            foreach(var goat in huntCandidates)
            {
                var attention=AttentionFor(goat);
                if(overdue && attention.WaitingRolls<mostWaiting) continue;
                float weight=goat.GetComponent<GoatHerdBehaviour>()?.Isolated==true ? Mathf.Max(1,settings.lonelyTargetWeight) : 1;
                // Equal isolation means equal attention, independent of distance
                // from the summit or which goat owns the local camera.
                float score=attention.Started/weight;
                if(!chosen || score<leastAttention-.0001f)
                {chosen=goat;leastAttention=score;totalWeight=weight;}
                else if(Mathf.Abs(score-leastAttention)<.0001f)
                {
                    totalWeight+=weight;
                    if(Random.value<weight/totalWeight) chosen=goat;
                }
            }
            return chosen;
        }

        private bool Eligible(GoatController goat)
            => ValidTarget(goat) && IsExposed(goat);

        private static bool ValidTarget(GoatController goat)
            => goat && !goat.IsPredatorCarried
                && goat.GetComponent<GoatDescentTricks>()?.BirdsAvoid != true
                && goat.GetComponent<RespawnController>()?.IsDead != true;

        public bool RepelNearby(GoatController goat, float range)
        {
            if (!MountainAuthority.IsHost || !initialized || !goat) return false;
            if (phase == FlightPhase.KnockedBack) return false;
            int hitIndex = -1;
            float nearest = range * range;
            for (int i = 0; i < birds.Length; i++)
            {
                if (birds[i] == null) continue;
                if (phase != FlightPhase.Circling && i != activeBirdIndex) continue;
                float distance = (birds[i].Root.position - goat.transform.position - Vector3.up).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; hitIndex = i;
            }
            if (hitIndex < 0) return false;
            // A single hunting bird owns the carry joint: release before changing its index.
            if (carried) ReleaseGoat(carried, false);
            activeBirdIndex = hitIndex;
            target = null; targetId = ""; manualAttack = false;
            var struck = birds[hitIndex];
            recoilStart = struck.Root.position;
            recoilDirection = Vector3.ProjectOnPlane(recoilStart - goat.transform.position, Vector3.up).normalized;
            if (recoilDirection.sqrMagnitude < .01f) recoilDirection = -goat.transform.forward;
            recoilRotation = struck.Root.rotation;
            phase = FlightPhase.KnockedBack;
            phaseStarted = Time.time; phaseUntil = Time.time + RecoilDuration;
            struck.ImpactVisible = true;
            CartoonEagleVisual.ShedFeathers(struck.Root, materials[3]);
            Debug.Log("EAGLE_ROUNDHOUSE_REPEL goat=" + goat.name);
            return true;
        }

        private void AdvanceRecoil(float now)
        {
            float t = Mathf.Clamp01((now - phaseStarted) / RecoilDuration);
            float retreat = 6f * (1f - Mathf.Exp(-9f * t));
            float drop = -1.25f * Mathf.Sin(Mathf.Clamp01((t - .12f) / .88f) * Mathf.PI);
            float pop = .65f * Mathf.Sin(Mathf.Clamp01(t / .25f) * Mathf.PI);
            Vector3 destination = recoilStart + recoilDirection * retreat + Vector3.up * (drop + pop);
            var bird = birds[activeBirdIndex];
            Vector3 movement = destination - bird.Root.position;
            // Stop at terrain before the recoil path enters a cliff or the floor.
            if (movement.sqrMagnitude > .0001f)
            {
                float safeTravel = movement.magnitude;
                foreach (var hit in Physics.SphereCastAll(bird.Root.position, .7f, movement.normalized,
                    safeTravel, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<GoatController>()
                        || hit.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                    safeTravel = Mathf.Min(safeTravel, Mathf.Max(0f, hit.distance - .15f));
                }
                destination = bird.Root.position + movement.normalized * safeTravel;
            }
            float recover = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .4f) / .6f));
            Quaternion stunned = recoilRotation * Quaternion.Euler(-24f, 0, 35f);
            bird.Root.SetPositionAndRotation(destination,
                Quaternion.Slerp(stunned, Quaternion.LookRotation(recoilDirection + Vector3.up * .3f), recover));
            bird.SnapshotPosition = bird.Root.position; bird.SnapshotRotation = bird.Root.rotation;
            if (t >= 1f) { bird.ImpactVisible = false; BeginReturn(now); }
        }

        private bool IsExposed(GoatController goat)
            => goat && TryFindAttackLane(goat,out _,out _);

        private bool TryFindAttackLane(GoatController goat,out Vector3 launchPoint,out Vector3 direction)
        {
            launchPoint=direction=Vector3.zero;if(!goat) return false;
            Vector3 head = goat.transform.position + Vector3.up * 1.35f;
            Vector3 radial=OutwardFromPillar(goat.transform.position);
            foreach(float angle in attackAngles)
            {
                Vector3 lane=Quaternion.AngleAxis(angle,Vector3.up)*radial;
                Vector3 launch=goat.transform.position+lane*7f+Vector3.up*8f;
                if(!ClearFlightPath(launch,head)) continue;
                launchPoint=launch;direction=lane;return true;
            }
            return false;
        }

        private static bool ClearFlightPath(Vector3 start,Vector3 end)
        {
            Vector3 span=end-start;if(span.sqrMagnitude<.001f) return true;
            int count=Physics.SphereCastNonAlloc(start,.45f,span.normalized,flightHits,span.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(count==flightHits.Length) return false;
            for(int i=0;i<count;i++)
            {
                var collider=flightHits[i].collider;if(!collider) continue;
                if(collider.GetComponentInParent<GoatController>() || collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                return false;
            }
            return true;
        }

        private static bool ClearPath(Vector3 start, Vector3 end)
        {
            Vector3 offset = end - start;
            if (offset.sqrMagnitude < .001f) return true;
            foreach (var hit in Physics.RaycastAll(start, offset.normalized, offset.magnitude,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GoatController>()
                    || hit.collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                return false;
            }
            return true;
        }

        private void CaptureGoat(float now)
        {
            if (!target) { BeginReturn(now); return; }
            var body = target.GetComponent<Rigidbody>();
            if (!body) { BeginReturn(now); return; }
            carried = target;
            carried.GetComponent<GoatComedyActions>()?.Clear();
            carried.GetComponent<GoatInteraction>()?.CancelAll();
            carried.GetComponent<GoatCliffGrip>()?.BreakGrip(CarryDuration + 1f);
            carried.GetComponent<GoatJumpController>()?.ResetInput();
            carried.SetPredatorCarried(true);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = false;
            body.useGravity = true;
            var anchor = new GameObject("Eagle carry anchor");
            anchor.transform.position = birds[activeBirdIndex].Root.position;
            carryAnchorBody = anchor.AddComponent<Rigidbody>();
            carryAnchorBody.isKinematic = true;
            carryAnchorBody.useGravity = false;
            carryAnchorBody.interpolation = RigidbodyInterpolation.Interpolate;
            carryJoint = carried.gameObject.AddComponent<ConfigurableJoint>();
            carryJoint.autoConfigureConnectedAnchor = false;
            carryJoint.connectedBody = carryAnchorBody;
            carryJoint.anchor = Vector3.up * 1.25f;
            carryJoint.connectedAnchor = Vector3.zero;
            carryJoint.xMotion = carryJoint.yMotion = carryJoint.zMotion = ConfigurableJointMotion.Limited;
            carryJoint.angularXMotion = carryJoint.angularYMotion = carryJoint.angularZMotion = ConfigurableJointMotion.Free;
            carryJoint.linearLimit = new SoftJointLimit { limit = .28f, contactDistance = .03f };
            carryJoint.linearLimitSpring = new SoftJointLimitSpring { spring = 4600f, damper = 360f };
            carryJoint.breakForce = Mathf.Infinity;
            carryJoint.breakTorque = Mathf.Infinity;
            carryJoint.enablePreprocessing = false;
            body.WakeUp();
            phase = FlightPhase.Carrying;
            phaseStarted = now;
            phaseUntil = now + CarryDuration;
            carryStart = birds[activeBirdIndex].Root.position;
            carryEnd = carryStart + attackOutward * 11f + Vector3.up * 16f;
            Debug.Log($"SKY_PREDATOR_CAPTURE bird={birds[activeBirdIndex].Id} target={targetId} position={body.position}");
        }

        public void ReleaseGoat(GoatController goat, bool launch)
        {
            if (!goat || goat != carried) return;
            string releasedId = targetId;
            if (carryJoint)
            {
                carryJoint.xMotion = carryJoint.yMotion = carryJoint.zMotion = ConfigurableJointMotion.Free;
                carryJoint.connectedBody = null;
                Destroy(carryJoint);
                carryJoint = null;
            }
            if (carryAnchorBody) { Destroy(carryAnchorBody.gameObject); carryAnchorBody = null; }
            var body = goat.GetComponent<Rigidbody>();
            if (body)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = launch
                    ? Vector3.ClampMagnitude(body.linearVelocity, 18f) + attackOutward * 2f + Vector3.up * 1.5f
                    : body.linearVelocity;
                body.angularVelocity = Vector3.zero;
            }
            goat.SetPredatorCarried(false);
            carried = null;
            BeginReturn(Time.time);
            Debug.Log($"SKY_PREDATOR_RELEASE target={releasedId} launched={launch}");
        }

        public void OnLocalAuthorityRestored()
        {
            if (carried) return;
            nextHuntRollAt = Time.time + HuntRollInterval;
            if (phase != FlightPhase.Circling) BeginReturn(Time.time);
        }

        private Vector3 OrbitPoint(int index, float time)
        {
            var bird=birds[index];
            Vector3 side=Vector3.Cross(Vector3.up,bird.PatrolOutward);
            float angle = time * (index % 2 == 0 ? .36f : -.31f)
                + index * (Mathf.PI * 2f / birds.Length);
            return bird.PatrolCenter + bird.PatrolOutward * (16f + Mathf.Cos(angle) * 6f)
                + side * (Mathf.Sin(angle) * 8f)
                + Vector3.up * (16f + Mathf.Sin(angle * 1.4f) * 1.3f + index % 3 * 2f);
        }

        private void UpdatePatrol()
        {
            var pair=LocalGoatPair.Instance;if(!pair) return;
            for(int i=0;i<birds.Length;i++)
            {
                var subject=i%2==0 ? pair.Primary : pair.Secondary;
                if(!subject || subject.GetComponent<RespawnController>()?.IsDead==true)
                    subject=i%2==0 ? pair.Secondary : pair.Primary;
                if(!subject || subject.GetComponent<RespawnController>()?.IsDead==true) continue;
                var bird=birds[i];
                bird.PatrolCenter=Vector3.MoveTowards(bird.PatrolCenter,subject.transform.position,10f*Time.fixedDeltaTime);
                bird.PatrolOutward=Vector3.RotateTowards(bird.PatrolOutward,OutwardFromPillar(subject.transform.position),1.4f*Time.fixedDeltaTime,0).normalized;
            }
        }

        private void UpdateOrbit(float time)
        {
            for (int i = 0; i < birds.Length; i++)
            {
                if (i == activeBirdIndex && phase != FlightPhase.Circling) continue;
                Vector3 point = OrbitPoint(i, time);
                if(birds[i].OrbitPlaced) point=Vector3.MoveTowards(birds[i].Root.position,point,FlightSpeed*Time.fixedDeltaTime);
                birds[i].OrbitPlaced=true;
                SetBirdPose(i, point, OrbitPoint(i, time + .05f) - point);
            }
        }

        private void SetBirdPose(int index, Vector3 position, Vector3 direction)
        {
            var bird = birds[index];
            bird.Root.position = position;
            if (direction.sqrMagnitude > .01f)
                bird.Root.rotation = Quaternion.RotateTowards(bird.Root.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    FlightTurnSpeed * Time.fixedDeltaTime);
            bird.SnapshotPosition = bird.Root.position;
            bird.SnapshotRotation = bird.Root.rotation;
        }

        private void LateUpdate()
        {
            if (birds[0] == null) return;
            for (int i = 0; i < birds.Length; i++)
            {
                var bird = birds[i];
                if (!MountainAuthority.IsHost)
                {
                    bird.Root.position = Vector3.Lerp(bird.Root.position, bird.SnapshotPosition,
                        1f - Mathf.Exp(-15f * Time.deltaTime));
                    bird.Root.rotation = Quaternion.Slerp(bird.Root.rotation, bird.SnapshotRotation,
                        1f - Mathf.Exp(-15f * Time.deltaTime));
                }
                bool working = i == activeBirdIndex
                    && (phase == FlightPhase.Diving || phase == FlightPhase.Struggling);
                bool gusting = i == activeBirdIndex && phase == FlightPhase.WindGusting;
                float windAge = WindDuration - (MountainAuthority.IsHost ? phaseUntil - Time.time : networkSecondsLeft);
                bird.HoverBlend = Mathf.MoveTowards(bird.HoverBlend, gusting ? 1f : 0f, Time.deltaTime * 3f);
                bird.Visual.localRotation = Quaternion.Euler(-65f * bird.HoverBlend, 0, 0);
                bird.HoverHead.SetHover(bird.HoverBlend);
                bird.WindBlend = Mathf.MoveTowards(bird.WindBlend,
                    gusting ? WindEnvelope(windAge) * (.8f + .2f * Mathf.Cos(windAge * 10.5f)) : 0f, Time.deltaTime * 4f);
                bird.Wind.SetFlow(bird.Root.position, bird.Root.rotation, bird.WindBlend, Time.time);
                float flap = Mathf.Sin(Time.time * (working ? 13f : 5f) + i * 1.4f) * 16f;
                bool stunned = i == activeBirdIndex && phase == FlightPhase.KnockedBack;
                float progress = MountainAuthority.IsHost
                    ? Mathf.Clamp01((Time.time - phaseStarted) / RecoilDuration)
                    : Mathf.Clamp01(1f - networkSecondsLeft / RecoilDuration);
                float raised = stunned ? 70f * (1f - Mathf.SmoothStep(0, 1, progress)) : 0f;
                float wingAngle = Mathf.Lerp(flap, -raised, stunned ? 1f - progress : 0f);
                if (gusting) wingAngle = Mathf.Lerp(wingAngle, Mathf.Sin(windAge * 10.5f) * 52f - 8f, bird.HoverBlend);
                if (i == activeBirdIndex && phase == FlightPhase.Inspecting)
                {
                    bool checking = MountainAuthority.IsHost ? inspectionLanded : networkSecondsLeft < 3f;
                    if (checking) wingAngle = 10f; // Wings fold down while examining the goat.
                }
                bird.LeftWing.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
                bird.RightWing.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
            }
        }

        public BirdState[] CaptureState()
        {
            var states = new BirdState[birds.Length];
            for (int i = 0; i < birds.Length; i++)
            {
                var bird = birds[i];
                states[i] = new BirdState
                {
                    id = bird.Id, position = bird.Root.position, rotation = bird.Root.rotation,
                    phase = (byte)(i == activeBirdIndex ? phase : FlightPhase.Circling),
                    target = i == activeBirdIndex ? targetId : "",
                    attempt = i == activeBirdIndex ? attemptNumber : 0,
                    secondsLeft = i == activeBirdIndex ? Mathf.Max(0f, phaseUntil - Time.time) : 0f
                };
            }
            return states;
        }

        public void ApplyState(BirdState[] states)
        {
            if (MountainAuthority.IsHost || states == null || birds[0] == null) return;
            phase = FlightPhase.Circling;
            targetId = "";
            foreach (var state in states)
            {
                for (int i = 0; i < birds.Length; i++)
                {
                    if (birds[i].Id != state.id) continue;
                    bool impact = state.phase == (byte)FlightPhase.KnockedBack;
                    if (impact && !birds[i].ImpactVisible)
                    {
                        birds[i].Root.SetPositionAndRotation(state.position, state.rotation);
                        CartoonEagleVisual.ShedFeathers(birds[i].Root, materials[3]);
                    }
                    birds[i].ImpactVisible = impact;
                    birds[i].SnapshotPosition = state.position;
                    birds[i].SnapshotRotation = state.rotation;
                    if (state.phase != (byte)FlightPhase.Circling)
                    {
                        activeBirdIndex = i;
                        phase = (FlightPhase)state.phase;
                        targetId = state.target ?? "";
                        attemptNumber = state.attempt;
                        networkSecondsLeft = state.secondsLeft;
                    }
                    break;
                }
            }
        }

        private static string GoatId(GoatController goat)
            => goat ? goat.GetComponent<GoatLocalControl>()?.Label ?? goat.name : "";

        private void OnDestroy()
        {
            if (carried) ReleaseGoat(carried, false);
            if (carryJoint) Destroy(carryJoint);
            if (carryAnchorBody) Destroy(carryAnchorBody.gameObject);
            if (Current == this) Current = null;
            foreach (var material in materials) if (material) Destroy(material);
        }
    }
}
