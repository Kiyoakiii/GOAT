using UnityEditor;
using UnityEngine;

namespace GoatDescent.Editor
{
    /// <summary>Colors are baked into the three meshes; separate imported materials are unnecessary.</summary>
    public sealed class EagleModelImporter : AssetPostprocessor
    {
        [MenuItem("Tools/Goat Descent/Import Five Blender Eagles")]
        public static void ReimportFlock()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string variant in new[] { "01_Captain", "02_Dumpling", "03_Punk", "04_Frost", "05_Bandit" })
            {
                string path = "Assets/GoatDescent/Resources/Eagles/Eagle_" + variant + ".fbx";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!model) throw new System.InvalidOperationException("Missing eagle model: " + path);
                var renderers = model.GetComponentsInChildren<MeshRenderer>();
                int colors = 0;
                foreach (var mesh in model.GetComponentsInChildren<MeshFilter>()) colors += mesh.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color) ? mesh.sharedMesh.vertexCount : 0;
                Debug.Log($"EAGLE_IMPORT_READY variant={variant} meshes={renderers.Length} coloredVertices={colors}");
            }
            AssetDatabase.SaveAssets();
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/GoatDescent/Resources/Eagles/Eagle_")) return;
            var importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
        }
    }
}
