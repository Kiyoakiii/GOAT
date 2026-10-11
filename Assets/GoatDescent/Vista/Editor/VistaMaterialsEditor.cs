using UnityEditor;
using UnityEngine;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaMaterialsEditor
    {
        internal struct MaterialSet
        {
            public Material rock;
            public Material trunk;
            public Material foliage;
            public Material summitFoliage;
            public Material water;
        }

        internal static MaterialSet GetOrCreateMaterials()
        {
            Shader foliageShader = Shader.Find("GoatDescent/PineFoliage");
            Material summitFoliage = GetOrCreateMaterial("M_ProceduralSummitFoliage", Color.white, foliageShader);
            if (summitFoliage.HasProperty("_FrostAmount"))
                summitFoliage.SetFloat("_FrostAmount", 0.84f);
            if (summitFoliage.HasProperty("_FrostTint"))
                summitFoliage.SetColor("_FrostTint", new Color(0.86f, 0.93f, 0.99f, 1f));
            if (summitFoliage.HasProperty("_Color"))
                summitFoliage.SetColor("_Color", new Color(0.90f, 0.99f, 0.91f, 1f));
            if (summitFoliage.HasProperty("_FoliageGlow"))
                summitFoliage.SetFloat("_FoliageGlow", 0.20f);
            summitFoliage.enableInstancing = true;
            EditorUtility.SetDirty(summitFoliage);
            return new MaterialSet
            {
                rock = GetOrCreateMaterial("M_ProceduralRock", new Color(0.31f, 0.32f, 0.25f)),
                trunk = GetOrCreateMaterial("M_ProceduralTrunk", new Color(0.25f, 0.15f, 0.09f)),
                foliage = GetOrCreateMaterial(
                    "M_ProceduralFoliage",
                    Color.white,
                    foliageShader),
                summitFoliage = summitFoliage,
                water = GetOrCreateMaterial("M_ProceduralWater", new Color(0.10f, 0.34f, 0.48f))
            };
        }

        internal static Material GetOrCreateMaterial(string name, Color color, Shader shaderOverride = null)
        {
            string path = $"{VistaAssetPaths.MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shaderOverride != null ? shaderOverride : Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shaderOverride != null && material.shader != shaderOverride)
                material.shader = shaderOverride;
            material.color = color;
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.16f);
            if (material.HasProperty("_FoliageGlow"))
                material.SetFloat("_FoliageGlow", 0.20f);
            if (name.Contains("Foliage") || name.Contains("Grass"))
                material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material GetOrCreateGrassMaterial()
        {
            Shader foliageShader = Shader.Find("GoatDescent/PineFoliage");
            Material grass = GetOrCreateMaterial(
                "M_ProceduralGrass",
                Color.white,
                foliageShader);
            if (grass.HasProperty("_WindAmplitude"))
                grass.SetFloat("_WindAmplitude", 0.21f);
            if (grass.HasProperty("_WindHeight"))
                grass.SetFloat("_WindHeight", 1.08f);
            if (grass.HasProperty("_WindSpeed"))
                grass.SetFloat("_WindSpeed", 1.05f);
            if (grass.HasProperty("_WindDirection"))
                grass.SetVector("_WindDirection", new Vector4(0.82f, 0f, 0.57f, 0f));
            if (grass.HasProperty("_FoliageGlow"))
                grass.SetFloat("_FoliageGlow", 0.18f);
            if (grass.HasProperty("_FrostAmount"))
                grass.SetFloat("_FrostAmount", 0.68f);
            if (grass.HasProperty("_FrostTint"))
                grass.SetColor("_FrostTint", new Color(0.78f, 0.88f, 0.95f, 1f));
            grass.enableInstancing = true;
            EditorUtility.SetDirty(grass);
            return grass;
        }
    }
}