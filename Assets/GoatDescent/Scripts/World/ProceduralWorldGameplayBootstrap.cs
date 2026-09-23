using System.Collections;
using GoatDescent.ProceduralWorld;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GoatDescent
{
    /// <summary>
    /// Uses the generated world as the playable Goat Descent map.  The former
    /// MountainPrototypeBuilder is intentionally not instantiated at runtime.
    /// </summary>
    public sealed class ProceduralWorldGameplayBootstrap : MonoBehaviour
    {
        private const string WorldSceneName = "ProceduralWorldMilestone";
        private bool bootstrapStarted;

        private void Start() => Begin();

        public void Begin()
        {
            if (bootstrapStarted)
                return;
            bootstrapStarted = true;
            Debug.Log("PROCEDURAL_GOAT_BOOTSTRAP_START");
            StartCoroutine(LoadAndSpawnWorld());
        }

        private IEnumerator LoadAndSpawnWorld()
        {
            Scene worldScene = SceneManager.GetSceneByName(WorldSceneName);
            if (!worldScene.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(WorldSceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    Debug.LogError($"PROCEDURAL_GOAT_WORLD failed: scene '{WorldSceneName}' is not in Build Settings.");
                    yield break;
                }
                while (!load.isDone)
                    yield return null;
                worldScene = SceneManager.GetSceneByName(WorldSceneName);
            }

            Debug.Log($"PROCEDURAL_GOAT_WORLD_SCENE_READY scene={worldScene.name} loaded={worldScene.isLoaded}");
            SceneManager.SetActiveScene(worldScene);
            DisablePreviewCameras(worldScene);

            WorldSettings settings = FindWorldSettings(worldScene);
            bool ownsSettings = settings == null;
            if (ownsSettings)
                settings = ScriptableObject.CreateInstance<WorldSettings>();

            Terrain terrainSurface = FindTerrain(worldScene);
            Debug.Log($"PROCEDURAL_GOAT_SETTINGS_READY resolution={settings.terrainResolution} terrainFound={(terrainSurface != null)}");
            TerrainBuildResult terrain = terrainSurface != null
                ? ReadSpawnSurface(terrainSurface, settings)
                : TerrainGenerator.Build(settings);
            Debug.Log($"PROCEDURAL_GOAT_TERRAIN_READY resolution={terrain.heights.GetLength(0)} seed={settings.seed} source={(terrainSurface ? "TerrainData" : "procedural fallback")}");
            Collider terrainCollider = FindTerrainCollider(terrainSurface);
            if (terrainCollider == null)
            {
                terrainCollider = CreateFallbackTerrainCollider(worldScene, settings, terrain, terrainSurface ? terrainSurface.transform : null);
                if (terrainCollider == null)
                {
                    if (ownsSettings)
                        Destroy(settings);
                    Debug.LogError("PROCEDURAL_GOAT_WORLD failed: neither TerrainCollider nor fallback mesh collision could be created.");
                    yield break;
                }
                Debug.LogWarning("PROCEDURAL_GOAT_WORLD using generated mesh collision because Unity removed the built-in TerrainCollider.");
            }

            Vector3 spawn = FindSummitSpawn(terrainCollider, terrain, settings);
            float vistaYaw = SummitSpawnUtility.FindVistaYaw(terrain, settings, spawn);
            float initialYaw = FindSummitForestFacingYaw(worldScene, spawn, vistaYaw);
            if (ownsSettings)
                Destroy(settings);
            CreateGoat(spawn, initialYaw);
            Debug.Log($"PROCEDURAL_GOAT_WORLD_READY scene={WorldSceneName} spawn=({spawn.x:F1}, {spawn.y:F1}, {spawn.z:F1}) cameraYaw={initialYaw:F1}");
        }

        private static float FindSummitForestFacingYaw(Scene scene, Vector3 spawn, float vistaYaw)
        {
            const float forestBlend = 0.82f;
            float nearestDistanceSquared = float.PositiveInfinity;
            float nearestTreeYaw = vistaYaw;

            // The summit grove sits below the generated chunk root. Inspect only
            // that small branch of the hierarchy rather than walking the whole
            // world (which also contains thousands of grass vertices/objects).
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform generatedChunks in root.transform)
            {
                if (generatedChunks.name != "Generated chunks")
                    continue;

                foreach (Transform chunk in generatedChunks)
                foreach (Transform grove in chunk)
                {
                    if (!grove.name.StartsWith("Summit Grove"))
                        continue;

                    foreach (Transform tree in grove)
                    {
                        if (!tree.name.StartsWith("Summit Conifer"))
                            continue;

                        Vector3 direction = tree.position - spawn;
                        direction.y = 0f;
                        float distanceSquared = direction.sqrMagnitude;
                        if (distanceSquared >= nearestDistanceSquared || distanceSquared < 0.01f)
                            continue;

                        nearestDistanceSquared = distanceSquared;
                        nearestTreeYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    }
                }
            }

            return nearestDistanceSquared < float.PositiveInfinity
                ? Mathf.LerpAngle(vistaYaw, nearestTreeYaw, forestBlend)
                : vistaYaw;
        }

        private static void DisablePreviewCameras(Scene scene)
        {
            // Preview cameras are direct children of the world root; do not walk
            // every generated tree and grass object just to turn those off.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera captureCamera in root.GetComponents<Camera>())
                    captureCamera.enabled = false;
                foreach (Transform child in root.transform)
                    foreach (Camera captureCamera in child.GetComponents<Camera>())
                        captureCamera.enabled = false;
            }
        }

        private static Collider FindTerrainCollider(Terrain terrain)
        {
            return terrain != null ? terrain.GetComponent<Collider>() : null;
        }

        private static WorldSettings FindWorldSettings(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                WorldGenerator generator = root.GetComponent<WorldGenerator>();
                if (generator != null && generator.Settings != null)
                    return generator.Settings;
                foreach (Transform child in root.transform)
                {
                    generator = child.GetComponent<WorldGenerator>();
                    if (generator != null && generator.Settings != null)
                        return generator.Settings;
                }
            }
            return null;
        }

        private static Terrain FindTerrain(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Terrain terrain = root.GetComponent<Terrain>();
                if (terrain != null && terrain.terrainData != null)
                    return terrain;
                foreach (Transform child in root.transform)
                {
                    terrain = child.GetComponent<Terrain>();
                    if (terrain != null && terrain.terrainData != null)
                        return terrain;
                }
            }
            return null;
        }

        private static TerrainBuildResult ReadSpawnSurface(Terrain terrain, WorldSettings settings)
        {
            TerrainData data = terrain.terrainData;
            int sourceResolution = data.heightmapResolution;
            int resolution = Mathf.Min(sourceResolution, 257);
            float[,] source = data.GetHeights(0, 0, sourceResolution, sourceResolution);
            var result = new TerrainBuildResult
            {
                heights = new float[resolution, resolution],
                slopes = new float[resolution, resolution]
            };

            float verticalScale = data.size.y * terrain.transform.lossyScale.y;
            float verticalOffset = terrain.transform.position.y;
            float maxHeight = float.NegativeInfinity;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int sourceX = Mathf.RoundToInt(x / (float)(resolution - 1) * (sourceResolution - 1));
                int sourceZ = Mathf.RoundToInt(z / (float)(resolution - 1) * (sourceResolution - 1));
                float worldHeight = verticalOffset + source[sourceZ, sourceX] * verticalScale;
                result.heights[z, x] = worldHeight / Mathf.Max(settings.heightScale, 0.001f);
                if (worldHeight > maxHeight)
                {
                    maxHeight = worldHeight;
                    result.highestPoint = new Vector3(
                        x / (float)(resolution - 1) * settings.worldSize,
                        worldHeight,
                        z / (float)(resolution - 1) * settings.worldSize);
                }
            }

            float spacing = settings.worldSize / (resolution - 1);
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int left = Mathf.Max(0, x - 1);
                int right = Mathf.Min(resolution - 1, x + 1);
                int down = Mathf.Max(0, z - 1);
                int up = Mathf.Min(resolution - 1, z + 1);
                float dx = (result.heights[z, right] - result.heights[z, left]) * settings.heightScale / Mathf.Max(spacing, 0.001f);
                float dz = (result.heights[up, x] - result.heights[down, x]) * settings.heightScale / Mathf.Max(spacing, 0.001f);
                result.slopes[z, x] = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
            }
            return result;
        }

        private static Collider CreateFallbackTerrainCollider(Scene scene, WorldSettings settings, TerrainBuildResult terrain, Transform terrainTransform)
        {
            if (terrain == null || terrain.heights == null)
                return null;

            int sourceResolution = terrain.heights.GetLength(0);
            // TerrainCollider is unavailable in this Unity package setup. A full
            // 513x513 triangle mesh is unnecessarily expensive to cook for player
            // movement, so use a coarser collision surface while leaving the
            // rendered terrain at its original resolution.
            const int maximumCollisionResolution = 129;
            int resolution = Mathf.Min(sourceResolution, maximumCollisionResolution);
            var vertices = new Vector3[resolution * resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int sourceX = Mathf.RoundToInt(x / (float)(resolution - 1) * (sourceResolution - 1));
                int sourceZ = Mathf.RoundToInt(z / (float)(resolution - 1) * (sourceResolution - 1));
                vertices[z * resolution + x] = new Vector3(
                    x / (float)(resolution - 1) * settings.worldSize,
                    terrain.heights[sourceZ, sourceX] * settings.heightScale,
                    z / (float)(resolution - 1) * settings.worldSize);
            }

            int quadCount = (resolution - 1) * (resolution - 1);
            var triangles = new int[quadCount * 6];
            int index = 0;
            for (int z = 0; z < resolution - 1; z++)
            for (int x = 0; x < resolution - 1; x++)
            {
                int lowerLeft = z * resolution + x;
                int upperLeft = lowerLeft + resolution;
                triangles[index++] = lowerLeft;
                triangles[index++] = upperLeft;
                triangles[index++] = lowerLeft + 1;
                triangles[index++] = lowerLeft + 1;
                triangles[index++] = upperLeft;
                triangles[index++] = upperLeft + 1;
            }

            var mesh = new Mesh
            {
                name = "Procedural World Runtime Collision",
                indexFormat = IndexFormat.UInt32,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateBounds();

            var collisionObject = new GameObject("Procedural World Runtime MeshCollider");
            SceneManager.MoveGameObjectToScene(collisionObject, scene);
            if (terrainTransform != null)
            {
                collisionObject.transform.SetPositionAndRotation(terrainTransform.position, terrainTransform.rotation);
                collisionObject.transform.localScale = terrainTransform.lossyScale;
            }

            MeshCollider collider = collisionObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            Debug.Log($"PROCEDURAL_GOAT_COLLISION_READY resolution={resolution} sourceResolution={sourceResolution} triangles={triangles.Length / 3}");
            return collider;
        }

        private static Vector3 FindSummitSpawn(Collider terrainCollider, TerrainBuildResult terrain, WorldSettings settings)
        {
            Vector3 estimatedPeak = SummitSpawnUtility.FindStableGroundPoint(terrain, settings);

            var ray = new Ray(new Vector3(estimatedPeak.x, 1000f, estimatedPeak.z), Vector3.down);
            if (terrainCollider.Raycast(ray, out RaycastHit hit, 1400f))
                return hit.point + Vector3.up * 0.12f;

            return estimatedPeak + Vector3.up * 0.12f;
        }

        private static void CreateGoat(Vector3 spawn, float initialYaw)
        {
            var goat = new GameObject("Mountain Goat");
            goat.transform.position = spawn;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                goat.layer = playerLayer;

            var body = goat.AddComponent<Rigidbody>();
            body.mass = 72f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            var capsule = goat.AddComponent<CapsuleCollider>();
            capsule.radius = 0.48f;
            capsule.height = 1.15f;
            capsule.center = new Vector3(0f, 0.58f, 0f);

            var detector = goat.AddComponent<GoatGroundDetector>();
            var controller = goat.AddComponent<GoatController>();
            controller.Configure(body, detector);
            goat.AddComponent<GoatJumpController>().Configure(controller, detector);
            goat.AddComponent<GoatLandingAssist>().Configure(body, detector);
            goat.AddComponent<GoatVisualController>().Configure(body, detector);
            goat.AddComponent<RespawnController>().Configure(body, spawn);

            var cameraRoot = new GameObject("Third Person Goat Camera");
            cameraRoot.tag = "MainCamera";
            var camera = cameraRoot.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.10f, 0.17f, 0.15f);
            camera.farClipPlane = 5000f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraRoot.AddComponent<ThirdPersonGoatCamera>().Configure(goat.transform, initialYaw);
        }
    }
}
