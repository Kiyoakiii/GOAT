using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class GoatTrailBuilder
    {
        public static Transform Build(Transform parent, string name, Func<Vector3, float> terrain, Material material,
            IList<Vector3> waypoints, float width = 4f, float lift = 0.35f, float stoneEvery = 9f,
            bool withRibbon = true, Color? stoneColor = null)
        {
            if (waypoints == null || waypoints.Count < 2) throw new ArgumentException("GoatTrailBuilder: need >= 2 waypoints");
            width = Mathf.Max(width, 8f);
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var samples = Resample(waypoints, 2.5f);
            if (withRibbon) Ribbon(root.transform, name, terrain, material, samples, width, lift);
            if (stoneEvery > 0f) Stones(root.transform, terrain, samples, width, stoneEvery, stoneColor ?? material.color * .8f);
            return root.transform;
        }

        private static List<Vector3> Resample(IList<Vector3> waypoints, float step)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 a = waypoints[i], b = waypoints[i + 1];
                float length = Vector3.Distance(a, b);
                int count = Mathf.Max(1, Mathf.RoundToInt(length / step));
                for (int k = 0; k < count; k++) points.Add(Vector3.Lerp(a, b, (float)k / count));
            }
            points.Add(waypoints[waypoints.Count - 1]);
            return points;
        }

        private static void Ribbon(Transform root, string name, Func<Vector3, float> terrain, Material material,
            List<Vector3> samples, float width, float lift)
        {
            int count = samples.Count;
            var vertices = new List<Vector3>(count * 2);
            var uvs = new List<Vector2>(count * 2);
            var tris = new List<int>((count - 1) * 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = samples[i];
                p.y = terrain(p) + lift;
                Vector3 a = samples[Mathf.Max(i - 1, 0)], b = samples[Mathf.Min(i + 1, count - 1)];
                Vector3 forward = (b - a).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized * (width * .5f);
                vertices.Add(p - side);
                vertices.Add(p + side);
                uvs.Add(new Vector2(0f, i * .35f));
                uvs.Add(new Vector2(1f, i * .35f));
            }
            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2, b = a + 1, c = a + 3, d = a + 2;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
            var mesh = new Mesh { name = name + "TrailMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var ribbon = new GameObject("Полоса тропы");
            ribbon.transform.SetParent(root, false);
            ribbon.AddComponent<MeshFilter>().sharedMesh = mesh;
            ribbon.AddComponent<MeshRenderer>().sharedMaterial = material;
            ribbon.gameObject.isStatic = true;
        }

        private static void Stones(Transform root, Func<Vector3, float> terrain, List<Vector3> samples,
            float width, float stoneEvery, Color color)
        {
            var cube = CubeMesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float accumulator = stoneEvery * .5f;
            var random = new System.Random(1234);
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 a = samples[i], b = samples[i + 1];
                float segment = Vector3.Distance(a, b);
                Vector3 forward = (b - a).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                accumulator += segment;
                while (accumulator >= stoneEvery)
                {
                    accumulator -= stoneEvery;
                    float t = 1f - accumulator / Mathf.Max(segment, .001f);
                    Vector3 basePoint = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                    float sideOffset = ((random.NextDouble() > .5 ? 1 : -1) * (width * .5f + 1.2f));
                    Vector3 position = basePoint + side * (float)sideOffset;
                    position.y = terrain(position) + .25f;
                    float scale = .8f + (float)random.NextDouble() * 1f;
                    var matrix = Matrix4x4.TRS(position,
                        Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f),
                        new Vector3(scale, scale * .55f, scale * .8f));
                    int baseIndex = vertices.Count;
                    foreach (var v in cube.vertices) vertices.Add(matrix.MultiplyPoint3x4(v));
                    foreach (int index in cube.triangles) triangles.Add(baseIndex + index);
                }
            }
            var mesh = new Mesh { name = "TrailStonesMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var stones = new GameObject("Камни по краю тропы");
            stones.transform.SetParent(root, false);
            stones.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Standard")) { color = color };
            stones.AddComponent<MeshRenderer>().sharedMaterial = material;
            stones.gameObject.isStatic = true;
        }

        private static Mesh CubeMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(go);
            return mesh;
        }
    }
}
