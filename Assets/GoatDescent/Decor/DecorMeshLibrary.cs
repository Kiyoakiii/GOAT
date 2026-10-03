using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class DecorMeshLibrary
    {
        private static Mesh[] _rockMeshes;
        private static Mesh _trunkMesh;
        private static Mesh _pineMesh;
        private static Mesh _octahedronMesh;
        private static Mesh _grassBladeMesh;
        private static Mesh _petalMesh;

        internal static Mesh[] RockMeshes()
        {
            EnsureRockMeshes();
            return _rockMeshes;
        }

        public static Mesh TrunkMesh()
        {
            if (_trunkMesh) return _trunkMesh;
            const int sides = 7;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 bottom0 = new Vector3(Mathf.Cos(a0) * .5f, 0f, Mathf.Sin(a0) * .5f);
                Vector3 bottom1 = new Vector3(Mathf.Cos(a1) * .5f, 0f, Mathf.Sin(a1) * .5f);
                Vector3 top0 = new Vector3(Mathf.Cos(a0) * .32f, 1f, Mathf.Sin(a0) * .32f);
                Vector3 top1 = new Vector3(Mathf.Cos(a1) * .32f, 1f, Mathf.Sin(a1) * .32f);
                AddTriangle(vertices, triangles, bottom0, top0, bottom1);
                AddTriangle(vertices, triangles, bottom1, top0, top1);
                AddTriangle(vertices, triangles, Vector3.zero, bottom0, bottom1);
            }
            _trunkMesh = MakeMesh("Tapered Grounded Stem", vertices, triangles);
            return _trunkMesh;
        }

        public static Mesh PineMesh()
        {
            if (_pineMesh) return _pineMesh;
            const int sides = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 apex = new Vector3(0f, 1f, 0f);
            Vector3 center = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * .5f, 0f, Mathf.Sin(a0) * .5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * .5f, 0f, Mathf.Sin(a1) * .5f);
                AddTriangle(vertices, triangles, p0, apex, p1);
                AddTriangle(vertices, triangles, center, p1, p0);
            }
            _pineMesh = MakeMesh("Low Poly Pine Cone", vertices, triangles);
            return _pineMesh;
        }

        public static Mesh OctahedronMesh()
        {
            if (_octahedronMesh) return _octahedronMesh;
            Vector3 top = new Vector3(0f, .5f, 0f);
            Vector3 bottom = new Vector3(0f, -.5f, 0f);
            Vector3[] ring =
            {
                new Vector3(.5f, 0f, 0f), new Vector3(0f, 0f, .5f),
                new Vector3(-.5f, 0f, 0f), new Vector3(0f, 0f, -.5f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                AddTriangle(vertices, triangles, top, ring[i], ring[next]);
                AddTriangle(vertices, triangles, bottom, ring[next], ring[i]);
            }
            _octahedronMesh = MakeMesh("Low Poly Foliage Cluster", vertices, triangles);
            return _octahedronMesh;
        }

        public static Mesh GrassBladeMesh()
        {
            if (_grassBladeMesh) return _grassBladeMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 leftBase = new Vector3(-.5f, 0f, 0f);
            Vector3 rightBase = new Vector3(.5f, 0f, 0f);
            Vector3 leftMid = new Vector3(-.26f, .56f, .08f);
            Vector3 rightMid = new Vector3(.26f, .56f, .08f);
            Vector3 tip = new Vector3(.10f, 1f, .18f);
            AddDoubleSidedTriangle(vertices, triangles, leftBase, leftMid, rightBase);
            AddDoubleSidedTriangle(vertices, triangles, rightBase, leftMid, rightMid);
            AddDoubleSidedTriangle(vertices, triangles, leftMid, tip, rightMid);
            _grassBladeMesh = MakeMesh("Curved Alpine Grass Blade", vertices, triangles);
            return _grassBladeMesh;
        }

        public static Mesh PetalMesh()
        {
            if (_petalMesh) return _petalMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 root = Vector3.zero;
            Vector3 left = new Vector3(-.5f, .12f, .3f);
            Vector3 tip = new Vector3(0f, .04f, 1f);
            Vector3 right = new Vector3(.5f, .12f, .3f);
            AddDoubleSidedTriangle(vertices, triangles, root, tip, left);
            AddDoubleSidedTriangle(vertices, triangles, root, right, tip);
            _petalMesh = MakeMesh("Faceted Flower Petal", vertices, triangles);
            return _petalMesh;
        }

        private static void EnsureRockMeshes()
        {
            if (_rockMeshes != null) return;
            _rockMeshes = new Mesh[5];
            for (int i = 0; i < _rockMeshes.Length; i++)
                _rockMeshes[i] = CreateRockMesh(i * 7919 + 53);
        }

        private static Mesh CreateRockMesh(int seed)
        {
            var random = new System.Random(seed);
            const int segments = 7;
            float[,] levelHeights = { { 0f, .3f, .72f }, { .36f, .5f, .34f } };
            var rings = new Vector3[3, segments];
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = (i / (float)segments + (float)random.NextDouble() * .035f) * Mathf.PI * 2f;
                    float radius = levelHeights[1, ring] * Mathf.Lerp(.82f, 1.16f, (float)random.NextDouble());
                    rings[ring, i] = new Vector3(Mathf.Cos(angle) * radius, levelHeights[0, ring], Mathf.Sin(angle) * radius);
                }
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    AddTriangle(vertices, triangles, rings[ring, i], rings[ring + 1, i], rings[ring, next]);
                    AddTriangle(vertices, triangles, rings[ring, next], rings[ring + 1, i], rings[ring + 1, next]);
                }
            }

            Vector3 peak = new Vector3(Range(random, -.08f, .08f), 1f, Range(random, -.08f, .08f));
            for (int i = 0; i < segments; i++)
            {
                AddTriangle(vertices, triangles, Vector3.zero, rings[0, i], rings[0, (i + 1) % segments]);
                AddTriangle(vertices, triangles, rings[2, i], peak, rings[2, (i + 1) % segments]);
            }

            var mesh = new Mesh { name = "Faceted Grounded Boulder" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MakeMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDoubleSidedTriangle(List<Vector3> vertices, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, c, b, a);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}