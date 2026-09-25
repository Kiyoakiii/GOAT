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
            const string bodyAlbedoPath = "Assets/GoatDescent/Art/RefinedGoat/GoatBody_Albedo.png";
            const string bodyNormalPath = "Assets/GoatDescent/Art/RefinedGoat/GoatBody_Normal.png";
            const string darkAlbedoPath = "Assets/GoatDescent/Art/RefinedGoat/GoatDark_Albedo.png";
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            var normalImporter = AssetImporter.GetAtPath(bodyNormalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            var bodyAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyAlbedoPath);
            var bodyNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyNormalPath);
            var darkAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(darkAlbedoPath);
            if (bodyAlbedo == null || bodyNormal == null || darkAlbedo == null)
                throw new System.InvalidOperationException("Baked goat coat textures are missing from " + path);

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
                        material.SetFloat("_Metallic", 0f);
                        if (source.name == "hideWhite")
                        {
                            material.SetTexture("_MainTex", bodyAlbedo);
                            material.SetTexture("_BumpMap", bodyNormal);
                            material.SetFloat("_BumpScale", 1f);
                            material.SetFloat("_Glossiness", .04f);
                            material.EnableKeyword("_NORMALMAP");
                            material.color = new Color(.78f, .76f, .71f, 1f);
                        }
                        else if (source.name == "hideDark")
                        {
                            material.SetTexture("_MainTex", darkAlbedo);
                            material.SetTexture("_BumpMap", null);
                            material.SetFloat("_Glossiness", .14f);
                            material.DisableKeyword("_NORMALMAP");
                            material.color = Color.white;
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
