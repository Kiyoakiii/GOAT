using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    [CreateAssetMenu(menuName = "Goat Descent/Procedural World/Vegetation Generation Settings", fileName = "VegetationGenerationSettings")]
    public sealed class VegetationGenerationSettings : ScriptableObject
    {
        [Header("Trees")]
        [Tooltip("Chance to keep a regular tree candidate after biome and slope checks. 0 disables regular trees; 1 restores their former density.")]
        [Range(0f, 1f)] public float treeDensity = 0.25f;

        [Tooltip("Maximum number of conifers around the goat spawn. The summit clearing remains free of trees.")]
        [Range(0, 200)] public int summitTreeCount = 30;

        [Header("Ground cover")]
        [Tooltip("Multiplier for grass, flowers and low bushes. The former density was 0.9.")]
        [Range(0f, 2f)] public float grassDensity = 0.25f;
    }
}
