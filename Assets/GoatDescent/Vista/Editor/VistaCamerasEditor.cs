using System.Collections.Generic;
using System.IO;
using GoatDescent.ProceduralWorld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaCamerasEditor
    {
        internal static void CreateCaptureCameras(Transform root, Scene scene, Terrain terrain, TerrainBuildResult result, int[,] grassDensityMap, WorldSettings settings)
        {
            Vector3 peak = result.highestPoint;
            Vector3 goatSpawn = SummitSpawnUtility.FindStableGroundPoint(result, settings);
            float vistaYaw = SummitSpawnUtility.FindVistaYaw(result, settings, goatSpawn);
            Vector3 vistaDirection = new Vector3(Mathf.Sin(vistaYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(vistaYaw * Mathf.Deg2Rad));
            Vector3 forest = FindTreeFocus(scene, FindBiomePoint(result, settings, BiomeKind.Forest, peak));
            Vector3 grass = FindGrassFocus(grassDensityMap, result, settings, forest);
            Vector3 coast = FindBiomePoint(result, settings, BiomeKind.Coast, new Vector3(settings.worldSize * 0.14f, 0f, settings.worldSize * 0.30f));
            Vector3 forestCameraPosition = forest + new Vector3(-115f, 0f, -115f);
            forestCameraPosition.y = terrain.SampleHeight(forestCameraPosition) + 72f;
            Vector3 grassCameraPosition = grass + new Vector3(-10f, 10f, -10f);
            Vector3 playerPosition = goatSpawn + vistaDirection * 8f;
            playerPosition.y = terrain.SampleHeight(playerPosition) + 2.6f;
            Vector3 worldCenter = new Vector3(settings.worldSize * 0.5f, coast.y, settings.worldSize * 0.5f);
            Vector3 coastOutward = Vector3.ProjectOnPlane(coast - worldCenter, Vector3.up).normalized;
            if (coastOutward.sqrMagnitude < 0.01f)
                coastOutward = Vector3.forward;
            Vector3 coastCameraPosition = coast + coastOutward * 240f;
            coastCameraPosition.y = Mathf.Max(coast.y, settings.seaLevel * settings.heightScale) + 48f;
            Vector3 coastTarget = coast - coastOutward * 55f + Vector3.up * 14f;
            Camera mountain = CreateCamera(root, scene, "MountainOverview", peak + new Vector3(-740f, 320f, -740f), peak + new Vector3(0f, 18f, 0f), 55f);
            Camera summitGrove = CreateCamera(root, scene, "SummitGroveOverview", goatSpawn + vistaDirection * 108f + Vector3.up * 102f, goatSpawn + Vector3.up * 8f, 54f);
            Camera forestCamera = CreateCamera(root, scene, "ForestOverview", forestCameraPosition, forest + new Vector3(0f, 13f, 0f), 50f);
            Camera grassCamera = CreateCamera(root, scene, "GrassOverview", grassCameraPosition, grass + new Vector3(0f, 0.3f, 0f), 48f);
            Camera coastCamera = CreateCamera(root, scene, "CoastOverview", coastCameraPosition, coastTarget, 58f);
            Vector3 playerLookTarget = goatSpawn - vistaDirection * 45f + Vector3.up * 7f;
            Camera player = CreateCamera(root, scene, "PlayerView", playerPosition, playerLookTarget, 67f);
            Capture(mountain, "MountainOverview");
            Capture(summitGrove, "SummitGroveOverview");
            Capture(forestCamera, "ForestOverview");
            Capture(grassCamera, "GrassOverview");
            Capture(coastCamera, "CoastOverview");
            Capture(player, "PlayerView");
        }

        private static Camera CreateCamera(Transform root, Scene scene, string name, Vector3 position, Vector3 target, float fieldOfView)
        {
            var cameraObject = VistaAssetUtils.CreateChild(name, root, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(target);
            return camera;
        }

        private static Vector3 FindBiomePoint(TerrainBuildResult result, WorldSettings settings, BiomeKind biome, Vector3 fallback)
        {
            float bestDistance = float.MaxValue;
            Vector3 best = fallback;
            Vector2 center = new Vector2(settings.worldSize * 0.5f, settings.worldSize * 0.5f);
            int resolution = result.heights.GetLength(0);
            for (int z = 4; z < resolution - 4; z += 6)
            for (int x = 4; x < resolution - 4; x += 6)
            {
                WorldSample sample = result.samples[z, x];
                if (sample.biome != biome)
                    continue;
                if (biome == BiomeKind.Forest && !WorldPlacementRules.AllowsTree(sample, result.slopes[z, x], settings))
                    continue;
                if (biome == BiomeKind.Coast && result.slopes[z, x] > 18f)
                    continue;
                float worldX = x / (float)(resolution - 1) * settings.worldSize;
                float worldZ = z / (float)(resolution - 1) * settings.worldSize;
                float distance = Vector2.Distance(new Vector2(worldX, worldZ), center);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = new Vector3(worldX, result.heights[z, x] * settings.heightScale, worldZ);
            }
            return best;
        }

        private static Vector3 FindGrassFocus(int[,] densityMap, TerrainBuildResult result, WorldSettings settings, Vector3 fallback)
        {
            const int radius = 8;
            int height = densityMap.GetLength(0);
            int width = densityMap.GetLength(1);
            var prefix = new int[height + 1, width + 1];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
                prefix[z + 1, x + 1] = densityMap[z, x] + prefix[z, x + 1] + prefix[z + 1, x] - prefix[z, x];

            int bestDensity = 0;
            Vector3 best = fallback;
            int resolution = result.heights.GetLength(0);
            for (int z = radius; z < height - radius; z++)
            for (int x = radius; x < width - radius; x++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)(width - 1) * (resolution - 1)), 0, resolution - 1);
                int sz = Mathf.Clamp(Mathf.RoundToInt(z / (float)(height - 1) * (resolution - 1)), 0, resolution - 1);
                WorldSample sample = result.samples[sz, sx];
                float slope = result.slopes[sz, sx];
                if (!WorldPlacementRules.AllowsGrass(sample, slope, settings))
                    continue;
                if (slope > settings.grassSlopeLimit * 0.75f)
                    continue;

                int xMin = x - radius;
                int xMax = x + radius + 1;
                int zMin = z - radius;
                int zMax = z + radius + 1;
                int density = prefix[zMax, xMax] - prefix[zMin, xMax] - prefix[zMax, xMin] + prefix[zMin, xMin];
                if (density <= bestDensity)
                    continue;

                bestDensity = density;
                float worldX = x / (float)(width - 1) * settings.worldSize;
                float worldZ = z / (float)(height - 1) * settings.worldSize;
                best = new Vector3(worldX, result.heights[sz, sx] * settings.heightScale, worldZ);
            }
            return best;
        }

        private static Vector3 FindTreeFocus(Scene scene, Vector3 fallback)
        {
            var trees = new List<Vector3>();
            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.gameObject.scene != scene || !transform.name.StartsWith("Tree "))
                    continue;
                trees.Add(transform.position);
            }
            if (trees.Count == 0)
                return fallback;
            int largestCluster = -1;
            float bestDistance = float.MaxValue;
            Vector3 best = fallback;
            foreach (Vector3 candidate in trees)
            {
                int neighbours = 0;
                foreach (Vector3 other in trees)
                    if ((other - candidate).sqrMagnitude <= 150f * 150f)
                        neighbours++;
                float distance = (candidate - fallback).sqrMagnitude;
                if (neighbours < largestCluster || neighbours == largestCluster && distance >= bestDistance)
                    continue;
                largestCluster = neighbours;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private static void Capture(Camera camera, string name)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(GetScreenshotRoot(), $"goat-procedural-world-{name}.png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                Object.DestroyImmediate(texture);
            }
        }

        private static string GetScreenshotRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "goat-procedural-world-milestone");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}