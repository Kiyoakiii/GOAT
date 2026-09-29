using UnityEngine;

namespace GoatDescent
{
    public enum HoofState { Anchored, Seeking, Sliding, Free }

    /// <summary>Four independent holds pull on one simulated centre of mass.</summary>
    public sealed class CliffBalanceGoat : MonoBehaviour
    {
        private sealed class Hoof
        {
            public Vector3 Offset;
            public Vector3 Position;
            public Vector3 Anchor;
            public float Quality;
            public float Grip;
            public float SlideTime;
            public float RetryAt;
            public float MaxCatchY;
            public float LandedAt;
            public float SupportSpan;
            public Vector3 StepTarget;
            public float StepFinishAt;
            public bool Stepping;
            public int VeinIndex;
            public HoofState State;
            public Transform Upper, Lower, Tip;
            public Renderer TipRenderer;
        }

        private readonly Hoof[] hooves = new Hoof[4];
        private CliffVeinField field;
        private CliffBalancePrototypeBootstrap level;
        private Transform torso;
        private AudioSource scrape;
        private Material anchoredMaterial, searchingMaterial, slidingMaterial, freeMaterial;
        private const float StandingHeight = .80f;
        private Vector3 center;
        private Vector3 velocity;
        private Vector3 movement;
        private Vector2 mouseBalance;
        private bool simulationInput;
        private float simulationUntil;
        private float nextStepAt;
        private int nextStepIndex;
        private float nextJumpAt;
        private float retryAt;
        private float lastPebble;
        private Vector3 supportCenter;
        private readonly Vector2[] footprint = new Vector2[4];
        private readonly Vector2[] hull = new Vector2[8];
        private static readonly int[] StepOrder = { 0, 2, 1, 3 };

        public bool Armed { get; private set; }
        public int AttachedCount { get; private set; }
        public float Balance01 { get; private set; } = 1f;
        public float Height => center.y;
        public Vector3 Velocity => velocity;
        public Vector3 CentreOfMass => center;
        public float DepthOffset => AttachedCount > 0 ? center.z - supportCenter.z : 0f;
        public int Falls { get; private set; }
        public Vector2 MouseBalance => mouseBalance;
        public string Posture => AttachedCount == 0 ? (velocity.y > .05f ? "В ПРЫЖКЕ" : "СВОБОДНОЕ ПАДЕНИЕ")
            : AttachedCount == 1 ? "ДЕРЖИТСЯ НА ОДНОМ КОПЫТЕ"
            : AttachedCount == 2 && hooves[0].State == HoofState.Anchored && hooves[1].State == HoofState.Anchored ? "ДЕРЖИТСЯ НА ПЕРЕДНИХ"
            : AttachedCount == 2 ? "ДВЕ ТОЧКИ ОПОРЫ"
            : AttachedCount == 3 ? "ОДНА НОГА СОРВАЛАСЬ" : "ЧЕТЫРЕ ОПОРЫ";

        public void Initialize(CliffVeinField supportField, CliffBalancePrototypeBootstrap owner)
        {
            field = supportField;
            level = owner;
            BuildVisuals();
            ResetBody(false);
        }

        public HoofState GetHoofState(int index) => hooves[index].State;
        public float GetHoofGrip(int index) => hooves[index].Grip;

        private void Update()
        {
            if (!level) return;
            if (Input.GetKeyDown(KeyCode.R)) { level.ResetRun(); return; }
            if (level.Completed) return;
            if (Time.time < retryAt) return;

            if (!simulationInput || Time.time >= simulationUntil)
            {
                simulationInput = false;
                movement = new Vector3(
                    (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f),
                    0f, (Input.GetKey(KeyCode.A) ? 1f : 0f) - (Input.GetKey(KeyCode.D) ? 1f : 0f));
                mouseBalance += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * .012f;
                mouseBalance = Vector2.ClampMagnitude(mouseBalance, 1f);
                mouseBalance = Vector2.MoveTowards(mouseBalance, Vector2.zero, Time.deltaTime * .15f);
                if (Input.GetKeyDown(KeyCode.Space)) JumpFromLedge();
            }
            if (!Armed && (movement.sqrMagnitude > .01f || mouseBalance.sqrMagnitude > .1225f)) level.BeginRun();
        }

