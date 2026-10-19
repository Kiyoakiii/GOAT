using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SceneScreenshotCI
    {
        public static void Capture()
        {
            string scenePath = System.Environment.GetEnvironmentVariable("SHOT_SCENE");
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("SHOT_SCENE env var is required");
                EditorApplication.Exit(1);
                return;
            }
            string outDir = System.Environment.GetEnvironmentVariable("SHOT_DIR") ?? "Logs/shots";
            string prefix = System.Environment.GetEnvironmentVariable("SHOT_PREFIX") ?? "scene";
            int width = ParseEnv("SHOT_WIDTH", 1600);
            int height = ParseEnv("SHOT_HEIGHT", 900);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Bounds bounds = SceneBounds();
            Vector3 center = bounds.center;
            float radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * .62f;
            float distance = ParseFloatEnv("SHOT_DISTANCE", radius * 1.55f);
            Directory.CreateDirectory(outDir);
            var cameraGo = new GameObject("shot-cam");
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.farClipPlane = distance * 8f;
            camera.clearFlags = CameraClearFlags.Skybox;
            var scenePost = Object.FindFirstObjectByType<GoatDescent.GoatScenePost>(FindObjectsInactive.Include);
            if (scenePost != null) cameraGo.AddComponent<GoatDescent.GoatScenePost>().CopyFrom(scenePost);
            float elevation = ParseFloatEnv("SHOT_ELEVATION", 20f);
            int shots = 0;
            for (int i = 0; i < 8; i++)
                shots += Shoot(camera, center, distance, elevation, i * 45f, $"{prefix}-az{i}", outDir, width, height);
            shots += Shoot(camera, center, distance * 1.1f, elevation + 35f, 22f, $"{prefix}-high", outDir, width, height);
            shots += Shoot(camera, center, distance * 1.05f, Mathf.Max(4f, elevation - 13f), 205f, $"{prefix}-low", outDir, width, height);
            Object.DestroyImmediate(cameraGo);
            Debug.Log($"SCENE_SHOTS saved {shots} images to {outDir} bounds={bounds.size}");
            EditorApplication.Exit(0);
        }

        private static int Shoot(Camera camera, Vector3 center, float distance, float elevation, float azimuth, string name, string outDir, int width, int height)
        {
            float elevationRad = elevation * Mathf.Deg2Rad;
            float azimuthRad = azimuth * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad), Mathf.Sin(elevationRad),
                Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad));
            camera.transform.position = center + direction * distance;
            camera.transform.LookAt(center);
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
            return 1;
        }

        private static Bounds SceneBounds()
        {
            var bounds = new Bounds();
            bool found = false;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!renderer.gameObject.scene.isLoaded) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found)
            {
                Debug.LogError("SCENE_SHOTS: no mesh renderers found");
                EditorApplication.Exit(1);
            }
            return bounds;
        }

        private static int ParseEnv(string name, int fallback)
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, out int parsed) ? parsed : fallback;
        }

        private static float ParseFloatEnv(string name, float fallback)
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            return float.TryParse(value, out float parsed) ? parsed : fallback;
        }
    }
}
