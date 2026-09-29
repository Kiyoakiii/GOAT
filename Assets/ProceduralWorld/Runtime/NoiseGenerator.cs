using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public static class NoiseGenerator
    {
        public static float Fractal(float x, float z, int seed, float frequency, int octaves, float persistence = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float normalizer = 0f;
            float currentFrequency = frequency;
            for (int octave = 0; octave < octaves; octave++)
            {
                float offsetX = Hash01(seed + octave * 17) * 8192f;
                float offsetZ = Hash01(seed + octave * 29) * 8192f;
                total += Mathf.PerlinNoise(x * currentFrequency + offsetX, z * currentFrequency + offsetZ) * amplitude;
                normalizer += amplitude;
                amplitude *= persistence;
                currentFrequency *= 2f;
            }
            return total / Mathf.Max(0.0001f, normalizer);
        }

        public static float Ridged(float x, float z, int seed, float frequency, int octaves)
        {
            float value = Fractal(x, z, seed, frequency, octaves, 0.54f);
            return Mathf.Pow(1f - Mathf.Abs(value * 2f - 1f), 1.55f);
        }

        public static Vector2 DomainWarp(float x, float z, int seed, float frequency, float amplitude)
        {
            return new Vector2(
                (Fractal(x, z, seed + 101, frequency, 3) - 0.5f) * amplitude,
                (Fractal(x, z, seed + 211, frequency, 3) - 0.5f) * amplitude);
        }

        public static float Hash01(int value)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return (hash & 0x00ffffff) / 16777215f;
            }
        }

        public static int DeriveSeed(int seed, int x, int z, int salt)
        {
            unchecked
            {
                int value = seed;
                value = value * 486187739 + x * 196613;
                value = value * 486187739 + z * 83492791;
                return value ^ salt;
            }
        }

        public static float SmoothMask(float value, float low, float high)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(low, high, value));
        }
    }
}
