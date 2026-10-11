using System.Collections.Generic;
using System.Linq;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaWorldBuilderEditor
    {
        internal static List<WorldChunk> CreateChunks(Transform root, Scene scene, WorldSettings settings)
        {
            var chunkRoot = new GameObject("Generated chunks");
            SceneManager.MoveGameObjectToScene(chunkRoot, scene);
            chunkRoot.transform.SetParent(root);
            var result = new List<WorldChunk>();
            int count = Mathf.CeilToInt(settings.worldSize / settings.chunkSize);
            for (int z = 0; z < count; z++)
            for (int x = 0; x < count; x++)
            {
                var chunkObject = new GameObject($"WorldChunk ({x}, {z})");
                SceneManager.MoveGameObjectToScene(chunkObject, scene);
                chunkObject.transform.SetParent(chunkRoot.transform);
                var chunk = chunkObject.AddComponent<WorldChunk>();
                float originX = x * settings.chunkSize;
                float originZ = z * settings.chunkSize;
                chunk.Configure(new Vector2Int(x, z), new Bounds(new Vector3(originX + settings.chunkSize * 0.5f, settings.heightScale * 0.5f, originZ + settings.chunkSize * 0.5f), new Vector3(settings.chunkSize, settings.heightScale, settings.chunkSize)));
                result.Add(chunk);
            }
            return result;
        }

        internal static void SpawnWorldObjects(List<WorldChunk> chunks, Scene scene, Terrain terrain, TerrainBuildResult terrainResult, WorldSettings settings, VegetationGenerationSettings vegetation, GameObject[] rocks, GameObject[] trees, GameObject[] summitTrees, GameObject[] cliffs, VistaMaterialsEditor.MaterialSet materials)
        {
            int spawnedRocks = 0;
            int spawnedTrees = 0;
            int spawnedCliffs = 0;
            foreach (WorldChunk chunk in chunks)
            {
                var rockRoot = VistaAssetUtils.CreateChild("Rocks", chunk.transform, scene);
                var treeRoot = VistaAssetUtils.CreateChild("Vegetation", chunk.transform, scene);
                var cliffRoot = VistaAssetUtils.CreateChild("Cliffs", chunk.transform, scene);
                var random = new System.Random(NoiseGenerator.DeriveSeed(settings.seed, chunk.Coordinate.x, chunk.Coordinate.y, 1409));
                foreach (Vector2 point in JitteredPoints(chunk.Bounds, 9, random))
                {
                    WorldSample sample = BiomeGenerator.Sample(point.x, point.y, settings);
                    float slope = TerrainGenerator.SampleSlope(point.x, point.y, settings);
                    if (!WorldPlacementRules.AllowsRock(sample, slope) || sample.rockDensity < 0.54f - settings.rockDensity * 0.15f)
                        continue;
                    GameObject prefab = rocks[sample.biome == BiomeKind.Mountain && random.NextDouble() > 0.45 ? rocks.Length - 1 : random.Next(rocks.Length)];
                    float scale = sample.biome == BiomeKind.Mountain ? Mathf.Lerp(0.85f, 1.35f, (float)random.NextDouble()) : Mathf.Lerp(0.45f, 0.95f, (float)random.NextDouble());
                    SpawnPrefab(prefab, $"Rock {spawnedRocks + 1:000}", point, terrain, rockRoot.transform, scene, scale, materials.rock, null, random);
                    spawnedRocks++;
                }
                random = new System.Random(NoiseGenerator.DeriveSeed(settings.seed, chunk.Coordinate.x, chunk.Coordinate.y, 2029));
                foreach (Vector2 point in JitteredPoints(chunk.Bounds, 42, random))
                {
                    if (random.NextDouble() >= vegetation.treeDensity)
                        continue;
                    WorldSample sample = BiomeGenerator.Sample(point.x, point.y, settings);
                    float slope = TerrainGenerator.SampleSlope(point.x, point.y, settings);
                    if (!WorldPlacementRules.AllowsTree(sample, slope, settings))
                        continue;
                    float densityThreshold = sample.biome == BiomeKind.Forest ? 0.43f : 0.64f;
                    if (sample.forestDensity < densityThreshold || sample.biome == BiomeKind.Meadows && random.NextDouble() > 0.22d)
                        continue;
                    int index = sample.biome == BiomeKind.Forest ? random.Next(0, 2) : random.Next(trees.Length);
                    SpawnPrefab(trees[index], $"Tree {spawnedTrees + 1:000}", point, terrain, treeRoot.transform, scene, Mathf.Lerp(0.72f, 1.28f, (float)random.NextDouble()), materials.trunk, materials.foliage, random);
                    spawnedTrees++;
                }
                random = new System.Random(NoiseGenerator.DeriveSeed(settings.seed, chunk.Coordinate.x, chunk.Coordinate.y, 3037));
                foreach (Vector2 point in JitteredPoints(chunk.Bounds, 6, random))
                {
                    WorldSample sample = BiomeGenerator.Sample(point.x, point.y, settings);
                    float slope = TerrainGenerator.SampleSlope(point.x, point.y, settings);
                    if (!WorldPlacementRules.AllowsCliff(sample, slope, settings) || sample.rockDensity < 0.48f - settings.cliffDensity * 0.13f)
                        continue;
                    SpawnPrefab(cliffs[random.Next(cliffs.Length)], $"Cliff {spawnedCliffs + 1:000}", point, terrain, cliffRoot.transform, scene, Mathf.Lerp(0.75f, 1.25f, (float)random.NextDouble()), materials.rock, null, random);
                    spawnedCliffs++;
                }
            }

            int summitTreeCount = SpawnSummitGrove(chunks, scene, terrain, terrainResult, settings, vegetation, summitTrees, materials);
            Debug.Log($"PROCEDURAL_WORLD_SPAWN_OK trees={spawnedTrees} summitTrees={summitTreeCount} rocks={spawnedRocks} cliffs={spawnedCliffs}");
        }

        internal static void CreateWater(Transform root, Scene scene, WorldSettings settings, Material material)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Coast water";
            SceneManager.MoveGameObjectToScene(water, scene);
            water.transform.SetParent(root);
            water.transform.position = new Vector3(settings.worldSize * 0.5f, settings.seaLevel * settings.heightScale, settings.worldSize * 0.5f);
            water.transform.localScale = new Vector3(settings.worldSize / 10f, 1f, settings.worldSize / 10f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().sharedMaterial = material;
        }

        internal static void CreateLighting(Transform root, Scene scene)
        {
            var lightObject = VistaAssetUtils.CreateChild("Directional light", root, scene);
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.12f;
            sun.color = new Color(1f, 0.84f, 0.67f);
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        }

        internal static GameObject[] LoadPrefabs(IEnumerable<string> paths)
        {
            var result = new List<GameObject>();
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    result.Add(prefab);
            }
            return result.ToArray();
        }

        private static int SpawnSummitGrove(List<WorldChunk> chunks, Scene scene, Terrain terrain, TerrainBuildResult terrainResult, WorldSettings settings, VegetationGenerationSettings vegetation, GameObject[] trees, VistaMaterialsEditor.MaterialSet materials)
        {
            if (trees == null || trees.Length < 4 || terrainResult == null || terrainResult.heights == null)
                return 0;

            Vector3 goatSpawn = SummitSpawnUtility.FindStableGroundPoint(terrainResult, settings);
            float vistaYaw = SummitSpawnUtility.FindVistaYaw(terrainResult, settings, goatSpawn);
            Vector3 vistaDirection = new Vector3(Mathf.Sin(vistaYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(vistaYaw * Mathf.Deg2Rad));
            WorldChunk summitChunk = FindSummitChunk(chunks, goatSpawn);
            if (summitChunk == null)
                return 0;

            Transform groveRoot = VistaAssetUtils.CreateChild("Summit Grove — goat spawn clearing", summitChunk.transform, scene).transform;
            int resolution = terrainResult.heights.GetLength(0);
            float spacing = settings.worldSize / (resolution - 1);
            int peakX = Mathf.Clamp(Mathf.RoundToInt(goatSpawn.x / spacing), 0, resolution - 1);
            int peakZ = Mathf.Clamp(Mathf.RoundToInt(goatSpawn.z / spacing), 0, resolution - 1);
            int targetTrees = Mathf.Max(0, vegetation.summitTreeCount);
            int planted = 0;
            var plantedPoints = new List<Vector2>(targetTrees);
            var random = new System.Random(NoiseGenerator.DeriveSeed(settings.seed, peakX, peakZ, 7759));
            const int maxAttempts = 1800;
            const float innerRadius = 34f;
            const float outerRadius = 118f;
            const float goldenAngle = 2.39996323f;
            float minimumHeight = goatSpawn.y - 160f;
            float maximumHeight = goatSpawn.y + 10f;
            float minimumSeparationSq = 12f * 12f;

            for (int attempt = 0; attempt < maxAttempts && planted < targetTrees; attempt++)
            {
                float normalizedRadius = (attempt + (float)random.NextDouble()) / maxAttempts;
                float radius = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius, outerRadius * outerRadius, normalizedRadius));
                float angle = attempt * goldenAngle + ((float)random.NextDouble() - 0.5f) * 0.24f;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);
                Vector2 point = new Vector2(goatSpawn.x + sin * radius, goatSpawn.z + cos * radius);
                if (point.x < 8f || point.y < 8f || point.x > settings.worldSize - 8f || point.y > settings.worldSize - 8f)
                    continue;

                float heading = Mathf.Atan2(sin, cos) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(vistaYaw, heading)) < 38f)
                    continue;

                int x = Mathf.Clamp(Mathf.RoundToInt(point.x / spacing), 0, resolution - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt(point.y / spacing), 0, resolution - 1);
                WorldSample sample = terrainResult.samples[z, x];
                float slope = terrainResult.slopes[z, x];
                float height = terrainResult.heights[z, x] * settings.heightScale;
                if (sample.biome == BiomeKind.Coast
                    || slope > Mathf.Min(settings.treeSlopeLimit + 14f, 48f)
                    || height < minimumHeight
                    || height > maximumHeight)
                    continue;

                bool tooClose = false;
                for (int i = 0; i < plantedPoints.Count; i++)
                {
                    if ((plantedPoints[i] - point).sqrMagnitude < minimumSeparationSq)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                int variantRoll = random.Next(0, 100);
                int prefabIndex = variantRoll < 26 ? 0 : variantRoll < 56 ? 1 : variantRoll < 82 ? 2 : 3;
                float scale = Mathf.Lerp(0.9f, 1.25f, (float)random.NextDouble());
                SpawnPrefab(trees[prefabIndex], $"Summit Conifer {planted + 1:000}", point, terrain, groveRoot, scene, scale, materials.trunk, materials.summitFoliage, random);
                plantedPoints.Add(point);
                planted++;
            }

            return planted;
        }

        private static WorldChunk FindSummitChunk(List<WorldChunk> chunks, Vector3 point)
        {
            WorldChunk nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (WorldChunk chunk in chunks)
            {
                Vector3 samplePoint = new Vector3(point.x, chunk.Bounds.center.y, point.z);
                if (chunk.Bounds.Contains(samplePoint))
                    return chunk;

                float distance = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(chunk.Bounds.center.x, chunk.Bounds.center.z));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = chunk;
                }
            }
            return nearest;
        }

        private static IEnumerable<Vector2> JitteredPoints(Bounds bounds, int grid, System.Random random)
        {
            float cell = bounds.size.x / grid;
            for (int z = 0; z < grid; z++)
            for (int x = 0; x < grid; x++)
            {
                float px = bounds.min.x + (x + 0.18f + (float)random.NextDouble() * 0.64f) * cell;
                float pz = bounds.min.z + (z + 0.18f + (float)random.NextDouble() * 0.64f) * cell;
                yield return new Vector2(px, pz);
            }
        }

        private static void SpawnPrefab(GameObject prefab, string name, Vector2 point, Terrain terrain, Transform treeRoot, Scene scene, float scale, Material mainMaterial, Material foliageMaterial, System.Random random)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(treeRoot);
            float height = terrain.SampleHeight(new Vector3(point.x, 0f, point.y));
            instance.transform.SetPositionAndRotation(new Vector3(point.x, height, point.y), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            instance.transform.localScale = Vector3.one * VistaAssetPaths.FbxUnitCompensation * scale;
            ConfigureRenderers(instance, mainMaterial, foliageMaterial);
            ConfigureLodGroup(instance);
        }

        private static void ConfigureRenderers(GameObject instance, Material mainMaterial, Material foliageMaterial)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                bool isNeedledGeometry = renderer.name.Contains("Foliage") || renderer.name.Contains("Branches");
                renderer.sharedMaterial = foliageMaterial != null && isNeedledGeometry ? foliageMaterial : mainMaterial;
            }
        }

        private static void ConfigureLodGroup(GameObject instance)
        {
            var lods = new List<LOD>();
            for (int level = 0; level < 3; level++)
            {
                var renderers = new List<Renderer>();
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    if (renderer.name.Contains($"LOD{level}"))
                        renderers.Add(renderer);
                if (renderers.Count > 0)
                    lods.Add(new LOD(level == 0 ? 0.48f : level == 1 ? 0.22f : 0.06f, renderers.ToArray()));
            }
            if (lods.Count == 0)
                return;
            LODGroup group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
        }
    }
}