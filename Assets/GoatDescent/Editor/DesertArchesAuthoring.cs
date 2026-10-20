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
        private const int TerrainGrid = 360;
        private const int PlinthLevels = 8;
        private const float WorldScale = 5f;
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
        private static Mesh[] boulders;
        private static Vector3 scalePivot;
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
        private static List<Vector3> trail;
        private static float[] trailDistances;
        private static float trailLength;
        private static int jumpSeries;
        private static int checkpointCount;

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
            sand = RockMaterial("Sand", new Color(.86f, .68f, .50f), 0f, .14f, .7f, new Vector3(5f, 63f, -19f), 0f, 3.5f);
            towerRock = RockMaterial("DistantTower", new Color(1.06f, .58f, .32f), .2f, .12f, 0f, new Vector3(0f, 0f, 0f), .045f, 8f);
            towerRockB = RockMaterial("DistantTowerB", new Color(1.00f, .54f, .30f), .24f, .165f, 1.15f, new Vector3(37f, 11f, 5f), -.055f, 7.2f);
            towerRockC = RockMaterial("DistantTowerC", new Color(1.10f, .62f, .36f), .17f, .093f, 2.35f, new Vector3(-23f, -17f, 41f), .07f, 8.6f);
            wood = Standard("DryWood", new Color(.27f, .19f, .12f), .04f);
            scrub = Standard("DryScrub", new Color(.46f, .42f, .25f), .02f);
            grass = Standard("DryGrass", new Color(.70f, .60f, .36f), .02f);
            haze = HazeMaterial();
            shardMesh = Save(DesertArchesMesh.Fragment(), "SandstoneShard.asset");
            collapse = DesertArchesMesh.Collector();
            trees = new Mesh[5];
            for (int i = 0; i < trees.Length; i++) trees[i] = Add("DryTree-" + i + ".asset", DesertArchesMesh.Tree(1f, 1700 + i * 97));
            bushes = new Mesh[5];
            for (int i = 0; i < bushes.Length; i++) bushes[i] = Add("DryBush-" + i + ".asset", DesertArchesMesh.Bush(1f, 2100 + i * 61));
            tufts = new Mesh[4];
            for (int i = 0; i < tufts.Length; i++) tufts[i] = Add("DryGrassTuft-" + i + ".asset", DesertArchesMesh.Tuft(1f, 2600 + i * 43));
            boulders = new Mesh[6];
            for (int i = 0; i < boulders.Length; i++) boulders[i] = Add("TrailBoulder-" + i + ".asset", DesertArchesMesh.Boulder(1f, .48f + i * .05f, 4100 + i * 131));

            var root = new GameObject("Пустынные арки — статичная панорама").transform;
            rocks = new GameObject("Каньон, пласты и столбы").transform; rocks.SetParent(root, false);
            terraces = new GameObject("Террасы, осыпи и растительность").transform; terraces.SetParent(root, false);
            plants = new GameObject("Сухие деревья").transform; plants.SetParent(root, false);
            distant = new GameObject("Дальний план — башни и хребты").transform; distant.SetParent(root, false);

            Floor();
            HeroSites();
            SecondaryRidges();
            DelegationTowers();
            Spawn(rocks, "Обрушение у арок", Add("ArchCollapse.asset", DesertArchesMesh.Finish(collapse, "Обрушение у арок")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            CameraSite();
            Scatter(shardMesh);
            Horizon();
            GoatTrail(root);
            ScaleWorld(root);
            Environment();

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 52f;
            camera.farClipPlane = 3000f * WorldScale;
            heroEyes = Clear(heroEyes, heroAim);
            Vector3 overviewEye = heroEyes + new Vector3(heroEyes.x - heroAim.x, 0f, heroEyes.z - heroAim.z).normalized * 620f;
            overviewEye.y = heroEyes.y + 260f;
            camera.transform.position = ToWorld(Clear(overviewEye, heroAim));
            camera.transform.LookAt(ToWorld(heroAim));
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.warmTint = new Color(1.07f, 1.0f, .9f);
            post.coolTint = new Color(.99f, .97f, .93f);
            post.saturation = 1.24f;
            post.bloomIntensity = .42f;
            post.contrast = 1.16f;
            post.vignette = .3f;
            post.bloomThreshold = .8f;

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
            Debug.Log("ARCH_CAM dist=" + Mathf.RoundToInt(distance * WorldScale) + " hit0=" + hits + " wallInFrame=" + Mathf.RoundToInt(inFrame * 100f) + "%");
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

        [MenuItem("Tools/GOAT/Статичные горы/Пустынные арки — снимки маршрута")]
        public static void RouteShots()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("ROUTE_SHOTS DesertArches missing scene " + ScenePath);
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var route = RouteWorld();
            if (route.Count < 2)
            {
                Debug.LogError("ROUTE_SHOTS DesertArches trail not found");
                return;
            }
            var cameraGo = new GameObject("route-shot-cam");
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.farClipPlane = 3000f * WorldScale;
            camera.clearFlags = CameraClearFlags.Skybox;
            var scenePost = Object.FindFirstObjectByType<GoatDescent.GoatScenePost>(FindObjectsInactive.Include);
            if (scenePost != null) cameraGo.AddComponent<GoatDescent.GoatScenePost>().CopyFrom(scenePost);
            var distances = new float[route.Count];
            for (int i = 1; i < route.Count; i++)
                distances[i] = distances[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            float length = distances[distances.Length - 1];
            const int count = 8;
            for (int shot = 0; shot < count; shot++)
            {
                float at = Mathf.Lerp(6f, Mathf.Max(6f, length - 6f), shot / (float)(count - 1));
                int index = 1;
                while (index < route.Count - 1 && distances[index] < at) index++;
                Vector3 a = route[Mathf.Max(index - 4, 0)];
                Vector3 b = route[Mathf.Min(index + 4, route.Count - 1)];
                Vector3 tangent = b - a;
                if (tangent.sqrMagnitude < .0001f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 point = route[index];
                int aimIndex = index;
                while (aimIndex < route.Count - 1 && distances[aimIndex] < at + 25f) aimIndex++;
                Vector3 aim = route[aimIndex] + Vector3.up * 6f;
                int lift;
                Vector3 eye = RouteEye(point, tangent, out lift);
                Vector3 direction = (aim - eye).normalized;
                float eyeGround = RouteGround(eye);
                float probes = RouteProbeClearance(eye, direction);
                bool clear = eye.y >= eyeGround + 6f && probes >= 2f;
                camera.transform.position = eye;
                camera.transform.LookAt(aim);
                Debug.Log("ROUTE_SHOTS DesertArches shot=" + (shot + 1) + " lift=" + lift + "м clear=" + clear
                    + " eye=" + Mathf.RoundToInt(eye.x) + "," + Mathf.RoundToInt(eye.y) + "," + Mathf.RoundToInt(eye.z)
                    + " eyeGround=" + Mathf.RoundToInt(eyeGround) + " probes=" + probes.ToString("0.0")
                    + " ground=" + Mathf.RoundToInt(DesertArchesMesh.Surface(eye.x / WorldScale, eye.z / WorldScale) * WorldScale));
                Render(camera, "route-DesertArches-" + (shot + 1).ToString("00"));
            }
            Object.DestroyImmediate(cameraGo);
            Debug.Log("ROUTE_SHOTS DesertArches count=" + count);
        }

        private static float RouteGround(Vector3 world)
        {
            return DesertArchesMesh.Surface(world.x / WorldScale, world.z / WorldScale) * WorldScale;
        }

        private static float RouteProbeClearance(Vector3 eye, Vector3 direction)
        {
            float worst = float.PositiveInfinity;
            float[] probes = { 10f, 20f, 30f };
            for (int i = 0; i < probes.Length; i++)
            {
                Vector3 point = eye + direction * probes[i];
                worst = Mathf.Min(worst, point.y - RouteGround(point));
            }
            return worst;
        }

        private static Vector3 RouteEye(Vector3 point, Vector3 tangent, out int lift)
        {
            Vector3 eye = point - tangent * 10f + Vector3.up * 12f;
            lift = 0;
            for (int step = 0; step < 24; step++)
            {
                float need = Mathf.Max(RouteGround(eye) + 6f - eye.y, 2f - RouteProbeClearance(eye, tangent));
                if (need <= 0f) break;
                float raise = Mathf.Max(2f, need + .25f);
                eye.y += raise;
                lift += Mathf.RoundToInt(raise);
            }
            return eye;
        }

        private static List<Vector3> RouteWorld()
        {
            Vector3 ground = DesertArchesMesh.Highest(430f, -40f, 120f);
            float yaw = Mathf.Atan2(Viewpoint.x - ground.x, Viewpoint.z - ground.z) * Mathf.Rad2Deg;
            wallAlong = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            wallAcross = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            sites.Clear();
            sites.Add(new Site { summit = ground + wallAlong * 150f });
            sites.Add(new Site { summit = ground + wallAlong * -280f });
            trail = BuildRoute();
            trailDistances = new float[trail.Count];
            trailLength = 0f;
            for (int i = 0; i < trail.Count; i++)
            {
                trail[i] = new Vector3(trail[i].x, Trail(trail[i]), trail[i].z);
                if (i > 0)
                    trailLength += Vector2.Distance(new Vector2(trail[i - 1].x, trail[i - 1].z), new Vector2(trail[i].x, trail[i].z));
                trailDistances[i] = trailLength;
            }
            var route = new List<Vector3>(trail.Count);
            for (int i = 0; i < trail.Count; i++) route.Add(ToWorld(trail[i]));
            return route;
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
            camera.transform.position = ToWorld(Clear(position, target));
            camera.transform.LookAt(ToWorld(target));
            Render(camera, name);
            camera.fieldOfView = previous;
        }

        private static void ScaleWorld(Transform root)
        {
            scalePivot = Vector3.zero;
            root.localPosition = Vector3.zero;
            root.localScale = Vector3.one * WorldScale;
            Material[] skins = { sandstone, sunlit, sand, towerRock, towerRockB, towerRockC };
            for (int i = 0; i < skins.Length; i++)
            {
                Material skin = skins[i];
                skin.SetFloat("_Tiling", skin.GetFloat("_Tiling") / WorldScale);
                skin.SetFloat("_StrataScale", skin.GetFloat("_StrataScale") / WorldScale);
                skin.SetFloat("_Shear", skin.GetFloat("_Shear") / WorldScale);
                skin.SetFloat("_Jitter", skin.GetFloat("_Jitter") / WorldScale);
                Vector4 swirl = skin.GetVector("_Swirl");
                swirl.y /= WorldScale; swirl.z /= WorldScale; swirl.w /= WorldScale;
                skin.SetVector("_Swirl", swirl);
                skin.SetFloat("_HazeStart", 600f * WorldScale);
                skin.SetFloat("_HazeEnd", 2200f * WorldScale);
                skin.SetFloat("_DepthFloor", skin.GetFloat("_DepthFloor") * WorldScale);
                skin.SetFloat("_DepthSpan", skin.GetFloat("_DepthSpan") * WorldScale);
                skin.SetFloat("_PlayaY", skin.GetFloat("_PlayaY") * WorldScale);
                skin.SetFloat("_PlayaSpan", skin.GetFloat("_PlayaSpan") * WorldScale);
                EditorUtility.SetDirty(skin);
            }
        }

        private static Vector3 ToWorld(Vector3 local)
        {
            return scalePivot + (local - scalePivot) * WorldScale;
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
                Add("Terrain.asset", DesertArchesMesh.Terrain(TerrainGrid, DesertArchesMesh.TerrainExtent, DesertArchesMesh.TerrainOuter, 22)),
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
            var skirts = DesertArchesMesh.Collector();
            Vector3 ground = DesertArchesMesh.Highest(430f, -40f, 120f);
            float yaw = Mathf.Atan2(Viewpoint.x - ground.x, Viewpoint.z - ground.z) * Mathf.Rad2Deg;
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 across = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float crest = ground.y + 430f;
            var decks = new List<Deck>();
            float[][] piers =
            {
                new[] { -620f, 150f, -26f },
                new[] { -80f, 110f, -6f },
                new[] { 620f, 150f, -14f }
            };
            for (int piece = 0; piece < piers.Length; piece++)
            {
                Vector3 centre = ground + along * piers[piece][0] + across * -42f;
                float top = crest + piers[piece][2];
                Material skin = piece == 1 ? sunlit : sandstone;
                Mesh slab = Add("WallPier-" + piece + ".asset", DesertArchesMesh.Slab(piers[piece][1], 108f, top, DesertArchesMesh.FloorY - 90f, 8, 6001f + piece * 7f));
                Spawn(rocks, "Устой стены " + piece, slab, skin,
                    new Vector3(centre.x, 0f, centre.z), new Vector3(0f, yaw, 0f), Vector3.one, true);
                SlabSatellites("Устой стены " + piece, centre, piers[piece][1], 108f, yaw, top, true, 3, skin, skirts);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, piers[piece][1] + 40f));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = piers[piece][1] * 1.15f, semiZ = 72f, top = top });
            }
            {
                float top = crest - 8f;
                Mesh slab = Add("WallCrest.asset", DesertArchesMesh.Slab(760f, 96f, top, 140f, 7, 6101f));
                Vector3 centre = new Vector3(ground.x - across.x * 60f, 0f, ground.z - across.z * 60f);
                Spawn(rocks, "Свод-пласт стены", slab, sandstone,
                    centre, new Vector3(0f, yaw, 0f), Vector3.one, true);
                SlabSatellites("Свод-пласт стены", centre, 760f, 96f, yaw, top, false, 3, sandstone, skirts);
                decks.Add(new Deck { centre = new Vector3(ground.x, 0f, ground.z), semiX = 750f, semiZ = 92f, top = top });
            }
            for (int crown = 0; crown < 7; crown++)
            {
                float offset = Range(random, -680f, 680f);
                float lift = Range(random, 4f, 34f);
                float sink = Range(random, 120f, 190f);
                float top = crest + lift + 6f;
                Material skin = crown % 2 == 0 ? sandstone : sunlit;
                Mesh outcrop = Add("WallCrown-" + crown + ".asset", DesertArchesMesh.Butte(1f, 1f, 20, 7103f + crown * 19f));
                Vector3 centre = ground + along * offset + across * Range(random, -28f, 28f);
                float crownYaw = yaw + Range(random, -20f, 20f);
                Spawn(rocks, "Вершина гребня " + crown, outcrop, skin,
                    new Vector3(centre.x, crest - sink, centre.z),
                    new Vector3(Range(random, -7f, 7f), crownYaw, Range(random, -7f, 7f)),
                    new Vector3(Range(random, 130f, 230f), sink + lift, Range(random, 66f, 118f)), true);
                Spawn(terraces, "Спутник вершины гребня " + crown, boulders[random.Next(boulders.Length)], skin,
                    new Vector3(centre.x + Range(random, -60f, 60f), top + Range(random, 2f, 10f), centre.z + Range(random, -34f, 34f)),
                    new Vector3(Range(random, -8f, 8f), crownYaw + Range(random, -50f, 50f), Range(random, -8f, 8f)),
                    new Vector3(Range(random, 42f, 74f), Range(random, 26f, 46f), Range(random, 34f, 62f)), false);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, Range(random, 170f, 260f)));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = 190f, semiZ = 88f, top = top });
            }
            for (int ledge = 0; ledge < 2; ledge++)
            {
                float side = ledge == 0 ? -1f : 1f;
                float offset = side * 560f;
                Vector3 centre = ground + along * offset + across * -190f;
                float top = ground.y + 40f - ledge * 14f;
                float ledgeYaw = yaw + (ledge * 4f - 2f);
                Material skin = ledge == 0 ? sunlit : sandstone;
                Mesh slab = Add("WallApron-" + ledge + ".asset", DesertArchesMesh.Slab(240f, 118f, top, DesertArchesMesh.FloorY - 90f, 7, 6301f + ledge * 11f));
                Spawn(rocks, "Терраса у подножия стены " + ledge, slab, skin,
                    new Vector3(centre.x, 0f, centre.z), new Vector3(0f, ledgeYaw, 0f), Vector3.one, true);
                SlabSatellites("Терраса у подножия стены " + ledge, centre, 240f, 118f, ledgeYaw, top, true, 3, skin, skirts);
                heroAreas.Add(new Vector4(centre.x, top, centre.z, 280f));
                decks.Add(new Deck { centre = new Vector3(centre.x, 0f, centre.z), semiX = 240f, semiZ = 155f, top = top });
            }
            Spawn(terraces, "Осыпь у подножия стены",
                Add("WallSkirt.asset", DesertArchesMesh.Finish(skirts, "Осыпь у подножия стены")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            wallCentre = ground;
            wallAlong = along;
            wallAcross = across;
            wallCrest = crest;

            Vector3 farPlace = ground + along * -280f;
            float farBase = DesertArchesMesh.Surface(farPlace.x, farPlace.z) - 6f;
            Window("Главное окно-арка", "WindowMain", ground + along * 150f, DesertArchesMesh.FloorY - DesertArchesMesh.GorgeDepth, 235f, 420f, 215f, 175f, 140f, yaw, decks, 7001);
            Window("Второе окно-арка", "WindowFar", farPlace, farBase, 170f, 260f, 108f, 132f, 92f, yaw, decks, 7003);

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

        private static void SecondaryRidges()
        {
            var shapeRandom = new System.Random(4517);
            Vector3 north = new Vector3(150f, 0f, 540f);
            float northGround = DesertArchesMesh.Surface(north.x, north.z);
            Spawn(rocks, "Второстепенный гребень — Северная завеса",
                Add("MidgroundRidgeNorth.asset", DesertArchesMesh.Mesa(215f, 88f, northGround + 250f, northGround - 70f, 37)),
                sunlit, new Vector3(north.x, 0f, north.z), new Vector3(0f, 24f, 0f), Vector3.one, true);
            heroAreas.Add(new Vector4(north.x, northGround, north.z, 230f));

            Vector3 south = new Vector3(70f, 0f, -520f);
            float southGround = DesertArchesMesh.Surface(south.x, south.z);
            Spawn(rocks, "Столовая гора — Южный бастион",
                Add("MidgroundButteSouth.asset", DesertArchesMesh.Butte(125f, 305f, 15, 6203f)),
                sandstone, new Vector3(south.x, southGround - 22f, south.z), new Vector3(0f, -38f, 0f), Vector3.one, true);
            heroAreas.Add(new Vector4(south.x, southGround, south.z, 190f));
            for (int satellite = 0; satellite < 3; satellite++)
            {
                float bearing = satellite / 3f * Mathf.PI * 2f + .4f;
                float px = south.x + Mathf.Cos(bearing) * Range(shapeRandom, 130f, 190f);
                float pz = south.z + Mathf.Sin(bearing) * Range(shapeRandom, 130f, 190f);
                float size = Range(shapeRandom, 24f, 46f);
                Spawn(terraces, "Осыпь Южного бастиона", boulders[shapeRandom.Next(boulders.Length)], sandstone,
                    new Vector3(px, DesertArchesMesh.Surface(px, pz) + size * .1f, pz),
                    new Vector3(Range(shapeRandom, -10f, 10f), Range(shapeRandom, 0f, 360f), Range(shapeRandom, -10f, 10f)),
                    new Vector3(size, size * Range(shapeRandom, .45f, .8f), size * Range(shapeRandom, .7f, 1.1f)), false);
            }

            Vector3 east = new Vector3(770f, 0f, 230f);
            float eastGround = DesertArchesMesh.Surface(east.x, east.z);
            Spawn(rocks, "Острог — Восточный клин",
                Add("MidgroundRidgeEast.asset", DesertArchesMesh.Mesa(118f, 190f, eastGround + 320f, eastGround - 60f, 53)),
                towerRockC, new Vector3(east.x, 0f, east.z), new Vector3(0f, 64f, 0f), Vector3.one, true);
            heroAreas.Add(new Vector4(east.x, eastGround, east.z, 220f));
            for (int spire = 0; spire < 2; spire++)
            {
                float px = east.x + (spire == 0 ? -96f : 88f);
                float pz = east.z + (spire == 0 ? 54f : -62f);
                float height = spire == 0 ? 168f : 214f;
                Spawn(rocks, "Шпиль Восточного клина " + (spire + 1),
                    Add("MidgroundSpireEast-" + spire + ".asset", DesertArchesMesh.Butte(34f, height, 11, 6607f + spire * 19f)),
                    spire == 0 ? towerRockB : towerRock, new Vector3(px, DesertArchesMesh.Surface(px, pz) - 18f, pz),
                    new Vector3(0f, spire * 41f, 0f), Vector3.one, true);
            }
        }

        private static void DelegationTowers()
        {
            Vector3 along = new Vector3(wallAlong.x, 0f, wallAlong.z).normalized;
            Vector3 across = new Vector3(wallAcross.x, 0f, wallAcross.z).normalized;
            Vector3 far = new Vector3(sites[1].summit.x, 0f, sites[1].summit.z);
            Vector3 centre = far - across * 132f - along * 4f;
            Vector3[] spots =
            {
                centre - along * 34f,
                centre + along * 6f,
                centre + along * 38f
            };
            float[] radii = { 11f, 13f, 9f };
            float[] heights = { 172f, 238f, 148f };
            Material[] skins = { sandstone, sunlit, towerRockB };
            for (int tower = 0; tower < spots.Length; tower++)
            {
                float ground = DesertArchesMesh.Surface(spots[tower].x, spots[tower].z);
                Mesh mesh = Add("DelegationTower-" + tower + ".asset",
                    DesertArchesMesh.Tower(radii[tower], heights[tower], 12, 7, 7301f + tower * 17f));
                Spawn(rocks, "Башня делегации " + (tower + 1), mesh, skins[tower],
                    new Vector3(spots[tower].x, ground - 8f, spots[tower].z),
                    new Vector3(0f, 23f + tower * 37f, 0f), Vector3.one, true);
                heroAreas.Add(new Vector4(spots[tower].x, ground, spots[tower].z, radii[tower] * 3.4f));
                for (int chip = 0; chip < 4; chip++)
                {
                    float bearing = Range(random, 0f, Mathf.PI * 2f);
                    float radius = radii[tower] * Range(random, 1.2f, 2.1f);
                    float px = spots[tower].x + Mathf.Cos(bearing) * radius;
                    float pz = spots[tower].z + Mathf.Sin(bearing) * radius;
                    float size = radii[tower] * Range(random, .16f, .34f);
                    Spawn(terraces, "Осыпь башни делегации", boulders[random.Next(boulders.Length)], skins[tower],
                        new Vector3(px, DesertArchesMesh.Surface(px, pz) + size * .15f, pz),
                        new Vector3(Range(random, -10f, 10f), Range(random, 0f, 360f), Range(random, -10f, 10f)),
                        new Vector3(size, size * Range(random, .5f, .8f), size * Range(random, .7f, 1f)), false);
                }
            }
        }

        private static void SlabSatellites(string name, Vector3 centre, float halfLength, float halfThickness,
            float yaw, float top, bool grounded, int count, Material material, CrownRockMesh skirt)
        {
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 across = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            for (int item = 0; item < count; item++)
            {
                float run = Range(random, -.78f, .78f);
                float side = random.NextDouble() < .5 ? -1f : 1f;
                Vector3 spot = centre + along * (halfLength * run)
                    + across * (side * halfThickness * Range(random, .52f, .82f));
                float size = halfThickness * Range(random, .26f, .46f);
                float surface = DesertArchesMesh.Surface(spot.x, spot.z);
                float y = grounded ? surface + size * Range(random, .16f, .34f) : top + size * Range(random, .12f, .3f);
                Spawn(terraces, name + " — спутник", boulders[random.Next(boulders.Length)], material,
                    new Vector3(spot.x, y, spot.z),
                    new Vector3(Range(random, -8f, 8f), yaw + Range(random, -45f, 45f), Range(random, -8f, 8f)),
                    new Vector3(size * Range(random, .85f, 1.15f), size * Range(random, .6f, .95f), size * Range(random, .72f, 1f)),
                    grounded);
                if (!grounded) continue;
                for (int chip = 0; chip < 6; chip++)
                {
                    float angle = Range(random, 0f, Mathf.PI * 2f);
                    float radius = size * Range(random, .55f, 1.25f);
                    float px = spot.x + Mathf.Cos(angle) * radius;
                    float pz = spot.z + Mathf.Sin(angle) * radius;
                    Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, size * Range(random, .12f, .3f), random);
                }
            }
            if (!grounded) return;
            for (int chip = 0; chip < 22; chip++)
            {
                float run = Range(random, -.92f, .92f);
                float side = random.NextDouble() < .5 ? -1f : 1f;
                float px = centre.x + along.x * halfLength * run + across.x * side * halfThickness * Range(random, .85f, 1.15f);
                float pz = centre.z + along.z * halfLength * run + across.z * side * halfThickness * Range(random, .85f, 1.15f);
                Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, halfThickness * Range(random, .04f, .1f), random);
            }
        }

        private static void LedgeSatellites(string name, Vector3 centre, float halfWidth, float halfDepth,
            float yaw, Vector3 clear, int count, Material material, CrownRockMesh skirt)
        {
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 across = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float size = Mathf.Min(halfWidth, halfDepth);
            for (int item = 0; item < count; item++)
            {
                float angle = Range(random, 0f, Mathf.PI * 2f);
                if (clear.sqrMagnitude > .001f)
                {
                    Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    if (Vector3.Dot(radial, clear) > .35f) angle += Mathf.PI;
                }
                float reach = Range(random, .68f, .95f);
                Vector3 spot = centre + along * (Mathf.Cos(angle) * halfWidth * reach)
                    + across * (Mathf.Sin(angle) * halfDepth * reach);
                float stone = size * Range(random, .22f, .4f);
                float surface = DesertArchesMesh.Surface(spot.x, spot.z);
                Spawn(terraces, name + " — камень-спутник", boulders[random.Next(boulders.Length)], material,
                    new Vector3(spot.x, surface + stone * Range(random, .16f, .34f), spot.z),
                    new Vector3(Range(random, -8f, 8f), yaw + Range(random, -60f, 60f), Range(random, -8f, 8f)),
                    new Vector3(stone * Range(random, .85f, 1.15f), stone * Range(random, .55f, .85f), stone * Range(random, .75f, 1.05f)),
                    true);
                for (int chip = 0; chip < 6; chip++)
                {
                    float bearing = Range(random, 0f, Mathf.PI * 2f);
                    float radius = stone * Range(random, .6f, 1.2f);
                    float px = spot.x + Mathf.Cos(bearing) * radius;
                    float pz = spot.z + Mathf.Sin(bearing) * radius;
                    Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, stone * Range(random, .14f, .34f), random);
                }
            }
            for (int chip = 0; chip < 18; chip++)
            {
                float angle = Range(random, 0f, Mathf.PI * 2f);
                float reach = Range(random, .92f, 1.14f);
                float px = centre.x + Mathf.Cos(angle) * halfWidth * reach;
                float pz = centre.z + Mathf.Sin(angle) * halfDepth * reach;
                Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, size * Range(random, .04f, .1f), random);
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
            var skirts = DesertArchesMesh.Collector();
            Site grand = sites[0];
            Vector3 towardWall = grand.summit - summit;
            towardWall.y = 0f;
            towardWall = towardWall.sqrMagnitude > .001f ? towardWall.normalized : Vector3.forward;
            heroEyes = new Vector3(summit.x + towardWall.x * 44f, top + 14f, summit.z + towardWall.z * 44f);
            heroAim = new Vector3(grand.summit.x, (grand.top + wallCrest) * .5f, grand.summit.z);
            Mesh ledge = Add("ViewLedge.asset", DesertArchesMesh.Terrace(150f, 128f, top, DesertArchesMesh.FloorY - 90f, 7, 91));
            Spawn(terraces, "Смотровая терраса на кромке каньона", ledge, sunlit, ledgeCentre, Vector3.zero, Vector3.one, true);
            LedgeSatellites("Смотровая терраса", ledgeCentre, 150f, 128f, 0f, towardWall, 2, sunlit, skirts);
            heroAreas.Add(new Vector4(summit.x, top, summit.z, 158f));
            Mesh bench = Add("ViewLedge-Lower.asset", DesertArchesMesh.Terrace(206f, 176f, top - 40f, DesertArchesMesh.FloorY - 90f, 6, 97));
            Spawn(terraces, "Нижняя смотровая терраса", bench, sandstone, ledgeCentre, Vector3.zero, Vector3.one, true);
            LedgeSatellites("Нижняя смотровая терраса", ledgeCentre, 206f, 176f, 0f, towardWall, 2, sandstone, skirts);
            Vector3 vantage = wallCentre + wallAcross * 300f + wallAlong * 620f;
            float vantageTop = wallCrest + 60f;
            Mesh mesa = Add("ViewMesa.asset", DesertArchesMesh.Terrace(46f, 42f, vantageTop, DesertArchesMesh.FloorY - 90f, 8, 811));
            Spawn(terraces, "Смотровой утёс на дальнем борту", mesa, sandstone, new Vector3(vantage.x, 0f, vantage.z), Vector3.zero, Vector3.one, true);
            LedgeSatellites("Смотровой утёс", vantage, 46f, 42f, 0f, Vector3.zero, 1, sandstone, skirts);
            Spawn(terraces, "Осыпь у смотровых террас",
                Add("ViewLedgeSkirt.asset", DesertArchesMesh.Finish(skirts, "Осыпь у смотровых террас")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
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
            float gridCos = Mathf.Cos(.41f);
            float gridSin = Mathf.Sin(.41f);
            for (float gx = -834f; gx <= 834f; gx += spacing)
                for (float gz = -834f; gz <= 834f; gz += spacing)
                {
                    float x = gx * gridCos - gz * gridSin;
                    float z = gx * gridSin + gz * gridCos;
                    if (Mathf.Abs(x) > 834f || Mathf.Abs(z) > 834f) continue;
                    float px = x + Range(random, -11.4f, 11.4f);
                    float pz = z + Range(random, -11.4f, 11.4f);
                    float py = DesertArchesMesh.Surface(px, pz);
                    if (Hero(px, py, pz)) continue;
                    float focus = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(280f, 1080f, new Vector2(px + 70f, pz - 150f).magnitude));
                    float bank = DesertArchesMesh.ChannelDistance(px, pz);
                    float rim = 1f - Mathf.Abs(Mathf.InverseLerp(150f, 400f, bank) * 2f - 1f);
                    float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.32f, .64f, Mathf.PerlinNoise(px * .0032f + 53f, pz * .0029f + 71f)));
                    float talus = Mathf.PerlinNoise(px * .0068f + 11f, pz * .0061f + 97f);
                    float cover = (.2f + .8f * focus) * (.3f + 1.2f * rim) * (.12f + .88f * patch);
                    float slope = DesertArchesMesh.Slope(px, pz);
                    if (py < DesertArchesMesh.FloorY + 60f)
                    {
                        int debris = talus > .6f ? 3 : talus > .38f ? 1 : 0;
                        for (int pebble = 0; pebble < debris; pebble++) Chip(rubble, px, py, pz, Range(random, 2.4f, 7.5f), random);
                        if (random.NextDouble() < .1f) Scrub(scrubField, px, py, pz, Range(random, 2.4f, 6.6f), random);
                        continue;
                    }
                    int stones = talus > .62f ? 1 + random.Next(3) : (talus > .34f && random.NextDouble() < .5 ? 1 : 0);
                    for (int stone = 0; stone < stones; stone++)
                        Chip(random.NextDouble() < .5 ? rubble : bright, px, py, pz, Range(random, 1.8f, 3.2f + Mathf.Min(slope, 1.6f) * 2.6f), random);
                    if (slope < .68f && random.NextDouble() < .3f * cover)
                        Scrub(scrubField, px, py, pz, Range(random, 3f, 9f), random);
                    if (slope < .86f && random.NextDouble() < .26f * cover)
                        Tuft(grassField, px, py, pz, Range(random, 2.4f, 6.6f), random);
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
                rock.transform.rotation = Quaternion.Euler(Range(random, -8f, 8f), Range(random, 0f, 360f), Range(random, -8f, 8f));
                rock.transform.localScale = new Vector3(size * Range(random, .9f, 1.15f), size * Range(random, .62f, .88f), size * Range(random, .72f, 1f));
                rock.isStatic = true;
                rock.AddComponent<MeshFilter>().sharedMesh = boulders[giant % boulders.Length];
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
                    float top = wallCrest + Range(random, -1.5f, 3.5f);
                    if (random.NextDouble() < .62) Scrub(scrubField, spot.x, top, spot.z, Range(random, .8f, 3.4f), random);
                    else Tuft(grassField, spot.x, top, spot.z, Range(random, .7f, 2.4f), random);
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
            trail = BuildRoute();
            trailDistances = new float[trail.Count];
            trailLength = 0f;
            for (int i = 0; i < trail.Count; i++)
            {
                trail[i] = new Vector3(trail[i].x, Trail(trail[i]), trail[i].z);
                if (i > 0)
                    trailLength += Vector2.Distance(new Vector2(trail[i - 1].x, trail[i - 1].z), new Vector2(trail[i].x, trail[i].z));
                trailDistances[i] = trailLength;
            }

            var material = Standard("GoatTrailSand", new Color(.55f, .38f, .24f), .02f);
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg");
            material.mainTextureScale = new Vector2(1.15f, 1.3f);
            EditorUtility.SetDirty(material);
            GoatTrailBuilder.Build(parent, "Козья тропа", Trail, material, trail, 8f, .3f, 0f, false);
            TrailApron(parent, material);
            TrailStones();
            var trailSkirts = DesertArchesMesh.Collector();
            JumpSpots(trailSkirts);
            CheckpointMarkers(parent, trailSkirts);
            FinishSite(parent, trailSkirts);
            ScreeApron(trailSkirts);
            Spawn(terraces, "Осыпь у ступеней тропы",
                Add("TrailSkirt.asset", DesertArchesMesh.Finish(trailSkirts, "Осыпь у ступеней тропы")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            Debug.Log("GOAT_TRAIL DesertArches waypoints=" + trail.Count + " length~" + Mathf.RoundToInt(trailLength * WorldScale)
                + "м jumps=" + jumpSeries + " checkpoints=" + checkpointCount);
        }

        private static float ApronHalf(float t)
        {
            return 3.25f
                + (Mathf.PerlinNoise(t * 7.3f + 7.3f, 3.7f) - .5f) * 1f
                + (Mathf.PerlinNoise(t * 33f + 1.9f, 8.1f) - .5f) * .5f;
        }

        private static void TrailApron(Transform parent, Material material)
        {
            if (trail == null || trail.Count < 2) return;
            int count = trail.Count;
            const int lanes = 6;
            int stride = lanes * 2 + 1;
            var vertices = new List<Vector3>(count * stride);
            var uvs = new List<Vector2>(count * stride);
            var triangles = new List<int>((count - 1) * lanes * 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = trail[i];
                Vector3 a = trail[Mathf.Max(i - 1, 0)];
                Vector3 b = trail[Mathf.Min(i + 1, count - 1)];
                Vector3 forward = b - a;
                forward.y = 0f;
                if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                float t = i / (float)(count - 1);
                float half = ApronHalf(t);
                for (int lane = -lanes; lane <= lanes; lane++)
                {
                    float k = lane / (float)lanes;
                    float edge = k * k;
                    float wobble = (Mathf.PerlinNoise(t * 41f + 17f, k * 1.7f + 3.3f) - .5f) * 1.15f * edge
                        + (Mathf.PerlinNoise(t * 97f + 29f, k * 5.3f + 7.1f) - .5f) * .42f * edge;
                    Vector3 p = point + side * (half * k + wobble);
                    float crown = .34f - .62f * edge;
                    float ripple = Mathf.Sin(t * trailLength * .42f + k * 5.1f) * .1f
                        + (Mathf.PerlinNoise(p.x * .29f + 3f, p.z * .27f + 5f) - .5f) * .3f
                        + (Mathf.PerlinNoise(p.x * .91f + 9f, p.z * .86f + 2f) - .5f) * .12f;
                    p.y = DesertArchesMesh.Surface(p.x, p.z) + crown + ripple;
                    vertices.Add(p);
                    float uvWarpU = (Mathf.PerlinNoise(p.x * .037f + 11f, p.z * .034f + 7f) - .5f) * .18f;
                    float uvWarpV = (Mathf.PerlinNoise(p.x * .029f + 31f, p.z * .033f + 17f) - .5f) * .5f
                        + (Mathf.PerlinNoise(p.x * .11f + 3f, p.z * .1f + 23f) - .5f) * .2f;
                    uvs.Add(new Vector2((k + 1f) * .5f + uvWarpU, t * trailLength * .12f + uvWarpV));
                }
            }
            for (int i = 0; i < count - 1; i++)
                for (int lane = 0; lane < stride - 1; lane++)
                {
                    int a = i * stride + lane;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            var mesh = new Mesh { name = "TrailApronMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Spawn(parent, "Полотно тропы — широкое плечо", mesh, material, Vector3.zero, Vector3.zero, Vector3.one, false);
        }

        private static void TrailStones()
        {
            var dark = DesertArchesMesh.Collector();
            var light = DesertArchesMesh.Collector();
            var random = new System.Random(9181);
            float at = 4f;
            int placed = 0;
            while (at < trailLength - 4f)
            {
                Vector3 point = TrailSample(at, out Vector3 tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float half = ApronHalf(at / Mathf.Max(1f, trailLength));
                int cluster = random.NextDouble() < .42 ? 2 : 1;
                for (int stone = 0; stone < cluster; stone++)
                {
                    float offset = (half + Range(random, .35f, 1.7f)) * (random.NextDouble() < .5 ? -1f : 1f);
                    Vector3 spot = point + side * offset + tangent * Range(random, -1.8f, 1.8f);
                    float size = Range(random, .42f, 1.25f);
                    CrownRockMesh target = random.NextDouble() < .5 ? dark : light;
                    DesertArchesMesh.Stamp(target, boulders[random.Next(boulders.Length)], Matrix4x4.TRS(
                        new Vector3(spot.x, DesertArchesMesh.Surface(spot.x, spot.z) + size * .2f, spot.z),
                        Quaternion.Euler(Range(random, -11f, 11f), Range(random, 0f, 360f), Range(random, -11f, 11f)),
                        new Vector3(size * Range(random, .9f, 1.3f), size * Range(random, .5f, .85f), size * Range(random, .8f, 1.1f))));
                    placed++;
                    if (placed % 3 != 0) continue;
                    float chipSize = Range(random, .12f, .3f);
                    Chip(target, spot.x + Range(random, -1.6f, 1.6f), DesertArchesMesh.Surface(spot.x, spot.z),
                        spot.z + Range(random, -1.6f, 1.6f), chipSize, random);
                }
                at += Range(random, 10f, 27f) * (random.NextDouble() < .16 ? 1.7f : 1f);
            }
            Spawn(terraces, "Камни по краю тропы",
                Add("TrailEdgeStones.asset", DesertArchesMesh.Finish(dark, "Камни по краю тропы")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
            Spawn(terraces, "Светлые камни по краю тропы",
                Add("TrailEdgeStonesSunlit.asset", DesertArchesMesh.Finish(light, "Светлые камни по краю тропы")),
                sunlit, Vector3.zero, Vector3.zero, Vector3.one, false);
        }

        private static List<Vector3> BuildRoute()
        {
            Vector3 along = new Vector3(wallAlong.x, 0f, wallAlong.z).normalized;
            Vector3 across = new Vector3(wallAcross.x, 0f, wallAcross.z).normalized;
            Vector3 main = new Vector3(sites[0].summit.x, 0f, sites[0].summit.z);
            Vector3 far = new Vector3(sites[1].summit.x, 0f, sites[1].summit.z);
            Vector3 mainEntry = main - across * 70f;
            Vector3 mainExit = main + across * 70f;
            Vector3 farEntry = far + across * 66f;
            Vector3 farExit = far - across * 66f;

            var control = new List<Vector3>();
            control.Add(mainEntry + along * 30f + across * 8f);
            control.Add(mainEntry + along * 10f - across * 8f);
            control.Add(mainEntry);
            control.Add(main);
            control.Add(mainExit);
            control.Add(Vector3.Lerp(mainExit, farEntry, .25f) + across * 8f);
            control.Add(Vector3.Lerp(mainExit, farEntry, .5f) + across * 14f);
            control.Add(Vector3.Lerp(mainExit, farEntry, .75f) + across * 8f);
            control.Add(farEntry);
            control.Add(far);
            control.Add(farExit);
            control.Add(far - across * 88f - along * 8f);
            control.Add(far - across * 100f);
            return SampleRoute(control, .9f);
        }

        private static List<Vector3> SampleRoute(IList<Vector3> control, float step)
        {
            var dense = new List<Vector3>();
            int last = control.Count - 1;
            for (int i = 0; i < last; i++)
            {
                Vector3 p0 = control[Mathf.Max(i - 1, 0)];
                Vector3 p1 = control[i];
                Vector3 p2 = control[i + 1];
                Vector3 p3 = control[Mathf.Min(i + 2, last)];
                int pieces = Mathf.Max(4, Mathf.RoundToInt(Vector3.Distance(p1, p2) / .35f));
                for (int piece = 0; piece < pieces; piece++)
                    dense.Add(CatmullPoint(p0, p1, p2, p3, piece / (float)pieces));
            }
            dense.Add(control[last]);
            return Resample(dense, step);
        }

        private static Vector3 CatmullPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            Vector3 point = .5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
            point.y = 0f;
            return point;
        }

        private static List<Vector3> Resample(IList<Vector3> source, float step)
        {
            var result = new List<Vector3> { source[0] };
            Vector3 previous = source[0];
            float carry = 0f;
            for (int i = 1; i < source.Count; i++)
            {
                Vector3 current = source[i];
                float segment = Vector3.Distance(previous, current);
                while (carry + segment >= step)
                {
                    float t = (step - carry) / Mathf.Max(segment, .0001f);
                    previous = Vector3.Lerp(previous, current, t);
                    result.Add(previous);
                    segment = Vector3.Distance(previous, current);
                    carry = 0f;
                }
                carry += segment;
                previous = current;
            }
            if (Vector3.Distance(result[result.Count - 1], source[source.Count - 1]) > step * .35f)
                result.Add(source[source.Count - 1]);
            return result;
        }

        private static float Trail(Vector3 point)
        {
            return DesertArchesMesh.Surface(point.x, point.z);
        }

        private static Vector3 TrailSample(float distance, out Vector3 tangent)
        {
            distance = Mathf.Clamp(distance, 0f, trailLength);
            int index = 1;
            while (index < trail.Count - 1 && trailDistances[index] < distance) index++;
            float span = trailDistances[index] - trailDistances[index - 1];
            float t = span > .0001f ? (distance - trailDistances[index - 1]) / span : 0f;
            Vector3 point = Vector3.Lerp(trail[index - 1], trail[index], t);
            Vector3 heading = trail[index] - trail[index - 1];
            heading.y = 0f;
            tangent = heading.sqrMagnitude > .0001f ? heading.normalized : Vector3.forward;
            return point;
        }

        private static Vector3 TerrainNormal(Vector3 point)
        {
            const float probe = .5f;
            float dx = DesertArchesMesh.Surface(point.x + probe, point.z) - DesertArchesMesh.Surface(point.x - probe, point.z);
            float dz = DesertArchesMesh.Surface(point.x, point.z + probe) - DesertArchesMesh.Surface(point.x, point.z - probe);
            return new Vector3(-dx, probe * 2f, -dz).normalized;
        }

        private static Quaternion SlopeRotation(Vector3 tangent, Vector3 normal)
        {
            tangent = Vector3.ProjectOnPlane(tangent, normal);
            if (tangent.sqrMagnitude < .0001f) tangent = Vector3.ProjectOnPlane(Vector3.forward, normal);
            Vector3 side = Vector3.Cross(normal, tangent.normalized).normalized;
            return Quaternion.LookRotation(side, normal);
        }

        private static void JumpSpots(CrownRockMesh skirt)
        {
            Mesh padA = Add("JumpPad-A.asset", DesertArchesMesh.StepPad(.5f, .5f, 12, 5501));
            Mesh padB = Add("JumpPad-B.asset", DesertArchesMesh.StepPad(.5f, .5f, 11, 5527));
            var steps = DesertArchesMesh.Collector();
            var brightSteps = DesertArchesMesh.Collector();
            jumpSeries = 0;
            float start = 6f;
            while (start < trailLength - 18f && jumpSeries < 56)
            {
                bool bright = jumpSeries % 3 == 0;
                CrownRockMesh batch = bright ? brightSteps : steps;
                Mesh pad = jumpSeries % 2 == 0 ? padA : padB;
                float runTop = Pad(batch, pad, start, 4.6f, 2.6f, float.NaN, skirt);
                float firstTop = Pad(batch, pad, start + 4.9f, 2.2f, 2f, runTop + .3f, skirt, runTop + .4f);
                float secondTop = Pad(batch, pad, start + 7.3f, 2.2f, 2f, firstTop + .3f, skirt, firstTop + .4f);
                Pad(batch, pad, start + 9.9f, 3.4f, 2.4f, secondTop + .05f, skirt);
                Vector3 point = TrailSample(start + 1.8f, out Vector3 heading);
                Vector3 side = Vector3.Cross(Vector3.up, heading).normalized;
                Vector3 spot = point + side * (jumpSeries % 2 == 0 ? 5.8f : -5.8f);
                float ground = DesertArchesMesh.Surface(spot.x, spot.z);
                float marker = Range(random, 1.35f, 1.95f);
                Spawn(terraces, "Камень-ориентир прыжковой серии " + (jumpSeries + 1),
                    boulders[random.Next(boulders.Length)], sunlit,
                    new Vector3(spot.x, ground + marker * .9f, spot.z),
                    new Vector3(Range(random, -7f, 7f), Range(random, 0f, 360f), Range(random, -7f, 7f)),
                    new Vector3(marker * 1.9f, marker * 2.6f, marker * 1.6f), true);
                for (int chip = 0; chip < 5; chip++)
                {
                    float bearing = Range(random, 0f, Mathf.PI * 2f);
                    float radius = marker * Range(random, 1.1f, 1.8f);
                    Chip(skirt, spot.x + Mathf.Cos(bearing) * radius, DesertArchesMesh.Surface(spot.x + Mathf.Cos(bearing) * radius, spot.z + Mathf.Sin(bearing) * radius),
                        spot.z + Mathf.Sin(bearing) * radius, Range(random, .16f, .34f), random);
                }
                jumpSeries++;
                start += Range(random, 15.5f, 26f) * (random.NextDouble() < .16 ? 1.7f : 1f);
            }
            Spawn(rocks, "Прыжковые уступы и полки тропы",
                Add("TrailJumpSteps.asset", DesertArchesMesh.Finish(steps, "Прыжковые уступы тропы")),
                sunlit, Vector3.zero, Vector3.zero, Vector3.one, true);
            Spawn(rocks, "Светлые прыжковые полки тропы",
                Add("TrailJumpStepsSunlit.asset", DesertArchesMesh.Finish(brightSteps, "Светлые прыжковые полки")),
                sunlit, Vector3.zero, Vector3.zero, Vector3.one, true);
        }

        private static float Pad(CrownRockMesh batch, Mesh pad, float at, float length, float width, float top, CrownRockMesh skirt, float ceiling = float.PositiveInfinity)
        {
            Vector3 point = TrailSample(at, out Vector3 tangent);
            float surface = DesertArchesMesh.Surface(point.x, point.z);
            float height = float.IsNaN(top) ? surface + .18f : Mathf.Clamp(top, surface - .2f, surface + .34f);
            height = Mathf.Min(height, ceiling);
            Vector3 normal = TerrainNormal(point);
            Quaternion rotation = SlopeRotation(tangent, normal) * Quaternion.Euler(0f, Range(random, -7f, 7f), 0f);
            float stretch = Range(random, .94f, 1.08f);
            DesertArchesMesh.Stamp(batch, pad, Matrix4x4.TRS(
                new Vector3(point.x, height, point.z), rotation,
                new Vector3(length * stretch, 1f, width * Range(random, .92f, 1.06f))));
            for (int chip = 0; chip < 3; chip++)
            {
                float bearing = Range(random, 0f, Mathf.PI * 2f);
                float radius = length * Range(random, .45f, .78f);
                float px = point.x + Mathf.Cos(bearing) * radius;
                float pz = point.z + Mathf.Sin(bearing) * radius;
                Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, Range(random, .07f, .18f), random);
            }
            return height;
        }

        private static void CheckpointMarkers(Transform parent, CrownRockMesh skirt)
        {
            string[] names =
            {
                "старт на кромке", "подъём серпантином", "главное окно", "траверс плато", "второе окно",
                "башни делегации", "верхняя полка", "осыпь спуска", "финиш — смотровая площадка"
            };
            var root = new GameObject("Чекпоинты маршрута").transform;
            root.SetParent(parent, false);
            checkpointCount = names.Length;
            for (int marker = 0; marker < names.Length; marker++)
            {
                float at = Mathf.Lerp(1f, trailLength - 1.2f, marker / (float)(names.Length - 1));
                Vector3 point = TrailSample(at, out Vector3 tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 spot = point + side * (ApronHalf(at / trailLength) + 1.4f) * (marker % 2 == 0 ? 1f : -1f);
                float ground = DesertArchesMesh.Surface(spot.x, spot.z);
                float size = 1.8f + marker * .05f;
                Spawn(root, "Чекпоинт " + (marker + 1) + " — " + names[marker],
                    boulders[(marker + 1) % boulders.Length], marker % 2 == 0 ? sunlit : sandstone,
                    new Vector3(spot.x, ground - size * .35f, spot.z),
                    new Vector3(Range(random, -6f, 6f), marker * 43f + 17f, Range(random, -6f, 6f)),
                    new Vector3(size * Range(random, .95f, 1.15f), size * Range(random, .72f, .92f), size * Range(random, .8f, 1.05f)),
                    true);
                for (int chip = 0; chip < 5; chip++)
                {
                    float bearing = Range(random, 0f, Mathf.PI * 2f);
                    float radius = size * Range(random, .7f, 1.15f);
                    float px = spot.x + Mathf.Cos(bearing) * radius;
                    float pz = spot.z + Mathf.Sin(bearing) * radius;
                    Chip(skirt, px, DesertArchesMesh.Surface(px, pz), pz, size * Range(random, .1f, .22f), random);
                }
            }
        }

        private static void FinishSite(Transform parent, CrownRockMesh skirt)
        {
            Vector3 point = TrailSample(trailLength - .8f, out Vector3 tangent);
            float ground = DesertArchesMesh.Surface(point.x, point.z);
            float top = ground + .2f;
            Mesh terrace = Add("FinishViewpoint.asset", DesertArchesMesh.Terrace(11f, 9f, top, ground - 34f, 4, 9401));
            Spawn(terraces, "Финиш — смотровая площадка над арками", terrace, sunlit,
                new Vector3(point.x, 0f, point.z), new Vector3(0f, Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg, 0f),
                Vector3.one, true);
            LedgeSatellites("Финишная смотровая площадка", new Vector3(point.x, 0f, point.z), 11f, 9f,
                Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg, -tangent, 2, sunlit, skirt);
            Spawn(parent, "Финишный знак — Тур", boulders[3], sandstone,
                new Vector3(point.x + tangent.z * 3.6f, top - 1.1f, point.z - tangent.x * 3.6f),
                new Vector3(0f, 23f, 0f), new Vector3(1.7f, 1.4f, 1.6f), true);
            heroAreas.Add(new Vector4(point.x, top, point.z, 18f));
        }

        private static void ScreeApron(CrownRockMesh skirt)
        {
            var scree = DesertArchesMesh.Collector();
            float from = Mathf.Max(0f, trailLength - 52f);
            for (float at = from; at < trailLength - 2f; at += .9f)
            {
                Vector3 point = TrailSample(at, out Vector3 tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float half = ApronHalf(at / trailLength);
                for (int stone = 0; stone < 3; stone++)
                {
                    float push = Range(random, .9f, half + .8f) * (stone % 2 == 0 ? 1f : -1f);
                    Vector3 spot = point + side * push;
                    float size = Range(random, .12f, .55f);
                    Chip(scree, spot.x, DesertArchesMesh.Surface(spot.x, spot.z), spot.z, size, random);
                }
            }
            for (int boulder = 0; boulder < 26; boulder++)
            {
                float at = Range(random, from, trailLength - 2f);
                Vector3 point = TrailSample(at, out Vector3 tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 spot = point + side * Range(random, 1.2f, 4f) * (boulder % 2 == 0 ? 1f : -1f);
                float size = Range(random, .35f, .85f);
                DesertArchesMesh.Stamp(scree, boulders[boulder % boulders.Length], Matrix4x4.TRS(
                    new Vector3(spot.x, DesertArchesMesh.Surface(spot.x, spot.z) + size * .18f, spot.z),
                    Quaternion.Euler(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f)),
                    new Vector3(size, size * Range(random, .5f, .8f), size * Range(random, .75f, 1f))));
            }
            Spawn(terraces, "Осыпь финишного спуска",
                Add("FinishScree.asset", DesertArchesMesh.Finish(scree, "Осыпь финишного спуска")),
                sandstone, Vector3.zero, Vector3.zero, Vector3.one, false);
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
            RenderSettings.fogDensity = .00019f / WorldScale;
            var sun = new GameObject("Полуденное пустынное солнце").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .93f, .78f);
            sun.intensity = 1.9f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .78f;
            sun.shadowBias = .06f;
            sun.shadowNormalBias = 1.1f;
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
            material.SetColor("_SandTint", new Color(.92f, .70f, .50f));
            material.SetColor("_SeamTint", new Color(.46f, .24f, .15f));
            material.SetColor("_ShadeTint", new Color(.55f, .36f, .29f));
            material.SetColor("_MidTint", new Color(1.00f, .64f, .45f));
            material.SetColor("_PlayaTint", new Color(.82f, .70f, .55f));
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
            material.SetFloat("_Playa", name == "Sand" ? .3f : .45f);
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
            if (material.HasProperty("_DistanceScale")) material.SetFloat("_DistanceScale", WorldScale);
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
            string[] prefix = { "DesertTower-", "SnowPeak", "DistantRange", "GrandArch", "FarArch", "DistantMesa", "WallCrest-" };
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
