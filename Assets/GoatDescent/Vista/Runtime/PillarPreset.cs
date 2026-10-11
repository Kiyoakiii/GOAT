using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    [CreateAssetMenu(fileName = "PillarPreset", menuName = "Procedural World/Pillar Preset")]
    public sealed class PillarPreset : ScriptableObject
    {
        [Header("Pillar")]
        public float height = 335f;
        public float rx = 62f;
        public float rz = 47f;
        public int seed = 33;
        [Range(0, 3)] public int detail = 0;
        [Range(0, 2)] public float terraceBoost = 1f;

        [Header("Platform")]
        public float platformRadius = 14f;
        public float platformThickness = 14f;

        [Header("Play Mode")]
        public bool generateOnStart = false;
        public bool spawnGoat = true;
    }
}
