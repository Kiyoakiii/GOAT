using UnityEngine;

namespace GoatDescent
{
    /// <summary>Five circling birds; the host rolls once every thirty seconds for a hunt.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class SkyPredatorEpisode : MonoBehaviour
    {
        private enum FlightPhase : byte { Circling, Approaching, Diving, Struggling, Looping, Carrying, Returning }

        private sealed class Bird
        {
            public string Id;
            public Transform Root;
            public Transform LeftWing;
            public Transform RightWing;
            public Vector3 SnapshotPosition;
            public Quaternion SnapshotRotation;
        }

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
        private const float HuntChance = .1f;
        private readonly Bird[] birds = new Bird[5];
        private readonly Material[] materials = new Material[3];
        private bool initialized;
        private float summitHeight;
        private Vector3 center, outward, sideways;
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

        private void Awake() => Current = this;

        private void Update()
        {
            if (!MountainAuthority.IsHost || !Input.GetKeyDown(KeyCode.F12)) return;
            var pair = LocalGoatPair.Instance;
            if (pair && pair.Active && (!pair.NetworkMode || pair.Active == pair.Primary))
                ForceAttack(pair.Active);
        }

        /// <summary>Debug shortcut: launch a hunt from the flock toward the specified goat.</summary>
        public bool ForceAttack(GoatController goat)
        {
            if (!MountainAuthority.IsHost || !initialized || birds[0] == null || !goat
                || phase == FlightPhase.Carrying) return false;
            var interaction = goat.GetComponent<GoatInteraction>();
            if (interaction && interaction.IsHolding && interaction.Partner)
                goat = interaction.Partner.GetComponent<GoatController>();
            if (!ValidTarget(goat)) return false;
            if (phase == FlightPhase.Circling)
                activeBirdIndex = ClosestBirdTo(goat.transform.position);
            BeginHunt(goat, activeBirdIndex, true, Time.time);
            Debug.Log($"SKY_PREDATOR_MANUAL bird={birds[activeBirdIndex].Id} target={targetId}");
            return true;
        }

        public void Initialize(Vector3 flockCenter, Vector3 outwardDirection, float summitY)
        {
            initialized = true;
            center = flockCenter;
            summitHeight = summitY;
            outward = Vector3.ProjectOnPlane(outwardDirection, Vector3.up).normalized;
            if (outward.sqrMagnitude < .01f) outward = Vector3.forward;
            sideways = Vector3.Cross(Vector3.up, outward);
            materials[0] = MakeMaterial("Predator feathers", new Color(.19f, .12f, .09f));
            materials[1] = MakeMaterial("Predator wing tips", new Color(.08f, .075f, .07f));
            materials[2] = MakeMaterial("Predator beak", new Color(.96f, .65f, .17f));
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
            var bird = new Bird { Id = $"SKY-{index + 1:00}", Root = root.transform };
            AddPart(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f),
                new Vector3(.52f, .82f, .46f), Quaternion.Euler(90f, 0f, 0f), materials[0]);
            AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, .12f, .64f),
                Vector3.one * .48f, Quaternion.identity, materials[0]);
            AddPart(root.transform, "Beak", PrimitiveType.Cube, new Vector3(0f, .02f, 1.01f),
                new Vector3(.19f, .15f, .4f), Quaternion.Euler(15f, 0f, 0f), materials[2]);
            AddPart(root.transform, "Tail", PrimitiveType.Cube, new Vector3(0f, -.06f, -1.0f),
                new Vector3(.75f, .08f, .72f), Quaternion.Euler(-12f, 0f, 0f), materials[1]);
            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = new GameObject(side < 0 ? "Left wing" : "Right wing");
                pivot.transform.SetParent(root.transform, false);
                pivot.transform.localPosition = new Vector3(side * .28f, .13f, .05f);
                AddPart(pivot.transform, "Wing", PrimitiveType.Cube,
                    new Vector3(side * 1.0f, 0f, -.13f), new Vector3(1.95f, .09f, .68f),
                    Quaternion.identity, materials[0]);
                AddPart(pivot.transform, "Flight feathers", PrimitiveType.Cube,
                    new Vector3(side * 1.9f, -.02f, -.35f), new Vector3(.9f, .06f, .45f),
                    Quaternion.Euler(0f, side * 13f, 0f), materials[1]);
                AddPart(root.transform, side < 0 ? "Left eye" : "Right eye", PrimitiveType.Sphere,
                    new Vector3(side * .2f, .18f, .8f), Vector3.one * .09f,
                    Quaternion.identity, materials[2]);
                AddPart(root.transform, side < 0 ? "Left talon" : "Right talon", PrimitiveType.Cube,
                    new Vector3(side * .27f, -.48f, .36f), new Vector3(.13f, .84f, .16f),
                    Quaternion.Euler(14f, 0f, side * 9f), materials[2]);
                if (side < 0) bird.LeftWing = pivot.transform;
                else bird.RightWing = pivot.transform;
            }
            return bird;
        }

        private static void AddPart(Transform parent, string name, PrimitiveType shape,
            Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
        }

        private void FixedUpdate()
        {
            if (!initialized || birds[0] == null || !MountainAuthority.IsHost) return;
            float now = Time.time;
            if (now >= nextHuntRollAt)
            {
                nextHuntRollAt = now + HuntRollInterval;
                RollForHunt(now);
            }
            UpdateOrbit(now);
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
            flightCeilingY = Mathf.Max(summitHeight + 14f, goat.transform.position.y + 11f,
                birds[birdIndex].Root.position.y + 6f);
            bool nearbyFace = Vector3.Dot(outward, attackOutward) > .5f
                && Mathf.Abs(goat.transform.position.y - center.y) < 20f;
            approachStage = nearbyFace ? 2 : 0;
            phase = FlightPhase.Approaching;
            phaseStarted = now;
            phaseUntil = 0f;
            Debug.Log($"SKY_PREDATOR_HUNT_BEGIN bird={birds[birdIndex].Id} target={targetId} forced={forced}");
        }

        private bool CanContinueHunt()
            => ValidTarget(target) && (manualAttack || IsExposed(target));

        private void AdvanceApproach(float now)
        {
            var bird = birds[activeBirdIndex];
            Vector3 launchPoint = target.transform.position + attackOutward * 7f + Vector3.up * 8f;
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
            returnStage = 0;
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
            Vector3 radial = transform.parent ? position - transform.parent.position : outward;
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
            float roll = Random.value;
            if (roll >= HuntChance)
            {
                Debug.Log($"SKY_PREDATOR_ROLL chance=0.10 value={roll:0.000} hunt=false");
                return;
            }
            activeBirdIndex = Random.Range(0, birds.Length);
            GoatController candidate = FindExposedTarget(birds[activeBirdIndex].Root.position);
            if (!candidate)
            {
                Debug.Log($"SKY_PREDATOR_ROLL chance=0.10 value={roll:0.000} hunt=false reason=no_exposed_goat");
                return;
            }
            BeginHunt(candidate, activeBirdIndex, false, now);
            Debug.Log($"SKY_PREDATOR_ROLL chance=0.10 value={roll:0.000} hunt=true bird={birds[activeBirdIndex].Id} target={targetId}");
        }

        private GoatController FindExposedTarget(Vector3 birdPosition)
        {
            GoatController nearestGoat = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
            {
                if (!Eligible(goat)) continue;
                float distance = (goat.transform.position - birdPosition).sqrMagnitude;
                if (distance < nearestDistance || (Mathf.Approximately(distance, nearestDistance)
                    && string.CompareOrdinal(GoatId(goat), GoatId(nearestGoat)) < 0))
                { nearestGoat = goat; nearestDistance = distance; }
            }
            return nearestGoat;
        }

        private static bool Eligible(GoatController goat)
            => ValidTarget(goat) && IsExposed(goat);

        private static bool ValidTarget(GoatController goat)
            => goat && !goat.IsPredatorCarried
                && goat.GetComponent<RespawnController>()?.IsDead != true;

        private static bool IsExposed(GoatController goat)
        {
            Vector3 head = goat.transform.position + Vector3.up * 1.35f;
            return ClearPath(head, head + Vector3.up * 14f);
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
            float angle = time * (index % 2 == 0 ? .36f : -.31f)
                + index * (Mathf.PI * 2f / birds.Length);
            return center + outward * (16f + Mathf.Cos(angle) * 6f)
                + sideways * (Mathf.Sin(angle) * 8f)
                + Vector3.up * (16f + Mathf.Sin(angle * 1.4f) * 1.3f + index % 3 * 2f);
        }

        private void UpdateOrbit(float time)
        {
            for (int i = 0; i < birds.Length; i++)
            {
                if (i == activeBirdIndex && phase != FlightPhase.Circling) continue;
                Vector3 point = OrbitPoint(i, time);
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
                float flap = Mathf.Sin(Time.time * (working ? 13f : 5f) + i * 1.4f) * 16f;
                bird.LeftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
                bird.RightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
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
