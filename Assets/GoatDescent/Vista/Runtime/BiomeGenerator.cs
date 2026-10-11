using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public struct WorldSample
    {
        public float height01;
        public float continentalness;
        public float mountainMask;
        public float temperature;
        public float humidity;
        public float forestDensity;
        public float rockDensity;
        public float grassDensity;
        public float snowMask;
        public BiomeKind biome;
    }

    public static class BiomeGenerator
    {
        public static WorldSample Sample(float x, float z, WorldSettings settings)
        {
            Vector2 warp = NoiseGenerator.DomainWarp(x, z, settings.seed + 701, settings.biomeScale * 1.7f, 290f);
            float px = x + warp.x;
            float pz = z + warp.y;
            float half = settings.worldSize * 0.5f;
            float radial = Vector2.Distance(new Vector2(x, z), new Vector2(half, half)) / Mathf.Max(1f, half);
            float macro = NoiseGenerator.Fractal(px, pz, settings.seed + 7, settings.biomeScale * 0.72f, 4, 0.52f);
            float land = NoiseGenerator.SmoothMask(macro * 0.68f + (1f - radial) * 0.55f, 0.36f, 0.60f);
            float hills = NoiseGenerator.Fractal(px, pz, settings.seed + 41, settings.biomeScale * 2.4f, 4, 0.53f);
            float mountainField = NoiseGenerator.Fractal(px, pz, settings.seed + 83, settings.mountainFrequency, 3, 0.55f);
            float mountainMask = land * NoiseGenerator.SmoothMask(mountainField + macro * 0.20f, 0.52f, 0.75f);
            float ridges = NoiseGenerator.Ridged(px, pz, settings.seed + 137, settings.mountainFrequency * 3f, 4);
            float ridgeCrest = Mathf.Pow(ridges, 1.45f);
            float detail = NoiseGenerator.Fractal(px, pz, settings.seed + 179, settings.biomeScale * 8f, 3, 0.5f) - 0.5f;
            float height = settings.seaLevel - 0.035f + land * 0.35f + hills * land * 0.20f;
            height += mountainMask * (0.14f + ridgeCrest * 0.43f) * settings.mountainStrength;
            height += detail * land * (0.035f + settings.erosionStrength * 0.025f);
            height = Mathf.Clamp01(height);

            float temperature = Mathf.Clamp01(0.73f - height * 0.58f + (NoiseGenerator.Fractal(px, pz, settings.seed + 229, settings.biomeScale * 0.6f, 2) - 0.5f) * 0.22f);
            float humidity = Mathf.Clamp01(NoiseGenerator.Fractal(px, pz, settings.seed + 277, settings.biomeScale * 1.4f, 3) * 0.72f + (1f - radial) * 0.16f);
            float forestDensity = NoiseGenerator.Fractal(px, pz, settings.seed + 331, settings.biomeScale * 5f, 3, 0.52f);
            float rockDensity = NoiseGenerator.Fractal(px, pz, settings.seed + 389, settings.biomeScale * 7f, 3, 0.5f);
            float grassDensity = NoiseGenerator.Fractal(px, pz, settings.seed + 457, settings.biomeScale * 11f, 3, 0.52f);
            float snowHeight = NoiseGenerator.SmoothMask(height, settings.snowLine - 0.10f, settings.snowLine + 0.035f);
            float coldness = 1f - NoiseGenerator.SmoothMask(temperature, settings.snowTemperatureThreshold - 0.10f, settings.snowTemperatureThreshold + 0.10f);
            float snowVariation = Mathf.Lerp(0.86f, 1.08f, NoiseGenerator.Fractal(px, pz, settings.seed + 503, settings.biomeScale * 8f, 2, 0.55f));
            float snowMask = Mathf.Clamp01(snowHeight * coldness * settings.snowCoverage * snowVariation);
            BiomeKind biome = height <= settings.seaLevel + 0.006f && land < 0.28f ? BiomeKind.Coast
                : mountainMask > 0.30f || height > settings.seaLevel + 0.34f ? BiomeKind.Mountain
                : humidity > 0.38f && temperature > 0.28f && forestDensity > 0.34f ? BiomeKind.Forest
                : BiomeKind.Meadows;

            return new WorldSample
            {
                height01 = height,
                continentalness = land,
                mountainMask = mountainMask,
                temperature = temperature,
                humidity = humidity,
                forestDensity = forestDensity,
                rockDensity = rockDensity,
                grassDensity = grassDensity,
                snowMask = snowMask,
                biome = biome
            };
        }
    }
}
