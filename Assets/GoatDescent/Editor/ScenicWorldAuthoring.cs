using System;
using System.Collections.Generic;
using System.IO;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GoatDescent.EditorTools
{
    public static class ScenicWorldAuthoring
    {
        public static void Build() => BuildScene(Argument("-scenicScene", "TerracePeak"));

        [MenuItem("Tools/GOAT/Живописные маршруты/Уступчатый пик — пересобрать")]
        public static void BuildTerracePeak() => BuildScene("TerracePeak");
        [MenuItem("Tools/GOAT/Живописные маршруты/Небесный мост — пересобрать")]
        public static void BuildSkyBridge() => BuildScene("SkyBridge");
        [MenuItem("Tools/GOAT/Живописные маршруты/Ледяной разлом — пересобрать")]
        public static void BuildFrozenRift() => BuildScene("FrozenRift");
        [MenuItem("Tools/GOAT/Живописные маршруты/Пустынные арки — пересобрать")]
        public static void BuildDesertArches() => BuildScene("DesertArches");
        [MenuItem("Tools/GOAT/Живописные маршруты/Изумрудная котловина — пересобрать")]
        public static void BuildEmeraldBasin() => BuildScene("EmeraldBasin");

        private static void BuildScene(string scene)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            switch (scene)
            {
                case "TerracePeak": TerraceAdventureAuthoring.Build(); return;
                case "SkyBridge": SkyBridgeAuthoring.Build(); break;
                case "FrozenRift": FrozenRiftAuthoring.Build(); break;
                case "DesertArches": DesertArchesAuthoring.Build(); break;
                case "EmeraldBasin": EmeraldBasinAuthoring.Build(); break;
                default: throw new ArgumentException(scene);
            }
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/" + scene + ".unity");
            Apply();
        }

        public static void ApplySaved()
        {
            string scene = Argument("-scenicScene", "TerracePeak");
            if(scene == "TerracePeak") { TerraceAdventureAuthoring.Build(); return; }
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/" + scene + ".unity");
            Apply();
        }

        [MenuItem("Tools/GOAT/Статичные горы/Доработать открытую сцену и маршрут")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string name = scene.name;
            if(name == "TerracePeak") { TerraceAdventureAuthoring.Build(); return; }
            string folder = "Assets/GoatDescent/Level/Static/" + name;
            if (!Directory.Exists(folder)) throw new InvalidOperationException("Open one of the five scenic levels.");
            foreach (var old in Object.FindObjectsByType<GoatDescent.ScenicGoatCourse>(FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);
            var previous = GameObject.Find("Крупные скальные пласты");
            if (previous) Object.DestroyImmediate(previous);
            var rock = Materials(name, folder);
            Lighting(name);
            Vector3[] route = ScenicTrailAuthoring.Build(name, folder, rock);
            Relief(folder, rock, route);
            var camera = Camera.main;
            camera.farClipPlane = 24000f;
            if (name == "EmeraldBasin") camera.fieldOfView = 67f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Capture(camera, name + "-scenic-overview");
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float field = camera.fieldOfView;
            for (int i = 0; i < 5; i++)
            {
                int at = Mathf.RoundToInt((route.Length - 8) * i / 4f);
                camera.transform.position = route[at] + Vector3.up * 2.8f;
                camera.transform.LookAt(route[Mathf.Min(route.Length - 1, at + 7)] + Vector3.up * 1.5f);
                camera.fieldOfView = 65f;
                camera.nearClipPlane = .08f;
                Capture(camera, name + "-scenic-trail-" + (i + 1));
            }
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = field;
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SCENIC_WORLD_SAVED " + name);
        }

        private static Material Materials(string scene, string folder)
        {
            bool desert = scene == "DesertArches", frozen = scene == "FrozenRift";
            Color rockColor = desert ? new Color(.70f,.32f,.13f) : frozen ? new Color(.40f,.46f,.53f) : new Color(.72f,.59f,.40f);
            var stone = new Material(Shader.Find("GoatDescent/ScenicRock")) { name = "ScenicStone" };
            stone.SetColor("_Color", rockColor);
            stone.SetColor("_CoverColor", frozen ? new Color(.78f,.85f,.90f) : new Color(.27f,.39f,.13f));
            stone.SetFloat("_Cover", desert ? 0f : .6f);
            stone = Save(stone, folder + "/ScenicStone.mat");
            var materials = new Dictionary<Material, Material>();
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var source = renderer.sharedMaterial;
                if (!source || !source.shader) continue;
                string shader = source.shader.name;
                bool cliff = shader == "GoatDescent/TerracePeakRock" || shader == "GoatDescent/TerracePeakMeadow"
                    || shader == "GoatDescent/EmeraldRock" || shader == "GoatDescent/SkyBridgeRock"
                    || shader == "GoatDescent/FrozenRiftRock" || shader == "GoatDescent/DesertSandstone";
                if (shader == "GoatDescent/FrozenRiftSnow")
                {
                    source.SetColor("_SnowColor",new Color(.74f,.81f,.87f));
                    source.SetFloat("_Lift",.06f);
                    source.SetFloat("_Haze",0f);
                    EditorUtility.SetDirty(source);
                }
                if (shader == "GoatDescent/FrozenRiftIce")
                {
                    source.SetColor("_Color",new Color(.12f,.52f,.76f));
                    source.SetFloat("_Emissive",.12f);
                    source.SetFloat("_Haze",.15f);
                    EditorUtility.SetDirty(source);
                }
                if (cliff)
                {
                    if (!materials.TryGetValue(source, out var material))
                    {
                        material = new Material(stone) { name = "Scenic-" + source.name.Replace('/', '_').Replace('\\', '_') };
                        float cover = source.HasProperty("_GrassAmount") ? source.GetFloat("_GrassAmount") : .3f;
                        string lower = source.name.ToLowerInvariant();
                        if (lower.Contains("grass") || lower.Contains("meadow") || lower.Contains("ledge") || lower.Contains("tuft")) cover = 1f;
                        if (lower.Contains("snowrange"))
                        {
                            material.SetColor("_Color", new Color(.46f,.56f,.67f));
                            material.SetColor("_CoverColor", new Color(.80f,.86f,.91f));
                            cover = 1f;
                        }
                        if (desert) cover = 0f;
                        material.SetFloat("_Cover", cover);
                        material = Save(material, folder + "/" + material.name + ".mat");
                        materials.Add(source, material);
                    }
                    renderer.sharedMaterial = material;
                }
                if (renderer.name == "Полоса тропы" || renderer.name == "Отметки вейпоинтов") renderer.enabled = false;
            }
            return stone;
        }

        internal static void Lighting(string scene)
        {
            bool frozen = scene == "FrozenRift", desert = scene == "DesertArches";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.36f,.46f,.60f);
            RenderSettings.ambientEquatorColor = new Color(.27f,.31f,.35f);
            RenderSettings.ambientGroundColor = new Color(.19f,.17f,.14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = desert ? new Color(.72f,.66f,.59f) : new Color(.57f,.72f,.87f);
            RenderSettings.fogDensity = .000065f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                if (light != RenderSettings.sun) { light.intensity = .12f; continue; }
                light.transform.rotation = Quaternion.Euler(38f, (Camera.main ? Camera.main.transform.eulerAngles.y : 0f) + 35f, 0f);
                light.intensity = 1.15f;
                light.color = frozen ? new Color(.96f,.98f,1f) : new Color(1f,.90f,.73f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .85f;
                light.shadowBias = .025f;
                light.shadowNormalBias = .3f;
            }
            if (RenderSettings.skybox)
            {
                var sky = RenderSettings.skybox;
                sky.SetColor("_SkyZenith", new Color(.12f,.41f,.79f));
                sky.SetColor("_SkyHorizon", new Color(.57f,.75f,.92f));
                sky.SetFloat("_CloudDensity", .25f);
                if (RenderSettings.sun) sky.SetVector("_SunDir", -RenderSettings.sun.transform.forward);
                EditorUtility.SetDirty(sky);
            }
            foreach (var post in Object.FindObjectsByType<GoatDescent.GoatScenePost>(FindObjectsSortMode.None))
            {
                post.bloomIntensity = .09f;
                post.bloomThreshold = .9f;
                post.contrast = 1.04f;
                post.saturation = 1.12f;
                post.vignette = .12f;
                post.warmTint = Color.white;
                post.coolTint = new Color(.94f,.98f,1.04f);
            }
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 6500f;
            QualitySettings.shadowCascades = 4;
        }

        private static void Relief(string folder, Material stone, Vector3[] route)
        {
            var occupied = new HashSet<Vector2Int>();
            foreach (var point in route)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(point.x / 100f), Mathf.FloorToInt(point.z / 100f));
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) occupied.Add(cell + new Vector2Int(x,z));
            }
            var batch = new CrownRockMesh();
            var random = new System.Random(6143);
            int count = 0;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (!filter.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.sharedMaterial
                    || renderer.sharedMaterial.shader.name != "GoatDescent/ScenicRock" || renderer.bounds.size.magnitude < 800f) continue;
                var mesh = filter.sharedMesh;
                if (!mesh || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                if (triangles.Length < 3 || vertices.Length < 3) continue;
                for (int attempt = 0; attempt < 220 && count < 650; attempt++)
                {
                    int at = random.Next(triangles.Length / 3) * 3;
                    Vector3 a = filter.transform.TransformPoint(vertices[triangles[at]]);
                    Vector3 b = filter.transform.TransformPoint(vertices[triangles[at + 1]]);
                    Vector3 c = filter.transform.TransformPoint(vertices[triangles[at + 2]]);
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    if (Mathf.Abs(normal.y) > .65f) continue;
                    float u = (float)random.NextDouble(), v = (float)random.NextDouble();
                    if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                    Vector3 point = a + (b - a) * u + (c - a) * v;
                    if (occupied.Contains(new Vector2Int(Mathf.FloorToInt(point.x / 100f), Mathf.FloorToInt(point.z / 100f)))) continue;
                    float size = Mathf.Lerp(20f, 65f, (float)random.NextDouble());
                    point -= normal * size * .28f;
                    batch.RockBlock(point, new Vector2(size * .65f, size * .65f), point.y + size, point.y - size * 1.6f, random);
                    count++;
                }
            }
            var root = new GameObject("Крупные скальные пласты");
            ScenicTrailAuthoring.Add(root.transform, "Расколотые выступы", batch.Build("ScenicRockRelief"), stone,
                folder + "/ScenicRockRelief.asset", false);
            Debug.Log("SCENIC_RELIEF blocks=" + count);
        }

        internal static T Save<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(asset, path); return asset; }
            if(existing is Mesh oldMesh) oldMesh.Clear();
            EditorUtility.CopySerialized(asset, existing);
            if(existing is Mesh updatedMesh) updatedMesh.UploadMeshData(false);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }

        internal static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var original = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,1600,900),0,0);
                texture.Apply();
                Directory.CreateDirectory("Logs/Scenic");
                File.WriteAllBytes("Logs/Scenic/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = original;
                RenderTexture.active = previous;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        private static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }
    }
}
