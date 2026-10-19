using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    internal static class SkyBridgeMesh
    {
        public const float WaterLevel = 6f;
        public const float ArchHalf = 240f;
        private static readonly Color Rock = new Color(0f, 0f, 0f, 1f);

        public static float Center(float z) => Mathf.Sin(z * .0041f) * 13f + Mathf.Sin(z * .0113f + 1.7f) * 4.5f;

        public static float ChannelHalf(float z) => RiverHalf(z) + 44f + 105f * Mathf.Exp(-(z * z) / (480f * 480f));

        public static float RiverHalf(float z) => 31f + Mathf.Sin(z * .0113f + 2f) * 7f
            + Mathf.Sin(z * .031f + .7f) * 4f - 9f * Mathf.Exp(-(z * z) / (230f * 230f));

        public static float RiverCenter(float z) => Center(z) + Mathf.Sin(z * .0064f) * 42f
            + Mathf.Sin(z * .0171f) * 16f;

        public static float TerrainHeight(float x, float z)
        {
            float d = Mathf.Abs(x - RiverCenter(z));
            float half = ChannelHalf(z);
            float floor = -2f + Mathf.PerlinNoise(x * .045f + 3.1f, z * .045f + 7.7f) * 4f;
            float rim = 126f + Mathf.PerlinNoise(x * .006f + 11f, z * .006f + 19f) * 20f;
            float wall = 148f + Mathf.Sin(z * .0061f + 2.1f) * 24f + Mathf.PerlinNoise(z * .014f + 3f, 4.7f) * 26f;
            float t = Mathf.Clamp01((d - half) / wall);
            float rise = 1f - Mathf.Pow(1f - t, 2.2f);
            float body = Mathf.Lerp(floor, rim, rise);
            float band = 26f + Mathf.PerlinNoise(z * .009f + 2f, x * .004f + 8f) * 13f;
            float q = (body - floor) / band;
            float fq = q - Mathf.Floor(q);
            float terraced = floor + (Mathf.Floor(q) + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1f, fq))) * band;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.03f, .24f, t))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.82f, 1f, t)));
            float h = Mathf.Lerp(body, terraced, blend * .62f);
            float basin = 98f + Mathf.PerlinNoise(x * .007f + 23f, z * .007f + 13f) * 30f;
            h = Mathf.Lerp(h, basin, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wall * .9f, wall * 2.3f, d - half)) * .8f);
            h += 74f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(300f, 700f, d));
            h += Mathf.PerlinNoise(x * .05f + 17f, z * .05f + 13f) * 2.4f;
            h += Mathf.PerlinNoise(x * .11f + 31f, z * .11f + 5f) * .9f
                + Mathf.PerlinNoise(x * .26f + 8f, z * .26f + 27f) * .35f;
            return h;
        }

        public static float RidgeCrest(float x)
        {
            float crest = 560f + Mathf.Sin(x * .0085f + 1.1f) * 44f
                + Mathf.PerlinNoise(x * .019f + 9f, 3.3f) * 120f
                + Mathf.Sin(x * .112f + 2.4f) * 9f
                + Mathf.Sin(x * .047f + 5.1f) * 24f;
            float twin = 300f * (Mathf.Exp(-Mathf.Pow((x - 210f) / 172f, 2f))
                + Mathf.Exp(-Mathf.Pow((x + 210f) / 172f, 2f)));
            float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(700f, 1000f, Mathf.Abs(x)));
            return Mathf.Lerp(crest + twin, TerrainHeight(x, 0f) - 50f, sink);
        }

        public static float RidgeTopHalf(float x) => 30f + RidgeHalfDepth(x) * .2f;

        public static float RidgeHalfDepth(float x) => 168f + Mathf.Sin(x * .0075f + 2.2f) * 34f
            + Mathf.PerlinNoise(x * .011f + 3f, 7f) * 46f;

        public static float ArchBottom(float x)
        {
            float a = Mathf.Abs(x) / ArchHalf;
            if (a >= 1f) return -40f;
            return 340f * Mathf.Pow(1f - Mathf.Pow(a, 3.0f), .45f);
        }

        public static float RidgeTop(float x, float z)
        {
            float d = Mathf.Max(1f, RidgeTopHalf(x));
            float k = Mathf.Clamp01(Mathf.Abs(z) / d);
            float crown = (1f - k * k) * 19f;
            float ripple = Mathf.Sin(x * .043f + z * .055f) * 4.4f + Mathf.Sin(x * .011f - z * .019f) * 8.5f
                + Mathf.Sin(x * .132f + z * .071f) * 3.4f;
            return RidgeCrest(x) + crown + ripple;
        }

        public static Vector3 RidgeFace(float x, float u, bool front)
        {
            float sign = front ? -1f : 1f;
            float e = Mathf.SmoothStep(0f, 1f, u);
            float depth = Mathf.Lerp(RidgeFaceDepth(x), RidgeTopHalf(x), Mathf.Pow(e, .68f)) + Rib(x, u) + Flutes(x);
            float top = RidgeTop(x, sign * TopDepth(x));
            float y = Mathf.Lerp(ArchBottom(x), top, e);
            y += Mathf.Sin(x * .029f + u * 5.1f) * 5.2f * Mathf.Sin(u * Mathf.PI);
            return new Vector3(x, y, sign * depth);
        }

        public static float MassifSurface(float x, float z)
        {
            float az = Mathf.Abs(z);
            if (az > BaseDepth(x) || az < TopDepth(x)) return float.MinValue;
            float low = 0f, high = 1f;
            for (int step = 0; step < 14; step++)
            {
                float mid = (low + high) * .5f;
                if (FaceDepthAt(x, mid) > az) low = mid; else high = mid;
            }
            return RidgeFace(x, (low + high) * .5f, z < 0f).y;
        }

        private static float FaceDepthAt(float x, float u)
        {
            float e = Mathf.SmoothStep(0f, 1f, u);
            return Mathf.Lerp(RidgeFaceDepth(x), RidgeTopHalf(x), Mathf.Pow(e, .68f)) + Rib(x, u) + Flutes(x);
        }

        public static float Walkable(float x, float z)
        {
            float ground = TerrainHeight(x, z);
            if (Mathf.Abs(x) > 548f) return ground;
            float az = Mathf.Abs(z);
            if (az > BaseDepth(x)) return ground;
            if (az <= TopDepth(x)) return Mathf.Max(ground, RidgeTop(x, z));
            if (Mathf.Abs(x) < ArchHalf) return ground;
            return Mathf.Max(ground, MassifSurface(x, z));
        }

        private static float Hash3(float x, float y, float z)
        {
            float value = Mathf.Sin(x * 127.1f + y * 311.7f + z * 74.7f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private static Mesh Jitter(Mesh mesh, float amplitude, float scale)
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
            foreach (var indices in groups.Values)
            {
                Vector3 average = Vector3.zero;
                foreach (int i in indices) average += normals[i];
                average.Normalize();
                Vector3 p = vertices[indices[0]];
                float h = Hash3(Mathf.Floor(p.x * scale), Mathf.Floor(p.y * scale), Mathf.Floor(p.z * scale));
                var offset = average * ((h - .5f) * 2f * amplitude);
                foreach (int i in indices) vertices[i] = p + offset;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static float Rib(float x, float u)
        {
            float gate = Mathf.PerlinNoise(x * .05f, 5.3f);
            float amplitude = 3f + 9f * Mathf.Pow(gate, 1.7f);
            return Mathf.Sin(x * .021f + u * 2.7f) * amplitude
                + Mathf.Sin(x * .038f - u * 1.9f) * amplitude * .4f;
        }

        private static float Flutes(float x)
        {
            float gate = Mathf.PerlinNoise(x * .055f, 3.7f);
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.6f, .84f, gate));
            if (strength <= 0f) return 0f;
            float shift = (Mathf.PerlinNoise(x * .04f, 9.1f) - .5f) * 12f;
            float amplitude = (2f + 3.8f * Mathf.Pow(gate, 2.1f)) * .02f * strength;
            return (Mathf.Sin((x + shift) * .09f) + Mathf.Sin((x + shift) * .2f + 1.7f) * .45f) * amplitude;
        }

        private static float TopDepth(float x) => RidgeTopHalf(x) + Rib(x, 1f) + Flutes(x);

        private static float BaseDepth(float x) => RidgeFaceDepth(x) + Rib(x, 0f) + Flutes(x);

        public static float RidgeBaseDepth(float x) => BaseDepth(x);

        public static float RidgeFaceDepth(float x) => RidgeHalfDepth(x) * 1.16f;

        public static Mesh Spur(float radius, float height, int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 9, rings = 5;
            float[] ys = { 0f, .22f, .48f, .72f, .9f };
            float[] rs = { 1f, .92f, .74f, .5f, .24f };
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides + ring * .14f;
                    float n = 1f + Mathf.Sin(side * 2.3f + seed) * .24f
                        + Mathf.Sin(side * 1.1f + seed * 1.7f + ring) * .13f;
                    points[ring, side] = new Vector3(Mathf.Cos(a) * radius * rs[ring] * n, height * ys[ring],
                        Mathf.Sin(a) * radius * .62f * rs[ring] * n);
                }
            Vector3 apex = new Vector3(Mathf.Sin(seed * 1.9f) * radius * .2f, height, Mathf.Cos(seed * 2.7f) * radius * .2f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, apex, points[rings - 1, next], points[rings - 1, side]);
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Tri(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next]);
                    Tri(mesh, points[ring, side], points[ring + 1, next], points[ring, next]);
                }
            }
            var spur = mesh.Build("Скальный отрог");
            Jitter(spur, 3.2f, .06f);
            return Soften(spur, .45f, .6f);
        }

        public static Mesh Terrain()
        {
            var mesh = new CrownRockMesh();
            const int grid = 232;
            int n = grid + 1;
            var points = new Vector3[n * n];
            var shades = new float[n * n];
            for (int j = 0; j < n; j++)
            {
                float v = j / (float)grid * 2f - 1f;
                float z = Dense(v, 1900f, .3f);
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)grid * 2f - 1f;
                    float x = Dense(u, 1900f, .2f);
                    float y = TerrainHeight(x, z);
                    float far = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1450f, 1880f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))));
                    points[j * n + i] = new Vector3(x, Mathf.Lerp(y, -340f, far), z);
                    float away = Mathf.Abs(x - RiverCenter(z));
                    float wall = ChannelHalf(z);
                    shades[j * n + i] = Mathf.Lerp(.68f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wall, wall + 58f, away)));
                }
            }
            for (int j = 0; j < grid; j++)
                for (int i = 0; i < grid; i++)
                {
                    Vector3 a = points[j * n + i], b = points[j * n + i + 1];
                    Vector3 c = points[(j + 1) * n + i], d = points[(j + 1) * n + i + 1];
                    float oa = shades[j * n + i], ob = shades[j * n + i + 1];
                    float oc = shades[(j + 1) * n + i], od = shades[(j + 1) * n + i + 1];
                    Tri(mesh, a, c, b, oa, oc, ob); Tri(mesh, b, c, d, ob, oc, od);
                }
            var built = mesh.Build("Склоны ущелья, террасы и плато");
            Jitter(built, 2.2f, .05f);
            return Soften(built, .62f, 1f);
        }

        private static float Dense(float s, float extent, float linear) => extent * (linear * s + (1f - linear) * s * s * s * s * s);

        public static Mesh Ridge()
        {
            var mesh = new CrownRockMesh();
            const int columns = 140, rows = 18, topRows = 5;
            var xs = new float[columns + 1];
            for (int i = 0; i <= columns; i++)
            {
                float s = i / (float)columns * 2f - 1f;
                xs[i] = 1000f * (.4f * s + .6f * s * Mathf.Pow(Mathf.Abs(s), 1.35f));
            }
            for (int k = 0; k < rows; k++)
            {
                float u0 = k / (float)rows, u1 = (k + 1f) / rows;
                for (int i = 0; i < columns; i++)
                {
                    Vector3 a = RidgeFace(xs[i], u0, true), b = RidgeFace(xs[i + 1], u0, true);
                    Vector3 c = RidgeFace(xs[i], u1, true), d = RidgeFace(xs[i + 1], u1, true);
                    float o0 = Mathf.Lerp(.6f, 1f, u0), o1 = Mathf.Lerp(.6f, 1f, u1);
                    Tri(mesh, a, c, b, o0, o1, o0); Tri(mesh, c, d, b, o1, o1, o0);
                    a = RidgeFace(xs[i], u0, false); b = RidgeFace(xs[i + 1], u0, false);
                    c = RidgeFace(xs[i], u1, false); d = RidgeFace(xs[i + 1], u1, false);
                    Tri(mesh, a, b, c, o0, o0, o1); Tri(mesh, c, b, d, o1, o0, o1);
                }
            }
            for (int i = 0; i < columns; i++)
            {
                float d0 = TopDepth(xs[i]);
                float d1 = TopDepth(xs[i + 1]);
                for (int k = 0; k < topRows; k++)
                {
                    float s0 = k / (float)topRows, s1 = (k + 1f) / topRows;
                    Vector3 a = new Vector3(xs[i], RidgeTop(xs[i], Mathf.Lerp(-d0, d0, s0)), Mathf.Lerp(-d0, d0, s0));
                    Vector3 b = new Vector3(xs[i + 1], RidgeTop(xs[i + 1], Mathf.Lerp(-d1, d1, s0)), Mathf.Lerp(-d1, d1, s0));
                    Vector3 c = new Vector3(xs[i], RidgeTop(xs[i], Mathf.Lerp(-d0, d0, s1)), Mathf.Lerp(-d0, d0, s1));
                    Vector3 d = new Vector3(xs[i + 1], RidgeTop(xs[i + 1], Mathf.Lerp(-d1, d1, s1)), Mathf.Lerp(-d1, d1, s1));
                    Tri(mesh, a, c, b, 1f, 1f, 1f); Tri(mesh, b, c, d, 1f, 1f, 1f);
                }
            }
            for (int end = 0; end < 2; end++)
            {
                int index = end == 0 ? 0 : columns;
                float x = xs[index];
                float depth = TopDepth(x);
                var outline = new List<Vector3>();
                for (int k = 0; k <= rows; k++) outline.Add(RidgeFace(x, k / (float)rows, true));
                for (int k = 0; k <= topRows; k++)
                    outline.Add(new Vector3(x, RidgeTop(x, Mathf.Lerp(-depth, depth, k / (float)topRows)),
                        Mathf.Lerp(-depth, depth, k / (float)topRows)));
                for (int k = rows; k >= 0; k--) outline.Add(RidgeFace(x, k / (float)rows, false));
                Vector3 center = new Vector3(x, (ArchBottom(x) + RidgeCrest(x)) * .5f, 0f);
                for (int k = 0; k < outline.Count; k++)
                {
                    Vector3 a = outline[k], b = outline[(k + 1) % outline.Count];
                    if (end == 0) Tri(mesh, center, b, a, .3f, .3f, .3f); else Tri(mesh, center, a, b, .3f, .3f, .3f);
                }
            }
            var arch = mesh.Build("Гребень со сквозным проёмом");
            Jitter(arch, 2.2f, .02f);
            return Soften(arch, .4f, .7f);
        }

        public static Mesh Vault()
        {
            var mesh = new CrownRockMesh();
            const int segments = 6;
            var columns = new List<float>();
            for (float x = -ArchHalf + 4f; x <= ArchHalf - 4f; x += 3.4f) columns.Add(x);
            for (int i = 0; i < columns.Count - 1; i++)
                for (int s = 0; s < segments; s++)
                {
                    float s0 = s / (float)segments, s1 = (s + 1f) / segments;
                    Vector3 a = VaultPoint(columns[i], s0), b = VaultPoint(columns[i + 1], s0);
                    Vector3 c = VaultPoint(columns[i], s1), d = VaultPoint(columns[i + 1], s1);
                    float o0 = Mathf.Lerp(.94f, .58f, Mathf.Abs(s0 - .5f) * 2f);
                    float o1 = Mathf.Lerp(.94f, .58f, Mathf.Abs(s1 - .5f) * 2f);
                    Tri(mesh, a, b, c, o0, o0, o1); Tri(mesh, c, b, d, o1, o0, o1);
                }
            for (int spike = 0; spike < 30; spike++)
            {
                float x = -ArchHalf + 14f + (spike * 37 % 100) / 100f * (ArchHalf * 2f - 28f);
                float s = .16f + (spike * 53 % 100) / 100f * .68f;
                Vector3 basePoint = VaultPoint(x, s) - Vector3.up * .6f;
                Spike(mesh, basePoint, 3.4f + (spike * 13 % 5) * 1.2f, 14f + (spike * 29 % 26), spike, .55f);
            }
            return Soften(mesh.Build("Свод, карнизы и сталактиты"), .35f, .5f);
        }

        private static Vector3 VaultPoint(float x, float s)
        {
            float front = -BaseDepth(x);
            float back = BaseDepth(x);
            float y = ArchBottom(x) + Mathf.Sin(s * Mathf.PI) * 16f;
            return new Vector3(x, y, Mathf.Lerp(front, back, s));
        }

        private static void Spike(CrownRockMesh mesh, Vector3 basePoint, float radius, float length, int seed, float shade = 1f)
        {
            const int sides = 6;
            var ring = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2f / sides;
                float n = 1f + Mathf.Sin(side * 2.3f + seed) * .18f;
                ring[side] = basePoint + new Vector3(Mathf.Cos(a) * radius * n, 0f, Mathf.Sin(a) * radius * n);
            }
            Vector3 tip = basePoint + Vector3.down * length
                + new Vector3(Mathf.Sin(seed * 1.7f) * radius * .5f, 0f, Mathf.Cos(seed * 2.1f) * radius * .5f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                mesh.Triangle(tip, ring[side], ring[next], Occl(shade * .72f), Occl(shade * .9f), Occl(shade * .9f));
                mesh.Triangle(basePoint, ring[next], ring[side], Occl(shade), Occl(shade * .9f), Occl(shade * .9f));
            }
        }

        public static Mesh Ledge(float radius, int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 9;
            float[] ys = { .5f, -.3f, -2.2f, -4.6f };
            float[] rs = { 1f, .96f, .66f, .12f };
            var points = new Vector3[4, sides];
            for (int ring = 0; ring < 4; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides;
                    float n = 1f + Mathf.Sin(side * 2.7f + seed) * .22f + Mathf.Sin(side * 1.3f + seed * 2.1f) * .12f;
                    float rx = radius * rs[ring] * n;
                    float rz = radius * .33f * rs[ring] * (1f + Mathf.Sin(side * 1.7f + seed) * .26f);
                    points[ring, side] = new Vector3(Mathf.Cos(a) * rx, ys[ring], Mathf.Sin(a) * rz);
                }
            Vector3 topCenter = new Vector3(0f, .8f, 0f);
            Vector3 bottomCenter = new Vector3(0f, -6f, 0f);
            float[] rings = { 1f, .78f, .52f, .34f };
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                mesh.Triangle(topCenter, points[0, next], points[0, side], Occl(1f), Occl(rings[0]), Occl(rings[0]));
                for (int ring = 0; ring < 3; ring++)
                {
                    mesh.Triangle(points[ring, side], points[ring + 1, side], points[ring + 1, next],
                        Occl(rings[ring]), Occl(rings[ring + 1]), Occl(rings[ring + 1]));
                    mesh.Triangle(points[ring, side], points[ring + 1, next], points[ring, next],
                        Occl(rings[ring]), Occl(rings[ring + 1]), Occl(rings[ring]));
                }
                mesh.Triangle(bottomCenter, points[3, next], points[3, side], Occl(.28f), Occl(rings[3]), Occl(rings[3]));
            }
            return mesh.Build("Зелёная полка");
        }

        public static Mesh River()
        {
            const int rows = 200, cols = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(-1200f, 1200f, row / (float)rows);
                float half = RiverHalf(z);
                float middle = RiverCenter(z);
                for (int col = 0; col <= cols; col++)
                {
                    float across = col / (float)cols * 2f - 1f;
                    float bend = Mathf.Cos(z * .0091f + 1.3f) * 6f * (1f - across * across);
                    vertices.Add(new Vector3(middle + across * half + bend, WaterLevel, z));
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.58f, 1f, Mathf.Abs(across)));
                    colors.Add(new Color(edge, 0f, 0f, 1f));
                }
            }
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    int a = row * (cols + 1) + col, b = a + 1, c = a + cols + 1, d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            return Liquid("Бирюзовая река", vertices, triangles, colors);
        }

        public static Mesh Pool(float radiusX, float radiusZ, int seed)
        {
            const int sides = 18;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            vertices.Add(new Vector3(0f, .12f, 0f));
            colors.Add(new Color(0f, 0f, 0f, 1f));
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2f / sides;
                float n = 1f + Mathf.Sin(side * 2.7f + seed) * .09f;
                vertices.Add(new Vector3(Mathf.Cos(a) * radiusX * n, 0f + Mathf.Sin(side * 1.7f + seed) * .12f,
                    Mathf.Sin(a) * radiusZ * n));
                colors.Add(new Color(1f, 0f, 0f, 1f));
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides + 1;
                triangles.Add(0); triangles.Add(next); triangles.Add(side + 1);
            }
            return Liquid("Заводь", vertices, triangles, colors);
        }

        public static Mesh Fall(Vector3 top, Vector3 bottom, Vector3 outward, float width, float bulge, int seed)
        {
            const int rows = 18, cols = 5;
            Vector3 bulgeDir = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, bulgeDir).normalized;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            var uv = new List<Vector2>();
            for (int row = 0; row <= rows; row++)
            {
                float s = row / (float)rows;
                float widthAt = width * Mathf.Lerp(1f, .74f, s);
                Vector3 center = Vector3.Lerp(top, bottom, s)
                    + bulgeDir * Mathf.Sin(s * Mathf.PI) * bulge
                    + side * Mathf.Sin(s * 6.3f + seed) * width * .04f * s;
                for (int col = 0; col <= cols; col++)
                {
                    float across = col / (float)cols * 2f - 1f;
                    vertices.Add(center + side * across * widthAt * .5f
                        + bulgeDir * Mathf.Abs(across) * Mathf.Abs(across) * bulge * .18f);
                    colors.Add(new Color(.4f, 0f, 0f, 1f));
                    uv.Add(new Vector2(col / (float)cols, s));
                }
            }
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    int a = row * (cols + 1) + col, b = a + 1, c = a + cols + 1, d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            return Liquid("Лента водопада", vertices, triangles, colors, uv);
        }

        private static Mesh Liquid(string name, List<Vector3> vertices, List<int> triangles, List<Color> colors,
            List<Vector2> uv = null)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors);
            if (uv != null) mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh MistPuff(float radius, int seed)
        {
            var mesh = new CrownRockMesh();
            Blob(mesh, Vector3.zero, radius, 9, seed);
            return mesh.Build("Клуб тумана");
        }

        private static void Blob(CrownRockMesh mesh, Vector3 center, float radius, int sides, int seed, float squash = 1f)
        {
            float[] ys = { -.5f, -.15f, .25f, .62f };
            float[] rs = { .68f, 1f, .82f, .36f };
            var points = new Vector3[4, sides];
            for (int ring = 0; ring < 4; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides;
                    float n = 1f + Mathf.Sin(side * 2.7f + seed) * .14f + Mathf.Sin(side * 1.3f + seed * 2.1f + ring) * .07f;
                    points[ring, side] = center + new Vector3(Mathf.Cos(a) * rs[ring] * n * radius, ys[ring] * radius * squash,
                        Mathf.Sin(a) * rs[ring] * n * .85f * radius);
                }
            Vector3 top = center + Vector3.up * radius * 1.05f * squash;
            Vector3 bottom = center + Vector3.down * radius * .8f * squash;
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                mesh.Triangle(top, points[3, next], points[3, side], Rock, Rock, Rock);
                mesh.Triangle(bottom, points[0, side], points[0, next], Rock, Rock, Rock);
                for (int ring = 0; ring < 3; ring++)
                {
                    mesh.Triangle(points[ring, side], points[ring + 1, side], points[ring + 1, next], Rock, Rock, Rock);
                    mesh.Triangle(points[ring, side], points[ring + 1, next], points[ring, next], Rock, Rock, Rock);
                }
            }
        }

        public static Mesh FarRanges()
        {
            var mesh = new CrownRockMesh();
            for (int i = 0; i < 36; i++)
            {
                float angle = i / 36f * Mathf.PI * 2f + .13f;
                float radius = 900f + (i * 149 % 300);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                if (Mathf.Abs(x) < 420f) z += Mathf.Sign(z) * 900f;
                Massif(mesh, new Vector3(x, -60f, z), 180f + (i * 113 % 180), 240f + (i * 97 % 140), i);
            }
            for (int i = 0; i < 26; i++)
            {
                float angle = i / 26f * Mathf.PI * 2f + .31f;
                float width = 400f + (i * 101 % 220);
                float radius = 2100f + width + (i * 181 % 460);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                if (Mathf.Abs(x) < 500f) z += Mathf.Sign(z) * 800f;
                float height = 420f + (i * 167 % 340);
                Massif(mesh, new Vector3(x, -110f, z), height, width, i + 61);
                Massif(mesh, new Vector3(x + width * .7f, -110f, z - width * .55f),
                    height * .62f, width * .62f, i + 97);
            }
            var ranges = mesh.Build("Дальние снежные хребты");
            Jitter(ranges, 5f, .0025f);
            return Soften(ranges, .05f, 1f);
        }

        private static void Massif(CrownRockMesh mesh, Vector3 basePoint, float height, float width, int seed)
        {
            const int sides = 21, rings = 8;
            float[] ys = { 0f, .1f, .23f, .38f, .53f, .68f, .82f, .93f };
            float[] rs = { 1f, .97f, .9f, .79f, .65f, .49f, .32f, .16f };
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides + ring * .07f + seed * .13f;
                    float n = 1f + Mathf.Sin(side * 2.1f + seed) * .15f
                        + Mathf.Sin(side * 1.3f + seed * 1.7f + ring * 1.9f) * .08f;
                    points[ring, side] = basePoint + new Vector3(Mathf.Cos(a) * width * rs[ring] * n,
                        height * ys[ring] + Mathf.Sin(side * 1.7f + ring) * height * .015f,
                        Mathf.Sin(a) * width * rs[ring] * n * .82f);
                }
            Vector3 apex = basePoint + new Vector3(Mathf.Sin(seed * 2.3f) * width * .1f, height,
                Mathf.Cos(seed * 1.9f) * width * .1f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, apex, points[rings - 1, next], points[rings - 1, side]);
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Tri(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next]);
                    Tri(mesh, points[ring, side], points[ring + 1, next], points[ring, next]);
                }
            }
        }

        private static Mesh Soften(Mesh mesh, float threshold, float blend)
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
                        if (Vector3.Dot(normals[i], normals[j]) > threshold) average += normals[j];
                    result[i] = Vector3.Lerp(normals[i], average.normalized, blend).normalized;
                }
            mesh.normals = result;
            return mesh;
        }

        internal sealed class TreeCluster
        {
            private readonly CrownRockMesh needles = new CrownRockMesh();
            private readonly CrownRockMesh trunks = new CrownRockMesh();
            private static readonly Color Tint = new Color(0f, 0f, 0f, 1f);

            public void Add(float x, float y, float z, float size, float rotation)
            {
                float cos = Mathf.Cos(rotation * Mathf.Deg2Rad), sin = Mathf.Sin(rotation * Mathf.Deg2Rad);
                Vector3 P(float px, float py, float pz) => new Vector3(x + (px * cos - pz * sin) * size,
                    y + py * size, z + (px * sin + pz * cos) * size);
                float[] baseY = { .1f, .27f, .43f, .58f, .72f };
                float[] heights = { .4f, .34f, .28f, .22f, .16f };
                float[] widths = { .3f, .26f, .215f, .165f, .105f };
                for (int tier = 0; tier < 5; tier++)
                {
                    Vector3 apex = P(0f, baseY[tier] + heights[tier], 0f);
                    Vector3 center = P(0f, baseY[tier], 0f);
                    var ring = new Vector3[6];
                    for (int side = 0; side < 6; side++)
                    {
                        float angle = (side * 60f + tier * 23f) * Mathf.Deg2Rad;
                        float w = widths[tier] * (1f + Mathf.Sin(side * 2.3f + tier * 1.7f) * .1f);
                        ring[side] = P(Mathf.Cos(angle) * w, baseY[tier] + Mathf.Sin(side * 1.9f + tier) * .008f,
                            Mathf.Sin(angle) * w);
                    }
                    for (int side = 0; side < 6; side++)
                    {
                        int next = (side + 1) % 6;
                        needles.Triangle(apex, ring[next], ring[side], Tint, Tint, Tint);
                        needles.Triangle(center, ring[side], ring[next], Tint, Tint, Tint);
                    }
                }
                Vector3 bottom = P(0f, -.04f, 0f);
                var lower = new Vector3[5];
                var upper = new Vector3[5];
                for (int side = 0; side < 5; side++)
                {
                    float angle = side * 72f * Mathf.Deg2Rad;
                    lower[side] = P(Mathf.Cos(angle) * .045f, -.04f, Mathf.Sin(angle) * .045f);
                    upper[side] = P(Mathf.Cos(angle) * .03f, .46f, Mathf.Sin(angle) * .03f);
                }
                for (int side = 0; side < 5; side++)
                {
                    int next = (side + 1) % 5;
                    trunks.Triangle(bottom, lower[side], lower[next], Tint, Tint, Tint);
                    trunks.Triangle(lower[side], upper[side], lower[next], Tint, Tint, Tint);
                    trunks.Triangle(lower[next], upper[side], upper[next], Tint, Tint, Tint);
                }
            }

            public Mesh Needles(string name) => needles.Build(name);
            public Mesh Trunks(string name) => trunks.Build(name);
        }

        public static Mesh BoulderField(int count, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float z = -760f + (float)random.NextDouble() * 1520f;
                if (Mathf.Abs(z) < 300f) continue;
                float side = random.Next(2) == 0 ? -1f : 1f;
                float d = ChannelHalf(z) + 6f + (float)random.NextDouble() * 120f;
                float x = RiverCenter(z) + side * d;
                float y = TerrainHeight(x, z);
                Blob(mesh, new Vector3(x, y + 2f, z), 6.5f + (float)random.NextDouble() * 3.5f, 7, i * 17 + 5, .8f);
            }
            return mesh.Build("Валуны и осыпи ущелья");
        }

        public static Mesh GravelBars()
        {
            const int rows = 170, cols = 4;
            var mesh = new CrownRockMesh();
            var left = new Vector3[rows + 1, cols + 1];
            var right = new Vector3[rows + 1, cols + 1];
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(-1200f, 1200f, row / (float)rows);
                float middle = RiverCenter(z), half = RiverHalf(z);
                for (int col = 0; col <= cols; col++)
                {
                    float t = col / (float)cols;
                    float wobble = Mathf.Sin(z * .031f + col * 1.7f) * 4f + Mathf.Sin(z * .011f) * 3f;
                    float dx = half + 3f + t * (72f + wobble);
                    float y = WaterLevel - .5f - t * 5f + Mathf.Sin(z * .09f + col) * .9f;
                    left[row, col] = new Vector3(middle - dx, y, z);
                    right[row, col] = new Vector3(middle + dx, y, z);
                }
            }
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    Tri(mesh, right[row, col], right[row + 1, col], right[row, col + 1]);
                    Tri(mesh, right[row, col + 1], right[row + 1, col], right[row + 1, col + 1]);
                    Tri(mesh, left[row, col], left[row, col + 1], left[row + 1, col]);
                    Tri(mesh, left[row, col + 1], left[row + 1, col + 1], left[row + 1, col]);
                }
            return mesh.Build("Галька и подмытые берега");
        }

        public static Mesh Shoals(int count, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float z = -700f + (float)random.NextDouble() * 1400f;
                float middle = RiverCenter(z), half = RiverHalf(z);
                float x = middle + ((float)random.NextDouble() - .5f) * half * 1.1f;
                Blob(mesh, new Vector3(x, WaterLevel - .55f, z), 8f + (float)random.NextDouble() * 14f, 7, i * 19 + 3, .16f);
            }
            return mesh.Build("Мели в русле");
        }

        public static Mesh Niche(float radius, float height, int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 9, rings = 5;
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float y = height * t;
                float r = radius * (.55f + .45f * Mathf.Sin(t * Mathf.PI * .9f + .3f));
                for (int side = 0; side < sides; side++)
                {
                    float a = Mathf.PI * (side / (float)(sides - 1) - .5f);
                    float n = 1f + Mathf.Sin(side * 2.1f + seed + ring) * .12f;
                    points[ring, side] = new Vector3(Mathf.Sin(a) * r * n, y, -Mathf.Cos(a) * r * .55f * n + radius * .3f);
                }
            }
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides - 1; side++)
                {
                    Tri(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, side + 1]);
                    Tri(mesh, points[ring, side], points[ring + 1, side + 1], points[ring, side + 1]);
                }
            return Soften(mesh.Build("Подмытая ниша"), .4f, .5f);
        }

        public static Mesh Scree(int couloirs, int perCouloir, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int c = 0; c < couloirs; c++)
            {
                if (Mathf.PerlinNoise(c * 3.7f, 1.3f) < .34f) continue;
                float side = c % 2 == 0 ? -1f : 1f;
                float z0 = -680f + (float)random.NextDouble() * 1360f;
                float t0 = .08f + (float)random.NextDouble() * .45f;
                int count = Mathf.RoundToInt(perCouloir * (.35f + 1.25f * Mathf.PerlinNoise(c * 1.9f, 7.7f)));
                for (int k = 0; k < count; k++)
                {
                    if (Mathf.PerlinNoise(c * 5.1f + k * .37f, 2.9f) < .22f) continue;
                    float z = z0 + (float)random.NextDouble() * 26f - 13f;
                    float t = t0 + (float)random.NextDouble() * .55f;
                    float d = ChannelHalf(z) + t * 132f;
                    float x = RiverCenter(z) + side * d;
                    Blob(mesh, new Vector3(x, TerrainHeight(x, z) + .6f, z),
                        1.5f + (float)random.NextDouble() * 3.2f, 6, c * 31 + k * 7, .7f);
                }
            }
            return mesh.Build("Осыпи в кулуарах");
        }

        public static Mesh Cairn(int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            float y = 0f;
            for (int i = 0; i < 5; i++)
            {
                float radius = 1.6f - i * .24f;
                Blob(mesh, new Vector3(((float)random.NextDouble() - .5f) * .5f, y,
                    ((float)random.NextDouble() - .5f) * .5f), radius, 7, seed + i * 11, .55f);
                y += radius * .68f;
            }
            return mesh.Build("Каменный тур");
        }

        public static Mesh JumpSteps(IList<Vector3> points, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int i = 2; i < points.Count - 2; i += 2)
            {
                Vector3 p = points[i];
                float r = 3.4f + (float)random.NextDouble() * 3.6f;
                var offset = new Vector3(((float)random.NextDouble() - .5f) * 8f,
                    -.9f - r * .12f, ((float)random.NextDouble() - .5f) * 8f);
                Blob(mesh, p + offset, r, 7, i * 13 + 5, .52f);
                if (random.NextDouble() < .45)
                    Blob(mesh, p + offset + new Vector3(r * 1.25f, -r * .3f, -r * .9f), r * .58f, 6,
                        i * 29 + 11, .58f);
            }
            return mesh.Build("Валуны-ступени для прыжков");
        }

        public static Mesh Fords(int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            float[] spots = { 132f, -18f, -168f };
            for (int s = 0; s < spots.Length; s++)
            {
                float z0 = spots[s];
                float middle = RiverCenter(z0), half = RiverHalf(z0);
                for (int k = 0; k < 10; k++)
                {
                    float t = k / 9f;
                    float x = middle + Mathf.Lerp(-half * .95f, half * .95f, t);
                    float z = z0 + Mathf.Sin(t * 6f + s) * 7f;
                    Blob(mesh, new Vector3(x, WaterLevel - .5f, z), 2.4f + (float)random.NextDouble() * 1.7f,
                        6, s * 19 + k * 5, .45f);
                }
            }
            return mesh.Build("Камни-переправа");
        }

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
            => mesh.Triangle(a, b, c, Rock, Rock, Rock);

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, float oa, float ob, float oc)
            => mesh.Triangle(a, b, c, Occl(oa), Occl(ob), Occl(oc));

        private static Color Occl(float value) => new Color(Mathf.Clamp01(value), 0f, 0f, 1f);
    }
}
