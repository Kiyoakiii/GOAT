using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public static class WorldPlacementRules
    {
        public static bool AllowsGrass(WorldSample sample, float slope, WorldSettings settings)
        {
            bool suitableBiome = sample.biome == BiomeKind.Meadows
                || sample.biome == BiomeKind.Forest
                || sample.biome == BiomeKind.Mountain && sample.height01 < settings.snowLine - 0.10f;
            return suitableBiome
                && slope <= settings.grassSlopeLimit
                && sample.height01 > settings.seaLevel + 0.015f
                && sample.snowMask < 0.35f;
        }

        public static bool AllowsTree(WorldSample sample, float slope, WorldSettings settings)
        {
            return (sample.biome == BiomeKind.Forest || sample.biome == BiomeKind.Meadows)
                && slope <= settings.treeSlopeLimit
                && sample.height01 > settings.seaLevel + 0.035f
                && sample.forestDensity > 0.38f;
        }

        public static bool AllowsRock(WorldSample sample, float slope)
        {
            return sample.biome != BiomeKind.Coast
                && (sample.rockDensity > 0.47f || slope > 20f || sample.biome == BiomeKind.Mountain);
        }

        public static bool AllowsCliff(WorldSample sample, float slope, WorldSettings settings)
        {
            return sample.biome == BiomeKind.Mountain
                && slope >= settings.cliffSlopeThreshold * 0.82f
                && sample.mountainMask > 0.28f;
        }
    }
}
