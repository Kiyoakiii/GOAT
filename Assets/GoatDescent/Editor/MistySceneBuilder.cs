using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class MistySceneBuilder
    {
        private const string ScenePath = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";

        [MenuItem("Tools/Procedural World/Rebuild Misty Scene")]
        public static void RebuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.92f, 0.72f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .8f;
            lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

            var controllerObject = new GameObject("Misty Scene Controller");
            var controller = controllerObject.AddComponent<GoatDescent.ProceduralWorld.MistyPillarsSceneController>();
            controller.ConfigureDefaultLayout();

            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.36f, .58f, .85f));
            sky.SetColor("_SkyHorizon", new Color(.82f, .9f, .97f));
            sky.SetColor("_SunColor", new Color(1f, .93f, .72f));
            sky.SetFloat("_SunSize", .09f);
            sky.SetColor("_CloudColor", new Color(1f, .98f, .94f));
            sky.SetFloat("_CloudDensity", .5f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .70f, .78f);
            RenderSettings.ambientEquatorColor = new Color(.44f, .48f, .52f);
            RenderSettings.ambientGroundColor = new Color(.30f, .27f, .24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00012f;
            RenderSettings.fogColor = new Color(.72f, .81f, .87f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("MISTY_SCENE_REBUILT path=" + ScenePath);
        }
    }
}