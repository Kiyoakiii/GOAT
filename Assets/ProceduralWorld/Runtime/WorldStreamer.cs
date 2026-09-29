using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class WorldStreamer : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float activeRadius = 900f;
        [SerializeField] private WorldChunk[] chunks = new WorldChunk[0];
        private Vector2Int lastChunk = new Vector2Int(int.MinValue, int.MinValue);

        public void Configure(Transform targetTransform, float radius, WorldChunk[] worldChunks)
        {
            target = targetTransform;
            activeRadius = radius;
            chunks = worldChunks;
            lastChunk = new Vector2Int(int.MinValue, int.MinValue);
            RefreshChunks();
        }

        private void Update()
        {
            if (target == null || chunks == null || chunks.Length == 0)
                return;
            Vector2Int currentChunk = new Vector2Int(
                Mathf.FloorToInt(target.position.x / Mathf.Max(1f, chunks[0].Bounds.size.x)),
                Mathf.FloorToInt(target.position.z / Mathf.Max(1f, chunks[0].Bounds.size.z)));
            if (currentChunk == lastChunk)
                return;
            lastChunk = currentChunk;
            RefreshChunks();
        }

        private void RefreshChunks()
        {
            if (target == null || chunks == null)
                return;
            float radiusSquared = activeRadius * activeRadius;
            foreach (WorldChunk chunk in chunks)
            {
                if (chunk == null)
                    continue;
                Vector3 closest = chunk.Bounds.ClosestPoint(target.position);
                chunk.gameObject.SetActive((closest - target.position).sqrMagnitude <= radiusSquared);
            }
        }
    }
}
