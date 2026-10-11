using GoatDescent.ProceduralWorld.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class GroundcoverRegenCI
    {
        public static void RegenerateGroundcover()
        {
            var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(empty, "Assets/GoatDescent/Scenes/Testing/_EmptyBatch.unity");
            ProceduralWorldEditorActions.GenerateGroundcoverPreviewFromMenu();
            Debug.Log("GROUNDCOVER_REGENERATED");
        }
    }
}