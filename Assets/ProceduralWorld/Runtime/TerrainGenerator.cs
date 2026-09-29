using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class TerrainBuildResult
    {
        public float[,] heights;
        public WorldSample[,] samples;
        public float[,] slopes;
        public Vector3 highestPoint;
    }

    public static class TerrainGenerator
    {
        public static TerrainBuildResult Build(WorldSettings settings)
        {
            int resolution = NormalizeResolution(settings.terrainResolution);

            var result = new TerrainBuildResult
            {
                heights = new float[resolution, resolution],
                samples = new WorldSample[resolution, resolution],
                slopes = new float[resolution, resolution]
            };
            float maxHeight = -1f;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x / (float)(resolution - 1) * settings.worldSize;
                float worldZ = z / (float)(resolution - 1) * settings.worldSize;
                WorldSample sample = BiomeGenerator.Sample(worldX, worldZ, settings);
                result.samples[z, x] = sample;
                result.heights[z, x] = sample.height01;
                if (sample.height01 > maxHeight)
                {
                    maxHeight = sample.height01;
                    result.highestPoint = new Vector3(worldX, sample.height01 * settings.heightScale, worldZ);
                }
            }
            CalculateSlopes(result, settings);
            return result;
        }

        public static float SampleSlope(float worldX, float worldZ, WorldSettings settings)
        {
            const float step = 5f;
            float left = BiomeGenerator.Sample(worldX - step, worldZ, settings).height01 * settings.heightScale;
            float right = BiomeGenerator.Sample(worldX + step, worldZ, settings).height01 * settings.heightScale;
            float down = BiomeGenerator.Sample(worldX, worldZ - step, settings).height01 * settings.heightScale;
            float up = BiomeGenerator.Sample(worldX, worldZ + step, settings).height01 * settings.heightScale;
            return Mathf.Atan(Mathf.Sqrt((right - left) * (right - left) + (up - down) * (up - down)) / (step * 2f)) * Mathf.Rad2Deg;
        }

        private static int NormalizeResolution(int requested)
        {
            int value = Mathf.Clamp(requested, 33, 1025);
            int power = Mathf.ClosestPowerOfTwo(value - 1);
            return power + 1;
        }

        private static void CalculateSlopes(TerrainBuildResult result, WorldSettings settings)
        {
            int resolution = result.heights.GetLength(0);
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
        }
    }

    /// <summary>
    /// Shared summit placement so gameplay spawn and summit dressing use the same stable ground point.
    /// </summary>
    public static class SummitSpawnUtility
    {
        public static Vector3 FindStableGroundPoint(TerrainBuildResult terrain, WorldSettings settings)
        {
            int resolution = terrain.heights.GetLength(0);
            float spacing = settings.worldSize / (resolution - 1);
            int peakX = Mathf.RoundToInt(terrain.highestPoint.x / spacing);
            int peakZ = Mathf.RoundToInt(terrain.highestPoint.z / spacing);
            int radius = Mathf.CeilToInt(32f / spacing);
            float minimumHeight = terrain.highestPoint.y - 12f;
            float bestHeight = float.NegativeInfinity;
            float bestSlope = float.PositiveInfinity;
            int bestX = peakX;
            int bestZ = peakZ;

            for (int z = Mathf.Max(0, peakZ - radius); z <= Mathf.Min(resolution - 1, peakZ + radius); z++)
            for (int x = Mathf.Max(0, peakX - radius); x <= Mathf.Min(resolution - 1, peakX + radius); x++)
            {
                float dx = (x - peakX) * spacing;
                float dz = (z - peakZ) * spacing;
                if (dx * dx + dz * dz > 32f * 32f)
                    continue;

                float height = terrain.heights[z, x] * settings.heightScale;
                float slope = terrain.slopes[z, x];
                if (height < minimumHeight || slope > 12f)
                    continue;

                if (height > bestHeight + 0.01f || (Mathf.Abs(height - bestHeight) <= 0.01f && slope < bestSlope))
                {
                    bestHeight = height;
                    bestSlope = slope;
                    bestX = x;
                    bestZ = z;
                }
            }

            if (float.IsNegativeInfinity(bestHeight))
                return terrain.highestPoint;

            return new Vector3(bestX * spacing, bestHeight, bestZ * spacing);
        }

        public static float FindVistaYaw(TerrainBuildResult terrain, WorldSettings settings, Vector3 spawn)
        {
            int resolution = terrain.heights.GetLength(0);
            float spacing = settings.worldSize / (resolution - 1);
            float lowestHeight = float.PositiveInfinity;
            float bestYaw = -27f;

            // Aim toward the lowest nearby horizon so the summit camera opens onto the valley.
            for (int angle = -180; angle < 180; angle += 30)
            {
                float radians = angle * Mathf.Deg2Rad;
                float x = spawn.x + Mathf.Sin(radians) * 80f;
                float z = spawn.z + Mathf.Cos(radians) * 80f;
                int sampleX = Mathf.Clamp(Mathf.RoundToInt(x / spacing), 0, resolution - 1);
                int sampleZ = Mathf.Clamp(Mathf.RoundToInt(z / spacing), 0, resolution - 1);
                float height = terrain.heights[sampleZ, sampleX] * settings.heightScale;
                if (height < lowestHeight)
                {
                    lowestHeight = height;
                    bestYaw = angle;
                }
            }

            return bestYaw;
        }
    }
}
