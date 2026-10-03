using System;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaDebugTexturesEditor
    {
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

        internal static void GenerateDebugTextures(WorldSettings settings, TerrainBuildResult result)
        {
            int size = 256;
            foreach (DebugMode mode in Enum.GetValues(typeof(DebugMode)))
            {
                string path = $"{VistaAssetPaths.DebugRoot}/{mode}.asset";
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
    }
}