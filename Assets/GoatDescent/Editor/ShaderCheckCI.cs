using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class ShaderCheckCI
    {
        public static void Check()
        {
            var stone = Shader.Find("GoatDescent/Weathered Sandstone");
            var foliage = Shader.Find("GoatDescent/Valley Foliage");
            var standard = Shader.Find("Standard");
            Debug.Log($"SHADER_CHECK stone={stone != null} foliage={foliage != null} standard={standard != null}");

            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MistyPillarsSceneController>();
            controller.ClearGenerated();
            controller.Generate();

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mat = renderer.sharedMaterial;
                if (renderer.name == "Stratified sandstone")
                {
                    var albedo = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    var normal = mat.HasProperty("_NormalTex") ? mat.GetTexture("_NormalTex") : null;
                    var stoneColor = mat.HasProperty("_StoneColor") ? mat.GetColor("_StoneColor") : Color.magenta;
                    Debug.Log($"STONE_MAT shader={mat.shader.name} albedo={albedo != null && albedo != Texture2D.whiteTexture} normal={normal != null} stone={stoneColor}");
                }
            }
        }
    }
}