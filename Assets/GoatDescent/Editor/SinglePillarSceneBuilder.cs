using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SinglePillarSceneBuilder
    {
        private const string ScenePath = "Assets/GoatDescent/Scenes/Testing/SinglePillarPlayground.unity";
        public const string PresetPath = "Assets/GoatDescent/Vista/Data/SinglePillarPreset.asset";

        [MenuItem("Tools/Procedural World/Rebuild Single Pillar Scene")]
        public static void RebuildScene() => RebuildScene(null);

        public static void RebuildScene(PillarPreset preset)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var lightObject = GameObject.Find("Directional Light");
            if (lightObject)
            {
                var light = lightObject.GetComponent<Light>();
                light.color = new Color(1f, .95f, .85f);
                light.intensity = 1.3f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .75f;
                lightObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.58f, .66f, .74f);
            RenderSettings.ambientEquatorColor = new Color(.42f, .46f, .48f);
            RenderSettings.ambientGroundColor = new Color(.26f, .24f, .20f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .0004f;
            RenderSettings.fogColor = new Color(.72f, .80f, .86f);
            var controller = new GameObject("Single Pillar Controller").AddComponent<SinglePillarSceneController>();
            controller.ConfigureDefaultLayout();
            if (preset) controller.ApplyPreset(preset);
            controller.SetGenerateOnStart(false);
            controller.Generate();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("SINGLE_PILLAR_SCENE_READY path=" + ScenePath + " preset=" + (preset ? preset.name : "defaults"));
        }

        public static void SetupPresetAsset()
        {
            var preset = AssetDatabase.LoadAssetAtPath<PillarPreset>(PresetPath);
            if (!preset)
            {
                preset = ScriptableObject.CreateInstance<PillarPreset>();
                AssetDatabase.CreateAsset(preset, PresetPath);
            }
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            RebuildScene(preset);
        }
    }
}
