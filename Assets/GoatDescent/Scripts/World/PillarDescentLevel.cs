using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>First playable route down the actual sandstone pillar used by the goat spawn.</summary>
    [DefaultExecutionOrder(80)]
    public sealed class PillarDescentLevel : MonoBehaviour
    {
        public int LedgeCount { get; private set; }
        public float MaxJumpGap { get; private set; }
        public float EarlyAverageGap { get; private set; }
        public float LateAverageGap { get; private set; }
        public bool Completed { get; private set; }
        public int ProgressIndex => LocalGoatPair.Instance && LocalGoatPair.Instance.Active
            && playerProgress.TryGetValue(LocalGoatPair.Instance.Active, out var progress)
                ? progress.Index : TeamProgressIndex;
        public int TeamProgressIndex { get; private set; } = -1;
        public int BellsCollected { get; private set; }
        public int BellCount { get; private set; }
        public int Falls { get; private set; }
        public int VaultsUsed { get; private set; }
        public float ElapsedSeconds => started ? (Completed ? finishTime : Time.time - startTime) : 0f;
        public bool Started => started;
        public Vector3 NextLedgePosition => ProgressIndex + 1 < ledges.Count ? ledges[ProgressIndex + 1].LandingPoint : Vector3.zero;
        public float CrumbleSecondsLeft => activeCrumble && activeCrumble.IsCracking ? activeCrumble.SecondsLeft : 0f;
        public string RecentEvent => Time.time < eventUntil ? eventText : "";
        public DescentLedge GetLedge(int index) => index >= 0 && index < ledges.Count ? ledges[index] : null;
        public string MapSignature
        {
            get
            {
                if (ledges.Count == 0) return "empty";
                uint hash = 2166136261u;
                foreach (var ledge in ledges)
                {
                    var point = ledge.LandingPoint;
                    hash = FoldMapHash(hash, Mathf.RoundToInt(point.x * 10f));
                    hash = FoldMapHash(hash, Mathf.RoundToInt(point.y * 10f));
                    hash = FoldMapHash(hash, Mathf.RoundToInt(point.z * 10f));
                    hash = FoldMapHash(hash, Mathf.RoundToInt(ledge.CapacityKg));
                }
                return $"{ledges.Count}:{hash:X8}";
            }
        }

        private static uint FoldMapHash(uint hash, int value)
            => unchecked((hash ^ (uint)value) * 16777619u);

        private readonly List<DescentLedge> ledges = new List<DescentLedge>();
        private readonly Dictionary<GoatController, PersonalProgress> playerProgress = new Dictionary<GoatController, PersonalProgress>();
        private readonly List<GoatController> participants = new List<GoatController>();
        private DescentLedge activeCrumble;
        private bool started;
        private float startTime;
        private float finishTime;
        private DescentLedge selectedLedge;
        private sealed class PersonalProgress
        {
            public int Index = -1;
            public int CheckpointIndex = -1;
            public bool Finished;
        }
        private string eventText = "";
        private float eventUntil;
        private Material beaconMaterial;
        private Material checkpointMaterial;
        private Material bellMaterial;
        private Material hornMaterial;

        private struct LedgeSite
        {
            public Vector3 Wall;
            public Vector3 Outward;
            public Vector3 Center;
            public float Angle;
            public bool Clear;
        }

        public static PillarDescentLevel Build(Transform pillar, Vector3 spawn, Terrain terrain)
        {
            var cliff = pillar.Find("Stratified sandstone");
            var cliffCollider = cliff ? cliff.GetComponent<MeshCollider>() : null;
            if (!cliffCollider) { Debug.LogError("LEVEL1_ROUTE missing start pillar collider"); return null; }

            var root = new GameObject("Level 1 — cliff descent");
            root.transform.SetParent(pillar, false);
            var level = root.AddComponent<PillarDescentLevel>();
            var rock = cliff.GetComponent<Renderer>().sharedMaterial;
            var ledgeMaterial = new Material(rock) { name = "Level 1 ledge stone" };
            if (ledgeMaterial.HasProperty("_StoneColor"))
                ledgeMaterial.SetColor("_StoneColor", new Color(.64f, .58f, .45f));
            level.beaconMaterial = MakeGlowMaterial("Next ledge", new Color(1f, .54f, .12f));
            level.checkpointMaterial = MakeGlowMaterial("Checkpoint", new Color(.1f, .9f, .75f));
            level.bellMaterial = MakeGlowMaterial("Mountain bell", new Color(1f, .77f, .16f));
            level.hornMaterial = MakeGlowMaterial("Horn vault", new Color(.66f, .3f, 1f));

            Vector3 face = pillar.forward;
            Vector3 groundProbe = pillar.position + face * 70f;
            float groundY = terrain ? terrain.SampleHeight(groundProbe) + terrain.transform.position.y : pillar.position.y;
            const float drop = 4.45f;
            var treeBounds = GetTreeBounds(pillar);
            if (treeBounds.Count == 0)
            {
                Debug.LogError("LEVEL1_ROUTE tree geometry unavailable; refusing to place ledges without clearance checks");
                Destroy(root);
                return null;
            }
            var sites = new List<LedgeSite[]>();
            int treeBlocked = 0;
            const int angleCount = 72;
            for (int i = 0; i < 72; i++)
            {
                float y = spawn.y - 4.2f - i * drop;
                if (y < groundY + 1.5f) break;
                var ring = new LedgeSite[angleCount];
                for (int a = 0; a < angleCount; a++)
                {
                    float angle = -180f + a * 5f;
                    Vector3 outward = Quaternion.AngleAxis(angle, Vector3.up) * face;
                    Ray ray = new Ray(pillar.position + outward * 110f + Vector3.up * (y - pillar.position.y), -outward);
                    if (!cliffCollider.Raycast(ray, out RaycastHit hit, 125f)) continue;
                    Vector3 center = hit.point + outward * 2.3f;
                    bool clear = IsTreeClear(hit.point, outward, i % 7 == 4 ? 3.4f : 4.1f, treeBounds);
                    if (!clear) treeBlocked++;
                    ring[a] = new LedgeSite { Wall = hit.point, Outward = outward, Center = center, Angle = angle, Clear = clear };
                }
                sites.Add(ring);
            }

            int[] chosen = PlanRoute(sites, spawn);
            if (chosen == null)
            {
                Debug.LogError($"LEVEL1_ROUTE no tree-free jump route; rings={sites.Count} treeBounds={treeBounds.Count}");
                Destroy(root);
                return null;
            }

            Vector3 last = spawn;
            float maxJumpGap = 0f;
            float earlySum = 0f, lateSum = 0f;
            int earlyCount = 0, lateCount = 0;
            for (int i = 0; i < chosen.Length; i++)
            {
                LedgeSite site = sites[i][chosen[i]];
                Vector3 next = CreateLedge(root.transform, site.Wall, site.Outward, ledgeMaterial, i, out GameObject shelf);
                bool checkpoint = i == 12 || i == 27 || i == 41;
                bool nearCheckpoint = Mathf.Abs(i - 12) <= 1 || Mathf.Abs(i - 27) <= 1 || Mathf.Abs(i - 41) <= 1;
                bool crumble = i >= 5 && i < chosen.Length - 2 && (i % 4 == 2 || i % 7 == 4)
                    && !nearCheckpoint;
                var ledge = shelf.AddComponent<DescentLedge>();
                ledge.Initialize(level, i, next, crumble, crumble ? 95f : 100000f);
                level.ledges.Add(ledge);
                if (crumble) CreateSafetyRock(shelf.transform, ledgeMaterial, i);
                CreateRouteBeacon(shelf.transform, level.beaconMaterial, ledge);
                if (checkpoint) CreateCheckpointMarker(shelf.transform, level.checkpointMaterial);
                if (i == 6 || i == 15 || i == 23 || i == 32 || i == 40 || i == 48)
                    level.CreateBell(shelf.transform, i);
                if (i > 0)
                {
                    float gap = HorizontalGap(last, next);
                    maxJumpGap = Mathf.Max(maxJumpGap, gap);
                    if (i < chosen.Length / 3) { earlySum += gap; earlyCount++; }
                    if (i >= chosen.Length * 2 / 3) { lateSum += gap; lateCount++; }
                }
                last = next;
                level.LedgeCount++;
            }

            foreach (var pair in new[] { (10, 12), (24, 27), (38, 41) })
                if (pair.Item2 < level.ledges.Count)
                    level.CreateHornStone(level.ledges[pair.Item1], level.ledges[pair.Item2]);

            CreateStartCairn(root.transform, cliffCollider, spawn, face, ledgeMaterial);
            level.MaxJumpGap = maxJumpGap;
            level.EarlyAverageGap = earlyCount > 0 ? earlySum / earlyCount : 0f;
            level.LateAverageGap = lateCount > 0 ? lateSum / lateCount : 0f;

            Vector3 finish = last + face * 5f;
            if (terrain) finish.y = terrain.SampleHeight(finish) + terrain.transform.position.y + .5f;
            else finish.y = groundY + .5f;
            level.CreateFinish(root.transform, finish);
            root.AddComponent<MountainHazardDirector>().Initialize(level.ledges, ledgeMaterial);
            if (level.ledges.Count > 10)
                root.AddComponent<SkyPredatorEpisode>().Initialize(level.ledges[10]);
            if (level.ledges.Count > 0) level.ledges[0].ShowTarget(true);
            Debug.Log($"LEVEL1_ROUTE_READY ledges={level.LedgeCount} maxJumpGap={maxJumpGap:F1} earlyGap={level.EarlyAverageGap:F1} lateGap={level.LateAverageGap:F1} treeBounds={treeBounds.Count} treeBlocked={treeBlocked} spawnY={spawn.y:F1} groundY={groundY:F1} lastY={last.y:F1} finishY={finish.y:F1}");
            return level;
        }

        private static List<Bounds> GetTreeBounds(Transform pillar)
        {
            var result = new List<Bounds>();
            AddMeshPartBounds(pillar.Find("Dense broadleaf forest and ledge shrubs"), result);
            AddMeshPartBounds(pillar.Find("Tree trunks and branches"), result);
            return result;
        }

        private static void AddMeshPartBounds(Transform part, List<Bounds> result)
        {
            var mesh = part ? part.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (!mesh || !mesh.isReadable) return;
            // The vista builder appends every crown and branch as a separate,
            // contiguous piece of mesh. Recover those pieces from triangle order.
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int first = -1, last = -1;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int lo = Mathf.Min(triangles[i], Mathf.Min(triangles[i + 1], triangles[i + 2]));
                int hi = Mathf.Max(triangles[i], Mathf.Max(triangles[i + 1], triangles[i + 2]));
                if (first >= 0 && lo > last)
                {
                    AddVertexBounds(part, vertices, first, last, result);
                    first = -1;
                }
                if (first < 0) first = lo;
                last = Mathf.Max(last, hi);
            }
            if (first >= 0) AddVertexBounds(part, vertices, first, last, result);
        }

        private static void AddVertexBounds(Transform part, Vector3[] vertices, int first, int last, List<Bounds> result)
        {
            Bounds bounds = new Bounds(part.TransformPoint(vertices[first]), Vector3.zero);
            for (int i = first + 1; i <= last; i++) bounds.Encapsulate(part.TransformPoint(vertices[i]));
            bounds.Expand(.6f);
            result.Add(bounds);
        }

        private static bool IsTreeClear(Vector3 wall, Vector3 outward, float width, List<Bounds> trees)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, outward);
            Vector3 halfSide = tangent * (width * .5f + 1f);
            Bounds landing = new Bounds(wall + outward * 2f + Vector3.up * 1.35f, Vector3.zero);
            foreach (float side in new[] { -1f, 1f })
            foreach (float depth in new[] { .2f, 5.3f })
            foreach (float height in new[] { -.3f, 4f })
                landing.Encapsulate(wall + halfSide * side + outward * depth + Vector3.up * height);
            foreach (Bounds tree in trees)
                if (tree.Intersects(landing)) return false;
            return true;
        }

        private static int[] PlanRoute(List<LedgeSite[]> sites, Vector3 spawn)
        {
            if (sites.Count == 0) return null;
            const float unreachable = 1e12f;
            int count = sites[0].Length;
            var cost = new float[sites.Count, count];
            var previous = new int[sites.Count, count];
            for (int i = 0; i < sites.Count; i++)
            for (int j = 0; j < count; j++) { cost[i, j] = unreachable; previous[i, j] = -1; }

            for (int j = 0; j < count; j++)
            {
                LedgeSite site = sites[0][j];
                if (!site.Clear || Mathf.Abs(site.Angle) > 45f) continue;
                float distance = HorizontalGap(spawn, site.Center);
                if (distance > 24f) continue;
                cost[0, j] = distance * .15f + Mathf.Abs(site.Angle) * .03f;
            }

            for (int i = 1; i < sites.Count; i++)
            {
                float progress = i / (float)(sites.Count - 1);
                float targetGap = Mathf.Lerp(4.4f, 7.5f, progress);
                float minimumGap = Mathf.Lerp(3.1f, 5.1f, progress);
                for (int j = 0; j < count; j++)
                {
                    LedgeSite site = sites[i][j];
                    if (!site.Clear) continue;
                    for (int k = 0; k < count; k++)
                    {
                        if (cost[i - 1, k] >= unreachable) continue;
                        float gap = HorizontalGap(sites[i - 1][k].Center, site.Center);
                        if (gap < minimumGap || gap > 8.2f) continue;
                        float candidateCost = cost[i - 1, k] + (gap - targetGap) * (gap - targetGap) + Mathf.Abs(site.Angle) * .001f;
                        if (candidateCost >= cost[i, j]) continue;
                        cost[i, j] = candidateCost;
                        previous[i, j] = k;
                    }
                }
                int clearCount = 0, reachableCount = 0;
                for (int j = 0; j < count; j++)
                {
                    if (sites[i][j].Clear) clearCount++;
                    if (cost[i, j] < unreachable) reachableCount++;
                }
                if (reachableCount == 0)
                {
                    Debug.LogWarning($"LEVEL1_ROUTE blocked at ring {i + 1}/{sites.Count}: clearSites={clearCount}");
                    return null;
                }
            }

            int best = -1;
            float bestCost = unreachable;
            int end = sites.Count - 1;
            for (int j = 0; j < count; j++)
                if (cost[end, j] < bestCost) { bestCost = cost[end, j]; best = j; }
            if (best < 0) return null;
            var route = new int[sites.Count];
            for (int i = end; i >= 0; i--)
            {
                route[i] = best;
                best = previous[i, best];
            }
            return route;
        }

        private static float HorizontalGap(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static void CreateStartCairn(Transform parent, MeshCollider cliff, Vector3 spawn, Vector3 face, Material material)
        {
            if (!cliff.Raycast(new Ray(spawn + face * 6f + Vector3.Cross(Vector3.up, face) * 2.5f + Vector3.up * 12f, Vector3.down), out RaycastHit ground, 30f)) return;
            var cairn = new GameObject("Start cairn — follow the cliff face");
            cairn.transform.SetParent(parent);
            cairn.transform.position = ground.point;
            var beaconStone = new Material(Shader.Find("Standard")) { name = "Warm start cairn stone", color = new Color(.9f, .58f, .24f) };
            for (int i = 0; i < 5; i++)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = "Warm route stone";
                stone.transform.SetParent(cairn.transform, false);
                stone.transform.localPosition = Vector3.up * (.23f + i * .29f);
                stone.transform.localScale = new Vector3(.94f - i * .1f, .55f, .8f - i * .08f);
                stone.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? beaconStone : material;
                Destroy(stone.GetComponent<Collider>());
            }
        }

        private static Vector3 CreateLedge(Transform parent, Vector3 wallPoint, Vector3 outward, Material material, int index, out GameObject shelf)
        {
            shelf = new GameObject($"Cliff ledge {index + 1:00} — rooted in rock");
            shelf.transform.SetParent(parent);
            shelf.transform.SetPositionAndRotation(wallPoint, Quaternion.LookRotation(outward, Vector3.up));
            float width = index % 7 == 4 ? 3.4f : 4.1f;
            float half = width * .5f;
            var vertices = new[]
            {
                new Vector3(-half * 1.1f, 0f, -3.2f), new Vector3(half * 1.1f, 0f, -3.2f),
                new Vector3(-half, 0f, 3.5f), new Vector3(half, 0f, 3.2f),
                new Vector3(-half * .85f, -1.7f, -2.7f), new Vector3(half * .85f, -1.7f, -2.7f),
                new Vector3(-half * .42f, -1.2f, 2.4f), new Vector3(half * .42f, -1.2f, 2.2f)
            };
            int[] triangles =
            {
                0, 2, 1, 1, 2, 3,
                0, 4, 2, 2, 4, 6, 3, 7, 1, 1, 7, 5,
                2, 6, 3, 3, 6, 7, 4, 5, 6, 5, 7, 6
            };
            var mesh = new Mesh { name = "Attached cliff shelf", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            shelf.AddComponent<MeshFilter>().sharedMesh = mesh;
            shelf.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = shelf.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, -.34f, .3f);
            collider.size = new Vector3(width * .9f, .68f, 5.6f);
            return wallPoint + outward * 2.3f;
        }

        private static Material MakeGlowMaterial(string name, Color color)
        {
            var result = new Material(Shader.Find("Standard")) { name = name, color = color };
            result.EnableKeyword("_EMISSION");
            result.SetColor("_EmissionColor", color * 1.8f);
            return result;
        }

        private static void MakeDecoration(Transform parent, PrimitiveType shape, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
        }

        private static void CreateRouteBeacon(Transform shelf, Material material, DescentLedge ledge)
        {
            var marker = new GameObject("Next landing marker");
            marker.transform.SetParent(shelf, false);
            MakeDecoration(marker.transform, PrimitiveType.Cylinder, "Beacon stem", new Vector3(0f, 1.3f, 2f), new Vector3(.12f, 1.2f, .12f), material);
            MakeDecoration(marker.transform, PrimitiveType.Sphere, "Beacon light", new Vector3(0f, 2.6f, 2f), new Vector3(.9f, .9f, .9f), material);
            ledge.TargetMarker = marker;
            marker.SetActive(false);
        }

        private static void CreateCheckpointMarker(Transform shelf, Material material)
        {
            MakeDecoration(shelf, PrimitiveType.Cylinder, "Checkpoint post", new Vector3(-1.45f, .55f, -1.35f), new Vector3(.15f, .55f, .15f), material);
            MakeDecoration(shelf, PrimitiveType.Sphere, "Checkpoint light", new Vector3(-1.45f, 1.17f, -1.35f), new Vector3(.38f, .38f, .38f), material);
        }

        private static void CreateSafetyRock(Transform shelf, Material material, int index)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "Solid side foothold";
            fallback.transform.SetParent(shelf, false);
            fallback.transform.localPosition = new Vector3(index % 2 == 0 ? 2.8f : -2.8f, -.27f, -.85f);
            fallback.transform.localScale = new Vector3(2.2f, .55f, 2.35f);
            fallback.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateBell(Transform shelf, int index)
        {
            var pickup = new GameObject($"Mountain bell {BellCount + 1}");
            pickup.transform.SetParent(shelf, false);
            pickup.transform.localPosition = new Vector3(index % 2 == 0 ? 1.15f : -1.15f, 1.05f, 2.1f);
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.radius = 1f;
            trigger.isTrigger = true;
            MakeDecoration(pickup.transform, PrimitiveType.Sphere, "Golden bell", Vector3.zero, new Vector3(.62f, .72f, .62f), bellMaterial);
            MakeDecoration(pickup.transform, PrimitiveType.Sphere, "Bell clapper", new Vector3(0f, -.43f, 0f), new Vector3(.23f, .23f, .23f), bellMaterial);
            pickup.AddComponent<MountainBell>().Initialize(this, $"BELL-{index + 1:00}");
            BellCount++;
        }

        private void CreateHornStone(DescentLedge source, DescentLedge destination)
        {
            var stone = new GameObject($"Horn shortcut {source.Index + 1} to {destination.Index + 1}");
            stone.transform.SetParent(source.transform, false);
            stone.transform.localPosition = new Vector3(0f, .35f, 2.15f);
            MakeDecoration(stone.transform, PrimitiveType.Sphere, "Violet strike stone", Vector3.zero, new Vector3(.85f, .65f, .85f), hornMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                var horn = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                horn.name = "Stone horn";
                horn.transform.SetParent(stone.transform, false);
                horn.transform.localPosition = new Vector3(side * .32f, .46f, 0f);
                horn.transform.localRotation = Quaternion.Euler(0f, 0f, side * -27f);
                horn.transform.localScale = new Vector3(.17f, .45f, .17f);
                horn.GetComponent<Renderer>().sharedMaterial = hornMaterial;
                Destroy(horn.GetComponent<Collider>());
            }
            stone.AddComponent<HornLaunchStone>().Initialize(this, source, destination);
        }

        private void FixedUpdate()
        {
            if (!MountainAuthority.IsHost) return;
            // One support sample per body per physics tick. Contact counts never multiply mass.
            participants.Clear();
            participants.AddRange(FindObjectsByType<GoatController>(FindObjectsSortMode.None));
            foreach (var goat in participants)
            {
                if (!goat || (goat.GetComponent<RespawnController>()?.IsDead ?? false)) continue;
                var ground = goat.GetComponent<GoatGroundDetector>();
                if (!ground || !ground.IsGrounded || ground.SlopeAngle >= 55f) continue;
                var ledge = ground.GroundHit.collider ? ground.GroundHit.collider.GetComponent<DescentLedge>() : null;
                if (ledge && ledge.Level == this) ledge.Touch(goat);
            }
            foreach (var ledge in ledges) ledge.EvaluateLoad(participants);
        }

        private void Update()
        {
            if (!MountainAuthority.IsHost && LocalGoatPair.Instance && LocalGoatPair.Instance.Active)
            {
                var ground = LocalGoatPair.Instance.Active.GetComponent<GoatGroundDetector>();
                var support = ground && ground.IsGrounded && ground.GroundHit.collider
                    ? ground.GroundHit.collider.GetComponent<DescentLedge>() : null;
                activeCrumble = support && support.IsCrumbling ? support : null;
            }
            if (Input.GetMouseButtonDown(1) && Camera.main
                && Physics.Raycast(Camera.main.ViewportPointToRay(new Vector3(.5f, .5f)), out var hit, 80f))
                selectedLedge = hit.collider.GetComponent<DescentLedge>();
        }

        private void OnGUI()
        {
            var ledge = selectedLedge;
            if (!ledge && LocalGoatPair.Instance && LocalGoatPair.Instance.Active)
            {
                var ground = LocalGoatPair.Instance.Active.GetComponent<GoatGroundDetector>();
                if (ground && ground.IsGrounded && ground.GroundHit.collider)
                    ledge = ground.GroundHit.collider.GetComponent<DescentLedge>();
            }
            if (!ledge) return;
            string details = $"{ledge.StableId}  {ledge.Phase}  {ledge.LoadKg:0}/{ledge.CapacityKg:0} кг\n"
                + $"До обрушения: {ledge.SecondsLeft:0.0} с  Причина: {ledge.LastReason}\n"
                + ledge.Contributions;
            GUI.Box(new Rect(Screen.width - 390, 16, 374, 78), details);
        }

        public MountainSnapshot CaptureSnapshot(int sequence)
        {
            var snapshot = new MountainSnapshot
            {
                sequence = sequence, schema = 5, mapSignature = MapSignature, hostTime = Time.time,
                ledges = new LedgeState[ledges.Count], teamProgress = TeamProgressIndex,
                scenario = LocalGoatPair.Instance ? LocalGoatPair.Instance.ScenarioCode : 0,
                completed = Completed, falls = Falls, bells = BellsCollected
            };
            for (int i = 0; i < ledges.Count; i++) snapshot.ledges[i] = ledges[i].CaptureState();
            var director = MountainHazardDirector.Current;
            snapshot.stones = director ? director.CaptureStones() : new StoneState[0];
            snapshot.loose = director ? director.CaptureSurfaces() : new LooseState[0];
            snapshot.birds = GetComponent<SkyPredatorEpisode>()?.CaptureState() ?? new BirdState[0];
            var bells = GetComponentsInChildren<MountainBell>(true);
            snapshot.bellsState = new BellState[bells.Length];
            for (int i = 0; i < bells.Length; i++) snapshot.bellsState[i] = bells[i].CaptureState();
            var goats = FindObjectsByType<GoatController>(FindObjectsSortMode.None);
            snapshot.goats = new GoatState[goats.Length];
            snapshot.players = new PlayerMountainProgress[goats.Length];
            for (int i = 0; i < goats.Length; i++)
            {
                var goat = goats[i];
                string id = goat.GetComponent<GoatLocalControl>()?.Label ?? goat.name;
                var body = goat.GetComponent<Rigidbody>();
                var visual = goat.GetComponent<GoatVisualController>();
                var interaction = goat.GetComponent<GoatInteraction>();
                snapshot.goats[i] = new GoatState
                { id = id, position = body.position, rotation = body.rotation, velocity = body.linearVelocity,
                  dead = goat.GetComponent<RespawnController>()?.IsDead ?? false,
                  linked = interaction?.IsLinked ?? false, holding = interaction?.IsHolding ?? false,
                  interactionStatus = interaction?.Status ?? "",
                  animation = visual ? visual.CurrentClip : "Goat_Idle",
                  facing = visual ? visual.Facing : goat.transform.forward };
                var progress = ProgressFor(goat);
                snapshot.players[i] = new PlayerMountainProgress
                { id = id, ledgeIndex = progress.Index, checkpointIndex = progress.CheckpointIndex,
                  finished = progress.Finished };
            }
            return snapshot;
        }

        public void ApplySnapshot(MountainSnapshot snapshot)
        {
            if (snapshot == null || MountainAuthority.IsHost) return;
            if (snapshot.ledges != null)
                foreach (var state in snapshot.ledges)
                    foreach (var ledge in ledges)
                        if (ledge.StableId == state.id) { ledge.ApplyState(state); break; }
            MountainHazardDirector.Current?.ApplyState(snapshot);
            GetComponent<SkyPredatorEpisode>()?.ApplyState(snapshot.birds);
            if (snapshot.bellsState != null)
                foreach (var state in snapshot.bellsState)
                    foreach (var bell in GetComponentsInChildren<MountainBell>(true))
                        if (bell.StableId == state.id) { bell.ApplyState(state); break; }
            TeamProgressIndex = snapshot.teamProgress;
            LocalGoatPair.Instance?.ApplyNetworkScenario(snapshot.scenario);
            Completed = snapshot.completed; Falls = snapshot.falls; BellsCollected = snapshot.bells;
            if (snapshot.players != null)
                foreach (var state in snapshot.players)
                    foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
                        if ((goat.GetComponent<GoatLocalControl>()?.Label ?? goat.name) == state.id)
                        {
                            var progress = ProgressFor(goat);
                            progress.Index = state.ledgeIndex;
                            progress.CheckpointIndex = state.checkpointIndex;
                            progress.Finished = state.finished;
                            break;
                        }
            if (snapshot.goats != null)
                foreach (var state in snapshot.goats)
                    foreach (var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
                        if ((goat.GetComponent<GoatLocalControl>()?.Label ?? goat.name) == state.id)
                        {
                            var body = goat.GetComponent<Rigidbody>();
                            body.isKinematic = true;
                            body.position = state.position;
                            body.rotation = state.rotation;
                            goat.SetNetworkVelocity(state.velocity);
                            goat.GetComponent<GoatVisualController>()?.ApplyNetworkPose(state.animation, state.facing);
                            goat.GetComponent<GoatInteraction>()?.ApplyNetworkState(state.linked,
                                state.holding, state.interactionStatus);
                            goat.GetComponent<RespawnController>()?.ApplyNetworkState(state.dead,
                                ProgressFor(goat).CheckpointIndex >= 0);
                            break;
                        }
            RefreshTargetMarkers();
        }

        private PersonalProgress ProgressFor(GoatController goat)
        {
            if (!playerProgress.TryGetValue(goat, out var value))
                playerProgress.Add(goat, value = new PersonalProgress());
            return value;
        }

        public void OnLedgeReached(GoatController goat, DescentLedge ledge)
        {
            if (!goat || Completed) return;
            var personal = ProgressFor(goat);
            if (!started) { started = true; startTime = Time.time; Announce("Спуск начался — доберись до долины!"); }
            if (!LocalGoatPair.Instance || goat == LocalGoatPair.Instance.Active)
                activeCrumble = ledge.IsCrumbling ? ledge : null;
            if (ledge.Index <= personal.Index) return;
            personal.Index = ledge.Index;
            TeamProgressIndex = Mathf.Max(TeamProgressIndex, ledge.Index);
            RefreshTargetMarkers();
            if (ledge.Index == 12 || ledge.Index == 27 || ledge.Index == 41)
            {
                personal.CheckpointIndex = ledge.Index;
                goat.GetComponent<RespawnController>()?.SetCheckpoint(ledge.LandingPoint + Vector3.up * .75f);
                Announce("Точка сохранения — теперь возродишься здесь!");
            }
        }

        public void RefreshTargetMarkers()
        {
            foreach (var ledge in ledges) ledge.ShowTarget(false);
            int next = ProgressIndex + 1;
            if (next >= 0 && next < ledges.Count) ledges[next].ShowTarget(true);
        }

        public void CollectBell()
        {
            if (!MountainAuthority.IsHost) return;
            BellsCollected++;
            Announce($"Колокольчик! {BellsCollected}/{BellCount}");
        }

        public void NotifyVaultStarted(HornLaunchStone stone)
        {
            foreach (var ledge in ledges) ledge.ShowTarget(false);
            stone.Destination.ShowTarget(true);
            Announce($"Роговой перелёт! Пропусти {stone.SkippedLedges} выступа и попади на голубой камень");
        }

        public void NotifyVaultCompleted()
        {
            VaultsUsed++;
            Announce($"Роговой перелёт удался! Ускорений: {VaultsUsed}/3");
        }

        public void RegisterFall() { Falls++; Announce("Козёл сорвался — возвращение к точке сохранения"); }

        public void OnPlayerRespawn(RespawnController life)
        {
            if (!life) return;
            var goat = life.GetComponent<GoatController>();
            if (!goat) return;
            var personal = ProgressFor(goat);
            personal.Index = life.HasCheckpoint ? personal.CheckpointIndex : -1;
            personal.Finished = false;
            if (!LocalGoatPair.Instance || goat == LocalGoatPair.Instance.Active)
            { activeCrumble = null; RefreshTargetMarkers(); }
        }

        private void Announce(string message) { eventText = message; eventUntil = Time.time + 3.2f; }


        private void CreateFinish(Transform parent, Vector3 position)
        {
            var goal = new GameObject("Valley finish");
            goal.transform.SetParent(parent);
            goal.transform.position = position;
            var collider = goal.AddComponent<SphereCollider>();
            collider.radius = 3f;
            collider.isTrigger = true;
            goal.AddComponent<PillarFinishTrigger>().Level = this;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Finish cairn";
            marker.transform.SetParent(goal.transform, false);
            marker.transform.localPosition = Vector3.up * .9f;
            marker.transform.localScale = new Vector3(.45f, .9f, .45f);
            Destroy(marker.GetComponent<Collider>());
        }

        public void Complete(GoatController goat)
        {
            if (!MountainAuthority.IsHost || !goat || Completed) return;
            ProgressFor(goat).Finished = true;
            foreach (var participant in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
            {
                if (LocalGoatPair.Instance && LocalGoatPair.Instance.NetworkMode
                    && participant == LocalGoatPair.Instance.Secondary
                    && !(MountainNetSession.Current?.Connected ?? false)) continue;
                if (participant && !ProgressFor(participant).Finished)
                { Announce("Один козёл добрался до долины — ждём остальных"); return; }
            }
            Completed = true;
            finishTime = started ? Time.time - startTime : 0f;
            Announce($"Долина! Колокольчики {BellsCollected}/{BellCount}, перелёты {VaultsUsed}/3, падения {Falls}, время {finishTime:0.0} с");
        }

        public void Complete() => Complete(LocalGoatPair.Instance ? LocalGoatPair.Instance.Primary : FindFirstObjectByType<GoatController>());
    }

    public sealed class DescentLedge : MonoBehaviour
    {
        public enum LedgePhase : byte { Intact, Strained, Cracking, Broken, Restoring }
        public PillarDescentLevel Level => level;
        public int Index { get; private set; }
        public string StableId => $"LEDGE-{Index + 1:000}";
        public Vector3 LandingPoint { get; private set; }
        public bool IsCrumbling { get; private set; }
        public LedgePhase Phase { get; private set; }
        public bool IsCracking => Phase == LedgePhase.Strained || Phase == LedgePhase.Cracking;
        public float SecondsLeft => !MountainAuthority.IsHost
            ? Mathf.Max(0f, clientSecondsLeft - (Time.time - clientStateAt))
            : IsCracking && LoadKg > CapacityKg
                ? Mathf.Max(0f, (1f - stress) * 1.25f / ((LoadKg - CapacityKg) / CapacityKg)) : 0f;
        public float CapacityKg { get; private set; }
        public float LoadKg { get; private set; }
        public float Stress => stress;
        public int VisibleCrackCount
        {
            get
            {
                if (crackLines == null) return 0;
                int count = 0;
                foreach (var line in crackLines) if (line && line.enabled) count++;
                return count;
            }
        }
        public string LastReason { get; private set; } = "создан";
        public string Contributions { get; private set; } = "нет опоры";
        public GameObject TargetMarker { get; set; }

        private PillarDescentLevel level;
        private BoxCollider platform;
        private Bounds platformBounds;
        private MeshRenderer stone;
        private Material stoneMaterial;
        private Color originalColor;
        private float stress;
        private float lastCrackSound;
        private float restoreAt;
        private float finishRestoringAt;
        private float clientStateAt, clientSecondsLeft;
        private float rockImpactKg, rockImpactUntil;
        private AudioSource crackAudio;
        private static AudioClip crackClip;
        private static Material crackMaterial;
        private LineRenderer[] crackLines;
        private readonly Dictionary<GoatController, float> impactKg = new Dictionary<GoatController, float>();
        private readonly Dictionary<GoatController, float> impactUntil = new Dictionary<GoatController, float>();
        private readonly HashSet<GoatController> counted = new HashSet<GoatController>();

        public void Initialize(PillarDescentLevel owner, int number, Vector3 point, bool fragile, float capacityKg)
        {
            level = owner;
            Index = number;
            LandingPoint = point;
            IsCrumbling = fragile;
            CapacityKg = capacityKg;
            platform = GetComponent<BoxCollider>();
            platformBounds = platform.bounds;
            stone = GetComponent<MeshRenderer>();
            if (!fragile) return;
            stoneMaterial = stone.material;
            originalColor = stoneMaterial.HasProperty("_StoneColor") ? stoneMaterial.GetColor("_StoneColor") : stoneMaterial.color;
            crackAudio = gameObject.AddComponent<AudioSource>();
            crackAudio.spatialBlend = 1f; crackAudio.maxDistance = 38f; crackAudio.playOnAwake = false;
            if (!crackClip) crackClip = MakeCrackClip();
            CreateCrackLines();
            UpdateCrackVisuals();
        }

        public void ShowTarget(bool visible) { if (TargetMarker) TargetMarker.SetActive(visible); }

        public void Touch(GoatController goat)
        {
            if (Phase == LedgePhase.Broken || Phase == LedgePhase.Restoring) return;
            level.OnLedgeReached(goat, this);
        }

        public LedgeState CaptureState() => new LedgeState
        {
            id = StableId, phase = (byte)Phase, loadKg = LoadKg, capacityKg = CapacityKg,
            stress = stress, secondsLeft = SecondsLeft, reason = LastReason,
            contributions = Contributions
        };

        public void ApplyState(LedgeState state)
        {
            if (state.id != StableId || MountainAuthority.IsHost) return;
            var previousPhase = Phase;
            Phase = (LedgePhase)state.phase; LoadKg = state.loadKg;
            CapacityKg = state.capacityKg; stress = state.stress;
            LastReason = state.reason; Contributions = state.contributions;
            clientSecondsLeft = state.secondsLeft; clientStateAt = Time.time;
            bool solid = Phase != LedgePhase.Broken && Phase != LedgePhase.Restoring;
            platform.enabled = solid;
            stone.enabled = Phase != LedgePhase.Broken;
            UpdateCrackVisuals();
            if (stoneMaterial) SetStoneColor(Color.Lerp(originalColor,
                new Color(1f, .24f, .09f), Mathf.Clamp01(stress)));
            if ((Phase == LedgePhase.Cracking
                    && (previousPhase != LedgePhase.Cracking || Time.time - lastCrackSound > .85f))
                || (Phase == LedgePhase.Broken && previousPhase != LedgePhase.Broken))
                SoundCrack();
        }

        public void EvaluateLoad(IReadOnlyList<GoatController> goats)
        {
            if (!MountainAuthority.IsHost || !IsCrumbling) return;
            LoadKg = rockImpactUntil > Time.time ? rockImpactKg * (rockImpactUntil - Time.time) / .5f : 0f;
            counted.Clear();
            var descriptions = new List<string>();
            if (LoadKg > 0f) descriptions.Add($"удар камня: {LoadKg:0} кг");
            foreach (var goat in goats)
            {
                if (!goat || counted.Contains(goat) || (goat.GetComponent<RespawnController>()?.IsDead ?? false)) continue;
                var ground = goat.GetComponent<GoatGroundDetector>();
                if (!ground || !ground.IsGrounded || ground.GroundHit.collider != platform) continue;
                var body = goat.GetComponent<Rigidbody>();
                counted.Add(goat);
                float extra = impactUntil.TryGetValue(goat, out float until) && Time.time < until
                    ? impactKg[goat] * (until - Time.time) / .35f : 0f;
                LoadKg += body.mass + extra;
                descriptions.Add($"{goat.name}: {body.mass:0} + удар {extra:0} кг");
                var partner = goat.GetComponent<GoatInteraction>()?.Partner;
                if (!partner) continue;
                var hangingGoat = partner.GetComponent<GoatController>();
                var partnerGround = partner.GetComponent<GoatGroundDetector>();
                if (!hangingGoat || counted.Contains(hangingGoat) || !partnerGround
                    || (partnerGround.IsGrounded && partnerGround.SlopeAngle < 55f)
                    || (hangingGoat.GetComponent<RespawnController>()?.IsDead ?? false)) continue;
                counted.Add(hangingGoat);
                float suspended = hangingGoat.GetComponent<Rigidbody>().mass;
                LoadKg += suspended;
                descriptions.Add($"{hangingGoat.name}: {suspended:0} кг через сцепление");
            }
            Contributions = descriptions.Count > 0 ? string.Join("; ", descriptions) : "нет опоры";
            if (Phase == LedgePhase.Broken)
            {
                if (Time.time >= restoreAt)
                { Phase = LedgePhase.Restoring; finishRestoringAt = Time.time + .8f;
                  LastReason = "ожидание свободного места"; stone.enabled = true; }
                UpdateCrackVisuals();
                return;
            }
            if (Phase == LedgePhase.Restoring)
            {
                if (Time.time >= finishRestoringAt && !WouldTrapGoat(goats)) ResetCrumble();
                else UpdateCrackVisuals();
                return;
            }
            if (LoadKg > CapacityKg)
            {
                float overload = (LoadKg - CapacityKg) / CapacityKg;
                stress += Time.fixedDeltaTime * overload / 1.25f;
                var next = stress >= 1f ? LedgePhase.Broken
                    : stress >= .18f ? LedgePhase.Cracking : LedgePhase.Strained;
                if (next != Phase)
                {
                    Phase = next;
                    LastReason = $"перегрузка {LoadKg:0}/{CapacityKg:0} кг";
                    if (Phase == LedgePhase.Cracking) SoundCrack();
                }
                if (Phase == LedgePhase.Cracking && Time.time - lastCrackSound > .85f) SoundCrack();
                if (Phase == LedgePhase.Broken)
                {
                    platform.enabled = false; stone.enabled = false; ShowTarget(false);
                    restoreAt = Time.time + 12f;
                    LastReason = $"разрушен нагрузкой {LoadKg:0} кг";
                    SoundCrack();
                    Debug.Log($"MOUNTAIN_LEDGE {StableId} {LastReason}");
                    MountainHazardDirector.Current?.OnLedgeBroken(this);
                }
            }
            else
            {
                stress = Mathf.MoveTowards(stress, 0f, Time.fixedDeltaTime * .25f);
                var next = stress <= 0f ? LedgePhase.Intact
                    : stress >= .18f ? LedgePhase.Cracking : LedgePhase.Strained;
                if (next != Phase) { Phase = next; LastReason = "нагрузка ушла, трещины затягиваются"; }
            }
            SetStoneColor(Color.Lerp(originalColor, new Color(1f, .24f, .09f), Mathf.Clamp01(stress)));
            UpdateCrackVisuals();
        }

        public void ResetCrumble()
        {
            if (!IsCrumbling) return;
            Phase = LedgePhase.Intact;
            stress = 0f; LastReason = "восстановлен по правилу уровня";
            platform.enabled = true;
            stone.enabled = true;
            SetStoneColor(originalColor);
            UpdateCrackVisuals();
        }

        private void CreateCrackLines()
        {
            if (!crackMaterial)
            {
                crackMaterial = new Material(Shader.Find("Standard"))
                { name = "Visible ledge fissures", color = new Color(.07f, .025f, .018f) };
                crackMaterial.SetFloat("_Glossiness", 0f);
            }
            var paths = new[]
            {
                new[] { new Vector3(-1.35f, .075f, -2f), new Vector3(-.55f, .075f, -1.15f),
                    new Vector3(-.86f, .075f, -.35f), new Vector3(.05f, .075f, .55f),
                    new Vector3(.72f, .075f, 1.7f) },
                new[] { new Vector3(-.86f, .08f, -.35f), new Vector3(-1.55f, .08f, .3f),
                    new Vector3(-1.25f, .08f, 1.12f) },
                new[] { new Vector3(.05f, .08f, .55f), new Vector3(.98f, .08f, .12f),
                    new Vector3(1.53f, .08f, .78f) }
            };
            crackLines = new LineRenderer[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                var trace = new GameObject($"Stress fissure {i + 1}");
                trace.transform.SetParent(transform, false);
                var line = trace.AddComponent<LineRenderer>();
                line.sharedMaterial = crackMaterial;
                line.useWorldSpace = false;
                line.positionCount = paths[i].Length;
                line.SetPositions(paths[i]);
                line.startWidth = line.endWidth = i == 0 ? .095f : .075f;
                line.numCornerVertices = 2;
                line.numCapVertices = 2;
                line.receiveShadows = false;
                line.enabled = false;
                crackLines[i] = line;
            }
        }

        private void UpdateCrackVisuals()
        {
            if (crackLines == null) return;
            int visible = Phase == LedgePhase.Cracking ? crackLines.Length
                : Phase == LedgePhase.Strained ? 1 : 0;
            for (int i = 0; i < crackLines.Length; i++)
                if (crackLines[i]) crackLines[i].enabled = i < visible;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!MountainAuthority.IsHost || !IsCrumbling || Phase >= LedgePhase.Broken) return;
            var goat = collision.rigidbody ? collision.rigidbody.GetComponent<GoatController>() : null;
            if (!goat) return;
            float speed = Mathf.Abs(collision.relativeVelocity.y);
            if (speed <= 3f) return;
            impactKg[goat] = goat.GetComponent<Rigidbody>().mass * Mathf.Clamp01((speed - 3f) / 7f) * .65f;
            impactUntil[goat] = Time.time + .35f;
            LastReason = $"приземление {goat.name}, {speed:0.0} м/с";
        }

        public void RegisterRockImpact(float impulse)
        {
            if (!MountainAuthority.IsHost || !IsCrumbling || Phase >= LedgePhase.Broken) return;
            rockImpactKg = Mathf.Min(220f, impulse * .4f);
            rockImpactUntil = Time.time + .5f;
            LastReason = $"удар падающего камня {impulse:0} Н·с";
        }

        private bool WouldTrapGoat(IReadOnlyList<GoatController> goats)
        {
            foreach (var goat in goats)
            {
                if (!goat || (goat.GetComponent<RespawnController>()?.IsDead ?? false)) continue;
                var capsule = goat.GetComponent<CapsuleCollider>();
                if (capsule && platformBounds.Intersects(capsule.bounds)) return true;
            }
            return false;
        }

        private void SoundCrack()
        {
            if (crackAudio && crackClip) crackAudio.PlayOneShot(crackClip);
            lastCrackSound = Time.time;
        }

        private static AudioClip MakeCrackClip()
        {
            const int sampleRate = 22050; const int samples = 5500;
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-16f * t) * Mathf.Min(1f, t * 400f);
                data[i] = envelope * .27f * (Mathf.Sin(i * .73f) + Mathf.Sin(i * 1.91f) * .52f);
            }
            var clip = AudioClip.Create("Rock overload crack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void SetStoneColor(Color color)
        {
            if (stoneMaterial.HasProperty("_StoneColor")) stoneMaterial.SetColor("_StoneColor", color);
            else stoneMaterial.color = color;
        }
    }

    public sealed class MountainBell : MonoBehaviour
    {
        public string StableId { get; private set; }
        private PillarDescentLevel level;
        private Vector3 basePosition;
        private bool collected;

        public void Initialize(PillarDescentLevel owner, string id)
        { level = owner; StableId = id; basePosition = transform.localPosition; }
        public BellState CaptureState() => new BellState { id = StableId, collected = collected };
        public void ApplyState(BellState state)
        { if (MountainAuthority.IsHost || state.id != StableId) return;
          collected = state.collected; gameObject.SetActive(!collected); }
        private void Update()
        {
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * .18f);
            transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.Self);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!MountainAuthority.IsHost) return;
            if (collected || !other.GetComponent<GoatController>() || !GoatLocalControl.CountsForRoute(other)) return;
            collected = true;
            level.CollectBell();
            gameObject.SetActive(false);
        }
    }

    public sealed class PillarFinishTrigger : MonoBehaviour
    {
        public PillarDescentLevel Level { get; set; }
        private void OnTriggerEnter(Collider other)
        {
            var goat = other.GetComponent<GoatController>();
            if (goat) Level?.Complete(goat);
        }
    }
}
