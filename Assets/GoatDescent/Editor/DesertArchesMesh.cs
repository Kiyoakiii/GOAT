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
        public const float GorgeDepth = 74f;
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
            float bluff = Bluff(x, z);
            float relief = Relief
                + (Mathf.PerlinNoise(x * .0017f + 13f, z * .0017f + 7f) - .5f) * 62f
                + (Mathf.PerlinNoise(x * .0055f + 41f, z * .0055f + 23f) - .5f) * 26f
                + bluff;
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
            step *= .52f + .96f * Mathf.PerlinNoise(x * .0107f + 37f, z * .0099f + 71f);
            step *= .68f + .78f * Mathf.PerlinNoise(x * .0061f + 131f, z * .0057f + 53f);
            float unit = (height - floor) / step
                + (Mathf.PerlinNoise(x * .0041f + 5f, z * .0037f + 9f) - .5f) * 1.15f
                + (Mathf.PerlinNoise(x * .0093f + 17f, z * .0087f + 3f) - .5f) * .45f
                + (Mathf.PerlinNoise(x * .0243f + 211f, z * .0227f + 137f) - .5f) * .95f;
            float level = Mathf.Floor(unit);
            float fraction = unit - level;
            float riser = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.16f, 1f, fraction));
            float terraced = floor + (level + riser) * step;
            float wall = carve * (1f - carve) * 4f;
            float reach = Mathf.Sqrt(x * x + z * z);
            float detail = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(560f, 800f, reach));
            const float bluffProbe = 14f;
            float bluffX = Bluff(x + bluffProbe, z) - Bluff(x - bluffProbe, z);
            float bluffZ = Bluff(x, z + bluffProbe) - Bluff(x, z - bluffProbe);
            float bluffSlope = Mathf.Sqrt(bluffX * bluffX + bluffZ * bluffZ) / (bluffProbe * 2f);
            float steep = Mathf.Clamp01(Mathf.Max(wall, bluffSlope * 2.4f));
            float ledgeNoise = Mathf.PerlinNoise(x * .0028f + 57f, z * .0026f + 31f);
            float ledge = Mathf.Max(upland * (.05f + .1f * ledgeNoise),
                steep * (.32f + .24f * ledgeNoise)) * detail;
            height = Mathf.Lerp(height, terraced, Mathf.Clamp01(ledge));
            height += (Mathf.PerlinNoise(x * .0075f + 31f, z * .0075f + 17f) - .5f) * 11f * wall * detail;
            float main = Mathf.Abs(x - ChannelX(z));
            float branch = Mathf.Abs(z - ChannelZ(x));
            float axisFlow = main <= branch ? z : x;
            float axisDrop = main <= branch ? x : z;
            float gradientLength = Mathf.Sqrt(bluffX * bluffX + bluffZ * bluffZ);
            float contourFlow = (-x * bluffZ + z * bluffX) / Mathf.Max(1f, gradientLength);
            float contourDrop = (x * bluffX + z * bluffZ) / Mathf.Max(1f, gradientLength);
            float across = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.6f, 4f, gradientLength));
            float flow = Mathf.Lerp(axisFlow, contourFlow, across);
            float drop = Mathf.Lerp(axisDrop, contourDrop, across);
            float meander = (Mathf.PerlinNoise(flow * .0071f + 23f, drop * .0037f + 61f) - .5f) * 26f
                + (Mathf.PerlinNoise(flow * .019f + 7f, drop * .011f + 31f) - .5f) * 9f;
            float rib = Mathf.Sin(flow * .235f + meander);
            float groove = Mathf.Pow(1f - Mathf.Abs(rib), 5f);
            float fineMeander = (Mathf.PerlinNoise(flow * .013f + 97f, drop * .008f + 19f) - .5f) * 14f;
            float fineRib = Mathf.Sin(flow * .061f + fineMeander);
            float fineGroove = Mathf.Pow(1f - Mathf.Abs(fineRib), 4f);
            float erosion = (groove * (2.6f + 2.4f * Mathf.PerlinNoise(flow * .013f + 97f, drop * .009f + 19f))
                + fineGroove * (1.1f + 1f * Mathf.PerlinNoise(flow * .031f + 43f, drop * .023f + 7f)))
                * steep * detail * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 55f, distance));
            height -= erosion;
            float ripple = (Mathf.PerlinNoise(x * .0173f + 131f, z * .0161f + 29f) - .5f) * 3.4f
                + (Mathf.PerlinNoise(x * .0413f + 67f, z * .0391f + 13f) - .5f) * 1.5f
                + (Mathf.PerlinNoise(x * .117f + 211f, z * .109f + 37f) - .5f) * .65f;
            height += ripple * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(40f, 170f, distance))
                * detail * (1f - steep * .55f);
            float playa = Mathf.Clamp01(1f - Mathf.Abs(carve - .64f) / .17f);
            if (playa > .01f)
            {
                float warp = Mathf.PerlinNoise(x * .0039f + 61f, z * .0036f + 13f) * 7.5f
                    + (Mathf.PerlinNoise(x * .0113f + 43f, z * .0097f + 83f) - .5f) * 3.4f;
                float warpB = Mathf.PerlinNoise(x * .0067f + 19f, z * .0063f + 89f) * 9f
                    + (Mathf.PerlinNoise(x * .0171f + 7f, z * .0151f + 53f) - .5f) * 3.8f;
                float dune = Mathf.Sin(x * .0269f + z * .0101f + warp) * 2.2f;
                dune += Mathf.Sin(x * .057f - z * .039f + warpB
                    + (Mathf.PerlinNoise(x * .0131f + 61f, z * .0117f + 5f) - .5f) * 9f) * .9f;
                dune += (Mathf.PerlinNoise(x * .0167f + 17f, z * .0151f + 79f) - .5f) * 4.6f;
                dune += (Mathf.PerlinNoise(x * .047f + 97f, z * .043f + 23f) - .5f) * 1.7f;
                height += dune * playa * detail;
                height -= (1f - Cracks(x, z, .03f)) * 5f * playa * detail;
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
            float duneWarp = Mathf.PerlinNoise(x * .0023f + 43f, z * .0021f + 91f) * 13f
                + (Mathf.PerlinNoise(x * .0067f + 17f, z * .0059f + 7f) - .5f) * 6.5f;
            float duneWarpB = Mathf.PerlinNoise(x * .0041f + 29f, z * .0037f + 3f) * 15f
                + (Mathf.PerlinNoise(x * .0097f + 61f, z * .0083f + 23f) - .5f) * 7f;
            float dunePatch = .5f + .85f * Mathf.PerlinNoise(x * .0017f + 113f, z * .0016f + 47f);
            float dunePhase = x * .0094f + z * .0041f + duneWarp
                + (Mathf.PerlinNoise(x * .0031f + 5f, z * .0029f + 77f) - .5f) * 22f;
            float dunePhaseB = x * .0217f - z * .0151f + duneWarpB
                + (Mathf.PerlinNoise(x * .0083f + 131f, z * .0071f + 19f) - .5f) * 26f;
            float dunes = (Mathf.Sin(dunePhase) * .82f
                + Mathf.Sin(dunePhaseB) * .3f
                + (Mathf.PerlinNoise(x * .0057f + 71f, z * .0049f + 13f) - .5f) * 2.1f)
                * dunePatch
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(680f, 1180f, radius));
            far += dunes * (1f - rim * .65f);
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TerrainExtent * .68f, TerrainExtent * 1.3f, radius));
            float farDetail = ((Mathf.PerlinNoise(x * .0121f + 83f, z * .0113f + 59f) - .5f) * 1.9f
                + (Mathf.PerlinNoise(x * .0297f + 41f, z * .0277f + 97f) - .5f) * .85f)
                * (1f - blend) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(520f, 700f, radius));
            far += farDetail;
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
                        float k0 = Mathf.Lerp(1f, scale, Mathf.Pow(r / (float)rings, 1.55f));
                        float k1 = Mathf.Lerp(1f, scale, Mathf.Pow((r + 1f) / rings, 1.55f));
                        Vector3 a = Frame(strip, u0, k0, extent);
                        Vector3 b = Frame(strip, u1, k0, extent);
                        Vector3 c = Frame(strip, u0, k1, extent);
                        Vector3 d = Frame(strip, u1, k1, extent);
                        Tri(mesh, a, b, c);
                        Tri(mesh, b, d, c);
                    }
                }
            }
            return Soften(mesh.Build("Каньон, пласты и дальние гряды"), .84f);
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

        private static float Lobe(float theta, float phi, float sharp)
        {
            float delta = Mathf.Repeat(theta - phi + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
            return Mathf.Pow(Mathf.Max(0f, Mathf.Cos(delta)), sharp);
        }

        private static float Arc(float theta, float frequency, float offset)
        {
            return Mathf.PerlinNoise(Mathf.Cos(theta) * frequency + offset,
                Mathf.Sin(theta) * frequency + offset * 1.618f + 23.7f) - .5f;
        }

        public static Mesh Terrace(float width, float depth, float top, float bottom, int levels, int detail)
        {
            var mesh = new CrownRockMesh();
            const int sides = 48;
            const int rings = 30;
            float phi0 = Mathf.Repeat(detail * .731f, Mathf.PI * 2f);
            float phi1 = Mathf.Repeat(phi0 + 2.35f + Mathf.Sin(detail * 2.3f) * .75f, Mathf.PI * 2f);
            float phi2 = Mathf.Repeat(phi0 - 2.1f + Mathf.Sin(detail * 4.1f) * .85f, Mathf.PI * 2f);
            float r1 = .71f + Mathf.Sin(detail * 3.7f) * .08f;
            float r2 = .49f + Mathf.Sin(detail * 5.9f) * .07f;
            float squishX = 1f + Mathf.Sin(detail * 1.7f) * .14f;
            float squishZ = 1f + Mathf.Sin(detail * 4.3f + 1.2f) * .13f;
            float drift = Mathf.Sin(detail * 2.9f) * .09f;
            float slope = .82f + Mathf.Sin(detail * .9f) * .16f;
            float Outline(float theta)
            {
                float w0 = Lobe(theta, phi0, 1.45f + Mathf.Sin(detail * 2.1f) * .35f);
                float w1 = Lobe(theta, phi1, 2.1f + Mathf.Sin(detail * 5.3f) * .5f);
                float w2 = Lobe(theta, phi2, 2.8f + Mathf.Sin(detail * 7.1f) * .55f);
                float sum = w0 + w1 + w2;
                float lobes = sum < .015f ? r2 : (w0 + w1 * r1 + w2 * r2) / sum;
                float edge = Arc(theta, 1.9f, detail) * .17f
                    + Arc(theta, 5.3f, detail * 1.7f + 7f) * .085f
                    + Arc(theta, 11.7f, detail * 2.9f + 13f) * .038f;
                return Mathf.Max(.28f, lobes + edge);
            }
            var points = new Vector3[rings, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle);
                float sz = Mathf.Sin(angle);
                float outline = Outline(angle);
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    float stair = Mathf.Floor(Mathf.Min(t, .999f) * levels) / Mathf.Max(1f, levels);
                    float y = Mathf.Lerp(top, bottom, Mathf.Pow(t, .94f));
                    float radius = 1f + stair * slope + Mathf.SmoothStep(.72f, 1f, t) * .2f
                        + Mathf.Sin(ring * .9f + side * .6f + detail) * .022f
                        + Arc(angle + t * .8f, 4.7f, detail + ring * .37f) * .05f * t;
                    float lean = drift * t;
                    points[ring, side] = new Vector3(
                        (cx * outline * squishX + lean) * width * radius,
                        y + (ring == 0 ? 0f : Mathf.Sin(ring * .9f + detail) * 1.1f * t),
                        (sz * outline * squishZ - lean * .6f) * depth * radius);
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
                float ao = Mathf.Lerp(.62f, .90f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .55f, t)))
                    - .22f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.82f, 1f, t));
                return new Color(ao, 1f, Mathf.Clamp01(t * 2.4f), 1f);
            }
            var mesh = new CrownRockMesh();
            float perimeter = Mathf.PI * (3f * (halfLength + halfThickness)
                - Mathf.Sqrt((3f * halfLength + halfThickness) * (halfLength + 3f * halfThickness)));
            int sides = Mathf.Clamp(Mathf.RoundToInt(perimeter / 8f), 120, 384);
            float loop = Mathf.Max(1f, perimeter / (Mathf.PI * 2f));
            float size = Mathf.Clamp(halfLength / 470f, .5f, 1.1f);
            float Loop(float theta, float period, float offset)
            {
                float radius = loop / Mathf.Max(1f, period);
                return Mathf.PerlinNoise(Mathf.Cos(theta) * radius + offset,
                    Mathf.Sin(theta) * radius + offset * 1.618f + 23.7f) - .5f;
            }
            float phi0 = Mathf.Repeat(seed * .917f, Mathf.PI * 2f);
            float phi1 = Mathf.Repeat(phi0 + Mathf.PI + Mathf.Sin(seed * 2.6f) * 1.1f, Mathf.PI * 2f);
            float phi2 = Mathf.Repeat(phi0 + Mathf.Sin(seed * 4.4f) * 2.3f, Mathf.PI * 2f);
            float r0 = 1f;
            float r1 = .74f + Mathf.Sin(seed * 3.1f) * .07f;
            float r2 = .52f + Mathf.Sin(seed * 5.3f) * .08f;
            float Outline(float theta)
            {
                float w0 = Lobe(theta, phi0, 1.35f + Mathf.Sin(seed * 1.9f) * .4f);
                float w1 = Lobe(theta, phi1, 1.9f + Mathf.Sin(seed * 6.7f) * .5f);
                float w2 = Lobe(theta, phi2, 2.6f + Mathf.Sin(seed * 7.9f) * .6f);
                float sum = w0 + w1 + w2;
                float lobes = sum < .015f ? r2 : (w0 * r0 + w1 * r1 + w2 * r2) / sum;
                float edge = Loop(theta, 126f, seed * 1.7f) * .072f
                    + Loop(theta, 57f, seed * 2.9f + 7f) * .034f
                    + Loop(theta, 26f, seed * 4.3f + 13f) * .015f;
                return Mathf.Max(.34f, lobes + edge);
            }
            var bands = new List<float>();
            int strata = Mathf.Max(1, levels);
            for (int band = 0; band < strata; band++)
            {
                bands.Add((band + .012f) / strata);
                bands.Add((band + .46f) / strata);
                bands.Add((band + .79f) / strata);
                bands.Add((band + .997f) / strata);
            }
            bands.Add(1f);
            var points = new Vector3[bands.Count, sides];
            var crest = new float[sides];
            var bulge = new float[sides];
            var axisX = new float[sides];
            var axisZ = new float[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle);
                float sz = Mathf.Sin(angle);
                float wave = (Loop(angle, 96f, seed * .71f) * 20f
                    + Loop(angle, 48f, seed * .43f + 11f) * 14f
                    + Loop(angle, 24f, seed * .29f + 23f) * 7f) * size;
                float sag = Loop(angle, 200f, seed * .61f + 41f);
                float collapse = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .31f, sag));
                crest[side] = wave - collapse * (20f + 16f * Mathf.Clamp01(sag * 2f)) * size;
                bulge[side] = Loop(angle, 154f, seed * .91f + 61f) * 5.2f
                    + Loop(angle, 73f, seed * .37f + 17f) * 2.6f
                    + Loop(angle, 35f, seed * .53f + 29f) * 1.1f;
                float nx = cx / halfLength;
                float nz = sz / halfThickness;
                float norm = 1f / Mathf.Max(.0001f, Mathf.Sqrt(nx * nx + nz * nz));
                axisX[side] = nx * norm;
                axisZ[side] = nz * norm;
            }
            float bandHeight = (top - bottom) / Mathf.Max(1f, levels);
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle);
                float sz = Mathf.Sin(angle);
                float at = side * 96f / sides;
                float shape = Outline(angle);
                float ox = cx * halfLength;
                float oz = sz * halfThickness;
                float sway = .62f + .38f * Mathf.Sin(angle * 2.7f + seed * 1.3f);
                float shelfWarp = (Mathf.PerlinNoise(at * .043f + seed * 1.7f, seed * .37f) - .5f) * .4f
                    + (Mathf.PerlinNoise(at * .011f + seed * .31f, seed * .73f) - .5f) * .26f;
                float crackPhase = at * .31f + seed * 2.3f;
                float finePhase = at * .63f + seed * 1.1f;
                float coarseWidth = Mathf.Lerp(3.8f, 1.7f,
                    Mathf.InverseLerp(.5f, 1.7f, .5f + 1.2f * Mathf.PerlinNoise(at * .023f + seed * .61f, seed * .17f)));
                float fineWidth = Mathf.Lerp(4.8f, 2.2f,
                    Mathf.InverseLerp(.5f, 1.7f, .5f + 1.2f * Mathf.PerlinNoise(at * .049f + seed * 1.31f, seed * .43f)));
                for (int ring = 0; ring < bands.Count; ring++)
                {
                    float t = bands[ring];
                    float y = Mathf.Lerp(top, bottom, Mathf.Pow(t, .95f));
                    float layerPosition = t * levels;
                    float layer = Mathf.Floor(layerPosition);
                    float portion = layerPosition - layer;
                    float lipT = Mathf.SmoothStep(.6f, .88f, portion);
                    float thick = .6f + .8f * Mathf.PerlinNoise(layer * .83f + seed * .51f, seed * .27f);
                    float stepOut = (lipT * .018f + layer * .0032f) * thick;
                    float rate = Mathf.Pow(2f,
                        (Mathf.PerlinNoise(at * .021f + seed * .41f, t * .63f + seed * .53f) - .5f) * 2f);
                    float wander = (Mathf.PerlinNoise(at * .019f + seed * 1.13f, t * .9f + seed * .7f) - .5f) * .7f;
                    float crack = 1f - Mathf.Abs(Mathf.PerlinNoise(crackPhase * rate + wander,
                        t * 2.1f + seed * .9f) * 2f - 1f);
                    float fineCrack = 1f - Mathf.Abs(Mathf.PerlinNoise(finePhase * rate + wander * 1.6f,
                        t * 4.7f + seed * .7f) * 2f - 1f);
                    float fade = Mathf.SmoothStep(.3f, .74f,
                        Mathf.PerlinNoise(at * .0125f + seed * .71f, t * .52f + seed * .31f));
                    float carve = (Mathf.Pow(crack, coarseWidth) * .0066f + Mathf.Pow(fineCrack, fineWidth) * .0027f)
                        * (.12f + .88f * fade);
                    float breakUp = (Mathf.PerlinNoise(at * .083f + seed * 2.7f, t * 2.9f + seed * .9f) - .5f) * .009f;
                    float drift = (Mathf.PerlinNoise(at * .014f + seed * .83f, t * 1.6f + seed * .47f) - .5f) * 1.1f;
                    float shapeT = shape * (1f + (shape - 1f) * drift);
                    float flare = 1f + stepOut + Mathf.SmoothStep(.62f, 1f, t) * .38f
                        + Mathf.Sin(ring * .7f + at * .3f + seed) * .007f + breakUp - carve;
                    float shelfLift = (shelfWarp
                        + (Mathf.PerlinNoise(at * .051f + seed * .21f, t * 2.3f + seed * .47f) - .5f) * .3f)
                        * bandHeight * .2f;
                    float crestWeight = 1f - Mathf.SmoothStep(0f, .34f, t);
                    float facade = bulge[side] * (.58f + .42f * Mathf.Sin(t * 3.4f + seed * .7f)) * sway;
                    float reach = shapeT * flare;
                    points[ring, side] = new Vector3(ox * reach + axisX[side] * facade,
                        y + shelfLift + crest[side] * crestWeight + (ring == 0
                            ? (Loop(angle, 42f, seed * .7f) - .5f) * 2.4f
                            : Mathf.Sin(ring * 1.1f + at * .4f + seed) * .8f),
                        oz * reach + axisZ[side] * facade);
                }
            }
            var lip = new Vector3[sides];
            var inner = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float at = side * 96f / sides;
                float bevel = 6.5f + 4.5f * Mathf.PerlinNoise(at * .07f + seed * 1.3f, seed * .79f);
                Vector3 outward = new Vector3(axisX[side], 0f, axisZ[side]);
                lip[side] = points[0, side] - outward * (bevel * .55f)
                    + Vector3.down * (1.6f + 1.4f * Mathf.PerlinNoise(at * .05f + seed * .53f, seed * 1.7f));
                inner[side] = points[0, side] - outward * (bevel * 1.55f)
                    + Vector3.down * (5.5f + 3f * Mathf.PerlinNoise(at * .04f + seed * .37f, seed * 2.3f));
            }
            float capY = 0f;
            for (int side = 0; side < sides; side++) capY += inner[side].y / sides;
            Vector3 crown = new Vector3(Mathf.Sin(seed * 2.2f) * halfLength * .12f, capY - 6f,
                Mathf.Cos(seed * 3.4f) * halfThickness * .18f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Color cap = new Color(.62f, .68f, .05f, 1f);
                for (int ring = 0; ring < bands.Count - 1; ring++)
                {
                    Color ca = Shade(1f - bands[ring]);
                    Color cb = Shade(1f - bands[ring + 1]);
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
                TriC(mesh, new Vector3(0f, bottom, 0f), points[bands.Count - 1, side], points[bands.Count - 1, next], Shade(0f), Shade(0f), Shade(0f));
                QuadC(mesh, points[0, side], lip[side], lip[next], points[0, next], cap, cap, cap, cap);
                QuadC(mesh, lip[side], inner[side], inner[next], lip[next], cap, cap, cap, cap);
                TriC(mesh, crown, inner[next], inner[side], cap, cap, cap);
            }
            return Soften(mesh.Build("Пласт песчаника"), .42f, .9f);
        }

        public static Mesh Boulder(float radius, float squash, float seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 28;
            const int rings = 9;
            float phi0 = Mathf.Repeat(seed * .811f, Mathf.PI * 2f);
            float phi1 = Mathf.Repeat(phi0 + 2.1f + Mathf.Sin(seed * 3.3f) * .8f, Mathf.PI * 2f);
            float phi2 = Mathf.Repeat(phi0 - 2.4f + Mathf.Sin(seed * 5.7f) * .7f, Mathf.PI * 2f);
            float r0 = .95f + Mathf.Sin(seed * 1.7f) * .06f;
            float r1 = .68f + Mathf.Sin(seed * 3.9f) * .09f;
            float r2 = .47f + Mathf.Sin(seed * 5.1f) * .08f;
            float zSquash = .82f + Mathf.Sin(seed * 4.1f) * .14f;
            float height = radius * Mathf.Max(.24f, squash);
            float twistBase = Mathf.Sin(seed * 7.3f) * .55f;
            float Outline(float theta)
            {
                float w0 = Lobe(theta, phi0, 1.8f + Mathf.Sin(seed * 2.1f) * .5f);
                float w1 = Lobe(theta, phi1, 2.4f + Mathf.Sin(seed * 4.7f) * .6f);
                float w2 = Lobe(theta, phi2, 3f + Mathf.Sin(seed * 6.3f) * .7f);
                float sum = w0 + w1 + w2;
                float lobes = sum < .015f ? r2 : (w0 * r0 + w1 * r1 + w2 * r2) / sum;
                float edge = Arc(theta, 2.2f, seed) * .19f
                    + Arc(theta, 5.6f, seed * 1.9f + 7f) * .1f
                    + Arc(theta, 12.1f, seed * 3.1f + 13f) * .045f;
                return Mathf.Max(.26f, lobes + edge);
            }
            var points = new Vector3[rings, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float rim = Mathf.Sin(side * 1.9f + seed * 2.7f) * .05f + Arc(angle, 6.7f, seed + 5f) * .07f;
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    float shape = Outline(angle + (t - .35f) * twistBase);
                    float bulge = 1f + Mathf.Sin(Mathf.Clamp01(t * 1.12f) * Mathf.PI) * .22f;
                    float skirt = 1f + Mathf.SmoothStep(.5f, 1f, t) * .48f;
                    float span = radius * shape * bulge * skirt;
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * span,
                        -height * Mathf.Pow(t, .84f) + (ring == 0
                            ? rim * radius
                            : Mathf.Sin(ring * 1.3f + side * .6f + seed) * height * .04f),
                        Mathf.Sin(angle) * span * zSquash);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                TriC(mesh, new Vector3(Mathf.Cos(phi0) * radius * .22f, radius * .2f,
                    Mathf.Sin(phi0) * radius * .22f * zSquash), points[0, next], points[0, side], Bake(1f), Bake(1f), Bake(1f));
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Color ca = Bake(1f - ring / (float)(rings - 1));
                    Color cb = Bake(1f - (ring + 1f) / (rings - 1));
                    QuadC(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next], ca, cb, cb, ca);
                }
                TriC(mesh, new Vector3(0f, -height, 0f), points[rings - 1, side], points[rings - 1, next], Bake(0f), Bake(0f), Bake(0f));
            }
            return Soften(mesh.Build("Органичный валун"));
        }

        public static Mesh StepPad(float radius, float depth, int sides, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            var top = new Vector3[sides];
            var bevel = new Vector3[sides];
            var foot = new Vector3[sides];
            Color cap = new Color(1f, 1f, 1f, 1f);
            Color wall = new Color(.72f, .84f, .96f, 1f);
            Color baseColor = new Color(.5f, .7f, .9f, 1f);
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float wobble = 1f + Mathf.Sin(angle * 3.1f + seed * .7f) * .07f + Range(random, -.045f, .045f);
                float x = Mathf.Cos(angle) * radius * wobble;
                float z = Mathf.Sin(angle) * depth * wobble;
                float lip = Mathf.Sin(angle * 2.3f + seed) * .014f;
                top[side] = new Vector3(x, lip, z);
                bevel[side] = new Vector3(x * 1.05f, -.24f - lip, z * 1.07f);
                foot[side] = new Vector3(x * .74f, -1f, z * .76f);
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                mesh.Triangle(new Vector3(0f, .07f, 0f), top[side], top[next], cap, cap, cap);
                mesh.Triangle(top[side], top[next], bevel[next], cap, wall, wall);
                mesh.Triangle(top[side], bevel[next], bevel[side], cap, wall, wall);
                mesh.Triangle(bevel[side], bevel[next], foot[next], wall, baseColor, baseColor);
                mesh.Triangle(bevel[side], foot[next], foot[side], wall, baseColor, baseColor);
                mesh.Triangle(foot[side], foot[next], new Vector3(0f, -1.08f, 0f), baseColor, baseColor, baseColor);
            }
            return mesh.Build("Плоская ступень тропы");
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
            int crowns = 2 + seed % 2;
            for (int crown = 0; crown < crowns; crown++)
            {
                Vector3 centre = new Vector3(Range(random, -.36f, .36f) * size,
                    Range(random, -.03f, .03f) * size, Range(random, -.36f, .36f) * size);
                Crown(mesh, centre, size * Range(random, .36f, .52f), size * Range(random, .44f, .72f),
                    9 + crown * 2 + seed % 3, random);
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

        private static void Crown(CrownRockMesh mesh, Vector3 centre, float radius, float height, int sides, System.Random random)
        {
            const int rings = 4;
            var points = new Vector3[rings, sides];
            var wobble = new float[sides];
            var skew = new float[sides];
            for (int side = 0; side < sides; side++)
            {
                wobble[side] = Range(random, .84f, 1.2f);
                skew[side] = Range(random, .82f, 1.15f);
            }
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float lift = Mathf.Sin(t * Mathf.PI * .5f);
                for (int side = 0; side < sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    float radial = radius * Mathf.Cos(t * Mathf.PI * .5f) * wobble[side];
                    float y = centre.y + height * lift - (ring == 0 ? height * .14f : 0f)
                        + (ring == 0 ? 0f : (Mathf.PerlinNoise(angle * 1.9f + sides, t * 4.3f + sides * .37f) - .5f) * height * .16f);
                    points[ring, side] = new Vector3(centre.x + Mathf.Cos(angle) * radial,
                        y, centre.z + Mathf.Sin(angle) * radial * skew[side]);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    Tri(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next]);
                    Tri(mesh, points[ring, side], points[ring + 1, next], points[ring, next]);
                }
                Tri(mesh, new Vector3(centre.x, centre.y + height, centre.z), points[rings - 1, next], points[rings - 1, side]);
            }
        }

        public static Mesh Tuft(float size, int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            int count = 4 + seed % 2;
            for (int blade = 0; blade < count; blade++)
            {
                float angle = blade / (float)count * 360f + Range(random, -21f, 21f) + seed * 17f;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                Vector3 root = radial * size * Range(random, .02f, .26f);
                Ribbon(mesh, root, radial, size * Range(random, .16f, .46f), size * Range(random, .6f, 1.2f),
                    size * Range(random, .055f, .095f), Range(random, -14f, 14f));
            }
            return mesh.Build("Пучок сухой травы");
        }

        private static void Ribbon(CrownRockMesh mesh, Vector3 root, Vector3 lean, float reach, float height, float width, float twist)
        {
            const int segments = 4;
            Vector3 side = Vector3.Cross(Vector3.up, lean).normalized;
            if (side.sqrMagnitude < .0001f) side = Vector3.right;
            Quaternion roll = Quaternion.AngleAxis(twist, Vector3.up);
            side = roll * side;
            var left = new Vector3[segments + 1];
            var right = new Vector3[segments + 1];
            for (int segment = 0; segment <= segments; segment++)
            {
                float t = segment / (float)segments;
                Vector3 centre = root + Vector3.up * (height * t) + lean * (reach * t * t)
                    + side * (Mathf.Sin(t * Mathf.PI) * width * .4f);
                float taper = Mathf.Lerp(1f, .12f, t);
                left[segment] = centre - side * (width * taper * .5f);
                right[segment] = centre + side * (width * taper * .5f);
            }
            for (int segment = 0; segment < segments; segment++)
            {
                int next = segment + 1;
                Tri(mesh, left[segment], left[next], right[next]);
                Tri(mesh, left[segment], right[next], right[segment]);
                Tri(mesh, left[next], left[segment], right[segment]);
                Tri(mesh, right[next], left[segment], right[segment]);
            }
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

        private static Mesh Soften(Mesh mesh, float threshold = .91f, float blend = .78f)
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
                        if (Vector3.Dot(normals[i], normals[j]) > threshold) average += normals[j];
                    result[i] = Vector3.Lerp(normals[i], average.normalized, blend).normalized;
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
