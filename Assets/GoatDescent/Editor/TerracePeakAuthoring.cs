using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class TerracePeakAuthoring
    {
        private const string AssetsPath = "Assets/GoatDescent/Level/Static/TerracePeak";
        private const string ScenePath = "Assets/GoatDescent/Scenes/TerracePeak.unity";

        [MenuItem("Tools/GOAT/Статичные горы/Уступчатый пик — открыть")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Уступчатого пика."); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Уступчатый пик — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AssetsPath);
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Уступчатый пик — статичная гора").transform;
            root.localScale = Vector3.one * 5f;
            var cliffs = new GameObject("Скальный массив и рёбра").transform; cliffs.SetParent(root, false);
            var grotto = new GameObject("Грот").transform; grotto.SetParent(root, false);
            var shelves = new GameObject("Зелёные полки").transform; shelves.SetParent(root, false);
            var valley = new GameObject("Долина и река").transform; valley.SetParent(root, false);
            var forest = new GameObject("Леса").transform; forest.SetParent(root, false);
            var stones = new GameObject("Валуны").transform; stones.SetParent(root, false);
            var far = new GameObject("Дальние снежные пики").transform; far.SetParent(root, false);

            Vector3 sunDir = -(Quaternion.Euler(42f, 30f, 0f) * Vector3.forward);
            var rock = RockMaterial("WarmTerraceRock", new Color(.8f, .64f, .44f), new Color(.97f, .81f, .55f),
                new Color(.38f, .5f, .32f), .0044f, .45f, sunDir);
            var lightRock = RockMaterial("SunlitTerraceRock", new Color(.86f, .7f, .48f), new Color(1f, .84f, .58f),
                new Color(.42f, .53f, .35f), .0052f, .38f, sunDir);
            var grottoRock = RockMaterial("GrottoRock", new Color(.46f, .38f, .3f), new Color(.66f, .52f, .36f),
                new Color(.3f, .36f, .29f), .0075f, .4f, sunDir);
            grottoRock.SetFloat("_ShadowFloor", .5f);
            grottoRock.SetFloat("_Stripe", .3f);
            grottoRock.SetFloat("_Emissive", .12f);
            grottoRock.SetColor("_EmissiveTint", new Color(1f, .74f, .46f));
            var meadow = MeadowMaterial("ValleyMeadow", new Color(.37f, .51f, .3f), new Color(.53f, .62f, .4f),
                new Color(.45f, .4f, .3f), sunDir);
            var shelfGrass = MeadowMaterial("TerraceGrass", new Color(.38f, .5f, .32f), new Color(.52f, .6f, .42f),
                new Color(.44f, .4f, .32f), sunDir);
            var ledgeTuft = MeadowMaterial("LedgeTuft", new Color(.33f, .46f, .29f), new Color(.46f, .56f, .4f),
                new Color(.38f, .44f, .32f), sunDir);
            var needles = Simple("FirNeedles", new Color(.13f, .23f, .13f), .04f);
            var trunks = Simple("FirTrunk", new Color(.2f, .16f, .12f), .04f);
            var water = WaterMaterial();
            var snow = FarMaterial("SnowCaps", new Color(.84f, .88f, .93f));
            var farRock = FarMaterial("DistantRock", new Color(.5f, .57f, .68f));
            var scree = Simple("ScreeApron", new Color(.62f, .54f, .44f), .05f);
            var trailDirt = Simple("TrailDirt", new Color(.55f, .44f, .28f), .06f);

            Emit(cliffs, "Уступчатый массив", "Massif", TerracePeakMesh.Massif(), rock, true);
            Emit(cliffs, "Скальные фасады", "Facades", TerracePeakMesh.Facades(), rock, false);
            Emit(cliffs, "Скальные рёбра", "Ribs", TerracePeakMesh.Ribs(), lightRock, false);
            Emit(cliffs, "Скальные карнизы", "Ledges", TerracePeakMesh.Ledges(), lightRock, false);
            Emit(cliffs, "Осыпные шлейфы", "Talus", TerracePeakMesh.Talus(), scree, false);
            Emit(cliffs, "Обвалы ярусов", "Collapse", TerracePeakMesh.Collapse(), rock, false);
            Emit(cliffs, "Скальный гребень вершины", "Spires", TerracePeakMesh.Spires(), rock, false);
            Emit(cliffs, "Площадка перед гротом", "Balcony", TerracePeakMesh.Balcony(), lightRock, true);
            Emit(cliffs, "Площадка за гротом", "ExitBalcony", TerracePeakMesh.ExitBalcony(), lightRock, true);
            Emit(grotto, "Скальный козырёк грота", "GrottoBrow", TerracePeakMesh.GrottoBrow(), rock, false);
            Emit(grotto, "Скальная обвязка выхода грота", "ExitGrottoBrow", TerracePeakMesh.ExitGrottoBrow(), rock, false);
            Emit(grotto, "Тёмная боковая ниша грота", "GrottoInterior", TerracePeakMesh.GrottoInterior(), grottoRock, false);
            Emit(grotto, "Сквозной грот — туннель", "GrottoTunnel", TerracePeakMesh.GrottoTunnel(), grottoRock, true);
            Emit(grotto, "Световые шахты грота", "GrottoShafts", TerracePeakMesh.GrottoShafts(), lightRock, false);
            Emit(shelves, "Травяные полки", "ShelfGrass", TerracePeakMesh.ShelfGrass(), shelfGrass, false);
            Emit(shelves, "Свисающая зелень", "MossFringe", TerracePeakMesh.MossFringe(), ledgeTuft, false);
            Emit(valley, "Долина у подножия", "Valley", TerracePeakMesh.Valley(), meadow, true);
            Emit(valley, "Бирюзовая река", "River", TerracePeakMesh.River(), water, false);
            Emit(forest, "Кроны елей", "ForestNeedles", TerracePeakMesh.Forest(false), needles, false);
            Emit(forest, "Стволы елей", "ForestTrunks", TerracePeakMesh.Forest(true), trunks, false);
            Emit(stones, "Валуны", "Boulders", TerracePeakMesh.Boulders(), rock, false);
            Emit(far, "Дальние хребты", "FarPeaks", TerracePeakMesh.FarPeaks(false), farRock, false);
            Emit(far, "Снежные шапки", "SnowCaps", TerracePeakMesh.FarPeaks(true), snow, false);

            var trailPoints = BuildRoute(out float trailLength);
            var trail = GoatTrailBuilder.Build(root, "Козья тропа — восхождение", TerracePeakMesh.GroundHeight,
                trailDirt, trailPoints, 3.6f, .5f, 0f, true, new Color(.42f, .34f, .2f));
            trail.gameObject.isStatic = true;
            var landmarkPoints = new List<Vector3>();
            var landmarkScales = new List<float>();
            int[] anchors = { 2, 18, 34, 52, 68, 84 };
            for (int index = 0; index < anchors.Length; index++)
            {
                int at = Mathf.Clamp(Mathf.RoundToInt((trailPoints.Count - 1) * anchors[index] / 100f), 0, trailPoints.Count - 1);
                Vector3 point = trailPoints[at];
                Vector3 back = trailPoints[Mathf.Max(0, at - 1)];
                Vector3 side = Vector3.Cross(Vector3.up, (point - back).normalized);
                landmarkPoints.Add(point + side * (7f + index * 1.4f));
                landmarkScales.Add(4.5f + index * .9f);
            }
            Emit(stones, "Камни-ориентиры тропы", "Landmarks",
                TerracePeakMesh.RockField(landmarkPoints, landmarkScales, 901), rock, false);
            Emit(stones, "Отметки вейпоинтов", "WaypointStuds",
                TerracePeakMesh.WaypointStuds(trailPoints), trailDirt, false);
            Emit(stones, "Камни по краю тропы", "TrailStones",
                TerracePeakMesh.TrailStones(trailPoints, 3.6f), trailDirt, false);
            Emit(stones, "Останцы долины", "ValleyOutcrops", TerracePeakMesh.ValleyOutcrops(), rock, false);

            int jumpCount = BuildJumpSpots(root, trailPoints, rock);
            int checkpointCount = BuildCheckpoints(root, trailPoints, trailLength, lightRock);
            GrottoLights(grotto);

            Debug.Log("GOAT_TRAIL TerracePeak waypoints=" + trailPoints.Count + " length~"
                + Mathf.RoundToInt(trailLength) + "м jumps=" + jumpCount + " checkpoints=" + checkpointCount);

            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 1.5f;
            camera.farClipPlane = 16000f;
            camera.transform.position = new Vector3(600f, 1675f, -3200f);
            camera.transform.LookAt(new Vector3(0f, 1425f, 0f));
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.warmTint = new Color(1.07f, 1f, .88f);
            post.coolTint = new Color(.93f, .97f, 1.05f);
            post.saturation = 1.22f;
            post.contrast = 1.12f;
            post.vignette = .32f;
            post.bloomThreshold = .78f;
            post.bloomIntensity = .5f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Render(camera, "TerracePeak-overview");
            camera.transform.position = new Vector3(350f, 1425f, -2700f);
            camera.transform.LookAt(new Vector3(0f, 1375f, -300f));
            Render(camera, "TerracePeak-grotto");
            camera.transform.position = new Vector3(0f, 1245f, -620f);
            camera.transform.LookAt(new Vector3(0f, 1240f, 600f));
            Render(camera, "TerracePeak-tunnel");
            camera.transform.position = new Vector3(2150f, 1875f, -1775f);
            camera.transform.LookAt(new Vector3(0f, 1375f, 0f));
            Render(camera, "TerracePeak-terrace");
            camera.transform.position = new Vector3(50f, 300f, -3100f);
            camera.transform.LookAt(new Vector3(0f, 1075f, 0f));
            Render(camera, "TerracePeak-valley");
            camera.transform.position = new Vector3(-1975f, 1700f, 3050f);
            camera.transform.LookAt(new Vector3(0f, 1175f, 0f));
            Render(camera, "TerracePeak-back");
            camera.transform.position = new Vector3(-350f, 2725f, -4950f);
            camera.transform.LookAt(new Vector3(0f, 1450f, 1200f));
            Render(camera, "TerracePeak-horizon");
            Debug.Log("TERRACE_PEAK_STATIC_SAVED scene=" + ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Уступчатый пик — снимки маршрута")]
        public static void RouteShots()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Уступчатого пика."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var route = BuildRoute(out _);
            var mountain = GameObject.Find("Уступчатый пик — статичная гора");
            Transform frame = mountain ? mountain.transform : null;
            var camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (!camera)
            {
                camera = new GameObject("Route Camera").AddComponent<Camera>();
                camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            }
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 1.2f;
            camera.farClipPlane = 16000f;
            ShadowLift();
            const int shots = 8;
            const float rise = 12f;
            const float setback = 10f;
            const float lookAhead = 25f;
            const float minClearance = 6f;
            const float rayClearance = 2f;
            const float maxLift = 30f;
            const float liftStep = 6f;
            const float shiftStep = 8f;
            const int maxShifts = 16;
            int rendered = 0;
            for (int index = 0; index < shots; index++)
            {
                int at = Mathf.Clamp(Mathf.RoundToInt((route.Count - 1) * index / (float)(shots - 1)), 0, route.Count - 1);
                int back = Mathf.Max(0, at - 3);
                int ahead = Mathf.Min(route.Count - 1, at + 3);
                Vector3 from = frame ? frame.TransformPoint(route[back]) : route[back];
                Vector3 to = frame ? frame.TransformPoint(route[ahead]) : route[ahead];
                Vector3 tangent = to - from;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < .001f) tangent = new Vector3(-from.z, 0f, from.x);
                if (tangent.sqrMagnitude < .001f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 waypoint = frame ? frame.TransformPoint(route[at]) : route[at];
                Vector3 position = waypoint - tangent * setback + Vector3.up * rise;
                Vector3 target = PointAhead(route, at, lookAhead, frame) + Vector3.up * 2f;
                bool clear = TryClearView(frame, position, target, minClearance, rayClearance,
                    maxLift, liftStep, out position, out int liftUsed);
                int shiftUsed = 0;
                if (!clear)
                {
                    Vector3 origin = frame ? frame.position : Vector3.zero;
                    Vector3 outward = waypoint - origin;
                    outward.y = 0f;
                    if (outward.sqrMagnitude < .001f) outward = -tangent;
                    outward.Normalize();
                    Vector3 anchor = position;
                    for (int shift = 1; shift <= maxShifts && !clear; shift++)
                    {
                        Vector3 candidate = anchor + outward * (shiftStep * shift);
                        if (TryClearView(frame, candidate, target, minClearance, rayClearance,
                                maxLift, liftStep, out candidate, out int candidateLift))
                        {
                            position = candidate;
                            liftUsed = candidateLift;
                            shiftUsed = shift;
                            clear = true;
                        }
                    }
                }
                if (!clear)
                {
                    Debug.LogWarning("ROUTE_SHOTS TerracePeak frame " + (index + 1).ToString("00")
                        + " skipped: no clear camera position");
                    continue;
                }
                Debug.Log("ROUTE_SHOTS TerracePeak frame " + (index + 1).ToString("00")
                    + " lift=" + Mathf.RoundToInt(liftUsed * liftStep) + "м shift="
                    + Mathf.RoundToInt(shiftUsed * shiftStep) + "м");
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);
                Render(camera, "route-TerracePeak-" + (index + 1).ToString("00"));
                rendered++;
            }
            Debug.Log("ROUTE_SHOTS TerracePeak count=" + rendered);
            Debug.Log("ROUTE_SHOTS TerracePeak waypoints=" + route.Count);
        }

        private static Vector3 PointAhead(List<Vector3> route, int at, float meters, Transform frame)
        {
            Vector3 current = frame ? frame.TransformPoint(route[at]) : route[at];
            Vector3 result = current;
            float remaining = meters;
            for (int index = at; index < route.Count - 1 && remaining > 0f; index++)
            {
                Vector3 first = frame ? frame.TransformPoint(route[index]) : route[index];
                Vector3 second = frame ? frame.TransformPoint(route[index + 1]) : route[index + 1];
                float length = Vector3.Distance(first, second);
                if (length >= remaining)
                    return Vector3.Lerp(first, second, remaining / Mathf.Max(length, .0001f));
                remaining -= length;
                result = second;
            }
            return result;
        }

        private static bool TryClearView(Transform frame, Vector3 origin, Vector3 target,
            float minClearance, float rayClearance, float maxLift, float liftStep,
            out Vector3 position, out int liftUsed)
        {
            int steps = Mathf.Max(0, Mathf.RoundToInt(maxLift / liftStep));
            for (int step = 0; step <= steps; step++)
            {
                Vector3 candidate = origin;
                candidate.y = origin.y + Mathf.Min(maxLift, step * liftStep);
                if (ClearView(frame, candidate, target, minClearance, rayClearance))
                {
                    position = candidate;
                    liftUsed = step;
                    return true;
                }
            }
            position = origin;
            liftUsed = 0;
            return false;
        }

        private static bool ClearView(Transform frame, Vector3 position, Vector3 target,
            float minClearance, float rayClearance)
        {
            if (position.y < SurfaceHeight(frame, position) + minClearance) return false;
            Vector3 direction = target - position;
            float distance = direction.magnitude;
            if (distance < .001f) return false;
            direction /= distance;
            float[] probes = { 10f, 20f, 30f };
            for (int index = 0; index < probes.Length; index++)
            {
                if (probes[index] > distance + 4f) break;
                Vector3 probe = position + direction * probes[index];
                if (SurfaceHeight(frame, probe) > position.y + rayClearance) return false;
            }
            return true;
        }

        private static float SurfaceHeight(Transform frame, Vector3 world)
        {
            if (!frame) return TerracePeakMesh.GroundHeight(world);
            Vector3 local = frame.InverseTransformPoint(world);
            float height = TerracePeakMesh.GroundHeight(local);
            return frame.TransformPoint(new Vector3(local.x, height, local.z)).y;
        }

        private static List<Vector3> BuildRoute(out float length)
        {
            var control = new List<Vector3>();
            var grounded = new List<bool>();
            void Add(Vector3 point, bool terrain)
            {
                control.Add(point);
                grounded.Add(terrain);
            }
            void Valley(float angle, float radius) => Add(TerracePeakMesh.ValleyPoint(angle, radius), true);
            void Face(int tier, float angle, float u) => Add(TerracePeakMesh.TierFace(tier, angle, u), false);
            void Ledge(int tier, float angle, float f) => Add(TerracePeakMesh.TierLedge(tier, angle, f), false);
            void ShelfArc(int tier, float from, float to, int steps, int phase)
            {
                for (int step = 1; step <= steps; step++)
                    Ledge(tier, Mathf.Lerp(from, to, step / (float)steps), .5f + .2f * Mathf.Sin(step + phase));
            }
            void Wall(int tier, float from, float to, float u0, float u1, int passes, float stepDegrees)
            {
                float angle = from, height = u0;
                for (int pass = 0; pass < passes; pass++)
                {
                    float target = pass % 2 == 0 ? to : Mathf.Lerp(from, to, .08f);
                    float targetHeight = Mathf.Lerp(u0, u1, (pass + 1f) / passes);
                    int count = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(target - angle) / stepDegrees));
                    for (int step = 1; step <= count; step++)
                    {
                        float t = step / (float)count;
                        Face(tier, Mathf.Lerp(angle, target, t), Mathf.Lerp(height, targetHeight, t));
                    }
                    angle = target;
                    height = targetHeight;
                }
            }
            void Descend(int tier, float from, float to, int passes, float stepDegrees)
            {
                float angle = from, height = .98f;
                for (int pass = 0; pass < passes; pass++)
                {
                    float target = pass % 2 == 0 ? to : Mathf.Lerp(from, to, .1f);
                    float targetHeight = Mathf.Lerp(.98f, .03f, (pass + 1f) / passes);
                    int count = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(target - angle) / stepDegrees));
                    for (int step = 1; step <= count; step++)
                    {
                        float t = step / (float)count;
                        Face(tier, Mathf.Lerp(angle, target, t), Mathf.Lerp(height, targetHeight, t));
                    }
                    angle = target;
                    height = targetHeight;
                }
            }

            Add(TerracePeakMesh.BankPoint(-65f, 95f), true);
            Valley(-60f, 390f); Valley(-52f, 372f); Valley(-44f, 382f); Valley(-36f, 362f);
            Valley(-28f, 372f); Valley(-20f, 360f); Valley(-12f, 370f); Valley(-4f, 362f);
            Valley(4f, 368f); Valley(10f, 356f);

            Face(0, 10f, .03f);
            Wall(0, 10f, -8f, .05f, 1f, 1, 5f);
            ShelfArc(0, -8f, -16f, 2, 0);
            Wall(1, -20f, -34f, .05f, 1f, 2, 5f);
            ShelfArc(1, -34f, -42f, 2, 1);
            Wall(2, -46f, -58f, .05f, 1f, 3, 4f);
            ShelfArc(2, -58f, -66f, 2, 2);
            Wall(3, -70f, -84f, .05f, 1f, 3, 4f);
            ShelfArc(3, -84f, -88f, 2, 1);

            Add(TerracePeakMesh.CaveMouth(-2f, 4f), false);
            var tunnel = TerracePeakMesh.TunnelCenterline();
            for (int index = 0; index < tunnel.Length; index++) Add(tunnel[index], false);
            Vector3 exit = tunnel[tunnel.Length - 1];
            Add(new Vector3(exit.x, exit.y + .8f, exit.z + 24f), false);

            const float exitAngle = TerracePeakMesh.TunnelExitSide * 360f / 128f;
            Wall(4, exitAngle, exitAngle + 12f, .05f, 1f, 1, 4f);
            ShelfArc(4, exitAngle + 12f, exitAngle + 22f, 2, 0);
            Wall(5, exitAngle + 22f, exitAngle + 34f, .05f, 1f, 1, 4f);
            ShelfArc(5, exitAngle + 34f, exitAngle + 44f, 2, 1);
            Wall(6, exitAngle + 44f, exitAngle + 52f, .05f, 1f, 3, 3f);
            ShelfArc(6, exitAngle + 52f, exitAngle + 60f, 1, 2);
            Wall(7, exitAngle + 60f, exitAngle + 68f, .05f, 1f, 1, 3f);
            for (float angle = exitAngle + 68f; angle < exitAngle + 100f; angle += 5f)
                Add(TerracePeakMesh.SummitPoint(angle, 404f), false);
            for (float angle = exitAngle + 100f; angle < exitAngle + 108f; angle += 4f)
                Add(TerracePeakMesh.SummitPoint(angle, 401f), false);

            Descend(7, exitAngle + 108f, exitAngle + 100f, 1, 4f);
            Ledge(6, exitAngle + 108f, .9f); Ledge(6, exitAngle + 103f, .1f);
            Descend(6, exitAngle + 103f, exitAngle + 95f, 2, 4f);
            Ledge(5, exitAngle + 95f, .9f); Ledge(5, exitAngle + 90f, .1f);
            Descend(5, exitAngle + 90f, exitAngle + 82f, 1, 4f);
            Ledge(4, exitAngle + 82f, .9f); Ledge(4, exitAngle + 77f, .1f);
            Descend(4, exitAngle + 77f, exitAngle + 69f, 2, 4f);
            Ledge(3, exitAngle + 69f, .9f); Ledge(3, exitAngle + 64f, .1f);
            Descend(3, exitAngle + 64f, exitAngle + 56f, 1, 4f);
            Ledge(2, exitAngle + 56f, .9f); Ledge(2, exitAngle + 51f, .1f);
            Descend(2, exitAngle + 51f, exitAngle + 43f, 2, 4f);
            Ledge(1, exitAngle + 43f, .9f); Ledge(1, exitAngle + 38f, .1f);
            Descend(1, exitAngle + 38f, exitAngle + 30f, 1, 4f);
            Ledge(0, exitAngle + 30f, .9f); Ledge(0, exitAngle + 25f, .1f);
            Descend(0, exitAngle + 25f, exitAngle + 17f, 1, 4f);

            Valley(exitAngle + 22f, 390f); Valley(exitAngle + 34f, 420f);

            return ResampleRoute(control, grounded, out length);
        }

        private static List<Vector3> ResampleRoute(List<Vector3> control, List<bool> grounded, out float length)
        {
            var route = new List<Vector3>();
            var random = new System.Random(20261019);
            for (int index = 0; index < control.Count - 1; index++)
            {
                Vector3 first = control[index], second = control[index + 1];
                float step = 3.7f + (float)random.NextDouble() * 1.4f;
                int count = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(first, second) / step));
                for (int sample = 0; sample < count; sample++)
                {
                    Vector3 point = Vector3.Lerp(first, second, sample / (float)count);
                    if (grounded[index] && grounded[index + 1]) point.y = TerracePeakMesh.GroundHeight(point);
                    route.Add(point);
                }
            }
            Vector3 tail = control[control.Count - 1];
            if (grounded[grounded.Count - 1]) tail.y = TerracePeakMesh.GroundHeight(tail);
            route.Add(tail);
            length = 0f;
            for (int index = 1; index < route.Count; index++) length += Vector3.Distance(route[index - 1], route[index]);
            return route;
        }

        private static int BuildJumpSpots(Transform parent, List<Vector3> route, Material rock)
        {
            const float RootScale = 5f;
            const float MinStoneDiameter = 2.5f;
            var tops = new List<Vector3>();
            var yaws = new List<float>();
            var sizes = new List<float>();
            var random = new System.Random(7717);
            float walked = 0f, next = 24f;
            int count = 0;
            for (int index = 2; index < route.Count - 3; index++)
            {
                walked += Vector3.Distance(route[index - 1], route[index]);
                if (walked < next) continue;
                if (TerracePeakMesh.TryTunnelFloor(route[index], out _))
                {
                    next = walked + 30f;
                    continue;
                }
                int best = index;
                Vector3 forward = route[best + 1] - route[best - 1];
                forward.y = 0f;
                if (forward.sqrMagnitude < .001f) continue;
                forward.Normalize();
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                float baseY = route[best].y;
                Vector3 outward = new Vector3(route[best].x, 0f, route[best].z);
                outward = outward.sqrMagnitude > .001f ? outward.normalized : forward;
                int low = Mathf.Max(0, best - 3), high = Mathf.Min(route.Count - 1, best + 3);
                Vector3 span = route[high] - route[low];
                float run = new Vector2(span.x, span.z).magnitude;
                float climb = Mathf.Abs(span.y) / Mathf.Max(run, .001f);
                bool onWall = climb > .3f
                    || baseY - TerracePeakMesh.GroundHeight(route[best] + outward * 2.5f) > 4f;
                float size = Mathf.Max(MinStoneDiameter / RootScale * 1.3f, 3.75f + (float)random.NextDouble() * .85f);
                float spacing = size * 1.02f + .2f;
                for (int pad = -1; pad <= 1; pad++)
                {
                    float diameter = pad == 0 ? size : size * .9f;
                    Vector3 point = route[best] + forward * (pad * spacing);
                    float support = onWall ? baseY : TerracePeakMesh.GroundHeight(point);
                    point.y = onWall ? baseY + .18f
                        : Mathf.Clamp(support, baseY - 2f, baseY + 2f) + diameter * .14f;
                    if (onWall) point -= outward * (diameter * .22f);
                    tops.Add(point);
                    yaws.Add(yaw);
                    sizes.Add(diameter);
                    if (diameter < 3.25f) continue;
                    float buddyDiameter = diameter * .58f;
                    Vector3 side = Vector3.Cross(Vector3.up, forward);
                    Vector3 buddy = route[best] + forward * (pad * spacing) + side * (diameter * .58f);
                    float buddySupport = onWall ? baseY : TerracePeakMesh.GroundHeight(buddy);
                    buddy.y = Mathf.Clamp(buddySupport, baseY - 2f, baseY + 2f) + buddyDiameter * .14f;
                    if (onWall) buddy -= outward * (buddyDiameter * .22f);
                    tops.Add(buddy);
                    yaws.Add(yaw);
                    sizes.Add(buddyDiameter);
                }
                count++;
                next = walked + 25f + (float)random.NextDouble() * 15f;
            }
            if (tops.Count > 0)
                Emit(parent, "Прыжковые места", "JumpCourse",
                    TerracePeakMesh.JumpCourse(tops, yaws, sizes, 7717), rock, true);
            return count;
        }

        private static int BuildCheckpoints(Transform parent, List<Vector3> route, float length, Material material)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(length / 400f) + 1);
            string[] labels =
            {
                "старт у реки", "подъём по внешней стене", "траверс первого яруса", "полки второго яруса",
                "подход к гроту", "вход в грот", "выход на балкон", "траверс третьего яруса",
                "гребень", "спуск по осыпи", "финиш в долине"
            };
            var root = new GameObject("Чекпоинты маршрута").transform;
            root.SetParent(parent, false);
            var mesh = Save(TerracePeakMesh.CheckpointStone(), "CheckpointStone.asset");
            var random = new System.Random(4421);
            for (int index = 0; index < count; index++)
            {
                int at = RouteIndex(route, length * index / (count - 1f));
                Vector3 point = route[at];
                Vector3 direction = route[Mathf.Min(route.Count - 1, at + 1)] - route[Mathf.Max(0, at - 1)];
                direction.y = 0f;
                if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
                direction.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                Vector3 position = point + side * 4.6f;
                float height = point.y;
                if (!TerracePeakMesh.TryTunnelFloor(position, out _))
                {
                    float ground = TerracePeakMesh.GroundHeight(position);
                    if (ground > point.y - 3f && ground < point.y + 3f) height = ground;
                }
                var item = new GameObject("Чекпоинт " + (index + 1) + " — " + labels[Mathf.Min(index, labels.Length - 1)]);
                item.transform.SetParent(root, false);
                item.transform.localPosition = new Vector3(position.x, height - 1.1f, position.z);
                item.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                    + 90f + (float)random.NextDouble() * 40f, 0f);
                item.transform.localScale = Vector3.one * (.92f + (float)random.NextDouble() * .24f);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterial = material;
                item.isStatic = true;
            }
            return count;
        }

        private static int RouteIndex(List<Vector3> route, float target)
        {
            float walked = 0f;
            for (int index = 1; index < route.Count; index++)
            {
                walked += Vector3.Distance(route[index - 1], route[index]);
                if (walked >= target) return index;
            }
            return route.Count - 1;
        }

        private static void GrottoLights(Transform parent)
        {
            var path = TerracePeakMesh.TunnelCenterline();
            int lightIndex = 0;
            for (int index = 0; index < path.Length; index++)
            {
                AddGrottoLight(parent, path[index] + Vector3.up * 7f, lightIndex++);
                if (index < path.Length - 1)
                    AddGrottoLight(parent, (path[index] + path[index + 1]) * .5f + Vector3.up * 7f, lightIndex++);
            }
        }

        private static void AddGrottoLight(Transform parent, Vector3 position, int index)
        {
            var item = new GameObject("Свет грота " + (index + 1));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            var light = item.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 320f;
            light.intensity = 7.5f;
            light.color = index % 2 == 0 ? new Color(1f, .82f, .58f) : new Color(.7f, .84f, 1f);
            light.shadows = LightShadows.None;
        }

        private static void Emit(Transform parent, string name, string assetName, Mesh mesh, Material material, bool collider)
        {
            mesh = Save(mesh, assetName + ".asset");
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            item.isStatic = true;
            if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Material RockMaterial(string name, Color tint, Color sunTint, Color mossTint, float tiling,
            float layers, Vector3 sunDir)
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakRock")) { name = name };
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", tint);
            material.SetColor("_SunTint", sunTint);
            material.SetColor("_MossTint", mossTint);
            material.SetFloat("_Tiling", tiling);
            material.SetFloat("_Layers", layers);
            material.SetFloat("_Moss", .85f);
            material.SetFloat("_Haze", 0f);
            material.SetVector("_SunDir", sunDir);
            material.SetColor("_ShadowTint", new Color(.56f, .53f, .49f));
            material.SetColor("_DeepTint", new Color(.44f, .44f, .42f));
            material.SetColor("_HazeColor", new Color(.82f, .83f, .78f));
            material.SetFloat("_ShadowFloor", .52f);
            material.SetFloat("_Stripe", .55f);
            material.SetFloat("_Emissive", 0f);
            return Save(material, name + ".mat");
        }

        private static Material MeadowMaterial(string name, Color grass, Color light, Color earth, Vector3 sunDir)
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakMeadow")) { name = name };
            material.SetColor("_GrassTint", grass);
            material.SetColor("_GrassLight", light);
            material.SetColor("_EarthTint", earth);
            material.SetVector("_SunDir", sunDir);
            material.SetFloat("_Tiling", .0011f);
            material.SetFloat("_Haze", 0f);
            material.SetColor("_HazeColor", new Color(.82f, .83f, .78f));
            return Save(material, name + ".mat");
        }

        private static Material WaterMaterial()
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakWater")) { name = "TurquoiseRiver" };
            material.SetColor("_Shallow", new Color(.2f, .64f, .7f));
            material.SetColor("_Deep", new Color(.06f, .32f, .42f));
            material.SetColor("_Foam", new Color(.72f, .86f, .86f));
            material.SetColor("_HazeColor", new Color(.8f, .8f, .75f));
            material.SetFloat("_Haze", 0f);
            return Save(material, "TurquoiseRiver.mat");
        }

        private static Material FarMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakFar")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetColor("_HazeColor", new Color(.82f, .86f, .93f));
            material.SetFloat("_HazeStart", 2500f);
            material.SetFloat("_HazeEnd", 9000f);
            material.SetFloat("_HazeStrength", .7f);
            return Save(material, name + ".mat");
        }

        private static Material Simple(string name, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", smoothness);
            return Save(material, name + ".mat");
        }

        private static T Save<T>(T asset, string name) where T : Object
        {
            string path = AssetsPath + "/" + name;
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(asset, path); return asset; }
            if (asset is Mesh source && existing is Mesh destination)
            {
                destination.Clear();
                destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices;
                destination.triangles = source.triangles;
                destination.normals = source.normals;
                destination.colors = source.colors;
                destination.uv = source.uv;
                destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }

        private static void ShadowLift()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientSkyColor = new Color(.66f, .7f, .74f);
            RenderSettings.ambientEquatorColor = new Color(.5f, .51f, .5f);
            RenderSettings.ambientGroundColor = new Color(.52f, .5f, .46f);
            var sun = RenderSettings.sun;
            if (sun) sun.shadowStrength = .6f;
        }

        private static void Environment()
        {
            ShadowLift();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.82f, .83f, .78f);
            RenderSettings.fogDensity = .000042f;
            var sunRotation = Quaternion.Euler(42f, 30f, 0f);
            Vector3 sunDir = -(sunRotation * Vector3.forward);
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.3f, .58f, .92f));
            sky.SetColor("_SkyHorizon", new Color(.66f, .8f, .94f));
            sky.SetColor("_GroundColor", new Color(.64f, .76f, .88f));
            sky.SetColor("_SunColor", new Color(1f, .95f, .8f));
            sky.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
            sky.SetFloat("_SunSize", .06f);
            sky.SetFloat("_CloudDensity", .5f);
            RenderSettings.skybox = Save(sky, "TerraceSky.mat");
            var sun = new GameObject("Тёплое солнце").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .9f, .72f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .6f;
            sun.transform.rotation = sunRotation;
            RenderSettings.sun = sun;
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
        }
    }
}