        // The Editor can drive the same controls without synthesising OS events.
        public void SimulateControls(Vector2 direction, Vector2 balance, float seconds, bool jump = false)
        {
            if (level.Completed) return;
            movement = new Vector3(direction.y, 0f, -direction.x);
            mouseBalance = Vector2.ClampMagnitude(balance, 1f);
            simulationInput = true;
            simulationUntil = Time.time + seconds;
            if (!Armed) level.BeginRun();
            if (jump) JumpFromLedge();
        }

        private void JumpFromLedge()
        {
            if (level.Completed || Time.time < nextJumpAt || AttachedCount == 0) return;
            if (!Armed) level.BeginRun();
            nextJumpAt = Time.time + .45f;
            foreach (Hoof hoof in hooves)
            {
                hoof.Stepping = false;
                hoof.MaxCatchY = Mathf.Min(hoof.MaxCatchY, hoof.Anchor.y - .12f);
                hoof.State = HoofState.Free;
                hoof.RetryAt = Time.time + .16f;
            }
            AttachedCount = 0;
            // Leave the ledge with a visible upward arc. The next hold can only
            // be caught after the hooves pass below the lip used for takeoff.
            velocity.y = Mathf.Max(velocity.y, 2.65f);
            velocity.x += movement.x * .75f;
            velocity.z += movement.z * .30f - .12f;
        }

        public void Arm() => Armed = true;

        private void FixedUpdate()
        {
            if (retryAt > 0f)
            {
                if (Time.time < retryAt) return;
                RecoverAfterFall();
                retryAt = 0f;
                return;
            }
            if (!Armed || !field || !level || level.Completed) return;
            float dt = Time.fixedDeltaTime;
            UpdateHooves(dt);
            MeasureBalance();

            Vector3 force = new Vector3(0f, -4.25f, 0f);
            Vector3 intent = Vector3.ClampMagnitude(movement, 1f);
            float steering = AttachedCount > 0 ? 1f : .30f;
            force += new Vector3(intent.x * 14.5f * steering, 0f, intent.z * 4.5f * steering);
            force += new Vector3(mouseBalance.y * 26f, 0f, -mouseBalance.x * 11.5f);
            for (int i = 0; i < hooves.Length; i++)
            {
                Hoof hoof = hooves[i];
                if (hoof.State != HoofState.Anchored) continue;
                Vector3 stretch = hoof.Anchor - (center + hoof.Offset);
                force += Vector3.ClampMagnitude(stretch * (27f * hoof.Quality * Mathf.Lerp(.5f, 1f, hoof.Grip)), 20f);
            }
            velocity += force * dt;
            velocity *= Mathf.Exp(-(AttachedCount > 0 ? 2.1f : .24f) * dt);
            velocity.x = Mathf.Clamp(velocity.x, -2.5f, 2.5f);
            velocity.y = Mathf.Clamp(velocity.y, -6.5f, 3.0f);
            velocity.z = Mathf.Clamp(velocity.z, -2.5f, 2.5f);
            center += velocity * dt;
            center.x = Mathf.Clamp(center.x, -4.65f, 4.65f);
            center.y = Mathf.Min(center.y, 19.5f);
            float mostInward = CliffSlope.WallZ(center.y) - .18f;
            if (center.z > mostInward) { center.z = mostInward; velocity.z = Mathf.Min(velocity.z, 0f); }
            EvaluateGrip(dt);
            UpdateVisuals();
            UpdateScrape(dt);

            if (center.y < 1.25f && Mathf.Abs(center.x) < 1.55f && AttachedCount > 0 && velocity.y > -2.4f &&
                Mathf.Abs(center.z - CliffSlope.StandingCenterZ(center.y)) < .40f)
                level.ReachCave();
            else if (center.y < -.5f) FullFall();
        }

