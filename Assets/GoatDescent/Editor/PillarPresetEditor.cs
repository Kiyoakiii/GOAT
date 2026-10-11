using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [CustomEditor(typeof(PillarPreset))]
    public class PillarPresetEditor : UnityEditor.Editor
    {
        private static readonly string[] Silhouettes = { "Classic", "Spire", "Mesa", "Leaning" };
        private static readonly string[] Vegetation = { "Lush", "Moderate", "Sparse" };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var preset = (PillarPreset)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Silhouette: " + Silhouettes[MistyPillarsVistaBuilder.SilhouetteKindOf(preset.seed)] +
                "\nVegetation: " + Vegetation[MistyPillarsVistaBuilder.VegetationKindOf(preset.seed)] +
                "\nSeed " + MistyPillarsVistaBuilder.SpawnTowerSeed + " is always Classic + Lush (vista spawn).",
                MessageType.Info);
            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(preset, "Randomize Pillar Seed");
                preset.seed = Random.Range(1, int.MaxValue);
                EditorUtility.SetDirty(preset);
            }
            if (GUILayout.Button("Rebuild Single Pillar Scene"))
                SinglePillarSceneBuilder.RebuildScene(preset);
        }
    }
}
