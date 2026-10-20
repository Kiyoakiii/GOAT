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
            float relief = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half + 6f, half + 80f, d));
            h += (Mathf.PerlinNoise(x * .021f + 41.7f, z * .021f + 19.3f) - .5f) * 8.5f * relief;
            h += (Mathf.PerlinNoise(x * .047f + 7.9f, z * .047f + 2.6f) - .5f) * 4.2f * relief;
            h += (Mathf.PerlinNoise(x * .096f + 29.1f, z * .096f + 5.4f) - .5f) * 1.5f * relief;
            return h;
        }

        public static Vector3 TerrainNormal(float x, float z)
        {
            const float e = 1.6f;
            float left = TerrainHeight(x - e, z), right = TerrainHeight(x + e, z);
            float back = TerrainHeight(x, z - e), front = TerrainHeight(x, z + e);
            return new Vector3(left - right, 2f * e, back - front).normalized;
        }

        public static bool MeshSurface(Mesh mesh, float x, float z, out float y)
        {
            y = float.MinValue;
            if (mesh == null) return false;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                float determinant = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(determinant) < .0001f) continue;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / determinant;
                if (u < -.01f || u > 1.01f) continue;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / determinant;
                if (v < -.01f || u + v > 1.01f) continue;
                float height = u * a.y + v * b.y + (1f - u - v) * c.y;
                if (height > y) y = height;
            }
            return y > float.MinValue;
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
            const int grid = 260;
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
                    float bank = Mathf.Lerp(.68f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wall, wall + 58f, away)));
                    float mottle = .74f + .26f * Mathf.PerlinNoise(x * .013f + 3.7f, z * .013f + 12.1f);
                    float ribbing = .9f + .1f * Mathf.Sin(y * .07f + Mathf.PerlinNoise(x * .02f + 5f, z * .02f + 1f) * 3f);
                    shades[j * n + i] = Mathf.Clamp(bank * mottle * ribbing, .38f, 1f);
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
            const int sides = 15;
            float[] ys = { .5f, -.34f, -2.3f, -4.8f, -8.2f };
            float[] rs = { 1f, .97f, .7f, .4f, .18f };
            float[] rings = { 1f, .78f, .56f, .38f, .26f };
            var points = new Vector3[5, sides];
            var cap = new Vector3[sides];
            for (int ring = 0; ring < 5; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides;
                    float n = RimNoise(a, seed + ring * 13, ring == 0 ? 1.15f : .5f + ring * .1f);
                    float bump = Mathf.PerlinNoise(Mathf.Cos(a) * 2.4f + seed * .31f,
                        Mathf.Sin(a) * 2.4f + ring * 1.7f) - .5f;
                    points[ring, side] = new Vector3(Mathf.Cos(a) * radius * rs[ring] * n,
                        ys[ring] + bump * radius * (ring == 0 ? .045f : .09f),
                        Mathf.Sin(a) * radius * .33f * rs[ring] * RimNoise(a + 2.1f, seed + 5, .85f));
                }
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2f / sides;
                float relief = Mathf.PerlinNoise(Mathf.Cos(a) * 3.1f + seed * .57f,
                    Mathf.Sin(a) * 3.1f + 9f) - .5f;
                cap[side] = new Vector3(Mathf.Cos(a) * radius * .52f, .64f + relief * radius * .06f,
                    Mathf.Sin(a) * radius * .18f);
            }
            Vector3 topCenter = new Vector3(radius * .05f, .55f, 0f);
            Vector3 bottomCenter = new Vector3(0f, -9.6f, 0f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                mesh.Triangle(topCenter, cap[next], cap[side], Occl(1f), Occl(.96f), Occl(.96f));
                mesh.Triangle(cap[side], points[0, side], points[0, next], Occl(.96f), Occl(rings[0]), Occl(rings[0]));
                mesh.Triangle(cap[side], points[0, next], cap[next], Occl(.96f), Occl(rings[0]), Occl(.96f));
                for (int ring = 0; ring < 4; ring++)
                {
                    mesh.Triangle(points[ring, side], points[ring + 1, side], points[ring + 1, next],
                        Occl(rings[ring]), Occl(rings[ring + 1]), Occl(rings[ring + 1]));
                    mesh.Triangle(points[ring, side], points[ring + 1, next], points[ring, next],
                        Occl(rings[ring]), Occl(rings[ring + 1]), Occl(rings[ring]));
                }
                mesh.Triangle(bottomCenter, points[4, next], points[4, side], Occl(.2f), Occl(rings[4]), Occl(rings[4]));
            }
            return Soften(mesh.Build("Зелёная полка"), .35f, .55f);
        }

        public static Mesh Cornice(float radius, int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 17;
            float phase = seed * 1.31f;
            float[] ys = { .32f, -.44f, -1.7f, -3.4f };
            float[] rs = { 1f, .92f, .6f, .18f };
            float[] rings = { 1f, .8f, .58f, .32f };
            var points = new Vector3[4, sides];
            for (int ring = 0; ring < 4; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides;
                    float thickness = 1f + Mathf.Cos(a + phase) * .55f;
                    points[ring, side] = new Vector3(
                        Mathf.Cos(a) * radius * rs[ring] * TornEdge(a, seed + ring * 11),
                        ys[ring] * thickness,
                        Mathf.Sin(a) * radius * .36f * rs[ring] * Mathf.Max(.34f, TornEdge(a + 1.9f, seed + 29)));
                }
            Vector3 topCenter = new Vector3(radius * .06f, .26f, 0f);
            Vector3 bottomCenter = new Vector3(0f, -4.1f, 0f);
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
                mesh.Triangle(bottomCenter, points[3, next], points[3, side], Occl(.22f), Occl(rings[3]), Occl(rings[3]));
            }
            return Soften(mesh.Build("Травяной козырёк"), .4f, .55f);
        }

        public static Mesh SatelliteStone(int seed)
        {
            var mesh = new CrownRockMesh();
            Lobe(mesh, Vector3.zero, 2.7f, seed, .78f);
            Lobe(mesh, new Vector3(1.6f, -.5f, .8f), 1.8f, seed + 17, .72f);
            return Soften(mesh.Build("Камень-спутник выступа"), .45f, .6f);
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

        private static float RimNoise(float angle, int seed, float strength)
        {
            float cx = Mathf.Cos(angle), sx = Mathf.Sin(angle);
            float coarse = Mathf.PerlinNoise(cx * 1.9f + seed * .31f, sx * 1.9f + seed * .67f) - .5f;
            float middle = Mathf.PerlinNoise(cx * 4.3f + seed * .77f, sx * 4.7f + seed * .13f) - .5f;
            float fine = Mathf.PerlinNoise(cx * 9.1f + seed * .07f, sx * 8.3f + seed * .89f) - .5f;
            return 1f + (coarse * .3f + middle * .17f + fine * .09f) * strength;
        }

        private static float TornEdge(float angle, int seed)
        {
            float edge = RimNoise(angle, seed, 1.6f);
            float cut = Mathf.PerlinNoise(Mathf.Cos(angle) * 2.6f + seed * .51f, Mathf.Sin(angle) * 2.6f + seed * .23f);
            if (cut > .62f) edge -= (cut - .62f) * 1.7f;
            return Mathf.Max(.36f, edge);
        }

        private static void Lobe(CrownRockMesh mesh, Vector3 center, float radius, int seed, float squash)
        {
            const int sides = 11;
            float[] ys = { -.46f, -.18f, .12f, .44f, .76f, 1.04f };
            float[] rs = { .78f, .95f, .97f, .86f, .62f, .27f };
            var points = new Vector3[6, sides];
            for (int ring = 0; ring < 6; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2f / sides;
                    float n = RimNoise(a, seed + ring * 17, 1f);
                    points[ring, side] = center + new Vector3(Mathf.Cos(a) * rs[ring] * n * radius,
                        ys[ring] * radius * squash, Mathf.Sin(a) * rs[ring] * n * .86f * radius);
                }
            Vector3 top = center + new Vector3(Mathf.Sin(seed * 2.3f) * radius * .12f, 1.08f * radius * squash,
                Mathf.Cos(seed * 1.7f) * radius * .12f);
            Vector3 bottom = center + Vector3.down * radius * .58f * squash;
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, top, points[5, next], points[5, side]);
                Tri(mesh, bottom, points[0, side], points[0, next]);
                for (int ring = 0; ring < 5; ring++)
                {
                    Tri(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next]);
                    Tri(mesh, points[ring, side], points[ring + 1, next], points[ring, next]);
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

        private static Vector3 StoneSeat(float x, float z, float radius, float squash)
        {
            float footprint = radius * .75f;
            float support = TerrainHeight(x, z);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                support = Mathf.Min(support, TerrainHeight(x + Mathf.Cos(angle) * footprint,
                    z + Mathf.Sin(angle) * footprint));
            }
            float height = radius * squash * 1.85f;
            float bottom = radius * squash * .8f;
            return new Vector3(x, support + bottom - height * .45f, z)
                - TerrainNormal(x, z) * (radius * .22f);
        }

        public static Mesh BoulderField(int count, int seed, IList<Vector3> path = null)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float z = -760f + (float)random.NextDouble() * 1520f;
                if (Mathf.Abs(z) < 210f) continue;
                float side = random.Next(2) == 0 ? -1f : 1f;
                float d = ChannelHalf(z) + 6f + (float)random.NextDouble() * 120f;
                float x = RiverCenter(z) + side * d;
                if (NearPath(path, x, z, 9f)) continue;
                float radius = 6.5f + (float)random.NextDouble() * 3.5f;
                Blob(mesh, StoneSeat(x, z, radius, .8f), radius, 7, i * 17 + 5, .8f);
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

        public static Mesh Scree(int couloirs, int perCouloir, int seed, IList<Vector3> path = null)
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
                    if (NearPath(path, x, z, 8f)) continue;
                    float radius = 1.5f + (float)random.NextDouble() * 3.2f;
                    Vector3 center = new Vector3(x, TerrainHeight(x, z), z) - TerrainNormal(x, z) * (radius * .4f);
                    Blob(mesh, center, radius, 6, c * 31 + k * 7, .7f);
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

        public static Mesh JumpSteps(IList<Vector3> points, IList<int> spots, int seed,
            out int stones, out float maxGap, out float maxRise)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            stones = 0; maxGap = 0f; maxRise = 0f;
            if (points == null || points.Count < 4 || spots == null) return mesh.Build("Валуны-ступени для прыжков");
            var centers = new Vector3[5];
            var lengths = new float[5];
            var widths = new float[5];
            var tops = new float[5];
            for (int s = 0; s < spots.Count; s++)
            {
                int i = Mathf.Clamp(spots[s], 2, points.Count - 3);
                Vector3 tangent = points[i + 1] - points[i - 1];
                tangent.y = 0f;
                if (tangent.sqrMagnitude < .001f) continue;
                tangent.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float lateral = Range(random, 3.1f, 4.5f);
                float direction = s % 2 == 0 ? 1f : -1f;
                int count = 3 + random.Next(2);
                float slope = (points[i + 1].y - points[i - 1].y)
                    / Mathf.Max(.1f, new Vector2(points[i + 1].x - points[i - 1].x,
                        points[i + 1].z - points[i - 1].z).magnitude);
                float along = -4.1f;
                bool ok = true;
                for (int k = 0; k < count; k++)
                {
                    float length = Range(random, 2.4f, 3.6f);
                    float width = Range(random, 1.7f, 2.6f);
                    float gap = Range(random, 1.6f, 3.2f);
                    centers[k] = points[i] + tangent * (along + length * .5f)
                        + side * (direction * (lateral + (k % 2 == 0 ? 0f : .35f)));
                    float ground = points[i].y + slope * (along + length * .5f);
                    if (k == 0)
                    {
                        tops[0] = ground + Range(random, .4f, .85f);
                        if (tops[0] - points[i].y > 1.4f || lateral - width * .5f > 3.5f) { ok = false; break; }
                        maxRise = Mathf.Max(maxRise, tops[0] - points[i].y);
                    }
                    else
                    {
                        float step = Range(random, -.45f, 1.05f);
                        tops[k] = tops[k - 1] + step;
                        if (tops[k] < ground + .18f) tops[k] = ground + .18f;
                        if (tops[k] - tops[k - 1] > 1.4f) { ok = false; break; }
                        maxGap = Mathf.Max(maxGap, gap);
                        maxRise = Mathf.Max(maxRise, Mathf.Abs(tops[k] - tops[k - 1]));
                    }
                    if (tops[k] - ground > 1.4f) { ok = false; break; }
                    lengths[k] = length; widths[k] = width;
                    along += length + gap;
                }
                if (!ok) continue;
                float yaw = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;
                for (int k = 0; k < count; k++)
                    FlatSlab(mesh, centers[k], tops[k],
                        yaw + ((float)random.NextDouble() - .5f) * 8f, lengths[k], widths[k],
                        Range(random, 2.2f, 4.2f), s * 17 + k * 7, .96f);
                stones += count;
            }
            return mesh.Build("Валуны-ступени для прыжков");
        }

        public static Mesh Walkway(IList<Vector3> points, float width, float lift, float shoulder)
        {
            int count = points == null ? 0 : points.Count;
            if (count < 2) return new Mesh { name = "Тропа" };
            const int tread = 7;
            var vertices = new List<Vector3>(count * (tread + 6));
            var colors = new List<Color>(count * (tread + 6));
            var triangles = new List<int>((count - 1) * 20);
            var profile = new int[count, tread];
            var skirtTopLeft = new int[count];
            var skirtBottomLeft = new int[count];
            var skirtTopRight = new int[count];
            var skirtBottomRight = new int[count];
            var floorLeft = new int[count];
            var floorRight = new int[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 a = points[Mathf.Max(i - 1, 0)], b = points[Mathf.Min(i + 1, count - 1)];
                Vector3 forward = new Vector3(b.x - a.x, 0f, b.z - a.z);
                if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                float n = i * .24f;
                float fine = i * .58f;
                float wobble = Mathf.PerlinNoise(n * .19f, 3.7f);
                float widen = 1f + (Mathf.PerlinNoise(n * .08f, 11.3f) - .5f) * .5f
                    + Mathf.Sin(n * .11f + 1.1f) * .055f;
                float halfLeft = width * .5f * widen * (1f + (Mathf.PerlinNoise(n * .13f, 21.7f) - .5f) * .26f);
                float halfRight = width * .5f * widen * (1f + (Mathf.PerlinNoise(n * .13f, 41.9f) - .5f) * .26f);
                float crown = .1f + Mathf.PerlinNoise(n * .21f, 7.7f) * .16f;
                Vector3 center = points[i] + Vector3.up * lift
                    + side * ((wobble - .5f) * width * .22f);
                float drop = shoulder * (.36f + Mathf.PerlinNoise(n * .17f, 5.1f) * .3f);
                float edgeLeft = halfLeft + shoulder * (.62f + Mathf.PerlinNoise(n * .31f, 9.3f) * .34f)
                    + (Mathf.PerlinNoise(n * .47f, 47.3f) - .5f) * shoulder * .8f;
                float edgeRight = halfRight + shoulder * (.62f + Mathf.PerlinNoise(n * .31f, 27.1f) * .34f)
                    + (Mathf.PerlinNoise(n * .43f, 71.7f) - .5f) * shoulder * .8f;
                float grain = Mathf.PerlinNoise(fine * .43f, 13.9f) - .5f;
                float[] lateral = { -edgeLeft, -halfLeft, -halfLeft * .45f, 0f, halfRight * .45f, halfRight, edgeRight };
                float[] height = { -drop, 0f, crown * .55f, crown, crown * .55f, 0f, -drop };
                float[] shade = { .72f, .96f, 1f, 1f, 1f, .96f, .72f };
                for (int k = 0; k < tread; k++)
                {
                    float jitter = (k == 0 || k == 6)
                        ? (Mathf.PerlinNoise(fine + k * 2.1f, 17.3f) - .5f) * shoulder * 1.05f
                        : (Mathf.PerlinNoise(fine + k * 2.1f, 17.3f) - .5f) * .14f;
                    float rubble = (k == 0 || k == 6) ? grain * .26f : grain * .08f;
                    profile[i, k] = vertices.Count;
                    vertices.Add(center + side * (lateral[k] + jitter)
                        + Vector3.up * (height[k] + rubble));
                    float treadShade = Mathf.Lerp(.78f, .88f, Mathf.PerlinNoise(fine * .51f + k * 1.7f, 6.1f));
                    colors.Add(Occl((k == 0 || k == 6)
                        ? shade[k] * (.9f + Mathf.PerlinNoise(fine * .51f + k * 1.7f, 6.1f) * .1f)
                        : treadShade));
                }
                skirtTopLeft[i] = vertices.Count;
                vertices.Add(vertices[profile[i, 0]]);
                colors.Add(Occl(Mathf.Clamp01(.6f + grain * .16f)));
                skirtBottomLeft[i] = vertices.Count;
                float depthLeft = 1.5f + Mathf.PerlinNoise(n * .23f, 33.1f) * 1.3f;
                vertices.Add(vertices[profile[i, 0]] + Vector3.down * depthLeft + side * (.18f + grain * .12f));
                colors.Add(Occl(.3f + Mathf.PerlinNoise(n * .29f, 2.3f) * .16f));
                skirtTopRight[i] = vertices.Count;
                vertices.Add(vertices[profile[i, 6]]);
                colors.Add(Occl(Mathf.Clamp01(.6f + grain * .16f)));
                skirtBottomRight[i] = vertices.Count;
                float depthRight = 1.5f + Mathf.PerlinNoise(n * .23f, 51.7f) * 1.3f;
                vertices.Add(vertices[profile[i, 6]] + Vector3.down * depthRight - side * (.18f + grain * .12f));
                colors.Add(Occl(.3f + Mathf.PerlinNoise(n * .29f, 8.9f) * .16f));
                floorLeft[i] = vertices.Count;
                vertices.Add(vertices[skirtBottomLeft[i]]);
                colors.Add(Occl(.22f));
                floorRight[i] = vertices.Count;
                vertices.Add(vertices[skirtBottomRight[i]]);
                colors.Add(Occl(.22f));
            }
            for (int i = 0; i < count - 1; i++)
            {
                Vector3 left = vertices[profile[i, 0]] - vertices[profile[i, 6]];
                left.y = 0f;
                left = left.sqrMagnitude > .001f ? left.normalized : Vector3.left;
                for (int k = 0; k < tread - 1; k++)
                    Quad(triangles, vertices, profile[i, k], profile[i + 1, k],
                        profile[i + 1, k + 1], profile[i, k + 1], Vector3.up);
                Quad(triangles, vertices, skirtTopLeft[i], skirtBottomLeft[i],
                    skirtBottomLeft[i + 1], skirtTopLeft[i + 1], left);
                Quad(triangles, vertices, skirtTopRight[i], skirtTopRight[i + 1],
                    skirtBottomRight[i + 1], skirtBottomRight[i], -left);
                Quad(triangles, vertices, floorLeft[i], floorRight[i],
                    floorRight[i + 1], floorLeft[i + 1], Vector3.down);
            }
            for (int end = 0; end < 2; end++)
            {
                int i = end == 0 ? 0 : count - 1;
                Vector3 a = points[Mathf.Max(i - 1, 0)], b = points[Mathf.Min(i + 1, count - 1)];
                Vector3 forward = new Vector3(b.x - a.x, 0f, b.z - a.z);
                forward = forward.sqrMagnitude > .0001f ? forward.normalized : Vector3.forward;
                var loop = new int[9];
                for (int k = 0; k < tread; k++) loop[k] = profile[i, k];
                loop[7] = skirtBottomRight[i];
                loop[8] = skirtBottomLeft[i];
                Vector3 center = Vector3.zero;
                foreach (int index in loop) center += vertices[index];
                center /= loop.Length;
                int hub = vertices.Count;
                vertices.Add(center);
                colors.Add(Occl(.44f));
                Fan(triangles, vertices, loop, hub, end == 0 ? -forward : forward);
            }
            var mesh = new Mesh
            {
                name = "Тропа по маршруту",
                indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Fan(List<int> triangles, List<Vector3> vertices, int[] loop, int hub, Vector3 target)
        {
            for (int i = 0; i < loop.Length; i++)
            {
                int a = loop[i], b = loop[(i + 1) % loop.Length];
                Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[hub];
                if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), target) >= 0f)
                {
                    triangles.Add(a); triangles.Add(b); triangles.Add(hub);
                }
                else
                {
                    triangles.Add(a); triangles.Add(hub); triangles.Add(b);
                }
            }
        }

        private static void Quad(List<int> triangles, List<Vector3> vertices, int a, int b, int c, int d,
            Vector3 target)
        {
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c], pd = vertices[d];
            Vector3 normal = Vector3.Cross(pb - pa, pc - pa) + Vector3.Cross(pc - pa, pd - pa);
            if (Vector3.Dot(normal, target) >= 0f)
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }
            else
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(a); triangles.Add(d); triangles.Add(c);
            }
        }

        public static Mesh Ravine(float width, float height, float depth, int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 15;
            var outer = new Vector3[sides];
            var mouth = new Vector3[sides];
            var throat = new Vector3[sides];
            var back = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float a = Mathf.PI * (side / (float)(sides - 1) - .5f);
                float n = 1f + Mathf.Sin(side * 2.1f + seed) * .09f + Mathf.Sin(side * 3.7f + seed * 1.3f) * .04f;
                float x = Mathf.Sin(a) * width * .5f;
                float y = Mathf.Cos(a) * height;
                mouth[side] = new Vector3(x * n, y * n, 0f);
                outer[side] = new Vector3(x * n * 1.27f, y * 1.12f + 1.5f, 2.8f + Mathf.Sin(side * 1.7f + seed) * 1.2f);
                throat[side] = new Vector3(x * n * .56f, y * .8f + 1.2f, -depth * .55f);
                back[side] = new Vector3(x * n * .19f, y * .34f + 2.4f, -depth);
            }
            Vector3 center = new Vector3(0f, height * .2f + 2.4f, -depth * 1.04f);
            for (int side = 0; side < sides - 1; side++)
            {
                int next = side + 1;
                FaceQuad(mesh, outer[side], mouth[side], outer[next], mouth[next], Vector3.forward,
                    Occl(.96f), Occl(.88f), Occl(.96f), Occl(.88f));
                FaceQuad(mesh, mouth[side], throat[side], mouth[next], throat[next], Vector3.forward,
                    Occl(.84f), Occl(.36f), Occl(.84f), Occl(.36f));
                FaceQuad(mesh, throat[side], back[side], throat[next], back[next], Vector3.forward,
                    Occl(.34f), Occl(.1f), Occl(.34f), Occl(.1f));
                FaceTriangle(mesh, center, back[next], back[side], Vector3.forward, Occl(.07f));
            }
            FaceQuad(mesh, mouth[0], mouth[sides - 1], back[sides - 1], back[0], Vector3.up,
                Occl(.2f), Occl(.2f), Occl(.1f), Occl(.1f));
            for (int side = 1; side < sides - 1; side++)
                FaceTriangle(mesh, mouth[0], mouth[side], mouth[side + 1], Vector3.forward, Occl(.2f));
            return Soften(mesh.Build("Теневое ущелье"), .4f, .5f);
        }

        private static void FlatSlab(CrownRockMesh mesh, Vector3 center, float top, float yaw,
            float length, float width, float depth, int seed, float shade)
        {
            const int sides = 9;
            var topRing = new Vector3[sides];
            var bottomRing = new Vector3[sides];
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2f / sides;
                float n = 1f + Mathf.Sin(side * 2.3f + seed) * .07f + Mathf.Sin(side * 1.3f + seed * 2.1f) * .03f;
                Vector3 local = new Vector3(Mathf.Cos(a) * length * .5f * n, 0f, Mathf.Sin(a) * width * .5f * n);
                topRing[side] = center + Vector3.up * top + rotation * local;
                bottomRing[side] = center + Vector3.up * top
                    + rotation * new Vector3(local.x * 1.16f, -depth, local.z * 1.16f);
            }
            Vector3 topCenter = center + Vector3.up * top;
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                FaceTriangle(mesh, topCenter, topRing[side], topRing[next], Vector3.up, Occl(shade));
                Vector3 outward = topRing[side] - topCenter;
                outward.y = 0f;
                FaceTriangle(mesh, topRing[side], bottomRing[side], topRing[next],
                    outward.sqrMagnitude > .001f ? outward.normalized : Vector3.forward, Occl(shade * .78f));
                FaceTriangle(mesh, topRing[next], bottomRing[side], bottomRing[next],
                    outward.sqrMagnitude > .001f ? outward.normalized : Vector3.forward, Occl(shade * .52f));
            }
        }

        private static void FaceQuad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Vector3 target, Color ca, Color cb, Color cc, Color cd)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), target) < 0f)
            {
                mesh.Triangle(a, c, b, ca, cc, cb);
                mesh.Triangle(b, c, d, cb, cc, cd);
            }
            else
            {
                mesh.Triangle(a, b, c, ca, cb, cc);
                mesh.Triangle(b, d, c, cb, cd, cc);
            }
        }

        private static void FaceTriangle(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c,
            Vector3 target, Color color)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), target) < 0f)
            {
                var swap = b; b = c; c = swap;
            }
            mesh.Triangle(a, b, c, color, color, color);
        }

        private static bool NearPath(IList<Vector3> path, float x, float z, float radius)
        {
            if (path == null) return false;
            float squared = radius * radius;
            for (int i = 0; i < path.Count; i += 2)
            {
                float dx = path[i].x - x, dz = path[i].z - z;
                if (dx * dx + dz * dz < squared) return true;
            }
            return false;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
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