        private void UpdateHooves(float dt)
        {
            for (int i = 0; i < hooves.Length; i++)
            {
                Hoof hoof = hooves[i];
                Vector3 desired = center + hoof.Offset;
                if (hoof.State == HoofState.Anchored)
                {
                    hoof.Position = hoof.Anchor;
                    if (i == StepOrder[nextStepIndex] && Time.time >= nextStepAt &&
                        movement.sqrMagnitude > .08f && AttachedCount >= 3)
                    {
                        Vector3 step = desired + Vector3.ClampMagnitude(movement, 1f) * .34f;
                        if (field.FindNearest(step, .28f, out var next) &&
                            Mathf.Abs(next.Point.y - hoof.Anchor.y) < .10f &&
                            (next.Point - hoof.Anchor).magnitude > .12f)
                        {
                            hoof.State = HoofState.Seeking;
                            hoof.MaxCatchY = hoof.Anchor.y + .12f;
                            hoof.StepTarget = next.Point;
                            hoof.StepFinishAt = Time.time + .12f;
                            hoof.Stepping = true;
                            nextStepAt = Time.time + .15f;
                        }
                        else nextStepAt = Time.time + .12f;
                        nextStepIndex = (nextStepIndex + 1) % StepOrder.Length;
                    }
                    continue;
                }
                if (hoof.Stepping)
                {
                    hoof.Position = Vector3.MoveTowards(hoof.Position, hoof.StepTarget, dt * 3.5f);
                    if (Time.time < hoof.StepFinishAt || (hoof.Position - hoof.StepTarget).sqrMagnitude > .0064f) continue;
                    if (field.FindNearest(hoof.StepTarget, .08f, out var stepSupport, hoof.MaxCatchY))
                    {
                        hoof.Anchor = hoof.Position = stepSupport.Point;
                        hoof.Quality = stepSupport.Quality;
                        hoof.SupportSpan = stepSupport.Span;
                        hoof.VeinIndex = stepSupport.VeinIndex;
                        hoof.Grip = Mathf.Max(.85f, stepSupport.Quality * .92f);
                        hoof.State = HoofState.Anchored;
                        hoof.MaxCatchY = float.PositiveInfinity;
                        hoof.LandedAt = Time.time;
                    }
                    hoof.Stepping = false;
                    continue;
                }
                if (hoof.State == HoofState.Sliding)
                {
                    hoof.SlideTime -= dt;
                    hoof.Position += Vector3.down * (1.1f * dt);
                    if (Time.time - lastPebble > .12f) { SpawnPebble(hoof.Position); lastPebble = Time.time; }
                    if (hoof.SlideTime > 0f) continue;
                    hoof.State = HoofState.Seeking;
                    hoof.RetryAt = Time.time + .13f;
                }
                if (Time.time >= hoof.RetryAt && field.FindNearest(desired, .24f, out var support, hoof.MaxCatchY))
                {
                    // A real contact arrests the fall, including on a lip too short
                    // to fit all four hooves. Other feet still have to find it nearby.
                    if (AttachedCount == 0 && velocity.y < 0f)
                    {
                        velocity.y = Mathf.Max(velocity.y, support.Span < .9f ? -.25f : -.70f);
                        velocity.x *= .65f;
                        velocity.z *= .65f;
                    }
                    hoof.Anchor = support.Point;
                    hoof.Position = support.Point;
                    hoof.Quality = support.Quality;
                    hoof.SupportSpan = support.Span;
                    hoof.VeinIndex = support.VeinIndex;
                    hoof.Grip = Mathf.Max(.85f, support.Quality * .92f);
                    hoof.State = HoofState.Anchored;
                    hoof.MaxCatchY = float.PositiveInfinity;
                    hoof.LandedAt = Time.time;
                }
                else
                {
                    hoof.State = HoofState.Seeking;
                    hoof.Position = Vector3.Lerp(hoof.Position, desired, Mathf.Min(1f, dt * 8f));
                }
            }
        }

        private void MeasureBalance()
        {
            AttachedCount = 0;
            supportCenter = Vector3.zero;
            foreach (Hoof hoof in hooves)
            {
                if (hoof.State != HoofState.Anchored) continue;
                footprint[AttachedCount] = new Vector2(hoof.Anchor.x, hoof.Anchor.z);
                AttachedCount++;
                supportCenter += hoof.Anchor;
            }
            if (AttachedCount == 0) { Balance01 = 0f; return; }
            supportCenter /= AttachedCount;
            // Gravity projects the centre of mass straight down onto the hoof plane.
            // The support area is a polygon in X/Z, not a rectangle in the wall's X/Y.
            // A lean moves the upper body's mass before the hooves or torso centre catch up.
            Vector2 projectedWeight = new Vector2(center.x + mouseBalance.y * .24f,
                center.z - mouseBalance.x * .04f);
            float margin = SignedSupportMargin(projectedWeight, AttachedCount);
            float stable = AttachedCount == 1 ? .62f : AttachedCount == 2 ? .66f : .72f;
            Balance01 = Mathf.Clamp01(stable + (margin >= 0f ?
                Mathf.Min(margin / .18f, 1f) * (1f - stable) : margin / .42f)
                - velocity.magnitude * .025f);
        }

