using UnityEngine;

namespace GoatDescent
{
    public static class TerrainNoise
    {
        public static Vector2 SeedOffset(int seed, int salt)
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

        public static float SeedUnit(int seed, int salt)
        {
            Vector2 offset = SeedOffset(seed, salt);
            return Mathf.Repeat((offset.x * 71f + offset.y * 113f) * .001f, 1f);
        }

        public static int StableSeed(int seed, int salt)
        {
            Vector2 offset = SeedOffset(seed, salt);
            return Mathf.RoundToInt(offset.x * 1000f + offset.y * 997f);
        }

        public static float Fbm(float x, float z, int seed, int octaves)
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

        public static float RidgedFbm(float x, float z, int seed, int octaves)
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
    }
}