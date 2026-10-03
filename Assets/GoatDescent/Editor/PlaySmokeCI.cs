using GoatDescent;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class PlaySmokeCI
    {
        private const string Armed = "PLAY_SMOKE_ARMED";
        private const string Done = "PLAY_SMOKE_DONE";
        private static double goatSeenAt = -1;
        private static GoatController goat;
        private static int logs;

        static PlaySmokeCI() => EditorApplication.update += Poll;

        public static void Check()
        {
            string path = System.Environment.GetEnvironmentVariable("PLAY_SMOKE_SCENE");
            if (string.IsNullOrEmpty(path)) path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var marker = GameObject.Find(MistyPillarsVistaBuilder.SpawnMarkerName);
            if (!marker)
            {
                Debug.LogError("PLAY_SMOKE: no spawn marker");
                EditorApplication.Exit(1);
                return;
            }
            Vector3 pos = marker.transform.position;
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                Debug.Log("PLAY_SMOKE marker=" + pos + " ground=" + hit.point + " collider=" + hit.collider.name +
                          " slope=" + Vector3.Angle(hit.normal, Vector3.up).ToString("F1"));
            else
                Debug.LogError("PLAY_SMOKE: no ground under marker");
            var forest = marker.transform.parent.Find(MistyPillarsVistaBuilder.ForestObjectName);
            if (forest && forest.GetComponent<MeshFilter>() && forest.GetComponent<MeshFilter>().sharedMesh)
            {
                Vector3 local = marker.transform.localPosition;
                int near = 0, total = 0;
                foreach (Vector3 v in forest.GetComponent<MeshFilter>().sharedMesh.vertices)
                {
                    total++;
                    if ((v - local).sqrMagnitude < 100f) near++;
                }
                Debug.Log("PLAY_SMOKE greenery vertices within 10u of spawn: " + near + " / " + total);
            }
            SessionState.SetBool(Done, false);
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }

        private static void Capture(Transform goat)
        {
            var camGo = new GameObject("smoke-cam");
            var cam = camGo.AddComponent<Camera>();
            var rt = new RenderTexture(800, 450, 24);
            cam.targetTexture = rt;
            Vector3 fwd = goat.forward; fwd.y = 0f; fwd.Normalize();
            camGo.transform.position = goat.position - fwd * 6f + Vector3.up * 3f;
            camGo.transform.LookAt(goat.position + Vector3.up);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(800, 450, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 800, 450), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("C:/Users/Вадим/Unity Projects/GOAT/spawn-view.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("PLAY_SMOKE screenshot saved");
        }

        private static void Poll()
        {
            if (SessionState.GetBool(Done, false))
            {
                EditorApplication.update -= Poll;
                EditorApplication.Exit(0);
                return;
            }
            if (!SessionState.GetBool(Armed, false) || !EditorApplication.isPlaying) return;
            if (!goat) goat = UnityEngine.Object.FindFirstObjectByType<GoatController>();
            if (!goat) return;
            if (goatSeenAt < 0)
            {
                goatSeenAt = EditorApplication.timeSinceStartup;
                Debug.Log("PLAY_SMOKE goat spawned at " + goat.transform.position);
                Capture(goat.transform);
            }
            if (logs++ % 60 == 0)
                Debug.Log("PLAY_SMOKE t=" + (EditorApplication.timeSinceStartup - goatSeenAt).ToString("F1") +
                          " pos=" + goat.transform.position +
                          " speed=" + goat.Velocity.magnitude.ToString("F2") +
                          " grounded=" + goat.Grounded +
                          " slope=" + goat.GetComponent<GoatGroundDetector>().SlopeAngle.ToString("F1"));
            if (EditorApplication.timeSinceStartup - goatSeenAt > 6.0)
            {
                Debug.Log("PLAY_SMOKE final pos=" + goat.transform.position +
                          " speed=" + goat.Velocity.magnitude.ToString("F2") +
                          " grounded=" + goat.Grounded);
                SessionState.SetBool(Armed, false);
                SessionState.SetBool(Done, true);
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
