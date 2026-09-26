using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class CreateGoatScene
    {
        public static void Create()
        {
            string path = "Assets/GoatDescent/Scenes/GoatPlayground.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Goat Mountain").AddComponent<GoatDescent.MountainSceneBuilder>();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"GOAT_SCENE_CREATED {path}");
        }
    }
}