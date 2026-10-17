using UnityEngine;

namespace GoatDescent.Levels
{
    [CreateAssetMenu(menuName = "GOAT/Biome Profile", fileName = "MountainForest")]
    public sealed class BiomeProfile : ScriptableObject
    {
        [SerializeField] private string biomeName = "Горный хвойный лес";
        [SerializeField] private Color rockColor = new Color(.64f, .53f, .37f);
        [SerializeField] private Color foliageColor = new Color(.22f, .38f, .14f);
        [SerializeField] private Color fogColor = new Color(.72f, .82f, .88f);
        [SerializeField, Range(0f, .01f)] private float fogDensity = .0012f;
        [SerializeField, Range(0f, 2f)] private float treeDensity = .65f;
        public string BiomeName => biomeName;
        public Color RockColor => rockColor;
        public Color FoliageColor => foliageColor;
        public Color FogColor => fogColor;
        public float FogDensity => fogDensity;
        public float TreeDensity => treeDensity;
    }
}
