using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class EmeraldBasinAuthoring
    {
        private const string AssetsPath = "Assets/GoatDescent/Level/Static/EmeraldBasin";
        private const string ScenePath = "Assets/GoatDescent/Scenes/EmeraldBasin.unity";

        private static Transform terraces, lake, falls, flora, peaks, trail;
        private static Material cliff, meadow, shore, talusMaterial, peakMaterial, needles, needlesLight, canopy;
        private static Material trunkMaterial, tuftMaterial, reedMaterial, bloomMaterial, gravelMaterial, dirtMaterial, markerMaterial;
        private static Material waterMaterial, cascadeMaterial, cascadeLowerMaterial, streamMaterial, foamMaterial;
        private static Mesh spruce, trunkMesh, boulderBank, boulderRidge, boulderSmall, shrubMesh, grassBlade, foamMesh;
        private static Mesh[] cascadeMeshes, streamMeshes, talusMeshes, pebbleMeshes, ledgeMeshes;
        private static System.Random random;
        private static List<Vector3> route;
        private static List<bool> routeWet;
        private static float routeLength;

        [MenuItem("Tools/GOAT/Статичные горы/Изумрудная котловина — открыть")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Изумрудной котловины."); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Изумрудная котловина — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AssetsPath); AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            random = new System.Random(48271);
            Materials();
            Meshes();
            var root = new GameObject("Изумрудная котловина — статичная композиция").transform;
            terraces = Group(root, "Кольцевые террасы и скальные стены");
            lake = Group(root, "Озеро, остров и берега");
            falls = Group(root, "Водопады и каскады");
            flora = Group(root, "Луга, рощи и камни");
            peaks = Group(root, "Дальние снежные хребты");
            trail = Group(root, "Козлина тропа — Кольцо водопадов");
            BuildBasin();
            BuildWaterfalls();
            PlanTrail();
            BuildVegetation();
            BuildTrail();
            BuildJumpSpots();
            BuildSkyline();
            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 76f; camera.farClipPlane = 3200f;
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.saturation = 1.2f;
            post.warmTint = new Color(1.03f, 1.01f, .94f);
            post.bloomIntensity = .6f;
            post.bloomThreshold = .78f;
            post.vignette = .28f;
            post.contrast = 1.06f;
            Orbit(camera, 225f);
            Render(camera, "EmeraldBasin-overview");
            Orbit(camera, 0f); Render(camera, "EmeraldBasin-east");
            Orbit(camera, 180f); Render(camera, "EmeraldBasin-west");
            Orbit(camera, 90f); Render(camera, "EmeraldBasin-south");
            camera.fieldOfView = 66f; camera.transform.position = new Vector3(0f, 96f, 230f);
            camera.transform.LookAt(new Vector3(0f, 44f, -150f));
            Render(camera, "EmeraldBasin-lake");
            camera.fieldOfView = 68f; camera.transform.position = new Vector3(150f, 140f, 150f);
            camera.transform.LookAt(new Vector3(-280f, 230f, -280f));
            Render(camera, "EmeraldBasin-falls");
            camera.fieldOfView = 76f; Orbit(camera, 225f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Debug.Log("EMERALD_BASIN_STATIC_SAVED scene=" + ScenePath);
        }

        private static void Orbit(Camera camera, float azimuth)
        {
            float radians = azimuth * Mathf.Deg2Rad;
            camera.transform.position = new Vector3(Mathf.Cos(radians) * 580f, 680f, Mathf.Sin(radians) * 580f);
            camera.transform.LookAt(new Vector3(0f, 505f, 0f));
        }

        private static void Materials()
        {
            cliff = RockMaterial("EmeraldCliff", new Color(1.40f, 1.24f, 1.00f), .40f, .07f, .22f, .42f, .50f, .30f, .55f, .20f);
            meadow = RockMaterial("EmeraldMeadow", new Color(.94f, .98f, .82f), 1f, .05f, .06f, .05f, .30f, .34f, .3f, .2f);
            shore = RockMaterial("EmeraldShore", new Color(1.00f, .94f, .82f), .9f, .06f, .12f, .10f, .26f, .30f, .35f, .42f,
                new Color(.87f, .82f, .73f));
            talusMaterial = RockMaterial("EmeraldTalus", new Color(1.02f, .98f, .92f), .05f, .10f, .06f, .30f, .70f, .20f, .5f, .4f);
            gravelMaterial = RockMaterial("EmeraldGravel", new Color(1.06f, 1.00f, .92f), .04f, .16f, .04f, .15f, .75f, .20f, .7f, .25f,
                new Color(.88f, .83f, .74f));
            peakMaterial = PeakMaterial();
            needles = StandardMaterial("SpruceNeedles", new Color(.055f, .18f, .09f), .04f);
            needlesLight = StandardMaterial("SpruceNeedlesLight", new Color(.10f, .26f, .12f), .05f);
            canopy = StandardMaterial("ForestCanopy", new Color(.075f, .21f, .11f), .03f);
            trunkMaterial = StandardMaterial("SpruceTrunk", new Color(.19f, .15f, .11f), .03f);
            tuftMaterial = StandardMaterial("MeadowShrub", new Color(.34f, .38f, .24f), .04f);
            reedMaterial = StandardMaterial("LakeReeds", new Color(.13f, .19f, .11f), .05f);
            bloomMaterial = StandardMaterial("AlpineBloom", new Color(.60f, .58f, .44f), .06f);
            dirtMaterial = StandardMaterial("TrailDirt", new Color(.45f, .35f, .22f), .04f);
            markerMaterial = StandardMaterial("TrailMarker", new Color(.76f, .74f, .70f), .12f);
            var water = new Material(Shader.Find("GoatDescent/EmeraldWater"));
            water.SetColor("_ShallowColor", new Color(.05f, .28f, .22f));
            water.SetColor("_DeepColor", new Color(.008f, .075f, .085f));
            water.SetColor("_FoamColor", new Color(.62f, .78f, .80f));
            water.SetVector("_Center", Vector4.zero);
            water.SetFloat("_Radius", EmeraldBasinMesh.LakeRadius);
            water.SetFloat("_Smoothness", .55f);
            water.SetFloat("_WaveScale", .012f);
            waterMaterial = Save(water, "LakeWater.mat");
            var cascade = new Material(Shader.Find("GoatDescent/EmeraldCascade"));
            cascade.SetColor("_Color", new Color(.88f, .94f, .96f));
            cascade.SetColor("_FoamColor", new Color(.92f, .97f, .98f));
            cascade.SetFloat("_Opacity", .82f);
            cascade.SetFloat("_Flow", 1.7f);
            cascadeMaterial = Save(cascade, "Cascade.mat");
            var lower = new Material(Shader.Find("GoatDescent/EmeraldCascade"));
            lower.SetColor("_Color", new Color(.88f, .94f, .98f));
            lower.SetColor("_FoamColor", new Color(.92f, .96f, 1f));
            lower.SetFloat("_Opacity", .62f);
            lower.SetFloat("_Flow", 1.4f);
            cascadeLowerMaterial = Save(lower, "CascadeLower.mat");
            var stream = new Material(Shader.Find("GoatDescent/EmeraldCascade"));
            stream.SetColor("_Color", new Color(.78f, .99f, .98f));
            stream.SetColor("_FoamColor", Color.white);
            stream.SetFloat("_Opacity", .9f);
            stream.SetFloat("_Flow", 1f);
            streamMaterial = Save(stream, "Stream.mat");
            var foam = new Material(Shader.Find("GoatDescent/EmeraldCascade"));
            foam.SetColor("_Color", new Color(.90f, .98f, 1f));
            foam.SetColor("_FoamColor", new Color(1f, 1f, 1f));
            foam.SetFloat("_Opacity", .7f);
            foam.SetFloat("_Flow", 2.4f);
            foamMaterial = Save(foam, "Foam.mat");
        }

        private static void Meshes()
        {
            spruce = Save(EmeraldBasinMesh.Spruce(), "Spruce.asset");
            trunkMesh = Save(Object.Instantiate(DecorMeshLibrary.TrunkMesh()), "SpruceTrunkMesh.asset");
            boulderBank = Save(EmeraldBasinMesh.Boulder(3), "BoulderBank.asset");
            boulderRidge = Save(EmeraldBasinMesh.Boulder(17), "BoulderRidge.asset");
            boulderSmall = Save(EmeraldBasinMesh.Boulder(29), "BoulderSmall.asset");
            shrubMesh = Save(Object.Instantiate(DecorMeshLibrary.OctahedronMesh()), "Shrub.asset");
            grassBlade = Save(Object.Instantiate(DecorMeshLibrary.GrassBladeMesh()), "GrassTuft.asset");
            foamMesh = Save(EmeraldBasinMesh.Foam(), "Foam.asset");
            cascadeMeshes = new Mesh[3];
            streamMeshes = new Mesh[3];
            for (int tier = 1; tier <= 3; tier++)
            {
                cascadeMeshes[tier - 1] = Save(EmeraldBasinMesh.Cascade(tier), "Cascade-" + tier + ".asset");
                streamMeshes[tier - 1] = Save(EmeraldBasinMesh.Stream(tier), "Stream-" + tier + ".asset");
            }
            talusMeshes = new Mesh[3];
            pebbleMeshes = new Mesh[3];
            ledgeMeshes = new Mesh[3];
            for (int variant = 0; variant < 3; variant++)
            {
                talusMeshes[variant] = Save(EmeraldBasinMesh.Talus(11 + variant * 7), "Talus-" + variant + ".asset");
                pebbleMeshes[variant] = Save(EmeraldBasinMesh.Pebbles(41 + variant * 13), "Pebbles-" + variant + ".asset");
                ledgeMeshes[variant] = Save(EmeraldBasinMesh.Ledge(61 + variant * 9), "Ledge-" + variant + ".asset");
            }
        }

        private static void BuildBasin()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                Place(terraces, "Скальная стена — ярус " + tier, Save(EmeraldBasinMesh.TerraceWall(tier), "Terrace-" + tier + ".asset"),
                    cliff, Vector3.zero, Vector3.one, true);
                Place(terraces, "Травяная полка — ярус " + tier, Save(EmeraldBasinMesh.TerraceShelf(tier), "Shelf-" + tier + ".asset"),
                    meadow, Vector3.zero, Vector3.one, true);
            }
            Place(terraces, "Внешние склоны над обрывом", Save(EmeraldBasinMesh.OuterSlopes(), "OuterSlopes.asset"),
                meadow, Vector3.zero, Vector3.one);
            Place(terraces, "Дно котловины и луга у озера", Save(EmeraldBasinMesh.BasinFloor(), "BasinFloor.asset"),
                shore, Vector3.zero, Vector3.one, true);
            Place(lake, "Остров на озере", Save(EmeraldBasinMesh.Islands(), "Islands.asset"), shore, Vector3.zero, Vector3.one, true);
            Place(lake, "Изумрудное озеро", Save(EmeraldBasinMesh.Water(), "Lake.asset"), waterMaterial, Vector3.zero, Vector3.one);
        }

        private static void BuildWaterfalls()
        {
            for (int fall = 0; fall < EmeraldBasinMesh.FallAngles.Length; fall++)
            {
                float angle = EmeraldBasinMesh.FallAngles[fall];
                float width = EmeraldBasinMesh.FallWidths[fall];
                float drift = fall % 2 == 0 ? .016f : -.016f;
                int bottom = EmeraldBasinMesh.FallBottoms[fall];
                int top = EmeraldBasinMesh.FallTops[fall];
                for (int tier = bottom; tier <= top; tier++)
                {
                    float tierAngle = angle + drift * (tier - bottom);
                    float wallRadius = EmeraldBasinMesh.TierInner(tier, tierAngle) - EmeraldBasinMesh.GullyDepth(tier, tierAngle) - 2f;
                    var drop = Place(falls, "Водопад — ярус " + tier + ", каскад " + (fall + 1), cascadeMeshes[tier - 1],
                        tier == top ? cascadeMaterial : cascadeLowerMaterial,
                        new Vector3(Mathf.Cos(tierAngle) * wallRadius, EmeraldBasinMesh.ShelfLevel[tier - 1] + .3f, Mathf.Sin(tierAngle) * wallRadius),
                        new Vector3(width, 1f, 1f));
                    drop.localRotation = Quaternion.Euler(0f, 90f - tierAngle * Mathf.Rad2Deg, 0f);
                    if (tier == bottom)
                    {
                        float from = EmeraldBasinMesh.TierInner(tier, tierAngle) - EmeraldBasinMesh.GullyRecess - 8f;
                        float streamY = tier == 1
                            ? EmeraldBasinMesh.FloorHeight(tierAngle, from) + 1.2f
                            : EmeraldBasinMesh.ShelfHeight(tier - 1, tierAngle, from) + 1.1f;
                        var flow = Place(falls, "Ручей по полке, каскад " + (fall + 1), streamMeshes[tier - 1], streamMaterial,
                            new Vector3(Mathf.Cos(tierAngle) * from, streamY, Mathf.Sin(tierAngle) * from), Vector3.one);
                        flow.localRotation = Quaternion.Euler(0f, 180f - tierAngle * Mathf.Rad2Deg, 0f);
                    }
                    float foamRadius = EmeraldBasinMesh.TierInner(tier, tierAngle) - EmeraldBasinMesh.GullyRecess - 14f;
                    float foamY = tier == 1
                        ? EmeraldBasinMesh.FloorHeight(tierAngle, foamRadius) + .5f
                        : EmeraldBasinMesh.ShelfHeight(tier - 1, tierAngle, foamRadius) + .6f;
                    if (random.NextDouble() < .12) continue;
                    var spray = Place(falls, "Пена у подножия, каскад " + (fall + 1), foamMesh, foamMaterial,
                        new Vector3(Mathf.Cos(tierAngle) * foamRadius, foamY, Mathf.Sin(tierAngle) * foamRadius),
                        new Vector3(width * .62f, 1.4f, width * .5f));
                    spray.localRotation = Quaternion.Euler(0f, fall * 41f + tier * 23f, 0f);
                }
            }
        }

        private static void PlanTrail()
        {
            route = new List<Vector3>();
            routeWet = new List<bool>();

            AddRoute(4.35f, 880f, false);
            AddRoute(4.75f, 800f, false);
            AddRoute(5.15f, 730f, false);
            AddRoute(5.55f, 668f, false);
            AddRoute(5.95f, 615f, false);
            AddRoute(6.25f, 570f, false);
            AddRoute(6.45f, 538f, false);
            AddRoute(6.28f, 516f, false);
            AddRoute(6.10f, 498f, false);
            AddRoute(6.30f, 490f, false);
            AddRoute(6.38f, 482f, false);
            AddRoute(6.05f, 470f, false);
            AddRoute(6.38f, 460f, false);
            AddRoute(6.10f, 452f, false);
            AddRoute(6.40f, 448f, true);
            AddRoute(6.43f, 438f, true);
            AddRoute(0.90f, 444f, false);
            AddRoute(1.35f, 452f, false);
            AddRoute(1.80f, 456f, false);
            AddRoute(2.10f, 448f, false);
            AddRoute(1.80f, 436f, false);
            AddRoute(2.13f, 424f, false);
            AddRoute(1.87f, 412f, false);
            AddRoute(1.83f, 402f, false);
            AddRoute(1.82f, 400f, true);
            AddRoute(1.87f, 390f, true);
            AddRoute(2.40f, 398f, false);
            AddRoute(3.00f, 404f, false);
            AddRoute(3.60f, 408f, false);
            AddRoute(4.10f, 404f, false);
            AddRoute(4.40f, 400f, false);
            AddRoute(4.60f, 402f, false);
            AddRoute(4.60f, 392f, false);
            AddRoute(4.30f, 380f, false);
            AddRoute(4.60f, 368f, false);
            AddRoute(4.35f, 358f, false);
            AddRoute(4.55f, 348f, false);
            AddRoute(4.58f, 342f, true);
            AddRoute(4.62f, 330f, true);
            for (int i = 0; i < 4; i++)
            {
                float shoreAngle = 5.75f + i * .38f;
                AddRoute(shoreAngle, EmeraldBasinMesh.LakeEdge(shoreAngle) + 9f + i % 3 * 4f, false);
            }
            AddRoute(5.62f, EmeraldBasinMesh.LakeEdge(5.62f) + 3f, true);
            AddRoute(5.85f, 118f, true);
            AddRoute(6.05f, 82f, true);
            AddRoute(6.25f, 48f, true);
            AddRoute(6.45f, 26f, true);
            AddRoute(6.58f, 17f, false);
            AddRoute(6.70f, 10f, false);
            routeLength = 0f;
            for (int i = 1; i < route.Count; i++)
            {
                var a = route[i - 1]; a.y = TrailSurface(a);
                var b = route[i]; b.y = TrailSurface(b);
                routeLength += Vector3.Distance(a, b);
            }
        }

        private static void AddRoute(float angle, float radius, bool wet)
        {
            route.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            routeWet.Add(wet);
        }

        private static float WallRamp(int tier, float angle, float radius)
        {
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 0f);
            float outer = EmeraldBasinMesh.WallRadius(tier, angle, 1f);
            float v = Mathf.Clamp01((radius - inner) / Mathf.Max(.001f, outer - inner));
            return EmeraldBasinMesh.WallHeight(tier, angle, v);
        }

        private static float TrailSurface(Vector3 position)
        {
            float radius = new Vector2(position.x, position.z).magnitude;
            float angle = Mathf.Atan2(position.z, position.x);
            if (radius > EmeraldBasinMesh.ShelfOuter(3, angle)) return EmeraldBasinMesh.SlopeHeight(angle, radius);
            for (int tier = 3; tier >= 1; tier--)
            {
                if (radius > EmeraldBasinMesh.WallRadius(tier, angle, 1f)) return EmeraldBasinMesh.ShelfHeight(tier, angle, radius);
                if (radius > EmeraldBasinMesh.WallRadius(tier, angle, 0f))
                {
                    float wall = WallRamp(tier, angle, radius);
                    float lower = tier == 1
                        ? (radius > EmeraldBasinMesh.LakeEdge(angle) ? EmeraldBasinMesh.FloorHeight(angle, radius) : EmeraldBasinMesh.LakeLevel)
                        : EmeraldBasinMesh.ShelfHeight(tier - 1, angle, radius);
                    return Mathf.Max(wall, lower);
                }
            }
            if (radius > EmeraldBasinMesh.LakeEdge(angle)) return EmeraldBasinMesh.FloorHeight(angle, radius);
            return Mathf.Max(EmeraldBasinMesh.IslandHeight(angle, radius), EmeraldBasinMesh.LakeLevel);
        }

        private static float TrailDistance(Vector3 position)
        {
            if (route == null || route.Count < 2) return 999f;
            float best = 999f;
            var point = new Vector2(position.x, position.z);
            for (int i = 0; i < route.Count - 1; i++)
            {
                var a = new Vector2(route[i].x, route[i].z);
                var ab = new Vector2(route[i + 1].x - route[i].x, route[i + 1].z - route[i].z);
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(.001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(point, a + ab * t));
            }
            return best;
        }

        private static void BuildTrail()
        {
            var sampler = new System.Func<Vector3, float>(TrailSurface);
            int start = 0;
            for (int i = 1; i <= route.Count; i++)
            {
                bool boundary = i == route.Count || routeWet[i] != routeWet[start];
                if (!boundary) continue;
                var run = route.GetRange(start, i - start);
                if (run.Count >= 2)
                {
                    bool crossing = routeWet[start];
                    GoatTrailBuilder.Build(trail, crossing ? "Переправа по камням" : "Тропа по кромке и полкам", sampler, dirtMaterial, run,
                        crossing ? 3.2f : 4f, crossing ? .16f : .35f, crossing ? 2.2f : 9f, !crossing,
                        crossing ? new Color(.64f, .63f, .60f) : new Color(.56f, .55f, .52f));
                }
                start = Mathf.Max(0, i - 1);
            }
            int[] markerPoints = { 0, 9, 21, 33, 42, route.Count - 1 };
            for (int marker = 0; marker < markerPoints.Length; marker++)
            {
                var point = route[Mathf.Clamp(markerPoints[marker], 0, route.Count - 1)];
                float y = TrailSurface(point);
                var item = Place(trail, "Камень-чекпоинт " + (marker + 1), boulderRidge, markerMaterial,
                    new Vector3(point.x, y - 1.2f, point.z), Vector3.one * Range(8f, 13f));
                item.localRotation = Quaternion.Euler(Range(-4f, 4f), marker * 71f, Range(-4f, 4f));
            }
            Debug.Log("GOAT_TRAIL EmeraldBasin waypoints=" + route.Count + " length~" + Mathf.RoundToInt(routeLength) + "м");
        }

        private static void BuildJumpSpots()
        {
            int[] climbs = { 7, 9, 12, 18, 21, 27, 30, 35, 42 };
            foreach (int index in climbs)
                JumpCluster(index, 0, false);
            int run = -1;
            for (int i = 0; i < route.Count; i++)
            {
                if (!routeWet[i]) { run = -1; continue; }
                if (run < 0) { run = i; continue; }
                if (i + 1 >= route.Count || !routeWet[i + 1])
                {
                    JumpCluster(run, i - run + 1, true);
                    run = -1;
                }
            }
        }

        private static void JumpCluster(int index, int span, bool crossing)
        {
            var point = route[index];
            float angle = Mathf.Atan2(point.z, point.x);
            float radius = new Vector2(point.x, point.z).magnitude;
            float yaw = 90f - angle * Mathf.Rad2Deg;
            int steps = Mathf.Max(3, span + (crossing ? 3 : 2));
            for (int step = 0; step < steps; step++)
            {
                float t = steps > 1 ? step / (steps - 1f) : .5f;
                float along = (t - .5f) * (crossing ? 26f : 18f) + Range(-2.5f, 2.5f);
                float side = random.NextDouble() > .5f ? 1f : -1f;
                float outward = Mathf.Sin(t * Mathf.PI) * Range(2f, 7f) * side + (crossing ? Mathf.Sin(t * Mathf.PI) * 5f : 0f);
                float a = angle + along / Mathf.Max(20f, radius);
                float r = radius + outward;
                var probe = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float y = TrailSurface(probe) - (crossing ? Range(1.1f, 1.7f) : Range(.4f, .9f));
                bool ledge = random.NextDouble() > .35f;
                float size = ledge ? Range(3.6f, 7.5f) : Range(2.6f, 5.5f);
                var mesh = ledge
                    ? ledgeMeshes[random.Next(0, ledgeMeshes.Length)]
                    : (random.NextDouble() > .5f ? boulderBank : boulderSmall);
                var item = Place(trail, crossing ? "Валун-переправа" : "Природная ступень-полка", mesh,
                    random.NextDouble() > .22f ? cliff : talusMaterial,
                    new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r),
                    new Vector3(size, size * Range(.55f, .8f), size * Range(.75f, 1.05f)));
                item.localRotation = Quaternion.Euler(Range(-6f, 6f), yaw + Range(-30f, 30f), Range(-5f, 5f));
            }
            var mark = Place(trail, "Ориентир прыжка", boulderSmall, markerMaterial,
                new Vector3(Mathf.Cos(angle) * (radius + Range(-8f, 8f)), TrailSurface(point) + .4f, Mathf.Sin(angle) * (radius + Range(-8f, 8f))),
                Vector3.one * Range(1.4f, 2.4f));
            mark.localRotation = Quaternion.Euler(Range(-8f, 8f), Range(0f, 360f), Range(-8f, 8f));
        }

        private static void BuildVegetation()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                int level = tier;
                Rove(27, 10, 20, a => new Vector2(EmeraldBasinMesh.TierInner(level, a) + 10f, EmeraldBasinMesh.TierOuter(level, a) - 12f),
                    26f, 11f, 26f, (a, r) => EmeraldBasinMesh.ShelfHeight(level, a, r));
                for (int boulder = 0; boulder < 14; boulder++)
                    ScatterBoulder(tier);
                for (int shrub = 0; shrub < 10; shrub++)
                    ScatterShrub(tier);
                for (int bloom = 0; bloom < 6; bloom++)
                    ScatterBloom(tier);
                TalusFans(tier);
            }
            Rove(14, 10, 20, a => new Vector2(EmeraldBasinMesh.LakeEdge(a) + 10f, Mathf.Min(EmeraldBasinMesh.LakeEdge(a) + 46f, EmeraldBasinMesh.TierInner(1, a) - 24f)), 14f, 10f, 24f, EmeraldBasinMesh.FloorHeight);
            Rove(22, 9, 15, a => new Vector2(560f, 900f), 32f, 12f, 34f, EmeraldBasinMesh.SlopeHeight);
            IslandGroves();
            ForestMasses();
            SandPaths();
            MegaRocks();
            GiantSpruces();
            CollapseRubble();
            PebbleBands();
            ReedBeds();
            for (int i = 0; i < 30; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float edge = EmeraldBasinMesh.LakeEdge(angle);
                float radius = edge + Range(2f, 12f);
                float y = EmeraldBasinMesh.FloorHeight(angle, radius) + .2f;
                Place(flora, "Прибрежный валун", random.NextDouble() > .5f ? boulderBank : boulderSmall, cliff,
                    new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius), Vector3.one * Range(2.4f, 6f));
            }
            for (int i = 0; i < 260; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float edge = EmeraldBasinMesh.LakeEdge(angle);
                float radius = Mathf.Min(edge + Range(6f, 34f), EmeraldBasinMesh.TierInner(1, angle) - 26f);
                var tuft = Place(flora, "Луговая трава", grassBlade, tuftMaterial,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.FloorHeight(angle, radius) + .1f, Mathf.Sin(angle) * radius),
                    new Vector3(Range(1.8f, 3.4f), Range(2.2f, 4f), Range(1.8f, 3.4f)));
                tuft.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            }
            for (int i = 0; i < 10; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(2f, 22f);
                Place(flora, "Камень на острове", boulderSmall, cliff,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.IslandHeight(angle, radius) + .1f, Mathf.Sin(angle) * radius),
                    Vector3.one * Range(1.4f, 3.4f));
            }
            for (int i = 0; i < 26; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(540f, 840f);
                Place(flora, "Валун на внешнем склоне", random.NextDouble() > .5f ? boulderRidge : boulderSmall, cliff,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.SlopeHeight(angle, radius), Mathf.Sin(angle) * radius),
                    Vector3.one * Range(3f, 8f));
            }
        }

        private static void TalusFans(int tier)
        {
            for (int fan = 0; fan < 6; fan++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                if (NearFall(angle, .22f)) continue;
                PlaceTalus(tier, angle, Range(1f, 1.6f));
            }
            for (int fall = 0; fall < EmeraldBasinMesh.FallAngles.Length; fall++)
            {
                PlaceTalus(tier, EmeraldBasinMesh.FallAngles[fall] + .3f, .8f);
                PlaceTalus(tier, EmeraldBasinMesh.FallAngles[fall] - .3f, .8f);
            }
        }

        private static void PlaceTalus(int tier, float angle, float scale)
        {
            float radius = EmeraldBasinMesh.WallRadius(tier, angle, 0f) - 1.5f;
            float foot = tier == 1
                ? EmeraldBasinMesh.FloorHeight(angle, radius)
                : EmeraldBasinMesh.ShelfHeight(tier - 1, angle, radius);
            float height = Range(9f, 17f) * scale;
            var item = Place(flora, "Осыпной конус у стены", talusMeshes[random.Next(0, talusMeshes.Length)], talusMaterial,
                new Vector3(Mathf.Cos(angle) * radius, foot - height * .12f, Mathf.Sin(angle) * radius),
                new Vector3(Range(24f, 38f) * scale, height, Range(15f, 26f) * scale));
            item.localRotation = Quaternion.Euler(0f, -90f - angle * Mathf.Rad2Deg, 0f);
        }

        private static void PebbleBands()
        {
            for (int band = 0; band < 74; band++)
            {
                if (random.NextDouble() < .12) continue;
                float angle = Range(0f, Mathf.PI * 2f);
                float edge = EmeraldBasinMesh.LakeEdge(angle);
                float radius = edge + Range(-1f, 9f);
                float y = Mathf.Max(EmeraldBasinMesh.FloorHeight(angle, radius), EmeraldBasinMesh.LakeLevel - .3f);
                var item = Place(lake, "Галечный пляж", pebbleMeshes[random.Next(0, pebbleMeshes.Length)], gravelMaterial,
                    new Vector3(Mathf.Cos(angle) * radius, y + .12f, Mathf.Sin(angle) * radius),
                    new Vector3(Range(9f, 22f) * Range(.7f, 1.3f), Range(.7f, 1.3f), Range(9f, 22f) * Range(.7f, 1.3f)));
                item.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            }
            for (int band = 0; band < 10; band++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(4f, 30f);
                Place(lake, "Галька на острове", pebbleMeshes[random.Next(0, pebbleMeshes.Length)], gravelMaterial,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.IslandHeight(angle, radius) + .12f, Mathf.Sin(angle) * radius),
                    new Vector3(Range(6f, 14f), Range(.7f, 1.2f), Range(6f, 14f)));
            }
        }

        private static void ReedBeds()
        {
            for (int clump = 0; clump < 38; clump++)
            {
                if (random.NextDouble() < .14) continue;
                float density = Range(.5f, 1.6f);
                float angle = Range(0f, Mathf.PI * 2f);
                float edge = EmeraldBasinMesh.LakeEdge(angle);
                float centre = edge + Range(-2f, 5f);
                if (TrailDistance(new Vector3(Mathf.Cos(angle) * centre, 0f, Mathf.Sin(angle) * centre)) < 7f) continue;
                int blades = random.Next(10, 34) + Mathf.RoundToInt(10f * density);
                for (int blade = 0; blade < blades; blade++)
                {
                    float a = angle + Range(-.035f, .035f);
                    float r = centre + Range(-6f, 6f);
                    float y = Mathf.Max(EmeraldBasinMesh.FloorHeight(a, r), EmeraldBasinMesh.LakeLevel - .4f) + .05f;
                    var reed = Place(flora, "Камыш у воды", grassBlade, reedMaterial,
                        new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r),
                        new Vector3(Range(.5f, .9f), Range(2.6f, 5.2f), Range(.5f, .9f)));
                    reed.localRotation = Quaternion.Euler(Range(-7f, 7f), Range(0f, 360f), Range(-7f, 7f));
                }
            }
        }

        private static void ScatterBloom(int tier)
        {
            if (random.NextDouble() < .12) return;
            float angle = Range(0f, Mathf.PI * 2f);
            if (NearFall(angle, .07f)) return;
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 1f) + 10f;
            float outer = EmeraldBasinMesh.ShelfOuter(tier, angle) - 9f;
            float radius = Range(inner, Mathf.Max(inner + 1f, outer));
            if (TrailDistance(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius)) < 6f) return;
            int bushes = random.Next(3, 7);
            for (int bush = 0; bush < bushes; bush++)
            {
                float a = angle + Range(-.03f, .03f);
                float r = radius + Range(-7f, 7f);
                var item = Place(flora, "Пятно цветения", shrubMesh, bloomMaterial,
                    new Vector3(Mathf.Cos(a) * r, EmeraldBasinMesh.ShelfHeight(tier, a, r) + .9f, Mathf.Sin(a) * r),
                    new Vector3(Range(2.4f, 5f), Range(1.4f, 2.6f), Range(2.4f, 5f)));
                item.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            }
        }

        private static void ForestMasses()
        {
            for (int patch = 0; patch < 92; patch++)
            {
                float centre = Range(0f, Mathf.PI * 2f);
                float radius = Range(560f, 920f);
                if (Mask(centre, radius, .004f, 7f) < .30f) continue;
                if (TrailDistance(new Vector3(Mathf.Cos(centre) * radius, 0f, Mathf.Sin(centre) * radius)) < 34f) continue;
                var mesh = Save(EmeraldBasinMesh.ForestPatch(centre, radius, Range(24f, 44f), random.Next(34, 62), 14f, 40f, 2.2f, 900 + patch,
                    EmeraldBasinMesh.SlopeHeight), "Forest-" + patch.ToString("D2") + ".asset");
                Place(flora, "Лесной массив на внешнем склоне", mesh, canopy, Vector3.zero, Vector3.one);
            }
            for (int patch = 0; patch < 14; patch++)
            {
                float centre = Range(0f, Mathf.PI * 2f);
                float radius = Range(496f, 526f);
                if (Mask(centre, radius, .006f, 23f) < .4f) continue;
                if (TrailDistance(new Vector3(Mathf.Cos(centre) * radius, 0f, Mathf.Sin(centre) * radius)) < 26f) continue;
                var mesh = Save(EmeraldBasinMesh.ForestPatch(centre, radius, 12f, random.Next(22, 38), 14f, 30f, 2f, 1200 + patch,
                    (a, r) => EmeraldBasinMesh.ShelfHeight(3, a, r)), "RimForest-" + patch.ToString("D2") + ".asset");
                Place(flora, "Лесной массив на кромке", mesh, canopy, Vector3.zero, Vector3.one);
            }
            for (int patch = 0; patch < 10; patch++)
            {
                float centre = Range(0f, Mathf.PI * 2f);
                float radius = Mathf.Min(EmeraldBasinMesh.LakeEdge(centre) + Range(18f, 34f), EmeraldBasinMesh.TierInner(1, centre) - 26f);
                if (Mask(centre, radius, .006f, 71f) < .42f) continue;
                if (TrailDistance(new Vector3(Mathf.Cos(centre) * radius, 0f, Mathf.Sin(centre) * radius)) < 24f) continue;
                var mesh = Save(EmeraldBasinMesh.ForestPatch(centre, radius, 15f, random.Next(18, 32), 12f, 28f, 2f, 1500 + patch,
                    (a, r) => EmeraldBasinMesh.FloorHeight(a, r)), "ShoreForest-" + patch.ToString("D2") + ".asset");
                Place(flora, "Лес, спускающийся к воде", mesh, canopy, Vector3.zero, Vector3.one);
            }
        }

        private static void CollapseRubble()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                var breaks = EmeraldBasinMesh.CollapseAngles(tier);
                for (int index = 0; index < breaks.Length; index++)
                {
                    float angle = breaks[index];
                    int blocks = random.Next(9, 16);
                    for (int block = 0; block < blocks; block++)
                    {
                        float a = angle + Range(-.15f, .15f);
                        float radius = EmeraldBasinMesh.WallRadius(tier, a, 0f) - Range(-8f, 14f);
                        float lower = EmeraldBasinMesh.WallRadius(tier, a, 0f);
                        float upper = EmeraldBasinMesh.WallRadius(tier, a, 1f);
                        float surface = EmeraldBasinMesh.WallHeight(tier, a, Mathf.Clamp01((radius - lower) / Mathf.Max(.001f, upper - lower)));
                        float size = Range(6f, 17f);
                        var item = Place(flora, "Нагромождение глыб в провале", random.NextDouble() > .5f ? boulderRidge : boulderBank, talusMaterial,
                            new Vector3(Mathf.Cos(a) * radius, surface - size * .16f, Mathf.Sin(a) * radius),
                            new Vector3(size, size * Range(.72f, 1.05f), size * Range(.82f, 1.05f)));
                        item.localRotation = Quaternion.Euler(Range(-8f, 8f), Range(0f, 360f), Range(-8f, 8f));
                    }
                }
            }
        }

        private static void IslandGroves()
        {
            for (int island = 0; island < EmeraldBasinMesh.IslandAngles.Length; island++)
            {
                float centreAngle = EmeraldBasinMesh.IslandAngles[island];
                float offset = EmeraldBasinMesh.IslandOffsets[island];
                float size = EmeraldBasinMesh.IslandSizes[island];
                float centreX = Mathf.Cos(centreAngle) * offset;
                float centreZ = Mathf.Sin(centreAngle) * offset;
                int clusters = island == 0 ? 8 : 4;
                for (int cluster = 0; cluster < clusters; cluster++)
                {
                    float clusterAngle = Range(0f, Mathf.PI * 2f);
                    float clusterRadius = Mathf.Sqrt((float)random.NextDouble()) * size * .38f;
                    float cx = centreX + Mathf.Cos(clusterAngle) * clusterRadius;
                    float cz = centreZ + Mathf.Sin(clusterAngle) * clusterRadius;
                    int trees = random.Next(7, 15);
                    for (int tree = 0; tree < trees; tree++)
                    {
                        float x = cx + Range(-8f, 8f);
                        float z = cz + Range(-8f, 8f);
                        float y = EmeraldBasinMesh.IslandHeight(Mathf.Atan2(z, x), new Vector2(x, z).magnitude) + .3f;
                        if (y < 1.2f) continue;
                        PlantSpruce(new Vector3(x, y, z), RangePower(14f, 24f, 1.5f));
                    }
                }
                for (int boulder = 0; boulder < 3; boulder++)
                {
                    float a = Range(0f, Mathf.PI * 2f);
                    float d = Mathf.Sqrt((float)random.NextDouble()) * size * .34f;
                    float x = centreX + Mathf.Cos(a) * d;
                    float z = centreZ + Mathf.Sin(a) * d;
                    float y = EmeraldBasinMesh.IslandHeight(Mathf.Atan2(z, x), new Vector2(x, z).magnitude);
                    if (y < .6f) continue;
                    Place(flora, "Камень на острове", boulderSmall, cliff, new Vector3(x, y, z), Vector3.one * Range(1.2f, 3f));
                }
            }
        }

        private static void SandPaths()
        {
            var sandMaterial = StandardMaterial("TrailSand", new Color(.82f, .70f, .51f), .05f);
            float[] paths = { 3.10f, 4.02f, 5.15f };
            for (int path = 0; path < paths.Length; path++)
            {
                float angle = paths[path];
                var points = new List<Vector3>();
                for (int step = 0; step < 6; step++)
                {
                    float t = step / 5f;
                    float a = angle + t * .24f;
                    float radius = Mathf.Lerp(660f, 524f, t) - Mathf.Sin(t * Mathf.PI) * 14f;
                    points.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }
                GoatTrailBuilder.Build(trail, "Песчаная тропинка " + (path + 1), new System.Func<Vector3, float>(TrailSurface),
                    sandMaterial, points, 3.4f, .2f, 0f, true);
            }
        }

        private static void MegaRocks()
        {
            for (int mega = 0; mega < 2; mega++)
            {
                int tier = random.Next(1, 4);
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(EmeraldBasinMesh.TierInner(tier, angle) + 34f, EmeraldBasinMesh.TierOuter(tier, angle) - 30f);
                float size = Range(20f, 30f);
                var block = Place(flora, "Мега-глыба на полке", boulderRidge, talusMaterial,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) - size * .16f, Mathf.Sin(angle) * radius),
                    new Vector3(size, size * Range(.72f, .95f), size * Range(.82f, 1.05f)));
                block.localRotation = Quaternion.Euler(Range(-6f, 6f), Range(0f, 360f), Range(-6f, 6f));
            }
            float shoreAngle = Range(0f, Mathf.PI * 2f);
            float shoreRadius = EmeraldBasinMesh.LakeEdge(shoreAngle) + Range(8f, 20f);
            float shoreSize = Range(22f, 28f);
            var shoreBlock = Place(flora, "Мега-глыба на берегу", boulderBank, talusMaterial,
                new Vector3(Mathf.Cos(shoreAngle) * shoreRadius, EmeraldBasinMesh.FloorHeight(shoreAngle, shoreRadius) - shoreSize * .16f, Mathf.Sin(shoreAngle) * shoreRadius),
                new Vector3(shoreSize, shoreSize * .85f, shoreSize));
            shoreBlock.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        }

        private static void GiantSpruces()
        {
            for (int giant = 0; giant < 5; giant++)
            {
                float angle = 3.92f + giant * .36f;
                float radius = Range(508f, 526f);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (TrailDistance(position) < 9f) continue;
                PlantSpruce(new Vector3(position.x, EmeraldBasinMesh.SlopeHeight(angle, radius) + .3f, position.z), Range(30f, 40f));
            }
            for (int giant = 0; giant < 9; giant++)
            {
                int tier = 1 + giant % 3;
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(EmeraldBasinMesh.TierInner(tier, angle) + 30f, EmeraldBasinMesh.TierOuter(tier, angle) - 26f);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (NearFall(angle, .12f) || TrailDistance(position) < 9f) continue;
                if (Mask(angle, radius, .004f, 11f) < .42f) continue;
                PlantSpruce(new Vector3(position.x, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) + .3f, position.z), Range(30f, 40f));
            }
        }

        private static void Rove(int clusters, int minTrees, int maxTrees, System.Func<float, Vector2> band, float spread,
            float minHeight, float maxHeight, System.Func<float, float, float> surface)
        {
            for (int cluster = 0; cluster < clusters; cluster++)
            {
                float centre = Range(0f, Mathf.PI * 2f);
                Vector2 range = band(centre);
                float radius = Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
                if (Mask(centre, radius, .004f, 11f) < .34f) continue;
                bool dense = Mask(centre, radius, .013f, 53f) > .55f;
                int trees = random.Next(minTrees, maxTrees + 1) * (dense ? 2 : 1);
                for (int tree = 0; tree < trees; tree++)
                {
                    float angle = centre + Range(-spread, spread) / Mathf.Max(45f, radius);
                    float distance = Mathf.Clamp(radius + Range(-spread, spread), range.x - spread * .35f, range.y + spread * .35f);
                    if (distance < 1f || NearFall(angle, .055f)) continue;
                    float y = surface(angle, distance) + .3f;
                    PlantSpruce(new Vector3(Mathf.Cos(angle) * distance, y, Mathf.Sin(angle) * distance), RangePower(minHeight, maxHeight, 2.2f));
                }
            }
        }

        private static void BuildSkyline()
        {
            Place(peaks, "Снежные хребты по кругу", Save(EmeraldBasinMesh.DistantRanges(), "DistantRanges.asset"),
                peakMaterial, Vector3.zero, Vector3.one);
        }

        private static void ScatterBoulder(int tier)
        {
            if (random.NextDouble() < .12) return;
            float angle = Range(0f, Mathf.PI * 2f);
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 1f) + 6f;
            float outer = EmeraldBasinMesh.ShelfOuter(tier, angle) - 6f;
            float radius = Range(inner, Mathf.Max(inner + 1f, outer));
            float size = Range(2.4f, 7.5f);
            var rock = Place(flora, "Валун на полке", random.NextDouble() > .5f ? boulderBank : boulderRidge, cliff,
                new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) - size * .12f, Mathf.Sin(angle) * radius),
                new Vector3(size, size * Range(.6f, 1.15f), size * Range(.7f, 1f)));
            rock.localRotation = Quaternion.Euler(0f, Range(0f, 360f), Range(-7f, 7f));
        }

        private static void ScatterShrub(int tier)
        {
            if (random.NextDouble() < .12) return;
            float angle = Range(0f, Mathf.PI * 2f);
            if (NearFall(angle, .07f)) return;
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 1f) + 8f;
            float outer = EmeraldBasinMesh.ShelfOuter(tier, angle) - 8f;
            float radius = Range(inner, Mathf.Max(inner + 1f, outer));
            var shrub = Place(flora, "Луговой кустарник", shrubMesh, tuftMaterial,
                new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) + 1f, Mathf.Sin(angle) * radius),
                new Vector3(Range(2.6f, 5.5f), Range(1.6f, 3.2f), Range(2.6f, 5.5f)));
            shrub.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        }

        private static void ScatterTuft(int tier)
        {
            if (random.NextDouble() < .13) return;
            float angle = Range(0f, Mathf.PI * 2f);
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 1f) + 9f;
            float outer = EmeraldBasinMesh.ShelfOuter(tier, angle) - 8f;
            float radius = Range(inner, Mathf.Max(inner + 1f, outer));
            var tuft = Place(flora, "Трава на полке", grassBlade, tuftMaterial,
                new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) + .1f, Mathf.Sin(angle) * radius),
                new Vector3(Range(1.6f, 3f), Range(2f, 3.8f), Range(1.6f, 3f)));
            tuft.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        }

        private static void PlantSpruce(Vector3 position, float height)
        {
            if (TrailDistance(position) < 6.5f) return;
            var tree = new GameObject("Ель").transform;
            tree.SetParent(flora); tree.position = position;
            tree.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            Parts(tree, "Хвоя", spruce, random.NextDouble() > .5f ? needles : needlesLight, Vector3.zero, Vector3.one * height);
            Parts(tree, "Ствол", trunkMesh, trunkMaterial, Vector3.zero, new Vector3(height * .13f, height * .45f, height * .13f));
        }

        private static void Parts(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false); item.transform.localPosition = position;
            item.transform.localScale = scale; item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            item.isStatic = true;
        }

        private static bool NearFall(float angle, float tolerance)
        {
            foreach (float fall in EmeraldBasinMesh.FallAngles)
                if (Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, fall * Mathf.Rad2Deg)) * Mathf.Deg2Rad < tolerance) return true;
            return false;
        }

        private static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static float RangePower(float min, float max, float power)
            => min + (max - min) * Mathf.Pow((float)random.NextDouble(), power);

        private static float Mask(float angle, float radius, float scale, float seed)
            => Mathf.PerlinNoise(Mathf.Cos(angle) * radius * scale + seed, Mathf.Sin(angle) * radius * scale + seed * 1.7f);

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform; group.SetParent(parent, false); return group;
        }

        private static Transform Place(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, bool collider = false)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false); item.transform.localPosition = position;
            item.transform.localScale = scale; item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material; item.isStatic = true;
            if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item.transform;
        }

        private static Material RockMaterial(string name, Color rockTint, float grass, float tiling, float layers, float flute,
            float grassSlope, float grassSlopeBlend, float dither, float contact, Color? sandOverride = null)
        {
            var material = new Material(Shader.Find("GoatDescent/EmeraldRock"));
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", rockTint);
            material.SetColor("_OliveTint", new Color(.42f, .43f, .33f));
            material.SetColor("_SandTint", sandOverride ?? new Color(.82f, .70f, .51f));
            material.SetColor("_SandDark", new Color(.73f, .62f, .42f));
            material.SetColor("_GrassTint", new Color(.22f, .32f, .20f));
            material.SetColor("_GrassMid", new Color(.29f, .38f, .22f));
            material.SetColor("_GrassKhaki", new Color(.39f, .41f, .28f));
            material.SetColor("_HazeTint", new Color(.72f, .80f, .80f));
            material.SetFloat("_Tiling", tiling);
            material.SetFloat("_Layers", layers);
            material.SetFloat("_GrassAmount", grass);
            material.SetFloat("_Flute", flute);
            material.SetFloat("_GrassSlope", grassSlope);
            material.SetFloat("_GrassSlopeBlend", grassSlopeBlend);
            material.SetFloat("_Dither", dither);
            material.SetFloat("_Contact", contact);
            material.SetFloat("_HazeStart", 300f);
            material.SetFloat("_HazeEnd", 1500f);
            material.SetFloat("_HazeAmount", .78f);
            return Save(material, name + ".mat");
        }

        private static Material PeakMaterial()
        {
            var material = new Material(Shader.Find("GoatDescent/EmeraldPeak"));
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", new Color(.46f, .56f, .74f));
            material.SetColor("_ForestTint", new Color(.10f, .18f, .11f));
            material.SetColor("_SnowTint", new Color(.97f, .98f, 1f));
            material.SetColor("_HazeTint", new Color(.76f, .84f, .92f));
            material.SetFloat("_Tiling", .014f);
            material.SetFloat("_ForestTop", 190f);
            material.SetFloat("_ForestBlend", 140f);
            material.SetFloat("_SnowStart", 400f);
            material.SetFloat("_SnowBlend", 170f);
            material.SetFloat("_Dither", .5f);
            return Save(material, "SnowPeaks.mat");
        }

        private static Material StandardMaterial(string name, Color color, float smoothness)
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
                destination.Clear(); destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices; destination.triangles = source.triangles;
                destination.normals = source.normals; destination.colors = source.colors;
                destination.uv = source.uv; destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); return existing;
        }

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.60f, .62f, .60f);
            RenderSettings.ambientEquatorColor = new Color(.52f, .48f, .38f);
            RenderSettings.ambientGroundColor = new Color(.32f, .27f, .19f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.84f, .80f, .70f); RenderSettings.fogDensity = .00018f;
            var sun = new GameObject("Летнее полуденное солнце").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, .93f, .78f); sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(72f, 30f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Заполняющий свет котловины").AddComponent<Light>();
            fill.type = LightType.Directional; fill.color = new Color(1f, .91f, .76f); fill.intensity = .65f;
            fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(38f, 45f, 0f);
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.30f, .58f, .93f));
            sky.SetColor("_SkyHorizon", new Color(.84f, .90f, .95f));
            sky.SetColor("_GroundColor", new Color(.74f, .82f, .85f));
            sky.SetColor("_SunColor", new Color(1f, .96f, .85f));
            sky.SetVector("_SunDir", -sun.transform.forward);
            sky.SetFloat("_SunSize", .085f);
            sky.SetFloat("_CloudDensity", .55f);
            sky.SetColor("_CloudColor", new Color(1f, .99f, .96f));
            RenderSettings.skybox = Save(sky, "EmeraldSky.mat");
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0); texture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }
    }
}
