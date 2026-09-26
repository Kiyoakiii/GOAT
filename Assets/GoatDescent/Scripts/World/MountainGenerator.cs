using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainGenerator
    {
        private static List<LandingPlatform> _route;

        public static void PrepareRoute(MountainSettings s)
        {
            _route = MountainRoute.Generate(s);
        }

        public static List<LandingPlatform> GetRoute()
        {
            return _route ?? new List<LandingPlatform>();
        }

        public static Vector3 SpawnPoint(MountainSettings s)
        {
            if (_route != null && _route.Count > 0)
                return new Vector3(_route[0].Center.x, _route[0].Center.y + 1.5f, _route[0].Center.z);
            return new Vector3(0f, s.SummitY + 1.5f, 0f);
        }

        public static float SummitTop(MountainSettings s)
        {
            return s.SummitY;
        }

        public static float HeightAt(MountainSettings s, float x, float z)
        {
            float y = RawHeight(s, x, z);
            if (_route != null && _route.Count >= 2)
            {
                float trailWidth = s.PlatformSize;
                float flatCore = trailWidth * .72f;
                if (NearestOnRoute(x, z, out Vector3 routePos, out float routeY, out float dist))
                {
                    if (dist <= trailWidth)
                    {
                        float t = Mathf.Clamp01((dist - flatCore) / (trailWidth - flatCore));
                        float eased = t * t * (3f - 2f * t);
                        y = Mathf.Lerp(routeY, y, eased);
                    }
                }
            }
            return y;
        }

        private static bool NearestOnRoute(float x, float z, out Vector3 routePos, out float routeY, out float dist)
        {
            routePos = default; routeY = 0f; dist = float.MaxValue;
            for (int i = 0; i < _route.Count - 1; i++)
            {
                Vector3 a = _route[i].Center;
                Vector3 b = _route[i + 1].Center;
                Vector2 p = new Vector2(x, z);
                Vector2 va = new Vector2(a.x, a.z);
                Vector2 vb = new Vector2(b.x, b.z);
                Vector2 ab = vb - va;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - va, ab) / len2) : 0f;
                Vector2 closest = va + ab * t;
                float d = Vector2.Distance(p, closest);
                if (d < dist)
                {
                    dist = d;
                    routePos = new Vector3(closest.x, 0f, closest.y);
                    routeY = Mathf.Lerp(a.y, b.y, t);
                }
            }
            return dist < float.MaxValue;
        }

        public static float RawHeight(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count == 0)
                return Mathf.Lerp(s.ValleyY, s.SummitY, 1f - Mathf.Clamp01(Mathf.Sqrt(x * x + z * z) / s.Radius));

            float routeY = NearestRouteHeight(s, x, z, out float distToRoute);
            float ridgeHalfWidth = s.Radius * .42f;

            float t = Mathf.Clamp01(distToRoute / ridgeHalfWidth);
            float falloff = 1f - t * t * (3f - 2f * t);
            float y = Mathf.Lerp(s.ValleyY, routeY, falloff);

            float angle = Mathf.Atan2(z, x);
            float radial = Mathf.Clamp01(Mathf.Sqrt(x * x + z * z) / s.Radius);

            float p = Mathf.InverseLerp(s.ValleyY, s.SummitY, y);
            p = ApplyStrata(s, p, x, z);
            y = Mathf.Lerp(s.ValleyY, s.SummitY, p);

            y -= CouloirFactor(s, radial, angle) * falloff;

            float noise = (Noise(s, x * MountainSettings.NoiseScale + s.Seed * 3.1f, z * MountainSettings.NoiseScale) - .5f) * 2f * s.NoiseAmplitude * .5f * falloff;
            y += noise;

            return y;
        }

        private static float NearestRouteHeight(MountainSettings s, float x, float z, out float dist)
        {
            float best = float.MaxValue;
            float bestY = s.ValleyY;
            for (int i = 0; i < _route.Count; i++)
            {
                float dx = x - _route[i].Center.x;
                float dz = z - _route[i].Center.z;
                float d = dx * dx + dz * dz;
                if (d < best)
                {
                    best = d;
                    bestY = _route[i].Center.y;
                }
            }
            dist = Mathf.Sqrt(best);
            return bestY;
        }

        private static float CouloirFactor(MountainSettings s, float radial, float angle)
        {
            if (s.CouloirCount <= 0 || radial < 0.34f * .8f) return 0f;
            float strongest = 0f;
            for (int i = 0; i < s.CouloirCount; i++)
            {
                float target = (i / (float)s.CouloirCount) * Mathf.PI * 2f + s.Seed * .31f;
                float d = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, target * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float falloff = Mathf.Exp(-(d * d) / (2f * MountainSettings.CouloirWidth * MountainSettings.CouloirWidth));
                if (falloff > strongest) strongest = falloff;
            }
            float depth = MountainSettings.CouloirDepth * Mathf.Clamp01((radial - 0.34f * .7f) / (1f - 0.34f * .7f));
            return strongest * depth;
        }

        private static float ApplyStrata(MountainSettings s, float p, float x, float z)
        {
            float wave = (Noise(s, x * MountainSettings.NoiseScale + s.Seed, z * MountainSettings.NoiseScale) - .5f) * MountainSettings.StrataWaviness;
            float bands = (p + wave) * s.StrataCount;
            float floorBand = Mathf.Floor(bands);
            float frac = bands - floorBand;
            float eased = Mathf.Pow(frac, MountainSettings.StrataSharpness);
            float stratified = (floorBand + eased) / s.StrataCount;
            return Mathf.Lerp(p, stratified, MountainSettings.StrataStrength);
        }

        public static float Noise(MountainSettings s, float x, float z)
        {
            return Mathf.PerlinNoise(x, z);
        }

        public static Vector3 NormalAt(MountainSettings s, float x, float z)
        {
            float e = 0.5f;
            float hL = HeightAt(s, x - e, z);
            float hR = HeightAt(s, x + e, z);
            float hD = HeightAt(s, x, z - e);
            float hU = HeightAt(s, x, z + e);
            return new Vector3(hL - hR, 2f * e, hD - hU).normalized;
        }

        public static Mesh Build(MountainSettings s)
        {
            PrepareRoute(s);
            int cols = s.Grid + 1;
            int rows = s.Grid + 1;
            float step = (s.Radius * 2f) / s.Grid;
            float zStart = -s.Radius;
            float floorY = s.ValleyY - 40f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int rz = 0; rz < rows; rz++)
            {
                float z = zStart + rz * step;
                for (int rx = 0; rx < cols; rx++)
                {
                    float x = -s.Radius + rx * step;
                    float y = HeightAt(s, x, z);
                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)rx / s.Grid, Mathf.InverseLerp(s.ValleyY, s.SummitY, y)));
                }
            }
            for (int rz = 0; rz < s.Grid; rz++)
                for (int rx = 0; rx < s.Grid; rx++)
                {
                    int a = rz * cols + rx;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }

            AddSideWall(verts, uvs, tris, 0, step, rows, cols, floorY, zStart);
            AddSideWall(verts, uvs, tris, cols - 1, step, rows, cols, floorY, zStart);
            AddEndWall(verts, uvs, tris, 0, step, cols, floorY, zStart);
            AddEndWall(verts, uvs, tris, rows - 1, step, cols, floorY, zStart + (rows - 1) * step);

            while (uvs.Count < verts.Count) uvs.Add(Vector2.zero);

            var mesh = new Mesh { name = "Procedural Mesa" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSideWall(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int col, float step, int rows, int cols, float floorY, float zStart)
        {
            for (int rz = 0; rz < rows - 1; rz++)
            {
                int i0 = rz * cols + col;
                int i1 = (rz + 1) * cols + col;
                int baseIdx = verts.Count;
                verts.Add(new Vector3(verts[i0].x, verts[i0].y, verts[i0].z));
                verts.Add(new Vector3(verts[i0].x, floorY, verts[i0].z));
                verts.Add(new Vector3(verts[i1].x, verts[i1].y, verts[i1].z));
                verts.Add(new Vector3(verts[i1].x, floorY, verts[i1].z));
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }

        private static void AddEndWall(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int row, float step, int cols, float floorY, float z)
        {
            for (int rx = 0; rx < cols - 1; rx++)
            {
                int i0 = row * cols + rx;
                int i1 = i0 + 1;
                int baseIdx = verts.Count;
                verts.Add(new Vector3(verts[i0].x, verts[i0].y, z));
                verts.Add(new Vector3(verts[i0].x, floorY, z));
                verts.Add(new Vector3(verts[i1].x, verts[i1].y, z));
                verts.Add(new Vector3(verts[i1].x, floorY, z));
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }
    }
}