using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Grooms the existing ibex ribbons once, on the shared runtime mesh copy.</summary>
    internal static class GoatFurGroom
    {
        private sealed class Strand
        {
            public Vector3 root;
            public readonly List<int> vertices = new List<int>(12);
        }

        public static void Apply(Mesh mesh)
        {
            if (!mesh || !mesh.isReadable || mesh.subMeshCount != 3) return;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uv = mesh.uv;
            if (normals.Length != vertices.Length || uv.Length != vertices.Length) return;
            var fibers = mesh.GetTriangles(2);
            var parent = new int[vertices.Length];
            var used = new bool[vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            for (int i = 0; i < fibers.Length; i += 3)
            {
                int a = fibers[i], b = fibers[i + 1], c = fibers[i + 2];
                parent[Root(parent, b)] = Root(parent, a);
                parent[Root(parent, c)] = Root(parent, a);
                used[a] = used[b] = used[c] = true;
            }
            // FBX duplicates vertices at normal seams between ribbon segments.
            // Join those copies before grooming, so a hair bends as one strand.
            var seams = new Dictionary<Vector3, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!used[i]) continue;
                if (seams.TryGetValue(vertices[i], out int other)) parent[Root(parent, i)] = Root(parent, other);
                else seams.Add(vertices[i], i);
            }
            var strands = new Dictionary<int, Strand>();
            for (int i = 0; i + 5 < fibers.Length; i += 3)
            {
                int root = Root(parent, fibers[i]);
                if (strands.ContainsKey(root)) continue;
                // The exported ribbons begin with a quad: corners 0 and 5
                // are its wide root edge, including duplicated normal seams.
                strands[root] = new Strand { root = (vertices[fibers[i]] + vertices[fibers[i + 5]]) * .5f };
            }
            for (int i = 0; i < vertices.Length; i++)
                if (used[i] && strands.TryGetValue(Root(parent, i), out var strand)) strand.vertices.Add(i);

            // Reuse the coat's UV and rounded surface normal at each hair root.
            // The source fibers have zero UVs and harsh per-ribbon face normals.
            const float cellSize = .0005f; // 10 cm on the imported scale-200 rig.
            var surface = new Dictionary<Vector3Int, List<int>>();
            var visited = new bool[vertices.Length];
            foreach (int i in mesh.GetTriangles(0))
            {
                if (visited[i]) continue;
                visited[i] = true;
                var cell = Cell(vertices[i], cellSize);
                if (!surface.TryGetValue(cell, out var bucket)) surface[cell] = bucket = new List<int>();
                bucket.Add(i);
            }
            var strandData = mesh.uv2;
            if (strandData.Length != vertices.Length) strandData = new Vector2[vertices.Length];
            foreach (var strand in strands.Values)
            {
                if (strand.vertices.Count < 4) continue;
                Vector3 root = strand.root;
                int nearest = Nearest(root, surface, vertices, cellSize);
                // Some beard roots float below the chin; give those the same
                // coat color instead of leaving their old, black zero-UV shading.
                if (nearest < 0) nearest = Nearest(root, surface, vertices, cellSize, 3);
                if (nearest < 0) continue;
                Vector3 tip = root;
                float lengthSquared = 0;
                foreach (int i in strand.vertices)
                {
                    float distance = (vertices[i] - root).sqrMagnitude;
                    if (distance > lengthSquared) { lengthSquared = distance; tip = vertices[i]; }
                }
                float length = Mathf.Sqrt(lengthSquared);
                if (length < 1e-7f) continue;
                Vector3 direction = (tip - root) / length;
                // A short, laid-down coat; retain the longer chin beard silhouette.
                float beard = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.075f, .16f, length * 200f));
                float trim = Mathf.Lerp(.58f, .88f, beard);
                Vector3 normal = normals[nearest].normalized;
                foreach (int i in strand.vertices)
                {
                    Vector3 delta = vertices[i] - root;
                    float along = Mathf.Max(0, Vector3.Dot(delta, direction));
                    Vector3 width = delta - direction * along;
                    vertices[i] = root + direction * (along * trim) + width * 1.12f;
                    normals[i] = Vector3.Lerp(normal, normals[i], .12f).normalized;
                    uv[i] = uv[nearest];
                    strandData[i] = new Vector2(Mathf.Clamp01(along / length), 0);
                }
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.uv2 = strandData;
            mesh.RecalculateBounds();
        }

        private static int Root(int[] parent, int i)
        {
            while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
            return i;
        }
        private static Vector3Int Cell(Vector3 point, float size)
            => new Vector3Int(Mathf.FloorToInt(point.x / size), Mathf.FloorToInt(point.y / size), Mathf.FloorToInt(point.z / size));
        private static int Nearest(Vector3 point, Dictionary<Vector3Int, List<int>> cells, Vector3[] vertices, float size, int radius = 1)
        {
            var center = Cell(point, size);
            float best = float.PositiveInfinity;
            int nearest = -1;
            for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            for (int z = -radius; z <= radius; z++)
            {
                if (!cells.TryGetValue(center + new Vector3Int(x, y, z), out var bucket)) continue;
                foreach (int i in bucket)
                {
                    float distance = (vertices[i] - point).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = i; }
                }
            }
            return nearest;
        }
    }
}
