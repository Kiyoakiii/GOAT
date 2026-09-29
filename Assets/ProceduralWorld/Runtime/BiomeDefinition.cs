using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public enum BiomeKind
    {
        Coast,
        Meadows,
        Forest,
        Mountain
    }

    [CreateAssetMenu(menuName = "Goat Descent/Procedural World/Biome Definition", fileName = "BiomeDefinition")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        public BiomeKind biome;
        public Color previewColor = Color.green;
        [Range(0f, 1f)] public float vegetationMultiplier = 1f;
        [Range(0f, 1f)] public float rockMultiplier = 1f;
        [Range(0f, 1f)] public float cliffMultiplier = 1f;
    }
}
