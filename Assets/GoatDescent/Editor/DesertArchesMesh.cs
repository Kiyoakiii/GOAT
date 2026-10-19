using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    internal static class DesertArchesMesh
    {
        private static readonly Color Open = new Color(1f, 1f, 1f, 1f);

        public const float FloorY = -104f;
        public const float TerrainExtent = 840f;
        public const float TerrainOuter = 2700f;
        private const float GorgeDepth = 54f;
        private const float HalfWidth = 62f;
        private const float Relief = 118f;
        private const float StepMin = 12f;
        private const float StepMax = 28f;
        private const float StepCanyon = 34f;
        private const float RiverExtent = 820f;

        private static readonly float[] Bluffs =
        {
            330f, -40f, 200f, 62f,
            250f, -430f, 180f, 52f,
            -320f, 330f, 205f, 60f,
            -560f, 120f, 150f, 74f,
            -500f, -330f, 170f, 88f,
            520f, 260f, 175f, 78f,
            300f, 570f, 155f, 62f,
            -170f, 650f, 150f, 70f,
            650f, -300f, 180f, 92f,
            790f, 240f, 160f, 84f,
            140f, 830f, 170f, 66f,
            -730f, -560f, 180f, 88f,
            430f, -710f, 165f, 72f,
            -270f, -700f, 160f, 78f,
            930f, -60f, 150f, 60f,
            -960f, 300f, 155f, 70f,
            -1180f, -180f, 190f, 74f,
            1150f, 620f, 200f, 82f
        };

        public static CrownRockMesh Collector() => new CrownRockMesh();

        public static Mesh Finish(CrownRockMesh mesh, string name) => mesh.Build(name);

        public static void Stamp(CrownRockMesh target, Mesh source, Matrix4x4 matrix)
        {
            var vertices = source.vertices;
            var triangles = source.triangles;
            bool mirrored = matrix.determinant < 0f;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(vertices[triangles[i]]);
                Vector3 b = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                Vector3 c = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);
                if (mirrored) target.Triangle(a, c, b, Open, Open, Open);
                else target.Triangle(a, b, c, Open, Open, Open);
            }
        }

        public static void Chip(CrownRockMesh target, Matrix4x4 matrix, System.Random random)
        {
            var points = new Vector3[4];
            for (int i = 0; i < 4; i++)
                points[i] = matrix.MultiplyPoint3x4(new Vector3(
                    Range(random, -1f, 1f), Range(random, -.4f, .55f), Range(random, -1f, 1f)));
            Vector3 center = (points[0] + points[1] + points[2] + points[3]) * .25f;
            Face(target, points, 0, 1, 2, center);
            Face(target, points, 0, 2, 3, center);
            Face(target, points, 0, 3, 1, center);
            Face(target, points, 1, 3, 2, center);
        }

        private static void Face(CrownRockMesh mesh, Vector3[] points, int a, int b, int c, Vector3 center)
        {
            Vector3 normal = Vector3.Cross(points[b] - points[a], points[c] - points[a]);
            if (Vector3.Dot(normal, points[a] - center) < 0f) Tri(mesh, points[a], points[c], points[b]);
            else Tri(mesh, points[a], points[b], points[c]);
        }

        public static float ChannelX(float z)
        {
            return 122f * Mathf.Sin(z * .00415f) + 44f * Mathf.Sin(z * .0131f + 1.1f);
        }

        public static float ChannelZ(float x)
        {
            return 118f * Mathf.Sin(x * .0046f + .7f) + 38f * Mathf.Sin(x * .0122f + 2.3f);
        }

        public static float ChannelDistance(float x, float z)
        {
            return Mathf.Min(Mathf.Abs(x - ChannelX(z)), Mathf.Abs(z - ChannelZ(x)));
        }

        public static float Height(float x, float z)
        {
            float distance = ChannelDistance(x, z);
            distance += ((Mathf.PerlinNoise(x * .0032f + 101f, z * .0031f + 43f) - .5f) * 74f
                + (Mathf.PerlinNoise(x * .0087f + 7f, z * .0081f + 91f) - .5f) * 28f)
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 115f, distance));
            float relief = Relief
                + (Mathf.PerlinNoise(x * .0017f + 13f, z * .0017f + 7f) - .5f) * 62f
                + (Mathf.PerlinNoise(x * .0055f + 41f, z * .0055f + 23f) - .5f) * 26f
                + Bluff(x, z);
            float plateau = FloorY + relief;
            float floor = FloorY - GorgeDepth;
            float carve = .30f * Bench(distance, 0f, 22f)
                + .20f * Bench(distance, 46f, 68f)
                + .18f * Bench(distance, 96f, 118f)
                + .16f * Bench(distance, 146f, 168f)
                + .16f * Bench(distance, 196f, 218f);
            float height = plateau - (relief + GorgeDepth) * carve;
            float upland = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .3f, carve));
            float near = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HalfWidth + 340f, HalfWidth + 30f, distance));
            float step = Mathf.Lerp(Mathf.Lerp(StepMin, StepMax, Mathf.PerlinNoise(x * .0038f + 63f, z * .0038f + 29f)), StepCanyon, near);
            float unit = (height - floor) / step
                + (Mathf.PerlinNoise(x * .0052f + 5f, z * .0048f + 9f) - .5f) * 1.7f
                + (Mathf.PerlinNoise(x * .0135f + 17f, z * .0125f + 3f) - .5f) * .85f;
            float level = Mathf.Floor(unit);
            float fraction = unit - level;
            float riser = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.52f, .99f, fraction));
            float terraced = floor + (level + riser) * step + riser * (1f - riser) * step * .22f;
            height = Mathf.Lerp(height, terraced, upland * .38f);
            float wall = carve * (1f - carve) * 4f;
            height += (Mathf.PerlinNoise(x * .0075f + 31f, z * .0075f + 17f) - .5f) * 11f * wall;
            float playa = Mathf.Clamp01(1f - Mathf.Abs(carve - .64f) / .17f);
            if (playa > .01f)
            {
                float dune = Mathf.Sin(x * .028f + z * .011f + Mathf.PerlinNoise(x * .0039f + 61f, z * .0036f + 13f) * 6f) * 2.6f;
                dune += Mathf.Sin(x * .061f - z * .042f + Mathf.PerlinNoise(x * .0071f + 19f, z * .0067f + 89f) * 5f) * 1.3f;
                height += dune * playa;
                height -= (1f - Cracks(x, z, .03f)) * 5f * playa;
            }
            return height;
        }

        private static float Cracks(float x, float z, float scale)
        {
            float px = x * scale, pz = z * scale;
            float gx = Mathf.Floor(px), gz = Mathf.Floor(pz);
            float best = 99f, second = 99f;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    float cellX = gx + i, cellZ = gz + j;
                    float ox = Mathf.PerlinNoise(cellX * .37f + 5.1f, cellZ * .53f + 11.7f);
                    float oz = Mathf.PerlinNoise(cellX * .53f + 23.3f, cellZ * .37f + 7.9f);
                    float dx = cellX + ox - px, dz = cellZ + oz - pz;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d < best) { second = best; best = d; }
                    else if (d < second) second = d;
                }
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .7f, second - best));
        }

        private static float Bench(float distance, float from, float to)
        {
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, distance));
        }

        public static float Surface(float x, float z)
        {
            float radius = Mathf.Sqrt(x * x + z * z);
            float rolling = (Mathf.PerlinNoise(x * .00062f + 71f, z * .00062f + 37f) - .5f) * 170f
                + (Mathf.PerlinNoise(x * .0016f + 11f, z * .0016f + 91f) - .5f) * 70f;
            float massif = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(780f, 1520f, radius));
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1140f, 1640f, radius));
            float far = FloorY + 112f + rolling
                + Mesa(x * .00092f + 23f, z * .00092f + 59f, 3.4f) * 132f
                + Mesa(x * .0019f + 37f, z * .0019f + 83f, 2.6f) * (60f + 190f * massif)
                + Mesa(x * .0026f + 7f, z * .0026f + 13f, 2.2f) * 300f * rim;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TerrainExtent * .68f, TerrainExtent * 1.3f, radius));
            return Mathf.Lerp(Height(x, z), far, blend);
        }

        private static float Mesa(float u, float v, float levels)
        {
            float noise = Mathf.PerlinNoise(u, v) * .72f + Mathf.PerlinNoise(u * 2.7f + 11f, v * 2.7f + 5f) * .28f;
            float scaled = noise * levels;
            float level = Mathf.Floor(scaled);
            return (level + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .86f, scaled - level))) / Mathf.Max(1f, levels);
        }

        private static float Ridged(float u, float v)
        {
            float noise = 1f - Mathf.Abs(Mathf.PerlinNoise(u, v) * 2f - 1f);
            return noise * noise;
        }

        public static float Slope(float x, float z)
        {
            const float probe = 8f;
            float dx = Surface(x + probe, z) - Surface(x - probe, z);
            float dz = Surface(x, z + probe) - Surface(x, z - probe);
            return Mathf.Sqrt(dx * dx + dz * dz) / (probe * 2f);
        }

        public static Vector3 Highest(float x, float z, float radius)
        {
            Vector3 best = new Vector3(x, Surface(x, z), z);
            for (int i = -6; i <= 6; i++)
                for (int j = -6; j <= 6; j++)
                {
                    float px = x + radius * i / 6f;
                    float pz = z + radius * j / 6f;
                    float py = Surface(px, pz);
                    if (py > best.y) best = new Vector3(px, py, pz);
                }
            return best;
        }

        private static float Bluff(float x, float z)
        {
            float result = 0f;
            for (int i = 0; i + 3 < Bluffs.Length; i += 4)
            {
                float dx = (x - Bluffs[i]) / Bluffs[i + 2];
                float dz = (z - Bluffs[i + 1]) / Bluffs[i + 2];
                float radius = dx * dx + dz * dz;
                if (radius >= 1f) continue;
                result = Mathf.Max(result, Bluffs[i + 3] * Mathf.Pow(1f - radius, 1.7f));
            }
            return result;
        }

        public static Mesh Terrain(int grid, float extent, float outer, int rings)
        {
            var mesh = new CrownRockMesh();
            var points = new Vector3[grid + 1, grid + 1];
            for (int z = 0; z <= grid; z++)
                for (int x = 0; x <= grid; x++)
                {
                    float px = Mathf.Lerp(-extent, extent, x / (float)grid);
                    float pz = Mathf.Lerp(-extent, extent, z / (float)grid);
                    points[x, z] = new Vector3(px, Surface(px, pz), pz);
                }
            for (int z = 0; z < grid; z++)
                for (int x = 0; x < grid; x++)
                {
                    Tri(mesh, points[x, z], points[x, z + 1], points[x + 1, z]);
                    Tri(mesh, points[x + 1, z], points[x, z + 1], points[x + 1, z + 1]);
                }
            float scale = outer / extent;
            for (int strip = 0; strip < 4; strip++)
            {
                for (int t = 0; t < grid; t++)
                {
                    float u0 = Mathf.Lerp(-extent, extent, t / (float)grid);
                    float u1 = Mathf.Lerp(-extent, extent, (t + 1) / (float)grid);
                    for (int r = 0; r < rings; r++)
                    {
                        float k0 = Mathf.Lerp(1f, scale, Mathf.Pow(r / (float)rings, 1.9f));
                        float k1 = Mathf.Lerp(1f, scale, Mathf.Pow((r + 1f) / rings, 1.9f));
                        Vector3 a = Frame(strip, u0, k0, extent);
                        Vector3 b = Frame(strip, u1, k0, extent);
                        Vector3 c = Frame(strip, u0, k1, extent);
                        Vector3 d = Frame(strip, u1, k1, extent);
                        Tri(mesh, a, b, c);
                        Tri(mesh, b, d, c);
                    }
                }
            }
            return Soften(mesh.Build("Каньон, пласты и дальние гряды"));
        }

        private static Vector3 Frame(int strip, float u, float k, float extent)
        {
            float x;
            float z;
            if (strip == 0) { x = u * k; z = -extent * k; }
            else if (strip == 1) { x = extent * k; z = u * k; }
            else if (strip == 2) { x = -u * k; z = extent * k; }
            else { x = -extent * k; z = -u * k; }
            return new Vector3(x, Surface(x, z), z);
        }

        public static Mesh Riverbed(int segments, int width, bool alongZ)
        {
            var mesh = new CrownRockMesh();
            for (int i = 0; i < segments; i++)
                for (int w = 0; w < width; w++)
                {
                    if (alongZ)
                    {
                        Tri(mesh, River(i, w, segments, width, true), River(i + 1, w, segments, width, true), River(i, w + 1, segments, width, true));
                        Tri(mesh, River(i + 1, w, segments, width, true), River(i + 1, w + 1, segments, width, true), River(i, w + 1, segments, width, true));
                    }
                    else
                    {
                        Tri(mesh, River(i, w, segments, width, false), River(i, w + 1, segments, width, false), River(i + 1, w, segments, width, false));
                        Tri(mesh, River(i + 1, w, segments, width, false), River(i, w + 1, segments, width, false), River(i + 1, w + 1, segments, width, false));
                    }
                }
            return mesh.Build("Пересохшее русло реки");
        }

        private static Vector3 River(int i, int w, int segments, int width, bool alongZ)
        {
            float t = i / (float)segments;
            float across = w / (float)width * 2f - 1f;
            float along = Mathf.Lerp(-RiverExtent, RiverExtent, t);
            float centre = alongZ
                ? ChannelX(along) + Mathf.Sin(along * .021f) * 5f + Mathf.Sin(along * .0075f) * 7f
                : ChannelZ(along) + Mathf.Sin(along * .021f + 2f) * 5f + Mathf.Sin(along * .0075f + 1f) * 7f;
            float span = 7f + Mathf.Sin(along * .03f + (alongZ ? 0f : 1f)) * 2.5f;
            float y = FloorY - GorgeDepth + 1.05f + (1f - across * across) * .95f
                + Mathf.Sin(along * .05f + across * 3f) * .4f;
            float side = centre + across * span;
            return alongZ ? new Vector3(side, y, along) : new Vector3(along, y, side);
        }

        public static Mesh Haze(int segments, float halfWidth, bool alongZ)
        {
            var mesh = new Mesh { name = "Пыльная дымка каньона" };
            var vertices = new Vector3[(segments + 1) * 3];
            var colors = new Color[(segments + 1) * 3];
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float along = Mathf.Lerp(-RiverExtent, RiverExtent, t);
                float centre = alongZ
                    ? ChannelX(along) + Mathf.Sin(along * .0091f) * 22f
                    : ChannelZ(along) + Mathf.Sin(along * .0091f + 2f) * 22f;
                float y = FloorY + 8f + Mathf.Sin(along * .0125f) * 5f;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .2f, t))
                    * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, .8f, t));
                for (int c = 0; c < 3; c++)
                {
                    float across = (c - 1f) * halfWidth * (1f + Mathf.Sin(along * .0073f) * .35f);
                    vertices[i * 3 + c] = alongZ
                        ? new Vector3(centre + across, y + (c == 1 ? 5f : 0f), along)
                        : new Vector3(along, y + (c == 1 ? 5f : 0f), centre + across);
                    colors[i * 3 + c] = new Color(1f, 1f, 1f, (c == 1 ? .5f : .1f) * fade);
                }
                if (i == segments) break;
                int a = i * 3;
                int b = (i + 1) * 3;
                for (int c = 0; c < 2; c++)
                {
                    triangles.Add(a + c); triangles.Add(b + c); triangles.Add(a + c + 1);
                    triangles.Add(a + c + 1); triangles.Add(b + c); triangles.Add(b + c + 1);
                }
            }
            mesh.SetVertices(new List<Vector3>(vertices));
            mesh.SetColors(new List<Color>(colors));
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Arch(float span, float height, float pier, float depth, float shoulder, float seed)
        {
            var mesh = new CrownRockMesh();
            const int arc = 24;
            const int legs = 5;
            const int radial = 10;
            float half = span * .5f;
            float spring = height * .46f;
            float rise = height - spring;
            float crest = height + shoulder;
            var inner = new List<Vector2>();
            var outer = new List<Vector2>();
            for (int leg = 0; leg < legs; leg++)
            {
                float y = spring * leg / legs;
                inner.Add(new Vector2(-half, y));
                outer.Add(new Vector2(-(half + pier) * Cornice(y, crest), y));
            }
            for (int i = 0; i <= arc; i++)
            {
                float p = i / (float)arc;
                float angle = Mathf.Lerp(Mathf.PI, 0f, p);
                float cx = Mathf.Cos(angle);
                float sy = Mathf.Sin(angle);
                float side = cx < 0f ? -1f : 1f;
                float jitterX = Mathf.PerlinNoise(p * 5.3f + 3.1f, seed * .7f) * .1f - .05f;
                float jitterY = Mathf.PerlinNoise(p * 7.1f + 11.3f, seed * .9f + 2f) * .11f - .055f;
                float lean = 1f + Mathf.Sin(p * 1.7f + seed) * .022f;
                float ix = side * Mathf.Pow(Mathf.Abs(cx), .5f) * half * (1f + jitterX) * lean;
                float iy = spring + Mathf.Pow(Mathf.Max(0f, sy), .74f) * rise * (1f + jitterY);
                float ox = side * Mathf.Pow(Mathf.Abs(cx), .58f) * (half + pier) * (1f + jitterX * .7f);
                float oy = spring + Mathf.Pow(Mathf.Max(0f, sy), .46f) * (crest - spring)
                    + Mathf.Sin(p * 2.4f + seed * .7f) * shoulder * .018f + jitterY * crest * .05f;
                inner.Add(new Vector2(ix, iy));
                outer.Add(new Vector2(ox * Cornice(oy, crest), Mathf.Max(1f, oy)));
            }
            for (int leg = legs - 1; leg >= 0; leg--)
            {
                float y = spring * leg / legs;
                inner.Add(new Vector2(half, y));
                outer.Add(new Vector2((half + pier) * Cornice(y, crest), y));
            }
            int count = inner.Count;
            float hz = depth * .5f;

            Vector3 Point(int index, float t, float z, out Color tint)
            {
                Vector2 a = inner[index];
                Vector2 b = outer[index];
                float fy = b.y / crest;
                float taper = Mathf.Lerp(1f, .58f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.35f, .95f, fy)));
                float pier = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .46f, fy));
                float rib = Mathf.Sin(t * 20f + seed * 2.1f) * 2.4f * pier * t;
                float grain = Mathf.Sin(z * .09f + t * 2.7f + seed + index * 1.13f) * .6f;
                float baseDark = Mathf.Lerp(.58f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, crest * .45f, b.y)));
                float vault = Mathf.Lerp(.68f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, 0f, t)));
                float depthLight = Mathf.Lerp(.34f, 0f, Mathf.InverseLerp(-hz, hz, z));
                tint = new Color(Mathf.Clamp01(Mathf.Min(baseDark, vault) + depthLight), 1f, Mathf.Clamp01(fy * 2f), 1f);
                return new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), (z + grain + rib) * taper);
            }

            for (int i = 0; i < count - 1; i++)
            {
                for (int layer = 0; layer < radial; layer++)
                {
                    float t0 = layer / (float)radial;
                    float t1 = (layer + 1f) / radial;
                    Vector3 p0 = Point(i, t0, -hz, out Color c0);
                    Vector3 p1 = Point(i, t1, -hz, out Color c1);
                    Vector3 p2 = Point(i + 1, t1, -hz, out Color c2);
                    Vector3 p3 = Point(i + 1, t0, -hz, out Color c3);
                    QuadC(mesh, p0, p1, p2, p3, c0, c1, c2, c3);
                    Vector3 q0 = Point(i, t0, hz, out Color b0);
                    Vector3 q1 = Point(i + 1, t0, hz, out Color b1);
                    Vector3 q2 = Point(i + 1, t1, hz, out Color b2);
                    Vector3 q3 = Point(i, t1, hz, out Color b3);
                    QuadC(mesh, q0, q1, q2, q3, b0, b1, b2, b3);
                }
                Vector3 s0 = Point(i, 0f, -hz, out Color d0);
                Vector3 s1 = Point(i + 1, 0f, -hz, out Color d1);
                Vector3 s2 = Point(i + 1, 0f, hz, out Color d2);
                Vector3 s3 = Point(i, 0f, hz, out Color d3);
                QuadC(mesh, s0, s1, s2, s3, d0, d1, d2, d3);
                s0 = Point(i + 1, 1f, -hz, out d0);
                s1 = Point(i, 1f, -hz, out d1);
                s2 = Point(i, 1f, hz, out d2);
                s3 = Point(i + 1, 1f, hz, out d3);
                QuadC(mesh, s0, s1, s2, s3, d0, d1, d2, d3);
            }
            Vector3 e0 = Point(0, 1f, -hz, out Color f0);
            Vector3 e1 = Point(0, 0f, -hz, out Color f1);
            Vector3 e2 = Point(0, 0f, hz, out Color f2);
            Vector3 e3 = Point(0, 1f, hz, out Color f3);
            QuadC(mesh, e0, e1, e2, e3, f0, f1, f2, f3);
            e0 = Point(count - 1, 0f, -hz, out f0);
            e1 = Point(count - 1, 1f, -hz, out f1);
            e2 = Point(count - 1, 1f, hz, out f2);
            e3 = Point(count - 1, 0f, hz, out f3);
            QuadC(mesh, e0, e1, e2, e3, f0, f1, f2, f3);
            return Soften(mesh.Build("Природная арка"));
        }

        private static float Cornice(float y, float crest)
        {
            float low = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(crest * .38f, crest * .33f, y));
            float mid = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(crest * .66f, crest * .62f, y));
            float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(crest * .88f, crest * .84f, y));
            return 1f + .17f * low + .07f * mid + .05f * high;
        }

        public static Mesh Terrace(float width, float depth, float top, float bottom, int levels, int detail)
        {
            var mesh = new CrownRockMesh();
            const int sides = 32;
            const int rings = 36;
            var points = new Vector3[rings, sides];
            float slope = 1.15f;
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float shape = 1f + Mathf.Sin(side * .87f + detail) * .075f + Mathf.Sin(side * 2.31f + detail * 1.7f) * .028f;
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    float stair = Mathf.Floor(Mathf.Min(t, .999f) * levels) / Mathf.Max(1f, levels);
                    float y = Mathf.Lerp(top, bottom, Mathf.Pow(t, .92f));
                    float radius = 1f + stair * slope + Mathf.Sin(ring * .9f + side * .6f + detail) * .02f;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * width * shape * radius,
                        y + (ring == 0 ? 0f : Mathf.Sin(ring * .9f + detail) * .8f),
                        Mathf.Sin(angle) * depth * shape * radius);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                TriC(mesh, new Vector3(0f, top, 0f), points[0, next], points[0, side], Bake(1f), Bake(1f), Bake(1f));
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Color ca = Bake(1f - ring / (float)(rings - 1));
                    Color cb = Bake(1f - (ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
                TriC(mesh, new Vector3(0f, bottom, 0f), points[rings - 1, side], points[rings - 1, next], Bake(0f), Bake(0f), Bake(0f));
            }
            return Soften(mesh.Build("Терраса песчаника"));
        }

        public static Mesh Tower(float radius, float height, int sides, int levels, float seed)
        {
            var mesh = new CrownRockMesh();
            const int rings = 13;
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float profile = 1f + .34f * Mathf.Pow(1f - t, 2.1f) - .26f * Mathf.Pow(t, 3f)
                    + Mathf.Floor(t * levels) / Mathf.Max(1f, levels) * .05f;
                for (int side = 0; side < sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    float shape = 1f + Mathf.Sin(side * 1.9f + seed) * .13f + Mathf.Sin(side * 4.3f + seed * 2.1f) * .055f;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius * profile * shape,
                        height * t + Mathf.Sin(side * 2.3f + ring * 1.1f + seed) * radius * .05f,
                        Mathf.Sin(angle) * radius * profile * shape);
                }
            }
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Color ca = Bake(ring / (float)(rings - 1));
                    Color cb = Bake((ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                TriC(mesh, new Vector3(0f, height, 0f), points[rings - 1, next], points[rings - 1, side], Bake(1f), Bake(1f), Bake(1f));
                TriC(mesh, Vector3.zero, points[0, side], points[0, next], Bake(0f), Bake(0f), Bake(0f));
            }
            return Soften(mesh.Build("Скальный шпиль"));
        }

        public static Mesh Slab(float halfLength, float halfThickness, float top, float bottom, int levels, float seed)
        {
            Color Shade(float t)
            {
                float ao = Mathf.Lerp(.86f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .55f, t)));
                return new Color(ao, 1f, Mathf.Clamp01(t * 2.4f), 1f);
            }
            var mesh = new CrownRockMesh();
            const int sides = 22;
            const int rings = 16;
            var points = new Vector3[rings, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle);
                float sz = Mathf.Sin(angle);
                float k = Mathf.Max(Mathf.Abs(cx) / Mathf.Max(1f, halfLength), Mathf.Abs(sz) / Mathf.Max(1f, halfThickness));
                float irregular = 1f + Mathf.Sin(side * 2.1f + seed) * .16f + Mathf.Sin(side * 5.3f + seed * 1.7f) * .08f
                    + Mathf.Sin(side * 11.7f + seed * 3.1f) * .035f;
                float ox = cx / k * irregular;
                float oz = sz / k * irregular;
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    float stair = Mathf.Floor(Mathf.Min(t, .999f) * levels) / Mathf.Max(1f, levels);
                    float y = Mathf.Lerp(top, bottom, Mathf.Pow(t, .95f));
                    float flare = 1f + stair * .26f + Mathf.Sin(ring * .7f + side * .3f + seed) * .018f;
                    points[ring, side] = new Vector3(ox * flare,
                        y + (ring == 0 ? Mathf.Sin(side * 1.3f + seed) * 4.2f : Mathf.Sin(ring * 1.1f + side * .4f + seed) * 2.4f),
                        oz * flare);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                TriC(mesh, new Vector3(0f, top, 0f), points[0, next], points[0, side], Shade(1f), Shade(1f), Shade(1f));
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Color ca = Shade(1f - ring / (float)(rings - 1));
                    Color cb = Shade(1f - (ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
                TriC(mesh, new Vector3(0f, bottom, 0f), points[rings - 1, side], points[rings - 1, next], Shade(0f), Shade(0f), Shade(0f));
            }
            return Soften(mesh.Build("Пласт песчаника"));
        }

        public static Mesh Butte(float radius, float height, int sides, float seed)
        {
            var mesh = new CrownRockMesh();
            const int rings = 10;
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float profile = t < .25f ? 1f + .3f * (.25f - t) * 2f : Mathf.Lerp(1.06f, .62f, Mathf.Pow((t - .25f) / .75f, 1.25f));
                float ledge = (1f - Mathf.Clamp01(t * 1.3f)) * Mathf.Floor(t * 3f) * .05f;
                for (int side = 0; side < sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    float shape = 1f + Mathf.Sin(side * 1.7f + seed) * .11f + Mathf.Sin(side * 3.9f + seed * 2.3f) * .055f
                        + Mathf.Sin(side * 9.1f + seed * 4.1f) * .022f;
                    float r = (profile + ledge) * shape;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius * r,
                        height * t + Mathf.Sin(side * 2.1f + ring * 1.3f + seed) * height * .015f,
                        Mathf.Sin(angle) * radius * r);
                }
            }
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Color ca = Bake(1f - ring / (float)(rings - 1));
                    Color cb = Bake(1f - (ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Color cap = Bake(1f);
                TriC(mesh, new Vector3(0f, height, 0f), points[rings - 1, next], points[rings - 1, side], cap, cap, cap);
                TriC(mesh, Vector3.zero, points[0, side], points[0, next], Bake(0f), Bake(0f), Bake(0f));
            }
            return Soften(mesh.Build("Столовая гора"));
        }

        public static Mesh DistantRock(int kind, int sides, float seed)
        {
            var mesh = new CrownRockMesh();
            int rings = kind == 1 ? 16 : 12;
            var points = new Vector3[rings, sides];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float top = Mathf.Clamp01(1f - t * 1.35f);
                float profile;
                float ledge;
                if (kind == 0)
                {
                    profile = 1.1f + .44f * Mathf.Pow(1f - t, 2.4f) - .16f * t;
                    ledge = top * Mathf.Floor(t * 4f) * .03f;
                }
                else if (kind == 1)
                {
                    profile = 1.06f - .5f * t;
                    ledge = top * Mathf.Floor(t * 6f) * .018f;
                }
                else
                {
                    profile = 1.05f + .2f * Mathf.Pow(1f - t, 3f) - .32f * t;
                    ledge = top * Mathf.Floor(t * 3f) * .04f;
                }
                for (int side = 0; side < sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    float shape = 1f + Mathf.Sin(side * 1.9f + seed) * .14f + Mathf.Sin(side * 4.3f + seed * 2.1f) * .07f
                        + Mathf.Sin(side * 8.1f + seed * 3.3f) * .035f + Mathf.Sin(side * 13.7f + seed * 5.1f) * .018f;
                    float radius = (profile + ledge) * shape;
                    float lift = t + Mathf.Sin(side * 2.3f + ring * 1.1f + seed) * .015f;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius, lift, Mathf.Sin(angle) * radius);
                }
            }
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Color ca = Bake(ring / (float)(rings - 1));
                    Color cb = Bake((ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                TriC(mesh, new Vector3(0f, 1f, 0f), points[rings - 1, next], points[rings - 1, side], Bake(1f), Bake(1f), Bake(1f));
                TriC(mesh, Vector3.zero, points[0, side], points[0, next], Bake(0f), Bake(0f), Bake(0f));
            }
            return Soften(mesh.Build("Столб пустынной башни"));
        }

        public static Mesh Range(int alongSegments, float seed, float from, float to)
        {
            var mesh = new CrownRockMesh();
            const int across = 14;
            bool snow = from > 0f;
            var points = new Vector3[alongSegments + 1, across + 1];
            for (int a = 0; a <= alongSegments; a++)
            {
                float u = a / (float)alongSegments;
                float crest = Crest(u, seed);
                float shift = snow
                    ? Mathf.Clamp((Mathf.PerlinNoise(u * 4.3f + seed * 2f, seed * .7f) - .5f) * (to - from) * .5f, -.3f, .3f)
                    : 0f;
                for (int c = 0; c <= across; c++)
                {
                    float v = Mathf.Lerp(from + shift, to + shift, c / (float)across);
                    v = Mathf.Clamp(v, 0f, 1f);
                    float ridge = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(v * 2f - 1f)), 1.05f)
                        * (1f + Mathf.Sin(v * 9f + seed) * .07f);
                    float detail = 1f + (Mathf.PerlinNoise(u * 9f + seed, v * 6f + seed * 1.7f) - .5f) * .34f;
                    float lift = snow ? 1.004f : 1f;
                    points[a, c] = new Vector3(u - .5f, crest * ridge * detail * lift, v - .5f);
                }
            }
            for (int a = 0; a < alongSegments; a++)
                for (int c = 0; c < across; c++)
                {
                    Tri(mesh, points[a, c], points[a, c + 1], points[a + 1, c]);
                    Tri(mesh, points[a + 1, c], points[a, c + 1], points[a + 1, c + 1]);
                }
            return mesh.Build(snow ? "Снежная линия хребта" : "Дальний хребет");
        }

        private static float Crest(float u, float seed)
        {
            float value = .5f
                + Mathf.Sin(u * 3.1f + seed * .7f) * .2f
                + Mathf.Sin(u * 7.2f + seed) * .14f
                + Mathf.Sin(u * 15.1f + seed * 2.1f) * .07f
                + (Mathf.PerlinNoise(u * 6f + seed, seed * .3f) - .5f) * .5f;
            value = Mathf.Clamp(value, .1f, 1.15f);
            float ends = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .18f, u)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, .82f, u));
            return value * Mathf.Lerp(.55f, 1f, ends);
        }

        public static Mesh Tree(float height, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            int roots = 4 + seed % 3;
            for (int root = 0; root < roots; root++)
            {
                float angle = root * 137f + seed * 31f;
                Vector3 outward = Quaternion.Euler(0f, angle, 0f) * new Vector3(1f, 0f, 0f);
                Vector3 start = new Vector3(0f, height * .26f, 0f);
                Vector3 mid = start + outward * height * .17f + Vector3.down * height * .1f;
                Vector3 end = mid + outward * height * .11f + Vector3.down * height * .16f;
                Tube(mesh, start, mid, height * .09f, height * .06f, 4, false);
                Tube(mesh, mid, end, height * .06f, height * .025f, 4, false);
            }
            Grow(mesh, new Vector3(0f, height * .15f, 0f), Vector3.up, height * .5f, height * .11f, 0, random);
            return mesh.Build("Сухое раздвоенное дерево");
        }

        private static void Grow(CrownRockMesh mesh, Vector3 origin, Vector3 direction, float length, float radius, int depth, System.Random random)
        {
            Vector3 point = origin;
            Vector3 dir = direction.normalized;
            const int joints = 3;
            for (int joint = 0; joint < joints; joint++)
            {
                float t = (joint + 1f) / joints;
                Vector3 bend = new Vector3(Range(random, -.55f, .55f), .22f, Range(random, -.55f, .55f));
                if (joint == 0 && depth == 0) bend *= .3f;
                Vector3 step = (dir + bend).normalized;
                Vector3 next = point + step * (length / joints);
                float end = radius * Mathf.Lerp(1f, .5f, t);
                Tube(mesh, point, next, radius, end, depth == 0 ? 6 : 5, joint == 0);
                point = next;
                dir = step;
                radius = end;
            }
            if (depth >= 2 || radius < .04f) return;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < .001f) side = Vector3.right;
            side.Normalize();
            int forks = depth == 0 ? 2 : (random.NextDouble() < .55 ? 2 : 1);
            for (int fork = 0; fork < forks; fork++)
            {
                float tilt = (depth == 0 ? 1f : Range(random, -1f, 1f)) * Range(random, 22f, 46f);
                Vector3 branchDir = Quaternion.AngleAxis(tilt * (fork == 0 ? 1f : -1f), side) * dir;
                branchDir = Quaternion.AngleAxis(Range(random, 0f, 360f), dir) * branchDir;
                Grow(mesh, point, branchDir, length * Range(random, .58f, .78f), radius * .72f, depth + 1, random);
            }
        }

        private static void Tube(CrownRockMesh mesh, Vector3 a, Vector3 b, float radiusA, float radiusB, int sides, bool cap)
        {
            Vector3 axis = b - a;
            if (axis.sqrMagnitude < .000001f) return;
            axis.Normalize();
            Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > .93f ? Vector3.forward : Vector3.up;
            Vector3 u = Vector3.Cross(axis, reference).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            var lower = new Vector3[sides];
            var upper = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float irregular = 1f + Mathf.Sin(side * 2.7f + radiusA * 11f) * .12f;
                Vector3 radial = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * irregular;
                lower[side] = a + radial * radiusA;
                upper[side] = b + radial * radiusB;
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Quad(mesh, lower[side], lower[next], upper[next], upper[side]);
            }
            if (cap)
                for (int side = 0; side < sides; side++)
                    Tri(mesh, a - axis * radiusA * .3f, lower[(side + 1) % sides], lower[side]);
        }

        public static Mesh Bush(float size, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            var blob = DecorMeshLibrary.OctahedronMesh();
            int count = 3 + seed % 2;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3(Range(random, -.6f, .6f), Range(random, .16f, .6f), Range(random, -.6f, .6f)) * size;
                Vector3 scale = new Vector3(Range(random, .5f, .88f), Range(random, .32f, .5f), Range(random, .5f, .88f)) * size;
                Stamp(mesh, blob, Matrix4x4.TRS(position, Quaternion.Euler(Range(random, -30f, 30f), Range(random, 0f, 360f), Range(random, -30f, 30f)), scale));
            }
            int twigs = 2 + seed % 2;
            for (int twig = 0; twig < twigs; twig++)
            {
                float angle = Range(random, 0f, 360f);
                Vector3 lean = Quaternion.Euler(0f, angle, 0f) * new Vector3(1f, 0f, 0f);
                Vector3 start = new Vector3(0f, size * .1f, 0f);
                Vector3 end = start + lean * size * .5f + Vector3.up * size * Range(random, .35f, .95f);
                Tube(mesh, start, end, size * .07f, size * .02f, 4, true);
            }
            return mesh.Build("Сухой куст");
        }

        public static Mesh Tuft(float size, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            var blade = DecorMeshLibrary.GrassBladeMesh();
            int count = 4 + seed % 4;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * 360f + seed * 17f;
                Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(Range(random, .05f, .42f), 0f, 0f) * size;
                Vector3 scale = new Vector3(size * Range(random, .3f, .5f), size * Range(random, .75f, 1.5f), size * Range(random, .3f, .5f));
                Stamp(mesh, blade, Matrix4x4.TRS(position, Quaternion.Euler(0f, Range(random, 0f, 360f), Range(random, -26f, 26f)), scale));
            }
            return mesh.Build("Пучок сухой травы");
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

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
            var source = mesh.triangles;
            var vertices = mesh.vertices;
            var triangles = new List<int>(source.Length);
            for (int i = 0; i < source.Length; i += 3)
            {
                Vector3 a = vertices[source[i]], b = vertices[source[i + 1]], c = vertices[source[i + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                float longest = Mathf.Max((b - a).sqrMagnitude, Mathf.Max((c - b).sqrMagnitude, (a - c).sqrMagnitude));
                if (cross.sqrMagnitude < 1e-7f || cross.sqrMagnitude < longest * 1.44f
                    || cross.sqrMagnitude < longest * 8e-3f) continue;
                triangles.Add(source[i]); triangles.Add(source[i + 1]); triangles.Add(source[i + 2]);
            }
            if (triangles.Count < source.Length)
            {
                mesh.SetTriangles(triangles, 0);
            }
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

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
            => mesh.Triangle(a, b, c, Open, Open, Open);

        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Tri(mesh, a, b, c); Tri(mesh, a, c, d); }

        private static void TriC(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            => mesh.Triangle(a, b, c, ca, cb, cc);

        private static void QuadC(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Color ca, Color cb, Color cc, Color cd)
        { TriC(mesh, a, b, c, ca, cb, cc); TriC(mesh, a, c, d, ca, cc, cd); }

        private static Color Bake(float t)
        {
            float ao = Mathf.Lerp(.62f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .55f, t)));
            return new Color(ao, 1f, Mathf.Clamp01(t * 2.4f), 1f);
        }
    }
}
