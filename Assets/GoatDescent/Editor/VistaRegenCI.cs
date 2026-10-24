using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.EditorTools
{
    public static class VistaRegenCI
    {
        public static void RegenerateMeshes()
        {
            string vistaScenePath = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            if (!System.IO.File.Exists(vistaScenePath)) throw new System.InvalidOperationException("The milestone scene is retired and will not be recreated. Use GoatPlayground for current gameplay.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(vistaScenePath, OpenSceneMode.Additive);
            try
            {
                GoatDescent.ProceduralWorld.Editor.MistyPillarsVistaEditor.ApplyToScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("VISTA_MESHES_REGENERATED");
        }
    }
}
