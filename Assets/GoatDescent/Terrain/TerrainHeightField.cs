using UnityEngine;

namespace GoatDescent
{
    public static class TerrainHeightField
    {
        private static float[] _profileRadii;
        private static float[] _profileHeights;
        private static float[] _heightField;
        private static int _heightFieldSize;
        private static readonly float[] _boulderX = new float[10];
        private static readonly float[] _boulderZ = new float[10];
        private static readonly float[] _boulderRadius = new float[10];
        private static int _boulderSeed = int.MinValue;

        internal static void SetProfile(float[] radii, float[] heights)
        {
            _profileRadii = radii;
            _profileHeights = heights;
        }

        internal static void BakeHeightField(MountainSettings s)
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

        public static float RawHeight(MountainSettings s, float x, float z)
        {
            return _heightField != null ? SampleHeightField(s, x, z) : AnalyticHeight(s, x, z);
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

        private static float HeightAtVertex(MountainSettings s, float x, float z)
        {
            float y = RawHeight(s, x, z);
            if (RouteQueries.CurrentRoute != null && RouteQueries.CurrentRoute.Count >= 2)
            {
                RouteQueries.FindNearestPlatform(x, z, out float best, out LandingPlatform platform);
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

        internal static float AnalyticHeight(MountainSettings s, float x, float z)
        {
            if (RouteQueries.CurrentRoute == null || RouteQueries.CurrentRoute.Count < 2)
                return Mathf.Lerp(s.ValleyY, s.SummitY, 1f - Mathf.Clamp01(Mathf.Sqrt(x * x + z * z) / s.Radius));

            Vector2 warpOffsetX = TerrainNoise.SeedOffset(s.Seed, 0x1B873593);
            Vector2 warpOffsetZ = TerrainNoise.SeedOffset(s.Seed, unchecked((int)0x85EBCA6Bu));
            float warpX = (Mathf.PerlinNoise(x * MountainSettings.WarpScale + warpOffsetX.x, z * MountainSettings.WarpScale + warpOffsetX.y) - .5f) * 2f;
            float warpZ = (Mathf.PerlinNoise(x * MountainSettings.WarpScale + warpOffsetZ.x, z * MountainSettings.WarpScale + warpOffsetZ.y) - .5f) * 2f;
            float wx = x + warpX * s.WarpAmplitude;
            float wz = z + warpZ * s.WarpAmplitude;

            float peakAngle = TerrainNoise.SeedUnit(s.Seed, 0x165667B1) * Mathf.PI * 2f;
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
            float seedPhase = TerrainNoise.SeedUnit(s.Seed, 0x27D4EB2F) * Mathf.PI * 2f;
            float asym = Mathf.Sin(angle * 3f + seedPhase) * s.Asymmetry
                + Mathf.Sin(angle * 5f + seedPhase * 1.73f) * s.Asymmetry * .5f;
            baseY += asym * Mathf.Clamp01(1f - radial * .6f);

            float p = Mathf.Pow(Mathf.Clamp01(Mathf.InverseLerp(s.ValleyY, s.SummitY, baseY)), 1.25f);
            p = ApplyStrata(s, p, x, z);
            float y = Mathf.Lerp(s.ValleyY, s.SummitY, p);

            float falloff = Mathf.Clamp01(1f - radial * .5f);
            y -= CouloirFactor(s, radial, angle) * falloff;

            float ridged = TerrainNoise.RidgedFbm(x, z, s.Seed, 4);
            float ridgeMask = Mathf.Lerp(.35f, 1f, Mathf.SmoothStep(.15f, .9f, p));
            y += (ridged - .38f) * (s.SummitY - s.ValleyY) * s.RidgeFraction * ridgeMask * falloff;
            y += (TerrainNoise.Fbm(x, z, s.Seed + 55, 2) - .5f) * s.NoiseAmplitude * .45f * falloff;

            y += Boulder(s, x, z);

            return y;
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

        internal static void EnsureBoulders(int seed)
        {
            if (_boulderSeed == seed) return;
            _boulderSeed = seed;
            var random = new System.Random(TerrainNoise.StableSeed(seed, 0x165667B1));
            for (int i = 0; i < _boulderX.Length; i++)
            {
                _boulderX[i] = (float)random.NextDouble() * 2f - 1f;
                _boulderZ[i] = (float)random.NextDouble() * 2f - 1f;
                _boulderRadius[i] = Mathf.Lerp(.3f, .65f, (float)random.NextDouble());
            }
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

        private static float CouloirFactor(MountainSettings s, float radial, float angle)
        {
            if (s.CouloirCount <= 0 || radial < 0.34f * .8f) return 0f;
            float strongest = 0f;
            float rotation = TerrainNoise.SeedUnit(s.Seed, unchecked((int)0xD3A2646Cu)) * Mathf.PI * 2f;
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
            Vector2 waveOffset = TerrainNoise.SeedOffset(s.Seed, unchecked((int)0xC2B2AE35u));
            Vector2 maskOffset = TerrainNoise.SeedOffset(s.Seed, 0x27D4EB2F);
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
    }
}