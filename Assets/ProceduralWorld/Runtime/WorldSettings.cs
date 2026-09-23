using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    [CreateAssetMenu(menuName = "Goat Descent/Procedural World/World Settings", fileName = "WorldSettings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("Deterministic world")]
        public int seed = 847291;
        [Min(512f)] public float worldSize = 2048f;
        [Min(64f)] public float heightScale = 360f;
        [Range(0.02f, 0.7f)] public float seaLevel = 0.22f;
        [Range(129, 1025)] public int terrainResolution = 513;
        [Min(128f)] public float chunkSize = 512f;

        [Header("Large forms")]
        [Range(0f, 1f)] public float mountainStrength = 0.80f;
        [Min(0.0001f)] public float mountainFrequency = 0.0017f;
        [Min(0.0001f)] public float biomeScale = 0.00105f;
        [Range(0f, 1f)] public float erosionStrength = 0.22f;

        [Header("Surface")]
        [Range(0.55f, 0.95f)] public float snowLine = 0.80f;
        [Range(0f, 1f)] public float snowTemperatureThreshold = 0.36f;
        [Range(0f, 1f)] public float snowCoverage = 0.86f;
        [Range(0f, 2f)] public float grassDensity = 0.9f;
        [Range(5f, 55f)] public float grassSlopeLimit = 30f;

        [Header("Placement")]
        [Range(0.1f, 2f)] public float vegetationDensity = 0.72f;
        [Range(0.1f, 2f)] public float rockDensity = 0.68f;
        [Range(0.1f, 2f)] public float cliffDensity = 0.58f;
        [Range(15f, 65f)] public float treeSlopeLimit = 34f;
        [Range(20f, 75f)] public float cliffSlopeThreshold = 43f;
    }
}
