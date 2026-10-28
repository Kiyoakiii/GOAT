using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class GoatHerdAssets
    {
        [MenuItem("Goat Descent/Assets/Create herd settings and coat mask")]
        public static void CreateAssets()
        {
            const string settingsPath = "Assets/GoatDescent/Resources/GoatHerdSettings.asset";
            if (!AssetDatabase.LoadAssetAtPath<GoatHerdSettings>(settingsPath))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GoatHerdSettings>(), settingsPath);
            BakeCoatMask();
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<GoatHerdSettings>(settingsPath));
        }

        private static void BakeCoatMask()
        {
            var definition = Resources.Load<GoatSkinDefinition>("GoatSkins/Skin11");
            if (!definition || !definition.mesh || !definition.mesh.isReadable) return;
            var mesh = definition.mesh;
            var vertices = mesh.vertices; var uv = mesh.uv; var triangles = mesh.GetTriangles(0);
            if (uv.Length != vertices.Length) return;
            var parent = new int[vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            void Union(int a, int b) { parent[Root(a)] = Root(b); }
            float quantum = Mathf.Max(.00000001f, mesh.bounds.size.magnitude * .000001f);
            var welded = new Dictionary<Vector3Int, int>();
            foreach (int vertex in triangles)
            {
                var p = vertices[vertex] / quantum;
                var key = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                if (welded.TryGetValue(key, out int other)) Union(vertex, other); else welded.Add(key, vertex);
            }
            for (int i = 0; i < triangles.Length; i += 3)
            { Union(triangles[i], triangles[i + 1]); Union(triangles[i], triangles[i + 2]); }
            var bounds = new Dictionary<int, Bounds>();
            foreach (int index in triangles)
            {
                int root = Root(index);
                if (!bounds.TryGetValue(root, out var value)) value = new Bounds(vertices[index], Vector3.zero);
                value.Encapsulate(vertices[index]); bounds[root] = value;
            }
            const int size = 1024;
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            float threshold = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) * .075f;
            int preservedTriangles = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var group = bounds[Root(triangles[i])].size;
                if (Mathf.Max(group.x, group.y, group.z) >= threshold) continue;
                Vector2 a = uv[triangles[i]] * (size - 1), b = uv[triangles[i + 1]] * (size - 1), c = uv[triangles[i + 2]] * (size - 1);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)) - 1, 0, size - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)) + 1, 0, size - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)) - 1, 0, size - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)) + 1, 0, size - 1);
                float Cross(Vector2 p, Vector2 q) => p.x * q.y - p.y * q.x;
                float area = Cross(b - a, c - a); if (Mathf.Abs(area) < .00001f) continue;
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f);
                    float v = Cross(p - a, c - a) / area, w = Cross(b - a, p - a) / area;
                    if (v >= -.08f && w >= -.08f && v + w <= 1.08f) pixels[y * size + x] = new Color32(0, 0, 0, 255);
                }
                preservedTriangles++;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels); texture.Apply();
            const string directory = "Assets/GoatDescent/Resources/GoatCoatMasks";
            Directory.CreateDirectory(directory);
            string path = directory + "/IbexCoatMask.png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                importer.sRGBTexture = false; importer.mipmapEnabled = true; importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Debug.Log($"GOAT_COAT_MASK components={bounds.Count} preservedTriangles={preservedTriangles}");
        }
    }
}
