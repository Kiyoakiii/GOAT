using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class RefinedGoatImporter
    {
        [MenuItem("Tools/Goat Descent/Import Refined Blender Goat")]
        public static void Build()
        {
            const string path = "Assets/GoatDescent/Art/RefinedGoat/Goat_Duo_Refined.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.SaveAndReimport();
            var root = new GameObject("Goat Duo Refined");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                model.transform.SetParent(root.transform, false);
                var animation = model.GetComponent<Animation>();
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    if (clip.name.StartsWith("__preview__")) continue;
                    if (clip.name == "Goat_Idle") animation.clip = clip;
                }
                animation.playAutomatically = false;
                var renderers = model.GetComponentsInChildren<Renderer>();
                // Authoring model is about one metre high. Preserve its hoof origin;
                // animated FBX renderer bounds are not stable before the first frame.
                model.transform.localScale = Vector3.one * 2f;
                model.transform.localPosition = Vector3.zero;
                foreach (var renderer in renderers)
                {
                    if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                    {
                        string materialPath = "Assets/GoatDescent/Art/RefinedGoat/" + source.name + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, materialPath); }
                        material.shader = Shader.Find("Standard");
                        material.SetFloat("_Glossiness", .15f);
                        if (source.name == "hideWhite" || source.name == "hideDark")
                        {
                            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Art/RefinedGoat/GoatPainted.png");
                            material.color = source.name == "hideWhite" ? new Color(.87f,.85f,.8f).gamma : new Color(.072f,.065f,.06f).gamma;
                        }
                        EditorUtility.SetDirty(material);
                        return material;
                    }).ToArray();
                }
                if (!AssetDatabase.IsValidFolder("Assets/GoatDescent/Resources")) AssetDatabase.CreateFolder("Assets/GoatDescent", "Resources");
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/GoatDescent/Resources/GoatDuoRefined.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("REFINED_GOAT_READY source=Goat_Duo_Refined.blend clips=Idle,Walk,Jump,EatGrass,Pee,Poop,Sequence,GoatA_Duo_Performance,GoatB_Duo_Performance");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}