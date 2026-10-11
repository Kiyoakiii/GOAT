using UnityEditor;
using UnityEngine;

namespace GoatDescent.ProceduralWorld.Editor
{
    [CustomEditor(typeof(MistyPillarsSceneController))]
    public class MistyPillarsSceneControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (MistyPillarsSceneController)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear Generated Pillars"))
                controller.ClearGenerated();
            if (GUILayout.Button("Generate Pillars"))
            {
                controller.ClearGenerated();
                controller.Generate();
            }
            if (GUILayout.Button("Generate + Spawn Goat"))
            {
                controller.ClearGenerated();
                controller.Generate();
                controller.SpawnGoat();
            }
        }
    }
}