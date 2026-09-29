using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainGenerator
    {
        private static List<LandingPlatform> _route;
        private static float[] _profileRadii;
        private static float[] _profileHeights;
        private static float[] _heightField;
        private static int _heightFieldSize;
        private static readonly float[] _boulderX = new float[10];
        private static readonly float[] _boulderZ = new float[10];
        private static readonly float[] _boulderRadius = new float[10];
        private static int _boulderSeed = int.MinValue;

        public static IReadOnlyList<LandingPlatform> CurrentRoute => _route;

        public static int MainRouteCount
        {
            get
            {
                if (_route == null) return 0;
                int count = 0;
                for (int i = 0; i < _route.Count; i++)
                    if (!_route[i].IsBranch) count++;
                return count;
            }
        }

        public static void PrepareRoute(MountainSettings s)
        {
            _route = MountainRoute.Generate(s);
            EnsureBoulders(s.Seed);
            int n = _route.Count;
            _profileRadii = new float[n];
            _profileHeights = new float[n];
            for (int i = 0; i < n; i++)
            {
                _profileRadii[i] = Mathf.Sqrt(_route[i].Center.x * _route[i].Center.x + _route[i].Center.z * _route[i].Center.z);
                _profileHeights[i] = _route[i].Center.y;
            }

            BakeHeightField(s);
            MountainRoute.AdjustForVisibility(s, _route, (x, z) => RawHeight(s, x, z));
            MountainRoute.AddBranches(s, _route);

            for (int i = 0; i < n; i++)
            {
                Vector3 p = _route[i].Center;
                float ground = SampleHeightField(s, p.x, p.z);
                LandingPlatform platform = _route[i];
                platform.Center = new Vector3(p.x, ground + MountainRoute.PlatformHeightOffset(platform.Type), p.z);
                _route[i] = platform;
            }

            for (int i = n; i < _route.Count; i++)
            {
                LandingPlatform platform = _route[i];
                float ground = SampleHeightField(s, platform.Center.x, platform.Center.z);
                platform.Center = new Vector3(platform.Center.x, ground + MountainRoute.PlatformHeightOffset(platform.Type), platform.Center.z);
                _route[i] = platform;
            }
        }

        public static Vector3 SpawnPoint(MountainSettings s)
        {
            if (_route != null && _route.Count > 0)
                return new Vector3(_route[0].Center.x, _route[0].Center.y + 1.5f, _route[0].Center.z);
            return new Vector3(0f, s.SummitY + 1.5f, 0f);
        }

        public static float HeightAt(MountainSettings s, float x, float z)
        {
            if (_heightField == null || _heightFieldSize < 2)
                return HeightAtVertex(s, x, z);

            float gx = Mathf.Clamp01((x + s.Radius) / (s.Radius * 2f)) * (_heightFieldSize - 1);
            float gz = Mathf.Clamp01((z + s.Radius) / (s.Radius * 2f)) * (_heightFieldSize - 1);
            int ix = Mathf.Min((int)gx, _heightFieldSize - 2);
            int iz = Mathf.Min((int)gz, _heightFieldSize - 2);
            float tx = gx - ix;
            float tz = gz - iz;
            float step = (s.Radius * 2f) / (_heightFieldSize - 1);
            float x0 = -s.Radius + ix * step;
            float z0 = -s.Radius + iz * step;

            if (tx < .00001f && tz < .00001f)
                return HeightAtVertex(s, x0, z0);

            float h00 = HeightAtVertex(s, x0, z0);
            float h10 = HeightAtVertex(s, x0 + step, z0);
            float h01 = HeightAtVertex(s, x0, z0 + step);
            float h11 = HeightAtVertex(s, x0 + step, z0 + step);

            if (tx + tz <= 1f)
                return h00 * (1f - tx - tz) + h10 * tx + h01 * tz;

            return h10 * (1f - tz) + h01 * (1f - tx) + h11 * (tx + tz - 1f);
        }

        private static float HeightAtVertex(MountainSettings s, float x, float z)
        {
            float y = RawHeight(s, x, z);
            if (_route != null && _route.Count >= 2)
            {
                FindNearestPlatform(x, z, out float best, out LandingPlatform platform);
                float platformRadius = Mathf.Max(.5f, platform.Radius);
                float gapRadius = platformRadius + s.PlatformGap;
                float bestY = platform.Center.y;
                if (platform.Type == LandingPlatformType.Crumbling)
                {
                    if (best <= gapRadius)
                    {
                        float t = Mathf.Clamp01(best / gapRadius);
                        y -= 1.35f * (1f - t * t * (3f - 2f * t));
                    }
                    return y;
                }
                if (best <= platformRadius)
                {
                    float t = Mathf.Clamp01(best / platformRadius);
                    float eased = t * t * (3f - 2f * t);
                    y = Mathf.Lerp(bestY, y, eased);
                }
                else if (best <= gapRadius)
                {
                    float t = Mathf.Clamp01((best - platformRadius) / s.PlatformGap);
                    float eased = t * t * (3f - 2f * t);
                    float sink = s.PlatformGap * .9f;
                    y = Mathf.Lerp(bestY - sink, y, eased);
                }
            }
            return y;
        }

        public static float RawHeight(MountainSettings s, float x, float z)
        {
            return _heightField != null ? SampleHeightField(s, x, z) : AnalyticHeight(s, x, z);
        }

        private static float AnalyticHeight(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count < 2)
                return Mathf.Lerp(s.ValleyY, s.SummitY, 1f - Mathf.Clamp01(Mathf.Sqrt(x * x + z * z) / s.Radius));

            Vector2 warpOffsetX = SeedOffset(s.Seed, 0x1B873593);
            Vector2 warpOffsetZ = SeedOffset(s.Seed, unchecked((int)0x85EBCA6Bu));
            float warpX = (Mathf.PerlinNoise(x * MountainSettings.WarpScale + warpOffsetX.x, z * MountainSettings.WarpScale + warpOffsetX.y) - .5f) * 2f;
            float warpZ = (Mathf.PerlinNoise(x * MountainSettings.WarpScale + warpOffsetZ.x, z * MountainSettings.WarpScale + warpOffsetZ.y) - .5f) * 2f;
            float wx = x + warpX * s.WarpAmplitude;
            float wz = z + warpZ * s.WarpAmplitude;

            float peakAngle = SeedUnit(s.Seed, 0x165667B1) * Mathf.PI * 2f;
            float peakX = Mathf.Cos(peakAngle) * s.PeakOffset;
            float peakZ = Mathf.Sin(peakAngle) * s.PeakOffset;
            float localX = wx - peakX;
            float localZ = wz - peakZ;
            float axisX = Mathf.Cos(peakAngle + .63f);
            float axisZ = Mathf.Sin(peakAngle + .63f);
            float along = localX * axisX + localZ * axisZ;
            float across = -localX * axisZ + localZ * axisX;
            float stretch = 1f + Mathf.Clamp(s.Stretch, 0f, 1.5f);
            float radial = Mathf.Clamp01(Mathf.Sqrt(along * along / (stretch * stretch) + across * across) / s.Radius);
            float baseY = ProfileHeightAt(s, radial);

            float angle = Mathf.Atan2(localZ, localX);
            float seedPhase = SeedUnit(s.Seed, 0x27D4EB2F) * Mathf.PI * 2f;
            float asym = Mathf.Sin(angle * 3f + seedPhase) * s.Asymmetry
                + Mathf.Sin(angle * 5f + seedPhase * 1.73f) * s.Asymmetry * .5f;
            baseY += asym * Mathf.Clamp01(1f - radial * .6f);

            float p = Mathf.Pow(Mathf.Clamp01(Mathf.InverseLerp(s.ValleyY, s.SummitY, baseY)), 1.25f);
            p = ApplyStrata(s, p, x, z);
            float y = Mathf.Lerp(s.ValleyY, s.SummitY, p);

            float falloff = Mathf.Clamp01(1f - radial * .5f);
            y -= CouloirFactor(s, radial, angle) * falloff;

            float ridged = RidgedFbm(x, z, s.Seed, 4);
            float ridgeMask = Mathf.Lerp(.35f, 1f, Mathf.SmoothStep(.15f, .9f, p));
            y += (ridged - .38f) * (s.SummitY - s.ValleyY) * s.RidgeFraction * ridgeMask * falloff;
            y += (Fbm(x, z, s.Seed + 55, 2) - .5f) * s.NoiseAmplitude * .45f * falloff;

            y += Boulder(s, x, z);

            return y;
        }

        private static void BakeHeightField(MountainSettings s)
        {
            _heightFieldSize = Mathf.Max(2, s.Grid + 1);
            _heightField = new float[_heightFieldSize * _heightFieldSize];
            float range = Mathf.Max(1f, s.SummitY - s.ValleyY);
            float step = (s.Radius * 2f) / (_heightFieldSize - 1);

            for (int zIndex = 0; zIndex < _heightFieldSize; zIndex++)
            {
                float z = -s.Radius + zIndex * step;
                for (int xIndex = 0; xIndex < _heightFieldSize; xIndex++)
                {
                    float x = -s.Radius + xIndex * step;
                    _heightField[zIndex * _heightFieldSize + xIndex] = (AnalyticHeight(s, x, z) - s.ValleyY) / range;
                }
            }

            MountainErosion.Apply(_heightField, _heightFieldSize, step, range, s);
        }

        private static float SampleHeightField(MountainSettings s, float x, float z)
        {
            if (_heightField == null || _heightFieldSize < 2)
                return AnalyticHeight(s, x, z);

            float gx = Mathf.Clamp01((x + s.Radius) / (s.Radius * 2f)) * (_heightFieldSize - 1);
            float gz = Mathf.Clamp01((z + s.Radius) / (s.Radius * 2f)) * (_heightFieldSize - 1);
            int ix = Mathf.Min((int)gx, _heightFieldSize - 2);
            int iz = Mathf.Min((int)gz, _heightFieldSize - 2);
            float tx = gx - ix;
            float tz = gz - iz;
            int index = iz * _heightFieldSize + ix;
            float h00 = _heightField[index];
            float h10 = _heightField[index + 1];
            float h01 = _heightField[index + _heightFieldSize];
            float h11 = _heightField[index + _heightFieldSize + 1];
            float height = tx + tz <= 1f
                ? h00 * (1f - tx - tz) + h10 * tx + h01 * tz
                : h10 * (1f - tz) + h01 * (1f - tx) + h11 * (tx + tz - 1f);
            return s.ValleyY + height * Mathf.Max(1f, s.SummitY - s.ValleyY);
        }

        private static float Boulder(MountainSettings s, float x, float z)
        {
            float total = 0f;
            for (int i = 0; i < 10; i++)
            {
                float cx = _boulderX[i] * s.Radius * .75f;
                float cz = _boulderZ[i] * s.Radius * .75f;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz)) / (s.Radius * _boulderRadius[i]);
                float h = Mathf.SmoothStep(1f, 0f, d);
                float h2 = Mathf.SmoothStep(1f, 0f, d * .55f);
                total += (h * .7f + h2 * .45f) * s.NoiseAmplitude * 2.2f;
            }
            return total;
        }

        private static void EnsureBoulders(int seed)
        {
            if (_boulderSeed == seed) return;
            _boulderSeed = seed;
            var random = new System.Random(StableSeed(seed, 0x165667B1));
            for (int i = 0; i < _boulderX.Length; i++)
            {
                _boulderX[i] = (float)random.NextDouble() * 2f - 1f;
                _boulderZ[i] = (float)random.NextDouble() * 2f - 1f;
                _boulderRadius[i] = Mathf.Lerp(.3f, .65f, (float)random.NextDouble());
            }
        }

        private static Vector2 SeedOffset(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)seed + (uint)salt + 0x9E3779B9u;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                uint y = x * 0x9E3779B9u + 0x85EBCA6Bu;
                y ^= y >> 16;
                float ox = (x & 0xFFFFu) / 65535f * 180f;
                float oy = (y & 0xFFFFu) / 65535f * 180f;
                return new Vector2(ox, oy);
            }
        }

        private static float SeedUnit(int seed, int salt)
        {
            Vector2 offset = SeedOffset(seed, salt);
            return Mathf.Repeat((offset.x * 71f + offset.y * 113f) * .001f, 1f);
        }

        private static int StableSeed(int seed, int salt)
        {
            Vector2 offset = SeedOffset(seed, salt);
            return Mathf.RoundToInt(offset.x * 1000f + offset.y * 997f);
        }

        private static float Fbm(float x, float z, int seed, int octaves)
        {
            float sum = 0f, amp = .5f, freq = 1f;
            Vector2 offset = SeedOffset(seed, unchecked((int)0xB5297A4Du));
            for (int o = 0; o < octaves; o++)
            {
                float n = Mathf.PerlinNoise(x * MountainSettings.NoiseScale * freq + offset.x, z * MountainSettings.NoiseScale * freq + offset.y);
                sum += n * amp;
                freq *= 2f;
                amp *= .5f;
            }
            return sum;
        }

        private static float RidgedFbm(float x, float z, int seed, int octaves)
        {
            float sum = 0f;
            float amplitude = .55f;
            float frequency = 1f;
            float weight = 1f;
            float amplitudeSum = 0f;
            Vector2 offset = SeedOffset(seed, 0x68E31DA4);

            for (int octave = 0; octave < octaves; octave++)
            {
                float n = Mathf.PerlinNoise(x * MountainSettings.NoiseScale * frequency + offset.x,
                    z * MountainSettings.NoiseScale * frequency + offset.y);
                float ridge = 1f - Mathf.Abs(n * 2f - 1f);
                ridge *= ridge;
                ridge *= weight;
                weight = Mathf.Clamp01(ridge * 2.2f);
                sum += ridge * amplitude;
                amplitudeSum += amplitude;
                amplitude *= .5f;
                frequency *= 2.03f;
            }

            return amplitudeSum > 0f ? sum / amplitudeSum : 0f;
        }

        private static float ProfileHeightAt(MountainSettings s, float radial)
        {
            if (_profileRadii == null || _profileRadii.Length == 0) return s.SummitY;
            float r = radial * s.Radius;
            if (r <= _profileRadii[0]) return s.SummitY;
            if (r >= _profileRadii[_profileRadii.Length - 1]) return s.ValleyY;
            for (int i = 0; i < _profileRadii.Length - 1; i++)
            {
                if (r < _profileRadii[i + 1])
                {
                    float t = Mathf.InverseLerp(_profileRadii[i], _profileRadii[i + 1], r);
                    return Mathf.Lerp(_profileHeights[i], _profileHeights[i + 1], t);
                }
            }
            return s.ValleyY;
        }

        private static void FindNearestPlatform(float x, float z, out float dist, out LandingPlatform platform)
        {
            dist = float.MaxValue;
            platform = default;
            if (_route == null || _route.Count == 0) return;
            for (int i = 0; i < _route.Count; i++)
            {
                float dx = x - _route[i].Center.x;
                float dz = z - _route[i].Center.z;
                float d = dx * dx + dz * dz;
                if (d < dist)
                {
                    dist = d;
                    platform = _route[i];
                }
            }
            dist = Mathf.Sqrt(dist);
        }

        private static float CouloirFactor(MountainSettings s, float radial, float angle)
        {
            if (s.CouloirCount <= 0 || radial < 0.34f * .8f) return 0f;
            float strongest = 0f;
            float rotation = SeedUnit(s.Seed, unchecked((int)0xD3A2646Cu)) * Mathf.PI * 2f;
            for (int i = 0; i < s.CouloirCount; i++)
            {
                float target = (i / (float)s.CouloirCount) * Mathf.PI * 2f + rotation;
                float d = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, target * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float falloff = Mathf.Exp(-(d * d) / (2f * MountainSettings.CouloirWidth * MountainSettings.CouloirWidth));
                if (falloff > strongest) strongest = falloff;
            }
            float depth = MountainSettings.CouloirDepth * Mathf.Clamp01((radial - 0.34f * .7f) / (1f - 0.34f * .7f));
            return strongest * depth;
        }

        private static float ApplyStrata(MountainSettings s, float p, float x, float z)
        {
            Vector2 waveOffset = SeedOffset(s.Seed, unchecked((int)0xC2B2AE35u));
            Vector2 maskOffset = SeedOffset(s.Seed, 0x27D4EB2F);
            float wave = (Mathf.PerlinNoise(x * MountainSettings.NoiseScale + waveOffset.x, z * MountainSettings.NoiseScale + waveOffset.y) - .5f) * MountainSettings.StrataWaviness;
            float mask = Mathf.SmoothStep(.5f, .85f, Mathf.PerlinNoise(x * MountainSettings.NoiseScale * .6f + maskOffset.x, z * MountainSettings.NoiseScale * .6f + maskOffset.y));
            float bands = (p + wave) * s.StrataCount;
            float floorBand = Mathf.Floor(bands);
            float frac = bands - floorBand;
            float eased = TerraceStep(frac, MountainSettings.StrataSharpness);
            float stratified = (floorBand + eased) / s.StrataCount;
            float blend = Mathf.Lerp(p, stratified, MountainSettings.StrataStrength);
            return Mathf.Lerp(p, blend, mask);
        }

        private static float TerraceStep(float frac, float sharpness)
        {
            float shelf = Mathf.Clamp01(frac * 1.3f);
            return Mathf.Pow(shelf, sharpness);
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

        public static float PlatformMaskAt(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count < 2) return 0f;
            FindNearestPlatform(x, z, out float best, out LandingPlatform platform);
            float platformRadius = Mathf.Max(.5f, platform.Radius);
            if (best <= platformRadius * .8f) return 1f;
            if (best >= platformRadius) return 0f;
            return Mathf.InverseLerp(platformRadius, platformRadius * .8f, best);
        }

        public static bool TryGetPlatformAt(float x, float z, out LandingPlatform platform)
        {
            FindNearestPlatform(x, z, out float distance, out platform);
            return platform.Radius > 0f && distance <= platform.Radius;
        }

        public static float DistanceToRoute(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count == 0) return float.MaxValue;
            FindNearestPlatform(x, z, out float distance, out _);
            return distance;
        }

        public static bool IsClearOfRoute(MountainSettings s, float x, float z, float footprintRadius)
        {
            if (_route == null || _route.Count == 0) return true;
            FindNearestPlatform(x, z, out float distance, out LandingPlatform platform);
            float radius = Mathf.Max(.5f, platform.Radius);
            return distance > radius + s.PlatformGap + footprintRadius + .5f;
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
            var colors = new List<Color>();

            for (int rz = 0; rz < rows; rz++)
            {
                float z = zStart + rz * step;
                for (int rx = 0; rx < cols; rx++)
                {
                    float x = -s.Radius + rx * step;
                    float y = HeightAt(s, x, z);
                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)rx / s.Grid, Mathf.InverseLerp(s.ValleyY, s.SummitY, y)));
                    float mask = PlatformMaskAt(s, x, z);
                    float edge = Mathf.Clamp01((1f - mask) * 3f);
                    FindNearestPlatform(x, z, out _, out LandingPlatform platform);
                    float type = (int)platform.Type / 5f;
                    colors.Add(new Color(mask, edge, type, platform.Progress));
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

            AddSideWall(verts, uvs, colors, tris, 0, step, rows, cols, floorY, zStart);
            AddSideWall(verts, uvs, colors, tris, cols - 1, step, rows, cols, floorY, zStart);
            AddEndWall(verts, uvs, colors, tris, 0, step, cols, floorY, zStart);
            AddEndWall(verts, uvs, colors, tris, rows - 1, step, cols, floorY, zStart + (rows - 1) * step);

            while (uvs.Count < verts.Count) uvs.Add(Vector2.zero);
            while (colors.Count < verts.Count) colors.Add(new Color(0f, 0f, 0f, 1f));

            var mesh = new Mesh { name = "Procedural Mesa" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSideWall(List<Vector3> verts, List<Vector2> uvs, List<Color> colors, List<int> tris, int col, float step, int rows, int cols, float floorY, float zStart)
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
                colors.Add(Color.black); colors.Add(Color.black);
                colors.Add(Color.black); colors.Add(Color.black);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }

        private static void AddEndWall(List<Vector3> verts, List<Vector2> uvs, List<Color> colors, List<int> tris, int row, float step, int cols, float floorY, float z)
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
                colors.Add(Color.black); colors.Add(Color.black);
                colors.Add(Color.black); colors.Add(Color.black);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }
    }
}
