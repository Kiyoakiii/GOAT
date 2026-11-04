using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class ScenicCourseCheckCI
    {
        private const string Armed = "GOAT.ScenicSmoke";
        private static GoatDescent.ScenicGoatCourse course;
        private static GoatController goat;
        private static Rigidbody body;
        private static RespawnController life;
        private static int sample, target;
        private static float started;
        private static Vector3 origin;
        private static bool moving;
        static ScenicCourseCheckCI() => EditorApplication.update += Poll;

        public static void Check()
        {
            string scene = "TerracePeak";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-scenicScene") scene = args[i + 1];
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/" + scene + ".unity");
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }

        private static void Poll()
        {
            if (!SessionState.GetBool(Armed, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (Time.time < 3f) return;
                if (!course)
                {
                    course = UnityEngine.Object.FindFirstObjectByType<GoatDescent.ScenicGoatCourse>();
                    goat = UnityEngine.Object.FindFirstObjectByType<GoatController>();
                    if (!course || !goat) throw new InvalidOperationException("Course did not spawn a goat.");
                    body = goat.GetComponent<Rigidbody>();
                    life = goat.GetComponent<RespawnController>();
                    goat.SetRemoteControl(true);
                    course.enabled = false;
                }
                if (!moving)
                {
                    int index = Mathf.RoundToInt((course.Points.Length - 40) * sample / 4f);
                    origin = course.Points[index];
                    life.SetKillPlane(origin.y - 60f);
                    life.Teleport(origin + Vector3.up * 1.2f);
                    target = index + 1;
                    started = Time.time;
                    moving = true;
                }
                while (target < course.Points.Length - 1 && Vector3.Distance(body.position, course.Points[target]) < 3f) target++;
                Vector3 direction = course.Points[target] - body.position;
                goat.SetRemoteMove(new Vector3(direction.x,0f,direction.z).normalized * .65f);
                if (life.IsDead) throw new InvalidOperationException("Goat died at sample " + sample + " position=" + body.position + " origin=" + origin + " target=" + target + " elapsed=" + (Time.time-started));
                if (Time.time - started < 8f) return;
                float distance = Vector3.Distance(body.position, origin);
                if (distance < 5f) throw new InvalidOperationException("Goat stuck at sample " + sample + " distance=" + distance);
                if (body.position.y < course.Points[Mathf.Max(0,target-1)].y - 5f) throw new InvalidOperationException("Goat fell from trail at sample " + sample);
                Debug.Log($"SCENIC_PLAY_SAMPLE_OK scene={course.gameObject.scene.name} sample={sample} moved={distance:F1} grounded={goat.Grounded}");
                sample++;
                moving = false;
                if (sample < 5) return;
                Debug.Log("SCENIC_PLAY_OK " + course.gameObject.scene.name);
                SessionState.SetBool(Armed, false);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SessionState.SetBool(Armed, false);
                EditorApplication.Exit(1);
            }
        }
    }
}
