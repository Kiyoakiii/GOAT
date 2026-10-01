using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class PairAnimationImporter
    {
        private const string Source = "Assets/GoatDescent/Art/RefinedGoat/Goat_PairGameplay.fbx";
        private const string PhysicsSource = "Assets/GoatDescent/Art/RefinedGoat/Goat_PhysicsActions.fbx";
        private const string Prefab = "Assets/GoatDescent/Resources/GoatDuoRefined.prefab";
        [MenuItem("Tools/Goat Descent/Import Pair Gameplay Animations")]
        public static void Build() => Import(Source, 9, "PAIR_ANIMATIONS_READY");

        [MenuItem("Tools/Goat Descent/Import Goat Physics Animations")]
        public static void ImportPhysics() => Import(PhysicsSource, 7, "GOAT_PHYSICS_ANIMATIONS_READY");

        private static void Import(string source, int expectedClips, string logPrefix)
        {
            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(source) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Missing exported animation FBX: " + source);
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (clips.Length != expectedClips) throw new InvalidOperationException("Expected " + expectedClips + " clips, received " + clips.Length);
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animation = root.GetComponentInChildren<Animation>();
                if (!animation) throw new InvalidOperationException("The existing goat prefab needs its Legacy Animation component.");
                foreach (var clip in clips)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        if (!string.IsNullOrEmpty(binding.path) && !animation.transform.Find(binding.path))
                            throw new InvalidOperationException("Pair clip binding does not match existing rig: " + binding.path);
                    animation.AddClip(clip, clip.name);
                }
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                AssetDatabase.SaveAssets();
                Debug.Log(logPrefix + " " + string.Join(", ", clips.Select(c => c.name + "=" + c.length.ToString("F2") + "s")));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
