using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.Levels
{
    public sealed class CrownRockMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Color> colors = new List<Color>();

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(ca); colors.Add(cb); colors.Add(cc);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }
        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color top, Color bottom)
        { Triangle(a, c, b, top, top, bottom); Triangle(c, d, b, top, bottom, bottom); }

        public void Shelf(LandingPlatform platform, float bottom, System.Random random)
        {
            const int sides = 9;
            const int rings = 5;
            Vector3 center = platform.Center;
            Vector3 inward = Vector3.forward;
            var points = new Vector3[rings, sides];
            float depth = Mathf.Min(10f + platform.Radius, center.y - bottom);
            float[] y = { 0f, -.7f, -depth * .18f, -depth * .58f, -depth };
            float[] scales = { 1f, 1.04f, 1.12f, .85f, .3f };
            float[] shift = { 0f, 1f, 4f, 8f, 13f };
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float irregular = Mathf.Lerp(.98f, 1.15f, (float)random.NextDouble());
                for (int ring = 0; ring < rings; ring++)
                    points[ring, side] = center + Vector3.up * y[ring] + inward * shift[ring]
                        + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle) > 0f ? Mathf.Sin(angle) * 2.5f : Mathf.Sin(angle))
                            * platform.Radius * scales[ring] * irregular;
            }
            Color cap = new Color(1f, .2f, (int)platform.Type / 5f, platform.Progress);
            Color wall = new Color(0f, 0f, 0f, platform.Progress);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Triangle(center, points[0, next], points[0, side], cap, cap, cap);
                for (int ring = 0; ring < rings - 1; ring++)
                    Quad(points[ring, side], points[ring + 1, side], points[ring, next], points[ring + 1, next], wall, wall);
                Triangle(center + Vector3.up * y[4] + inward * shift[4], points[4, side], points[4, next], wall, wall, wall);
            }
        }

        public void Connection(LandingPlatform from, LandingPlatform to, float width)
        {
            Vector3 side = Vector3.Cross(Vector3.up, to.Center - from.Center).normalized;
            if (Vector3.Dot(side, Vector3.forward) < 0f) side = -side;
            float rear = Mathf.Lerp(4f, 6.5f, .5f + .5f * Mathf.Sin(from.Index * 1.73f));
            if (width <= 1f) rear = width;
            rear = Mathf.Lerp(width, rear, Mathf.Abs(side.z));
            Vector3 travel = Vector3.ProjectOnPlane(to.Center - from.Center, Vector3.up).normalized;
            Vector3 start = from.Center + travel * from.Radius * .6f;
            Vector3 end = to.Center - travel * to.Radius * .6f;
            Vector3 a = start + side * rear, b = start - side * width;
            Vector3 c = end + side * rear, d = end - side * width;
            Color first = new Color(1f, .2f, 0f, from.Progress), last = new Color(1f, .2f, 0f, to.Progress);
            Color wall = new Color(0f, 0f, 0f, from.Progress);
            Vector3 foot = Vector3.down * 11f + Vector3.forward * 8f;
            Vector3 ab = a + foot, bb = b + foot, cb = c + foot, db = d + foot;
            if (Vector3.Cross(b - a, c - a).y < 0f)
            {
                Triangle(a, c, b, first, last, first); Triangle(b, c, d, first, last, last);
                Quad(c, cb, a, ab, wall, wall); Quad(b, bb, d, db, wall, wall);
            }
            else
            {
                Triangle(a, b, c, first, first, last); Triangle(b, d, c, first, last, last);
                Quad(a, ab, c, cb, wall, wall); Quad(d, db, b, bb, wall, wall);
            }
            Triangle(ab, cb, bb, wall, wall, wall); Triangle(bb, cb, db, wall, wall, wall);
        }

        public void RockBlock(Vector3 center, Vector2 size, float top, float bottom, System.Random random)
        {
            var outline = new[] { new Vector2(-.7f, -1f), new Vector2(.65f, -1f), new Vector2(1f, -.55f),
                new Vector2(.92f, .65f), new Vector2(.5f, 1f), new Vector2(-.6f, .9f),
                new Vector2(-1f, .5f), new Vector2(-1f, -.5f) };
            const int rings = 7;
            var points = new Vector3[rings, outline.Length];
            Color wall = new Color(0f, 0f, 0f, 1f);
            for (int side = 0; side < outline.Length; side++)
            {
                float fracture = Mathf.Lerp(.92f, 1.04f, (float)random.NextDouble());
                float crown = Mathf.Lerp(-4f, 4f, (float)random.NextDouble());
                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    float width = fracture * Mathf.Lerp(.85f, 1.04f, t)
                        + Mathf.Sin(side * 1.7f + ring * .8f) * .08f;
                    float px = center.x + outline[side].x * size.x * width;
                    float pz = center.z + outline[side].y * size.y * width;
                    points[ring, side] = new Vector3(px, Mathf.Lerp(top + crown, bottom, t), pz);
                }
            }
            for (int side = 0; side < outline.Length; side++)
            {
                int next = (side + 1) % outline.Length;
                Triangle(new Vector3(center.x, top, center.z), points[0, next], points[0, side], wall, wall, wall);
                for (int ring = 0; ring < rings - 1; ring++)
                    Quad(points[ring, side], points[ring + 1, side], points[ring, next], points[ring + 1, next], wall, wall);
                Triangle(new Vector3(center.x, bottom, center.z), points[rings - 1, side], points[rings - 1, next], wall, wall, wall);
            }
        }

        public void Massif(LevelRecipe recipe, LevelRoute route)
        {
            const int grid = 150;
            float extent = recipe.Radius * 1.75f;
            var points = new Vector3[grid + 1, grid + 1];
            Color rock = new Color(0f, 0f, 0f, 1f);
            for (int z = 0; z <= grid; z++)
                for (int x = 0; x <= grid; x++)
                {
                    float px = Mathf.Lerp(-extent, extent, x / (float)grid);
                    float pz = Mathf.Lerp(-extent, extent, z / (float)grid);
                    float height = RockHeight(px, pz - recipe.Radius * .24f, recipe);
                    float nearest = float.PositiveInfinity, pathHeight = 0f, corridor = 0f;
                    bool walking = false;
                    Vector2 position = new Vector2(px, pz);
                    for (int i = 0; i < route.Connections.Length; i++)
                    {
                        var from = route.Platforms[i]; var to = route.Platforms[i + 1];
                        Vector2 start = new Vector2(from.Center.x, from.Center.z);
                        Vector2 delta = new Vector2(to.Center.x, to.Center.z) - start;
                        float t = Mathf.Clamp01(Vector2.Dot(position - start, delta) / delta.sqrMagnitude);
                        float distance = Vector2.Distance(position, start + delta * t);
                        if (distance >= nearest) continue;
                        nearest = distance;
                        pathHeight = Mathf.Lerp(from.Center.y, to.Center.y, t);
                        corridor = Mathf.Lerp(from.Radius, to.Radius, t) + 2f;
                        walking = route.Connections[i] == RouteConnection.Walk;
                    }
                    float weight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(corridor, corridor + 5f, nearest));
                    float surface = pathHeight - (walking ? .3f : 7f);
                    height = Mathf.Lerp(height, surface, weight);
                    points[x, z] = new Vector3(px, height, pz);
                }
            for (int z = 0; z < grid; z++)
                for (int x = 0; x < grid; x++)
                {
                    Triangle(points[x, z], points[x, z + 1], points[x + 1, z], rock, rock, rock);
                    Triangle(points[x + 1, z], points[x, z + 1], points[x + 1, z + 1], rock, rock, rock);
                }
        }

        private static float RockHeight(float x, float z, LevelRecipe recipe)
        {
            float radius = recipe.Radius;
            float height = recipe.SummitHeight - recipe.ValleyHeight;
            float floor = recipe.ValleyHeight - 43f;
            float result = floor;
            for (int peak = 0; peak < 4; peak++)
            {
                float cx = (peak == 0 ? -.12f : peak == 1 ? .3f : peak == 2 ? -.42f : .12f) * radius;
                float cz = (peak == 0 ? .16f : peak == 1 ? .32f : peak == 2 ? -.08f : -.42f) * radius;
                float dx = (x - cx) / (radius * (peak == 3 ? .95f : 1.2f));
                float dz = (z - cz) / (radius * (peak == 3 ? 1.1f : 1.42f));
                float angle = Mathf.Atan2(dz, dx);
                float ribs = 1f + Mathf.Sin(angle * 5f + peak * 2f + recipe.Seed * .13f) * .12f
                    + Mathf.Sin(angle * 9f - peak) * .045f;
                float distance = Mathf.Sqrt(dx * dx + dz * dz) / ribs;
                float profile = Mathf.Pow(Mathf.Clamp01(1f - distance), .7f);
                float peakHeight = height * (peak == 0 ? 1.38f : peak == 1 ? 1.2f : peak == 2 ? .98f : .65f);
                float noise = (Mathf.PerlinNoise(x * .035f + peak * 17f + recipe.Seed, z * .035f + peak * 11f) - .5f) * 9f;
                result = Mathf.Max(result, floor + profile * (peakHeight + 43f) + noise * profile);
            }
            float phase = (Mathf.PerlinNoise(x * .017f + recipe.Seed * .17f, z * .017f) - .5f) * height * .32f
                + x * .2f - z * .1f;
            float step = height * Mathf.Lerp(.08f, .17f, Mathf.PerlinNoise(x * .013f + 19f, z * .013f + recipe.Seed));
            float level = (result - floor - phase) / step;
            float fraction = level - Mathf.Floor(level);
            float terrace = (Mathf.Floor(level) + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.78f, 1f, fraction))) * step;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(floor, floor + 30f, result));
            return Mathf.Lerp(result, floor + phase + terrace, edge);
        }

        public void Backdrop(LevelRecipe recipe)
        {
            const int grid = 96;
            const float extent = 1100f;
            Color color = new Color(0f, 0f, 0f, 1f);
            for (int z = 0; z < grid; z++)
                for (int x = 0; x < grid; x++)
                {
                    Vector3 a = Landscape(x, z, grid, extent, recipe), b = Landscape(x + 1, z, grid, extent, recipe);
                    Vector3 c = Landscape(x, z + 1, grid, extent, recipe), d = Landscape(x + 1, z + 1, grid, extent, recipe);
                    Triangle(a, c, b, color, color, color); Triangle(b, c, d, color, color, color);
                }
        }

        private static Vector3 Landscape(int x, int z, int grid, float extent, LevelRecipe recipe)
        {
            float px = Mathf.Lerp(-extent, extent, x / (float)grid), pz = Mathf.Lerp(-extent, extent, z / (float)grid);
            float radius = new Vector2(px, pz).magnitude;
            float distant = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(recipe.Radius * 1.5f, 800f, radius));
            float noise = Mathf.PerlinNoise(px * .003f + recipe.Seed * .17f, pz * .003f + recipe.Seed * .11f);
            float ridge = 1f - Mathf.Abs(noise * 2f - 1f);
            float y = recipe.ValleyHeight - 45f + Mathf.Pow(ridge, 3f) * distant * 130f;
            return new Vector3(px, y, pz);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
