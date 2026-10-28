using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.Editor
{
    public static class EagleFlockPreview
    {
        [MenuItem("Tools/Goat Descent/Render Five Eagle Preview")]
        public static void Render()
        {
            EagleModelImporter.ReimportFlock();
            var scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("Eagle preview camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.transform.rotation = Quaternion.Euler(22, 180, 0);
            camera.transform.position = -camera.transform.forward * 20;
            camera.orthographic = true; camera.orthographicSize = 6.2f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f,.12f,.16f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 50;
            var palette = new Material[7];
            palette[6] = new Material(Resources.Load<Shader>("EaglePlumage")) { color = Color.white };
            for (int i = 0; i < 5; i++)
            {
                var root = new GameObject("Preview eagle " + (i + 1));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = camera.transform.right * ((i % 3 - 1) * 6.1f)
                    + camera.transform.up * (i < 3 ? 2.8f : -2.4f);
                root.transform.rotation = Quaternion.Euler(0,-18,0);
                CartoonEagleVisual.Build(root.transform, palette, i, out var left, out var right);
                left.localRotation = Quaternion.Euler(0,0,-8);
                right.localRotation = Quaternion.Euler(0,0,8);
            }
            var lightObject = new GameObject("Eagle preview key");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.25f; light.color = new Color(1,.9f,.78f);
            light.transform.rotation = Quaternion.Euler(35,-35,0);
            var fillObject = new GameObject("Eagle preview fill");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject, scene);
            var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional;
            fill.intensity = .7f; fill.color = new Color(.68f,.82f,1);
            fill.transform.rotation = Quaternion.Euler(20,160,0);
            var render = new RenderTexture(1920,1280,24);
            var image = new Texture2D(1920,1280,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
                image.ReadPixels(new Rect(0,0,1920,1280),0,0); image.Apply();
                string output = Path.GetFullPath("Builds/EagleFlock/unity-five-eagles.png");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output,image.EncodeToPNG());
                Debug.Log("EAGLE_FLOCK_PREVIEW " + output);
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(image); render.Release(); Object.DestroyImmediate(render);
                Object.DestroyImmediate(palette[6]); EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
