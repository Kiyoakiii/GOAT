using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class SkyBridgeAuthoring
    {
        private const string AssetsPath = "Assets/GoatDescent/Level/Static/SkyBridge";
        private const string ScenePath = "Assets/GoatDescent/Scenes/SkyBridge.unity";
        private const float WorldScale = 5f;
        private static Material rock, ledge, snow, waterMat, fallMat, mistMat, foamMat, needlesMat, trunkMat;
        private static Material boulderMat, gravelMat, nicheMat, trailMat;
        private static Mesh puffMesh, nicheMesh, satelliteMesh;
        private static List<Vector3> route;
        private static List<int> routeStages;
        private static readonly Color TrailTint = new Color(.55f, .48f, .38f);
        private static readonly string[] RouteStageNames =
        {
            "старт у реки", "серпантин склона", "траверс террас", "сквозь врата",
            "подход к тыльной стороне", "подъём на арку", "козья тропа по своду",
            "спуск с арки", "у водопадов", "броды и финиш у валунов"
        };

        [MenuItem("Tools/GOAT/Статичные горы/Небесный мост — открыть")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Небесного моста."); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Небесный мост — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AssetsPath); AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Небесный мост — статичная композиция").transform;
            root.localScale = Vector3.one * WorldScale;
            var terrainRoot = Group(root, "Склоны ущелья, террасы и зелёные полки");
            var archRoot = Group(root, "Гребень и сквозной проём");
            var forestRoot = Group(root, "Еловые склоны");
            var waterRoot = Group(root, "Бирюзовая река, заводи и водопады");
            var mistRoot = Group(root, "Туман на дне ущелья");
            var stoneRoot = Group(root, "Валуны и осыпи");
            var farRoot = Group(root, "Дальние снежные хребты");
            route = TrailPoints();

            rock = Landscape("SkyBridgeRock", new Color(1.35f, 1.17f, .96f), new Color(.29f, .37f, .19f), Color.white, 100000f, .9f, .03f, .2f, 480f, .4f);
            rock.SetFloat("_HazeStrength", .38f);
            ledge = Landscape("GreenLedge", new Color(1.28f, 1.12f, .9f), new Color(.3f, .4f, .2f), Color.white, 100000f, 1.5f, .045f, .1f, -100000f, .85f);
            ledge.SetFloat("_HazeStrength", .4f);
            snow = Landscape("SnowRange", new Color(.95f, 1f, 1.08f), new Color(.16f, .24f, .17f), new Color(.8f, .85f, .92f), 300f, .05f, .02f, .1f, -100000f, 0f);
            snow.SetColor("_HazeColor", new Color(.44f, .56f, .7f));
            snow.SetFloat("_HazeStrength", .45f);
            waterMat = Fluid("TurquoiseRiver", "GoatDescent/SkyBridgeWater");
            waterMat.SetColor("_Shallow", new Color(.06f, .62f, .58f));
            waterMat.SetColor("_Deep", new Color(.015f, .26f, .3f));
            waterMat.SetColor("_Foam", new Color(.78f, .95f, .94f));
            waterMat.SetFloat("_Opacity", .95f);
            waterMat.SetFloat("_Smoothness", .45f);
            waterMat.SetColor("_HazeColor", new Color(.46f, .58f, .72f));
            waterMat.SetFloat("_HazeStart", 5000f);
            waterMat.SetFloat("_HazeEnd", 16000f);
            waterMat.SetFloat("_HazeStrength", .15f);
            fallMat = Fluid("Cascade", "GoatDescent/SkyBridgeWaterfall");
            fallMat.SetColor("_Color", new Color(.8f, .94f, .95f));
            fallMat.SetColor("_Core", new Color(.96f, 1f, 1f));
            fallMat.SetFloat("_Opacity", .66f);
            mistMat = Fluid("ValleyMistPuff", "GoatDescent/SkyBridgeMist");
            foamMat = Fluid("CascadeFoam", "GoatDescent/SkyBridgeMist");
            foamMat.SetColor("_Color", new Color(.84f, .97f, .96f));
            foamMat.SetColor("_Shadow", new Color(.6f, .72f, .78f));
            foamMat.SetFloat("_Opacity", .48f);
            foamMat.SetFloat("_NoiseScale", .09f);
            needlesMat = Simple("FirNeedles", new Color(.24f, .29f, .21f), .04f);
            trunkMat = Simple("FirTrunk", new Color(.18f, .15f, .12f), .05f);
            mistMat.SetColor("_Color", new Color(.84f, .9f, .94f));
            mistMat.SetColor("_Shadow", new Color(.52f, .62f, .7f));
            mistMat.SetFloat("_Opacity", .28f);
            boulderMat = Landscape("BoulderRock", new Color(1.32f, 1.15f, .94f), new Color(.2f, .27f, .19f), Color.white, 100000f, .25f, .05f, .14f, -100000f, 0f);
            boulderMat.SetFloat("_HazeStrength", .32f);
            gravelMat = Landscape("GravelBank", new Color(1.05f, .96f, .82f), new Color(.2f, .27f, .19f), Color.white, 100000f, .2f, .18f, .08f, -100000f, 0f);
            gravelMat.SetFloat("_HazeStrength", .28f);
            var screeMat = Landscape("ScreeStone", new Color(1.22f, 1.1f, .94f), new Color(.2f, .27f, .2f), Color.white, 100000f, .15f, .22f, .06f, -100000f, 0f);
            screeMat.SetFloat("_HazeStrength", .3f);
            nicheMat = Landscape("WetNiche", new Color(.5f, .46f, .42f), new Color(.14f, .16f, .14f), Color.white, 100000f, .1f, .05f, .12f, -100000f, 0f);
            trailMat = Landscape("TrailEarth", TrailTint, new Color(.3f, .36f, .21f), Color.white,
                100000f, .06f, .14f, .08f, -100000f, .5f);
            trailMat.SetFloat("_HazeStrength", .3f);
            rock.SetFloat("_ShadowFloor", .58f);
            rock.SetFloat("_Detail", .9f);
            ledge.SetFloat("_ShadowFloor", .52f);
            boulderMat.SetFloat("_ShadowFloor", .45f);
            gravelMat.SetFloat("_ShadowFloor", .4f);
            screeMat.SetFloat("_ShadowFloor", .42f);
            nicheMat.SetFloat("_ShadowFloor", .25f);
            trailMat.SetFloat("_ShadowFloor", .3f);
            trailMat.SetFloat("_TexAmount", 0f);
            trailMat.SetFloat("_Detail", .3f);
            trailMat.SetFloat("_Tiling", .055f);
            puffMesh = Save(SkyBridgeMesh.MistPuff(1f, 5), "MistPuff.asset");
            nicheMesh = Save(SkyBridgeMesh.Niche(34f, 64f, 5), "FallNiche.asset");
            satelliteMesh = Save(SkyBridgeMesh.SatelliteStone(31), "LedgeSatellite.asset");

            var ledgeMeshes = new[]
            {
                Save(SkyBridgeMesh.Ledge(30f, 3), "LedgeLarge.asset"),
                Save(SkyBridgeMesh.Ledge(22f, 17), "LedgeMedium.asset"),
                Save(SkyBridgeMesh.Ledge(15f, 29), "LedgeSmall.asset")
            };
            var ledgeRadii = new[] { 30f, 22f, 15f };

            Place(terrainRoot, "Террасы ущелья, склоны и плато", Save(SkyBridgeMesh.Terrain(), "CanyonTerrain.asset"),
                rock, Vector3.zero, Vector3.one, true);
            Place(archRoot, "Гребень со сквозным проёмом", Save(SkyBridgeMesh.Ridge(), "SkyArchRidge.asset"),
                rock, Vector3.zero, Vector3.one, true);
            Debug.Log("SKY_BRIDGE_ARCH axis=Z span=" + Mathf.RoundToInt(SkyBridgeMesh.ArchHalf * 2f)
                + " crown=340 leftFoot=" + Mathf.RoundToInt(-SkyBridgeMesh.ArchHalf) + " rightFoot="
                + Mathf.RoundToInt(SkyBridgeMesh.ArchHalf) + " center=0,170,0 depth="
                + Mathf.RoundToInt(SkyBridgeMesh.RidgeBaseDepth(0f)));
            Place(archRoot, "Свод, карнизы и сталактиты", Save(SkyBridgeMesh.Vault(), "ArchVault.asset"),
                rock, Vector3.zero, Vector3.one);

            var spurs = new[]
            {
                Save(SkyBridgeMesh.Spur(130f, 235f, 7), "SpurLarge.asset"),
                Save(SkyBridgeMesh.Spur(96f, 172f, 23), "SpurMedium.asset"),
                Save(SkyBridgeMesh.Spur(66f, 118f, 41), "SpurSmall.asset")
            };
            ArchSpurs(stoneRoot, spurs, boulderMat);
            Volumes(terrainRoot, stoneRoot, spurs);

            var archForest = new SkyBridgeMesh.TreeCluster();
            CrestForest(archForest);
            ArchLedges(archRoot, archForest, ledgeMeshes, ledgeRadii);
            CrownShelves(archRoot, ledgeMeshes);
            PlaceForest(forestRoot, "Лес гребня и полок арки", "ArchCrestNeedles.asset", "ArchCrestTrunks.asset", archForest);
            SlopeForest(forestRoot, "Лес западного склона", "WestSlopeNeedles.asset", "WestSlopeTrunks.asset", 7717, -1f, 112);
            SlopeForest(forestRoot, "Лес восточного склона", "EastSlopeNeedles.asset", "EastSlopeTrunks.asset", 4139, 1f, 112);
            SlopeForest(forestRoot, "Лес дальних склонов", "FarSlopeNeedles.asset", "FarSlopeTrunks.asset", 2081, 1f, 70);
            PlateauGroves(forestRoot, "PlateauWestNeedles.asset", "PlateauWestTrunks.asset", 6151, -1f);
            PlateauGroves(forestRoot, "PlateauEastNeedles.asset", "PlateauEastTrunks.asset", 3319, 1f);

            Place(stoneRoot, "Валуны и осыпи ущелья", Save(SkyBridgeMesh.BoulderField(130, 331, route), "BoulderField.asset"),
                boulderMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Галька и подмытые берега", Save(SkyBridgeMesh.GravelBars(), "GravelBars.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Мели в русле", Save(SkyBridgeMesh.Shoals(28, 617), "Shoals.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Камни-переправа", Save(SkyBridgeMesh.Fords(733), "Fords.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Осыпи в кулуарах", Save(SkyBridgeMesh.Scree(26, 14, 811, route), "Scree.asset"),
                screeMat, Vector3.zero, Vector3.one);
            var corniceForest = new SkyBridgeMesh.TreeCluster();
            var corniceMeshes = new[]
            {
                Save(SkyBridgeMesh.Cornice(26f, 71), "CorniceLarge.asset"),
                Save(SkyBridgeMesh.Cornice(19f, 83), "CorniceMedium.asset"),
                Save(SkyBridgeMesh.Cornice(13f, 97), "CorniceSmall.asset")
            };
            Cornices(forestRoot, corniceMeshes, new[] { 26f, 19f, 13f }, corniceForest);
            PlaceForest(forestRoot, "Лес висячих козырьков", "CorniceNeedles.asset", "CorniceTrunks.asset", corniceForest);

            var pools = new[]
            {
                Save(SkyBridgeMesh.Pool(1f, 1f, 11), "PoolCalm.asset"),
                Save(SkyBridgeMesh.Pool(1.3f, .9f, 23), "PoolWide.asset")
            };
            Water(waterRoot, archRoot, ledgeMeshes, pools);
            Mist(mistRoot);
            Place(farRoot, "Снежные хребты по кругу", Save(SkyBridgeMesh.FarRanges(), "SnowRanges.asset"),
                snow, Vector3.zero, Vector3.one);
            GoatTrail(root);
            var jumpSpots = JumpSpots();
            Mesh jumpMesh = Save(SkyBridgeMesh.JumpSteps(route, jumpSpots, 431,
                out int stepStones, out float stepGap, out float stepRise), "JumpSteps.asset");
            Place(stoneRoot, "Валуны-ступени маршрута", jumpMesh, boulderMat, Vector3.zero, Vector3.one, true);
            JumpMarkers(root, jumpSpots);
            Debug.Log("SKY_BRIDGE_JUMPS spots=" + jumpSpots.Count + " stones=" + stepStones
                + " maxGap=" + stepGap.ToString("F2") + " maxRise=" + stepRise.ToString("F2"));

            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 50f; camera.nearClipPlane = .5f; camera.farClipPlane = 5400f * WorldScale;
            camera.clearFlags = CameraClearFlags.Skybox; camera.depthTextureMode = DepthTextureMode.Depth;
            camera.allowHDR = true;
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.coolTint = new Color(.96f, .99f, 1.03f);
            post.warmTint = new Color(1.04f, 1f, .94f);
            post.saturation = 1.05f;
            post.bloomIntensity = .3f;
            post.bloomThreshold = .88f;
            post.vignette = .3f;
            post.contrast = 1.05f;
            OverviewPose(camera);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Render(camera, "SkyBridge-overview");
            Pose(camera, new Vector3(70f, 330f, 720f), new Vector3(0f, 270f, -180f));
            Render(camera, "SkyBridge-opening");
            Pose(camera, new Vector3(920f, 540f, 720f), new Vector3(0f, 400f, 0f));
            Render(camera, "SkyBridge-side");
            Pose(camera, new Vector3(30f, 96f, 430f), new Vector3(0f, 200f, -320f));
            Render(camera, "SkyBridge-river");
            Pose(camera, new Vector3(-150f, 640f, -1120f), new Vector3(0f, 360f, 0f));
            Render(camera, "SkyBridge-back");
            Pose(camera, new Vector3(430f, 1040f, 540f), new Vector3(0f, 540f, 0f));
            Render(camera, "SkyBridge-crest");
            Debug.Log("SKY_BRIDGE_STATIC_SAVED scene=" + ScenePath + " triangles=" + Triangles());
        }

        [MenuItem("Tools/GOAT/Статичные горы/Небесный мост — кадры маршрута")]
        public static void RouteShots()
        {
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Небесного моста."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null) { Debug.LogWarning("В сцене нет камеры."); return; }
            route = TrailPoints();
            RefreshLook();
            RefreshTrails();
            Physics.SyncTransforms();
            int shots = Mathf.Min(8, route.Count);
            int rendered = 0;
            for (int i = 0; i < shots; i++)
            {
                int index = Mathf.Clamp(Mathf.RoundToInt((i + .5f) / shots * (route.Count - 1)), 0, route.Count - 1);
                int ahead = Mathf.Min(route.Count - 1, index + 12);
                int behind = Mathf.Max(0, index - 12);
                Vector3 tangent = route[ahead] - route[behind];
                if (tangent.sqrMagnitude < .01f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 waypoint = route[index];
                Vector3 target = route[ahead] + Vector3.up * 2f;
                Vector3 position = waypoint - tangent * 6f + Vector3.up * 8f;
                float lift = 0f;
                bool clear = RouteEye(position, target, waypoint.y, ref lift);
                Debug.Log("ROUTE_SHOTS SkyBridge shot=" + (i + 1) + " lift=" + Mathf.RoundToInt(lift * WorldScale)
                    + "м eye=" + Mathf.RoundToInt(position.x * WorldScale) + "," + Mathf.RoundToInt(position.y * WorldScale)
                    + "," + Mathf.RoundToInt(position.z * WorldScale)
                    + " ground=" + Mathf.RoundToInt(Ground(position.x, position.z, waypoint.y) * WorldScale)
                    + " clear=" + clear);
                if (!clear)
                {
                    Debug.LogWarning("ROUTE_SHOTS SkyBridge skip=" + (i + 1) + " reason=intersection");
                    continue;
                }
                camera.transform.position = position * WorldScale;
                camera.transform.LookAt(target * WorldScale);
                Render(camera, "route-SkyBridge-" + (i + 1).ToString("00"));
                rendered++;
            }
            Debug.Log("ROUTE_SHOTS SkyBridge count=" + rendered + "/" + shots);
        }

        private static void RefreshTrails()
        {
            var tread = SkyBridgeMesh.Walkway(route, 5.2f, .14f, 1.5f);
            var crest = RouteSection(6);
            var crown = crest.Count > 1 ? SkyBridgeMesh.Walkway(crest, 9.5f, .08f, 2.6f) : null;
            int refreshed = 0;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string name = filter.gameObject.name;
                Mesh replacement = name.Contains("Полотно тропы") ? tread
                    : name.Contains("своду арки") ? crown : null;
                if (replacement == null) continue;
                filter.sharedMesh = replacement;
                var collider = filter.GetComponent<MeshCollider>();
                if (collider != null) collider.sharedMesh = replacement;
                refreshed++;
            }
            Debug.Log("ROUTE_SHOTS SkyBridge trails=" + refreshed);
        }

        private static float Ground(float x, float z, float reference)
        {
            float terrain = SkyBridgeMesh.TerrainHeight(x, z);
            float walkable = SkyBridgeMesh.Walkable(x, z);
            return walkable > reference + 12f ? terrain : Mathf.Max(terrain, walkable);
        }

        private static bool RouteEye(Vector3 eye, Vector3 target, float reference, ref float lift)
        {
            Vector3 direction = (target - eye).sqrMagnitude > .001f
                ? (target - eye).normalized : Vector3.forward;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float raise = Mathf.Max(0f, Ground(eye.x, eye.z, reference) + 6f - eye.y);
                for (int probe = 1; probe <= 3; probe++)
                {
                    Vector3 sample = eye + direction * (probe * 10f);
                    float ahead = Ground(sample.x, sample.z, reference) + 2f;
                    if (sample.y < ahead) raise = Mathf.Max(raise, ahead - sample.y);
                }
                if (raise <= .01f) break;
                eye += Vector3.up * raise;
                lift += raise;
                direction = (target - eye).sqrMagnitude > .001f ? (target - eye).normalized : Vector3.forward;
            }
            for (int step = 0; step < 8; step++)
            {
                if (!Physics.CheckSphere(eye * WorldScale, 3f, ~0, QueryTriggerInteraction.Ignore)) return true;
                eye += Vector3.up * 4f;
                lift += 4f;
            }
            return false;
        }

        private static int Triangles()
        {
            var meshes = new HashSet<Mesh>();
            int total = 0;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (filter.sharedMesh == null || !meshes.Add(filter.sharedMesh)) continue;
                total += filter.sharedMesh.triangles.Length / 3;
            }
            return total;
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static Transform Place(Transform parent, string name, Mesh mesh, Material material, Vector3 position,
            Vector3 scale, bool collider = false)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            item.isStatic = true;
            if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item.transform;
        }

        private static Material Landscape(string name, Color rockTint, Color grassTint, Color snowTint,
            float snowLine, float grass, float tiling, float layers, float greenLine, float occlusion = 0f)
        {
            var material = new Material(Shader.Find("GoatDescent/SkyBridgeRock"));
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", rockTint);
            material.SetColor("_GrassTint", grassTint);
            material.SetColor("_SnowTint", snowTint);
            material.SetFloat("_SnowLine", snowLine);
            material.SetFloat("_GreenLine", greenLine);
            material.SetFloat("_Occlusion", occlusion);
            material.SetColor("_HazeColor", new Color(.46f, .58f, .72f));
            material.SetFloat("_HazeStart", 3800f);
            material.SetFloat("_HazeEnd", 15000f);
            material.SetFloat("_HazeStrength", .4f);
            material.SetFloat("_Grass", grass);
            material.SetFloat("_Tiling", tiling);
            material.SetFloat("_Layers", layers);
            return Save(material, name + ".mat");
        }

        private static Material Fluid(string name, string shader) => Save(new Material(Shader.Find(shader)), name + ".mat");

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
            RenderSettings.ambientIntensity = 1.18f;
            RenderSettings.ambientSkyColor = new Color(.5f, .58f, .69f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .5f, .49f);
            RenderSettings.ambientGroundColor = new Color(.32f, .36f, .31f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .6f, .73f); RenderSettings.fogDensity = .00019f / WorldScale;
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.3f, .53f, .82f));
            sky.SetColor("_SkyHorizon", new Color(.52f, .69f, .87f));
            sky.SetColor("_GroundColor", new Color(.52f, .56f, .55f));
            sky.SetColor("_SunColor", new Color(1f, .94f, .8f));
            sky.SetColor("_CloudColor", new Color(.92f, .94f, .95f));
            sky.SetFloat("_SunSize", .1f);
            sky.SetFloat("_CloudDensity", .62f);
            sky.SetVector("_SunDir", new Vector4(-.52f, .62f, .59f, 0f));
            RenderSettings.skybox = Save(sky, "SkyBridgeSky.mat");
            var sun = Directional("Солнце над гребнем", new Color(1f, .92f, .8f), 1.15f,
                new Vector3(38f, 138f, 0f), LightShadows.Soft, .68f);
            sun.shadowBias = .06f;
            sun.shadowNormalBias = .9f;
            RenderSettings.sun = sun;
            Directional("Отражённый свет склонов", new Color(.55f, .66f, .8f), .4f, new Vector3(24f, -46f, 0f));
            Directional("Отражённый свет противоположной стены", new Color(.5f, .58f, .54f), .28f,
                new Vector3(16f, 134f, 0f));
            Directional("Отражённый свет дна ущелья", new Color(.46f, .55f, .47f), .42f, new Vector3(-58f, 34f, 0f));
            Directional("Отражённый свет реки", new Color(.42f, .56f, .54f), .26f, new Vector3(-72f, 152f, 0f));
        }

        private static Light Directional(string name, Color color, float intensity, Vector3 euler,
            LightShadows shadows = LightShadows.None, float shadowStrength = 1f)
        {
            var existing = GameObject.Find(name);
            var light = existing != null ? existing.GetComponent<Light>() : null;
            if (light == null) light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = shadowStrength;
            light.transform.rotation = Quaternion.Euler(euler);
            return light;
        }

        private static void RefreshLook()
        {
            void Tune(string name, float floor, float detail, float occlusion = -1f)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetsPath + "/" + name + ".mat");
                if (material == null) return;
                material.SetFloat("_ShadowFloor", floor);
                material.SetFloat("_Detail", detail);
                if (occlusion >= 0f) material.SetFloat("_Occlusion", occlusion);
                EditorUtility.SetDirty(material);
            }
            Tune("SkyBridgeRock", .58f, .9f, .4f);
            Tune("GreenLedge", .52f, 1f);
            Tune("BoulderRock", .45f, 1f);
            Tune("GravelBank", .4f, 1f);
            Tune("ScreeStone", .42f, 1f);
            Tune("WetNiche", .25f, 1f);
            Tune("TrailEarth", .3f, .3f);
            var trail = AssetDatabase.LoadAssetAtPath<Material>(AssetsPath + "/TrailEarth.mat");
            if (trail != null)
            {
                trail.SetColor("_RockTint", TrailTint);
                trail.SetFloat("_TexAmount", 0f);
                trail.SetFloat("_Tiling", .055f);
                EditorUtility.SetDirty(trail);
            }
            AssetDatabase.SaveAssets();
            Environment();
        }

        private static void PlateauGroves(Transform parent, string needlesName, string trunksName, int seed, float side)
        {
            var cluster = new SkyBridgeMesh.TreeCluster();
            var random = new System.Random(seed);
            for (int patch = 0; patch < 6; patch++)
            {
                float z = -700f + (float)random.NextDouble() * 1400f;
                float d = SkyBridgeMesh.ChannelHalf(z) + 170f + (float)random.NextDouble() * 250f;
                float x = SkyBridgeMesh.RiverCenter(z) + side * d;
                if (SkyBridgeMesh.TerrainHeight(x, z) < 45f) continue;
                float radius = 60f + (float)random.NextDouble() * 90f;
                int count = 24 + random.Next(20);
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float r = radius * Mathf.Sqrt((float)random.NextDouble());
                    float px = x + Mathf.Cos(angle) * r;
                    float pz = z + Mathf.Sin(angle) * r * .8f;
                    float y = SkyBridgeMesh.TerrainHeight(px, pz);
                    if (y < 40f) continue;
                    if (NearRoute(px, pz, 14f)) continue;
                    if (Steep(px, pz, .6f)) continue;
                    cluster.Add(px, y - .6f, pz, TreeSize(random, 18f, 34f), (float)random.NextDouble() * 360f);
                }
            }
            PlaceForest(parent, "Лесной массив на плато", needlesName, trunksName, cluster);
        }

        private static void Cornices(Transform parent, Mesh[] cornices, float[] radii, SkyBridgeMesh.TreeCluster forest)
        {
            var random = new System.Random(5231);
            int index = 0;
            for (int i = 0; i < 30; i++)
            {
                float z = -700f + (float)random.NextDouble() * 1400f;
                if (Mathf.Abs(z) < 300f) continue;
                float side = random.Next(2) == 0 ? -1f : 1f;
                float d = SkyBridgeMesh.ChannelHalf(z) + 55f + (float)random.NextDouble() * 105f;
                float x = SkyBridgeMesh.RiverCenter(z) + side * d;
                float y = SkyBridgeMesh.TerrainHeight(x, z);
                if (NearRoute(x, z, 16f)) continue;
                int kind = index++ % cornices.Length;
                float stretch = .65f + (float)random.NextDouble() * .5f;
                float squash = stretch * (.55f + (float)random.NextDouble() * .9f);
                var item = Place(parent, "Висячий травяной козырёк", cornices[kind], ledge,
                    new Vector3(x, y - 1.2f, z), new Vector3(stretch, squash, stretch));
                item.localRotation = Quaternion.Euler(-18f + (float)random.NextDouble() * 10f,
                    (float)random.NextDouble() * 360f, (float)random.NextDouble() * 22f - 11f);
                if (stretch > .92f && random.NextDouble() < .55f)
                {
                    float sa = (float)random.NextDouble() * Mathf.PI * 2f;
                    float stoneScale = .5f + (float)random.NextDouble() * .3f;
                    float yaw = (float)random.NextDouble() * 360f;
                    bool seated = false;
                    Vector3 seat = Vector3.zero;
                    float exposure = float.MinValue;
                    for (int attempt = 0; attempt < 4; attempt++)
                    {
                        if (!ShelfSeat(cornices[kind], radii[kind], sa + attempt * Mathf.PI * .5f, squash,
                            stoneScale, out Vector3 candidate)) continue;
                        Vector3 rootPoint = item.localPosition
                            + item.localRotation * Vector3.Scale(item.localScale, candidate);
                        float clear = rootPoint.y - SkyBridgeMesh.TerrainHeight(rootPoint.x, rootPoint.z);
                        if (clear <= exposure) continue;
                        exposure = clear; seat = candidate; seated = true;
                    }
                    if (seated)
                    {
                        var stone = Place(parent, "Камень-спутник козырька", satelliteMesh, boulderMat, Vector3.zero,
                            Vector3.one * stoneScale);
                        stone.SetParent(item, false);
                        stone.localPosition = seat;
                        stone.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    }
                }
                int trees = 1 + random.Next(2);
                for (int tree = 0; tree < trees; tree++)
                {
                    float ox = ((float)random.NextDouble() - .5f) * 22f;
                    float oz = ((float)random.NextDouble() - .5f) * 14f;
                    if (NearRoute(x + ox, z + oz, 12f)) continue;
                    forest.Add(x + ox, y + .8f, z + oz, TreeSize(random, 12f, 22f),
                        (float)random.NextDouble() * 360f);
                }
            }
        }

        private static void ArchSpurs(Transform parent, Mesh[] spurs, Material material)
        {
            var random = new System.Random(4411);
            int index = 0;
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (360f + (float)random.NextDouble() * 40f);
                float z = (i < 2 ? -1f : 1f) * (SkyBridgeMesh.RidgeBaseDepth(x) + 40f);
                SpurAt(parent, spurs[0], material, new Vector3(x, SkyBridgeMesh.TerrainHeight(x, z) - 55f, z),
                    random, 1f);
            }
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (340f + (float)random.NextDouble() * 90f);
                float z = -360f + (float)random.NextDouble() * 720f;
                SpurAt(parent, spurs[index++ % spurs.Length], material,
                    new Vector3(x, SkyBridgeMesh.TerrainHeight(x, z) - 45f, z), random, .85f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = (300f + (float)random.NextDouble() * 240f) * (random.Next(2) == 0 ? 1f : -1f);
                float z = (i % 2 == 0 ? -1f : 1f) * (SkyBridgeMesh.RidgeFaceDepth(x) + 44f);
                SpurAt(parent, spurs[index++ % spurs.Length], material,
                    new Vector3(x, SkyBridgeMesh.TerrainHeight(x, z) - 45f, z), random);
            }
        }

        private static void SpurAt(Transform parent, Mesh mesh, Material material, Vector3 position, System.Random random,
            float scale = 1f)
        {
            if (NearRoute(position.x, position.z, 55f)) return;
            var item = Place(parent, "Скальный отрог у арки", mesh, material, position,
                Vector3.one * (scale * (.78f + (float)random.NextDouble() * .55f)));
            item.localRotation = Quaternion.Euler(-9f + (float)random.NextDouble() * 18f,
                (float)random.NextDouble() * 360f, -9f + (float)random.NextDouble() * 18f);
        }

        private static void CrestForest(SkyBridgeMesh.TreeCluster cluster)        {
            var random = new System.Random(5501);
            for (float x = -820f; x <= 820f; x += 14f)
            {
                float top = SkyBridgeMesh.RidgeTopHalf(x);
                for (int row = 0; row < 4; row++)
                {
                    float z = ((row - 1.5f) * .3f + ((float)random.NextDouble() - .5f) * .26f) * top;
                    float px = x + ((float)random.NextDouble() - .5f) * 12f;
                    if (Mathf.PerlinNoise(px * .013f, z * .02f + 3.1f) < .16f) continue;
                    if (NearRoute(px, z, 9f)) continue;
                    float y = SkyBridgeMesh.RidgeTop(px, z);
                    cluster.Add(px, y - .8f, z, TreeSize(random, 14f, 26f), (float)random.NextDouble() * 360f);
                }
            }
            for (float x = -820f; x <= 820f; x += 14f)
                for (int edge = 0; edge < 2; edge++)
                {
                    float sign = edge == 0 ? -1f : 1f;
                    float z = sign * (SkyBridgeMesh.RidgeTopHalf(x) * .92f);
                    float px = x + ((float)random.NextDouble() - .5f) * 9f;
                    if (NearRoute(px, z, 9f)) continue;
                    cluster.Add(px, SkyBridgeMesh.RidgeTop(px, z) - 1f,
                        z + ((float)random.NextDouble() - .5f) * 6f,
                        TreeSize(random, 16f, 26f), (float)random.NextDouble() * 360f);
                }
            float[] giantX = { -320f, -215f, -150f, 155f, 320f };
            for (int i = 0; i < giantX.Length; i++)
            {
                float x = giantX[i];
                float z = ((float)random.NextDouble() - .5f) * 24f;
                float gx = x + ((float)random.NextDouble() - .5f) * 18f;
                if (NearRoute(gx, z, 12f)) continue;
                cluster.Add(gx, SkyBridgeMesh.RidgeTop(gx, z) - 1f, z,
                    30f + (float)random.NextDouble() * 10f, (float)random.NextDouble() * 360f);
            }
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float gx = side * (152f + (float)random.NextDouble() * 42f);
                float gz = (random.Next(2) == 0 ? -1f : 1f) * (236f + (float)random.NextDouble() * 52f);
                if (!NearRoute(gx, gz, 12f))
                    cluster.Add(gx, SkyBridgeMesh.TerrainHeight(gx, gz) - 1f, gz,
                        32f + (float)random.NextDouble() * 8f, (float)random.NextDouble() * 360f);
            }
        }

        private static float TreeSize(System.Random random, float min, float max)
            => min + (max - min) * Mathf.Pow((float)random.NextDouble(), 2.2f);

        private static void ArchLedges(Transform parent, SkyBridgeMesh.TreeCluster forest, Mesh[] ledges, float[] radii)
        {
            var random = new System.Random(9137);
            int index = 0;
            for (int face = 0; face < 2; face++)
                for (int tier = 0; tier < 5; tier++)
                    for (int spot = 0; spot < 8; spot++)
                    {
                        float u = .1f + tier * .17f + (float)random.NextDouble() * .05f;
                        float x = (206f + (float)random.NextDouble() * 614f) * (random.Next(2) == 0 ? 1f : -1f);
                        Vector3 point = SkyBridgeMesh.RidgeFace(x, Mathf.Min(1f, u), face == 0);
                        if (NearRoute(point.x, point.z, 18f)) continue;
                        float outward = face == 0 ? -1f : 1f;
                        int kind = index++ % ledges.Length;
                        float size = 2.1f + (float)random.NextDouble() * .8f;
                        var item = Place(parent, "Зелёная полка на склоне арки", ledges[kind], ledge,
                            new Vector3(point.x, point.y, point.z + outward * 4f), Vector3.one * size);
                        item.localRotation = Quaternion.Euler(0f, 0f, (float)(random.NextDouble() * 8f - 4f));
                        if (size > 2.5f && random.NextDouble() < .5f)
                        {
                            float sa = (float)random.NextDouble() * Mathf.PI * 2f;
                            float stoneScale = 1.65f + (float)random.NextDouble() * .9f;
                            float yaw = (float)random.NextDouble() * 360f;
                            if (ShelfSeat(ledges[kind], radii[kind], sa, size, stoneScale, out Vector3 seat))
                            {
                                var stone = Place(parent, "Камень-спутник полки", satelliteMesh, boulderMat,
                                    Vector3.zero, Vector3.one * stoneScale);
                                stone.SetParent(item, false);
                                stone.localPosition = seat;
                                stone.localRotation = Quaternion.Euler(0f, yaw, 0f);
                            }
                        }
                        int trees = 2 + random.Next(2);
                        for (int tree = 0; tree < trees; tree++)
                        {
                            float ox = ((float)random.NextDouble() - .5f) * 34f;
                            float oz = ((float)random.NextDouble() - .5f) * 5f;
                            if (NearRoute(point.x + ox, point.z + outward * 4f + oz, 8f)) continue;
                            forest.Add(point.x + ox, point.y + .5f, point.z + outward * 4f + oz,
                                46f + (float)random.NextDouble() * 24f, (float)random.NextDouble() * 360f);
                        }
                    }
        }

        private static bool ShelfSeat(Mesh shelf, float radius, float angle, float scaleY, float stoneScale,
            out Vector3 position)
        {
            float x = Mathf.Cos(angle) * radius * .32f;
            float z = Mathf.Sin(angle) * radius * .12f;
            position = Vector3.zero;
            if (!SkyBridgeMesh.MeshSurface(shelf, x, z, out float surface)) return false;
            float depth = -satelliteMesh.bounds.min.y * scaleY * stoneScale;
            position = new Vector3(x, surface + depth - .16f * scaleY * stoneScale, z);
            return true;
        }

        private static void SlopeForest(Transform parent, string label, string needlesName, string trunksName,
            int seed, float side, int count)
        {
            var cluster = new SkyBridgeMesh.TreeCluster();
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float z = -760f + (float)random.NextDouble() * 1520f;
                if (Mathf.Abs(z) < 330f) continue;
                float distance = SkyBridgeMesh.ChannelHalf(z) + 118f + (float)random.NextDouble() * 240f;
                float x = SkyBridgeMesh.RiverCenter(z) + side * distance;
                float y = SkyBridgeMesh.TerrainHeight(x, z);
                if (y < 30f) continue;
                if (NearRoute(x, z, 14f)) continue;
                if (Mathf.PerlinNoise(x * .009f, z * .009f + 7.7f) < .34f) continue;
                if (Steep(x, z, .62f)) continue;
                cluster.Add(x, y - .6f, z, TreeSize(random, 18f, 34f), (float)random.NextDouble() * 360f);
            }
            PlaceForest(parent, label, needlesName, trunksName, cluster);
        }

        private static void PlaceForest(Transform parent, string label, string needlesName, string trunksName,
            SkyBridgeMesh.TreeCluster cluster)
        {
            var group = new GameObject(label).transform;
            group.SetParent(parent, false);
            Place(group, "Хвоя", Save(cluster.Needles(label), needlesName), needlesMat, Vector3.zero, Vector3.one);
            Place(group, "Стволы", Save(cluster.Trunks(label), trunksName), trunkMat, Vector3.zero, Vector3.one);
        }

        private static void Water(Transform waterRoot, Transform archRoot, Mesh[] ledges, Mesh[] pools)
        {
            NoShadow(Place(waterRoot, "Бирюзовая река сквозь проём",
                Save(SkyBridgeMesh.River(), "TurquoiseRiver.asset"), waterMat, Vector3.zero, Vector3.one));
            ArchFall(waterRoot, archRoot, ledges[0], pools[0]);
            SideFall(waterRoot, "Западный водопад", "WestFall.asset", -188f, 336f, 62f, 5, ledges[1], pools[1]);
            SideFall(waterRoot, "Восточный водопад", "EastFall.asset", 190f, -352f, 56f, 7, ledges[2], pools[0]);
        }

        private static void ArchFall(Transform waterRoot, Transform archRoot, Mesh ledgeMesh, Mesh poolMesh)
        {
            Place(archRoot, "Уступ водопада под сводом", ledgeMesh, ledge, new Vector3(120f, 262f, -46f), Vector3.one * 1.9f);
            NoShadow(Place(waterRoot, "Заводь под сводом", poolMesh, waterMat, new Vector3(116f, 266f, -42f),
                new Vector3(24f, 1f, 17f)));
            NoShadow(Place(waterRoot, "Водопад сквозь проём", Save(SkyBridgeMesh.Fall(
                new Vector3(114f, 264f, -28f), new Vector3(92f, SkyBridgeMesh.WaterLevel, -6f), Vector3.forward,
                78f, 24f, 3), "ArchFall.asset"), fallMat, Vector3.zero, Vector3.one));
            Foam(waterRoot, new Vector3(94f, 12f, -4f), 22f);
            Place(waterRoot, "Подмытая ниша за водопадом", nicheMesh, nicheMat, new Vector3(92f, -14f, -28f),
                Vector3.one * 1.6f).localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static void SideFall(Transform waterRoot, string label, string asset, float x, float z,
            float width, int seed, Mesh ledgeMesh, Mesh poolMesh)
        {
            float y = SkyBridgeMesh.TerrainHeight(x, z);
            float innerX = SkyBridgeMesh.RiverCenter(z) + Mathf.Sign(x) * (SkyBridgeMesh.RiverHalf(z) + 26f);
            Place(waterRoot, label + " — уступ", ledgeMesh, ledge, new Vector3(x, y - 7f, z), Vector3.one * 1.8f);
            NoShadow(Place(waterRoot, label + " — заводь", poolMesh, waterMat, new Vector3(x, y + 2f, z),
                new Vector3(19f, 1f, 14f)));
            Vector3 top = new Vector3(x, y + 1f, z);
            Vector3 bottom = new Vector3(innerX, SkyBridgeMesh.WaterLevel, z - 24f);
            Vector3 outward = new Vector3(-Mathf.Sign(x), 0f, 0f);
            NoShadow(Place(waterRoot, label + " — падающая вода",
                Save(SkyBridgeMesh.Fall(top, bottom, outward, width, 34f, seed), asset), fallMat, Vector3.zero, Vector3.one));
            Foam(waterRoot, bottom + Vector3.up * 10f, width * .8f);
            var niche = Place(waterRoot, label + " — подмытая ниша", nicheMesh, nicheMat,
                new Vector3(innerX + Mathf.Sign(innerX) * 10f, -14f, z - 34f), Vector3.one * 1.7f);
            niche.localRotation = Quaternion.Euler(0f, x < 0f ? -90f : 90f, 0f);
        }

        private static void Foam(Transform parent, Vector3 center, float spread)
        {
            var random = new System.Random((int)(center.z * 7f) + 991);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 1.257f;
                var item = Place(parent, "Пена у подножия водопада", puffMesh, foamMat,
                    center + new Vector3(Mathf.Cos(angle) * spread, (float)random.NextDouble() * 8f,
                        Mathf.Sin(angle) * spread * .7f),
                    new Vector3(16f + (float)random.NextDouble() * 12f, 8f + (float)random.NextDouble() * 6f,
                        16f + (float)random.NextDouble() * 12f));
                item.localRotation = Quaternion.Euler(0f, angle * 57f, 0f);
                NoShadow(item);
            }
        }

        private static void Mist(Transform parent)
        {
            var puff = puffMesh;
            var random = new System.Random(6421);
            for (int i = 0; i < 64; i++)
            {
                float z = -1100f + (float)random.NextDouble() * 2200f;
                float x = SkyBridgeMesh.RiverCenter(z) + ((float)random.NextDouble() - .5f) * 150f;
                float y = 3f + (float)random.NextDouble() * 10f;
                var item = Place(parent, "Клуб тумана", puff, mistMat, new Vector3(x, y, z),
                    new Vector3(62f + (float)random.NextDouble() * 62f, 8f + (float)random.NextDouble() * 5f,
                        62f + (float)random.NextDouble() * 62f));
                item.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                NoShadow(item);
            }
            for (int i = 0; i < 14; i++)
            {
                float z = -260f + i * 40f;
                var item = Place(parent, "Туман под сводом", puff, mistMat,
                    new Vector3(SkyBridgeMesh.RiverCenter(z) + ((i * 41) % 90) - 45f, 4f + (i % 3) * 4f, z),
                    new Vector3(58f, 7f, 42f));
                item.localRotation = Quaternion.Euler(0f, i * 41f, 0f);
                NoShadow(item);
            }
        }

        private sealed class RouteStage
        {
            public int Surface;
            public List<Vector3> Keys;
        }

        private static List<Vector3> TrailPoints()
        {
            routeStages = new List<int>();
            var stages = new List<RouteStage>
            {
                Stage(0, Bank(-360f, 1f, 18f), Bank(-340f, 1f, 35f), Bank(-328f, 1f, 48f)),
                Stage(0, Wall(-328f, 40f), Wall(-270f, 52f), Wall(-258f, 105f), Wall(-320f, 118f),
                    Wall(-325f, 165f), Wall(-270f, 178f)),
                Stage(0, Wall(-270f, 185f), Wall(-240f, 215f),
                    Key(SkyBridgeMesh.RiverCenter(-218f) + SkyBridgeMesh.RiverHalf(-218f) + 50f, -218f),
                    Bank(-200f, 1f, 14f)),
                Stage(0, GateKeys(-200f, 140f).ToArray()),
                Stage(0, Bank(148f, 1f, 20f),
                    Key(SkyBridgeMesh.RiverCenter(165f) - SkyBridgeMesh.ChannelHalf(165f) - 20f, 172f),
                    Key(-245f, 182f)),
                Stage(1, Key(-250f, 180f), Key(-350f, 172f), Key(-358f, 145f), Key(-255f, 140f),
                    Key(-250f, 112f), Key(-350f, 107f), Key(-358f, 82f), Key(-290f, 72f)),
                Stage(2, Key(-288f, 34f), Key(-220f, 18f), Key(-150f, 26f), Key(-75f, 10f), Key(0f, 22f),
                    Key(75f, 10f), Key(150f, 24f), Key(220f, 16f), Key(268f, 32f)),
                Stage(1, Key(270f, -52f), Key(270f, -82f), Key(348f, -108f), Key(350f, -138f),
                    Key(258f, -162f), Key(252f, -192f), Key(325f, -220f), Key(295f, -250f), Key(255f, -282f)),
                Stage(0, Key(218f, -310f), Key(190f, -352f),
                    Key(SkyBridgeMesh.RiverCenter(-368f) + SkyBridgeMesh.RiverHalf(-368f) + 24f, -368f),
                    Bank(-380f, 1f, 16f)),
                Stage(0, Bank(-380f, 1f, 16f), Bank(-310f, 1f, 16f), Bank(-240f, 1f, 16f),
                    Bank(-168f, 1f, 16f), Key(SkyBridgeMesh.RiverCenter(-168f), -168f), Bank(-168f, -1f, 16f),
                    Bank(-100f, -1f, 16f), Bank(-18f, -1f, 16f),
                    Key(SkyBridgeMesh.RiverCenter(-18f), -18f), Bank(-18f, 1f, 16f),
                    Bank(-100f, 1f, 16f), Bank(-180f, 1f, 16f),
                    Key(SkyBridgeMesh.RiverCenter(-230f) + SkyBridgeMesh.RiverHalf(-230f) + 50f, -230f))
            };
            var points = new List<Vector3>();
            for (int s = 0; s < stages.Count; s++)
            {
                var keys = SmoothKeys(stages[s].Keys, 2);
                var samples = Resample(keys, 4.2f);
                for (int i = 0; i < samples.Count; i++)
                {
                    if (points.Count > 0 && FlatDistance(points[points.Count - 1], samples[i]) < .35f) continue;
                    points.Add(new Vector3(samples[i].x,
                        RouteHeight(stages[s].Surface, samples[i].x, samples[i].z), samples[i].z));
                    routeStages.Add(s);
                }
            }
            return points;
        }

        private static RouteStage Stage(int surface, params Vector3[] keys)
            => new RouteStage { Surface = surface, Keys = new List<Vector3>(keys) };

        private static List<Vector3> GateKeys(float from, float to)
        {
            var keys = new List<Vector3>();
            for (float z = from; z <= to + .1f; z += 38f) keys.Add(Gate(z));
            if (keys.Count == 0 || keys[keys.Count - 1].z < to - 1f) keys.Add(Gate(to));
            return keys;
        }

        private static Vector3 Key(float x, float z) => new Vector3(x, 0f, z);

        private static Vector3 Bank(float z, float side, float offset) => Key(
            SkyBridgeMesh.RiverCenter(z) + side * (SkyBridgeMesh.RiverHalf(z) + offset), z);

        private static Vector3 Wall(float z, float band) => Key(
            SkyBridgeMesh.RiverCenter(z) + SkyBridgeMesh.ChannelHalf(z) + band, z);

        private static Vector3 Gate(float z) => Key(
            SkyBridgeMesh.RiverCenter(z) + SkyBridgeMesh.RiverHalf(z) + 12f + Mathf.Sin(z * .021f) * 8f, z);

        private static float RouteHeight(int surface, float x, float z)
        {
            if (surface == 2) return SkyBridgeMesh.RidgeTop(x, z);
            if (surface == 1) return Mathf.Max(SkyBridgeMesh.TerrainHeight(x, z), SkyBridgeMesh.Walkable(x, z));
            float y = SkyBridgeMesh.TerrainHeight(x, z);
            if (Mathf.Abs(x - SkyBridgeMesh.RiverCenter(z)) < SkyBridgeMesh.ChannelHalf(z) + 1f)
                y = Mathf.Max(y, SkyBridgeMesh.WaterLevel + .15f);
            return y;
        }

        private static List<Vector3> SmoothKeys(List<Vector3> keys, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                if (keys.Count < 3) break;
                var next = new List<Vector3> { keys[0] };
                for (int i = 0; i < keys.Count - 1; i++)
                {
                    next.Add(Vector3.Lerp(keys[i], keys[i + 1], .25f));
                    next.Add(Vector3.Lerp(keys[i], keys[i + 1], .75f));
                }
                next.Add(keys[keys.Count - 1]);
                keys = next;
            }
            return keys;
        }

        private static List<Vector3> Resample(List<Vector3> keys, float step)
        {
            var points = new List<Vector3> { keys[0] };
            float carry = 0f;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                Vector3 a = keys[i], b = keys[i + 1];
                float length = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                if (length < .0001f) continue;
                float travelled = 0f;
                while (true)
                {
                    float need = step - carry;
                    if (travelled + need > length) { carry += length - travelled; break; }
                    travelled += need;
                    points.Add(Vector3.Lerp(a, b, travelled / length));
                    carry = 0f;
                }
            }
            if (FlatDistance(points[points.Count - 1], keys[keys.Count - 1]) > .25f)
                points.Add(keys[keys.Count - 1]);
            return points;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
            => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static float RouteLength()
        {
            float length = 0f;
            for (int i = 1; i < route.Count; i++) length += Vector3.Distance(route[i - 1], route[i]);
            return length;
        }

        private static float RouteLengthFlat()
        {
            float length = 0f;
            for (int i = 1; i < route.Count; i++) length += FlatDistance(route[i - 1], route[i]);
            return length;
        }

        private static bool NearRoute(float x, float z, float radius)
        {
            if (route == null) return false;
            float squared = radius * radius;
            for (int i = 0; i < route.Count; i += 2)
            {
                float dx = route[i].x - x, dz = route[i].z - z;
                if (dx * dx + dz * dz < squared) return true;
            }
            return false;
        }

        private static bool Steep(float x, float z, float limit)
        {
            const float span = 7f;
            float dx = Mathf.Abs(SkyBridgeMesh.TerrainHeight(x + span, z) - SkyBridgeMesh.TerrainHeight(x - span, z));
            float dz = Mathf.Abs(SkyBridgeMesh.TerrainHeight(x, z + span) - SkyBridgeMesh.TerrainHeight(x, z - span));
            return Mathf.Max(dx, dz) / (span * 2f) > limit;
        }

        private static List<Vector3> RouteSection(int stage)
        {
            var section = new List<Vector3>();
            for (int i = 0; i < route.Count; i++)
                if (routeStages[i] == stage) section.Add(route[i]);
            return section;
        }

        private static List<int> JumpSpots()
        {
            var spots = new List<int>();
            float travelled = 0f, next = 26f;
            for (int i = 1; i < route.Count - 1; i++)
            {
                travelled += FlatDistance(route[i - 1], route[i]);
                if (travelled < next) continue;
                Vector3 a = route[Mathf.Max(i - 4, 0)], b = route[Mathf.Min(i + 4, route.Count - 1)];
                float span = FlatDistance(a, b);
                float slope = span > .1f ? Mathf.Abs(b.y - a.y) / span : 1f;
                if (slope > .48f) { next = travelled + 8f; continue; }
                spots.Add(i);
                next = travelled + 25f + ((i * 37) % 13);
            }
            return spots;
        }

        private static void JumpMarkers(Transform parent, List<int> spots)
        {
            var group = new GameObject("Прыжковые места — " + spots.Count).transform;
            group.SetParent(parent, false);
            for (int i = 0; i < spots.Count; i++)
            {
                var marker = new GameObject("Прыжок " + (i + 1)).transform;
                marker.SetParent(group, false);
                marker.localPosition = route[spots[i]];
            }
        }

        private static void WaypointGuide(Transform parent)
        {
            var group = new GameObject("Маршрут — " + route.Count + " вейпоинтов").transform;
            group.SetParent(parent, false);
            for (int i = 0; i < route.Count; i++)
            {
                var point = new GameObject("Вейпоинт " + (i + 1)).transform;
                point.SetParent(group, false);
                point.localPosition = route[i];
            }
        }

        private static int Checkpoints(Transform parent, Mesh cairn)
        {
            float length = RouteLength();
            var cumulative = new float[route.Count];
            for (int i = 1; i < route.Count; i++)
                cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            var marks = new List<int>();
            for (float target = 0f; target <= length + .1f; target += 400f)
            {
                int index = 0;
                while (index < cumulative.Length - 1 && cumulative[index + 1] <= target) index++;
                if (marks.Count > 0 && index <= marks[marks.Count - 1] + 4) continue;
                marks.Add(index);
            }
            if (marks.Count == 0 || cumulative[marks[marks.Count - 1]] < length - 120f) marks.Add(route.Count - 1);
            var group = new GameObject("Чекпоинты — каждые ~400 м").transform;
            group.SetParent(parent, false);
            var random = new System.Random(7727);
            for (int k = 0; k < marks.Count; k++)
            {
                int index = marks[k];
                Vector3 ahead = route[Mathf.Min(index + 3, route.Count - 1)];
                Vector3 behind = route[Mathf.Max(index - 3, 0)];
                Vector3 direction = new Vector3(ahead.x - behind.x, 0f, ahead.z - behind.z);
                if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
                direction.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                bool exposed = routeStages[index] == 5 || routeStages[index] == 7;
                float offset = exposed ? 0f : 4.6f + (float)random.NextDouble() * 2.4f;
                Vector3 position = route[index] + side * offset + Vector3.down * .35f;
                float scale = exposed ? 3.1f : 4.2f + (float)random.NextDouble() * .9f;
                var item = Place(group, "Чекпоинт " + (k + 1) + " — " + RouteStageNames[routeStages[index]],
                    cairn, boulderMat, position, Vector3.one * scale);
                item.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                var trigger = item.gameObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(8f, 5f, 8f);
                trigger.center = new Vector3(0f, 2.5f, 0f);
            }
            return marks.Count;
        }

        private static void GoatTrail(Transform parent)
        {
            var trail = Group(parent, "Козлиная тропа — сквозь врата");
            Mesh tread = Save(SkyBridgeMesh.Walkway(route, 5.2f, .14f, 1.5f), "RouteWalkway.asset");
            Place(trail, "Полотно тропы по всему маршруту", tread, trailMat, Vector3.zero, Vector3.one, true);
            var crest = RouteSection(6);
            if (crest.Count > 1)
            {
                Mesh crown = Save(SkyBridgeMesh.Walkway(crest, 9.5f, .08f, 2.6f), "CrownWalkway.asset");
                Place(trail, "Козья тропа по своду арки", crown, trailMat, Vector3.zero, Vector3.one, true);
            }
            WaypointGuide(parent);
            var cairn = Save(SkyBridgeMesh.Cairn(97), "Cairn.asset");
            int checkpoints = Checkpoints(parent, cairn);
            var finish = Group(parent, "Финиш у валунов");
            var finishRandom = new System.Random(3089);
            Vector3 end = route[route.Count - 1];
            for (int i = 0; i < 7; i++)
            {
                float angle = i / 7f * Mathf.PI * 2f + .4f;
                float radius = 7f + (float)finishRandom.NextDouble() * 9f;
                float stoneScale = 1.6f + (float)finishRandom.NextDouble() * 1.6f;
                float sx = end.x + Mathf.Cos(angle) * radius;
                float sz = end.z + Mathf.Sin(angle) * radius;
                Vector3 ground = new Vector3(sx, Mathf.Max(SkyBridgeMesh.TerrainHeight(sx, sz),
                    SkyBridgeMesh.Walkable(sx, sz)), sz);
                Vector3 center = ground - SkyBridgeMesh.TerrainNormal(sx, sz) * (.3f + 2.45f * stoneScale);
                var stone = Place(finish, "Финишный валун", satelliteMesh, boulderMat, center,
                    Vector3.one * stoneScale);
                stone.localRotation = Quaternion.Euler(-6f + (float)finishRandom.NextDouble() * 12f,
                    (float)finishRandom.NextDouble() * 360f, -6f + (float)finishRandom.NextDouble() * 12f);
            }
            Debug.Log("GOAT_TRAIL SkyBridge waypoints=" + route.Count + " length~" + Mathf.RoundToInt(RouteLength())
                + "м (горизонталь " + Mathf.RoundToInt(RouteLengthFlat()) + "м) checkpoints=" + checkpoints);
        }

        private static void Volumes(Transform terrainRoot, Transform stoneRoot, Mesh[] spurs)
        {
            var random = new System.Random(8123);
            var ravineLarge = Save(SkyBridgeMesh.Ravine(22f, 40f, 34f, 917), "RavineLarge.asset");
            var ravineSmall = Save(SkyBridgeMesh.Ravine(15f, 28f, 24f, 313), "RavineSmall.asset");
            var shadows = Group(terrainRoot, "Теневые ущелья между террасами");
            int index = 0;
            for (float z = -780f; z <= 780f; z += 115f)
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    float band = 55f + (float)random.NextDouble() * 120f;
                    float x = SkyBridgeMesh.RiverCenter(z) + side * (SkyBridgeMesh.ChannelHalf(z) + band);
                    if (NearRoute(x, z, 34f)) continue;
                    float y = SkyBridgeMesh.TerrainHeight(x, z);
                    var item = Place(shadows, "Теневое ущелье между террасами",
                        index % 2 == 0 ? ravineLarge : ravineSmall, rock,
                        new Vector3(x, y - 2f, z), Vector3.one * (.75f + (float)random.NextDouble() * .65f));
                    item.localRotation = Quaternion.LookRotation(new Vector3(-side, 0f, 0f), Vector3.up);
                    index++;
                }
            var outliers = Group(stoneRoot, "Скалы-останцы в долине");
            for (int i = 0; i < 16; i++)
            {
                float z = -720f + (float)random.NextDouble() * 1440f;
                float side = random.Next(2) == 0 ? -1f : 1f;
                float low = SkyBridgeMesh.RiverHalf(z) + 16f;
                float high = SkyBridgeMesh.ChannelHalf(z) - 20f;
                if (high <= low) continue;
                float d = Mathf.Lerp(low, high, (float)random.NextDouble());
                float x = SkyBridgeMesh.RiverCenter(z) + side * d;
                float stoneScale = .34f + (float)random.NextDouble() * .3f;
                if (NearRoute(x, z, 18f + stoneScale * 95f)) continue;
                float y = SkyBridgeMesh.TerrainHeight(x, z);
                var item = Place(outliers, "Скала-останец", spurs[i % spurs.Length], boulderMat,
                    new Vector3(x, y - 12f, z), Vector3.one * stoneScale);
                item.localRotation = Quaternion.Euler(-6f + (float)random.NextDouble() * 12f,
                    (float)random.NextDouble() * 360f, -6f + (float)random.NextDouble() * 12f);
            }
        }

        private static void CrownShelves(Transform parent, Mesh[] ledges)
        {
            var crest = RouteSection(6);
            if (crest.Count < 12) return;
            var random = new System.Random(6151);
            var group = Group(parent, "Полки козьей тропы по своду");
            int step = Mathf.Max(6, crest.Count / 10);
            int index = 0;
            for (int i = 4; i < crest.Count - 4; i += step)
            {
                Vector3 ahead = crest[Mathf.Min(i + 3, crest.Count - 1)];
                Vector3 behind = crest[Mathf.Max(i - 3, 0)];
                Vector3 direction = new Vector3(ahead.x - behind.x, 0f, ahead.z - behind.z);
                if (direction.sqrMagnitude < .001f) continue;
                direction.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                for (int s = -1; s <= 1; s += 2)
                {
                    float offset = 10f + (float)random.NextDouble() * 7f;
                    int kind = index++ % ledges.Length;
                    var shelf = Place(group, "Зелёная полка на своде", ledges[kind], ledge,
                        crest[i] + side * (s * offset) + Vector3.down * 1.5f,
                        Vector3.one * (.85f + (float)random.NextDouble() * .45f));
                    shelf.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                }
            }
        }

        private static void Landmark(Transform parent, Mesh mesh, float x, float z, float scale)
        {
            var item = Place(parent, "Камень-ориентир", mesh, boulderMat,
                new Vector3(x, SkyBridgeMesh.Walkable(x, z) - .3f, z), Vector3.one * scale);
            item.localRotation = Quaternion.Euler(0f, Mathf.Repeat(x * 7f + z * 3f, 360f), 0f);
        }

        private static void Pose(Camera camera, Vector3 position, Vector3 target)
        {
            camera.transform.position = Lift(position) * WorldScale;
            camera.transform.LookAt(target * WorldScale);
        }

        private static Vector3 Lift(Vector3 position)
        {
            float ground = Mathf.Max(SkyBridgeMesh.TerrainHeight(position.x, position.z),
                SkyBridgeMesh.Walkable(position.x, position.z));
            if (ground > position.y - 14f) position.y = ground + 14f;
            return position;
        }

        private static bool SightClear(Vector3 position, Vector3 target)
        {
            for (int step = 1; step <= 14; step++)
            {
                Vector3 point = Vector3.Lerp(position, target, step / 15f);
                float solid = Mathf.Max(SkyBridgeMesh.TerrainHeight(point.x, point.z),
                    SkyBridgeMesh.Walkable(point.x, point.z));
                if (solid > point.y - 8f) return false;
            }
            return true;
        }

        private static void OverviewPose(Camera camera)
        {
            Vector3 target = new Vector3(0f, 170f, 0f);
            var candidates = new List<Vector3>
            {
                new Vector3(0f, 240f, 1680f),
                new Vector3(0f, 260f, 1860f),
                new Vector3(0f, 220f, 1440f),
                new Vector3(60f, 250f, 1780f),
                new Vector3(-60f, 250f, 1780f),
                new Vector3(0f, 420f, 2100f)
            };
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 position = Lift(candidates[i]);
                if (!SightClear(position, target)) continue;
                Pose(camera, position, target);
                return;
            }
            Pose(camera, new Vector3(0f, 400f, 2200f), target);
        }

        private static void NoShadow(Transform item)
        {
            var renderer = item.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
