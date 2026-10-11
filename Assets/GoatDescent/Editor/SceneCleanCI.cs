using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SceneCleanCI
    {
        public static void Clean()
        {
            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MistyPillarsSceneController>();
            if (controller) controller.ClearGenerated();
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Misty forest pillars vista"))
                    Object.DestroyImmediate(root);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SCENE_CLEANED roots=" + scene.rootCount);
        }
    }
}