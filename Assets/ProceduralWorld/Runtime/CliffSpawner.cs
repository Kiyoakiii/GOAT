using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class CliffSpawner : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float density = 0.6f;
        public GameObject[] variants = new GameObject[0];

        public bool IsSuitable(WorldSample sample, float slope, WorldSettings settings)
        {
            return WorldPlacementRules.AllowsCliff(sample, slope, settings)
                && sample.rockDensity >= 1f - density * 0.48f;
        }
    }
}
