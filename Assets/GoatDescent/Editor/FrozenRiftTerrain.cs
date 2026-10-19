using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class FrozenRiftTerrain
    {
        public const float Extent = 1450f;
        public const int Grid = 200;
        public const int Tiers = 4;
        public const float RingOuter = 3000f;

        private const float TerraceStep = 76f;
        private const float FaceStart = .42f;
        private const float RockSlope = .68f;

        public static float Channel(float z) =>
            Mathf.Sin(z * .0042f) * 150f + Mathf.Sin(z * .011f + 1.2f) * 60f + Mathf.Sin(z * .024f) * 20f;

        public static float HalfWidth(float z) =>
            (438f + Mathf.Sin(z * .0061f + .7f) * 52f + Mathf.Sin(z * .017f) * 18f) * Mathf.Lerp(1f, .5f, EndMask(z));

        public static float BedWidth(float z) =>
            (74f + Mathf.Sin(z * .0091f) * 14f) * Mathf.Lerp(1f, .35f, EndMask(z));

        public static float RimHeight(float z) =>
            208f + Mathf.Sin(z * .0053f + .4f) * 32f + Mathf.Sin(z * .0141f) * 12f + 112f * EndMask(z);

        public static float BedLevel(float z) =>
            Mathf.Lerp(-208f + Mathf.Sin(z * .0067f) * 22f, RimHeight(z) - 30f, EndMask(z));

        private static float EndMask(float z) => Mathf.SmoothStep(0f, .3f, Mathf.InverseLerp(960f, 1420f, Mathf.Abs(z)));

        public static float RimX(float z, int side) => Channel(z) + side * HalfWidth(z);

        public static float TierY(float z, int tier) =>
            Mathf.Lerp(BedLevel(z), RimHeight(z), (tier + .72f) / Tiers);

        public static float TierFaceX(float z, int side, int tier) =>
            Channel(z) + side * (BedWidth(z) + (HalfWidth(z) - BedWidth(z)) * (tier + .74f) / Tiers);

        public static float TierShelfX(float z, int side, int tier, float t) =>
            Channel(z) + side * (BedWidth(z) + (HalfWidth(z) - BedWidth(z)) * (tier + .04f + .3f * t) / Tiers);

        public static float BedX(float z, float t) => Channel(z) + t * BedWidth(z) * .86f;

        private static float Platform(float distance, float z)
        {
            float half = HalfWidth(z);
            float u = distance - half;
            if (u <= 0f)
            {
                float bed = BedLevel(z), bedHalf = BedWidth(z);
                if (distance <= bedHalf) return bed + BedNoise(distance, z);
                float wander = (Mathf.PerlinNoise(z * .0045f + 13f, distance * .0021f + 7f) - .5f) * .62f;
                float v = Mathf.Clamp01((distance - bedHalf) / Mathf.Max(1f, half - bedHalf)) * Tiers + wander;
                float tier = Mathf.Floor(v);
                float rise = (tier + Mathf.SmoothStep(FaceStart, 1f, Mathf.Clamp01(v - tier))) / Tiers;
                float swell = (Mathf.PerlinNoise(z * .008f + 31f, v * .9f) - .5f) * 9f;
                return Mathf.Lerp(bed, RimHeight(z), rise) + swell * Mathf.SmoothStep(.25f, .8f, Mathf.Abs(Mathf.Clamp01(v - tier) * 2f - 1f));
            }
            float baseHeight = RimHeight(z) + Mathf.Min(u * .17f, 62f) + Mathf.Max(0f, u - 560f) * .015f;
            float noise = (Mathf.PerlinNoise(u * .0075f + 4f, z * .006f + 7f) - .5f) * 17f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 70f, u));
            return baseHeight + noise;
        }

        private static float BedNoise(float distance, float z) =>
            (Mathf.PerlinNoise(distance * .009f + 3f, z * .009f + 5f) - .5f) * 9f;

        public static float Height(float x, float z)
        {
            float distance = Mathf.Abs(x - Channel(z));
            float raw = Platform(distance, z);
            float u = distance - HalfWidth(z);
            if (u <= 0f) return raw;
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 110f, u)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(860f, 1040f, u)));
            if (weight <= .01f) return raw;
            float phase = (Mathf.PerlinNoise(x * .0021f + 11f, z * .0021f + 29f) - .5f) * 12f * weight;
            float level = (raw + phase) / TerraceStep;
            float tier = Mathf.Floor(level);
            float surface = Mathf.Lerp(raw, (tier + Mathf.SmoothStep(.8f, 1f, level - tier)) * TerraceStep - phase, weight);
            return surface + Drift(x, z, u);
        }

        private static float Drift(float x, float z, float u)
        {
            float amount = Mathf.SmoothStep(14f, 80f, u) * (1f - Mathf.SmoothStep(620f, 800f, u));
            if (amount <= .01f) return 0f;
            float along = x * .94f + z * .34f;
            float wave = Mathf.Sin(along * .055f);
            float lee = Mathf.Clamp01((x - Channel(z)) * .0018f + .5f);
            float patch = .55f + .9f * Mathf.PerlinNoise(x * .0055f + 5f, z * .0082f + 9f);
            float amplitude = Mathf.Min(8f, (3.2f + 4.8f * Mathf.PerlinNoise(x * .0092f + 5f, z * .0128f + 9f)) * (.42f + .58f * lee) * patch);
            return wave * amplitude * amount;
        }

        public static float Slope(float x, float z)
        {
            const float e = 9f;
            float dx = Mathf.Abs(Height(x + e, z) - Height(x - e, z));
            float dz = Mathf.Abs(Height(x, z + e) - Height(x, z - e));
            return Mathf.Max(dx, dz) / (2f * e);
        }

        private static bool IsRock(float x, float z) =>
            Slope(x, z) > RockSlope && Mathf.Abs(x - Channel(z)) < HalfWidth(z) + 80f;

        private static bool IsShelf(float x, float z) => x * x + z * z > 950f * 950f;

        private static bool IsBed(float x, float z) => Mathf.Abs(x - Channel(z)) <= BedWidth(z) * .92f;

        private static float Hash(int i, int j, int salt)
        {
            float value = Mathf.Sin((i * 127.1f + j * 311.7f + salt * 74.7f) * .017f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        public static void Build(out Mesh rock, out Mesh snow, out Mesh bed)
        {
            float cell = Extent * 2f / Grid;
            var positions = new Vector3[Grid + 1, Grid + 1];
            for (int j = 0; j <= Grid; j++)
                for (int i = 0; i <= Grid; i++)
                {
                    float x = -Extent + i * cell, z = -Extent + j * cell;
                    float edge = i == 0 || i == Grid || j == 0 || j == Grid ? 0f : 1f;
                    float jx = (Hash(i, j, 1) - .5f) * 9f * edge, jz = (Hash(i, j, 2) - .5f) * 9f * edge;
                    float jy = (Hash(i, j, 3) - .5f) * 1.8f * edge;
                    positions[i, j] = new Vector3(x + jx, Height(x + jx, z + jz) + jy, z + jz);
                }
            var kind = new byte[Grid, Grid];
            for (int j = 0; j < Grid; j++)
                for (int i = 0; i < Grid; i++)
                {
                    float x = (positions[i, j].x + positions[i + 1, j + 1].x) * .5f;
                    float z = (positions[i, j].z + positions[i + 1, j + 1].z) * .5f;
                    kind[i, j] = IsBed(x, z) ? (byte)2 : IsRock(x, z) && !IsShelf(x, z) ? (byte)0 : (byte)1;
                }
            var rockBatch = new FrozenRiftBatch(false);
            var snowBatch = new FrozenRiftBatch(false);
            var bedBatch = new FrozenRiftBatch(false);
            for (int j = 0; j < Grid; j++)
                for (int i = 0; i < Grid; i++)
                {
                    Vector3 a = positions[i, j], b = positions[i, j + 1], c = positions[i + 1, j + 1], d = positions[i + 1, j];
                    switch (kind[i, j])
                    {
                        case 1:
                            snowBatch.Quad(a, b, c, d);
                            break;
                        case 2: bedBatch.Quad(a, b, c, d); break;
                        default: rockBatch.Quad(a, b, c, d); break;
                    }
                }
            rock = rockBatch.Build("Скальные стены и ярусы разлома");
            snow = snowBatch.Build("Снежные плато и карнизы");
            bed = bedBatch.Build("Ледяное дно разлома");
        }

        public static void Ring(out Mesh snow, out Mesh rock)
        {
            const int segments = 256;
            const int steps = 10;
            var snowBatch = new FrozenRiftBatch(false);
            var rockBatch = new FrozenRiftBatch(false);
            const float delta = Mathf.PI * 2f / segments;
            Vector3 Point(float angle, int step)
            {
                float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                float inner = Extent / Mathf.Max(Mathf.Abs(cosine), Mathf.Abs(sine));
                float t = step / (float)steps;
                float radius = Mathf.Lerp(inner - 32f, RingOuter, t);
                float x = radius * cosine, z = radius * sine;
                float scale = inner / radius;
                float baseHeight = (BaseAt(angle - delta, radius, scale) + BaseAt(angle, radius, scale)
                    + BaseAt(angle + delta, radius, scale)) / 3f + 6f;
                float top = Mathf.Lerp(baseHeight + 8f, 445f, t);
                float taper = Mathf.Lerp(1f, .5f, Mathf.Clamp01((t - .25f) / .75f));
                float ridges = (Mathf.PerlinNoise(angle * 8.3f + 3.1f, 1.7f) - .5f) * 126f
                    + (Mathf.PerlinNoise(angle * 17.7f + 11.3f, 3.3f) - .5f) * 50f
                    + (Mathf.PerlinNoise(x * .0022f + 13f, z * .0022f + 17f) - .5f) * 56f;
                float skirt = 3f * Mathf.Clamp01((1.5f - step) / 1.5f);
                return new Vector3(x, top + ridges * taper + skirt, z);
            }
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                for (int step = 0; step < steps; step++)
                {
                    Vector3 a = Point(a0, step), b = Point(a0, step + 1), c = Point(a1, step + 1), d = Point(a1, step);
                    float low = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
                    float high = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
                    float span = Mathf.Max(Vector3.Distance(a, b), Vector3.Distance(b, c));
                    float rib = Mathf.PerlinNoise((a0 + a1) * 2.9f + 5f, 8.2f);
                    FrozenRiftBatch target = high - low > span * .42f && rib > .68f ? rockBatch : snowBatch;
                    target.Quad(a, b, c, d);
                }
            }
            snow = snowBatch.Build("Снежные хребты горизонта");
            rock = rockBatch.Build("Скальные рёбра дальних хребтов");
        }

        private static float BaseAt(float angle, float radius, float scale) =>
            Height(radius * Mathf.Cos(angle) * scale, radius * Mathf.Sin(angle) * scale);
    }
}
