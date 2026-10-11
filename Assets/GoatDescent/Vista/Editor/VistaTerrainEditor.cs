using System;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaTerrainEditor
    {
        internal static int[,] ConfigureTerrainData(TerrainData terrainData, TerrainBuildResult result, WorldSettings settings, VegetationGenerationSettings vegetation)
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
            int[,] grassDensityMap = BuildGrassDensityMap(result, settings, vegetation);
            terrainData.SetDetailResolution(grassDensityMap.GetLength(0), 16);
            terrainData.detailPrototypes = Array.Empty<DetailPrototype>();
            EditorUtility.SetDirty(terrainData);
            return grassDensityMap;
        }

        private static int[,] BuildGrassDensityMap(TerrainBuildResult result, WorldSettings settings, VegetationGenerationSettings vegetation)
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
                float density = vegetation.grassDensity * patch * biomeMultiplier * slopeMultiplier * (1f - sample.snowMask);
                densityMap[z, x] = Mathf.Clamp(Mathf.RoundToInt(density * 12f), 0, 22);
                grassClumpCount += densityMap[z, x];
            }
            Debug.Log($"PROCEDURAL_WORLD_GRASS_OK clumps={grassClumpCount} detailResolution={detailResolution} seed={settings.seed}");
            return densityMap;
        }

        private static TerrainData GetOrCreateTerrainData()
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(VistaAssetPaths.TerrainDataPath);
            if (data != null)
                return data;
            data = new TerrainData { name = "ProceduralWorldTerrainData" };
            AssetDatabase.CreateAsset(data, VistaAssetPaths.TerrainDataPath);
            return data;
        }

        private static TerrainLayer GetOrCreateTerrainLayer(string name, Color color, Vector2 tileSize)
        {
            string texturePath = $"{VistaAssetPaths.MaterialRoot}/{name}TerrainTexture.asset";
            string layerPath = $"{VistaAssetPaths.MaterialRoot}/{name}TerrainLayer.asset";
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
    }
}