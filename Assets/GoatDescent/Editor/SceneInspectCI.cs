using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SceneInspectCI
    {
        public static void Inspect()
        {
            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var spawner = Object.FindFirstObjectByType<GoatSpawner>();
            var marker = GameObject.Find("Pillar Goat Spawn");
            var camera = Camera.main;
            Debug.Log($"SCENE_INSPECT spawner={spawner != null} marker={marker != null} markerPos={marker?.transform.position} camera={camera != null} cameraTag={(camera != null ? camera.tag : "none")}");
        }
    }
}