        private float SignedSupportMargin(Vector2 weight, int count)
        {
            if (count == 1)
            {
                Vector2 offset = weight - footprint[0];
                return .42f - Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y * 4f);
            }
            if (count == 2) return .28f - DistanceToSegment(weight, footprint[0], footprint[1]);

            // Four points at most: insertion sort and a monotone-chain convex hull.
            for (int i = 1; i < count; i++)
            {
                Vector2 value = footprint[i];
                int j = i - 1;
                while (j >= 0 && (footprint[j].x > value.x ||
                    (footprint[j].x == value.x && footprint[j].y > value.y)))
                { footprint[j + 1] = footprint[j]; j--; }
                footprint[j + 1] = value;
            }
            int size = 0;
            for (int i = 0; i < count; i++)
            {
                while (size >= 2 && Cross(hull[size - 1] - hull[size - 2], footprint[i] - hull[size - 1]) <= 0f) size--;
                hull[size++] = footprint[i];
            }
            int lowerSize = size;
            for (int i = count - 2; i >= 0; i--)
            {
                while (size > lowerSize && Cross(hull[size - 1] - hull[size - 2], footprint[i] - hull[size - 1]) <= 0f) size--;
                hull[size++] = footprint[i];
            }
            int vertexCount = size - 1;
            if (vertexCount < 3) return .04f - DistanceToSegment(weight, hull[0], hull[Mathf.Max(1, vertexCount - 1)]);
            float margin = float.PositiveInfinity;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector2 a = hull[i], b = hull[(i + 1) % vertexCount];
                Vector2 edge = b - a;
                margin = Mathf.Min(margin, Cross(edge, weight - a) / edge.magnitude);
            }
            return margin;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            if (edge.sqrMagnitude < .00001f) return Vector2.Distance(p, a);
            return Vector2.Distance(p, a + edge * Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude));
        }

        private void EvaluateGrip(float dt)
        {
            if (AttachedCount == 0) return;
            bool resting = movement.sqrMagnitude < .02f && mouseBalance.sqrMagnitude < .04f && velocity.magnitude < .9f;
            for (int i = 0; i < hooves.Length; i++)
            {
                Hoof hoof = hooves[i];
                if (hoof.State != HoofState.Anchored) continue;
                float stretch = (hoof.Anchor - center - hoof.Offset).magnitude;
                if (resting && hoof.SupportSpan < .9f && stretch < .75f)
                {
                    hoof.Grip = Mathf.MoveTowards(hoof.Grip, Mathf.Max(.9f, hoof.Quality), dt * .85f);
                    continue;
                }
                float lateralLoad = Mathf.Max(0f, (hoof.Anchor.x - supportCenter.x) * Mathf.Sign(center.x - supportCenter.x))
                    * Mathf.Abs(center.x - supportCenter.x) * .65f;
                float depthLoad = Mathf.Abs(center.z - supportCenter.z) * .70f;
                float depthSide = (hoof.Anchor.z - supportCenter.z) * Mathf.Sign(center.z - supportCenter.z);
                float edgeLoad = Mathf.Max(0f, depthSide) * 2.2f;
                float contactVariation = .025f * Mathf.Sin(hoof.Anchor.x * 11f + hoof.Anchor.y * 7f);
                float projectionLoad = Mathf.Max(0f, .80f - Balance01) * (AttachedCount <= 2 ? .65f : 1.6f);
                float stress = stretch / .62f + (1f - Balance01) * .42f + lateralLoad + depthLoad + edgeLoad + contactVariation + projectionLoad
                    + (AttachedCount == 1 ? .08f : AttachedCount == 2 ? .04f : 0f);
                if (Time.time - hoof.LandedAt < 1.2f) stress *= .60f;
                if (stress > .72f)
                    hoof.Grip -= (stress - .62f) * (1.4f - hoof.Quality * .5f) * dt;
                else hoof.Grip = Mathf.MoveTowards(hoof.Grip, hoof.Quality, dt * .27f);
                if (hoof.Grip > .12f && stretch < .90f) continue;
                hoof.State = HoofState.Sliding;
                hoof.SlideTime = .27f + hoof.Quality * .30f;
                hoof.RetryAt = Time.time + .42f;
            }
            MeasureBalance();
        }

        private void FullFall()
        {
            Falls++;
            retryAt = Time.time + .9f;
            level.NoteFall();
        }

        public void ResetBody(bool resetFalls)
        {
            center = new Vector3(0f, 18.45f, CliffSlope.StandingCenterZ(18.45f));
            velocity = Vector3.zero;
            supportCenter = center;
            retryAt = 0f;
            Armed = false;
            mouseBalance = Vector2.zero;
            movement = Vector3.zero;
            nextStepAt = 0f;
            nextStepIndex = 0;
            nextJumpAt = 0f;
            simulationInput = false;
            if (resetFalls) Falls = 0;
            if (scrape) scrape.volume = 0f;
            for (int i = 0; i < hooves.Length; i++)
            {
                Hoof hoof = hooves[i];
                hoof.MaxCatchY = float.PositiveInfinity;
                hoof.LandedAt = -100f;
                hoof.Stepping = false;
                Vector3 desired = center + hoof.Offset;
                if (field.FindNearest(desired, .48f, out var support))
                {
                    hoof.Anchor = hoof.Position = support.Point;
                    hoof.Quality = support.Quality;
                    hoof.SupportSpan = support.Span;
                    hoof.VeinIndex = support.VeinIndex;
                    hoof.Grip = 1f;
                    hoof.State = HoofState.Anchored;
                }
                else { hoof.Position = desired; hoof.State = HoofState.Seeking; }
            }
            MeasureBalance();
            UpdateVisuals();
        }

        public void RecoverAfterFall()
        {
            ResetBody(false);
            Armed = true;
        }

        private void BuildVisuals()
        {
            var fur = MakeMaterial("Grey goat", new Color(.72f, .72f, .69f));
            var dark = MakeMaterial("Hooves and horns", new Color(.24f, .25f, .26f));
            anchoredMaterial = MakeMaterial("Firm hoof", new Color(.47f, .72f, .48f));
            searchingMaterial = MakeMaterial("Searching hoof", new Color(.92f, .75f, .30f));
            slidingMaterial = MakeMaterial("Sliding hoof", new Color(1f, .39f, .22f));
            freeMaterial = MakeMaterial("Free hoof", new Color(.49f, .49f, .50f));
            torso = new GameObject("Visible goat centre of mass").transform;
            torso.SetParent(transform, false);
            scrape = gameObject.AddComponent<AudioSource>();
            scrape.clip = CreateScrapeClip();
            scrape.loop = true;
            scrape.playOnAwake = false;
            scrape.spatialBlend = 0f;
            scrape.volume = 0f;
            scrape.Play();
            // The goat faces along the ledge. Its back stays roughly horizontal,
            // with four legs below it and gravity acting in world-down.
            Transform body = MakePart(torso, PrimitiveType.Capsule, "Horizontal body", Vector3.zero,
                new Vector3(.47f, .66f, .39f), fur);
            body.localRotation = Quaternion.Euler(0f, 0f, 90f);
            MakePart(torso, PrimitiveType.Sphere, "Chest", new Vector3(.46f, .05f, -.01f), new Vector3(.40f, .45f, .35f), fur);
            MakePart(torso, PrimitiveType.Sphere, "Neck", new Vector3(.63f, .25f, -.01f), new Vector3(.28f, .50f, .30f), fur);
            MakePart(torso, PrimitiveType.Sphere, "Head", new Vector3(.83f, .46f, -.01f), new Vector3(.35f, .28f, .31f), fur);
            MakePart(torso, PrimitiveType.Sphere, "Muzzle", new Vector3(1.10f, .37f, -.03f), new Vector3(.27f, .17f, .25f), fur);
            MakePart(torso, PrimitiveType.Sphere, "Tail", new Vector3(-.75f, .18f, .02f), new Vector3(.20f, .20f, .20f), fur);
            for (int side = -1; side <= 1; side += 2)
            {
                var horn = MakePart(torso, PrimitiveType.Cylinder, "Horn", new Vector3(.75f, .75f, side * .13f), new Vector3(.065f, .23f, .065f), dark);
                horn.localRotation = Quaternion.Euler(0f, 0f, -18f);
                MakePart(torso, PrimitiveType.Sphere, "Ear", new Vector3(.61f, .51f, side * .20f), new Vector3(.24f, .11f, .11f), fur);
            }
            for (int i = 0; i < hooves.Length; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float foreAft = i < 2 ? .45f : -.45f;
                var hoof = new Hoof { Offset = new Vector3(foreAft + side * .11f,
                    -StandingHeight + side * .015f, side < 0 ? -.05f : .06f) };
                hoof.Upper = MakePart(transform, PrimitiveType.Cylinder, $"Leg {i + 1} upper", Vector3.zero, Vector3.one, fur);
                hoof.Lower = MakePart(transform, PrimitiveType.Cylinder, $"Leg {i + 1} lower", Vector3.zero, Vector3.one, fur);
                hoof.Tip = MakePart(transform, PrimitiveType.Cube, $"Hoof {i + 1}", Vector3.zero, new Vector3(.18f, .10f, .16f), dark);
                hoof.TipRenderer = hoof.Tip.GetComponent<Renderer>();
                hooves[i] = hoof;
            }
        }

        private static Material MakeMaterial(string name, Color color) => new Material(Shader.Find("Standard")) { name = name, color = color };

        private static Transform MakePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        private void UpdateVisuals()
        {
            transform.position = center;
            float shake = (1f - Balance01) * Mathf.Sin(Time.time * 31f) * 2.6f;
            float lean = AttachedCount > 0 ? (center.x - supportCenter.x) * -20f - mouseBalance.y * 11f : velocity.x * -8f;
            float depthLean = AttachedCount > 0 ? (center.z - supportCenter.z) * 130f - mouseBalance.x * 12f : velocity.z * 12f;
            torso.localRotation = Quaternion.Euler(Mathf.Clamp(depthLean, -38f, 38f), 0f,
                Mathf.Clamp(lean + shake, -24f, 24f));
            for (int i = 0; i < hooves.Length; i++)
            {
                Hoof hoof = hooves[i];
                float hipX = (i < 2 ? .44f : -.44f) + (i % 2 == 0 ? -.055f : .055f);
                Vector3 shoulder = torso.TransformPoint(new Vector3(hipX, -.17f, hoof.Offset.z));
                float tuck = AttachedCount == 0 ? Mathf.Max(0f, velocity.y) * .07f : 0f;
                Vector3 tip = hoof.Position + Vector3.up * (.05f + tuck);
                Vector3 knee = Vector3.Lerp(shoulder, tip, .52f) + new Vector3(i < 2 ? .10f : -.10f, -.04f, -.02f);
                PlaceLimb(hoof.Upper, shoulder, knee, .065f);
                PlaceLimb(hoof.Lower, knee, tip, .052f);
                hoof.Tip.position = tip;
                hoof.TipRenderer.sharedMaterial = hoof.State == HoofState.Anchored ? anchoredMaterial
                    : hoof.State == HoofState.Seeking ? searchingMaterial
                    : hoof.State == HoofState.Sliding ? slidingMaterial : freeMaterial;
            }
        }

        private static void PlaceLimb(Transform limb, Vector3 a, Vector3 b, float width)
        {
            Vector3 delta = b - a;
            limb.position = (a + b) * .5f;
            limb.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            limb.localScale = new Vector3(width, delta.magnitude * .5f, width);
        }

        private void SpawnPebble(Vector3 point)
        {
            var pebble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pebble.name = "Falling rock dust";
            pebble.transform.position = point + new Vector3(0f, 0f, -.035f);
            pebble.transform.localScale = Vector3.one * .045f;
            Destroy(pebble.GetComponent<Collider>());
            pebble.AddComponent<CliffPebbleDust>();
        }

        private void UpdateScrape(float dt)
        {
            int slipping = 0;
            foreach (Hoof hoof in hooves) if (hoof.State == HoofState.Sliding) slipping++;
            float target = slipping > 0 ? Mathf.Min(.31f, .12f + slipping * .055f) : 0f;
            scrape.volume = Mathf.MoveTowards(scrape.volume, target, dt * 1.8f);
            scrape.pitch = .85f + Mathf.Min(velocity.magnitude * .13f, .65f);
        }

        private static AudioClip CreateScrapeClip()
        {
            const int sampleRate = 22050;
            var clip = AudioClip.Create("Hoof scraping rock", sampleRate / 2, 1, sampleRate, false);
            var samples = new float[sampleRate / 2];
            var random = new System.Random(9134);
            float filtered = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                filtered = filtered * .68f + ((float)random.NextDouble() * 2f - 1f) * .32f;
                samples[i] = filtered * (.6f + .3f * Mathf.Sin(i * .021f));
            }
            clip.SetData(samples, 0);
            return clip;
        }
    }

    public sealed class CliffPebbleDust : MonoBehaviour
    {
        private float born;
        private void Start() => born = Time.time;
        private void Update()
        {
            transform.position += Vector3.down * (1.1f + (Time.time - born) * 3f) * Time.deltaTime;
            if (Time.time - born > .7f) Destroy(gameObject);
        }
    }
}
