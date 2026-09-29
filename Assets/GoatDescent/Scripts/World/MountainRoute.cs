using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public enum LandingPlatformType
    {
        Standard,
        Rest,
        Precise,
        Ice,
        Crumbling,
        Grip
    }

    public struct LandingPlatform
    {
        public Vector3 Center;
        public float Radius;
        public float Progress;
        public int Index;
        public int MergeIndex;
        public LandingPlatformType Type;
        public bool IsBranch;
        public bool IsWindy;
        public bool IsCheckpoint;
    }

    public static class MountainRoute
    {
        private enum Pattern
        {
            Spiral,
            Zigzag,
            Traverse,
            Wall,
            Leap,
            Scree
        }

        public static List<LandingPlatform> Generate(MountainSettings s)
        {
            var platforms = new List<LandingPlatform>();
            if (!s.EnableRoute) return platforms;

            var random = new System.Random(SeedFor(s.Seed, 0x2C1B3C6D));
            float radiusStart = s.Radius * .08f;
            float radiusEnd = s.Radius * .86f;
            float difficulty = Mathf.Clamp01(s.Difficulty);
            float maximumGap = Mathf.Lerp(5.4f, 8.4f, difficulty);
            float maximumDrop = Mathf.Lerp(3.4f, 5f, difficulty);
            int horizontalCount = Mathf.CeilToInt((radiusEnd - radiusStart) / (maximumGap * .65f)) + 1;
            int verticalCount = Mathf.CeilToInt((s.SummitY - s.ValleyY) / maximumDrop) + 1;
            int count = Mathf.Clamp(Mathf.Max(s.PlatformCount, Mathf.Max(horizontalCount, verticalCount)), 6, 200);
            float radialStep = (radiusEnd - radiusStart) / (count - 1);
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            int turnDirection = random.Next(0, 2) == 0 ? -1 : 1;
            float totalDrop = s.SummitY - s.ValleyY;
            int iceStart = count > 9 ? random.Next(Mathf.Max(2, count / 4), Mathf.Max(3, count * 2 / 3)) : -1;
            int sectionStart = 0;
            int sectionLength = 4;
            Pattern pattern = Pattern.Spiral;
            Pattern previousPattern = Pattern.Spiral;

            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)(count - 1);
                float localDifficulty = Mathf.Clamp01(difficulty * .65f + progress * .35f);
                if (i >= sectionStart + sectionLength)
                {
                    previousPattern = pattern;
                    pattern = ChoosePattern(random, previousPattern, localDifficulty);
                    sectionStart = i;
                    sectionLength = random.Next(3, 6);
                }

                float radius = radiusStart + radialStep * i;
                float localMaximumGap = Mathf.Lerp(5.4f, 8.4f, localDifficulty);
                float targetGap = Mathf.Min(localMaximumGap, Mathf.Max(radialStep, s.PlatformStep));
                float factor = PatternGapFactor(pattern);
                float gap = Mathf.Clamp(targetGap * factor * Range(random, .86f, 1.12f), radialStep, localMaximumGap);
                float lateralDistance = Mathf.Sqrt(Mathf.Max(0f, gap * gap - radialStep * radialStep));
                float side;

                switch (pattern)
                {
                    case Pattern.Zigzag:
                        side = (i % 2 == 0 ? -1f : 1f) * turnDirection;
                        break;
                    case Pattern.Traverse:
                        side = ((sectionStart / Mathf.Max(1, sectionLength)) % 2 == 0 ? 1f : -1f) * turnDirection;
                        break;
                    case Pattern.Wall:
                        side = (i % 2 == 0 ? 1f : -1f) * turnDirection;
                        break;
                    case Pattern.Scree:
                        side = turnDirection * Range(random, -.7f, .7f);
                        break;
                    default:
                        side = turnDirection;
                        break;
                }

                float turnScale = pattern == Pattern.Wall ? .68f : pattern == Pattern.Zigzag ? 1.25f : 1f;
                float maximumLateral = Mathf.Sqrt(Mathf.Max(0f, localMaximumGap * localMaximumGap - radialStep * radialStep));
                float turnedLateral = Mathf.Min(lateralDistance * turnScale, maximumLateral);
                angle += side * turnedLateral / Mathf.Max(radius, s.Radius * .12f);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float y = s.SummitY - progress * totalDrop + Range(random, -.18f, .18f) * s.PlatformGap;

                LandingPlatformType type = LandingPlatformType.Standard;
                bool checkpoint = i == 0 || i % 6 == 0;
                bool windy = s.EnableWind && pattern == Pattern.Leap && i > 0 && i < count - 1;

                if (checkpoint)
                    type = LandingPlatformType.Rest;
                else if (s.EnablePlatformTypes)
                {
                    if (pattern == Pattern.Wall) type = LandingPlatformType.Grip;
                    else if (pattern == Pattern.Leap) type = LandingPlatformType.Precise;
                    else if (iceStart >= 0 && i >= iceStart && i <= iceStart + 2) type = LandingPlatformType.Ice;
                    else if (localDifficulty > .3f && random.NextDouble() < .12 + localDifficulty * .08)
                        type = LandingPlatformType.Crumbling;
                    else if (localDifficulty > .55f && random.NextDouble() < .2)
                        type = LandingPlatformType.Precise;
                }

                if (type == LandingPlatformType.Crumbling) checkpoint = false;
                platforms.Add(new LandingPlatform
                {
                    Center = new Vector3(x, y, z),
                    Radius = PlatformRadius(s, type),
                    Progress = progress,
                    Index = i,
                    MergeIndex = i,
                    Type = type,
                    IsBranch = false,
                    IsWindy = windy,
                    IsCheckpoint = checkpoint && type == LandingPlatformType.Rest
                });
            }

            return platforms;
        }

        public static void AdjustForVisibility(MountainSettings s, List<LandingPlatform> mainRoute, Func<float, float, float> heightAt)
        {
            if (mainRoute == null || mainRoute.Count < 2 || heightAt == null) return;

            for (int i = 0; i < mainRoute.Count; i++)
            {
                var current = mainRoute[i];
                current.Center.y = heightAt(current.Center.x, current.Center.z) + PlatformHeightOffset(current.Type);
                if (i == 0)
                {
                    mainRoute[i] = current;
                    continue;
                }

                Vector3 previous = mainRoute[i - 1].Center + Vector3.up * 1.5f;
                float maximumGap = Mathf.Lerp(5.4f, 8.4f, Mathf.Clamp01(s.Difficulty));
                float radial = new Vector2(current.Center.x, current.Center.z).magnitude;
                float previousRadial = new Vector2(previous.x, previous.z).magnitude;
                float baseAngle = Mathf.Atan2(current.Center.z, current.Center.x);
                if (radial > .01f && previousRadial > .01f)
                {
                    float cosineLimit = (previousRadial * previousRadial + radial * radial - maximumGap * maximumGap)
                        / (2f * previousRadial * radial);
                    float allowedAngle = Mathf.Acos(Mathf.Clamp(cosineLimit, -1f, 1f));
                    float previousAngle = Mathf.Atan2(previous.z, previous.x);
                    float angleDelta = Mathf.DeltaAngle(previousAngle * Mathf.Rad2Deg, baseAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    if (Mathf.Abs(angleDelta) > allowedAngle)
                        baseAngle = previousAngle + Mathf.Sign(angleDelta) * allowedAngle;
                    current.Center.x = Mathf.Cos(baseAngle) * radial;
                    current.Center.z = Mathf.Sin(baseAngle) * radial;
                    current.Center.y = heightAt(current.Center.x, current.Center.z) + PlatformHeightOffset(current.Type);
                }

                Vector3 target = current.Center + Vector3.up * 1.4f;
                float originalGap = Vector2.Distance(new Vector2(previous.x, previous.z), new Vector2(target.x, target.z));
                if (originalGap <= maximumGap && IsVisible(previous, target, heightAt, s.Radius))
                {
                    mainRoute[i] = current;
                    continue;
                }

                float angleSearch = Mathf.Clamp(maximumGap / Mathf.Max(radial, 1f) * .45f, .025f, .35f);
                float[] offsets = { 0f, -angleSearch * .25f, angleSearch * .25f, -angleSearch * .55f,
                    angleSearch * .55f, -angleSearch, angleSearch };
                for (int candidate = 0; candidate < offsets.Length; candidate++)
                {
                    float candidateAngle = baseAngle + offsets[candidate];
                    float x = Mathf.Cos(candidateAngle) * radial;
                    float z = Mathf.Sin(candidateAngle) * radial;
                    float candidateGap = Vector2.Distance(new Vector2(previous.x, previous.z), new Vector2(x, z));
                    if (candidateGap > maximumGap) continue;
                    float y = heightAt(x, z) + PlatformHeightOffset(current.Type);
                    Vector3 candidateTarget = new Vector3(x, y + 1.4f, z);
                    if (!IsVisible(previous, candidateTarget, heightAt, s.Radius)) continue;
                    current.Center = new Vector3(x, y, z);
                    break;
                }

                mainRoute[i] = current;
            }
        }

        public static void AddBranches(MountainSettings s, List<LandingPlatform> platforms)
        {
            if (!s.EnableBranches || platforms == null || platforms.Count < 12) return;
            var random = new System.Random(SeedFor(s.Seed, 0x6A09E667));
            int mainCount = platforms.Count;
            int nextBranchAt = random.Next(5, 8);

            while (nextBranchAt < mainCount - 4)
            {
                int mergeIndex = Mathf.Min(mainCount - 1, nextBranchAt + 3);
                LandingPlatform start = platforms[nextBranchAt];
                LandingPlatform end = platforms[mergeIndex];
                Vector3 direction = end.Center - start.Center;
                Vector3 side = new Vector3(-direction.z, 0f, direction.x).normalized;
                float sign = random.Next(0, 2) == 0 ? -1f : 1f;
                float offset = Mathf.Lerp(3f, 6.2f, Mathf.Clamp01(s.Difficulty)) * Range(random, .88f, 1.12f) * sign;
                float maximumGap = Mathf.Lerp(5.4f, 8.4f, Mathf.Clamp01(s.Difficulty));
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    Vector3 first = BranchPosition(start.Center, end.Center, side, offset, 1f / 3f);
                    Vector3 second = BranchPosition(start.Center, end.Center, side, offset, 2f / 3f);
                    float firstGap = HorizontalDistance(start.Center, first);
                    float middleGap = HorizontalDistance(first, second);
                    float lastGap = HorizontalDistance(second, end.Center);
                    if (Mathf.Max(firstGap, Mathf.Max(middleGap, lastGap)) <= maximumGap) break;
                    offset *= .65f;
                }

                for (int point = 1; point <= 2; point++)
                {
                    float t = point / 3f;
                    Vector3 position = BranchPosition(start.Center, end.Center, side, offset, t);
                    position.y += Mathf.Lerp(.4f, 1.1f, Mathf.Clamp01(s.Difficulty));
                    platforms.Add(new LandingPlatform
                    {
                        Center = position,
                        Radius = Mathf.Max(1.1f, s.PlatformSize * .34f),
                        Progress = Mathf.Lerp(start.Progress, end.Progress, t),
                        Index = -1,
                        MergeIndex = mergeIndex,
                        Type = !s.EnablePlatformTypes ? LandingPlatformType.Standard
                            : point == 2 && s.Difficulty > .65f ? LandingPlatformType.Crumbling : LandingPlatformType.Precise,
                        IsBranch = true,
                        IsWindy = s.EnableWind && s.Difficulty > .75f,
                        IsCheckpoint = false
                    });
                }

                nextBranchAt += random.Next(8, 12);
            }
        }

        public static float PlatformHeightOffset(LandingPlatformType type)
        {
            return type == LandingPlatformType.Crumbling ? .15f : 1.2f;
        }

        private static Pattern ChoosePattern(System.Random random, Pattern previous, float difficulty)
        {
            var allowed = new List<Pattern> { Pattern.Spiral, Pattern.Zigzag, Pattern.Traverse, Pattern.Scree };
            if (difficulty > .2f) allowed.Add(Pattern.Wall);
            if (difficulty > .25f) allowed.Add(Pattern.Leap);
            Pattern selected = previous;
            for (int attempt = 0; attempt < 6 && selected == previous; attempt++)
                selected = allowed[random.Next(allowed.Count)];
            return selected;
        }

        private static float PatternGapFactor(Pattern pattern)
        {
            switch (pattern)
            {
                case Pattern.Wall: return .68f;
                case Pattern.Leap: return 1.28f;
                case Pattern.Scree: return .82f;
                case Pattern.Traverse: return .92f;
                default: return 1f;
            }
        }

        private static float PlatformRadius(MountainSettings s, LandingPlatformType type)
        {
            float radius = Mathf.Max(1f, s.PlatformSize * .5f);
            switch (type)
            {
                case LandingPlatformType.Rest: return radius * 1.45f;
                case LandingPlatformType.Precise: return radius * .68f;
                case LandingPlatformType.Ice: return radius * .92f;
                case LandingPlatformType.Crumbling: return radius * .9f;
                case LandingPlatformType.Grip: return radius * .82f;
                default: return radius;
            }
        }

        private static bool IsVisible(Vector3 from, Vector3 to, Func<float, float, float> heightAt, float radius)
        {
            float horizontalDistance = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
            int samples = Mathf.Clamp(Mathf.CeilToInt(horizontalDistance / Mathf.Max(1f, radius * .035f)), 4, 24);
            for (int i = 1; i < samples; i++)
            {
                float t = i / (float)samples;
                Vector3 point = Vector3.Lerp(from, to, t);
                if (heightAt(point.x, point.z) > point.y - .35f) return false;
            }
            return true;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static Vector3 BranchPosition(Vector3 start, Vector3 end, Vector3 side, float offset, float t)
        {
            Vector3 position = Vector3.Lerp(start, end, t);
            return position + side * offset * Mathf.Sin(t * Mathf.PI);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static int SeedFor(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)seed + (uint)salt + 0x9E3779B9u;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return (int)x;
            }
        }
    }
}
