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
        private static Material rock, ledge, snow, waterMat, fallMat, mistMat, foamMat, needlesMat, trunkMat;
        private static Material boulderMat, gravelMat, nicheMat, trailMat;
        private static Mesh puffMesh, nicheMesh;

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
            var terrainRoot = Group(root, "Склоны ущелья, террасы и зелёные полки");
            var archRoot = Group(root, "Гребень и сквозной проём");
            var forestRoot = Group(root, "Еловые склоны");
            var waterRoot = Group(root, "Бирюзовая река, заводи и водопады");
            var mistRoot = Group(root, "Туман на дне ущелья");
            var stoneRoot = Group(root, "Валуны и осыпи");
            var farRoot = Group(root, "Дальние снежные хребты");

            rock = Landscape("SkyBridgeRock", new Color(1.56f, 1.4f, 1.1f), new Color(.27f, .37f, .19f), Color.white, 100000f, .9f, .03f, .2f, 480f, .75f);
            ledge = Landscape("GreenLedge", new Color(1.48f, 1.32f, 1.02f), new Color(.44f, .57f, .29f), Color.white, 100000f, 1.5f, .045f, .1f, -100000f, .85f);
            snow = Landscape("SnowRange", new Color(.6f, .66f, .76f), new Color(.14f, .3f, .16f), new Color(.97f, .99f, 1f), 300f, .75f, .02f, .1f, 330f, 0f);
            snow.SetFloat("_HazeStrength", .8f);
            waterMat = Fluid("TurquoiseRiver", "GoatDescent/SkyBridgeWater");
            waterMat.SetColor("_Shallow", new Color(.15f, .66f, .62f));
            waterMat.SetColor("_Deep", new Color(.04f, .32f, .36f));
            waterMat.SetColor("_Foam", new Color(.94f, .99f, 1f));
            waterMat.SetFloat("_Opacity", .84f);
            waterMat.SetColor("_HazeColor", new Color(.53f, .74f, .91f));
            fallMat = Fluid("Cascade", "GoatDescent/SkyBridgeWaterfall");
            mistMat = Fluid("ValleyMistPuff", "GoatDescent/SkyBridgeMist");
            foamMat = Fluid("CascadeFoam", "GoatDescent/SkyBridgeMist");
            foamMat.SetColor("_Color", Color.white);
            foamMat.SetColor("_Shadow", new Color(.84f, .9f, .95f));
            foamMat.SetFloat("_Opacity", .62f);
            foamMat.SetFloat("_NoiseScale", .09f);
            needlesMat = Simple("FirNeedles", new Color(.23f, .25f, .21f), .04f);
            trunkMat = Simple("FirTrunk", new Color(.19f, .16f, .13f), .05f);
            mistMat.SetColor("_Color", new Color(.93f, .96f, .97f));
            mistMat.SetColor("_Shadow", new Color(.62f, .7f, .74f));
            mistMat.SetFloat("_Opacity", .5f);
            foamMat.SetColor("_Shadow", new Color(.62f, .7f, .76f));
            boulderMat = Landscape("BoulderRock", new Color(1.5f, 1.34f, 1.02f), new Color(.2f, .27f, .19f), Color.white, 100000f, .25f, .05f, .14f, -100000f, 0f);
            gravelMat = Landscape("GravelBank", new Color(1.45f, 1.38f, 1.24f), new Color(.2f, .28f, .2f), Color.white, 100000f, .2f, .18f, .08f, -100000f, 0f);
            var screeMat = Landscape("ScreeStone", new Color(1.42f, 1.36f, 1.22f), new Color(.2f, .28f, .2f), Color.white, 100000f, .15f, .22f, .06f, -100000f, 0f);
            nicheMat = Landscape("WetNiche", new Color(.5f, .46f, .42f), new Color(.14f, .16f, .14f), Color.white, 100000f, .1f, .05f, .12f, -100000f, 0f);
            trailMat = Simple("TrailEarth", new Color(.42f, .36f, .27f), .06f);
            puffMesh = Save(SkyBridgeMesh.MistPuff(1f, 5), "MistPuff.asset");
            nicheMesh = Save(SkyBridgeMesh.Niche(34f, 64f, 5), "FallNiche.asset");

            var ledgeMeshes = new[]
            {
                Save(SkyBridgeMesh.Ledge(30f, 3), "LedgeLarge.asset"),
                Save(SkyBridgeMesh.Ledge(22f, 17), "LedgeMedium.asset"),
                Save(SkyBridgeMesh.Ledge(15f, 29), "LedgeSmall.asset")
            };

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

            var archForest = new SkyBridgeMesh.TreeCluster();
            CrestForest(archForest);
            ArchLedges(archRoot, archForest, ledgeMeshes);
            PlaceForest(forestRoot, "Лес гребня и полок арки", "ArchCrestNeedles.asset", "ArchCrestTrunks.asset", archForest);
            SlopeForest(forestRoot, "Лес западного склона", "WestSlopeNeedles.asset", "WestSlopeTrunks.asset", 7717, -1f, 112);
            SlopeForest(forestRoot, "Лес восточного склона", "EastSlopeNeedles.asset", "EastSlopeTrunks.asset", 4139, 1f, 112);
            SlopeForest(forestRoot, "Лес дальних склонов", "FarSlopeNeedles.asset", "FarSlopeTrunks.asset", 2081, 1f, 70);
            PlateauGroves(forestRoot, "PlateauWestNeedles.asset", "PlateauWestTrunks.asset", 6151, -1f);
            PlateauGroves(forestRoot, "PlateauEastNeedles.asset", "PlateauEastTrunks.asset", 3319, 1f);

            Place(stoneRoot, "Валуны и осыпи ущелья", Save(SkyBridgeMesh.BoulderField(130, 331), "BoulderField.asset"),
                boulderMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Галька и подмытые берега", Save(SkyBridgeMesh.GravelBars(), "GravelBars.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Мели в русле", Save(SkyBridgeMesh.Shoals(28, 617), "Shoals.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Камни-переправа", Save(SkyBridgeMesh.Fords(733), "Fords.asset"),
                gravelMat, Vector3.zero, Vector3.one);
            Place(stoneRoot, "Осыпи в кулуарах", Save(SkyBridgeMesh.Scree(26, 14, 811), "Scree.asset"),
                screeMat, Vector3.zero, Vector3.one);
            var corniceForest = new SkyBridgeMesh.TreeCluster();
            Cornices(forestRoot, ledgeMeshes, corniceForest);
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
            Place(stoneRoot, "Валуны-ступени маршрута",
                Save(SkyBridgeMesh.JumpSteps(TrailPoints(), 431), "JumpSteps.asset"),
                boulderMat, Vector3.zero, Vector3.one);

            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 50f; camera.nearClipPlane = .5f; camera.farClipPlane = 5400f;
            camera.clearFlags = CameraClearFlags.Skybox; camera.depthTextureMode = DepthTextureMode.Depth;
            camera.allowHDR = true;
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.coolTint = new Color(.94f, .99f, 1.05f);
            post.warmTint = new Color(1.05f, 1f, .92f);
            post.saturation = 1.18f;
            post.bloomIntensity = .65f;
            post.bloomThreshold = .62f;
            post.vignette = .38f;
            post.contrast = 1.1f;
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
            material.SetColor("_HazeColor", new Color(.53f, .74f, .91f));
            material.SetFloat("_HazeStart", 700f);
            material.SetFloat("_HazeEnd", 2600f);
            material.SetFloat("_HazeStrength", .55f);
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
            RenderSettings.ambientSkyColor = new Color(.44f, .53f, .66f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .39f, .4f);
            RenderSettings.ambientGroundColor = new Color(.2f, .23f, .21f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.53f, .64f, .72f); RenderSettings.fogDensity = .00029f;
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.4f, .63f, .9f));
            sky.SetColor("_SkyHorizon", new Color(.66f, .81f, .93f));
            sky.SetColor("_GroundColor", new Color(.83f, .81f, .73f));
            sky.SetColor("_SunColor", new Color(1f, .95f, .82f));
            sky.SetFloat("_SunSize", .12f);
            sky.SetFloat("_CloudDensity", .74f);
            sky.SetVector("_SunDir", new Vector4(-.52f, .62f, .59f, 0f));
            RenderSettings.skybox = Save(sky, "SkyBridgeSky.mat");
            var sun = new GameObject("Солнце над гребнем").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .88f, .7f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, 138f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Отражённый свет склонов").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.62f, .74f, .88f);
            fill.intensity = .32f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(24f, -46f, 0f);
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
                    cluster.Add(px, y - .6f, pz, TreeSize(random, 9f, 24f), (float)random.NextDouble() * 360f);
                }
            }
            PlaceForest(parent, "Лесной массив на плато", needlesName, trunksName, cluster);
        }

        private static void Cornices(Transform parent, Mesh[] ledges, SkyBridgeMesh.TreeCluster forest)
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
                var item = Place(parent, "Висячий травяной козырёк", ledges[index++ % ledges.Length], ledge,
                    new Vector3(x, y - 1.2f, z), Vector3.one * (.65f + (float)random.NextDouble() * .5f));
                item.localRotation = Quaternion.Euler(-18f + (float)random.NextDouble() * 10f,
                    (float)random.NextDouble() * 360f, (float)random.NextDouble() * 22f - 11f);
                int trees = 1 + random.Next(2);
                for (int tree = 0; tree < trees; tree++)
                {
                    float ox = ((float)random.NextDouble() - .5f) * 22f;
                    float oz = ((float)random.NextDouble() - .5f) * 14f;
                    forest.Add(x + ox, y + .8f, z + oz, TreeSize(random, 6f, 14f),
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
                    cluster.Add(px, SkyBridgeMesh.RidgeTop(px, z) - 1f,
                        z + ((float)random.NextDouble() - .5f) * 6f,
                        TreeSize(random, 16f, 26f), (float)random.NextDouble() * 360f);
                }
            float[] giantX = { -320f, -215f, -150f, 155f, 320f };
            for (int i = 0; i < giantX.Length; i++)
            {
                float x = giantX[i];
                float z = ((float)random.NextDouble() - .5f) * 24f;
                cluster.Add(x + ((float)random.NextDouble() - .5f) * 18f, SkyBridgeMesh.RidgeTop(x, z) - 1f, z,
                    30f + (float)random.NextDouble() * 10f, (float)random.NextDouble() * 360f);
            }
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float gx = side * (152f + (float)random.NextDouble() * 42f);
                float gz = (random.Next(2) == 0 ? -1f : 1f) * (236f + (float)random.NextDouble() * 52f);
                cluster.Add(gx, SkyBridgeMesh.TerrainHeight(gx, gz) - 1f, gz,
                    32f + (float)random.NextDouble() * 8f, (float)random.NextDouble() * 360f);
            }
        }

        private static float TreeSize(System.Random random, float min, float max)
            => min + (max - min) * Mathf.Pow((float)random.NextDouble(), 2.2f);

        private static void ArchLedges(Transform parent, SkyBridgeMesh.TreeCluster forest, Mesh[] ledges)
        {
            var random = new System.Random(9137);
            int index = 0;
            for (int face = 0; face < 2; face++)
                for (int tier = 0; tier < 5; tier++)
                    for (int spot = 0; spot < 12; spot++)
                    {
                        float u = .1f + tier * .17f + (float)random.NextDouble() * .05f;
                        float x = (206f + (float)random.NextDouble() * 614f) * (random.Next(2) == 0 ? 1f : -1f);
                        Vector3 point = SkyBridgeMesh.RidgeFace(x, Mathf.Min(1f, u), face == 0);
                        float outward = face == 0 ? -1f : 1f;
                        var item = Place(parent, "Зелёная полка на склоне арки", ledges[index++ % ledges.Length], ledge,
                            new Vector3(point.x, point.y, point.z + outward * 4f),
                            Vector3.one * (1.7f + (float)random.NextDouble() * .7f));
                        item.localRotation = Quaternion.Euler(0f, 0f, (float)(random.NextDouble() * 8f - 4f));
                        int trees = 5 + random.Next(2);
                        for (int tree = 0; tree < trees; tree++)
                        {
                            float ox = ((float)random.NextDouble() - .5f) * 44f;
                            float oz = ((float)random.NextDouble() - .5f) * 5f;
                            forest.Add(point.x + ox, point.y + .5f, point.z + outward * 4f + oz,
                                TreeSize(random, 20f, 30f), (float)random.NextDouble() * 360f);
                        }
                    }
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
                if (Mathf.PerlinNoise(x * .009f, z * .009f + 7.7f) < .34f) continue;
                cluster.Add(x, y - .6f, z, TreeSize(random, 9f, 22f), (float)random.NextDouble() * 360f);
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
            for (int i = 0; i < 100; i++)
            {
                float z = -1100f + (float)random.NextDouble() * 2200f;
                float x = SkyBridgeMesh.RiverCenter(z) + ((float)random.NextDouble() - .5f) * 170f;
                float y = 3f + (float)random.NextDouble() * 12f;
                var item = Place(parent, "Клуб тумана", puff, mistMat, new Vector3(x, y, z),
                    new Vector3(88f + (float)random.NextDouble() * 92f, 10f + (float)random.NextDouble() * 7f,
                        88f + (float)random.NextDouble() * 92f));
                item.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                NoShadow(item);
            }
            for (int i = 0; i < 14; i++)
            {
                float z = -260f + i * 40f;
                var item = Place(parent, "Туман под сводом", puff, mistMat,
                    new Vector3(SkyBridgeMesh.RiverCenter(z) + ((i * 41) % 90) - 45f, 4f + (i % 3) * 4f, z),
                    new Vector3(86f, 10f, 62f));
                item.localRotation = Quaternion.Euler(0f, i * 41f, 0f);
                NoShadow(item);
            }
        }

        private static List<Vector3> TrailPoints() => new List<Vector3>
        {
            new Vector3(70f, 150f, 700f),
            new Vector3(150f, 145f, 640f),
            new Vector3(185f, 135f, 570f),
            new Vector3(80f, 110f, 470f),
            new Vector3(140f, 95f, 420f),
            new Vector3(185f, 80f, 370f),
            new Vector3(140f, 62f, 325f),
            new Vector3(95f, 45f, 285f),
            new Vector3(120f, 30f, 245f),
            new Vector3(95f, 18f, 205f),
            new Vector3(75f, 10f, 165f),
            new Vector3(60f, 6f, 125f),
            new Vector3(55f, 5f, 90f),
            new Vector3(35f, 5f, 25f),
            new Vector3(18f, 5f, -5f),
            new Vector3(-5f, 5f, -40f),
            new Vector3(-25f, 5f, -80f),
            new Vector3(-40f, 5f, -125f),
            new Vector3(-30f, 5f, -170f),
            new Vector3(-55f, 6f, -255f),
            new Vector3(-235f, 30f, -175f),
            new Vector3(-285f, 110f, -150f),
            new Vector3(-320f, 200f, -120f),
            new Vector3(-290f, 290f, -90f),
            new Vector3(-240f, 380f, -65f),
            new Vector3(-215f, 450f, -45f),
            new Vector3(-255f, 510f, -30f),
            new Vector3(-310f, 555f, -18f),
            new Vector3(-350f, 590f, -8f),
            new Vector3(-300f, 640f, 2f),
            new Vector3(-240f, 690f, 6f),
            new Vector3(-175f, 740f, 8f),
            new Vector3(-110f, 760f, 8f),
            new Vector3(-45f, 770f, 5f),
            new Vector3(110f, 770f, -6f),
            new Vector3(255f, 700f, -14f),
            new Vector3(390f, 520f, -28f),
            new Vector3(500f, 300f, -60f),
            new Vector3(580f, 190f, -200f),
            new Vector3(640f, 168f, -420f)
        };

        private static void GoatTrail(Transform parent)
        {
            var waypoints = TrailPoints();
            float length = 0f;
            for (int i = 0; i < waypoints.Count - 1; i++) length += Vector3.Distance(waypoints[i], waypoints[i + 1]);
            GoatTrailBuilder.Build(parent, "Козлиная тропа — сквозь врата",
                p => SkyBridgeMesh.Walkable(p.x, p.z), trailMat, waypoints, 4f, .35f, 9f, true,
                new Color(.34f, .31f, .26f));
            var cairn = Save(SkyBridgeMesh.Cairn(97), "Cairn.asset");
            Landmark(parent, cairn, 70f, 700f, 5.1f);
            Landmark(parent, cairn, 150f, 330f, 4.2f);
            Landmark(parent, cairn, 80f, 110f, 3.9f);
            Landmark(parent, cairn, -110f, 5f, 5.4f);
            Landmark(parent, cairn, 100f, -5f, 4.8f);
            Landmark(parent, cairn, 300f, -20f, 4.2f);
            Landmark(parent, cairn, 640f, -420f, 4.8f);
            Debug.Log("GOAT_TRAIL SkyBridge waypoints=" + waypoints.Count + " length~" + Mathf.RoundToInt(length) + "м");
        }

        private static void Landmark(Transform parent, Mesh mesh, float x, float z, float scale)
        {
            var item = Place(parent, "Камень-ориентир", mesh, boulderMat,
                new Vector3(x, SkyBridgeMesh.Walkable(x, z) - .3f, z), Vector3.one * scale);
            item.localRotation = Quaternion.Euler(0f, Mathf.Repeat(x * 7f + z * 3f, 360f), 0f);
        }

        private static void Pose(Camera camera, Vector3 position, Vector3 target)
        {
            camera.transform.position = Lift(position);
            camera.transform.LookAt(target);
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
