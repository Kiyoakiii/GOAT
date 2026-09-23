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

        private IEnumerator Start()
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

            SceneManager.SetActiveScene(worldScene);
            DisablePreviewCameras(worldScene);
            yield return null;
            Physics.SyncTransforms();

            WorldSettings settings = FindWorldSettings(worldScene);
            bool ownsSettings = settings == null;
            if (ownsSettings)
                settings = ScriptableObject.CreateInstance<WorldSettings>();

            TerrainBuildResult terrain = TerrainGenerator.Build(settings);
            Collider terrainCollider = FindTerrainCollider(worldScene);
            if (terrainCollider == null)
            {
                terrainCollider = CreateFallbackTerrainCollider(worldScene, settings, terrain);
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
            float initialYaw = SummitSpawnUtility.FindVistaYaw(terrain, settings, spawn);
            if (ownsSettings)
                Destroy(settings);
            CreateGoat(spawn, initialYaw);
            Debug.Log($"PROCEDURAL_GOAT_WORLD_READY scene={WorldSceneName} spawn=({spawn.x:F1}, {spawn.y:F1}, {spawn.z:F1})");
        }

        private static void DisablePreviewCameras(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera captureCamera in root.GetComponentsInChildren<Camera>(true))
                captureCamera.enabled = false;
        }

        private static Collider FindTerrainCollider(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                if (collider.GetType().Name == "TerrainCollider")
                    return collider;
            return null;
        }

        private static WorldSettings FindWorldSettings(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (WorldGenerator generator in root.GetComponentsInChildren<WorldGenerator>(true))
                if (generator.Settings != null)
                    return generator.Settings;
            return null;
        }

        private static Collider CreateFallbackTerrainCollider(Scene scene, WorldSettings settings, TerrainBuildResult terrain)
        {
            if (terrain == null || terrain.heights == null)
                return null;

            int resolution = terrain.heights.GetLength(0);
            var vertices = new Vector3[resolution * resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                vertices[z * resolution + x] = new Vector3(
                    x / (float)(resolution - 1) * settings.worldSize,
                    terrain.heights[z, x] * settings.heightScale,
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
            Transform terrainTransform = FindTerrainTransform(scene);
            if (terrainTransform != null)
            {
                collisionObject.transform.SetPositionAndRotation(terrainTransform.position, terrainTransform.rotation);
                collisionObject.transform.localScale = terrainTransform.lossyScale;
            }

            MeshCollider collider = collisionObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            return collider;
        }

        private static Transform FindTerrainTransform(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                if (component != null && component.GetType().Name == "Terrain")
                    return component.transform;
            return null;
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
