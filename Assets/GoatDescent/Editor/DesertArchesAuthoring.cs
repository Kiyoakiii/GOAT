using System.Collections.Generic;
using System.IO;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class DesertArchesAuthoring
    {
        private const string AssetsPath = "Assets/GoatDescent/Level/Static/DesertArches";
        private const string ScenePath = "Assets/GoatDescent/Scenes/DesertArches.unity";
        private const int TerrainGrid = 208;
        private const int PlinthLevels = 8;
        private static readonly Vector3 Viewpoint = new Vector3(-270f, 0f, 315f);

        private struct Deck
        {
            public Vector3 centre;
            public float semiX;
            public float semiZ;
            public float top;
        }

        private struct Site
        {
            public Vector3 summit;
            public float top;
            public float yaw;
            public float span;
            public float depth;
            public float height;
            public float shoulder;
            public List<Deck> decks;
        }

        private static readonly List<Site> sites = new List<Site>();
        private static readonly List<Vector4> heroAreas = new List<Vector4>();
        private static System.Random random = new System.Random(7717);
        private static Transform rocks, terraces, plants, distant;
        private static Material sandstone, sunlit, sand, wood, scrub, grass, towerRock, towerRockB, towerRockC, haze;
        private static Mesh shardMesh;
        private static CrownRockMesh collapse;
        private static Mesh[] trees, bushes, tufts;
        private static Vector3 heroEyes;
        private static Vector3 heroAim = new Vector3(405f, 130f, -235f);
        private static Vector3 archView;
        private static Vector3 archBase;
        private static Vector3 wallCentre;
        private static Vector3 wallAlong = Vector3.right;
        private static Vector3 wallAcross = Vector3.forward;
        private static float wallCrest;
        private static Vector3 ledgeCentre;
        private static float ledgeTop;

        [MenuItem("Tools/GOAT/Статичные горы/Пустынные арки — открыть")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Пустынных арок."); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Пустынные арки — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AssetsPath); AssetDatabase.Refresh();
            PruneLegacy();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sites.Clear(); heroAreas.Clear();

            sandstone = RockMaterial("Sandstone", new Color(1.10f, .60f, .34f), .34f, .13f, .35f, new Vector3(13f, -7f, 29f), .02f, 6.5f);
            sunlit = RockMaterial("SunlitSandstone", new Color(1.14f, .72f, .44f), .26f, .16f, 1.9f, new Vector3(-41f, 23f, 7f), .035f, 7.5f);
            sand = RockMaterial("Sand", new Color(.98f, .76f, .55f), 0f, .14f, .7f, new Vector3(5f, 63f, -19f), 0f, 3.5f);
            towerRock = RockMaterial("DistantTower", new Color(1.06f, .58f, .32f), .2f, .12f, 0f, new Vector3(0f, 0f, 0f), .045f, 8f);
            towerRockB = RockMaterial("DistantTowerB", new Color(1.00f, .54f, .30f), .24f, .165f, 1.15f, new Vector3(37f, 11f, 5f), -.055f, 7.2f);
            towerRockC = RockMaterial("DistantTowerC", new Color(1.10f, .62f, .36f), .17f, .093f, 2.35f, new Vector3(-23f, -17f, 41f), .07f, 8.6f);
            wood = Standard("DryWood", new Color(.20f, .14f, .09f), .04f);
            scrub = Standard("DryScrub", new Color(.20f, .22f, .12f), .02f);
            grass = Standard("DryGrass", new Color(.58f, .50f, .26f), .02f);
            haze = HazeMaterial();
            shardMesh = Save(DesertArchesMesh.Fragment(), "SandstoneShard.asset");
            collapse = DesertArchesMesh.Collector();
            Mesh stem = Save(Object.Instantiate(DecorMeshLibrary.TrunkMesh()), "DryStem.asset");
            Mesh blob = Save(Object.Instantiate(DecorMeshLibrary.OctahedronMesh()), "ScrubBlob.asset");
            Mesh blade = Save(Object.Instantiate(DecorMeshLibrary.GrassBladeMesh()), "DryBlade.asset");
            trees = new Mesh[5];
            for (int i = 0; i < trees.Length; i++) trees[i] = Add("DryTree-" + i + ".asset", DesertArchesMesh.Tree(1f, 1700 + i * 97));
            bushes = new Mesh[5];
            for (int i = 0; i < bushes.Length; i++) bushes[i] = Add("DryBush-" + i + ".asset", DesertArchesMesh.Bush(1f, 2100 + i * 61));
            tufts = new Mesh[4];
            for (int i = 0; i < tufts.Length; i++) tufts[i] = Add("DryGrassTuft-" + i + ".asset", DesertArchesMesh.Tuft(1f, 2600 + i * 43));

            var root = new GameObject("Пустынные арки — статичная панорама").transform;
            rocks = new GameObject("Каньон, пласты и столбы").transform; rocks.SetParent(root, false);
            terraces = new GameObject("Террасы, осыпи и растительность").transform; terraces.SetParent(root, false);
            plants = new GameObject("Сухие деревья").transform; plants.SetParent(root, false);
            distant = new GameObject("Дальний план — башни и хребты").transform; distant.SetParent(root, false);

            Floor();
            HeroSites();
            Spawn(rocks, "Обрушение у арок", Add("ArchCollapse.asset", DesertArchesMesh.Finish(collapse, "Обрушение у арок")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            CameraSite();
            Scatter(shardMesh);
            Horizon();
            GoatTrail(root);
            RouteGuide(root);
            Environment();

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 52f;
            camera.farClipPlane = 3000f;
            heroEyes = Clear(heroEyes, heroAim);
            Vector3 overviewEye = heroEyes + new Vector3(heroEyes.x - heroAim.x, 0f, heroEyes.z - heroAim.z).normalized * 620f;
            overviewEye.y = heroEyes.y + 260f;
            camera.transform.position = Clear(overviewEye, heroAim);
            camera.transform.LookAt(heroAim);
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.warmTint = new Color(1.07f, 1.0f, .9f);
            post.coolTint = new Color(.99f, .97f, .93f);
            post.saturation = 1.24f;
            post.bloomIntensity = .5f;
            post.contrast = 1.16f;
            post.vignette = .3f;
            post.bloomThreshold = .72f;

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);

            Render(camera, "DesertArches-overview");
            Site grand = sites[0];
            Site second = sites[1];
            Vector3 windowA = grand.summit + Vector3.up * grand.height * .5f;
            Vector3 windowB = second.summit + Vector3.up * second.height * .5f;
            Vector3 wallLow = new Vector3(grand.summit.x, DesertArchesMesh.Surface(grand.summit.x, grand.summit.z), grand.summit.z);
            Vector3 wallHigh = new Vector3(grand.summit.x, wallCrest, grand.summit.z);
            Vector3 aim = Vector3.Lerp(wallLow, wallHigh, .5f);
            float distance, inFrame;
            int hits;
            Vector3 archEye = FrameArch(archView, aim, 35f, wallLow, wallHigh, windowA, windowB, out distance, out hits, out inFrame);
            Shoot(camera, "DesertArches-arch", archEye, aim, 35f);
            Debug.Log("ARCH_CAM dist=" + Mathf.RoundToInt(distance) + " hit0=" + hits + " wallInFrame=" + Mathf.RoundToInt(inFrame * 100f) + "%");
            float rimZ = 300f;
            float rimX = DesertArchesMesh.ChannelX(rimZ) + 240f;
            Vector3 canyonEye = new Vector3(rimX, DesertArchesMesh.Surface(rimX, rimZ) + 24f, rimZ);
            Shoot(camera, "DesertArches-canyon", canyonEye,
                new Vector3(DesertArchesMesh.ChannelX(-420f), DesertArchesMesh.FloorY - 60f, -420f));
            Shoot(camera, "DesertArches-terraces", heroEyes + new Vector3(34f, 12f, 46f), heroEyes + new Vector3(-30f, -48f, -150f));
            Vector3 outward = (heroEyes - grand.summit).normalized;
            Shoot(camera, "DesertArches-horizon", heroEyes + Vector3.up * 210f,
                heroEyes + new Vector3(outward.x, .06f, outward.z) * 1700f + Vector3.up * 260f);
            Vector3 backEye = wallCentre - wallAcross * 880f;
            backEye.y = DesertArchesMesh.Surface(backEye.x, backEye.z) + 30f;
            Shoot(camera, "DesertArches-back", backEye, new Vector3(windowA.x, grand.top + grand.height * 1.1f, windowA.z));
            Debug.Log("DESERT_ARCHES_STATIC_SAVED scene=" + ScenePath);
        }

        private static Vector3 Lift(Vector3 position)
        {
            float ground = DesertArchesMesh.Surface(position.x, position.z);
            if (ground > position.y - 5f) position.y = ground + 15f;
            return position;
        }

        private static float Blocked(Vector3 from, Vector3 to)
        {
            const int steps = 28;
            for (int step = 1; step < steps; step++)
            {
                float t = step / (float)steps;
                Vector3 point = Vector3.Lerp(from, to, t);
                if (point.y - DesertArchesMesh.Surface(point.x, point.z) < 8f) return t;
            }
            return 0f;
        }

        private static Vector3 Clear(Vector3 position, Vector3 target)
        {
            Vector3 eye = Lift(position);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float at = Blocked(eye, target);
                if (at <= 0f) return eye;
                Vector3 sample = Vector3.Lerp(eye, target, at);
                eye.y = Mathf.Max(eye.y + 26f, DesertArchesMesh.Surface(sample.x, sample.z) + 42f);
            }
            eye += (eye - target).normalized * 220f;
            eye.y += 70f;
            return Lift(eye);
        }

        private static void Shoot(Camera camera, string name, Vector3 position, Vector3 target, float fieldOfView = 0f)
        {
            float previous = camera.fieldOfView;
            if (fieldOfView > 0f) camera.fieldOfView = fieldOfView;
            camera.transform.position = Clear(position, target);
            camera.transform.LookAt(target);
            Render(camera, name);
            camera.fieldOfView = previous;
        }

        private static float Elevation(Vector3 eye, Vector3 point)
        {
            Vector3 delta = point - eye;
            return Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
        }

        private static float WallCoverage(Vector3 eye, Vector3 low, Vector3 high, float fieldOfView)
        {
            return Mathf.Abs(Elevation(eye, high) - Elevation(eye, low)) / fieldOfView;
        }

        private static bool RayBlocked(Vector3 eye, Vector3 target)
        {
            for (int step = 1; step < 28; step++)
            {
                Vector3 point = Vector3.Lerp(eye, target, step / 28f);
                if (point.y < DesertArchesMesh.Surface(point.x, point.z) + 2f) return true;
            }
            return false;
        }

        private static int WindowHits(Vector3 eye, Vector3 windowA, Vector3 windowB)
        {
            int hits = 0;
            if (RayBlocked(eye, windowA)) hits++;
            if (RayBlocked(eye, windowB)) hits++;
            return hits;
        }

        private static float EdgeRise(Vector3 forward, Vector3 up, float offset, float depth)
        {
            Vector3 direction = (forward + up * offset).normalized;
            return direction.y * depth / Mathf.Max(.001f, Vector3.Dot(direction, forward));
        }

        private static void FrameEdges(Vector3 eye, Vector3 target, float fieldOfView, out float bottom, out float top)
        {
            Vector3 forward = (target - eye).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            float half = Mathf.Tan(fieldOfView * .5f * Mathf.Deg2Rad);
            float depth = Mathf.Max(1f, Vector3.Dot(target - eye, forward));
            bottom = eye.y + EdgeRise(forward, up, -half, depth);
            top = eye.y + EdgeRise(forward, up, half, depth);
        }

        private static Vector3 FrameArch(Vector3 position, Vector3 target, float fieldOfView,
            Vector3 low, Vector3 high, Vector3 windowA, Vector3 windowB,
            out float distance, out int hits, out float inFrame)
        {
            Vector3 eye = Lift(position);
            distance = Vector3.Distance(eye, target);
            hits = 0;
            inFrame = 0f;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                hits = WindowHits(eye, windowA, windowB);
                FrameEdges(eye, target, fieldOfView, out float bottom, out float top);
                distance = Vector3.Distance(eye, target);
                inFrame = WallCoverage(eye, low, high, fieldOfView);
                bool clear = hits == 0;
                bool depth = bottom < DesertArchesMesh.FloorY;
                bool whole = top > wallCrest + 20f;
                if (clear && depth && whole) break;
                Vector3 flat = target - eye;
                flat.y = 0f;
                if (!clear) eye.y += 45f;
                if (!depth || !whole)
                {
                    eye -= flat.normalized * 120f;
                    eye.y += 18f;
                }
                eye = Lift(eye);
            }
            return eye;
        }

        private static void Floor()
        {
            Spawn(rocks, "Каньон, пласты и дальние гряды",
                Add("Terrain.asset", DesertArchesMesh.Terrain(TerrainGrid, DesertArchesMesh.TerrainExtent, DesertArchesMesh.TerrainOuter, 16)),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, true);
            Spawn(terraces, "Сухая река — главное русло",
                Add("DryRiver.asset", DesertArchesMesh.Riverbed(240, 6, true)), sand, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(terraces, "Сухая река — приток каньона",
                Add("DryRiverBranch.asset", DesertArchesMesh.Riverbed(190, 5, false)), sand, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(terraces, "Пыльная дымка главного русла", Save(DesertArchesMesh.Haze(220, 62f, true), "CanyonHaze.asset"),
                haze, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(terraces, "Пыльная дымка притока", Save(DesertArchesMesh.Haze(180, 46f, false), "CanyonHazeBranch.asset"),
                haze, Vector3.zero, Vector3.zero, Vector3.one, false);
        }

        private static void HeroSites()
        {
            Vector3 ground = DesertArchesMesh.Highest(430f, -40f, 120f);
            float yaw = Mathf.Atan2(Viewpoint.x - ground.x, Viewpoint.z - ground.z) * Mathf.Rad2Deg;
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 across = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float crest = ground.y + 430f;
            var decks = new List<Deck>();
            float[][] piers =
            {
                new[] { -620f, 150f, -30f },
                new[] { -80f, 110f, 0f },
                new[] { 620f, 150f, -18f }
            };
            for (int piece = 0; piece < piers.Length; piece++)
            {
                Vector3 centre = ground + along * piers[piece][0] + across * -42f;
                float top = crest + piers[piece][2];
                Mesh slab = Add("WallPier-" + piece + ".asset", DesertArchesMesh.Slab(piers[piece][1], 108f, top, DesertArchesMesh.FloorY - 90f, 8, 6001f + piece * 7f));
                Spawn(rocks, "Устой стены " + piece, slab, piece == 1 ? sunlit : sandstone,
                    new Vector3(centre.x, 0f, centre.z), new Vector3(0f, yaw, 0f), Vector3.one, true);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, piers[piece][1] + 40f));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = piers[piece][1] * 1.15f, semiZ = 72f, top = top });
            }
            for (int band = 0; band < 3; band++)
            {
                float offset = (band - 1) * 460f;
                float top = crest + (band == 1 ? 30f : 0f);
                Mesh slab = Add("WallCrest-" + band + ".asset", DesertArchesMesh.Slab(580f, 96f, top, 140f, 6, 6101f + band * 13f));
                Spawn(rocks, "Свод-пласт стены " + band, slab, band == 1 ? sandstone : sunlit,
                    new Vector3(ground.x + along.x * offset - across.x * 60f, 0f, ground.z + along.z * offset - across.z * 60f),
                    new Vector3(0f, yaw + (band - 1) * .6f, 0f), Vector3.one, true);
                decks.Add(new Deck
                {
                    centre = new Vector3(ground.x + along.x * offset, 0f, ground.z + along.z * offset),
                    semiX = 520f, semiZ = 92f, top = top
                });
            }
            for (int crown = 0; crown < 7; crown++)
            {
                float offset = Range(random, -680f, 680f);
                float lift = Range(random, 16f, 120f);
                float top = crest + lift + 6f;
                Mesh outcrop = Add("WallCrown-" + crown + ".asset", DesertArchesMesh.Butte(1f, 1f, 14, 7103f + crown * 19f));
                Vector3 centre = ground + along * offset + across * Range(random, -28f, 28f);
                Spawn(rocks, "Вершина гребня " + crown, outcrop, crown % 2 == 0 ? sandstone : sunlit,
                    new Vector3(centre.x, crest - 52f, centre.z), new Vector3(0f, yaw + Range(random, -20f, 20f), 0f),
                    new Vector3(Range(random, 150f, 300f), lift + 60f, Range(random, 60f, 100f)), true);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, Range(random, 170f, 260f)));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = 190f, semiZ = 88f, top = top });
            }
            for (int ledge = 0; ledge < 2; ledge++)
            {
                float side = ledge == 0 ? -1f : 1f;
                float offset = side * 560f;
                Vector3 centre = ground + along * offset + across * -190f;
                float top = ground.y + 40f - ledge * 14f;
                Mesh slab = Add("WallApron-" + ledge + ".asset", DesertArchesMesh.Slab(240f, 118f, top, DesertArchesMesh.FloorY - 90f, 7, 6301f + ledge * 11f));
                Spawn(rocks, "Терраса у подножия стены " + ledge, slab, ledge == 0 ? sunlit : sandstone,
                    new Vector3(centre.x, 0f, centre.z), new Vector3(0f, yaw + (ledge * 4f - 2f), 0f), Vector3.one, true);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, 280f));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = 240f, semiZ = 155f, top = top });
            }
            wallCentre = ground;
            wallAlong = along;
            wallAcross = across;
            wallCrest = crest;

            Window("Главное окно-арка", "WindowMain", ground + along * 150f, -158f, 235f, 420f, 140f, 175f, 140f, yaw, decks, 7001);
            Window("Второе окно-арка", "WindowFar", ground + along * -280f, ground.y - 6f, 170f, 260f, 52f, 132f, 92f, yaw, decks, 7003);

            for (int block = 0; block < 34; block++)
            {
                float offset = Range(random, -560f, 560f);
                float front = Range(random, -1f, 1f) * 150f;
                Vector3 spot = ground + along * offset + across * front;
                float floor = DesertArchesMesh.Surface(spot.x, spot.z);
                DesertArchesMesh.Stamp(collapse, shardMesh, Matrix4x4.TRS(new Vector3(spot.x, floor + 1f, spot.z),
                    Quaternion.Euler(Range(random, -14f, 14f), Range(random, 0f, 360f), Range(random, -14f, 14f)),
                    new Vector3(Range(random, 10f, 26f), Range(random, 4f, 12f), Range(random, 10f, 26f))));
            }
        }

        private static void Window(string title, string asset, Vector3 place, float baseY,
            float span, float height, float pier, float depth, float shoulder, float yaw, List<Deck> decks, int seed)
        {
            Mesh arch = Add(asset + ".asset", DesertArchesMesh.Arch(span, height, pier, depth, shoulder, seed));
            Spawn(rocks, title, arch, sandstone, new Vector3(place.x, baseY, place.z), new Vector3(0f, yaw, 0f), Vector3.one, true);
            sites.Add(new Site
            {
                summit = new Vector3(place.x, baseY, place.z),
                top = baseY,
                yaw = yaw,
                span = span,
                depth = depth,
                height = height,
                shoulder = shoulder,
                decks = decks
            });
        }

        private static void CameraSite()
        {
            Vector3 summit = DesertArchesMesh.Highest(-285f, 125f, 110f);
            float top = summit.y + 3f;
            ledgeCentre = new Vector3(summit.x, 0f, summit.z);
            ledgeTop = top;
            Site grand = sites[0];
            Vector3 towardWall = grand.summit - summit;
            towardWall.y = 0f;
            towardWall = towardWall.sqrMagnitude > .001f ? towardWall.normalized : Vector3.forward;
            heroEyes = new Vector3(summit.x + towardWall.x * 44f, top + 14f, summit.z + towardWall.z * 44f);
            heroAim = new Vector3(grand.summit.x, (grand.top + wallCrest) * .5f, grand.summit.z);
            Mesh ledge = Add("ViewLedge.asset", DesertArchesMesh.Terrace(150f, 128f, top, DesertArchesMesh.FloorY - 90f, 7, 91));
            Spawn(terraces, "Смотровая терраса на кромке каньона", ledge, sunlit, ledgeCentre, Vector3.zero, Vector3.one, true);
            heroAreas.Add(new Vector4(summit.x, top, summit.z, 158f));
            Mesh bench = Add("ViewLedge-Lower.asset", DesertArchesMesh.Terrace(206f, 176f, top - 40f, DesertArchesMesh.FloorY - 90f, 6, 97));
            Spawn(terraces, "Нижняя смотровая терраса", bench, sandstone, ledgeCentre, Vector3.zero, Vector3.one, true);
            Vector3 vantage = wallCentre + wallAcross * 300f + wallAlong * 620f;
            float vantageTop = wallCrest + 60f;
            Mesh mesa = Add("ViewMesa.asset", DesertArchesMesh.Terrace(46f, 42f, vantageTop, DesertArchesMesh.FloorY - 90f, 8, 811));
            Spawn(terraces, "Смотровой утёс на дальнем борту", mesa, sandstone, new Vector3(vantage.x, 0f, vantage.z), Vector3.zero, Vector3.one, true);
            heroAreas.Add(new Vector4(vantage.x, vantageTop, vantage.z, 70f));
            archBase = vantage;
            archView = wallCentre + wallAcross * 2400f;
            archView.y = wallCrest + 120f;
        }

        private static void Scatter(Mesh shard)
        {
            var rubble = DesertArchesMesh.Collector();
            var bright = DesertArchesMesh.Collector();
            var scrubField = DesertArchesMesh.Collector();
            var grassField = DesertArchesMesh.Collector();
            var random = new System.Random(5150);
            const float spacing = 24f;
            for (float x = -834f; x <= 834f; x += spacing)
                for (float z = -834f; z <= 834f; z += spacing)
                {
                    float px = x + Range(random, -9f, 9f);
                    float pz = z + Range(random, -9f, 9f);
                    float py = DesertArchesMesh.Surface(px, pz);
                    if (Hero(px, py, pz)) continue;
                    float focus = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(280f, 1080f, new Vector2(px + 70f, pz - 150f).magnitude));
                    float bank = DesertArchesMesh.ChannelDistance(px, pz);
                    float rim = 1f - Mathf.Abs(Mathf.InverseLerp(150f, 400f, bank) * 2f - 1f);
                    float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.32f, .64f, Mathf.PerlinNoise(px * .0032f + 53f, pz * .0029f + 71f)));
                    float grove = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, .72f, Mathf.PerlinNoise(px * .0019f + 17f, pz * .0021f + 29f)));
                    float talus = Mathf.PerlinNoise(px * .0068f + 11f, pz * .0061f + 97f);
                    float cover = (.2f + .8f * focus) * (.3f + 1.2f * rim) * (.12f + .88f * patch);
                    float slope = DesertArchesMesh.Slope(px, pz);
                    if (py < DesertArchesMesh.FloorY + 60f)
                    {
                        int debris = talus > .6f ? 3 : talus > .38f ? 1 : 0;
                        for (int pebble = 0; pebble < debris; pebble++) Chip(rubble, px, py, pz, Range(random, .9f, 3f), random);
                        if (random.NextDouble() < .1f) Scrub(scrubField, px, py, pz, Range(random, 0.8f, 2.2f), random);
                        continue;
                    }
                    int stones = talus > .62f ? 1 + random.Next(3) : (talus > .34f && random.NextDouble() < .5 ? 1 : 0);
                    for (int stone = 0; stone < stones; stone++)
                        Chip(random.NextDouble() < .5 ? rubble : bright, px, py, pz, Range(random, .7f, 1.4f + Mathf.Min(slope, 1.6f) * 2.2f), random);
                    if (slope < .68f && random.NextDouble() < .3f * cover)
                        Scrub(scrubField, px, py, pz, Range(random, 1f, 3f), random);
                    if (slope < .86f && random.NextDouble() < .26f * cover)
                        Tuft(grassField, px, py, pz, Range(random, 0.8f, 2.2f), random);
                    if (slope < .5f && py > DesertArchesMesh.FloorY + 70f && random.NextDouble() < .045f * cover * grove)
                    {
                        float size = Range(random, 12f, 16f);
                        Tree(px, py, pz, size, random);
                        int cluster = 1 + random.Next(3);
                        for (int extra = 0; extra < cluster; extra++)
                            Tree(px + Range(random, -24f, 24f), py, pz + Range(random, -24f, 24f), size * Range(random, .55f, .9f), random);
                    }
                }
            for (int i = 0; i < 54; i++)
            {
                float angle = Range(random, 0f, 360f);
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 146f;
                float px = ledgeCentre.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float pz = ledgeCentre.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                Chip(random.NextDouble() < .5 ? rubble : bright, px, ledgeTop, pz, Range(random, 1.4f, 3.8f), random);
                if (i % 3 == 0) Scrub(scrubField, px, ledgeTop, pz, Range(random, 1f, 3f), random);
                if (i % 2 == 0) Tuft(grassField, px, ledgeTop, pz, Range(random, 0.8f, 2f), random);
            }
            Tree(ledgeCentre.x - 34f, ledgeTop, ledgeCentre.z - 26f, 15.5f, random);
            Tree(ledgeCentre.x - 52f, ledgeTop, ledgeCentre.z - 18f, 12.5f, random);
            Tree(ledgeCentre.x - 58f, ledgeTop, ledgeCentre.z + 24f, 13.4f, random);
            Tree(ledgeCentre.x + 30f, ledgeTop, ledgeCentre.z - 62f, 12.6f, random);
            Tree(ledgeCentre.x + 96f, ledgeTop, ledgeCentre.z + 42f, 14.4f, random);
            for (int giant = 0; giant < 5; giant++)
            {
                Vector3 spot = ledgeCentre + Quaternion.Euler(0f, giant * 71f + 34f, 0f) * new Vector3(58f + giant * 21f, 0f, 0f);
                float size = Range(random, 7f, 11f);
                var rock = new GameObject("Глыба-гигант " + (giant + 1));
                rock.transform.SetParent(terraces, false);
                rock.transform.position = new Vector3(spot.x, ledgeTop - size * .12f, spot.z);
                rock.transform.rotation = Quaternion.Euler(Range(random, -10f, 10f), Range(random, 0f, 360f), Range(random, -10f, 10f));
                rock.transform.localScale = new Vector3(size, size * .6f, size * .8f);
                rock.isStatic = true;
                rock.AddComponent<MeshFilter>().sharedMesh = shardMesh;
                rock.AddComponent<MeshRenderer>().sharedMaterial = sandstone;
            }
            for (int giant = 0; giant < 4; giant++)
            {
                float distance = 62f + giant * 52f;
                Vector3 spot = ledgeCentre + Quaternion.Euler(0f, giant * 97f + 61f, 0f) * new Vector3(distance, 0f, 0f);
                float ground = distance < 150f ? ledgeTop : DesertArchesMesh.Surface(spot.x, spot.z);
                Tree(spot.x, ground, spot.z, Range(random, 18f, 24f), random);
            }
            for (int i = 0; i < sites.Count; i++)
            {
                Site site = sites[i];
                for (int item = 0; item < 34; item++)
                {
                    float angle = Range(random, 0f, 360f);
                    float radius = Range(random, 24f, site.span * 1.05f);
                    float px = site.summit.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                    float pz = site.summit.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                    Chip(random.NextDouble() < .5 ? rubble : bright, px, site.top, pz, Range(random, 1.3f, 3.8f), random);
                    if (item % 2 == 0) Scrub(scrubField, px, site.top, pz, Range(random, 1f, 3f), random);
                    if (item % 3 == 0) Tuft(grassField, px, site.top, pz, Range(random, 0.8f, 2f), random);
                    if (item % 4 == 0) Tree(px + Range(random, -16f, 16f), site.top, pz + Range(random, -16f, 16f), Range(random, 12f, 16f), random);
                }
                for (int block = 0; block < 6; block++)
                {
                    float angle = Range(random, 0f, 360f);
                    float radius = Range(random, site.span * .5f, site.span * 1f);
                    Vector3 spot = new Vector3(site.summit.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius, site.top - 1f,
                        site.summit.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                    DesertArchesMesh.Stamp(rubble, shard, Matrix4x4.TRS(spot,
                        Quaternion.Euler(Range(random, -10f, 10f), Range(random, 0f, 360f), Range(random, -10f, 10f)),
                        new Vector3(Range(random, 4f, 9f), Range(random, 2f, 5f), Range(random, 4f, 9f))));
                }
            }
            Spawn(terraces, "Осыпи и обломки песчаника", Add("CanyonRubble.asset", DesertArchesMesh.Finish(rubble, "Осыпи песчаника")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(terraces, "Светлые обломки пластов", Add("CanyonRubbleSunlit.asset", DesertArchesMesh.Finish(bright, "Светлые обломки")),
                sunlit, Vector3.zero, Vector3.zero, Vector3.one, false);
            Vector3 away = -wallAcross;
            Vector3 aside = new Vector3(-away.z, 0f, away.x);
            for (int plant = 0; plant < 40; plant++)
            {
                Vector3 spot = archBase + away * Range(random, 6f, 34f) + aside * Range(random, -40f, 40f);
                float top = archBase.y + 1f;
                if (random.NextDouble() < .3) Tree(spot.x, top, spot.z, Range(random, 9f, 14f), random);
                else if (random.NextDouble() < .6) Scrub(scrubField, spot.x, top, spot.z, Range(random, 1f, 3f), random);
                else Tuft(grassField, spot.x, top, spot.z, Range(random, 0.8f, 2.2f), random);
            }
            Spawn(plants, "Сухие кусты", Add("ScrubField.asset", DesertArchesMesh.Finish(scrubField, "Сухие кусты")),
                scrub, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(plants, "Пучки сухой травы", Add("GrassField.asset", DesertArchesMesh.Finish(grassField, "Сухая трава")),
                grass, Vector3.zero, Vector3.zero, Vector3.one, false);
            for (int plant = 0; plant < 220; plant++)
            {
                float offset = Range(random, -580f, 580f);
                float side = Range(random, -1f, 1f) < 0f ? -1f : 1f;
                Vector3 spot = wallCentre + wallAlong * offset + wallAcross * (side * Range(random, 6f, 60f));
                if (DesertArchesMesh.ChannelDistance(spot.x, spot.z) < 110f) continue;
                if (random.NextDouble() < .5)
                {
                    float top = wallCrest + 2f;
                    if (random.NextDouble() < .62) Scrub(scrubField, spot.x, top, spot.z, Range(random, 1f, 3f), random);
                    else Tuft(grassField, spot.x, top, spot.z, Range(random, 0.8f, 2.2f), random);
                }
                else
                {
                    float ground = DesertArchesMesh.Surface(spot.x, spot.z);
                    if (random.NextDouble() < .34) Tree(spot.x, ground, spot.z, Range(random, 11f, 18f), random);
                    else if (random.NextDouble() < .6) Scrub(scrubField, spot.x, ground, spot.z, Range(random, 1f, 3f), random);
                    else Tuft(grassField, spot.x, ground, spot.z, Range(random, 0.8f, 2.2f), random);
                }
            }
        }

        private static void Chip(CrownRockMesh collector, float x, float y, float z, float size, System.Random random)
        {
            DesertArchesMesh.Chip(collector, Matrix4x4.TRS(new Vector3(x, y + size * .16f, z),
                Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f)),
                new Vector3(size * Range(random, .7f, 1.3f), size * Range(random, .32f, .7f), size * Range(random, .7f, 1.3f))), random);
        }

        private static void Scrub(CrownRockMesh collector, float x, float y, float z, float size, System.Random random)
        {
            Mesh mesh = bushes[random.Next(bushes.Length)];
            DesertArchesMesh.Stamp(collector, mesh, Matrix4x4.TRS(new Vector3(x, y - .3f, z),
                Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), Vector3.one * size));
        }

        private static void Tuft(CrownRockMesh collector, float x, float y, float z, float size, System.Random random)
        {
            Mesh mesh = tufts[random.Next(tufts.Length)];
            DesertArchesMesh.Stamp(collector, mesh, Matrix4x4.TRS(new Vector3(x, y - .2f, z),
                Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), new Vector3(size * Range(random, .8f, 1.2f), size, size * Range(random, .8f, 1.2f))));
        }

        private static void Tree(float x, float y, float z, float scale, System.Random random)
        {
            Mesh mesh = trees[random.Next(trees.Length)];
            var item = new GameObject("Сухое раздвоенное дерево");
            item.transform.SetParent(plants, false);
            item.transform.position = new Vector3(x, y - .6f, z);
            item.transform.rotation = Quaternion.Euler(Range(random, -6f, 6f), Range(random, 0f, 360f), Range(random, -6f, 6f));
            item.transform.localScale = Vector3.one * scale;
            item.isStatic = true;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = wood;
        }

        private static void Horizon()
        {
            var random = new System.Random(90210);
            var pillars = new Mesh[9];
            int variant = 0;
            for (int kind = 0; kind < 3; kind++)
                for (int seed = 0; seed < 3; seed++)
                    pillars[variant++] = Add("DesertPillar-" + kind + "-" + seed + ".asset",
                        DesertArchesMesh.DistantRock(kind, kind == 1 ? 12 : 14, 300f + kind * 41.7f + seed * 13.1f));
            for (int i = 0; i < 68; i++)
            {
                float angle = i / 68f * 360f + Range(random, -3.6f, 3.6f);
                float radius = Range(random, 600f, 1080f);
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                float ground = DesertArchesMesh.Surface(x, z);
                float roll = (float)random.NextDouble();
                if (roll < .09f) continue;
                float height = roll < .21f ? Range(random, 430f, 620f) : roll < .5f ? Range(random, 235f, 380f) : Range(random, 135f, 230f);
                float width = height * Range(random, .13f, .34f);
                float tilt = roll > .78f ? Range(random, 5f, 12f) * (random.NextDouble() < .5 ? 1f : -1f) : Range(random, -2.5f, 2.5f);
                float tiltYaw = Range(random, 0f, 360f) * Mathf.Deg2Rad;
                Material skin = i % 3 == 0 ? towerRock : i % 3 == 1 ? towerRockB : towerRockC;
                Spawn(distant, "Столб и башня пустыни", pillars[(i * 7) % pillars.Length], skin,
                    new Vector3(x, ground - height * .07f, z),
                    new Vector3(tilt * Mathf.Cos(tiltYaw), Range(random, 0f, 360f), tilt * Mathf.Sin(tiltYaw)),
                    new Vector3(width, height, width), false);
                if (i % 3 == 0)
                {
                    float small = height * Range(random, .34f, .62f);
                    float smallWidth = small * Range(random, .15f, .4f);
                    Spawn(distant, "Малый столб рядом", pillars[(i * 5 + 2) % pillars.Length], towerRockB,
                        new Vector3(x + Range(random, -90f, 90f), ground - small * .08f, z + Range(random, -90f, 90f)),
                        new Vector3(Range(random, -4f, 4f), Range(random, 0f, 360f), Range(random, -4f, 4f)),
                        new Vector3(smallWidth, small, smallWidth), false);
                }
            }
            var ridges = new Mesh[4];
            for (int i = 0; i < ridges.Length; i++)
            {
                float seed = 120f + i * 173.7f;
                ridges[i] = Add("DistantMesa-" + i + ".asset", DesertArchesMesh.Butte(1f, 1f, i % 2 == 0 ? 16 : 14, seed));
            }
            for (int ring = 0; ring < 3; ring++)
            {
                int count = ring == 0 ? 15 : ring == 1 ? 13 : 10;
                for (int i = 0; i < count; i++)
                {
                    float angle = i / (float)count * 360f + Range(random, -8f, 8f) + ring * 13f;
                    float radius = ring == 0 ? Range(random, 900f, 1120f) : ring == 1 ? Range(random, 1180f, 1400f) : Range(random, 1450f, 1640f);
                    float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                    float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                    float baseY = DesertArchesMesh.Surface(x, z) - 90f;
                    float height = ring == 0 ? Range(random, 230f, 350f) : ring == 1 ? Range(random, 290f, 460f) : Range(random, 350f, 540f);
                    float width = height * Range(random, .75f, 1.25f);
                    int variantIndex = (i + ring * 2) % ridges.Length;
                    Spawn(distant, "Столовая гора", ridges[variantIndex], ring == 2 ? towerRockC : towerRock,
                        new Vector3(x, baseY, z), new Vector3(0f, Range(random, 0f, 360f), 0f),
                        new Vector3(width, height, width * Range(random, .8f, 1.2f)), false);
                }
            }
        }

        private static void GoatTrail(Transform parent)
        {
            var route = new List<Vector3>();
            const int wash = 6;
            for (int i = 0; i <= wash; i++)
            {
                float t = i / (float)wash;
                float z = Mathf.Lerp(60f, -140f, t);
                route.Add(new Vector3(DesertArchesMesh.ChannelX(z) + Mathf.Lerp(52f, 36f, t), 0f, z));
            }
            for (int i = 1; i <= 9; i++)
            {
                float t = i / 9f;
                float x = Mathf.Lerp(route[route.Count - 1].x, 640f, t);
                route.Add(new Vector3(x, 0f, DesertArchesMesh.ChannelZ(x) + 6f));
            }
            float bankX = 660f;
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f;
                float x = Mathf.Lerp(bankX, 700f, t);
                route.Add(new Vector3(x, 0f, DesertArchesMesh.ChannelZ(x) + Mathf.Lerp(22f, 275f, Mathf.Pow(t, 1.1f))));
            }
            Vector3 windowB = wallCentre + wallAlong * -212f;
            Vector3 behindB = windowB + wallAcross * -170f;
            Vector3 from = route[route.Count - 1];
            for (int i = 1; i <= 4; i++)
            {
                Vector3 point = Vector3.Lerp(from, behindB, i / 4f);
                route.Add(new Vector3(point.x, 0f, point.z));
            }
            for (int i = 1; i <= 3; i++)
            {
                Vector3 point = windowB + wallAcross * Mathf.Lerp(-110f, 120f, i / 3f);
                route.Add(new Vector3(point.x, 0f, point.z));
            }

            var material = Standard("GoatTrailSand", new Color(.92f, .72f, .5f), .02f);
            Transform trail = GoatTrailBuilder.Build(parent, "Козья тропа", Trail, material, route, 4.2f, .35f, 9f, true, new Color(.72f, .55f, .38f));
            for (int marker = 0; marker < 5; marker++)
            {
                Vector3 point = route[Mathf.RoundToInt(Mathf.Lerp(6f, route.Count - 3f, marker / 4f))];
                Vector3 offset = Quaternion.Euler(0f, marker * 73f + 21f, 0f) * new Vector3(13f, 0f, 0f);
                var landmark = new GameObject("Камень-ориентир " + (marker + 1));
                landmark.transform.SetParent(trail, false);
                landmark.transform.position = new Vector3(point.x + offset.x, Trail(point + offset), point.z + offset.z);
                landmark.transform.rotation = Quaternion.Euler(Range(random, -8f, 8f), Range(random, 0f, 360f), Range(random, -8f, 8f));
                landmark.transform.localScale = new Vector3(Range(random, 7f, 12f), Range(random, 5f, 8f), Range(random, 7f, 12f));
                landmark.isStatic = true;
                landmark.AddComponent<MeshFilter>().sharedMesh = shardMesh;
                landmark.AddComponent<MeshRenderer>().sharedMaterial = sandstone;
            }
            for (int step = 0; step < route.Count; step += 4)
            {
                Vector3 point = route[step];
                Vector3 heading = route[Mathf.Min(step + 1, route.Count - 1)] - point;
                heading.y = 0f;
                if (heading.sqrMagnitude < .001f) continue;
                heading.Normalize();
                Vector3 side = new Vector3(-heading.z, 0f, heading.x);
                float flip = step % 8 == 0 ? 1f : -1f;
                for (int jump = 0; jump < 3; jump++)
                {
                    Vector3 spot = point + heading * (jump * 7f - 4f) + side * flip * (7f + jump * 2.6f);
                    float size = Range(random, 2.2f, 3.2f) + jump * 1.5f;
                    var stone = new GameObject("Валун-ступень " + (step + 1) + "-" + (jump + 1));
                    stone.transform.SetParent(trail, false);
                    stone.transform.position = new Vector3(spot.x, Trail(spot) - size * .14f, spot.z);
                    stone.transform.rotation = Quaternion.Euler(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f));
                    stone.transform.localScale = new Vector3(size * Range(random, .9f, 1.3f), size * Range(random, .55f, .8f), size * Range(random, .9f, 1.3f));
                    stone.isStatic = true;
                    stone.AddComponent<MeshFilter>().sharedMesh = shardMesh;
                    stone.AddComponent<MeshRenderer>().sharedMaterial = jump == 2 ? sunlit : sandstone;
                    stone.AddComponent<MeshCollider>().sharedMesh = shardMesh;
                }
            }
            for (int shelf = 0; shelf < 5; shelf++)
            {
                float t = shelf / 4f;
                float x = Mathf.Lerp(636f, 706f, t);
                float z = DesertArchesMesh.ChannelZ(x) + Mathf.Lerp(46f, 168f, Mathf.Pow(t, 1.1f));
                float baseY = DesertArchesMesh.Surface(x, z);
                float top = baseY + 9f + t * 24f;
                Mesh ledge = Add("TrailLedge-" + shelf + ".asset",
                    DesertArchesMesh.Terrace(19f + t * 9f, 15f + t * 7f, top, baseY - 28f, 3, 311 + shelf * 7));
                Spawn(terraces, "Уступ тропы " + (shelf + 1), ledge, shelf % 2 == 0 ? sunlit : sandstone,
                    new Vector3(x, 0f, z), new Vector3(0f, Range(random, 0f, 360f), 0f), Vector3.one, true);
                heroAreas.Add(new Vector4(x, top, z, 42f));
            }
            float length = 0f;
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 a = route[i - 1], b = route[i];
                a.y = Trail(a);
                b.y = Trail(b);
                length += Vector3.Distance(a, b);
            }
            Debug.Log("GOAT_TRAIL DesertArches waypoints=" + route.Count + " length~" + Mathf.RoundToInt(length) + "м");
        }

        private static float Trail(Vector3 point)
        {
            return DesertArchesMesh.Surface(point.x, point.z);
        }

        private static Vector3 Ring(Vector3 centre, float angle, float radius)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(centre.x + Mathf.Cos(rad) * radius, 0f, centre.z + Mathf.Sin(rad) * radius);
        }

        private static void RouteGuide(Transform parent)
        {
            var guide = new GameObject("Ориентиры маршрута — игровая настройка отложена").transform;
            guide.SetParent(parent, false);
            if (sites.Count > 0)
            {
                var crown = new GameObject("Ветвь маршрута по своду главной арки").transform;
                crown.SetParent(guide, false);
                Site grand = sites[0];
                for (int step = 0; step <= 14; step++)
                {
                    float t = step / 14f;
                    var point = new GameObject("Свод " + step).transform;
                    point.SetParent(crown, false);
                    point.position = grand.summit + Vector3.up * (grand.top + Mathf.Sin(t * Mathf.PI) * grand.height * .96f)
                        + Quaternion.Euler(0f, grand.yaw, 0f) * new Vector3(Mathf.Lerp(-grand.span * .46f, grand.span * .46f, t), 0f, 0f);
                }
            }
            Vector3 start = sites.Count > 0 ? sites[0].summit + Vector3.up * sites[0].top : heroEyes;
            for (int step = 0; step <= 12; step++)
            {
                float t = step / 12f;
                Vector3 path = Vector3.Lerp(start, heroEyes, t);
                var point = new GameObject(step == 0 ? "Старт у главной арки" : step == 12 ? "Смотровая терраса" : "Терраса " + step).transform;
                point.SetParent(guide, false);
                point.position = new Vector3(path.x, DesertArchesMesh.Surface(path.x, path.z) + 2f, path.z);
            }
        }

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = .92f;
            RenderSettings.ambientSkyColor = new Color(.38f, .45f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.60f, .44f, .30f);
            RenderSettings.ambientGroundColor = new Color(.55f, .40f, .28f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.84f, .66f, .50f);
            RenderSettings.fogDensity = .00019f;
            var sun = new GameObject("Полуденное пустынное солнце").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .93f, .78f);
            sun.intensity = 2.0f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .78f;
            sun.transform.rotation = Quaternion.Euler(45f, 103f, 0f);
            RenderSettings.sun = sun;
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.32f, .62f, .95f));
            sky.SetColor("_SkyHorizon", new Color(.88f, .85f, .79f));
            sky.SetColor("_GroundColor", new Color(.80f, .74f, .66f));
            sky.SetColor("_SunColor", new Color(1f, .96f, .87f));
            sky.SetColor("_CloudColor", new Color(1f, .99f, .96f));
            sky.SetFloat("_SunSize", .06f);
            sky.SetFloat("_CloudDensity", .25f);
            sky.SetVector("_SunDir", -sun.transform.forward);
            RenderSettings.skybox = Save(sky, "DesertSky.mat");
        }

        private static bool Hero(float x, float y, float z)
        {
            for (int i = 0; i < heroAreas.Count; i++)
            {
                Vector4 area = heroAreas[i];
                if (Mathf.Abs(y - area.y) > 80f) continue;
                float dx = x - area.x;
                float dz = z - area.z;
                if (dx * dx + dz * dz < area.w * area.w) return true;
            }
            return false;
        }

        private static Vector3 Safe(Vector3 position)
        {
            return new Vector3(position.x, Mathf.Max(position.y, DesertArchesMesh.Surface(position.x, position.z) + 14f), position.z);
        }

        private static Transform Spawn(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 euler, Vector3 scale, bool collider)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localRotation = Quaternion.Euler(euler);
            item.transform.localScale = scale;
            item.isStatic = true;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item.transform;
        }

        private static Material RockMaterial(string name, Color tint, float layers, float tiling, float swirl, Vector3 offset, float shear, float jitter)
        {
            var material = new Material(Shader.Find("GoatDescent/DesertSandstone")) { name = name };
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", tint);
            material.SetColor("_SandTint", new Color(1.00f, .76f, .53f));
            material.SetColor("_SeamTint", new Color(.46f, .24f, .15f));
            material.SetColor("_ShadeTint", new Color(.55f, .36f, .29f));
            material.SetColor("_MidTint", new Color(1.00f, .64f, .45f));
            material.SetColor("_PlayaTint", new Color(1.00f, .89f, .76f));
            material.SetColor("_VarnishTint", new Color(.28f, .17f, .11f));
            material.SetColor("_HazeTint", new Color(.84f, .64f, .48f));
            material.SetFloat("_Tiling", tiling);
            material.SetFloat("_Layers", layers);
            material.SetFloat("_StrataScale", .072f);
            material.SetFloat("_Dust", name == "Sand" ? 1f : .52f);
            material.SetFloat("_Mid", .6f);
            material.SetFloat("_Shear", shear);
            material.SetFloat("_Jitter", jitter);
            material.SetFloat("_HazeStart", 600f);
            material.SetFloat("_HazeEnd", 2200f);
            material.SetFloat("_HazeStrength", .38f);
            material.SetFloat("_HazeDesat", .22f);
            material.SetFloat("_Varnish", .26f);
            material.SetFloat("_Contact", .42f);
            material.SetFloat("_DepthFloor", -180f);
            material.SetFloat("_DepthSpan", 205f);
            material.SetFloat("_DepthShade", .34f);
            material.SetFloat("_PlayaY", -100f);
            material.SetFloat("_PlayaSpan", 36f);
            material.SetFloat("_Playa", name == "Sand" ? .35f : .6f);
            material.SetVector("_Swirl", new Vector4(swirl, offset.x, offset.y, offset.z));
            return Save(material, name + ".mat");
        }

        private static Material HazeMaterial()
        {
            var shader = Shader.Find("GoatDescent/DesertHaze");
            if (!shader) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "DesertHaze" };
            if (material.HasProperty("_HazeColor")) material.SetColor("_HazeColor", new Color(.96f, .89f, .78f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(1f, 1f, 1f, .6f));
            if (material.HasProperty("_Strength")) material.SetFloat("_Strength", .5f);
            return Save(material, "DesertHaze.mat");
        }

        private static Material Standard(string name, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", smoothness);
            return Save(material, name + ".mat");
        }

        private static Mesh Add(string name, Mesh mesh)
        {
            return Save(mesh, name);
        }

        private static void PruneLegacy()
        {
            string[] exact = { "Canyon", "HorizonRidges", "HorizonPlateau" };
            string[] prefix = { "DesertTower-", "SnowPeak", "DistantRange", "GrandArch", "FarArch", "DistantMesa" };
            foreach (string guid in AssetDatabase.FindAssets("t:Object", new[] { AssetsPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                bool drop = name.StartsWith("Rock-") && name.Length <= 7;
                for (int i = 0; i < exact.Length; i++) if (name == exact[i]) drop = true;
                for (int i = 0; i < prefix.Length; i++) if (name.StartsWith(prefix[i])) drop = true;
                if (drop) AssetDatabase.DeleteAsset(path);
            }
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

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
