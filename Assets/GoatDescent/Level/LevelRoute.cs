using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.Levels
{
    public enum RouteConnection { Walk, Jump }

    public sealed class LevelRoute
    {
        public readonly LandingPlatform[] Platforms;
        public readonly int[] Episodes;
        public readonly RouteConnection[] Connections;
        public readonly int[] Sections;
        public readonly string Signature;
        public readonly float TargetSeconds;
        public Vector3 Spawn => Platforms[0].Center + Vector3.up * 1.5f;
        public Vector3 Finish => Platforms[Platforms.Length - 1].Center;
        public LevelRoute(LevelRecipe recipe, LandingPlatform[] platforms, int[] episodes, RouteConnection[] connections, int[] sections = null)
        {
            Sections = sections ?? new int[platforms.Length];
            Platforms = platforms; Episodes = episodes; Connections = connections; TargetSeconds = recipe.TargetSeconds;
            uint hash = 2166136261u;
            hash = Fold(hash, LevelRecipe.GeneratorVersion);
            hash = Fold(hash, recipe.Seed);
            hash = Fold(hash, (int)recipe.Form);
            hash = Fold(hash, Mathf.RoundToInt(recipe.Radius * 1000f));
            hash = Fold(hash, Mathf.RoundToInt(recipe.SummitHeight * 1000f));
            hash = Fold(hash, Mathf.RoundToInt(recipe.ValleyHeight * 1000f));
            foreach (var episode in recipe.Episodes)
            {
                hash = Fold(hash, (int)episode.Challenge);
                hash = Fold(hash, Mathf.RoundToInt(episode.TargetSeconds * 1000f));
                hash = Fold(hash, Mathf.RoundToInt(episode.Difficulty * 1000f));
            }
            string recipeData = recipe.LevelName + (recipe.Biome ? JsonUtility.ToJson(recipe.Biome) : "");
            foreach (char c in recipeData) hash = unchecked((hash ^ c) * 16777619u);
            foreach (var point in platforms)
            {
                hash = Fold(hash, Mathf.RoundToInt(point.Center.x * 1000f));
                hash = Fold(hash, Mathf.RoundToInt(point.Center.y * 1000f));
                hash = Fold(hash, Mathf.RoundToInt(point.Center.z * 1000f));
                hash = Fold(hash, (int)point.Type);
                hash = Fold(hash, Mathf.RoundToInt(point.Radius * 1000f));
            }
            for (int i = 0; i < connections.Length; i++) hash = Fold(hash, (int)connections[i]);
            foreach (int section in Sections) hash = Fold(hash, section);
            Signature = LevelRecipe.GeneratorVersion + ":" + platforms.Length + ":" + hash.ToString("X8");
        }
        private static uint Fold(uint hash, int value) => unchecked((hash ^ (uint)value) * 16777619u);
    }

    public static class LevelRouteGenerator
    {
        private const float SecondsPerSupport = 2.6f;
        public static LevelRoute Generate(LevelRecipe recipe)
        {
            if (!recipe) throw new ArgumentNullException(nameof(recipe));
            if (recipe.Form != LevelForm.StoneCrown)
                throw new NotSupportedException("Сначала требуется реализация формы " + recipe.Form);
            if (recipe.Episodes == null || recipe.Episodes.Length == 0 || recipe.SummitHeight <= recipe.ValleyHeight)
                throw new ArgumentException("Рецепту нужны эпизоды и положительная высота спуска.");
            var counts = new int[recipe.Episodes.Length];
            int total = 0;
            for (int e = 0; e < counts.Length; e++)
            {
                if (recipe.Episodes[e] == null || recipe.Episodes[e].TargetSeconds < 1f)
                    throw new ArgumentException("Некорректный эпизод " + e);
                counts[e] = Mathf.Max(6, Mathf.CeilToInt(recipe.Episodes[e].TargetSeconds / SecondsPerSupport));
                total += counts[e];
            }
            total = Mathf.Max(total, Mathf.CeilToInt((recipe.SummitHeight - recipe.ValleyHeight) / 1.9f) + 1);
            counts[counts.Length - 1] += total - Sum(counts);
            float totalWeight = 0f;
            for (int e = 0; e < counts.Length; e++)
                totalWeight += Weight(recipe.Episodes[e].Challenge) * counts[e];
            totalWeight -= Weight(recipe.Episodes[0].Challenge);
            float descentUnit = (recipe.SummitHeight - recipe.ValleyHeight) / totalWeight;
            float height = recipe.SummitHeight;
            var random = new System.Random(recipe.SeedFor(1));
            var platforms = new LandingPlatform[total];
            var episodes = new int[total];
            var connections = new RouteConnection[total - 1];
            var sections = new int[total];
            float span = recipe.Radius * .43f;
            float x = -span;
            float z = -recipe.Radius * .08f;
            float direction = 1f;
            int turnSteps = 0, section = 0, index = 0;
            float rowOffset = Mathf.Lerp(-3f, 3f, (float)random.NextDouble());
            for (int e = 0; e < counts.Length; e++)
            {
                LevelEpisode episode = recipe.Episodes[e];
                for (int local = 0; local < counts[e]; local++, index++)
                {
                    float progress = index / (float)(total - 1);
                    bool turning = turnSteps > 0;
                    if (index > 0)
                    {
                        if (turning)
                        {
                            z -= 6.1f;
                            turnSteps--;
                            if (turnSteps == 0)
                            {
                                direction = -direction; section++;
                                span = recipe.Radius * Mathf.Lerp(.36f, .48f, (float)random.NextDouble());
                            }
                        }
                        else
                        {
                            float spacing = Mathf.Lerp(5.7f, 6.7f, episode.Difficulty)
                                * Mathf.Lerp(.94f, 1.04f, (float)random.NextDouble());
                            x += direction * spacing;
                            if (x * direction >= span) turnSteps = 4;
                        }
                    }
                    float step = descentUnit * Weight(episode.Challenge);
                    if (index > 0) height -= step;
                    float climb = 0f;
                    if (episode.Challenge == LevelChallenge.Climbing && local > 0 && local < counts[e] - 2
                        && !turning && turnSteps == 0 && Mathf.Abs(x) < span - 12f)
                    {
                        float amplitude = Mathf.Clamp(2f * (2.5f - step), 0f, 1.6f);
                        climb = local % 4 == 1 ? amplitude : local % 4 == 2 ? amplitude * .5f : 0f;
                    }
                    bool checkpoint = local == 0 || local % 10 == 0 || index == total - 1;
                    bool jump = !checkpoint && !turning && turnSteps == 0 && Mathf.Abs(x) < span - 12f &&
                        (episode.Challenge == LevelChallenge.Jumps || episode.Challenge == LevelChallenge.Finale && local % 6 >= 1);
                    LandingPlatformType type = checkpoint ? LandingPlatformType.Rest :
                        jump ? LandingPlatformType.Precise : episode.Challenge == LevelChallenge.Climbing
                            ? LandingPlatformType.Grip : LandingPlatformType.Standard;
                    float wobble = turning ? 0f : Mathf.Sin(x * .035f + recipe.Seed * .19f) * 1.2f;
                    platforms[index] = new LandingPlatform
                    {
                        Center = new Vector3(x + rowOffset,
                            height + climb, z + wobble),
                        Radius = checkpoint ? 4.2f : jump ? 2.15f : episode.Challenge == LevelChallenge.Ledges ? 1.8f
                            : episode.Challenge == LevelChallenge.Rest ? 3.3f : Mathf.Lerp(2.4f, 3f, (float)random.NextDouble()),
                        Index = index, MergeIndex = index, Progress = progress, Type = type, IsCheckpoint = checkpoint
                    };
                    episodes[index] = e; sections[index] = section;
                    if (index > 0) connections[index - 1] = jump && !platforms[index - 1].IsCheckpoint
                        ? RouteConnection.Jump : RouteConnection.Walk;
                }
            }
            var result = new LevelRoute(recipe, platforms, episodes, connections, sections);
            var errors = LevelRouteValidator.Validate(result);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            return result;
        }
        private static float Weight(LevelChallenge challenge) => challenge == LevelChallenge.Climbing ? .45f
            : challenge == LevelChallenge.Rest ? .85f : 1.2f;
        private static int Sum(int[] values) { int result = 0; foreach (int v in values) result += v; return result; }
    }

    public static class LevelRouteValidator
    {
        public static List<string> Validate(LevelRoute route)
        {
            var errors = new List<string>();
            for (int i = 0; i < route.Platforms.Length; i++)
            {
                var point = route.Platforms[i];
                if (!float.IsFinite(point.Center.x) || !float.IsFinite(point.Center.y) || !float.IsFinite(point.Center.z))
                    errors.Add("Опора " + i + ": нечисловые координаты");
                if (i == 0) continue;
                var previous = route.Platforms[i - 1];
                float drop = previous.Center.y - point.Center.y;
                float distance = Vector2.Distance(new Vector2(previous.Center.x, previous.Center.z), new Vector2(point.Center.x, point.Center.z));
                if (drop < -1.5f || drop > 2.5f || distance > 8.2f)
                    errors.Add("Эпизод " + route.Episodes[i] + ", переход " + i + ": слишком большой перепад или расстояние");
                if (route.Connections[i - 1] == RouteConnection.Jump && distance - previous.Radius - point.Radius > 4f)
                    errors.Add("Переход " + i + ": слишком широкий разрыв");
                if (point.Progress <= previous.Progress) errors.Add("Переход " + i + ": нарушен прогресс");
            }
            return errors;
        }
    }
}
