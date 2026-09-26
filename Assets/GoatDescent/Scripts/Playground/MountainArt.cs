using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    // Shared palette and shaped geometry for the mountain set, owned by its scene root.
    public sealed class MountainArt
    {
        private readonly MountainGeneratedAssets owner;
        public readonly Material Snow, Stone, Ice, Timber, EndGrain, Metal, Gold, Coral, Rope;
        public MountainArt(GameObject root)
        {
            owner = root.AddComponent<MountainGeneratedAssets>();
            Snow = Mat(new Color(.72f, .80f, .83f));
            Stone = Mat(new Color(.25f, .32f, .40f));
            Ice = Mat(new Color(.29f, .57f, .66f), .28f);
            Timber = Mat(new Color(.29f, .16f, .10f));
            EndGrain = Mat(new Color(.64f, .43f, .26f));
            Metal = Mat(new Color(.16f, .22f, .27f), .35f);
            Gold = Mat(new Color(.94f, .65f, .22f));
            Coral = Mat(new Color(.79f, .27f, .22f));
            Rope = Mat(new Color(.67f, .59f, .43f));
        }
        public T Own<T>(T asset) where T : Object { owner.Assets.Add(asset); return asset; }
        public Material Mat(Color color, float gloss = .08f)
        {
            var m = Own(new Material(Shader.Find("Standard")) { color = color });
            m.SetFloat("_Glossiness", gloss);
            return m;
        }
        public GameObject MeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Material material, bool collision = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
        public GameObject Rock(Transform parent, Vector3 position, Vector3 size, int seed, Material material, bool collision = true)
        {
            var verts = new List<Vector3>(); var tris = new List<int>();
            var rings = new Vector3[4, 8];
            float[] heights = { -.5f, -.34f, .32f, .5f };
            float[] radii = { .66f, 1f, .85f, .58f };
            for (int r = 0; r < 4; r++)
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * .25f;
                    float jitter = 1f + Mathf.Sin(seed * 1.71f + i * 4.6f + r*.65f) * .18f;
                    rings[r, i] = Vector3.Scale(new Vector3(Mathf.Cos(a) * radii[r] * .5f * jitter + Mathf.Sin(seed+r)*.035f,
                        heights[r], Mathf.Sin(a) * radii[r] * .5f * jitter + Mathf.Cos(seed+r)*.035f), size);
                }
            for (int r = 0; r < 3; r++) for (int i = 0; i < 8; i++)
                Quad(verts, tris, rings[r, i], rings[r + 1, i], rings[r + 1, (i + 1) % 8], rings[r, (i + 1) % 8]);
            for (int i = 0; i < 8; i++)
            {
                Triangle(verts, tris, Vector3.up * size.y * .5f, rings[3, (i + 1) % 8], rings[3, i]);
                Triangle(verts, tris, Vector3.down * size.y * .5f, rings[0, i], rings[0, (i + 1) % 8]);
            }
            return MeshPart(parent, "Weathered rock", MakeMesh("Weathered bevel rock", verts, tris), position, material, collision);
        }
        public GameObject Beam(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = (a + b) * .5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            go.transform.localScale = new Vector3(radius * 2f, Vector3.Distance(a, b) * .5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) { go.GetComponent<Collider>().enabled = false; Object.Destroy(go.GetComponent<Collider>()); }
            return go;
        }
        public GameObject Ring(Transform parent, string name, Vector3 center, float radius, float thickness, Material material)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int segments = 40, tube = 6;
            for (int i = 0; i <= segments; i++) for (int j = 0; j <= tube; j++)
            {
                float a = i * Mathf.PI * 2f / segments, b = j * Mathf.PI * 2f / tube;
                float r = radius + Mathf.Cos(b) * thickness;
                v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Sin(b) * thickness));
            }
            for (int i = 0; i < segments; i++) for (int j = 0; j < tube; j++)
            {
                int n = i * (tube + 1) + j;
                t.AddRange(new[] { n, n + tube + 1, n + 1, n + 1, n + tube + 1, n + tube + 2 });
            }
            return MeshPart(parent, name, MakeMesh(name, v, t), center, material);
        }
        public void Flag(Transform parent, Vector3 ground, Material cloth)
        {
            Beam(parent, "Trail marker pole", ground, ground + Vector3.up * 2.4f, .055f, Timber, false);
            var v = new List<Vector3> { Vector3.zero, new Vector3(1.05f, -.12f, 0f), new Vector3(.81f, -.36f, 0f),
                new Vector3(1.06f, -.59f, 0f), new Vector3(0f, -.53f, 0f) };
            var mesh = MakeMesh("Swallowtail cloth", v, new List<int> { 0,1,2, 0,2,4, 2,3,4, 2,1,0, 4,2,0, 4,3,2 });
            MeshPart(parent, "Wind-blown trail pennant", mesh, ground + Vector3.up * 2.3f, cloth).AddComponent<MountainFlag>();
        }
        public Mesh MakeMesh(string name, List<Vector3> vertices, List<int> indices)
        {
            var mesh = Own(new Mesh { name = name });
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        public static void Triangle(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        { int n = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(n); t.Add(n + 1); t.Add(n + 2); }
        public static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Triangle(v,t,a,b,c); Triangle(v,t,a,c,d); }
    }
    public sealed class MountainGeneratedAssets : MonoBehaviour
    {
        public readonly List<Object> Assets = new List<Object>();
        private void OnDestroy() { foreach (var asset in Assets) if (asset) Destroy(asset); }
    }
    public sealed class MountainFlag : MonoBehaviour
    {
        private Mesh mesh; private Vector3[] original, working;
        private void Start() { mesh = GetComponent<MeshFilter>().sharedMesh; original = mesh.vertices; working = mesh.vertices; }
        private void Update()
        {
            for (int i = 0; i < working.Length; i++)
            { working[i] = original[i]; working[i].z += Mathf.Sin(Time.time * 3.5f - original[i].x * 4f + transform.position.z) * .12f * original[i].x; }
            mesh.vertices = working; mesh.RecalculateNormals();
        }
    }
}
