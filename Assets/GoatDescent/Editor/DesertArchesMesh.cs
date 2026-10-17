using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class DesertArchesMesh
    {
        private static readonly Color Rock = new Color(0f, 0f, 0f, 1f);

        public static Mesh Mesa(float width, float depth, float top, float bottom, int detail)
        {
            var mesh = new CrownRockMesh();
            const int sides = 28, rings = 24;
            var points = new Vector3[rings, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float shape = 1f + Mathf.Sin(side * .79f + detail) * .09f + Mathf.Sin(side * 2.31f + detail) * .025f;
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring < 3 ? ring * .007f : Mathf.Lerp(.035f, 1f, (ring - 3f) / (rings - 4f));
                    float y = Mathf.Lerp(top, bottom, t);
                    float fracture = ring == 0 ? Mathf.Sin(side * 2.7f + detail) * .8f : Mathf.Sin(side * 3.13f + ring * 1.72f + detail) * .8f;
                    float taper = 1f + t * .28f + (ring == 1 ? .012f : ring == 2 ? -.022f : Mathf.Sin(ring * .8f + detail) * .007f) + Mathf.Sin(ring * 2f + side * .7f) * .014f;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * width * shape * taper + t * 5f + fracture,
                        y + (ring == 0 ? Mathf.Sin(side * 2f + detail) * .9f : Mathf.Sin(side * .63f + ring * .44f + detail) * 3.2f),
                        Mathf.Sin(angle) * depth * shape * taper + t * 3f + fracture);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, new Vector3(0f, top, 0f), points[0, next], points[0, side]);
                for (int ring = 0; ring < rings - 1; ring++)
                    Quad(mesh, points[ring, side], points[ring, next], points[ring + 1, next], points[ring + 1, side]);
                Tri(mesh, new Vector3(0f, bottom, 0f), points[rings - 1, side], points[rings - 1, next]);
            }
            return Soften(mesh.Build("Скальный массив " + detail));
        }

        public static Mesh Fragment()
        {
            var mesh = new CrownRockMesh();
            const int sides = 7;
            var points = new Vector3[3, sides];
            for (int ring = 0; ring < 3; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    float radius = (ring == 1 ? 1f : .75f) * (1f + Mathf.Sin(side * 2.1f) * .13f);
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius,
                        .15f - ring * .4f + Mathf.Sin(side * 1.7f + ring) * .08f, Mathf.Sin(angle) * radius * .8f);
                }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, Vector3.up * .15f, points[0, next], points[0, side]);
                for (int ring = 0; ring < 2; ring++)
                    Quad(mesh, points[ring, side], points[ring, next], points[ring + 1, next], points[ring + 1, side]);
                Tri(mesh, Vector3.down * .65f, points[2, side], points[2, next]);
            }
            return mesh.Build("Обломок песчаника");
        }

        public static Mesh Arch(float scale)
        {
            var mesh = new CrownRockMesh();
            const int steps = 24;
            var inner = new Vector3[2, steps + 1];
            var outer = new Vector3[2, steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI / steps;
                float crack = Mathf.Sin(i * 2.7f) * 1.5f;
                for (int face = 0; face < 2; face++)
                {
                    float z = face == 0 ? -20f : 23f;
                    float faceCrack = Mathf.Sin(i * 2.37f + face) * .7f;
                    inner[face, i] = new Vector3(Mathf.Cos(angle) * (57f + crack),
                        56f + Mathf.Sin(angle) * 86f, z + Mathf.Sin(i * 1.3f) * 2f + faceCrack) * scale;
                    outer[face, i] = new Vector3(Mathf.Cos(angle) * (88f + crack),
                        63f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(angle)), .48f) * 137f + Mathf.Sin(i * .8f) * 4f, z + Mathf.Sin(i * 1.9f) * 3f + faceCrack) * scale;
                }
            }
            for (int i = 0; i < steps; i++)
            {
                for (int layer = 0; layer < 4; layer++)
                {
                    Vector3 Point(int face, int index, int ring)
                    {
                        float t = ring / 4f;
                        Vector3 point = Vector3.Lerp(inner[face, index], outer[face, index], t);
                        point.z += Mathf.Sin(index * 1.91f + ring * 3.1f) * Mathf.Sin(t * Mathf.PI) * .55f * scale;
                        return point;
                    }
                    ArchQuad(mesh, Point(0, i, layer + 1), Point(0, i + 1, layer + 1), Point(0, i + 1, layer), Point(0, i, layer));
                    ArchQuad(mesh, Point(1, i + 1, layer + 1), Point(1, i, layer + 1), Point(1, i, layer), Point(1, i + 1, layer));
                }
                ArchQuad(mesh, outer[1, i], outer[1, i + 1], outer[0, i + 1], outer[0, i]);
                ArchQuad(mesh, inner[0, i], inner[0, i + 1], inner[1, i + 1], inner[1, i]);
            }
            for (int end = 0; end < 2; end++)
            {
                int i = end == 0 ? 0 : steps;
                Vector3 of = outer[0, i], ob = outer[1, i], inf = inner[0, i], inb = inner[1, i];
                Vector3 ofb = new Vector3(of.x * 1.08f, -105f * scale, of.z);
                Vector3 obb = new Vector3(ob.x * 1.08f, -105f * scale, ob.z);
                Vector3 ifb = new Vector3(inf.x * .96f, -105f * scale, inf.z);
                Vector3 ibb = new Vector3(inb.x * .96f, -105f * scale, inb.z);
                if (end == 0)
                {
                    ArchQuad(mesh, ofb, of, inf, ifb); ArchQuad(mesh, ibb, inb, ob, obb);
                    ArchQuad(mesh, obb, ob, of, ofb); ArchQuad(mesh, ifb, inf, inb, ibb);
                }
                else
                {
                    ArchQuad(mesh, ifb, inf, of, ofb); ArchQuad(mesh, obb, ob, inb, ibb);
                    ArchQuad(mesh, ofb, of, ob, obb); ArchQuad(mesh, ibb, inb, inf, ifb);
                }
            }
            return Soften(mesh.Build("Природная арка"));
        }

        public static Mesh Ground()
        {
            var mesh = new CrownRockMesh();
            const int grid = 70;
            Vector3 Point(int x, int z)
            {
                float px = Mathf.Lerp(-700f, 700f, x / (float)grid);
                float pz = Mathf.Lerp(-700f, 700f, z / (float)grid);
                float distance = new Vector2(px, pz).magnitude;
                float hills = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(230f, 650f, distance));
                float channel = 85f + Mathf.Sin(pz * .006f) * 35f;
                float bank = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(38f, 155f, Mathf.Abs(px - channel)));
                float y = Mathf.Lerp(-92f, -22f, bank) + Mathf.PerlinNoise(px * .006f + 19f, pz * .006f + 7f) * hills * 95f;
                return new Vector3(px, y, pz);
            }
            for (int z = 0; z < grid; z++)
                for (int x = 0; x < grid; x++)
                {
                    Tri(mesh, Point(x, z), Point(x, z + 1), Point(x + 1, z));
                    Tri(mesh, Point(x + 1, z), Point(x, z + 1), Point(x + 1, z + 1));
                }
            return mesh.Build("Дно и дальние склоны каньона");
        }

        private static Mesh Soften(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var groups = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!groups.TryGetValue(vertices[i], out var indices))
                { indices = new List<int>(); groups.Add(vertices[i], indices); }
                indices.Add(i);
            }
            var result = new Vector3[normals.Length];
            foreach (var indices in groups.Values)
                foreach (int i in indices)
                {
                    Vector3 average = Vector3.zero;
                    foreach (int j in indices)
                        if (Vector3.Dot(normals[i], normals[j]) > .91f) average += normals[j];
                    result[i] = Vector3.Lerp(normals[i], average.normalized, .78f).normalized;
                }
            mesh.normals = result;
            return mesh;
        }

        private static void ArchQuad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            => Quad(mesh, d, c, b, a);

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
            => mesh.Triangle(a, b, c, Rock, Rock, Rock);
        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Tri(mesh, a, b, c); Tri(mesh, a, c, d); }
    }
}
