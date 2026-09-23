using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class RockSpawner : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float density = 0.7f;
        public GameObject[] variants = new GameObject[0];

        public bool IsSuitable(WorldSample sample, float slope, WorldSettings settings)
        {
            return WorldPlacementRules.AllowsRock(sample, slope) && sample.rockDensity >= 1f - density * 0.55f;
        }
    }
}
