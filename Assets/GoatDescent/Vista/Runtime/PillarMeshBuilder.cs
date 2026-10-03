using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class PillarMeshBuilder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<int> triangles = new List<int>();

        public int AddVertex(Vector3 p, Color color) { int i = vertices.Count; vertices.Add(p); colors.Add(color); return i; }
        public void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
        public void FlattenFaces()
        {
            var positions = new List<Vector3>(triangles.Count);
            var tints = new List<Color>(triangles.Count);
            for (int i = 0; i < triangles.Count; i++) { positions.Add(vertices[triangles[i]]); tints.Add(colors[triangles[i]]); triangles[i] = i; }
            vertices.Clear(); vertices.AddRange(positions); colors.Clear(); colors.AddRange(tints);
        }

        public void AddCrown(Vector3 center, Vector3 scale, int seed, Color tint, int sides, int rings)
        {
            int first = vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float lat = r / (float)rings * Mathf.PI;
                for (int s = 0; s <= sides; s++)
                {
                    float angle = s / (float)sides * Mathf.PI * 2f;
                    Vector3 u = new Vector3(Mathf.Sin(lat) * Mathf.Cos(angle), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(angle));
                    float wobble = 1f + .18f * Noise(u.x * 4f + u.y, u.z * 4f + u.y * 3f, seed);
                    float shade = .8f + .2f * u.y + .1f * Noise(u.x * 8f, u.z * 8f, seed);
                    AddVertex(center + Vector3.Scale(u, scale) * wobble, new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < sides; s++)
            {
                int a = first + r * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                Triangle(a, b, c); Triangle(b, d, c);
            }
        }

        public void AddBranch(Vector3 start, Vector3 end, float r0, float r1, int sides)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 right = Vector3.Cross(dir, Mathf.Abs(dir.y) > .9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(dir, right); int first = vertices.Count;
            for (int r = 0; r < 2; r++)
            for (int s = 0; s < sides; s++)
            {
                float a = s / (float)sides * Mathf.PI * 2f;
                AddVertex((r == 0 ? start : end) + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * (r == 0 ? r0 : r1), Color.white);
            }
            for (int s = 0; s < sides; s++)
            {
                int a = first + s, b = first + (s + 1) % sides;
                Triangle(a, b, a + sides); Triangle(b, b + sides, a + sides);
            }
        }

        public static float Noise(float x, float y, int seed) => Mathf.PerlinNoise(x + seed * 3.71f, y + seed * 1.13f) * 2f - 1f;
        public static float Next(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        public static Vector3 RockPoint(float t, float angle, float height, float rx, float rz, int seed)
        {
            float phase = seed * .73f;
            float outline = 1f + .14f * Mathf.Sin(angle * 3f + phase) + .09f * Mathf.Sin(angle * 7f - phase * 1.7f);
            float grooves = .075f * Mathf.Sin(angle * 19f + phase) + .04f * Mathf.Sin(angle * 37f - phase);
            float erosion = .045f * Noise(Mathf.Cos(angle) * 2.5f + t * .8f, Mathf.Sin(angle) * 2.5f + t * 7f, seed);
            float profile = 1.10f - .12f * t + .035f * Mathf.Sin(t * 13f + phase) - .055f * Mathf.Repeat(t * 11f + phase, 1f);
            profile += .18f * Mathf.Pow(1f - t, 4f);
            if (t > .94f) profile *= Mathf.Lerp(1f, .82f, (t - .94f) / .06f);
            float radius = profile * (outline + grooves + erosion);
            float leanX = Mathf.Sin(t * 2.5f + phase) * rx * .10f, leanZ = Mathf.Cos(t * 3.1f + phase) * rz * .09f;
            float terrace = Mathf.Sin(angle * 4f + phase) * 1.5f + Noise(t * 18f, angle * 3f, seed) * 2.4f;
            return new Vector3(Mathf.Cos(angle) * rx * radius + leanX, height * t + terrace * Mathf.SmoothStep(0f, 1f, t * 8f), Mathf.Sin(angle) * rz * radius + leanZ);
        }
    }
}