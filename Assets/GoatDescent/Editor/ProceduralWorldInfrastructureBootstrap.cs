#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.Editor
{
    /// <summary>
    /// A one-time, non-destructive infrastructure probe for the procedural-world pipeline.
    /// It works in an additive scene and restores the user's active scene afterwards.
    /// </summary>
    [InitializeOnLoad]
    public static class ProceduralWorldInfrastructureBootstrap
    {
        // Bump this key only when the deterministic preview itself changes, so a script reload refreshes its capture once.
        private const string InitialBuildKey = "GoatDescent.ProceduralWorld.Infrastructure.InitialBuildComplete.v7";
        private const string ScenePath = "Assets/GoatDescent/Scenes/ProceduralWorldInfrastructure.unity";
        private const string TerrainDataPath = "Assets/Art/Generated/Terrain/Infrastructure/InfrastructureTerrainData.asset";
        private const string RockAssetPath = "Assets/Art/Generated/Rocks/Infrastructure/LowPolyRock_Prototype.fbx";
        private const string MaterialPath = "Assets/Art/Generated/Materials/Infrastructure/LowPolyRockPreview.mat";
        private const int Seed = 847291;

        static ProceduralWorldInfrastructureBootstrap()
        {
            EditorApplication.delayCall += BuildInitialInfrastructureOnce;
        }

        [MenuItem("Tools/Procedural World/Create Infrastructure Terrain")]
        public static void CreateInfrastructureTerrain()
        {
            BuildInfrastructure();
        }

        private static void BuildInitialInfrastructureOnce()
        {
            if (SessionState.GetBool(InitialBuildKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (BuildInfrastructure())
                SessionState.SetBool(InitialBuildKey, true);
        }

        private static bool BuildInfrastructure()
        {
            try
            {
                AssetDatabase.Refresh();
                var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockAssetPath);
                if (rockPrefab == null)
                {
                    Debug.LogError($"PROCEDURAL_WORLD_INFRASTRUCTURE_CHECK failed: Blender rock was not imported at {RockAssetPath}.");
                    return false;
                }

                EnsureFolders();
                var terrainData = GetOrCreateTerrainData();
                var rockMaterial = GetOrCreateRockMaterial();
                var activeScene = SceneManager.GetActiveScene();
                var probeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                try
                {
                    CreatePreviewScene(probeScene, terrainData, rockPrefab, rockMaterial);
                    EditorSceneManager.SaveScene(probeScene, ScenePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                finally
                {
                    EditorSceneManager.CloseScene(probeScene, true);
                    if (activeScene.IsValid() && activeScene.isLoaded)
                        SceneManager.SetActiveScene(activeScene);
                }

                string screenshotPath = Path.Combine(Path.GetTempPath(), "goat-procedural-world-infrastructure.png");
                bool screenshotCreated = File.Exists(screenshotPath);
                Debug.Log($"PROCEDURAL_WORLD_INFRASTRUCTURE_CHECK ready=True seed={Seed} terrainResolution={terrainData.heightmapResolution} " +
                          $"rockImported=True scene={ScenePath} screenshot={screenshotPath} screenshotCreated={screenshotCreated}");
                return screenshotCreated;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Generated/Terrain/Infrastructure"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Generated/Materials/Infrastructure"));
        }

        private static TerrainData GetOrCreateTerrainData()
        {
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData { name = "InfrastructureTerrainData" };
                AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            }

            terrainData.heightmapResolution = 257;
            terrainData.alphamapResolution = 256;
            terrainData.baseMapResolution = 256;
            terrainData.size = new Vector3(512f, 220f, 512f);

            var heights = GenerateHeights(terrainData.heightmapResolution);
            terrainData.SetHeights(0, 0, heights);
            terrainData.terrainLayers = new[]
            {
                GetOrCreateTerrainLayer("Meadow", new Color(0.24f, 0.38f, 0.16f), new Vector2(18f, 18f)),
                GetOrCreateTerrainLayer("Dirt", new Color(0.33f, 0.24f, 0.14f), new Vector2(13f, 13f)),
                GetOrCreateTerrainLayer("Rock", new Color(0.29f, 0.31f, 0.27f), new Vector2(17f, 17f)),
                GetOrCreateTerrainLayer("Mountain", new Color(0.49f, 0.47f, 0.40f), new Vector2(21f, 21f))
            };
            terrainData.SetAlphamaps(0, 0, GenerateAlphamaps(heights));
            EditorUtility.SetDirty(terrainData);
            return terrainData;
        }

        private static TerrainLayer GetOrCreateTerrainLayer(string name, Color color, Vector2 tileSize)
        {
            string texturePath = $"Assets/Art/Generated/Materials/Infrastructure/{name}TerrainTexture.asset";
            string layerPath = $"Assets/Art/Generated/Materials/Infrastructure/{name}TerrainLayer.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
                {
                    name = $"{name} Terrain Texture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
                var pixels = new Color[64];
                for (int i = 0; i < pixels.Length; i++)
                {
                    float variation = ((i * 23 + Seed) % 9 - 4) * 0.012f;
                    pixels[i] = color + new Color(variation, variation, variation, 0f);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                AssetDatabase.CreateAsset(texture, texturePath);
            }

            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer { name = $"{name} Terrain Layer" };
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.diffuseTexture = texture;
            layer.tileSize = tileSize;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Material GetOrCreateRockMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = "Low Poly Rock Preview" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.color = new Color(0.34f, 0.35f, 0.27f);
            material.SetFloat("_Glossiness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static float[,] GenerateHeights(int resolution)
        {
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = z / (float)(resolution - 1);
                float warpedU = u + (Mathf.PerlinNoise(u * 2.2f + 13.7f, v * 2.2f + 5.1f) - 0.5f) * 0.095f;
                float warpedV = v + (Mathf.PerlinNoise(u * 2.2f + 71.3f, v * 2.2f + 19.2f) - 0.5f) * 0.095f;
                float coast = SmoothMask(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)), 0f, 0.16f);
                float rolling = (FractalNoise(warpedU * 3.1f, warpedV * 3.1f, 3) - 0.44f) * 0.14f;

                // Two deliberately broad massifs establish the silhouette before ridge detail is applied.
                float massifA = GaussianMask(warpedU, warpedV, 0.69f, 0.67f, 0.22f, 0.27f);
                float massifB = GaussianMask(warpedU, warpedV, 0.42f, 0.73f, 0.16f, 0.20f);
                float ridgesA = RidgedNoise(warpedU * 3.4f + 47f, warpedV * 3.4f + 11f, 3);
                float ridgesB = RidgedNoise(warpedU * 4.1f + 19f, warpedV * 4.1f + 61f, 3);
                float mountains = massifA * (0.34f + ridgesA * 0.31f) + massifB * (0.20f + ridgesB * 0.24f);
                heights[z, x] = Mathf.Clamp01(0.014f + coast * (0.06f + rolling + mountains));
            }
            return heights;
        }

        private static float[,,] GenerateAlphamaps(float[,] heights)
        {
            int heightResolution = heights.GetLength(0);
            int resolution = 256;
            var maps = new float[resolution, resolution, 4];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int hx = Mathf.Clamp(Mathf.RoundToInt(x / (float)(resolution - 1) * (heightResolution - 1)), 1, heightResolution - 2);
                int hz = Mathf.Clamp(Mathf.RoundToInt(z / (float)(resolution - 1) * (heightResolution - 1)), 1, heightResolution - 2);
                float height = heights[hz, hx];
                float slope = Mathf.Clamp01((Mathf.Abs(heights[hz, hx + 1] - heights[hz, hx - 1]) + Mathf.Abs(heights[hz + 1, hx] - heights[hz - 1, hx])) * 34f);
                float mountain = SmoothMask(height, 0.26f, 0.52f);
                float rock = Mathf.Clamp01(slope * 1.6f + SmoothMask(height, 0.34f, 0.52f));
                float dirt = Mathf.Clamp01(0.3f + slope * 0.65f - mountain * 0.45f);
                float meadow = Mathf.Clamp01(1f - rock * 0.76f - mountain * 0.82f);
                float mountainRock = Mathf.Clamp01(mountain * 0.82f + rock * mountain * 0.68f);
                float total = meadow + dirt + rock + mountainRock;
                maps[z, x, 0] = meadow / total;
                maps[z, x, 1] = dirt / total;
                maps[z, x, 2] = rock / total;
                maps[z, x, 3] = mountainRock / total;
            }
            return maps;
        }

        private static float FractalNoise(float x, float y, int octaves)
        {
            float total = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int octave = 0; octave < octaves; octave++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                frequency *= 2.03f;
                amplitude *= 0.5f;
            }
            return total;
        }

        private static float GaussianMask(float x, float y, float centerX, float centerY, float radiusX, float radiusY)
        {
            float dx = (x - centerX) / radiusX;
            float dy = (y - centerY) / radiusY;
            return Mathf.Exp(-(dx * dx + dy * dy) * 1.45f);
        }

        private static float SmoothMask(float value, float low, float high)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(low, high, value));
        }

        private static float RidgedNoise(float x, float y, int octaves)
        {
            float noise = FractalNoise(x, y, octaves);
            return Mathf.Pow(1f - Mathf.Abs(noise * 2f - 1f), 1.65f);
        }

        private static void CreatePreviewScene(Scene scene, TerrainData terrainData, GameObject rockPrefab, Material rockMaterial)
        {
            var root = new GameObject("Procedural World Infrastructure Preview");
            SceneManager.MoveGameObjectToScene(root, scene);

            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Infrastructure Terrain — seed 847291";
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrainObject.transform.SetParent(root.transform);
            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 5;

            var random = new System.Random(Seed);
            var rockAnchors = new[]
            {
                new Vector2(212f, 80f), new Vector2(248f, 108f), new Vector2(172f, 152f),
                new Vector2(214f, 184f), new Vector2(253f, 219f), new Vector2(300f, 247f),
                new Vector2(342f, 279f), new Vector2(379f, 310f),
                new Vector2(274f, 320f), new Vector2(221f, 290f), new Vector2(336f, 362f)
            };
            var rocks = new GameObject("Imported Blender rocks");
            SceneManager.MoveGameObjectToScene(rocks, scene);
            rocks.transform.SetParent(root.transform);
            for (int index = 0; index < 18; index++)
            {
                Vector2 anchor = rockAnchors[index % rockAnchors.Length];
                float x = anchor.x + ((float)random.NextDouble() - 0.5f) * 22f;
                float z = anchor.y + ((float)random.NextDouble() - 0.5f) * 22f;
                float y = terrain.SampleHeight(new Vector3(x, 0f, z));
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, scene);
                instance.name = $"Blender rock {index + 1:00}";
                instance.transform.SetParent(rocks.transform);
                instance.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                // Blender FBX carries a centimetre-scale root here; compensate it so rocks read at terrain scale.
                instance.transform.localScale = Vector3.one * Mathf.Lerp(480f, 800f, (float)random.NextDouble());
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = rockMaterial;
            }

            var sunObject = new GameObject("Preview sun");
            SceneManager.MoveGameObjectToScene(sunObject, scene);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.18f;
            sun.color = new Color(1f, 0.82f, 0.62f);
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(38f, -31f, 0f);

            var cameraObject = new GameObject("Infrastructure Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.31f, 0.46f, 0.56f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1200f;
            cameraObject.transform.position = new Vector3(205f, 30f, -20f);
            cameraObject.transform.LookAt(new Vector3(305f, 76f, 292f));
            CapturePreview(camera);
        }

        private static void CapturePreview(Camera camera)
        {
            const int width = 1600;
            const int height = 900;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "goat-procedural-world-infrastructure.png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
