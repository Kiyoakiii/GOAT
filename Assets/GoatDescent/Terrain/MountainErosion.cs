using System;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainErosion
    {
        private const int DropletLifetime = 32;
        private const float Inertia = 0.08f;
        private const float SedimentCapacity = 3.2f;
        private const float MinimumCapacity = 0.004f;
        private const float DepositSpeed = 0.3f;
        private const float ErodeSpeed = 0.3f;
        private const float EvaporateSpeed = 0.025f;
        private const int TalusPasses = 7;

        public static void Apply(float[] heights, int size, float cellSize, float heightRange, MountainSettings settings)
        {
            if (heights == null || size < 3 || settings == null || heightRange <= 0f) return;

            if (settings.ErosionStrength > 0f)
                Hydraulic(heights, size, settings.Seed, settings.ErosionStrength);
            if (settings.TalusStrength > 0f)
                Thermal(heights, size, cellSize, heightRange, settings.TalusAngle, settings.TalusStrength);

            for (int i = 0; i < heights.Length; i++)
                heights[i] = Mathf.Clamp(heights[i], -0.05f, 1.05f);
        }

        private static void Hydraulic(float[] map, int size, int seed, float strength)
        {
            var random = new System.Random(seed ^ 0x51ED270B);
            int drops = Mathf.Clamp(size * size, 12000, 110000);
            float erodeScale = Mathf.Clamp(strength, 0f, 3f);

            for (int drop = 0; drop < drops; drop++)
            {
                float x = 2f + (float)random.NextDouble() * (size - 5f);
                float y = 2f + (float)random.NextDouble() * (size - 5f);
                float directionX = 0f;
                float directionY = 0f;
                float speed = 1f;
                float water = 1f;
                float sediment = 0f;

                for (int life = 0; life < DropletLifetime; life++)
                {
                    int cellX = (int)x;
                    int cellY = (int)y;
                    if (cellX < 1 || cellY < 1 || cellX >= size - 2 || cellY >= size - 2) break;

                    float offsetX = x - cellX;
                    float offsetY = y - cellY;
                    int index = cellY * size + cellX;
                    float oldHeight = Sample(map, size, x, y);
                    Gradient(map, size, x, y, out float gradientX, out float gradientY);

                    directionX = directionX * Inertia - gradientX * (1f - Inertia);
                    directionY = directionY * Inertia - gradientY * (1f - Inertia);
                    float directionLength = Mathf.Sqrt(directionX * directionX + directionY * directionY);
                    if (directionLength < 0.0001f) break;
                    directionX /= directionLength;
                    directionY /= directionLength;

                    float nextX = x + directionX;
                    float nextY = y + directionY;
                    if (nextX < 1f || nextY < 1f || nextX >= size - 2f || nextY >= size - 2f) break;

                    float newHeight = Sample(map, size, nextX, nextY);
                    float deltaHeight = newHeight - oldHeight;
                    float capacity = Mathf.Max(-deltaHeight, MinimumCapacity) * speed * water * SedimentCapacity;

                    if (sediment > capacity || deltaHeight > 0f)
                    {
                        float amount = deltaHeight > 0f
                            ? Mathf.Min(deltaHeight, sediment)
                            : (sediment - capacity) * DepositSpeed;
                        amount = Mathf.Max(0f, amount);
                        sediment -= amount;
                        Deposit(map, size, cellX, cellY, offsetX, offsetY, amount);
                    }
                    else
                    {
                        float amount = Mathf.Min((capacity - sediment) * ErodeSpeed * erodeScale, -deltaHeight);
                        amount = Mathf.Max(0f, amount);
                        Erode(map, size, cellX, cellY, offsetX, offsetY, amount);
                        sediment += amount;
                    }

                    speed = Mathf.Sqrt(Mathf.Max(0.01f, speed * speed - deltaHeight * 4f));
                    water *= 1f - EvaporateSpeed;
                    x = nextX;
                    y = nextY;
                    if (water < 0.05f) break;
                }
            }
        }

        private static void Thermal(float[] map, int size, float cellSize, float heightRange, float talusAngle, float strength)
        {
            float threshold = Mathf.Tan(Mathf.Clamp(talusAngle, 20f, 60f) * Mathf.Deg2Rad) * cellSize / heightRange;
            float transferFactor = 0.28f * Mathf.Clamp(strength, 0f, 3f);
            var delta = new float[map.Length];

            for (int pass = 0; pass < TalusPasses; pass++)
            {
                Array.Clear(delta, 0, delta.Length);
                for (int y = 2; y < size - 3; y++)
                {
                    for (int x = 2; x < size - 3; x++)
                    {
                        int index = y * size + x;
                        Transfer(index, index + 1, map, delta, threshold, transferFactor);
                        Transfer(index, index + size, map, delta, threshold, transferFactor);
                    }
                }

                for (int i = 0; i < map.Length; i++)
                    map[i] += delta[i];
            }
        }

        private static void Transfer(int a, int b, float[] map, float[] delta, float threshold, float factor)
        {
            float difference = map[a] - map[b];
            float excess = Mathf.Abs(difference) - threshold;
            if (excess <= 0f) return;
            float amount = excess * factor * 0.5f;
            if (difference > 0f)
            {
                delta[a] -= amount;
                delta[b] += amount;
            }
            else
            {
                delta[a] += amount;
                delta[b] -= amount;
            }
        }

        private static void Gradient(float[] map, int size, float x, float y, out float gx, out float gy)
        {
            int cellX = (int)x;
            int cellY = (int)y;
            float tx = x - cellX;
            float ty = y - cellY;
            int i = cellY * size + cellX;
            float h00 = map[i];
            float h10 = map[i + 1];
            float h01 = map[i + size];
            float h11 = map[i + size + 1];
            gx = (h10 - h00) * (1f - ty) + (h11 - h01) * ty;
            gy = (h01 - h00) * (1f - tx) + (h11 - h10) * tx;
        }

        private static float Sample(float[] map, int size, float x, float y)
        {
            int cellX = Mathf.Clamp((int)x, 0, size - 2);
            int cellY = Mathf.Clamp((int)y, 0, size - 2);
            float tx = x - cellX;
            float ty = y - cellY;
            int i = cellY * size + cellX;
            float a = Mathf.Lerp(map[i], map[i + 1], tx);
            float b = Mathf.Lerp(map[i + size], map[i + size + 1], tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static void Deposit(float[] map, int size, int x, int y, float tx, float ty, float amount)
        {
            int i = y * size + x;
            map[i] += amount * (1f - tx) * (1f - ty);
            map[i + 1] += amount * tx * (1f - ty);
            map[i + size] += amount * (1f - tx) * ty;
            map[i + size + 1] += amount * tx * ty;
        }

        private static void Erode(float[] map, int size, int x, int y, float tx, float ty, float amount)
        {
            int i = y * size + x;
            map[i] -= amount * (1f - tx) * (1f - ty);
            map[i + 1] -= amount * tx * (1f - ty);
            map[i + size] -= amount * (1f - tx) * ty;
            map[i + size + 1] -= amount * tx * ty;
        }
    }
}
