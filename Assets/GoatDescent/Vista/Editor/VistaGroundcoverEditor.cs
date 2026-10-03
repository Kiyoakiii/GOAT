using System.Collections.Generic;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaGroundcoverEditor
    {
        internal static void CreateGrassMeshes(Transform root, Scene scene, Terrain terrain, int[,] densityMap, TerrainBuildResult result, WorldSettings settings)
        {
            var grassMeshes = new Mesh[VistaAssetPaths.GroundcoverMeshPaths.Length];
            for (int i = 0; i < grassMeshes.Length; i++)
                grassMeshes[i] = GetOrCreateGroundcoverMesh(i);
            Material grassMaterial = VistaMaterialsEditor.GetOrCreateGrassMaterial();
            int detailResolution = densityMap.GetLength(0);
            float groundcoverChunkSize = Mathf.Min(settings.chunkSize, 128f);
            int chunkCount = Mathf.CeilToInt(settings.worldSize / groundcoverChunkSize);
            int[] clumpsPerChunk = new int[chunkCount * chunkCount];
            float cellSize = settings.worldSize / detailResolution;
            for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                int count = densityMap[z, x];
                if (count <= 0)
                    continue;
                int chunkX = Mathf.Min(Mathf.FloorToInt((x + 0.5f) * cellSize / groundcoverChunkSize), chunkCount - 1);
                int chunkZ = Mathf.Min(Mathf.FloorToInt((z + 0.5f) * cellSize / groundcoverChunkSize), chunkCount - 1);
                clumpsPerChunk[chunkZ * chunkCount + chunkX] += count;
            }

            var instancesByChunk = new List<CombineInstance>[chunkCount * chunkCount];
            for (int i = 0; i < instancesByChunk.Length; i++)
                instancesByChunk[i] = new List<CombineInstance>(clumpsPerChunk[i]);

            var random = new System.Random(settings.seed ^ 0x2C9277B5);
            TerrainData terrainData = terrain.terrainData;
            int sourceResolution = result.heights.GetLength(0);
            var speciesCounts = new int[grassMeshes.Length];
            for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                int count = densityMap[z, x];
                if (count <= 0)
                    continue;

                float cellStartX = x * cellSize;
                float cellStartZ = z * cellSize;
                Vector2 clusterCenter = Vector2.zero;
                for (int i = 0; i < count; i++)
                {
                    if (i % 3 == 0)
                        clusterCenter = new Vector2(cellStartX + (float)random.NextDouble() * cellSize, cellStartZ + (float)random.NextDouble() * cellSize);
                    float clusterAngle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float clusterRadius = Mathf.Sqrt((float)random.NextDouble()) * 0.48f;
                    float worldX = Mathf.Clamp(clusterCenter.x + Mathf.Cos(clusterAngle) * clusterRadius, 0f, settings.worldSize - 0.001f);
                    float worldZ = Mathf.Clamp(clusterCenter.y + Mathf.Sin(clusterAngle) * clusterRadius, 0f, settings.worldSize - 0.001f);
                    float u = worldX / settings.worldSize;
                    float v = worldZ / settings.worldSize;
                    int sx = Mathf.Clamp(Mathf.RoundToInt(u * (sourceResolution - 1)), 0, sourceResolution - 1);
                    int sz = Mathf.Clamp(Mathf.RoundToInt(v * (sourceResolution - 1)), 0, sourceResolution - 1);
                    WorldSample sample = result.samples[sz, sx];
                    if (!WorldPlacementRules.AllowsGrass(sample, result.slopes[sz, sx], settings))
                        continue;

                    int chunkX = Mathf.Min(Mathf.FloorToInt(worldX / groundcoverChunkSize), chunkCount - 1);
                    int chunkZ = Mathf.Min(Mathf.FloorToInt(worldZ / groundcoverChunkSize), chunkCount - 1);
                    int chunkIndex = chunkZ * chunkCount + chunkX;
                    var localPosition = new Vector3(
                        worldX - chunkX * groundcoverChunkSize,
                        terrainData.GetInterpolatedHeight(u, v) + 0.015f,
                        worldZ - chunkZ * groundcoverChunkSize);
                    float scale = Mathf.Lerp(0.84f, 1.18f, (float)random.NextDouble());
                    int species = SelectGroundcoverVariant(sample, random);
                    var combine = new CombineInstance
                    {
                        mesh = grassMeshes[species],
                        transform = Matrix4x4.TRS(localPosition, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Vector3.one * scale)
                    };
                    instancesByChunk[chunkIndex].Add(combine);
                    speciesCounts[species]++;
                }
            }

            int totalClumps = 0;
            int totalTriangles = 0;
            for (int z = 0; z < chunkCount; z++)
            for (int x = 0; x < chunkCount; x++)
            {
                int index = z * chunkCount + x;
                string assetPath = $"{VistaAssetPaths.GrassRoot}/GrassChunk_{x}_{z}.asset";
                Mesh chunkMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (chunkMesh != null && !chunkMesh.isReadable)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    chunkMesh = null;
                }
                if (chunkMesh == null && instancesByChunk[index].Count == 0)
                    continue;
                if (chunkMesh == null)
                {
                    chunkMesh = new Mesh { name = $"GrassChunk_{x}_{z}" };
                    AssetDatabase.CreateAsset(chunkMesh, assetPath);
                }

                chunkMesh.Clear();
                int estimatedVertexCount = 0;
                for (int instance = 0; instance < instancesByChunk[index].Count; instance++)
                    estimatedVertexCount += instancesByChunk[index][instance].mesh.vertexCount;
                chunkMesh.indexFormat = estimatedVertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                if (instancesByChunk[index].Count > 0)
                {
                    chunkMesh.CombineMeshes(instancesByChunk[index].ToArray(), true, true, false);
                    MeshUtility.SetMeshCompression(chunkMesh, ModelImporterMeshCompression.Medium);
                }
                chunkMesh.RecalculateBounds();
                chunkMesh.UploadMeshData(true);
                EditorUtility.SetDirty(chunkMesh);

                if (instancesByChunk[index].Count == 0)
                    continue;
                var grassChunk = VistaAssetUtils.CreateChild($"Grass Patch ({x}, {z})", root, scene);
                grassChunk.transform.position = new Vector3(x * groundcoverChunkSize, 0f, z * groundcoverChunkSize);
                grassChunk.AddComponent<MeshFilter>().sharedMesh = chunkMesh;
                MeshRenderer renderer = grassChunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = grassMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                totalClumps += instancesByChunk[index].Count;
                totalTriangles += (int)(chunkMesh.GetIndexCount(0) / 3);
            }
            Debug.Log($"PROCEDURAL_WORLD_GROUNDCOVER_OK patches={chunkCount * chunkCount} patchSize={groundcoverChunkSize:F0}m plants={totalClumps} triangles={totalTriangles} meshCompression=Medium vertexColors=Color32 meadowGrass={speciesCounts[0]} alpineBloom={speciesCounts[1]} mountainHeather={speciesCounts[2]} frostJuniper={speciesCounts[3]} alpineBerryShrub={speciesCounts[4]} renderersPerVisiblePatch=1 shadowCasting=Off seed={settings.seed}");
        }

        internal static GameObject GetOrCreateGrassPrefab()
        {
            Mesh mesh = GetOrCreateGrassMeshAsset();
            Material material = VistaMaterialsEditor.GetOrCreateGrassMaterial();
            bool isExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VistaAssetPaths.GrassPrefabPath) != null;
            if (isExistingPrefab)
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(VistaAssetPaths.GrassPrefabPath);
                try
                {
                    ConfigureGrassPrefabRoot(prefabContents, mesh, material);
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, VistaAssetPaths.GrassPrefabPath);
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
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, VistaAssetPaths.GrassPrefabPath);
                }
                finally
                {
                    Object.DestroyImmediate(prefabRoot);
                }
            }
            AssetDatabase.ImportAsset(VistaAssetPaths.GrassPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VistaAssetPaths.GrassPrefabPath);
            if (prefab == null || prefab.GetComponent<MeshFilter>() == null || prefab.GetComponent<MeshFilter>().sharedMesh == null)
                throw new System.InvalidOperationException("Grass detail prefab is missing its mesh filter or mesh.");
            return prefab;
        }

        private static Mesh GetOrCreateGrassMeshAsset()
        {
            return GetOrCreateGroundcoverMesh(0);
        }

        private static Mesh GetOrCreateGroundcoverMesh(int variantIndex)
        {
            VistaAssetUtils.EnsureFolder(VistaAssetPaths.GrassRoot);
            string assetPath = VistaAssetPaths.GroundcoverMeshPaths[variantIndex];
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            Mesh regenerated = CreateGroundcoverMesh(variantIndex);
            if (mesh == null)
            {
                mesh = regenerated;
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
            else
            {
                mesh.Clear();
                mesh.indexFormat = regenerated.indexFormat;
                mesh.SetVertices(regenerated.vertices);
                mesh.SetNormals(regenerated.normals);
                mesh.SetColors(regenerated.colors32);
                mesh.SetTriangles(regenerated.triangles, 0);
                var windWeights = new List<Vector4>(regenerated.vertexCount);
                regenerated.GetUVs(3, windWeights);
                mesh.SetUVs(3, windWeights);
                mesh.RecalculateBounds();
                Object.DestroyImmediate(regenerated);
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

        private static Mesh CreateGroundcoverMesh(int variantIndex)
        {
            var vertices = new List<Vector3>(72);
            var normals = new List<Vector3>(72);
            var colors = new List<Color32>(72);
            var triangles = new List<int>(108);
            var random = new System.Random(847291 + variantIndex * 1297);

            switch (variantIndex)
            {
                case 0:
                    CreateMeadowGrass(vertices, normals, colors, triangles, random);
                    break;
                case 1:
                    CreateAlpineBloom(vertices, normals, colors, triangles, random);
                    break;
                case 2:
                    CreateMountainHeather(vertices, normals, colors, triangles, random);
                    break;
                case 3:
                    CreateFrostJuniper(vertices, normals, colors, triangles, random);
                    break;
                default:
                    CreateAlpineBerryShrub(vertices, normals, colors, triangles, random);
                    break;
            }

            var mesh = new Mesh { name = VistaAssetPaths.GroundcoverMeshNames[variantIndex] };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;
            for (int i = 0; i < vertices.Count; i++)
            {
                minimumHeight = Mathf.Min(minimumHeight, vertices[i].y);
                maximumHeight = Mathf.Max(maximumHeight, vertices[i].y);
            }
            float heightRange = Mathf.Max(0.001f, maximumHeight - minimumHeight);
            var windWeights = new List<Vector4>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                float normalizedHeight = Mathf.Clamp01((vertices[i].y - minimumHeight) / heightRange);
                windWeights.Add(new Vector4(normalizedHeight, 1f, 0f, 0f));
            }
            mesh.SetUVs(3, windWeights);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateMeadowGrass(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, System.Random random)
        {
            const int bladeCount = 6;
            Color baseColor = new Color(0.47f, 0.66f, 0.31f, 0f);
            Color tipColor = new Color(0.78f, 0.88f, 0.43f, 0.05f);
            for (int i = 0; i < bladeCount; i++)
            {
                float angle = i * Mathf.PI * 2f / bladeCount + (float)random.NextDouble() * 0.42f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-direction.z, 0f, direction.x) * Mathf.Lerp(0.035f, 0.065f, (float)random.NextDouble());
                Vector3 basePoint = direction * Mathf.Lerp(0f, 0.12f, (float)random.NextDouble());
                Vector3 tip = basePoint + direction * Mathf.Lerp(0.14f, 0.34f, (float)random.NextDouble())
                    + Vector3.up * Mathf.Lerp(0.48f, 0.83f, (float)random.NextDouble());
                AddFoliageLeaf(vertices, normals, colors, triangles, basePoint - side, basePoint + side, tip, baseColor, tipColor);
            }
        }

        private static void CreateAlpineBloom(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, System.Random random)
        {
            Color leafBase = new Color(0.44f, 0.64f, 0.41f, 0f);
            Color leafTip = new Color(0.70f, 0.82f, 0.50f, 0.12f);
            const int leafCount = 4;
            for (int i = 0; i < leafCount; i++)
            {
                float angle = i * Mathf.PI * 2f / leafCount + (float)random.NextDouble() * 0.28f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-direction.z, 0f, direction.x) * Mathf.Lerp(0.055f, 0.085f, (float)random.NextDouble());
                Vector3 basePoint = direction * 0.025f + Vector3.up * 0.025f;
                Vector3 tip = basePoint + direction * Mathf.Lerp(0.12f, 0.22f, (float)random.NextDouble())
                    + Vector3.up * Mathf.Lerp(0.22f, 0.34f, (float)random.NextDouble());
                AddFoliageLeaf(vertices, normals, colors, triangles, basePoint - side, basePoint + side, tip, leafBase, leafTip);
            }

            float flowerAngle = (float)random.NextDouble() * Mathf.PI * 2f;
            Vector3 flowerDirection = new Vector3(Mathf.Cos(flowerAngle), 0f, Mathf.Sin(flowerAngle));
            Vector3 stemTip = flowerDirection * 0.055f + Vector3.up * Mathf.Lerp(0.43f, 0.56f, (float)random.NextDouble());
            Vector3 stemSide = new Vector3(-flowerDirection.z, 0f, flowerDirection.x) * 0.012f;
            AddFoliageLeaf(vertices, normals, colors, triangles, -stemSide, stemSide, stemTip,
                new Color(0.38f, 0.60f, 0.32f, 0f), new Color(0.60f, 0.78f, 0.40f, 0.08f));
            double bloomRoll = random.NextDouble();
            Color bloomColor = bloomRoll < 0.34d
                ? new Color(0.88f, 0.47f, 0.66f, 0f)
                : bloomRoll < 0.68d
                    ? new Color(0.71f, 0.60f, 0.91f, 0f)
                    : new Color(0.96f, 0.74f, 0.34f, 0f);
            AddFlowerHead(vertices, normals, colors, triangles, stemTip, bloomColor);
        }

        private static void CreateMountainHeather(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, System.Random random)
        {
            Color leafBase = new Color(0.47f, 0.60f, 0.37f, 0.03f);
            Color leafTip = new Color(0.78f, 0.77f, 0.44f, 0.18f);
            const int sprayCount = 5;
            for (int i = 0; i < sprayCount; i++)
            {
                float angle = i * Mathf.PI * 2f / sprayCount + (float)random.NextDouble() * 0.34f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-direction.z, 0f, direction.x);
                Vector3 basePoint = direction * 0.08f + Vector3.up * 0.045f;
                Vector3 tip = basePoint + direction * Mathf.Lerp(0.28f, 0.43f, (float)random.NextDouble())
                    + Vector3.up * Mathf.Lerp(0.22f, 0.38f, (float)random.NextDouble());
                Vector3 side = tangent * Mathf.Lerp(0.075f, 0.11f, (float)random.NextDouble());
                AddFoliageLeaf(vertices, normals, colors, triangles, basePoint - side, basePoint + side, tip, leafBase, leafTip);
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f + 0.4f;
                Vector3 center = new Vector3(Mathf.Cos(angle) * 0.34f, 0.24f + i * 0.025f, Mathf.Sin(angle) * 0.34f);
                Vector3 side = new Vector3(0.034f, 0f, 0.026f);
                AddFoliageLeaf(vertices, normals, colors, triangles, center - side, center + side,
                    center + Vector3.up * 0.065f, new Color(0.71f, 0.43f, 0.46f, 0f), new Color(0.82f, 0.56f, 0.49f, 0.04f));
            }
        }

        private static void CreateFrostJuniper(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, System.Random random)
        {
            Color leafBase = new Color(0.39f, 0.60f, 0.58f, 0.22f);
            Color leafTip = new Color(0.81f, 0.91f, 0.91f, 0.88f);
            const int branchCount = 6;
            for (int i = 0; i < branchCount; i++)
            {
                float angle = i * Mathf.PI * 2f / branchCount + (float)random.NextDouble() * 0.3f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-direction.z, 0f, direction.x);
                Vector3 basePoint = direction * 0.035f + Vector3.up * 0.035f;
                Vector3 tip = basePoint + direction * Mathf.Lerp(0.24f, 0.37f, (float)random.NextDouble())
                    + Vector3.up * Mathf.Lerp(0.22f, 0.36f, (float)random.NextDouble());
                Vector3 side = tangent * Mathf.Lerp(0.045f, 0.07f, (float)random.NextDouble());
                AddFoliageLeaf(vertices, normals, colors, triangles, basePoint - side, basePoint + side, tip, leafBase, leafTip);
            }
        }

        private static void CreateAlpineBerryShrub(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, System.Random random)
        {
            Color stemBase = new Color(0.27f, 0.40f, 0.25f, 0.10f);
            Color stemTip = new Color(0.39f, 0.54f, 0.30f, 0.14f);
            Color leafBase = new Color(0.26f, 0.45f, 0.27f, 0.08f);
            Color leafTip = new Color(0.56f, 0.70f, 0.34f, 0.16f);
            const int branchCount = 6;
            for (int branch = 0; branch < branchCount; branch++)
            {
                float angle = branch * Mathf.PI * 2f / branchCount + (float)random.NextDouble() * 0.34f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-direction.z, 0f, direction.x);
                float length = Mathf.Lerp(0.27f, 0.43f, (float)random.NextDouble());
                float height = Mathf.Lerp(0.30f, 0.47f, (float)random.NextDouble());
                Vector3 basePoint = direction * 0.025f + Vector3.up * 0.025f;
                Vector3 branchTip = basePoint + direction * length + Vector3.up * height;
                Vector3 stemSide = tangent * 0.014f;
                AddFoliageLeaf(vertices, normals, colors, triangles, basePoint - stemSide, basePoint + stemSide, branchTip, stemBase, stemTip);

                for (int leaf = 0; leaf < 3; leaf++)
                {
                    float alongBranch = 0.30f + leaf * 0.20f;
                    Vector3 leafCenter = Vector3.Lerp(basePoint, branchTip, alongBranch);
                    float sideSign = leaf % 2 == 0 ? 1f : -1f;
                    Vector3 leafDirection = (direction * sideSign + Vector3.up * 0.18f).normalized;
                    Vector3 leafTipPosition = leafCenter + leafDirection * Mathf.Lerp(0.12f, 0.19f, (float)random.NextDouble());
                    Vector3 leafSide = tangent * Mathf.Lerp(0.055f, 0.082f, (float)random.NextDouble());
                    AddFoliageLeaf(vertices, normals, colors, triangles, leafCenter - leafSide, leafCenter + leafSide, leafTipPosition, leafBase, leafTip);
                }

                if (branch % 2 == 0)
                {
                    Vector3 berryCluster = Vector3.Lerp(basePoint, branchTip, 0.67f) + tangent * 0.035f;
                    Color berryColor = branch == 0
                        ? new Color(0.47f, 0.29f, 0.59f, 0.04f)
                        : new Color(0.68f, 0.31f, 0.34f, 0.04f);
                    AddLowPolyBerry(vertices, normals, colors, triangles, berryCluster, 0.035f, berryColor);
                    AddLowPolyBerry(vertices, normals, colors, triangles, berryCluster + direction * 0.045f + Vector3.up * 0.022f, 0.028f, berryColor);
                }
            }
        }

        private static void AddLowPolyBerry(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, Vector3 center, float radius, Color color)
        {
            Vector3 top = center + Vector3.up * radius;
            Vector3 bottom = center - Vector3.up * radius;
            Vector3[] ring =
            {
                center + Vector3.right * radius,
                center + Vector3.forward * radius,
                center - Vector3.right * radius,
                center - Vector3.forward * radius
            };

            for (int i = 0; i < ring.Length; i++)
            {
                int next = (i + 1) % ring.Length;
                AddBerryFace(vertices, normals, colors, triangles, top, ring[i], ring[next], center, color);
                AddBerryFace(vertices, normals, colors, triangles, bottom, ring[next], ring[i], center, color);
            }
        }

        private static void AddBerryFace(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c, Vector3 center, Color color)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(normal, (a + b + c) / 3f - center) < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
                normal = -normal;
            }

            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }

        private static void AddFlowerHead(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles, Vector3 center, Color petalColor)
        {
            const int petalCount = 6;
            for (int i = 0; i < petalCount; i++)
            {
                float angle = i * Mathf.PI * 2f / petalCount;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-direction.z, 0f, direction.x);
                Vector3 petalBase = center + direction * 0.012f;
                Vector3 petalTip = center + direction * 0.132f + Vector3.up * 0.008f;
                Vector3 side = tangent * 0.042f;
                AddFoliageLeaf(vertices, normals, colors, triangles, petalBase - side, petalBase + side, petalTip, petalColor, petalColor);
            }

            Vector3 centerSide = new Vector3(0.018f, 0f, 0f);
            AddFoliageLeaf(vertices, normals, colors, triangles, center - centerSide,
                center + centerSide, center + new Vector3(0f, 0.01f, 0.018f),
                new Color(0.96f, 0.84f, 0.48f, 0f), new Color(0.96f, 0.84f, 0.48f, 0f));
        }

        private static void AddFoliageLeaf(List<Vector3> vertices, List<Vector3> normals, List<Color32> colors, List<int> triangles,
            Vector3 left, Vector3 right, Vector3 tip, Color baseColor, Color tipColor)
        {
            int first = vertices.Count;
            Vector3 normal = Vector3.Cross(right - left, tip - left).normalized;
            vertices.Add(left);
            vertices.Add(right);
            vertices.Add(tip);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(baseColor);
            colors.Add(baseColor);
            colors.Add(tipColor);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }

        private static int SelectGroundcoverVariant(WorldSample sample, System.Random random)
        {
            double roll = random.NextDouble();
            if (sample.biome == BiomeKind.Mountain)
            {
                if (sample.snowMask > 0.16f && roll < 0.58d)
                    return 3;
                if (roll < 0.14d)
                    return 4;
                return roll < 0.49d ? 0 : roll < 0.72d ? 1 : 2;
            }

            if (sample.biome == BiomeKind.Forest)
                return roll < 0.14d ? 4 : roll < 0.49d ? 0 : roll < 0.66d ? 1 : roll < 0.95d ? 2 : 3;

            return roll < 0.14d ? 4 : roll < 0.55d ? 0 : roll < 0.72d ? 1 : roll < 0.92d ? 2 : 3;
        }
    }
}