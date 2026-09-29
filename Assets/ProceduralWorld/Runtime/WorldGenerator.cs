using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private WorldSettings settings;
        [SerializeField] private Transform generatedRoot;

        public WorldSettings Settings => settings;

        public void Configure(WorldSettings worldSettings, Transform targetGeneratedRoot)
        {
            settings = worldSettings;
            generatedRoot = targetGeneratedRoot;
        }

        public TerrainBuildResult GenerateHeightField()
        {
            if (settings == null)
            {
                Debug.LogError("PROCEDURAL_WORLD_GENERATOR failed: WorldSettings is missing.", this);
                return null;
            }
            return TerrainGenerator.Build(settings);
        }

        public WorldSample Evaluate(float worldX, float worldZ)
        {
            return BiomeGenerator.Sample(worldX, worldZ, settings);
        }

    }
}
