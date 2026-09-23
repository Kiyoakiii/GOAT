using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class VegetationSpawner : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float density = 0.7f;
        public GameObject[] treeVariants = new GameObject[0];

        public bool IsSuitable(WorldSample sample, float slope, WorldSettings settings)
        {
            return WorldPlacementRules.AllowsTree(sample, slope, settings)
                && sample.forestDensity >= 1f - density * 0.5f;
        }
    }
}
