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
        private const float WorldScale = 5f;

        private static Transform terraces, lake, falls, flora, peaks, trail;
        private static Material cliff, cliffSoft, cliffBare, meadow, shore, talusMaterial, peakMaterial, needles, needlesLight, canopy;
        private static Material trunkMaterial, tuftMaterial, reedMaterial, bloomMaterial, gravelMaterial, dirtMaterial, markerMaterial;
        private static Material waterMaterial, cascadeMaterial, cascadeLowerMaterial, streamMaterial, foamMaterial;
        private static Mesh spruce, trunkMesh, boulderBank, boulderRidge, boulderSmall, shrubMesh, grassBlade, foamMesh;
        private static Mesh[] cascadeMeshes, streamMeshes, talusMeshes, pebbleMeshes, ledgeMeshes, landingMeshes, midHillMeshes;
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
            BuildMidground();
            BuildSkyline();
            Environment();
            root.position = Vector3.zero;
            root.localScale = Vector3.one * WorldScale;
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 76f; camera.farClipPlane = 3200f * WorldScale;
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
            camera.fieldOfView = 66f;
            PlaceCamera(camera, new Vector3(0f, 96f, 230f), new Vector3(0f, 44f, -150f));
            Render(camera, "EmeraldBasin-lake");
            camera.fieldOfView = 68f;
            PlaceCamera(camera, new Vector3(150f, 140f, 150f), new Vector3(-280f, 230f, -280f));
            Render(camera, "EmeraldBasin-falls");
            camera.fieldOfView = 76f; Orbit(camera, 225f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Debug.Log("EMERALD_BASIN_STATIC_SAVED scene=" + ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Изумрудная котловина — снимки маршрута")]
        public static void RouteShots()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("ROUTE_SHOTS EmeraldBasin missing scene " + ScenePath);
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var points = RouteLine();
            if (points.Count < 2)
            {
                Debug.LogError("ROUTE_SHOTS EmeraldBasin trail not found");
                return;
            }
            var camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (!camera)
            {
                var go = new GameObject("Route Camera");
                camera = go.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                go.AddComponent<GoatDescent.GoatScenePost>();
            }
            camera.fieldOfView = 66f;
            camera.nearClipPlane = .5f;
            camera.farClipPlane = 3200f * WorldScale;
            Physics.SyncTransforms();
            float length = 0f;
            for (int index = 1; index < points.Count; index++) length += Vector3.Distance(points[index - 1], points[index]);
            const int count = 8;
            for (int shot = 0; shot < count; shot++)
            {
                float at = Mathf.Lerp(12f, Mathf.Max(12f, length - 12f), shot / (float)(count - 1));
                int index = 1;
                float travelled = 0f;
                while (index < points.Count - 1 && travelled < at)
                {
                    travelled += Vector3.Distance(points[index - 1], points[index]);
                    index++;
                }
                Vector3 point = points[index];
                Vector3 behind = points[Mathf.Max(index - 6, 0)];
                Vector3 ahead = points[Mathf.Min(index + 6, points.Count - 1)];
                Vector3 tangent = ahead - behind;
                if (tangent.sqrMagnitude < .0001f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 eye = point + Vector3.up * 12f - tangent * 10f;
                Vector3 look = point + tangent * 25f + Vector3.up * 4f;
                camera.transform.position = ClearCameraEye(eye, look);
                camera.transform.LookAt(look);
                Render(camera, "route-EmeraldBasin-" + (shot + 1).ToString("00"));
            }
            Debug.Log("ROUTE_SHOTS EmeraldBasin count=" + count);
            Debug.Log("ROUTE_SHOTS EmeraldBasin waypoints=" + points.Count);
        }

        private static Vector3 ClearCameraEye(Vector3 eye, Vector3 look)
        {
            float raised = 0f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 direction = look - eye;
                if (direction.sqrMagnitude < .0001f) direction = Vector3.forward;
                direction.Normalize();
                float lift = Mathf.Max(0f, HeightAt(eye) + 6f - eye.y);
                for (int probe = 1; probe <= 3; probe++)
                {
                    float distance = probe * 10f;
                    Vector3 sample = eye + direction * distance;
                    float clearance = sample.y - HeightAt(sample);
                    float needed = Mathf.Lerp(2.4f, 1.2f, (probe - 1) * .5f);
                    if (clearance < needed) lift = Mathf.Max(lift, needed - clearance);
                }
                if (lift <= .01f) return eye;
                float step = Mathf.Min(Mathf.Min(lift + .25f, 15f), 60f - raised);
                if (step <= 0f) return eye;
                eye += Vector3.up * step;
                raised += step;
            }
            return eye;
        }

        private static float HeightAt(Vector3 world)
            => TrailSurface(new Vector3(world.x, 0f, world.z) / WorldScale) * WorldScale;

        private static List<Vector3> RouteLine()
        {
            var axes = new List<List<Vector3>>();
            var ribbons = new List<List<Vector3>>();
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var parent = filter.transform.parent;
                if (!parent || (!parent.name.StartsWith("Тропа") && !parent.name.StartsWith("Переправа"))) continue;
                if (filter.name == "Ось тропы")
                {
                    var axisMesh = filter.sharedMesh;
                    if (!axisMesh || axisMesh.vertexCount < 2) continue;
                    var matrix = filter.transform.localToWorldMatrix;
                    var axis = new List<Vector3>(axisMesh.vertexCount);
                    foreach (var vertex in axisMesh.vertices) axis.Add(matrix.MultiplyPoint3x4(vertex));
                    axes.Add(axis);
                    continue;
                }
                if (filter.name != "Полоса тропы") continue;
                var mesh = filter.sharedMesh;
                if (!mesh || mesh.vertexCount < 4) continue;
                var vertices = mesh.vertices;
                var localToWorld = filter.transform.localToWorldMatrix;
                var line = new List<Vector3>(vertices.Length / 2);
                for (int index = 0; index + 1 < vertices.Length; index += 2)
                    line.Add(localToWorld.MultiplyPoint3x4((vertices[index] + vertices[index + 1]) * .5f));
                if (line.Count >= 2) ribbons.Add(line);
            }
            var lines = axes.Count > 0 ? axes : ribbons;
            if (lines.Count == 0) return new List<Vector3>();
            int largest = 0;
            for (int index = 1; index < lines.Count; index++)
                if (lines[index].Count > lines[largest].Count) largest = index;
            var route = new List<Vector3>(lines[largest]);
            lines.RemoveAt(largest);
            while (lines.Count > 0)
            {
                int best = -1;
                bool reverse = false;
                float bestDistance = 90f;
                for (int index = 0; index < lines.Count; index++)
                {
                    float head = Vector3.Distance(route[route.Count - 1], lines[index][0]);
                    float tail = Vector3.Distance(route[route.Count - 1], lines[index][lines[index].Count - 1]);
                    if (head < bestDistance) { bestDistance = head; best = index; reverse = false; }
                    if (tail < bestDistance) { bestDistance = tail; best = index; reverse = true; }
                }
                if (best < 0) break;
                var next = lines[best];
                if (reverse) next.Reverse();
                route.AddRange(next);
                lines.RemoveAt(best);
            }
            return route;
        }

        private static void Orbit(Camera camera, float azimuth)
        {
            float radians = azimuth * Mathf.Deg2Rad;
            PlaceCamera(camera, new Vector3(Mathf.Cos(radians) * 580f, 680f, Mathf.Sin(radians) * 580f),
                new Vector3(0f, 505f, 0f));
        }

        private static void PlaceCamera(Camera camera, Vector3 eye, Vector3 target)
        {
            eye *= WorldScale;
            target *= WorldScale;
            float ground = TrailSurface(eye / WorldScale) * WorldScale + 12f * WorldScale;
            if (ground > eye.y) eye.y = ground;
            camera.transform.position = eye;
            camera.transform.LookAt(target);
        }

        private static void Materials()
        {
            cliff = RockMaterial("EmeraldCliff", new Color(1.38f, 1.24f, 1.02f), .40f, .07f, .18f, .10f, .50f, .30f, .72f, .14f);
            cliffSoft = RockMaterial("EmeraldCliffSoft", new Color(1.33f, 1.20f, .99f), .42f, .07f, .18f, .06f, .52f, .32f, .72f, .13f);
            cliffBare = RockMaterial("EmeraldCliffBare", new Color(1.30f, 1.18f, .98f), .39f, .07f, .05f, .03f, .56f, .36f, .72f, .12f);
            meadow = RockMaterial("EmeraldMeadow", new Color(.94f, .98f, .82f), 1f, .05f, .06f, .03f, .30f, .34f, .42f, .18f);
            shore = RockMaterial("EmeraldShore", new Color(1.00f, .94f, .82f), .9f, .06f, .10f, .05f, .26f, .30f, .48f, .34f,
                new Color(.87f, .82f, .73f));
            talusMaterial = RockMaterial("EmeraldTalus", new Color(1.02f, .98f, .92f), .05f, .10f, .05f, .08f, .70f, .20f, .62f, .30f);
            gravelMaterial = RockMaterial("EmeraldGravel", new Color(1.06f, 1.00f, .92f), .04f, .16f, .04f, .04f, .75f, .20f, .75f, .22f,
                new Color(.88f, .83f, .74f));
            peakMaterial = PeakMaterial();
            needles = StandardMaterial("SpruceNeedles", new Color(.075f, .225f, .105f), .04f);
            needlesLight = StandardMaterial("SpruceNeedlesLight", new Color(.135f, .31f, .155f), .05f);
            canopy = StandardMaterial("ForestCanopy", new Color(.075f, .21f, .11f), .03f);
            trunkMaterial = StandardMaterial("SpruceTrunk", new Color(.19f, .15f, .11f), .03f);
            tuftMaterial = StandardMaterial("MeadowShrub", new Color(.30f, .33f, .21f), .04f);
            reedMaterial = StandardMaterial("LakeReeds", new Color(.13f, .19f, .11f), .05f);
            bloomMaterial = StandardMaterial("AlpineBloom", new Color(.60f, .58f, .44f), .06f);
            dirtMaterial = StandardMaterial("TrailDirt", new Color(.40f, .31f, .20f), .04f);
            markerMaterial = StandardMaterial("TrailMarker", new Color(.76f, .74f, .70f), .12f);
            var water = new Material(Shader.Find("GoatDescent/EmeraldWater"));
            water.SetColor("_ShallowColor", new Color(.05f, .28f, .22f));
            water.SetColor("_DeepColor", new Color(.008f, .075f, .085f));
            water.SetColor("_FoamColor", new Color(.62f, .78f, .80f));
            water.SetVector("_Center", Vector4.zero);
            water.SetFloat("_Radius", EmeraldBasinMesh.LakeRadius * WorldScale);
            water.SetFloat("_Smoothness", .55f);
            water.SetFloat("_WaveScale", .012f / WorldScale);
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
            landingMeshes = new Mesh[3];
            midHillMeshes = new Mesh[3];
            for (int variant = 0; variant < 3; variant++)
            {
                talusMeshes[variant] = Save(EmeraldBasinMesh.Talus(11 + variant * 7), "Talus-" + variant + ".asset");
                pebbleMeshes[variant] = Save(EmeraldBasinMesh.Pebbles(41 + variant * 13), "Pebbles-" + variant + ".asset");
                ledgeMeshes[variant] = Save(EmeraldBasinMesh.Ledge(61 + variant * 9), "Ledge-" + variant + ".asset");
                landingMeshes[variant] = Save(EmeraldBasinMesh.Landing(211 + variant * 13), "Landing-" + variant + ".asset");
                midHillMeshes[variant] = Save(EmeraldBasinMesh.MidHill(131 + variant * 17), "MidHill-" + variant + ".asset");
            }
        }

        private static void BuildBasin()
        {
            var wallMaterials = new[] { cliff, cliffSoft, cliffBare, cliff, cliffBare, cliffSoft };
            for (int tier = 1; tier <= 3; tier++)
            {
                const int sectors = 6;
                for (int sector = 0; sector < sectors; sector++)
                {
                    float from = sector * Mathf.PI * 2f / sectors;
                    float to = (sector + 1) * Mathf.PI * 2f / sectors;
                    Place(terraces, "Скальная стена — ярус " + tier + ", сектор " + (sector + 1),
                        Save(EmeraldBasinMesh.TerraceWall(tier, from, to), "Terrace-" + tier + "-" + sector + ".asset"),
                        wallMaterials[sector], Vector3.zero, Vector3.one, true);
                }
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

            AddRoute(5.60f, 600f, false);
            AddRoute(5.48f, 610f, false);
            AddRoute(5.58f, 582f, false);
            AddRoute(5.42f, 556f, false);
            AddShelfRoute(3, 5.42f, .55f);

            AddWallRoute(3, 5.35f, .95f);
            AddWallRoute(3, 5.40f, .62f);
            AddWallRoute(3, 5.33f, .30f);
            AddWallRoute(3, 5.36f, .02f);

            ArcShelf(2, 5.36f, 5.21f, .55f);

            AddWallRoute(2, 5.18f, .95f);
            AddWallRoute(2, 5.23f, .60f);
            AddWallRoute(2, 5.16f, .28f);
            AddWallRoute(2, 5.20f, .02f);

            ArcShelf(1, 5.20f, 5.10f, .5f);
            ArcShelf(1, 5.10f, 5.10f + Mathf.PI * 2f, .38f);

            AddWallRoute(2, 5.10f, .02f);
            AddWallRoute(2, 5.16f, .34f);
            AddWallRoute(2, 5.09f, .66f);
            AddWallRoute(2, 5.14f, .98f);

            ArcShelf(2, 5.14f, 5.05f, .55f);

            AddWallRoute(3, 5.00f, .02f);
            AddWallRoute(3, 5.06f, .34f);
            AddWallRoute(3, 4.99f, .66f);
            AddWallRoute(3, 5.04f, .98f);

            ArcShelf(3, 5.04f, 4.92f, .5f);

            AddWallRoute(3, 4.78f, .98f);
            AddWallRoute(3, 4.84f, .66f);
            AddWallRoute(3, 4.77f, .34f);
            AddWallRoute(3, 4.80f, .02f);

            ArcShelf(2, 4.80f, 4.70f, .55f);

            AddWallRoute(2, 4.68f, .98f);
            AddWallRoute(2, 4.74f, .66f);
            AddWallRoute(2, 4.67f, .34f);
            AddWallRoute(2, 4.70f, .02f);

            ArcShelf(1, 4.70f, 4.62f, .5f);

            AddWallRoute(1, 4.42f, .95f);
            AddWallRoute(1, 4.47f, .55f);
            AddWallRoute(1, 4.43f, .02f);

            float wall1 = EmeraldBasinMesh.WallRadius(1, 4.44f, 0f);
            float shore = EmeraldBasinMesh.LakeEdge(4.45f);
            float beach = Mathf.Min(wall1 - 6f, Mathf.Max(shore + 10f, 330f));
            float entry = Mathf.Min(shore - 4f, beach - 8f);
            AddRoute(4.45f, beach, false);
            AddRoute(4.46f, entry, true);
            AddRoute(4.55f, 205f, true);

            DensifyRoute();
            routeLength = 0f;
            for (int i = 1; i < route.Count; i++) routeLength += Flat(route[i - 1], route[i]);
            Debug.Log("GOAT_TRAIL EmeraldBasin waypoints=" + route.Count + " length~" + Mathf.RoundToInt(routeLength) + "м");
        }

        private static void AddRoute(float angle, float radius, bool wet)
        {
            route.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            routeWet.Add(wet);
        }

        private static float ShelfRadius(int tier, float angle, float fraction)
        {
            float inner = EmeraldBasinMesh.WallRadius(tier, angle, 1f) + 9f;
            float outer = EmeraldBasinMesh.ShelfOuter(tier, angle) - 9f;
            return Mathf.Lerp(inner, outer, fraction);
        }

        private static void AddShelfRoute(int tier, float angle, float fraction)
            => AddRoute(angle, ShelfRadius(tier, angle, fraction), false);

        private static void ArcShelf(int tier, float from, float to, float fraction)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(to - from) / .05f));
            for (int step = 1; step <= steps; step++)
            {
                float angle = Mathf.Lerp(from, to, step / (float)steps);
                AddShelfRoute(tier, angle, fraction);
            }
        }

        private static void AddWallRoute(int tier, float angle, float v)
            => AddRoute(angle, EmeraldBasinMesh.WallRadius(tier, angle, v), false);

        private static void DensifyRoute()
        {
            if (route == null || route.Count < 2) return;
            var dense = new List<Vector3>(route.Count * 6);
            var wet = new List<bool>(route.Count * 6);
            dense.Add(route[0]); wet.Add(routeWet[0]);
            float carried = 0f;
            float step = Range(3.8f, 5.2f);
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 a = route[i - 1], b = route[i];
                float segment = Flat(a, b);
                if (segment < .0001f) continue;
                float travelled = 0f;
                while (carried + segment - travelled >= step)
                {
                    travelled += step - carried;
                    dense.Add(Vector3.Lerp(a, b, travelled / segment));
                    wet.Add(routeWet[i - 1] || routeWet[i]);
                    carried = 0f;
                    step = Range(3.8f, 5.2f);
                }
                carried += segment - travelled;
            }
            if (Flat(dense[dense.Count - 1], route[route.Count - 1]) > .5f)
            {
                dense.Add(route[route.Count - 1]);
                wet.Add(routeWet[routeWet.Count - 1]);
            }
            route = dense;
            routeWet = wet;
        }

        private static float Flat(Vector3 a, Vector3 b)
            => new Vector2(a.x - b.x, a.z - b.z).magnitude;

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
                    TrailRibbon(crossing ? "Переправа по камням" : "Тропа по кромке и полкам", sampler, dirtMaterial, run,
                        crossing ? 1.3f : 2f, crossing ? .12f : .16f, crossing ? 0f : 9f, !crossing,
                        crossing ? gravelMaterial : talusMaterial, true);
                }
                start = Mathf.Max(0, i - 1);
            }
            Debug.Log("GOAT_CHECKPOINTS EmeraldBasin count=" + PlaceCheckpoints());
            FinishMarker();
        }

        private static List<Vector3> TrailSamples(IList<Vector3> waypoints, float step)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 a = waypoints[i], b = waypoints[i + 1];
                float length = Vector3.Distance(a, b);
                int count = Mathf.Max(1, Mathf.RoundToInt(length / step));
                for (int k = 0; k < count; k++) points.Add(Vector3.Lerp(a, b, k / (float)count));
            }
            points.Add(waypoints[waypoints.Count - 1]);
            return points;
        }

        private static void TrailRibbon(string name, System.Func<Vector3, float> terrain, Material material,
            IList<Vector3> waypoints, float width, float lift, float stoneEvery, bool withRibbon, Material stoneMaterial, bool axis)
        {
            if (waypoints == null || waypoints.Count < 2) return;
            var samples = TrailSamples(waypoints, 2.2f);
            var root = new GameObject(name).transform;
            root.SetParent(trail, false);
            if (withRibbon) TrailRibbonMesh(root, name, terrain, material, samples, width, lift);
            if (axis) TrailAxis(root, terrain, samples);
            if (stoneEvery > 0f) TrailEdgeStones(root, terrain, samples, width, stoneEvery, stoneMaterial);
        }

        private static void TrailRibbonMesh(Transform root, string name, System.Func<Vector3, float> terrain,
            Material material, List<Vector3> samples, float width, float lift)
        {
            int count = samples.Count;
            var offsets = new[] { -1.12f, -1f, -.58f, 0f, .58f, 1f, 1.12f };
            var profile = new[] { 0f, 0f, .45f, 1f, .45f, 0f, 0f };
            float half = width * .5f;
            float camber = width * .07f;
            var vertices = new List<Vector3>(count * offsets.Length);
            var uvs = new List<Vector2>(count * offsets.Length);
            var tris = new List<int>((count - 1) * (offsets.Length - 1) * 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = samples[i];
                Vector3 a = samples[Mathf.Max(i - 1, 0)], b = samples[Mathf.Min(i + 1, count - 1)];
                Vector3 forward = b - a;
                if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                float wobble = .86f + .26f * Mathf.PerlinNoise(p.x * .5f + 7.3f, p.z * .5f + 3.1f)
                    + .12f * (Mathf.PerlinNoise(p.x * 1.9f + 1.1f, p.z * 1.9f + 5.7f) - .5f);
                float jitterLeft = (Mathf.PerlinNoise(i * .37f + 8.1f, 3.7f) - .5f) * width * .19f
                    + (Mathf.PerlinNoise(i * 1.73f + 2.9f, 9.4f) - .5f) * width * .07f;
                float jitterRight = (Mathf.PerlinNoise(i * .43f + 13.7f, 6.2f) - .5f) * width * .19f
                    + (Mathf.PerlinNoise(i * 2.11f + 5.3f, 1.8f) - .5f) * width * .07f;
                float heightNoise = (Mathf.PerlinNoise(p.x * 1.7f + 4.1f, p.z * 1.7f + 7.6f) - .5f) * .06f
                    + (Mathf.PerlinNoise(p.x * 4.3f + 9.2f, p.z * 4.3f + 2.5f) - .5f) * .025f;
                for (int column = 0; column < offsets.Length; column++)
                {
                    bool skirt = column == 0 || column == offsets.Length - 1;
                    bool rim = column == 1 || column == offsets.Length - 2;
                    float jitter = offsets[column] < 0f ? jitterLeft : jitterRight;
                    float lateral = offsets[column] * half * wobble;
                    if (rim || skirt) lateral += jitter;
                    Vector3 point = p + side * lateral;
                    float y = skirt
                        ? terrain(point) - .05f + heightNoise * .35f
                        : terrain(point) + lift + profile[column] * camber + heightNoise * (1f - Mathf.Abs(offsets[column]) * .25f);
                    vertices.Add(new Vector3(point.x, y, point.z));
                    uvs.Add(new Vector2(offsets[column] * .5f + .5f, i * .3f));
                }
            }
            for (int i = 0; i < count - 1; i++)
            {
                for (int column = 0; column < offsets.Length - 1; column++)
                {
                    int a = i * offsets.Length + column;
                    int b = a + 1;
                    int c = a + offsets.Length;
                    int d = c + 1;
                    tris.Add(a); tris.Add(d); tris.Add(b);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }
            var mesh = new Mesh { name = name + "TrailMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var ribbon = new GameObject("Полоса тропы");
            ribbon.transform.SetParent(root, false);
            ribbon.AddComponent<MeshFilter>().sharedMesh = mesh;
            ribbon.AddComponent<MeshRenderer>().sharedMaterial = material;
            ribbon.isStatic = true;
        }

        private static void TrailAxis(Transform root, System.Func<Vector3, float> terrain, List<Vector3> samples)
        {
            var vertices = new List<Vector3>(samples.Count);
            foreach (var sample in samples) vertices.Add(new Vector3(sample.x, terrain(sample), sample.z));
            var mesh = new Mesh { name = "TrailAxisMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(new int[0], 0);
            mesh.RecalculateBounds();
            var axis = new GameObject("Ось тропы");
            axis.transform.SetParent(root, false);
            axis.AddComponent<MeshFilter>().sharedMesh = mesh;
        }

        private static void TrailEdgeStones(Transform root, System.Func<Vector3, float> terrain, List<Vector3> samples,
            float width, float every, Material material)
        {
            var local = new System.Random(7301);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var source = boulderSmall.vertices;
            var sourceTris = boulderSmall.triangles;
            float travel = every * .4f;
            for (int i = 1; i < samples.Count; i++)
            {
                Vector3 a = samples[i - 1], b = samples[i];
                Vector3 forward = b - a;
                float segment = new Vector2(forward.x, forward.z).magnitude;
                if (segment < .0001f) continue;
                forward /= segment;
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                travel += segment;
                while (travel >= every)
                {
                    travel -= every;
                    float t = Mathf.Clamp01(1f - travel / segment);
                    Vector3 basePoint = Vector3.Lerp(a, b, t);
                    for (int edge = -1; edge <= 1; edge += 2)
                    {
                        float offset = width * .5f * (1.05f + (float)local.NextDouble() * .5f) * edge;
                        var position = basePoint + side * offset + forward * ((float)local.NextDouble() - .5f) * 1.4f;
                        float size = width * (.13f + (float)local.NextDouble() * .15f);
                        float ground = terrain(position);
                        position = new Vector3(position.x, ground - size * .38f, position.z);
                        var rotation = Quaternion.Euler((float)local.NextDouble() * 12f - 6f,
                            (float)local.NextDouble() * 360f, (float)local.NextDouble() * 12f - 6f);
                        var matrix = Matrix4x4.TRS(position, rotation,
                            new Vector3(size, size * (.55f + (float)local.NextDouble() * .35f), size * (.7f + (float)local.NextDouble() * .4f)));
                        int baseIndex = vertices.Count;
                        foreach (var vertex in source) vertices.Add(matrix.MultiplyPoint3x4(vertex));
                        foreach (int index in sourceTris) triangles.Add(baseIndex + index);
                    }
                }
            }
            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = "TrailEdgeStonesMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var stones = new GameObject("Камни у кромки тропы");
            stones.transform.SetParent(root, false);
            stones.AddComponent<MeshFilter>().sharedMesh = mesh;
            stones.AddComponent<MeshRenderer>().sharedMaterial = material;
            stones.isStatic = true;
        }

        private static void FinishMarker()
        {
            var point = route[route.Count - 1];
            float y = TrailSurface(point);
            var marker = Place(trail, "Финиш на острове", boulderRidge, markerMaterial,
                new Vector3(point.x, y - 1.1f, point.z), Vector3.one * 4.6f);
            marker.localRotation = Quaternion.Euler(0f, 37f, 0f);
            Cairn(new Vector3(point.x + 4.2f, 0f, point.z + 2.4f));
        }

        private static int PlaceCheckpoints()
        {
            int number = 0;
            float travelled = 0f;
            float next = 0f;
            for (int i = 0; i < route.Count; i++)
            {
                if (i > 0) travelled += Flat(route[i - 1], route[i]);
                if (routeWet[i] || (i > 0 && travelled < next)) continue;
                var point = route[i];
                Vector3 ahead = route[Mathf.Min(i + 2, route.Count - 1)];
                Vector3 behind = route[Mathf.Max(i - 2, 0)];
                var heading = new Vector3(ahead.x - behind.x, 0f, ahead.z - behind.z);
                if (heading.sqrMagnitude < .01f) heading = Vector3.right;
                heading.Normalize();
                var side = new Vector3(-heading.z, 0f, heading.x);
                float y = TrailSurface(point);
                number++;
                var marker = Place(trail, "Чекпоинт " + number, boulderRidge, markerMaterial,
                    new Vector3(point.x + side.x * 3.6f, y - 1.1f, point.z + side.z * 3.6f), Vector3.one * Range(3.2f, 4.2f));
                marker.localRotation = Quaternion.Euler(0f, number * 61f, 0f);
                Cairn(new Vector3(point.x - side.x * 3.2f, y, point.z - side.z * 3.2f));
                next = travelled + 400f;
            }
            return number;
        }

        private static void BuildJumpSpots()
        {
            int clusters = 0;
            float travelled = 0f;
            float next = 18f;
            for (int i = 1; i < route.Count; i++)
            {
                travelled += Flat(route[i - 1], route[i]);
                if (routeWet[i] || travelled < next) continue;
                JumpCluster(i);
                clusters++;
                next = travelled + Range(34f, 44f);
            }
            int stones = 0;
            int run = -1;
            for (int i = 0; i <= route.Count; i++)
            {
                bool wet = i < route.Count && routeWet[i];
                if (wet) { if (run < 0) run = i; continue; }
                if (run >= 0) { stones += CrossingRun(run, i - 1); run = -1; }
            }
            Debug.Log("GOAT_JUMPS EmeraldBasin clusters=" + clusters + " crossingStones=" + stones);
        }

        private static void JumpCluster(int index)
        {
            var point = route[index];
            Vector3 ahead = route[Mathf.Min(index + 2, route.Count - 1)];
            Vector3 behind = route[Mathf.Max(index - 2, 0)];
            var heading = new Vector3(ahead.x - behind.x, 0f, ahead.z - behind.z);
            if (heading.sqrMagnitude < .04f) heading = new Vector3(1f, 0f, 0f);
            heading.Normalize();
            var side = new Vector3(-heading.z, 0f, heading.x);
            int steps = 3 + (random.NextDouble() < .4 ? 1 : 0);
            float spacing = Range(3.0f, 3.45f);
            float yaw = Mathf.Atan2(-heading.z, heading.x) * Mathf.Rad2Deg;
            float previousTop = TrailSurface(point) + .3f;
            for (int step = 0; step < steps; step++)
            {
                float along = (step - (steps - 1) * .5f) * spacing;
                float lateral = Mathf.Sin(step * 2.1f + index * .7f) * Range(.05f, .2f);
                var probe = point + heading * along + side * lateral;
                float ground = TrailSurface(probe);
                float width = Range(2.15f, 2.7f);
                float depth = Range(2.1f, 2.65f);
                float thickness = Range(.95f, 1.2f);
                float embed = thickness * Range(.30f, .36f);
                float top = ground + thickness - embed;
                if (step > 0) top = Mathf.Clamp(top, previousTop - 1.1f, previousTop + 1.1f);
                previousTop = top;
                float y = top - thickness;
                var slab = Place(trail, "Прыжковая плита", landingMeshes[(step + index) % landingMeshes.Length],
                    random.NextDouble() > .3f ? cliff : talusMaterial,
                    new Vector3(probe.x, y, probe.z), new Vector3(width, thickness, depth));
                slab.localRotation = Quaternion.Euler(Range(-3f, 3f), yaw + Range(-8f, 8f), Range(-3f, 3f));
                if (top - ground > 1.4f)
                {
                    var column = Place(trail, "Опора прыжковой плиты", boulderBank, talusMaterial,
                        new Vector3(probe.x, ground - .2f, probe.z),
                        new Vector3(width * .7f, (top - ground) * .95f, depth * .65f));
                    column.localRotation = Quaternion.Euler(0f, yaw + Range(-12f, 12f), 0f);
                }
                LedgeSkirt(probe, y, width * 1.05f);
                if (width > 2.45f) SatelliteStone(probe, y, width, yaw);
            }
            if (random.NextDouble() < .22f) Cairn(point + side * Range(3.5f, 5f));
        }

        private static int CrossingRun(int start, int end)
        {
            int stones = 0;
            for (int i = start; i <= end; i++)
            {
                CrossingStone(route[i], i);
                stones++;
                if (i < end && Flat(route[i], route[i + 1]) > 3.4f)
                {
                    CrossingStone(Vector3.Lerp(route[i], route[i + 1], .5f), i);
                    stones++;
                }
            }
            return stones;
        }

        private static void CrossingStone(Vector3 point, int index)
        {
            float width = Range(2.2f, 2.75f);
            float depth = width * Range(.85f, 1.08f);
            float thickness = Range(.9f, 1.2f);
            float ground = TrailSurface(point);
            float support = Mathf.Max(EmeraldBasinMesh.LakeLevel + .32f, ground + .1f) + Range(-.1f, .14f);
            float top = Mathf.Max(support, ground + thickness * .64f);
            float y = top - thickness;
            var stone = Place(trail, "Валун-переправа", landingMeshes[index % landingMeshes.Length],
                random.NextDouble() > .35f ? cliff : talusMaterial,
                new Vector3(point.x, y, point.z), new Vector3(width, thickness, depth));
            stone.localRotation = Quaternion.Euler(Range(-2f, 2f), Range(0f, 360f), Range(-2f, 2f));
            var rim = Place(trail, "Мокрая кромка валуна", boulderSmall, talusMaterial,
                new Vector3(point.x, y + thickness * Range(.22f, .36f), point.z),
                new Vector3(width * 1.28f, thickness * .62f, depth * 1.15f));
            rim.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
        }

        private static void LedgeSkirt(Vector3 centre, float anchorY, float size)
        {
            int stones = random.Next(3, 6);
            for (int stone = 0; stone < stones; stone++)
            {
                float bearing = Range(0f, Mathf.PI * 2f);
                float distance = size * Range(1.02f, 1.45f);
                var probe = new Vector3(centre.x + Mathf.Cos(bearing) * distance, 0f, centre.z + Mathf.Sin(bearing) * distance);
                float s = size * Range(.07f, .18f);
                var item = Place(trail, "Юбка осыпи под полкой",
                    random.NextDouble() > .34f ? boulderSmall : pebbleMeshes[random.Next(0, pebbleMeshes.Length)],
                    random.NextDouble() > .45f ? talusMaterial : gravelMaterial,
                    new Vector3(probe.x, Mathf.Max(TrailSurface(probe), anchorY) - s * Range(.12f, .34f), probe.z),
                    new Vector3(s * Range(.8f, 1.3f), s * Range(.4f, .85f), s * Range(.7f, 1.2f)));
                item.localRotation = Quaternion.Euler(Range(-8f, 8f), Range(0f, 360f), Range(-8f, 8f));
            }
        }

        private static void SatelliteStone(Vector3 anchor, float anchorY, float size, float yaw)
        {
            float satelliteSize = size * Range(.36f, .52f);
            float bearing = Range(0f, Mathf.PI * 2f);
            float overlap = size * Range(.8f, 1.04f);
            var probe = new Vector3(anchor.x + Mathf.Cos(bearing) * overlap, 0f, anchor.z + Mathf.Sin(bearing) * overlap);
            var item = Place(trail, "Спутник-камень внахлёст",
                random.NextDouble() > .5f ? ledgeMeshes[random.Next(0, ledgeMeshes.Length)] : boulderSmall,
                random.NextDouble() > .4f ? cliff : talusMaterial,
                new Vector3(probe.x, anchorY + size * Range(.05f, .2f), probe.z),
                new Vector3(satelliteSize, satelliteSize * Range(.5f, .72f), satelliteSize * Range(.75f, 1f)));
            item.localRotation = Quaternion.Euler(Range(-8f, 8f),
                yaw + Range(60f, 140f) * (random.NextDouble() > .5f ? 1f : -1f), Range(-8f, 8f));
        }

        private static void Cairn(Vector3 point)
        {
            float ground = TrailSurface(point);
            int stones = random.Next(2, 4);
            float yaw = Range(0f, 360f);
            float size = Range(1.5f, 2.1f);
            float lift = 0f;
            for (int stone = 0; stone < stones; stone++)
            {
                float s = size * Mathf.Pow(.66f, stone) * Range(.85f, 1.15f);
                float squeeze = Range(.55f, .85f);
                var item = Place(trail, "Ориентир прыжка — камень " + (stone + 1),
                    stone % 2 == 0 ? boulderSmall : boulderBank, markerMaterial,
                    new Vector3(point.x + Range(-.14f, .14f) * size, ground + lift - s * .1f, point.z + Range(-.14f, .14f) * size),
                    new Vector3(s, s * squeeze, s * Range(.78f, 1.08f)));
                item.localRotation = Quaternion.Euler(Range(-8f, 8f), yaw + Range(-24f, 24f), Range(-8f, 8f));
                lift += s * squeeze * .58f;
            }
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
                float size = Range(3f, 8f);
                Place(flora, "Валун на внешнем склоне", random.NextDouble() > .5f ? boulderRidge : boulderSmall, cliff,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.SlopeHeight(angle, radius) - size * .40f, Mathf.Sin(angle) * radius),
                    Vector3.one * size);
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
                        float v = Mathf.Clamp01(Range(-.04f, .88f));
                        float size = Range(6f, 17f);
                        Vector3 normal = WallNormal(tier, a, v);
                        Vector3 surface = WallPoint(tier, a, v);
                        var item = Place(flora, "Нагромождение глыб в провале", random.NextDouble() > .5f ? boulderRidge : boulderBank, talusMaterial,
                            surface - normal * (size * .42f),
                            new Vector3(size, size * Range(.72f, 1.05f), size * Range(.82f, 1.05f)));
                        item.localRotation = Quaternion.FromToRotation(Vector3.up, normal)
                            * Quaternion.Euler(0f, Range(0f, 360f), Range(-8f, 8f));
                    }
                }
            }
        }

        private static Vector3 WallPoint(int tier, float angle, float v)
        {
            float radius = EmeraldBasinMesh.WallRadius(tier, angle, v);
            return new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.WallHeight(tier, angle, v), Mathf.Sin(angle) * radius);
        }

        private static Vector3 WallNormal(int tier, float angle, float v)
        {
            const float da = .012f, dv = .05f;
            Vector3 lowerV = WallPoint(tier, angle, Mathf.Max(0f, v - dv));
            Vector3 upperV = WallPoint(tier, angle, Mathf.Min(1f, v + dv));
            Vector3 normal = Vector3.Cross(WallPoint(tier, angle + da, v) - WallPoint(tier, angle - da, v), upperV - lowerV);
            Vector3 inward = new Vector3(-Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
            if (normal.sqrMagnitude < .000001f) return inward;
            normal.Normalize();
            return Vector3.Dot(normal, inward) < 0f ? -normal : normal;
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
            var sandMaterial = StandardMaterial("TrailSand", new Color(.74f, .63f, .46f), .05f);
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
                TrailRibbon("Песчаная тропинка " + (path + 1), new System.Func<Vector3, float>(TrailSurface),
                    sandMaterial, points, 1.6f, .12f, 0f, true, talusMaterial, false);
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
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) - size * .40f, Mathf.Sin(angle) * radius),
                    new Vector3(size, size * Range(.72f, .95f), size * Range(.82f, 1.05f)));
                block.localRotation = Quaternion.Euler(Range(-6f, 6f), Range(0f, 360f), Range(-6f, 6f));
            }
            float shoreAngle = Range(0f, Mathf.PI * 2f);
            float shoreRadius = EmeraldBasinMesh.LakeEdge(shoreAngle) + Range(8f, 20f);
            float shoreSize = Range(22f, 28f);
            var shoreBlock = Place(flora, "Мега-глыба на берегу", boulderBank, talusMaterial,
                new Vector3(Mathf.Cos(shoreAngle) * shoreRadius, EmeraldBasinMesh.FloorHeight(shoreAngle, shoreRadius) - shoreSize * .40f, Mathf.Sin(shoreAngle) * shoreRadius),
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

        private static void BuildMidground()
        {
            for (int hill = 0; hill < 22; hill++)
            {
                float angle = hill / 22f * Mathf.PI * 2f + Range(-.11f, .11f);
                float radius = Range(760f, 930f);
                float height = Range(26f, 58f);
                float width = Range(60f, 120f);
                var item = Place(peaks, "Средний план — холм на внешнем склоне", midHillMeshes[hill % midHillMeshes.Length],
                    hill % 4 == 0 ? talusMaterial : meadow,
                    new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.SlopeHeight(angle, radius) - height * .1f, Mathf.Sin(angle) * radius),
                    new Vector3(width, height, width * Range(.75f, 1.15f)));
                item.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            }
            for (int hill = 0; hill < 38; hill++)
            {
                float angle = hill / 38f * Mathf.PI * 2f + Range(-.06f, .06f);
                float radius = Range(950f, 1290f);
                float height = Range(58f, 128f);
                float width = Range(100f, 195f);
                float ground = radius > 1012f ? EmeraldBasinMesh.RangeHeight(angle, radius) : EmeraldBasinMesh.SlopeHeight(angle, radius);
                var item = Place(peaks, "Средний план — зелёный холм", midHillMeshes[hill % midHillMeshes.Length],
                    hill % 5 == 0 ? talusMaterial : meadow,
                    new Vector3(Mathf.Cos(angle) * radius, ground - height * .08f, Mathf.Sin(angle) * radius),
                    new Vector3(width, height, width * Range(.72f, 1.18f)));
                item.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            }
            for (int ridge = 0; ridge < 30; ridge++)
            {
                float angle = ridge / 30f * Mathf.PI * 2f + Range(-.09f, .09f) + .12f;
                float radius = Range(960f, 1240f);
                float height = Range(66f, 165f);
                float width = Range(18f, 42f);
                float ground = radius > 1012f ? EmeraldBasinMesh.RangeHeight(angle, radius) : EmeraldBasinMesh.SlopeHeight(angle, radius);
                var item = Place(peaks, "Средний план — скала-останец",
                    ridge % 2 == 0 ? boulderRidge : boulderBank,
                    ridge % 3 == 0 ? cliff : talusMaterial,
                    new Vector3(Mathf.Cos(angle) * radius, ground - height * .16f, Mathf.Sin(angle) * radius),
                    new Vector3(width, height, width * Range(.6f, .95f)));
                item.localRotation = Quaternion.Euler(0f, Range(0f, 360f), Range(-4f, 4f));
            }
            for (int tree = 0; tree < 90; tree++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = Range(860f, 1240f);
                float ground = radius > 1012f ? EmeraldBasinMesh.RangeHeight(angle, radius) : EmeraldBasinMesh.SlopeHeight(angle, radius);
                PlantSpruce(new Vector3(Mathf.Cos(angle) * radius, ground + .3f, Mathf.Sin(angle) * radius), Range(22f, 46f));
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
                new Vector3(Mathf.Cos(angle) * radius, EmeraldBasinMesh.ShelfHeight(tier, angle, radius) - size * .40f, Mathf.Sin(angle) * radius),
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
            Parts(tree, "Ствол", trunkMesh, trunkMaterial, Vector3.zero, new Vector3(height * .11f, height * .45f, height * .11f));
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
            material.SetColor("_GrassTint", new Color(.24f, .31f, .19f));
            material.SetColor("_GrassMid", new Color(.31f, .37f, .21f));
            material.SetColor("_GrassKhaki", new Color(.40f, .41f, .27f));
            material.SetColor("_HazeTint", new Color(.72f, .80f, .80f));
            material.SetFloat("_Tiling", tiling / WorldScale);
            material.SetFloat("_Layers", layers);
            material.SetFloat("_GrassAmount", grass);
            material.SetFloat("_Flute", flute);
            material.SetFloat("_GrassSlope", grassSlope);
            material.SetFloat("_GrassSlopeBlend", grassSlopeBlend);
            material.SetFloat("_Dither", dither);
            material.SetFloat("_Contact", contact);
            material.SetFloat("_HazeStart", 300f * WorldScale);
            material.SetFloat("_HazeEnd", 1500f * WorldScale);
            material.SetFloat("_HazeAmount", .78f);
            return Save(material, name + ".mat");
        }

        private static Material PeakMaterial()
        {
            var material = new Material(Shader.Find("GoatDescent/EmeraldPeak"));
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", new Color(.34f, .43f, .57f));
            material.SetColor("_ForestTint", new Color(.09f, .16f, .12f));
            material.SetColor("_SnowTint", new Color(.62f, .70f, .82f));
            material.SetColor("_HazeTint", new Color(.58f, .68f, .82f));
            material.SetFloat("_Tiling", .014f / WorldScale);
            material.SetFloat("_ForestTop", 190f * WorldScale);
            material.SetFloat("_ForestBlend", 140f * WorldScale);
            material.SetFloat("_SnowStart", 560f * WorldScale);
            material.SetFloat("_SnowBlend", 210f * WorldScale);
            material.SetFloat("_Dither", .6f);
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
            RenderSettings.fogColor = new Color(.82f, .83f, .85f); RenderSettings.fogDensity = .00018f / WorldScale;
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
