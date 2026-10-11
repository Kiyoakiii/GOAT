using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class RuntimeMeshSink : MistyPillarsVistaBuilder.IMeshSink
    {
        private readonly List<Object> assets;

        public RuntimeMeshSink(List<Object> assetTracker) { assets = assetTracker; }

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
            mesh.UploadMeshData(true);
            assets.Add(mesh);
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }
    }
}
