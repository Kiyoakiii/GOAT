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
            var cliffs = new GameObject("Скальный массив и рёбра").transform; cliffs.SetParent(root, false);
            var grotto = new GameObject("Грот").transform; grotto.SetParent(root, false);
            var shelves = new GameObject("Зелёные полки").transform; shelves.SetParent(root, false);
            var valley = new GameObject("Долина и река").transform; valley.SetParent(root, false);
            var forest = new GameObject("Леса").transform; forest.SetParent(root, false);
            var stones = new GameObject("Валуны").transform; stones.SetParent(root, false);
            var far = new GameObject("Дальние снежные пики").transform; far.SetParent(root, false);

            Vector3 sunDir = -(Quaternion.Euler(42f, 30f, 0f) * Vector3.forward);
            var rock = RockMaterial("WarmTerraceRock", new Color(.8f, .64f, .44f), new Color(.97f, .81f, .55f),
                new Color(.38f, .5f, .32f), .022f, .45f, sunDir);
            var lightRock = RockMaterial("SunlitTerraceRock", new Color(.86f, .7f, .48f), new Color(1f, .84f, .58f),
                new Color(.42f, .53f, .35f), .026f, .38f, sunDir);
            var grottoDark = Simple("GrottoDark", new Color(.02f, .021f, .022f), 0f);
            var meadow = MeadowMaterial("ValleyMeadow", new Color(.37f, .51f, .3f), new Color(.53f, .62f, .4f),
                new Color(.45f, .4f, .3f), sunDir);
            var shelfGrass = MeadowMaterial("TerraceGrass", new Color(.38f, .5f, .32f), new Color(.52f, .6f, .42f),
                new Color(.44f, .4f, .32f), sunDir);
            var ledgeTuft = MeadowMaterial("LedgeTuft", new Color(.33f, .46f, .29f), new Color(.46f, .56f, .4f),
                new Color(.38f, .44f, .32f), sunDir);
            var needles = Simple("FirNeedles", new Color(.13f, .23f, .13f), .04f);
            var trunks = Simple("FirTrunk", new Color(.2f, .16f, .12f), .04f);
            var water = WaterMaterial();
            var snow = FarMaterial("SnowCaps", new Color(.9f, .93f, .97f));
            var farRock = FarMaterial("DistantRock", new Color(.54f, .61f, .72f));
            var scree = Simple("ScreeApron", new Color(.62f, .54f, .44f), .05f);
            var trailDirt = Simple("TrailDirt", new Color(.55f, .44f, .28f), .06f);

            Emit(cliffs, "Уступчатый массив", "Massif", TerracePeakMesh.Massif(), rock, true);
            Emit(cliffs, "Скальные фасады", "Facades", TerracePeakMesh.Facades(), rock, false);
            Emit(cliffs, "Скальные рёбра", "Ribs", TerracePeakMesh.Ribs(), lightRock, false);
            Emit(cliffs, "Скальные карнизы", "Ledges", TerracePeakMesh.Ledges(), lightRock, false);
            Emit(cliffs, "Осыпные шлейфы", "Talus", TerracePeakMesh.Talus(), scree, false);
            Emit(cliffs, "Обвалы ярусов", "Collapse", TerracePeakMesh.Collapse(), rock, false);
            Emit(cliffs, "Скальный гребень вершины", "Spires", TerracePeakMesh.Spires(), rock, false);
            Emit(cliffs, "Площадка перед гротом", "Balcony", TerracePeakMesh.Balcony(), lightRock, false);
            Emit(grotto, "Скальный козырёк грота", "GrottoBrow", TerracePeakMesh.GrottoBrow(), rock, false);
            Emit(grotto, "Тёмная глубина грота", "GrottoInterior", TerracePeakMesh.GrottoInterior(), grottoDark, false);
            Emit(shelves, "Травяные полки", "ShelfGrass", TerracePeakMesh.ShelfGrass(), shelfGrass, false);
            Emit(shelves, "Свисающая зелень", "MossFringe", TerracePeakMesh.MossFringe(), ledgeTuft, false);
            Emit(valley, "Долина у подножия", "Valley", TerracePeakMesh.Valley(), meadow, true);
            Emit(valley, "Бирюзовая река", "River", TerracePeakMesh.River(), water, false);
            Emit(forest, "Кроны елей", "ForestNeedles", TerracePeakMesh.Forest(false), needles, false);
            Emit(forest, "Стволы елей", "ForestTrunks", TerracePeakMesh.Forest(true), trunks, false);
            Emit(stones, "Валуны", "Boulders", TerracePeakMesh.Boulders(), rock, false);
            Emit(far, "Дальние хребты", "FarPeaks", TerracePeakMesh.FarPeaks(false), farRock, false);
            Emit(far, "Снежные шапки", "SnowCaps", TerracePeakMesh.FarPeaks(true), snow, false);

            var trailPoints = TrailWaypoints();
            var trail = GoatTrailBuilder.Build(root, "Козья тропа — восхождение", TerracePeakMesh.GroundHeight,
                trailDirt, trailPoints, 3.6f, .5f, 9f, true, new Color(.42f, .34f, .2f));
            trail.gameObject.isStatic = true;
            var landmarkPoints = new List<Vector3>();
            var landmarkScales = new List<float>();
            int[] anchors = { 4, 10, 16, 23, 30, 36 };
            for (int index = 0; index < anchors.Length; index++)
            {
                int at = Mathf.Min(anchors[index], trailPoints.Count - 1);
                Vector3 point = trailPoints[at];
                Vector3 back = trailPoints[Mathf.Max(0, at - 1)];
                Vector3 side = Vector3.Cross(Vector3.up, (point - back).normalized);
                landmarkPoints.Add(point + side * (7f + index * 1.4f));
                landmarkScales.Add(4.5f + index * .9f);
            }
            Emit(stones, "Камни-ориентиры тропы", "Landmarks",
                TerracePeakMesh.RockField(landmarkPoints, landmarkScales, 901), rock, false);

            float trailLength = 0f;
            for (int index = 1; index < trailPoints.Count; index++)
                trailLength += Vector3.Distance(trailPoints[index - 1], trailPoints[index]);
            Debug.Log("GOAT_TRAIL TerracePeak waypoints=" + trailPoints.Count + " length~"
                + Mathf.RoundToInt(trailLength) + "м");

            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 52f;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 3200f;
            camera.transform.position = new Vector3(120f, 335f, -640f);
            camera.transform.LookAt(new Vector3(0f, 285f, 0f));
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
            camera.transform.position = new Vector3(70f, 285f, -540f);
            camera.transform.LookAt(new Vector3(0f, 275f, -60f));
            Render(camera, "TerracePeak-grotto");
            camera.transform.position = new Vector3(430f, 375f, -355f);
            camera.transform.LookAt(new Vector3(0f, 275f, 0f));
            Render(camera, "TerracePeak-terrace");
            camera.transform.position = new Vector3(10f, 60f, -620f);
            camera.transform.LookAt(new Vector3(0f, 215f, 0f));
            Render(camera, "TerracePeak-valley");
            camera.transform.position = new Vector3(-395f, 340f, 610f);
            camera.transform.LookAt(new Vector3(0f, 235f, 0f));
            Render(camera, "TerracePeak-back");
            camera.transform.position = new Vector3(-70f, 545f, -990f);
            camera.transform.LookAt(new Vector3(0f, 290f, 240f));
            Render(camera, "TerracePeak-horizon");
            Debug.Log("TERRACE_PEAK_STATIC_SAVED scene=" + ScenePath);
        }

        private static float Crossing(int tier, float nearDegrees, float search)
        {
            float best = nearDegrees, lowest = 99999f;
            for (float offset = -search; offset <= search; offset += 2f)
            {
                float angle = nearDegrees + offset;
                float height = TerracePeakMesh.WallHeight(tier, angle);
                if (height < lowest) { lowest = height; best = angle; }
            }
            return best;
        }

        private static void Climb(List<Vector3> trail, int tier, float nearDegrees, float exitOffset, float search = 26f)
        {
            float crossing = Crossing(tier, nearDegrees, search);
            trail.Add(TerracePeakMesh.TierFace(tier, crossing + 8f, .12f));
            trail.Add(TerracePeakMesh.TierFace(tier, crossing - 4f, .62f));
            trail.Add(TerracePeakMesh.TierLedge(tier, crossing - exitOffset, .55f));
        }

        private static List<Vector3> TrailWaypoints()
        {
            var trail = new List<Vector3>();
            trail.Add(TerracePeakMesh.BankPoint(-72f, 110f));
            trail.Add(TerracePeakMesh.ValleyPoint(-34f, 392f));
            trail.Add(TerracePeakMesh.ValleyPoint(0f, 362f));
            Climb(trail, 0, 14f, 32f);
            Climb(trail, 1, -6f, -20f);
            Climb(trail, 2, -34f, -30f);
            Climb(trail, 3, -60f, -20f);
            trail.Add(TerracePeakMesh.TierLedge(3, -98f, .72f));
            trail.Add(TerracePeakMesh.CaveMouth(-5f, 10f));
            trail.Add(TerracePeakMesh.CaveMouth(2f, 46f));
            trail.Add(TerracePeakMesh.CaveMouth(-2f, 84f));
            trail.Add(TerracePeakMesh.CaveMouth(4f, 44f));
            trail.Add(TerracePeakMesh.CaveMouth(0f, 12f));
            trail.Add(TerracePeakMesh.TierLedge(3, -110f, .55f));
            Climb(trail, 4, -132f, -14f, 14f);
            trail.Add(TerracePeakMesh.TierLedge(4, -114f, .6f));
            Climb(trail, 5, -120f, -12f, 12f);
            trail.Add(TerracePeakMesh.TierLedge(5, -92f, .6f));
            trail.Add(TerracePeakMesh.TierLedge(5, -74f, .72f));
            Climb(trail, 6, -56f, -16f, 12f);
            trail.Add(TerracePeakMesh.TierLedge(6, -22f, .62f));
            Climb(trail, 7, -6f, -18f, 10f);
            trail.Add(TerracePeakMesh.TierLedge(7, 26f, .62f));
            trail.Add(TerracePeakMesh.TierLedge(7, 38f, .86f));
            return trail;
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
            material.SetVector("_SunDir", sunDir);
            material.SetColor("_ShadowTint", new Color(.47f, .43f, .4f));
            material.SetColor("_DeepTint", new Color(.35f, .35f, .34f));
            material.SetColor("_HazeColor", new Color(.82f, .83f, .78f));
            return Save(material, name + ".mat");
        }

        private static Material MeadowMaterial(string name, Color grass, Color light, Color earth, Vector3 sunDir)
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakMeadow")) { name = name };
            material.SetColor("_GrassTint", grass);
            material.SetColor("_GrassLight", light);
            material.SetColor("_EarthTint", earth);
            material.SetVector("_SunDir", sunDir);
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
            return Save(material, "TurquoiseRiver.mat");
        }

        private static Material FarMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("GoatDescent/TerracePeakFar")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetColor("_HazeColor", new Color(.82f, .86f, .93f));
            material.SetFloat("_HazeStart", 500f);
            material.SetFloat("_HazeEnd", 1800f);
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

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .59f, .63f);
            RenderSettings.ambientEquatorColor = new Color(.32f, .35f, .36f);
            RenderSettings.ambientGroundColor = new Color(.36f, .35f, .31f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.82f, .83f, .78f);
            RenderSettings.fogDensity = .00021f;
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
