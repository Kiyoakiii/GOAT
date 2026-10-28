using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SceneOpenCI
    {
        public static void OpenMilestone()
        {
            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            if (!System.IO.File.Exists(path)) throw new System.InvalidOperationException("The milestone scene is retired. Open Assets/GoatDescent/Scenes/GoatPlayground.unity instead.");
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int rootCount = scene.rootCount;
            int objectCount = scene.rootCount > 0 ? scene.GetRootGameObjects()[0].transform.childCount : 0;
            Debug.Log($"SCENE_OPEN_OK rootObjects={rootCount} firstRootChildren={objectCount}");
        }
    }
}
