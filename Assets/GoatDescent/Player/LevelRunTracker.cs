using System;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent
{
    public sealed class LevelRunTracker : MonoBehaviour
    {
        private LevelRecipe recipe;
        private RouteProgressTracker[] progress;
        private RespawnController[] lives;
        private bool[] previouslyDead;
        private float startedAt;
        private int lastEpisode = -1;
        public int Falls { get; private set; }
        public bool Completed { get; private set; }
        private float finishedAt;
        private float teamProgress;
        private GUIStyle runStyle;
        public float ElapsedSeconds => (Completed ? finishedAt : Time.time) - startedAt;
        public event Action CompletedRun;

        public void Configure(LevelRecipe value, params GoatController[] players)
        {
            recipe = value;
            progress = new RouteProgressTracker[players.Length]; lives = new RespawnController[players.Length];
            previouslyDead = new bool[players.Length];
            for (int i = 0; i < players.Length; i++)
            { progress[i] = players[i].GetComponent<RouteProgressTracker>(); lives[i] = players[i].GetComponent<RespawnController>(); }
            startedAt = Time.time;
        }
        private void Update()
        {
            if (!recipe || Completed || progress == null || !MountainAuthority.IsHost) return;
            float team = 1f;
            for (int i = 0; i < progress.Length; i++)
            {
                if (!progress[i] || !lives[i]) return;
                team = Mathf.Min(team, progress[i].CurrentProgress);
                if (lives[i].IsDead && !previouslyDead[i]) Falls++;
                previouslyDead[i] = lives[i].IsDead;
            }
            teamProgress = team;
            int episode = EpisodeAt(team);
            if (episode != lastEpisode)
            {
                lastEpisode = episode;
                Debug.Log("LEVEL_EPISODE name=" + recipe.Episodes[episode].Title + " elapsed=" + ElapsedSeconds.ToString("F1") + " falls=" + Falls);
            }
            if (team < .999f) return;
            finishedAt = Time.time; Completed = true;
            Debug.Log("LEVEL_FINISHED name=" + recipe.LevelName + " elapsed=" + ElapsedSeconds.ToString("F1") + " falls=" + Falls);
            CompletedRun?.Invoke();
        }
        private void OnGUI()
        {
            if (!recipe || progress == null) return;
            runStyle ??= new GUIStyle(GUI.skin.box)
            { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 8, 8) };
            int seconds = Mathf.FloorToInt(ElapsedSeconds);
            string clock = (seconds / 60) + ":" + (seconds % 60).ToString("D2");
            string title = Completed ? "Маршрут пройден обеими козами!" : recipe.Episodes[EpisodeAt(teamProgress)].Title;
            GUI.Box(new Rect(24, 144, Mathf.Min(440f, Screen.width - 48f), 62),
                title + "\n" + Mathf.RoundToInt(teamProgress * 100f) + "%   " + clock + "   Падения: " + Falls, runStyle);
        }
        private int EpisodeAt(float value)
        {
            float seconds = value * recipe.TargetSeconds;
            for (int i = 0; i < recipe.Episodes.Length; i++)
            { seconds -= recipe.Episodes[i].TargetSeconds; if (seconds < 0f) return i; }
            return recipe.Episodes.Length - 1;
        }
    }
}
