using System;
using UnityEngine;

namespace GoatDescent.Levels
{
    public enum LevelForm { StoneCrown, MistyPillars, BrokenRidge }
    public enum LevelChallenge { Approach, Ledges, Climbing, Jumps, Rest, Finale }

    [Serializable]
    public sealed class LevelEpisode
    {
        [SerializeField] private string title;
        [SerializeField] private LevelChallenge challenge;
        [SerializeField, Range(15f, 120f)] private float targetSeconds = 60f;
        [SerializeField, Range(0f, 1f)] private float difficulty = .5f;

        public string Title => title;
        public LevelChallenge Challenge => challenge;
        public float TargetSeconds => targetSeconds;
        public float Difficulty => difficulty;

        public LevelEpisode(string name, LevelChallenge kind, float seconds, float hardness)
        { title = name; challenge = kind; targetSeconds = seconds; difficulty = hardness; }
    }

    [CreateAssetMenu(menuName = "GOAT/Level Recipe", fileName = "StoneCrown")]
    public sealed class LevelRecipe : ScriptableObject
    {
        public const int GeneratorVersion = 5;
        [SerializeField] private string levelName = "Каменная корона";
        [SerializeField] private LevelForm form;
        [SerializeField] private int seed = 7;
        [SerializeField] private BiomeProfile biome;
        [Header("Mountain silhouette")]
        [SerializeField, Range(80f, 300f)] private float radius = 145f;
        [SerializeField, Range(100f, 400f)] private float summitHeight = 215f;
        [SerializeField] private float valleyHeight = 8f;
        [Header("Route skeleton")]
        [SerializeField] private LevelEpisode[] episodes = DefaultEpisodes();

        public string LevelName => levelName;
        public LevelForm Form => form;
        public int Seed => seed;
        public BiomeProfile Biome => biome;
        public float Radius => radius;
        public float SummitHeight => summitHeight;
        public float ValleyHeight => valleyHeight;
        public LevelEpisode[] Episodes => episodes;
        public float TargetSeconds
        {
            get { float total = 0f; foreach (var episode in episodes) total += episode.TargetSeconds; return total; }
        }
        public void Configure(int value, BiomeProfile profile) { seed = value; biome = profile; }
        public int SeedFor(int stream) => unchecked(seed * 397 ^ stream * 7919 ^ GeneratorVersion);
        public static LevelEpisode[] DefaultEpisodes() => new[]
        {
            new LevelEpisode("Верхние полки", LevelChallenge.Approach, 50f, .15f),
            new LevelEpisode("Край скалы", LevelChallenge.Ledges, 65f, .4f),
            new LevelEpisode("Каменные рёбра", LevelChallenge.Climbing, 75f, .5f),
            new LevelEpisode("Разломы", LevelChallenge.Jumps, 80f, .65f),
            new LevelEpisode("Сосновая терраса", LevelChallenge.Rest, 40f, .2f),
            new LevelEpisode("Нижняя корона", LevelChallenge.Finale, 90f, .75f)
        };
    }
}
