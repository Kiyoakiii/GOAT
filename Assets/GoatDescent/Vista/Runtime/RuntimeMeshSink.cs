using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class RuntimeMeshSink : MistyPillarsVistaBuilder.IMeshSink
    {
        private readonly List<Object> assets;
        private readonly bool retainSpawnMeshData;

        public RuntimeMeshSink(List<Object> assetTracker,bool retainSpawnGeometry=false)
        { assets=assetTracker;retainSpawnMeshData=retainSpawnGeometry; }

        public GameObject Emit(Transform parent, string objectName, string assetName, PillarMeshBuilder builder, Material material, bool flatten)
        {
            var mesh = new Mesh { name = assetName };
            if (flatten) builder.FlattenFaces();
            mesh.indexFormat = builder.vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(builder.vertices);
            mesh.SetColors(builder.colors);
            mesh.SetTriangles(builder.triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bool retain=retainSpawnMeshData && parent.name=="Forest pillar "+MistyPillarsVistaBuilder.SpawnTowerSeed;
            // The builder attaches the rock collider after Emit returns. A player
            // cannot cook its collision mesh after CPU geometry has been discarded.
            bool pendingCollision = objectName == MistyPillarsVistaBuilder.RockObjectName;
            mesh.UploadMeshData(!retain && !pendingCollision);
            assets.Add(mesh);
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }

        internal void CompleteCollisionMesh(GameObject obj)
        {
            bool retain = retainSpawnMeshData && obj.transform.parent.name == "Forest pillar " + MistyPillarsVistaBuilder.SpawnTowerSeed;
            if (!retain) obj.GetComponent<MeshFilter>().sharedMesh.UploadMeshData(true);
        }
    }
}
