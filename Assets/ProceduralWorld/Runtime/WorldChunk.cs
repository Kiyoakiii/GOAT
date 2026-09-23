using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class WorldChunk : MonoBehaviour
    {
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Bounds bounds;

        public Vector2Int Coordinate => coordinate;
        public Bounds Bounds => bounds;

        public void Configure(Vector2Int chunkCoordinate, Bounds chunkBounds)
        {
            coordinate = chunkCoordinate;
            bounds = chunkBounds;
        }
    }
}
