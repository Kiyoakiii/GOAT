using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    internal sealed class FrozenRiftBatch
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly Dictionary<Vector3, int> welded = new Dictionary<Vector3, int>();
        private readonly bool weld;
        private readonly bool trackUv;

        public FrozenRiftBatch(bool weld = false, bool trackUv = false)
        {
            this.weld = weld; this.trackUv = trackUv;
        }

        public int TriangleCount => triangles.Count / 3;

        public void Add(Mesh mesh, Matrix4x4 transform)
        {
            var points = mesh.vertices; var indices = mesh.triangles; var map = new int[points.Length];
            for (int i = 0; i < points.Length; i++) map[i] = Vertex(transform.MultiplyPoint3x4(points[i]), Vector2.zero);
            for (int i = 0; i < indices.Length; i++) triangles.Add(map[indices[i]]);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            triangles.Add(Vertex(a, Vector2.zero));
            triangles.Add(Vertex(b, Vector2.zero));
            triangles.Add(Vertex(c, Vector2.zero));
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(a, b, c); Triangle(a, c, d);
        }

        public void TexturedQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            triangles.Add(Vertex(a, ua)); triangles.Add(Vertex(b, ub)); triangles.Add(Vertex(c, uc));
            triangles.Add(Vertex(a, ua)); triangles.Add(Vertex(c, uc)); triangles.Add(Vertex(d, ud));
        }

        private int Vertex(Vector3 position, Vector2 texCoord)
        {
            if (weld && welded.TryGetValue(position, out int existing)) return existing;
            vertices.Add(position); if (trackUv) uv.Add(texCoord);
            int index = vertices.Count - 1;
            if (weld) welded.Add(position, index);
            return index;
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            if (trackUv) mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }

    internal static class FrozenRiftMesh
    {
        private static readonly Color Rock = new Color(0f, 0f, 0f, 1f);

        public static Mesh Crystal(int seed)
        {
            var random = new System.Random(seed * 7919 + 41);
            var mesh = new CrownRockMesh();
            int sides = 6 + seed % 3;
            float thin = Range(random, .26f, .4f);
            var radius = new[] { 1f, Range(random, .8f, .93f), Range(random, .56f, .72f), Range(random, .3f, .44f) };
            var level = new[] { 0f, Range(random, .3f, .42f), Range(random, .58f, .72f), Range(random, .8f, .92f) };
            var rings = new Vector3[4, sides];
            for (int ring = 0; ring < 4; ring++)
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2f / sides + ring * .19f;
                    float r = radius[ring] * (1f + Mathf.Sin(i * 2.7f + seed * .7f + ring) * .1f);
                    rings[ring, i] = new Vector3(Mathf.Cos(angle) * r,
                        level[ring] + (ring == 0 ? 0f : Mathf.Sin(i * 1.9f + seed + ring) * .022f),
                        Mathf.Sin(angle) * r * thin);
                }
            var apex = new Vector3(Range(random, -.15f, .15f), 1f, Range(random, -.2f, .2f) * thin);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                for (int ring = 0; ring < 3; ring++)
                    Quad(mesh, rings[ring, i], rings[ring + 1, i], rings[ring + 1, next], rings[ring, next]);
                Tri(mesh, rings[3, i], apex, rings[3, next]);
                Tri(mesh, Vector3.zero, rings[0, i], rings[0, next]);
            }
            return mesh.Build("Гранёный ледяной монолит");
        }

        public static Mesh IceBlock(int seed)
        {
            var random = new System.Random(seed * 3911 + 17);
            var mesh = new CrownRockMesh();
            int sides = 6 + seed % 2;
            const int rings = 4;
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2f / sides + ring * .22f;
                    float radius = Range(random, .6f, 1f) * (ring == 0 ? .84f : ring == rings - 1 ? .32f : 1f);
                    points[ring, i] = new Vector3(Mathf.Cos(angle) * radius,
                        ring * .33f + Range(random, -.05f, .05f), Mathf.Sin(angle) * radius * .8f);
                }
            var apex = new Vector3(Range(random, -.12f, .12f), 1.24f, Range(random, -.12f, .12f));
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Tri(mesh, Vector3.down * .16f, points[0, i], points[0, next]);
                for (int ring = 0; ring < rings - 1; ring++)
                    Quad(mesh, points[ring, i], points[ring + 1, i], points[ring + 1, next], points[ring, next]);
                Tri(mesh, points[rings - 1, i], apex, points[rings - 1, next]);
            }
            return mesh.Build("Ледяная глыба");
        }

        public static Mesh CrystalCluster(int seed)
        {
            var random = new System.Random(seed * 1223 + 91);
            var batch = new FrozenRiftBatch();
            int spikes = 4 + seed % 3;
            for (int i = 0; i < spikes; i++)
            {
                var shard = Crystal(seed * 13 + i * 7 + 3);
                float angle = i * Mathf.PI * 2f / spikes + Range(random, -.32f, .32f);
                float offset = Range(random, .1f, .52f);
                float scale = Range(random, .42f, 1f);
                var matrix = Matrix4x4.TRS(
                    new Vector3(Mathf.Cos(angle) * offset, Range(random, -.06f, .06f), Mathf.Sin(angle) * offset),
                    Quaternion.Euler(Range(random, -27f, 27f), Range(random, 0f, 360f), Range(random, -27f, 27f)),
                    new Vector3(scale * Range(random, .6f, 1.1f), scale * Range(random, 1f, 2.1f), scale * Range(random, .6f, 1.1f)));
                batch.Add(shard, matrix);
                Object.DestroyImmediate(shard);
            }
            return batch.Build("Ледяная друза");
        }

        public static Mesh Boulder(int seed)
        {
            var random = new System.Random(seed * 5443 + 29);
            var mesh = new CrownRockMesh();
            const int sides = 7;
            const int rings = 3;
            var points = new Vector3[rings, sides];
            var height = new[] { 0f, Range(random, .18f, .32f), Range(random, .36f, .52f) };
            for (int ring = 0; ring < rings; ring++)
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2f / sides + ring * .31f;
                    float radius = ring == 0 ? 1f : ring == 1 ? Range(random, .8f, 1.04f) : Range(random, .4f, .62f);
                    points[ring, i] = new Vector3(Mathf.Cos(angle) * radius,
                        height[ring] + Range(random, -.05f, .05f), Mathf.Sin(angle) * radius * Range(random, .68f, .96f));
                }
            var apex = new Vector3(Range(random, -.14f, .14f), height[2] + .2f, Range(random, -.14f, .14f));
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Tri(mesh, Vector3.down * .34f, points[0, i], points[0, next]);
                for (int ring = 0; ring < rings - 1; ring++)
                    Quad(mesh, points[ring, i], points[ring + 1, i], points[ring + 1, next], points[ring, next]);
                Tri(mesh, points[rings - 1, i], apex, points[rings - 1, next]);
            }
            return mesh.Build("Морозный скальный обломок");
        }

        public static Mesh Pine(bool snow)
        {
            var mesh = new CrownRockMesh();
            const int sides = 10;
            for (int tier = 0; tier < 6; tier++)
            {
                if (snow && tier < 5) continue;
                float bottom = .15f + tier * .115f, top = bottom + .31f;
                float width = .25f * (1f - tier * .12f);
                var peak = new Vector3(.008f * tier, top + (snow ? .01f : 0f), 0f);
                for (int i = 0; i < sides; i++)
                {
                    float cover = Noise(i * .83f + tier * 3.1f, tier * 1.7f + 2.3f);
                    if (snow && cover < .26f) continue;
                    Vector3 Rim(int index)
                    {
                        float a = index * Mathf.PI * 2f / sides;
                        float r = width * (1f + Mathf.Sin(index * 2.3f + tier) * .08f) * (snow ? .84f : 1f);
                        return new Vector3(Mathf.Cos(a) * r, bottom + (snow ? .06f : 0f) + Mathf.Sin(index * 1.9f + tier) * .025f, Mathf.Sin(a) * r);
                    }
                    Tri(mesh, peak, Rim(i + 1), Rim(i));
                    if (!snow) Tri(mesh, new Vector3(0f, bottom, 0f), Rim(i), Rim(i + 1));
                }
            }
            return mesh.Build(snow ? "Снежная шапка на ели" : "Еловые лапы");
        }

        private static float Noise(float x, float y)
        {
            float value = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        public static Mesh MistBank()
        {
            var batch = new FrozenRiftBatch(false, true);
            for (int panel = 0; panel < 3; panel++)
            {
                var rotation = Quaternion.Euler(0f, panel * 60f, 0f);
                Vector3 left = rotation * new Vector3(-.5f, 0f, 0f), right = rotation * new Vector3(.5f, 0f, 0f);
                Vector3 leftTop = rotation * new Vector3(-.5f, 1f, 0f), rightTop = rotation * new Vector3(.5f, 1f, 0f);
                batch.TexturedQuad(left, right, rightTop, leftTop,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }
            return batch.Build("Полоса тумана в разломе");
        }

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c) => mesh.Triangle(a, b, c, Rock, Rock, Rock);

        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(mesh, a, b, c); Tri(mesh, a, c, d);
        }

        private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
