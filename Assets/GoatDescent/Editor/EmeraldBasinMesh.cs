using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    internal static class EmeraldBasinMesh
    {
        private static readonly Color Surface = new Color(0f, 0f, 0f, 1f);
        public const float LakeLevel = 0f;
        public const float LakeRadius = 290f;
        public static readonly float[] ShelfLevel = { 24f, 215f, 405f, 580f };
        public static readonly float[] InnerRadius = { 0f, 380f, 430f, 480f };
        public static readonly float[] OuterRadius = { 0f, 430f, 480f, 530f };
        public static readonly float[] FallAngles = { .10f, .55f, 1.80f, 3.90f, 4.60f };
        public static readonly float[] FallWidths = { 74f, 48f, 58f, 42f, 66f };
        public static readonly int[] FallTops = { 3, 3, 2, 2, 3 };
        public static readonly int[] FallBottoms = { 3, 1, 2, 1, 1 };
        public static readonly float[] IslandAngles = { 0f, 2.35f, 4.55f };
        public static readonly float[] IslandOffsets = { 0f, 130f, 205f };
        public static readonly float[] IslandSizes = { 140f, 75f, 60f };
        public static readonly float[] IslandTops = { 10.5f, 6.5f, 5.2f };
        public const float GullyRecess = 34f;
        private static readonly float[,] CollapseCentres =
        {
            { 0f, 0f, 0f },
            { .55f, 2.95f, -99f },
            { 1.45f, 4.05f, -99f },
            { .35f, 2.25f, 5.15f },
            { 1.75f, 4.55f, -99f }
        };
        private const int Sides = 192;

        public static float RingScale(float angle) => 1f + Ring(angle, 17.3f, 1.6f) * .23f;

        public static float TierInner(int tier, float angle) => InnerRadius[tier] * RingScale(angle);

        public static float TierOuter(int tier, float angle) => OuterRadius[tier] * RingScale(angle);

        public static float CollapseMask(int tier, float angle)
        {
            float collapse = 0f;
            for (int index = 0; index < 3; index++)
            {
                float centre = CollapseCentres[tier, index];
                float distance = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, centre * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                collapse = Mathf.Max(collapse, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.30f, .13f, distance)));
            }
            return collapse;
        }

        public static float[] CollapseAngles(int tier)
        {
            var angles = new List<float>();
            for (int index = 0; index < 3; index++)
                if (CollapseCentres[tier, index] > -50f) angles.Add(CollapseCentres[tier, index]);
            return angles.ToArray();
        }

        private sealed class PolarGrid
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            public int Add(Vector3 point)
            {
                vertices.Add(point);
                return vertices.Count - 1;
            }

            public void Quad(int a, int b, int c, int d)
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Mesh PolarTerrain(string name, int sides, int rings, float innerRadius, float outerRadius, float power,
            System.Func<float, float, float> height)
        {
            var grid = new PolarGrid();
            var index = new int[rings + 1, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                for (int ring = 0; ring <= rings; ring++)
                {
                    float radius = Mathf.Lerp(innerRadius, outerRadius, Mathf.Pow(ring / (float)rings, power));
                    index[ring, side] = grid.Add(new Vector3(Mathf.Cos(angle) * radius, height(angle, radius), Mathf.Sin(angle) * radius));
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                for (int ring = 0; ring < rings; ring++)
                    grid.Quad(index[ring, side], index[ring, next], index[ring + 1, next], index[ring + 1, side]);
            }
            return grid.Build(name);
        }

        public static float Ring(float angle, float seed, float frequency)
            => Mathf.PerlinNoise(Mathf.Cos(angle) * frequency + seed, Mathf.Sin(angle) * frequency + seed * 1.7f) - .5f;

        public static float GullyDepth(int tier, float angle)
        {
            float depth = 0f;
            for (int fall = 0; fall < FallAngles.Length; fall++)
            {
                if (tier < FallBottoms[fall] || tier > FallTops[fall]) continue;
                float width = (FallWidths[fall] * .5f + 12f) / Mathf.Max(80f, InnerRadius[tier]);
                float distance = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, FallAngles[fall] * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float notch = Mathf.Clamp01(1f - distance / Mathf.Max(.001f, width));
                depth = Mathf.Max(depth, notch * notch * GullyRecess);
            }
            return depth;
        }

        public static float LakeEdge(float angle)
            => LakeRadius + Ring(angle, 2.7f, 1.4f) * 46f + Ring(angle, 5.9f, 3.3f) * 24f + Ring(angle, 13.1f, 6.2f) * 10f;

        public static float WallRadius(int tier, float angle, float v)
        {
            float radius = Mathf.Lerp(InnerRadius[tier] - 3f, InnerRadius[tier] + 7f, v) * RingScale(angle);
            radius -= GullyDepth(tier, angle) * (1f - v);
            radius += CollapseMask(tier, angle) * 5f;
            radius += Mathf.Sin(angle * 21f + tier * 2.1f) * 1.4f;
            radius += Mathf.Sin(angle * 47f + tier * 1.3f) * .55f;
            radius += Mathf.Sin(angle * 7f + tier) * (2.8f + v * 1.6f);
            radius += Mathf.Sin(v * 9.4f + tier * 1.7f) * 2.4f;
            return radius;
        }

        public static float WallHeight(int tier, float angle, float v)
        {
            float baseY = Mathf.Lerp(ShelfLevel[tier - 1] - 8f, ShelfLevel[tier], Mathf.Pow(v, .95f));
            float rampY = Mathf.Lerp(ShelfLevel[tier - 1], ShelfLevel[tier], v);
            float y = Mathf.Lerp(baseY, rampY, CollapseMask(tier, angle));
            y += Mathf.Sin(v * 12f + tier) * 1.2f;
            y += Mathf.Sin(angle * 13f + tier * 3f) * 1.3f * Mathf.Sin(v * Mathf.PI);
            return y;
        }

        public static float ShelfOuter(int tier, float angle)
            => OuterRadius[tier] * RingScale(angle) + Ring(angle, tier * 2.3f + 4f, 2.1f) * 7f;

        public static float ShelfNoise(int tier, float angle, float t)
            => Ring(angle, tier * 4.1f + 9f, 2.6f) * 4f * (.35f + .65f * Mathf.Sin(t * Mathf.PI));

        public static float ShelfHeight(int tier, float angle, float radius)
        {
            float inner = WallRadius(tier, angle, 1f);
            float outer = ShelfOuter(tier, angle);
            float t = Mathf.Clamp01((radius - inner) / Mathf.Max(1f, outer - inner));
            return ShelfLevel[tier] + t * 3f + ShelfNoise(tier, angle, t);
        }

        public static float FloorHeight(float angle, float radius)
        {
            float edge = LakeEdge(angle);
            if (radius <= edge)
                return Mathf.Lerp(-72f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 118f, edge, radius)));
            float y = Mathf.Lerp(.6f, 20f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge, 222f, radius)));
            y += Ring(angle, 5.3f, 3.1f) * 5f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge + 12f, 218f, radius));
            y += Ring(angle, 9.7f, 7.4f) * 2.2f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge + 4f, 80f, radius));
            return y;
        }

        public static float IslandHeight(float angle, float radius)
        {
            float y = -72f;
            for (int island = 0; island < IslandAngles.Length; island++)
            {
                var centre = new Vector2(Mathf.Cos(IslandAngles[island]) * IslandOffsets[island], Mathf.Sin(IslandAngles[island]) * IslandOffsets[island]);
                var point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                float shape = IslandSizes[island] * (1f + Ring(angle, 8.5f + island * 3.1f, 2.4f) * .3f);
                float t = Mathf.Clamp01(Vector2.Distance(point, centre) / shape);
                y = Mathf.Max(y, Mathf.Lerp(IslandTops[island], -72f, Mathf.Pow(t, 2.6f)));
            }
            return y;
        }

        public static float OuterHeight(float angle, float radius)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(534f, 1010f, radius));
            float y = Mathf.Lerp(580f, 96f, Mathf.SmoothStep(0f, 1f, t));
            y += Ring(angle, 11.7f, 1.9f) * 62f * Mathf.Sin(t * Mathf.PI);
            y += Ring(angle, 2.4f, 4.1f) * 11f;
            y += Ring(angle, 17.3f, 9.4f) * 6f * Mathf.Sin(t * Mathf.PI);
            return y;
        }

        public static float RangeHeight(float angle, float radius)
        {
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(990f, 1330f, radius));
            float ground = Mathf.Lerp(96f, 40f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1010f, 1800f, radius)));
            ground += Ring(angle, 2.4f, 4.1f) * 11f;
            ground += (Ring(angle, 3.7f, 7.1f) * 34f + Ring(angle, 12.1f, 12.5f) * 16f) * rise;
            float spineNear = 1240f + Ring(angle, 7.1f, 1.7f) * 300f;
            float spineFar = 1680f + Ring(angle, 13.7f, 2.2f) * 340f;
            float near = Ridge(radius, spineNear, 420f) * (330f + (Ring(angle, 4.3f, 3.3f) + .5f) * 220f);
            float far = Ridge(radius, spineFar, 580f) * (430f + (Ring(angle, 9.9f, 3.9f) + .5f) * 250f);
            float crest = Mathf.Max(near, far);
            float summits = (Ring(angle, 21.3f, 8.5f) + .5f) * 95f * Mathf.Clamp01(crest / 440f);
            float taper = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1760f, 2160f, radius));
            return ground + (crest + summits) * taper * rise;
        }

        private static float Ridge(float radius, float spine, float width)
            => Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(radius - spine) / width), 1.35f);

        public static Mesh TerraceWall(int tier)
        {
            var mesh = new CrownRockMesh();
            const int wallRows = 16;
            var wall = new Vector3[wallRows, Sides];
            for (int side = 0; side < Sides; side++)
            {
                float angle = side * Mathf.PI * 2f / Sides;
                for (int row = 0; row < wallRows; row++)
                {
                    float v = row / (wallRows - 1f);
                    float radius = WallRadius(tier, angle, v);
                    wall[row, side] = new Vector3(Mathf.Cos(angle) * radius, WallHeight(tier, angle, v), Mathf.Sin(angle) * radius);
                }
            }
            for (int side = 0; side < Sides; side++)
            {
                int next = (side + 1) % Sides;
                for (int row = 0; row < wallRows - 1; row++)
                {
                    Quad(mesh, wall[row, side], wall[row, next], wall[row + 1, next], wall[row + 1, side]);
                    Quad(mesh, wall[row + 1, side], wall[row + 1, next], wall[row, next], wall[row, side]);
                }
            }
            return mesh.Build("Скальная стена яруса " + tier);
        }

        public static Mesh TerraceShelf(int tier)
        {
            var grid = new PolarGrid();
            const int rows = 12;
            var index = new int[rows + 1, Sides];
            for (int side = 0; side < Sides; side++)
            {
                float angle = side * Mathf.PI * 2f / Sides;
                float inner = WallRadius(tier, angle, 1f);
                float outer = ShelfOuter(tier, angle);
                for (int row = 0; row <= rows; row++)
                {
                    float t = row / (float)rows;
                    float radius = Mathf.Lerp(inner, outer, t);
                    float y = row == 0 ? WallHeight(tier, angle, 1f) : ShelfLevel[tier] + t * 3f + ShelfNoise(tier, angle, t);
                    index[row, side] = grid.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                }
            }
            for (int side = 0; side < Sides; side++)
            {
                int next = (side + 1) % Sides;
                for (int row = 0; row < rows; row++)
                    grid.Quad(index[row, side], index[row, next], index[row + 1, next], index[row + 1, side]);
            }
            return grid.Build("Полка яруса " + tier);
        }

        public static Mesh BasinFloor()
            => PolarTerrain("Дно котловины и луга у озера", 224, 56, 2f, 470f, .62f, FloorHeight);

        public static Mesh Islands()
        {
            var grid = new PolarGrid();
            for (int island = 0; island < IslandAngles.Length; island++)
            {
                float centreX = Mathf.Cos(IslandAngles[island]) * IslandOffsets[island];
                float centreZ = Mathf.Sin(IslandAngles[island]) * IslandOffsets[island];
                float size = IslandSizes[island];
                const int sides = 72, rings = 16;
                var index = new int[rings + 1, sides];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    for (int ring = 0; ring <= rings; ring++)
                    {
                        float radius = Mathf.Lerp(1.4f, size * 1.5f, ring / (float)rings);
                        float x = centreX + Mathf.Cos(angle) * radius;
                        float z = centreZ + Mathf.Sin(angle) * radius;
                        float span = new Vector2(x, z).magnitude;
                        float worldAngle = Mathf.Atan2(z, x);
                        index[ring, side] = grid.Add(new Vector3(x, IslandHeight(worldAngle, span), z));
                    }
                }
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    for (int ring = 0; ring < rings; ring++)
                        grid.Quad(index[ring, side], index[ring, next], index[ring + 1, next], index[ring + 1, side]);
                }
            }
            return grid.Build("Острова на озере");
        }

        public static float SlopeHeight(float angle, float radius)
        {
            float inner = ShelfOuter(3, angle);
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, inner + 70f, radius));
            return Mathf.Lerp(ShelfLevel[3] + 3f, OuterHeight(angle, radius), blend);
        }

        public static Mesh OuterSlopes()
        {
            var grid = new PolarGrid();
            const int sides = 320, rings = 80;
            var index = new int[rings + 1, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float inner = ShelfOuter(3, angle);
                for (int ring = 0; ring <= rings; ring++)
                {
                    float radius = Mathf.Lerp(inner, 1012f, ring / (float)rings);
                    index[ring, side] = grid.Add(new Vector3(Mathf.Cos(angle) * radius, SlopeHeight(angle, radius), Mathf.Sin(angle) * radius));
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                for (int ring = 0; ring < rings; ring++)
                    grid.Quad(index[ring, side], index[ring, next], index[ring + 1, next], index[ring + 1, side]);
            }
            return grid.Build("Внешние склоны котловины");
        }

        public static Mesh DistantRanges()
            => PolarTerrain("Снежные хребты по всему горизонту", 224, 56, 990f, 2200f, 1.2f, RangeHeight);

        public static Mesh Water()
        {
            var mesh = new CrownRockMesh();
            const int sides = 144;
            var center = new Vector3(0f, LakeLevel, 0f);
            var rim = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float radius = LakeEdge(angle) - 1.5f;
                rim[side] = new Vector3(Mathf.Cos(angle) * radius, LakeLevel, Mathf.Sin(angle) * radius);
            }
            for (int side = 0; side < sides; side++)
                Tri(mesh, center, rim[(side + 1) % sides], rim[side]);
            return mesh.Build("Изумрудное озеро");
        }

        public static Mesh Talus(int seed)
        {
            var mesh = new CrownRockMesh();
            const int cols = 9, rows = 5;
            var points = new Vector3[rows + 1, cols + 1];
            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                float reach = Mathf.Pow(1f - v, 1.25f);
                for (int col = 0; col <= cols; col++)
                {
                    float u = col / (float)cols;
                    float wobble = 1f + Mathf.Sin(u * 7.3f + seed * 1.9f) * .14f + Mathf.Sin(u * 13.1f + v * 2.7f) * .08f;
                    points[row, col] = new Vector3((u - .5f) * (1f - v * .55f) * wobble, v,
                        reach * (1f + Mathf.Sin(u * 5.1f + seed * 1.3f) * .16f));
                }
            }
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                    Quad(mesh, points[row, col], points[row, col + 1], points[row + 1, col + 1], points[row + 1, col]);
            for (int col = 0; col < cols; col++)
            {
                Tri(mesh, Vector3.zero, points[0, col], points[0, col + 1]);
                Tri(mesh, new Vector3(0f, 1f, 0f), points[rows, col + 1], points[rows, col]);
            }
            return mesh.Build("Осыпной конус");
        }

        public static Mesh Pebbles(int seed)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int lump = 0; lump < 16; lump++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .5f;
                float size = .08f + (float)random.NextDouble() * .18f;
                Lump(mesh, new Vector3(Mathf.Cos(angle) * radius, -size * .3f, Mathf.Sin(angle) * radius), size, seed + lump * 7);
            }
            return mesh.Build("Галечная отсыпь");
        }

        private static void Lump(CrownRockMesh mesh, Vector3 center, float size, int seed)
        {
            const int sides = 6;
            var lower = new Vector3[sides];
            var upper = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float wobble = 1f + Mathf.Sin(angle * 2.7f + seed * 1.3f) * .22f;
                lower[side] = center + new Vector3(Mathf.Cos(angle) * size * wobble, size * .1f, Mathf.Sin(angle) * size * wobble);
                upper[side] = center + new Vector3(Mathf.Cos(angle) * size * .62f * wobble, size * .78f, Mathf.Sin(angle) * size * .62f * wobble);
            }
            var top = center + new Vector3(0f, size, 0f);
            var bottom = center - new Vector3(0f, size * .35f, 0f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, lower[side], upper[side], upper[next]);
                Tri(mesh, lower[side], upper[next], lower[next]);
                Tri(mesh, top, upper[next], upper[side]);
                Tri(mesh, bottom, lower[side], lower[next]);
            }
        }

        public static Mesh Spruce()
        {
            var mesh = new CrownRockMesh();
            const int sides = 8;
            for (int tier = 0; tier < 6; tier++)
            {
                float bottom = .17f + tier * .115f;
                float top = bottom + .28f;
                float width = .25f * (1f - tier * .115f);
                var apex = new Vector3(Mathf.Sin(tier * 2.1f) * .02f, top, Mathf.Cos(tier * 1.3f) * .02f);
                for (int side = 0; side < sides; side++)
                {
                    Vector3 Rim(int index)
                    {
                        float angle = index * Mathf.PI * 2f / sides;
                        float radius = width * (1f + Mathf.Sin(index * 2.3f + tier * 1.7f) * .12f);
                        return new Vector3(Mathf.Cos(angle) * radius, bottom + Mathf.Sin(index * 1.9f + tier) * .025f, Mathf.Sin(angle) * radius);
                    }
                    Tri(mesh, apex, Rim(side + 1), Rim(side));
                    Tri(mesh, new Vector3(0f, bottom, 0f), Rim(side), Rim(side + 1));
                }
            }
            return mesh.Build("Ель");
        }

        public static Mesh ForestPatch(float centreAngle, float centreRadius, float spread, int trees,
            float minHeight, float maxHeight, float sizePower, int seed, System.Func<float, float, float> surface)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(seed);
            for (int tree = 0; tree < trees; tree++)
            {
                float angle = centreAngle + ((float)random.NextDouble() * 2f - 1f) * spread / Mathf.Max(40f, centreRadius);
                float radius = centreRadius + ((float)random.NextDouble() * 2f - 1f) * spread;
                float height = Mathf.Lerp(minHeight, maxHeight, Mathf.Pow((float)random.NextDouble(), sizePower));
                float yaw = (float)random.NextDouble() * 360f;
                var basePosition = new Vector3(Mathf.Cos(angle) * radius, surface(angle, radius) - 1.6f, Mathf.Sin(angle) * radius);
                Cone(mesh, basePosition, height, yaw, 1f + (float)random.NextDouble() * .16f);
            }
            return mesh.Build("Лесной массив");
        }

        private static void Cone(CrownRockMesh mesh, Vector3 position, float height, float yaw, float width)
        {
            const int sides = 6;
            for (int tier = 0; tier < 3; tier++)
            {
                float bottom = .1f + tier * .3f;
                float top = bottom + .48f;
                float spread = (.44f - tier * .12f) * width;
                var origin = position + new Vector3(0f, bottom * height, 0f);
                var apex = position + new Vector3(0f, top * height, 0f);
                for (int side = 0; side < sides; side++)
                {
                    Vector3 Rim(int index)
                    {
                        float angle = (yaw + index * 360f / sides) * Mathf.Deg2Rad;
                        float radius = spread * (1f + Mathf.Sin(index * 2.3f + tier) * .1f);
                        return origin + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    }
                    Tri(mesh, apex, Rim(side + 1), Rim(side));
                }
            }
        }

        public static Mesh Boulder(int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 9;
            var heights = new[] { 0f, .34f, .7f, 1f };
            var widths = new[] { .72f, 1f, .9f, .36f };
            var points = new Vector3[heights.Length, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                for (int ring = 0; ring < heights.Length; ring++)
                {
                    float radius = widths[ring] * (1f + Mathf.Sin(angle * 3.1f + seed * 1.7f + ring * 1.3f) * .17f);
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius, heights[ring], Mathf.Sin(angle) * radius * .82f);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                for (int ring = 0; ring < heights.Length - 1; ring++)
                    Quad(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next]);
                Tri(mesh, new Vector3(0f, 1.04f, 0f), points[heights.Length - 1, next], points[heights.Length - 1, side]);
                Tri(mesh, new Vector3(0f, -.06f, 0f), points[0, side], points[0, next]);
            }
            return mesh.Build("Валун " + seed);
        }

        public static Mesh Ledge(int seed)
        {
            var mesh = new CrownRockMesh();
            const int sides = 11;
            var heights = new[] { 0f, .24f, .56f, .76f };
            var widths = new[] { 1f, .95f, .66f, .28f };
            var points = new Vector3[heights.Length, sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                for (int ring = 0; ring < heights.Length; ring++)
                {
                    float stretch = 1f + Mathf.Sin(angle * 1.7f + seed * .9f) * .22f + Mathf.Sin(angle * 3.3f + seed * 1.6f + ring * .8f) * .1f;
                    float radius = widths[ring] * stretch * (1f + Mathf.Sin(angle * 4.9f + seed * 2.3f) * .07f);
                    float y = heights[ring] * (1f + Mathf.Sin(angle * 2.6f + seed * 1.1f) * .07f + Mathf.Sin(angle * 5.7f + ring) * .03f);
                    points[ring, side] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius * .78f);
                }
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                for (int ring = 0; ring < heights.Length - 1; ring++)
                    Quad(mesh, points[ring, side], points[ring + 1, side], points[ring + 1, next], points[ring, next]);
                Tri(mesh, new Vector3(0f, .8f, 0f), points[heights.Length - 1, next], points[heights.Length - 1, side]);
                Tri(mesh, new Vector3(0f, -.06f, 0f), points[0, side], points[0, next]);
            }
            return mesh.Build("Природная полка-ступень " + seed);
        }

        public static Mesh Cascade(int tier)
        {
            var mesh = new CrownRockMesh();
            float height = ShelfLevel[tier] - ShelfLevel[tier - 1];
            const int rows = 16, cols = 4;
            var sheet = new Vector3[rows + 1, cols + 1];
            var shade = new Color[rows + 1, cols + 1];
            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                float y = .4f + (height - 1.6f) * v;
                float z = Mathf.Lerp(-(GullyRecess + 7f), 3f, v) + Mathf.Sin(v * 3.6f + tier) * 1.3f;
                float taper = Mathf.Lerp(.52f, 1f, Mathf.Pow(v, .8f));
                float ragged = 1f + Mathf.Sin(v * 7.3f + tier * 2.1f) * .16f;
                for (int col = 0; col <= cols; col++)
                {
                    float u = col / (float)cols;
                    float x = Mathf.Lerp(-.5f, .5f, u) * taper * ragged;
                    float bow = Mathf.Cos(u * Mathf.PI - Mathf.PI * .5f) * 1.4f;
                    float sway = Mathf.Sin(v * 2.6f + tier * 1.7f) * 2.2f + Mathf.Sin(v * 5.9f + u * 4.3f + tier) * .8f;
                    sheet[row, col] = new Vector3(x, y, z + bow + sway + Mathf.Sin(u * 3.1f + v * 2.2f) * .4f);
                    float edge = 1f - Mathf.Abs(u - .5f) * 2f;
                    shade[row, col] = new Color(Mathf.Pow(v, .8f), Mathf.Clamp01(edge * 1.35f), 0f, 1f);
                }
            }
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    Tri(mesh, sheet[row, col], sheet[row, col + 1], sheet[row + 1, col + 1],
                        shade[row, col], shade[row, col + 1], shade[row + 1, col + 1]);
                    Tri(mesh, sheet[row, col], sheet[row + 1, col + 1], sheet[row + 1, col],
                        shade[row, col], shade[row + 1, col + 1], shade[row + 1, col]);
                }
            return mesh.Build("Водопадный каскад " + tier);
        }

        public static Mesh Stream(int tier)
        {
            var mesh = new CrownRockMesh();
            float from = InnerRadius[tier] - 4f;
            float to = tier == 1 ? LakeRadius - 6f : InnerRadius[tier - 1] + 8f;
            float drop = tier == 1 ? 24f : 3.4f;
            float length = Mathf.Max(8f, from - to);
            const int rows = 16;
            for (int row = 0; row < rows; row++)
            {
                float u = row / (float)rows;
                float next = (row + 1) / (float)rows;
                float drift = Mathf.Sin(u * 2.4f + tier) * 5.5f;
                float driftNext = Mathf.Sin(next * 2.4f + tier) * 5.5f;
                var a = new Vector3(length * u, -drop * u, drift - 4f);
                var b = new Vector3(length * u, -drop * u, drift + 4f);
                var c = new Vector3(length * next, -drop * next, driftNext + 4f);
                var d = new Vector3(length * next, -drop * next, driftNext - 4f);
                Quad(mesh, a, d, c, b);
            }
            return mesh.Build("Ручей по полке " + tier);
        }

        public static Mesh Foam()
        {
            var mesh = new CrownRockMesh();
            const int sides = 22;
            var inner = new Vector3[sides];
            var outer = new Vector3[sides];
            var skirt = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float irregular = 1f + Mathf.Sin(angle * 4.7f) * .13f;
                inner[side] = new Vector3(Mathf.Cos(angle) * .34f, .3f, Mathf.Sin(angle) * .34f);
                outer[side] = new Vector3(Mathf.Cos(angle) * irregular, .04f, Mathf.Sin(angle) * irregular);
                skirt[side] = new Vector3(Mathf.Cos(angle) * irregular * 1.05f, -.32f, Mathf.Sin(angle) * irregular * 1.05f);
            }
            var apex = new Vector3(0f, .38f, 0f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Tri(mesh, apex, inner[next], inner[side]);
                Quad(mesh, inner[side], inner[next], outer[next], outer[side]);
                Quad(mesh, outer[side], outer[next], skirt[next], skirt[side]);
            }
            return mesh.Build("Пена и брызги");
        }

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
            => mesh.Triangle(a, b, c, Surface, Surface, Surface);

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            => mesh.Triangle(a, b, c, ca, cb, cc);

        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(mesh, a, b, c);
            Tri(mesh, a, c, d);
        }
    }
}
