using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GoatDescent.ProceduralWorld.Editor
{
    public static class ProceduralWorldEditorActions
    {
        public const string SettingsPath = "Assets/ProceduralWorld/Data/WorldSettings_Prototype.asset";
        public const string ScenePath = "Assets/ProceduralWorld/Scenes/ProceduralWorldMilestone.unity";
        private const string TerrainDataPath = "Assets/ProceduralWorld/Generated/Terrain/ProceduralWorldTerrainData.asset";
        private const string MaterialRoot = "Assets/ProceduralWorld/Generated/Materials";
        private const string DebugRoot = "Assets/ProceduralWorld/Generated/Debug";
        private const string GrassRoot = "Assets/ProceduralWorld/Generated/Grass";
        private const string GrassMeshPath = GrassRoot + "/GrassClump.asset";
        private const string GrassPrefabPath = GrassRoot + "/GrassClump.prefab";
        private const float FbxUnitCompensation = 100f;

        private static readonly string[] RockPaths =
        {
            "Assets/Art/Generated/Rocks/Rock_Small.fbx",
            "Assets/Art/Generated/Rocks/Rock_Medium.fbx",
            "Assets/Art/Generated/Rocks/Rock_Large.fbx",
            "Assets/Art/Generated/Rocks/Mountain_Boulder.fbx"
        };

        private static readonly string[] TreePaths =
        {
            "Assets/Art/Generated/Trees/Tree_Pine.fbx",
            "Assets/Art/Generated/Trees/Tree_Fir.fbx",
            "Assets/Art/Generated/Trees/Tree_Dead.fbx",
            "Assets/Art/Generated/Trees/Tree_Stump.fbx"
        };

        private static readonly string[] SummitTreePaths =
        {
            "Assets/Art/Generated/Trees/Tree_Pine.fbx",
            "Assets/Art/Generated/Trees/Tree_Pine_OldGrowth.fbx",
            "Assets/Art/Generated/Trees/Tree_Pine_Windbent.fbx",
            "Assets/Art/Generated/Trees/Tree_Fir.fbx"
        };

        private static readonly string[] CliffPaths =
        {
            "Assets/Art/Generated/Cliffs/Cliff_Straight.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Corner.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Tall.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Wide.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Broken.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Cap.fbx"
        };

        [MenuItem("Tools/Procedural World/Generate World")]
        public static void GenerateWorldFromMenu()
        {
            GenerateWorld(GetOrCreateSettings());
        }

        [MenuItem("Tools/Procedural World/Regenerate Terrain")]
        public static void RegenerateTerrainFromMenu()
        {
            GenerateWorld(GetOrCreateSettings());
        }

        [MenuItem("Tools/Procedural World/Generate Debug Maps")]
        public static void GenerateDebugMapsFromMenu()
        {
            WorldSettings settings = GetOrCreateSettings();
            TerrainBuildResult result = TerrainGenerator.Build(settings);
            GenerateDebugTextures(settings, result);
            AssetDatabase.SaveAssets();
            Debug.Log("PROCEDURAL_WORLD_DEBUG_MAPS_OK modes=Height,MountainMask,Biomes,Temperature,Humidity,Slope,ForestDensity,RockDensity,CliffMask,SnowMask,GrassDensity");
        }

        [MenuItem("Tools/Procedural World/Clear Generated Objects")]
        public static void ClearGeneratedObjectsFromMenu()
        {
            if (!File.Exists(ScenePath))
                return;
            Scene activeScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.name == "Procedural World Milestone")
                        UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (activeScene.IsValid())
                    SceneManager.SetActiveScene(activeScene);
            }
            Debug.Log($"PROCEDURAL_WORLD_CLEAR_OK scene={ScenePath}");
        }

        public static WorldSettings GetOrCreateSettings()
        {
            EnsureFolders();
            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WorldSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            EnsureBiomeDefinition("Coast", BiomeKind.Coast, new Color(0.20f, 0.48f, 0.55f), 0.10f, 0.25f, 0.05f);
            EnsureBiomeDefinition("Meadows", BiomeKind.Meadows, new Color(0.37f, 0.60f, 0.24f), 0.42f, 0.42f, 0.08f);
            EnsureBiomeDefinition("Forest", BiomeKind.Forest, new Color(0.12f, 0.35f, 0.16f), 0.88f, 0.55f, 0.16f);
            EnsureBiomeDefinition("Mountain", BiomeKind.Mountain, new Color(0.46f, 0.44f, 0.38f), 0.05f, 0.90f, 0.85f);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static void GenerateWorld(WorldSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            EnsureFolders();
            AssetDatabase.Refresh();
            var stopwatch = Stopwatch.StartNew();
            TerrainData terrainData = GetOrCreateTerrainData();
            TerrainBuildResult terrainResult = TerrainGenerator.Build(settings);
            int[,] grassDensityMap = ConfigureTerrainData(terrainData, terrainResult, settings);
            MaterialSet materials = GetOrCreateMaterials();
            GameObject[] rocks = LoadPrefabs(RockPaths);
            GameObject[] trees = LoadPrefabs(TreePaths);
            GameObject[] summitTrees = LoadPrefabs(SummitTreePaths);
            GameObject[] cliffs = LoadPrefabs(CliffPaths);
            if (rocks.Length == 0 || trees.Length == 0 || summitTrees.Length < 4 || cliffs.Length == 0)
                throw new InvalidOperationException("Generated Blender FBX assets are missing. Run Tools/Blender/generate_world_asset_pack.py first.");

            Scene activeScene = SceneManager.GetActiveScene();
            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var worldRoot = new GameObject("Procedural World Milestone");
                SceneManager.MoveGameObjectToScene(worldRoot, previewScene);
                var generator = worldRoot.AddComponent<WorldGenerator>();
                generator.Configure(settings, worldRoot.transform);

                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainObject.name = $"Terrain — seed {settings.seed}";
                SceneManager.MoveGameObjectToScene(terrainObject, previewScene);
                terrainObject.transform.SetParent(worldRoot.transform);
                Terrain terrain = terrainObject.GetComponent<Terrain>();
                TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>() ?? terrainObject.AddComponent<TerrainCollider>();
                terrainCollider.terrainData = terrainData;
                terrain.drawInstanced = true;
                terrain.detailObjectDistance = 150f;
                terrain.detailObjectDensity = 0.9f;
                terrain.heightmapPixelError = 5;

                CreateWater(worldRoot.transform, previewScene, settings, materials.water);
                List<WorldChunk> chunks = CreateChunks(worldRoot.transform, previewScene, settings);
                SpawnWorldObjects(chunks, previewScene, terrain, terrainResult, settings, rocks, trees, summitTrees, cliffs, materials);
                CreateGrassMeshes(worldRoot.transform, previewScene, terrain, grassDensityMap, terrainResult, settings);
                CreateLighting(worldRoot.transform, previewScene);
                CreateCaptureCameras(worldRoot.transform, previewScene, terrain, terrainResult, grassDensityMap, settings);
                GenerateDebugTextures(settings, terrainResult);
                EditorSceneManager.SaveScene(previewScene, ScenePath);
                EnsureSceneInBuildSettings(ScenePath);
                AssetDatabase.SaveAssets();
                stopwatch.Stop();
                Debug.Log($"PROCEDURAL_WORLD_MILESTONE_OK seed={settings.seed} terrain={settings.worldSize:F0}m chunks={chunks.Count} scene={ScenePath} ms={stopwatch.ElapsedMilliseconds} screenshots={GetScreenshotRoot()}");
            }
            finally
            {
                EditorSceneManager.CloseScene(previewScene, true);
                if (activeScene.IsValid())
                    SceneManager.SetActiveScene(activeScene);
            }
        }

        private static int[,] ConfigureTerrainData(TerrainData terrainData, TerrainBuildResult result, WorldSettings settings)
        {
            int resolution = result.heights.GetLength(0);
            terrainData.heightmapResolution = resolution;
            terrainData.alphamapResolution = Mathf.ClosestPowerOfTwo(resolution - 1);
            terrainData.size = new Vector3(settings.worldSize, settings.heightScale, settings.worldSize);
            terrainData.SetHeights(0, 0, result.heights);
            terrainData.terrainLayers = new[]
            {
                GetOrCreateTerrainLayer("Meadow", new Color(0.23f, 0.39f, 0.16f), new Vector2(26f, 26f)),
                GetOrCreateTerrainLayer("Dirt", new Color(0.35f, 0.25f, 0.15f), new Vector2(20f, 20f)),
                GetOrCreateTerrainLayer("Rock", new Color(0.31f, 0.32f, 0.28f), new Vector2(23f, 23f)),
                GetOrCreateTerrainLayer("Mountain", new Color(0.48f, 0.46f, 0.40f), new Vector2(30f, 30f)),
                GetOrCreateTerrainLayer("Snow", new Color(0.78f, 0.86f, 0.91f), new Vector2(22f, 22f))
            };
            int alphaResolution = terrainData.alphamapResolution;
            int sourceResolution = result.heights.GetLength(0);
            var maps = new float[alphaResolution, alphaResolution, 5];
            for (int z = 0; z < alphaResolution; z++)
            for (int x = 0; x < alphaResolution; x++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)(alphaResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
                int sz = Mathf.Clamp(Mathf.RoundToInt(z / (float)(alphaResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
                WorldSample sample = result.samples[sz, sx];
                float slope = result.slopes[sz, sx] / 90f;
                float mountain = NoiseGenerator.SmoothMask(sample.mountainMask + sample.height01 * 0.25f, 0.36f, 0.68f);
                float rock = Mathf.Clamp01(slope * 1.35f + mountain * 0.7f + sample.rockDensity * 0.16f - 0.18f);
                float dirt = Mathf.Clamp01(0.18f + slope * 0.5f + (sample.biome == BiomeKind.Coast ? 0.48f : 0f));
                float grass = Mathf.Clamp01(1f - rock * 0.75f - mountain * 0.78f - dirt * 0.30f);
                float mountainRock = Mathf.Clamp01(mountain * 0.90f + rock * mountain * 0.55f);
                float snow = sample.snowMask;
                float uncovered = 1f - snow;
                grass *= uncovered;
                dirt *= uncovered;
                rock *= uncovered;
                mountainRock *= uncovered;
                float total = Mathf.Max(0.001f, grass + dirt + rock + mountainRock + snow);
                maps[z, x, 0] = grass / total;
                maps[z, x, 1] = dirt / total;
                maps[z, x, 2] = rock / total;
                maps[z, x, 3] = mountainRock / total;
                maps[z, x, 4] = snow / total;
            }
            terrainData.SetAlphamaps(0, 0, maps);
            int[,] grassDensityMap = BuildGrassDensityMap(result, settings);
            terrainData.SetDetailResolution(grassDensityMap.GetLength(0), 16);
            terrainData.detailPrototypes = Array.Empty<DetailPrototype>();
            EditorUtility.SetDirty(terrainData);
            return grassDensityMap;
        }

        private static int[,] BuildGrassDensityMap(TerrainBuildResult result, WorldSettings settings)
        {
            const int detailResolution = 512;
            var densityMap = new int[detailResolution, detailResolution];
            int sourceResolution = result.heights.GetLength(0);
            int grassClumpCount = 0;
            for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)(detailResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
                int sz = Mathf.Clamp(Mathf.RoundToInt(z / (float)(detailResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
                WorldSample sample = result.samples[sz, sx];
                float slope = result.slopes[sz, sx];
                if (!WorldPlacementRules.AllowsGrass(sample, slope, settings))
                    continue;

                float patch = NoiseGenerator.SmoothMask(sample.grassDensity, 0.40f, 0.64f);
                float biomeMultiplier = sample.biome == BiomeKind.Forest ? 0.62f : 1f;
                float slopeMultiplier = 1f - NoiseGenerator.SmoothMask(slope, settings.grassSlopeLimit * 0.55f, settings.grassSlopeLimit);
                float density = settings.grassDensity * patch * biomeMultiplier * slopeMultiplier * (1f - sample.snowMask);
                densityMap[z, x] = Mathf.Clamp(Mathf.RoundToInt(density * 14f), 0, 24);
                grassClumpCount += densityMap[z, x];
            }
            Debug.Log($"PROCEDURAL_WORLD_GRASS_OK clumps={grassClumpCount} detailResolution={detailResolution} seed={settings.seed}");
            return densityMap;
        }

        private static void CreateGrassMeshes(Transform root, Scene scene, Terrain terrain, int[,] densityMap, TerrainBuildResult result, WorldSettings settings)
        {
            Mesh grassMesh = GetOrCreateGrassMeshAsset();
            Material grassMaterial = GetOrCreateGrassMaterial();
            int detailResolution = densityMap.GetLength(0);
            int chunkCount = Mathf.CeilToInt(settings.worldSize / settings.chunkSize);
            int[] clumpsPerChunk = new int[chunkCount * chunkCount];
            float cellSize = settings.worldSize / detailResolution;
            for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                int count = densityMap[z, x];
                if (count <= 0)
                    continue;
                int chunkX = Mathf.Min(Mathf.FloorToInt((x + 0.5f) * cellSize / settings.chunkSize), chunkCount - 1);
                int chunkZ = Mathf.Min(Mathf.FloorToInt((z + 0.5f) * cellSize / settings.chunkSize), chunkCount - 1);
                clumpsPerChunk[chunkZ * chunkCount + chunkX] += count;
            }

            var instancesByChunk = new List<CombineInstance>[chunkCount * chunkCount];
            for (int i = 0; i < instancesByChunk.Length; i++)
                instancesByChunk[i] = new List<CombineInstance>(clumpsPerChunk[i]);

            var random = new System.Random(settings.seed ^ 0x2C9277B5);
            TerrainData terrainData = terrain.terrainData;
            int sourceResolution = result.heights.GetLength(0);
            for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                int count = densityMap[z, x];
                if (count <= 0)
                    continue;

                float cellStartX = x * cellSize;
                float cellStartZ = z * cellSize;
                for (int i = 0; i < count; i++)
                {
                    float worldX = cellStartX + (float)random.NextDouble() * cellSize;
                    float worldZ = cellStartZ + (float)random.NextDouble() * cellSize;
                    float u = worldX / settings.worldSize;
                    float v = worldZ / settings.worldSize;
                    int sx = Mathf.Clamp(Mathf.RoundToInt(u * (sourceResolution - 1)), 0, sourceResolution - 1);
                    int sz = Mathf.Clamp(Mathf.RoundToInt(v * (sourceResolution - 1)), 0, sourceResolution - 1);
                    WorldSample sample = result.samples[sz, sx];
                    if (!WorldPlacementRules.AllowsGrass(sample, result.slopes[sz, sx], settings))
                        continue;

                    int chunkX = Mathf.Min(Mathf.FloorToInt(worldX / settings.chunkSize), chunkCount - 1);
                    int chunkZ = Mathf.Min(Mathf.FloorToInt(worldZ / settings.chunkSize), chunkCount - 1);
                    int chunkIndex = chunkZ * chunkCount + chunkX;
                    var localPosition = new Vector3(
                        worldX - chunkX * settings.chunkSize,
                        terrainData.GetInterpolatedHeight(u, v) + 0.015f,
                        worldZ - chunkZ * settings.chunkSize);
                    float scale = Mathf.Lerp(0.80f, 1.25f, (float)random.NextDouble());
                    var combine = new CombineInstance
                    {
                        mesh = grassMesh,
                        transform = Matrix4x4.TRS(localPosition, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Vector3.one * scale)
                    };
                    instancesByChunk[chunkIndex].Add(combine);
                }
            }

            int totalClumps = 0;
            int totalTriangles = 0;
            for (int z = 0; z < chunkCount; z++)
            for (int x = 0; x < chunkCount; x++)
            {
                int index = z * chunkCount + x;
                string assetPath = $"{GrassRoot}/GrassChunk_{x}_{z}.asset";
                Mesh chunkMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (chunkMesh == null)
                {
                    chunkMesh = new Mesh { name = $"GrassChunk_{x}_{z}" };
                    AssetDatabase.CreateAsset(chunkMesh, assetPath);
                }

                chunkMesh.Clear();
                chunkMesh.indexFormat = IndexFormat.UInt32;
                if (instancesByChunk[index].Count > 0)
                    chunkMesh.CombineMeshes(instancesByChunk[index].ToArray(), true, true, false);
                chunkMesh.RecalculateBounds();
                EditorUtility.SetDirty(chunkMesh);

                if (instancesByChunk[index].Count == 0)
                    continue;
                var grassChunk = CreateChild($"Grass Patch ({x}, {z})", root, scene);
                grassChunk.transform.position = new Vector3(x * settings.chunkSize, 0f, z * settings.chunkSize);
                grassChunk.AddComponent<MeshFilter>().sharedMesh = chunkMesh;
                MeshRenderer renderer = grassChunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = grassMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                totalClumps += instancesByChunk[index].Count;
                totalTriangles += (int)(chunkMesh.GetIndexCount(0) / 3);
            }
            Debug.Log($"PROCEDURAL_WORLD_GRASS_MESHES_OK chunks={chunkCount * chunkCount} clumps={totalClumps} triangles={totalTriangles} seed={settings.seed}");
        }

        private static GameObject GetOrCreateGrassPrefab()
        {
            Mesh mesh = GetOrCreateGrassMeshAsset();
            Material material = GetOrCreateGrassMaterial();
            bool isExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath) != null;
            if (isExistingPrefab)
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(GrassPrefabPath);
                try
                {
                    ConfigureGrassPrefabRoot(prefabContents, mesh, material);
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, GrassPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
            else
            {
                GameObject prefabRoot = new GameObject("GrassClump");
                try
                {
                    ConfigureGrassPrefabRoot(prefabRoot, mesh, material);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, GrassPrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
            AssetDatabase.ImportAsset(GrassPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath);
            if (prefab == null || prefab.GetComponent<MeshFilter>() == null || prefab.GetComponent<MeshFilter>().sharedMesh == null)
                throw new InvalidOperationException("Grass detail prefab is missing its mesh filter or mesh.");
            return prefab;
        }

        private static Mesh GetOrCreateGrassMeshAsset()
        {
            EnsureFolder(GrassRoot);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GrassMeshPath);
            Mesh regenerated = CreateGrassClumpMesh();
            if (mesh == null)
            {
                mesh = regenerated;
                AssetDatabase.CreateAsset(mesh, GrassMeshPath);
            }
            else
            {
                mesh.Clear();
                mesh.SetVertices(regenerated.vertices);
                mesh.SetNormals(regenerated.normals);
                mesh.SetTriangles(regenerated.triangles, 0);
                mesh.RecalculateBounds();
                UnityEngine.Object.DestroyImmediate(regenerated);
                EditorUtility.SetDirty(mesh);
            }
            return mesh;
        }

        private static void ConfigureGrassPrefabRoot(GameObject root, Mesh mesh, Material material)
        {
            MeshFilter filter = root.GetComponent<MeshFilter>();
            if (filter == null)
                filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Mesh CreateGrassClumpMesh()
        {
            var vertices = new List<Vector3>(60);
            var normals = new List<Vector3>(60);
            var triangles = new List<int>(60);
            var random = new System.Random(847291);
            const int bladeCount = 8;
            for (int i = 0; i < bladeCount; i++)
            {
                float angle = i * Mathf.PI * 2f / bladeCount + (float)random.NextDouble() * 0.35f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-direction.z, 0f, direction.x) * Mathf.Lerp(0.10f, 0.16f, (float)random.NextDouble());
                Vector3 basePoint = direction * Mathf.Lerp(0f, 0.13f, (float)random.NextDouble());
                Vector3 tip = basePoint + direction * Mathf.Lerp(0.18f, 0.48f, (float)random.NextDouble())
                    + Vector3.up * Mathf.Lerp(0.58f, 0.94f, (float)random.NextDouble());
                AddGrassBlade(vertices, normals, triangles, basePoint - side, basePoint + side, tip);
            }

            var mesh = new Mesh { name = "GrassClump" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddGrassBlade(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 left, Vector3 right, Vector3 tip)
        {
            int first = vertices.Count;
            Vector3 normal = Vector3.Cross(right - left, tip - left).normalized;
            vertices.Add(left);
            vertices.Add(right);
            vertices.Add(tip);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);

            int back = vertices.Count;
            vertices.Add(left);
            vertices.Add(right);
            vertices.Add(tip);
            normals.Add(-normal);
            normals.Add(-normal);
            normals.Add(-normal);
            triangles.Add(back);
            triangles.Add(back + 2);
            triangles.Add(back + 1);
        }

        private static List<WorldChunk> CreateChunks(Transform root, Scene scene, WorldSettings settings)
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

        private static void SpawnWorldObjects(List<WorldChunk> chunks, Scene scene, Terrain terrain, TerrainBuildResult terrainResult, WorldSettings settings, GameObject[] rocks, GameObject[] trees, GameObject[] summitTrees, GameObject[] cliffs, MaterialSet materials)
        {
            int spawnedRocks = 0;
            int spawnedTrees = 0;
            int spawnedCliffs = 0;
            foreach (WorldChunk chunk in chunks)
            {
                var rockRoot = CreateChild("Rocks", chunk.transform, scene);
                var treeRoot = CreateChild("Vegetation", chunk.transform, scene);
                var cliffRoot = CreateChild("Cliffs", chunk.transform, scene);
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

            int summitTreeCount = SpawnSummitGrove(chunks, scene, terrain, terrainResult, settings, summitTrees, materials);
            Debug.Log($"PROCEDURAL_WORLD_SPAWN_OK trees={spawnedTrees} summitTrees={summitTreeCount} rocks={spawnedRocks} cliffs={spawnedCliffs}");
        }

        private static int SpawnSummitGrove(List<WorldChunk> chunks, Scene scene, Terrain terrain, TerrainBuildResult terrainResult, WorldSettings settings, GameObject[] trees, MaterialSet materials)
        {
            if (trees == null || trees.Length < 4 || terrainResult == null || terrainResult.heights == null)
                return 0;

            Vector3 goatSpawn = SummitSpawnUtility.FindStableGroundPoint(terrainResult, settings);
            float vistaYaw = SummitSpawnUtility.FindVistaYaw(terrainResult, settings, goatSpawn);
            Vector3 vistaDirection = new Vector3(Mathf.Sin(vistaYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(vistaYaw * Mathf.Deg2Rad));
            WorldChunk summitChunk = FindSummitChunk(chunks, goatSpawn);
            if (summitChunk == null)
                return 0;

            Transform groveRoot = CreateChild("Summit Grove — goat spawn clearing", summitChunk.transform, scene).transform;
            int resolution = terrainResult.heights.GetLength(0);
            float spacing = settings.worldSize / (resolution - 1);
            int peakX = Mathf.Clamp(Mathf.RoundToInt(goatSpawn.x / spacing), 0, resolution - 1);
            int peakZ = Mathf.Clamp(Mathf.RoundToInt(goatSpawn.z / spacing), 0, resolution - 1);
            int targetTrees = Mathf.RoundToInt(130f * Mathf.Clamp(settings.vegetationDensity, 0.35f, 1.25f));
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
            instance.transform.localScale = Vector3.one * FbxUnitCompensation * scale;
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
                    lods.Add(new LOD(level == 0 ? 0.38f : level == 1 ? 0.15f : 0.04f, renderers.ToArray()));
            }
            if (lods.Count == 0)
                return;
            LODGroup group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
        }

        private static GameObject CreateChild(string name, Transform parent, Scene scene)
        {
            var result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, scene);
            result.transform.SetParent(parent);
            return result;
        }

        private static void CreateWater(Transform root, Scene scene, WorldSettings settings, Material material)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Coast water";
            SceneManager.MoveGameObjectToScene(water, scene);
            water.transform.SetParent(root);
            water.transform.position = new Vector3(settings.worldSize * 0.5f, settings.seaLevel * settings.heightScale, settings.worldSize * 0.5f);
            water.transform.localScale = new Vector3(settings.worldSize / 10f, 1f, settings.worldSize / 10f);
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLighting(Transform root, Scene scene)
        {
            var lightObject = CreateChild("Directional light", root, scene);
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.12f;
            sun.color = new Color(1f, 0.84f, 0.67f);
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        }

        private static void CreateCaptureCameras(Transform root, Scene scene, Terrain terrain, TerrainBuildResult result, int[,] grassDensityMap, WorldSettings settings)
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
            var cameraObject = CreateChild(name, root, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.27f, 0.43f, 0.55f);
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
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string GetScreenshotRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "goat-procedural-world-milestone");
            Directory.CreateDirectory(root);
            return root;
        }

        private static GameObject[] LoadPrefabs(IEnumerable<string> paths)
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

        private static TerrainData GetOrCreateTerrainData()
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data != null)
                return data;
            data = new TerrainData { name = "ProceduralWorldTerrainData" };
            AssetDatabase.CreateAsset(data, TerrainDataPath);
            return data;
        }

        private static TerrainLayer GetOrCreateTerrainLayer(string name, Color color, Vector2 tileSize)
        {
            string texturePath = $"{MaterialRoot}/{name}TerrainTexture.asset";
            string layerPath = $"{MaterialRoot}/{name}TerrainLayer.asset";
            const int textureSize = 128;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false) { name = $"{name} Terrain Texture" };
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            else if (texture.width != textureSize || texture.height != textureSize)
            {
                if (!texture.Reinitialize(textureSize, textureSize, TextureFormat.RGBA32, false))
                    throw new InvalidOperationException($"Could not resize generated terrain texture '{texturePath}'.");
            }

            Color[] pixels = new Color[textureSize * textureSize];
            Color secondary = GetTerrainSecondaryColor(name, color);
            int textureSeed = StableNameSeed(name);
            for (int y = 0; y < textureSize; y++)
            for (int x = 0; x < textureSize; x++)
            {
                float u = x / (float)(textureSize - 1);
                float v = y / (float)(textureSize - 1);
                float broad = TileableNoise(u, v, textureSeed + 11, 4f);
                float medium = TileableNoise(u, v, textureSeed + 37, 11f);
                float fine = TileableNoise(u, v, textureSeed + 83, 27f);
                float variation = (broad - 0.5f) * 0.48f + (medium - 0.5f) * 0.28f + (fine - 0.5f) * 0.14f;
                float brightness = Mathf.Clamp(1f + variation, 0.76f, 1.18f);
                float paletteBlend = Mathf.SmoothStep(0.40f, 0.68f, broad) * 0.22f;
                pixels[y * textureSize + x] = Color.Lerp(color, secondary, paletteBlend) * brightness;
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 2;
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);

            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer { name = $"{name} Terrain Layer" };
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.diffuseTexture = texture;
            layer.tileSize = tileSize;
            layer.metallic = 0f;
            layer.smoothness = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Color GetTerrainSecondaryColor(string name, Color baseColor)
        {
            if (name == "Meadow")
                return new Color(0.34f, 0.38f, 0.17f);
            if (name == "Dirt")
                return new Color(0.43f, 0.31f, 0.19f);
            if (name == "Rock")
                return new Color(0.39f, 0.38f, 0.33f);
            if (name == "Mountain")
                return new Color(0.55f, 0.52f, 0.45f);
            if (name == "Snow")
                return new Color(0.66f, 0.75f, 0.82f);
            return Color.Lerp(baseColor, Color.white, 0.12f);
        }

        private static int StableNameSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * 16777619;
                return (int)hash;
            }
        }

        private static float TileableNoise(float u, float v, int seed, float frequency)
        {
            float offsetX = NoiseGenerator.Hash01(seed) * 8192f;
            float offsetY = NoiseGenerator.Hash01(seed + 197) * 8192f;
            float x = u * frequency + offsetX;
            float y = v * frequency + offsetY;
            float left = Mathf.Lerp(
                Mathf.PerlinNoise(x, y),
                Mathf.PerlinNoise(x - frequency, y),
                Mathf.SmoothStep(0f, 1f, u));
            float right = Mathf.Lerp(
                Mathf.PerlinNoise(x, y - frequency),
                Mathf.PerlinNoise(x - frequency, y - frequency),
                Mathf.SmoothStep(0f, 1f, u));
            return Mathf.Lerp(left, right, Mathf.SmoothStep(0f, 1f, v));
        }

        private static MaterialSet GetOrCreateMaterials()
        {
            Shader foliageShader = Shader.Find("GoatDescent/PineFoliage");
            Material summitFoliage = GetOrCreateMaterial("M_ProceduralSummitFoliage", Color.white, foliageShader);
            if (summitFoliage.HasProperty("_FrostAmount"))
                summitFoliage.SetFloat("_FrostAmount", 0.84f);
            if (summitFoliage.HasProperty("_FrostTint"))
                summitFoliage.SetColor("_FrostTint", new Color(0.86f, 0.93f, 0.99f, 1f));
            if (summitFoliage.HasProperty("_Color"))
                summitFoliage.SetColor("_Color", new Color(0.90f, 0.99f, 0.91f, 1f));
            if (summitFoliage.HasProperty("_FoliageGlow"))
                summitFoliage.SetFloat("_FoliageGlow", 0.20f);
            summitFoliage.enableInstancing = true;
            EditorUtility.SetDirty(summitFoliage);
            return new MaterialSet
            {
                rock = GetOrCreateMaterial("M_ProceduralRock", new Color(0.31f, 0.32f, 0.25f)),
                trunk = GetOrCreateMaterial("M_ProceduralTrunk", new Color(0.25f, 0.15f, 0.09f)),
                foliage = GetOrCreateMaterial(
                    "M_ProceduralFoliage",
                    Color.white,
                    foliageShader),
                summitFoliage = summitFoliage,
                water = GetOrCreateMaterial("M_ProceduralWater", new Color(0.10f, 0.34f, 0.48f))
            };
        }

        private static Material GetOrCreateMaterial(string name, Color color, Shader shaderOverride = null)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shaderOverride != null ? shaderOverride : Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shaderOverride != null && material.shader != shaderOverride)
                material.shader = shaderOverride;
            material.color = color;
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.16f);
            if (material.HasProperty("_FoliageGlow"))
                material.SetFloat("_FoliageGlow", 0.20f);
            if (name.Contains("Foliage") || name.Contains("Grass"))
                material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateGrassMaterial()
        {
            Shader foliageShader = Shader.Find("GoatDescent/PineFoliage");
            Material grass = GetOrCreateMaterial(
                "M_ProceduralGrass",
                new Color(0.31f, 0.48f, 0.19f),
                foliageShader);
            if (grass.HasProperty("_WindAmplitude"))
                grass.SetFloat("_WindAmplitude", 0.14f);
            if (grass.HasProperty("_WindHeight"))
                grass.SetFloat("_WindHeight", 1.08f);
            if (grass.HasProperty("_WindSpeed"))
                grass.SetFloat("_WindSpeed", 1.05f);
            if (grass.HasProperty("_FoliageGlow"))
                grass.SetFloat("_FoliageGlow", 0.24f);
            grass.enableInstancing = true;
            EditorUtility.SetDirty(grass);
            return grass;
        }

        private static void GenerateDebugTextures(WorldSettings settings, TerrainBuildResult result)
        {
            int size = 256;
            foreach (DebugMode mode in Enum.GetValues(typeof(DebugMode)))
            {
                string path = $"{DebugRoot}/{mode}.asset";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = $"Procedural World {mode}" };
                    AssetDatabase.CreateAsset(texture, path);
                }
                for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                {
                    int sx = Mathf.RoundToInt(x / (float)(size - 1) * (result.heights.GetLength(1) - 1));
                    int sz = Mathf.RoundToInt(z / (float)(size - 1) * (result.heights.GetLength(0) - 1));
                    WorldSample sample = result.samples[sz, sx];
                    float slope = result.slopes[sz, sx];
                    texture.SetPixel(x, z, DebugColor(mode, sample, slope, settings));
                }
                texture.Apply();
                EditorUtility.SetDirty(texture);
            }
        }

        private static Color DebugColor(DebugMode mode, WorldSample sample, float slope, WorldSettings settings)
        {
            switch (mode)
            {
                case DebugMode.Height: return Color.Lerp(Color.black, Color.white, sample.height01);
                case DebugMode.MountainMask: return Color.Lerp(Color.black, Color.red, sample.mountainMask);
                case DebugMode.Biomes: return sample.biome == BiomeKind.Coast ? new Color(0.12f, 0.42f, 0.70f) : sample.biome == BiomeKind.Forest ? new Color(0.08f, 0.38f, 0.12f) : sample.biome == BiomeKind.Mountain ? new Color(0.62f, 0.58f, 0.48f) : new Color(0.42f, 0.72f, 0.22f);
                case DebugMode.Temperature: return Color.Lerp(Color.blue, Color.red, sample.temperature);
                case DebugMode.Humidity: return Color.Lerp(new Color(0.25f, 0.18f, 0.05f), Color.cyan, sample.humidity);
                case DebugMode.Slope: return Color.Lerp(Color.black, Color.white, slope / 90f);
                case DebugMode.ForestDensity: return Color.Lerp(Color.black, Color.green, sample.forestDensity);
                case DebugMode.RockDensity: return Color.Lerp(Color.black, new Color(0.65f, 0.65f, 0.65f), sample.rockDensity);
                case DebugMode.CliffMask: return WorldPlacementRules.AllowsCliff(sample, slope, settings) ? Color.magenta : Color.black;
                case DebugMode.SnowMask: return Color.Lerp(new Color(0.08f, 0.12f, 0.18f), Color.white, sample.snowMask);
                case DebugMode.GrassDensity: return Color.Lerp(Color.black, new Color(0.38f, 0.72f, 0.24f), sample.grassDensity);
                default: return Color.black;
            }
        }

        private static void EnsureBiomeDefinition(string name, BiomeKind kind, Color color, float vegetation, float rock, float cliff)
        {
            string path = $"Assets/ProceduralWorld/Data/Biomes/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path) != null)
                return;
            var definition = ScriptableObject.CreateInstance<BiomeDefinition>();
            definition.name = name;
            definition.biome = kind;
            definition.previewColor = color;
            definition.vegetationMultiplier = vegetation;
            definition.rockMultiplier = rock;
            definition.cliffMultiplier = cliff;
            AssetDatabase.CreateAsset(definition, path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/ProceduralWorld");
            EnsureFolder("Assets/ProceduralWorld/Runtime");
            EnsureFolder("Assets/ProceduralWorld/Editor");
            EnsureFolder("Assets/ProceduralWorld/Data");
            EnsureFolder("Assets/ProceduralWorld/Data/Biomes");
            EnsureFolder("Assets/ProceduralWorld/Generated");
            EnsureFolder("Assets/ProceduralWorld/Generated/Terrain");
            EnsureFolder(MaterialRoot);
            EnsureFolder(DebugRoot);
            EnsureFolder(GrassRoot);
            EnsureFolder("Assets/ProceduralWorld/Scenes");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.path == scenePath)
                    return;

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private struct MaterialSet
        {
            public Material rock;
            public Material trunk;
            public Material foliage;
            public Material summitFoliage;
            public Material water;
        }

        private enum DebugMode
        {
            Height,
            MountainMask,
            Biomes,
            Temperature,
            Humidity,
            Slope,
            ForestDensity,
            RockDensity,
            CliffMask,
            SnowMask,
            GrassDensity
        }
    }
}
