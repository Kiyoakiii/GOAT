using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class ReferenceIbexAppearanceImporter
    {
        private const string Art="Assets/GoatDescent/Art/ReferenceIbex";

        internal static bool BakedAppearanceEnabled => !File.Exists(Art+"/AppearanceManifest.json")
            || (bool?)Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Art+"/AppearanceManifest.json"))["runtimeEnabled"] != false;

        private static void SetBakedAppearanceEnabled(bool enabled)
        {
            string path=Art+"/AppearanceManifest.json";
            if(!File.Exists(path)) return;
            var manifest=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            manifest["runtimeEnabled"]=enabled;
            File.WriteAllText(path,manifest.ToString());
            AssetDatabase.ImportAsset(path);
        }

        [MenuItem("Tools/Goat Descent/Import baked ibex appearance")]
        public static void Apply()
        {
            if (!BakedAssetsAvailable())
                throw new InvalidOperationException("The archived baked coat assets are not included in the gameplay checkout. The lightweight goat remains active; restore the complete authoring archive before importing the detailed coat.");
            if(!File.Exists(Art+"/AppearanceManifest.json"))
                throw new InvalidOperationException("Run Tools/Blender/export_ibex_appearance.py first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture("Ibex_Albedo",false,true,2048);
            ConfigureTexture("IbexWhite_Albedo",false,true,2048);
            ConfigureTexture("Ibex_Normal",true,false,2048);
            ConfigureTexture("Ibex_Parameters",false,false,2048);
            ConfigureTexture("Ibex_HairAtlas",false,false,2048,true);
            ConfigureTexture("Ibex_HairNormal",true,false,2048);
            var coat=Surface("Ibex_Surface","Ibex_Albedo");
            var white=Surface("IbexWhite_Surface","IbexWhite_Albedo");
            // Horn reflectance comes from the native material parameter map.
            var horns=Surface("Ibex_Horns","Ibex_Albedo");
            var beard=Material("Ibex_Fibers","GoatDescent/Ibex Baked Beard");
            var whiteBeard=Material("IbexWhite_Fibers","GoatDescent/Ibex Baked Beard");
            var hair=Hair("Ibex_HairCards","Ibex_Albedo");
            var whiteHair=Hair("IbexWhite_HairCards","IbexWhite_Albedo");
            ReferenceIbexImporter.ImportSkin(coat,beard,horns,white,whiteBeard,hair,whiteHair);
            SetBakedAppearanceEnabled(true);
            AssetDatabase.SaveAssets();
            Debug.Log("IBEX_BAKED_APPEARANCE_IMPORTED skins=11,12 surfaceMaps=2048 hairAtlas=2048 lightingAndCameraUnchanged=True");
        }

        [MenuItem("Tools/Goat Descent/Import baked ibex appearance", true)]
        private static bool BakedAssetsAvailable()
        {
            foreach (string name in new[] { "AppearanceManifest.json", "ReferenceIbex.fbx", "Ibex_Albedo.png", "IbexWhite_Albedo.png", "Ibex_Normal.png", "Ibex_Parameters.png", "Ibex_HairAtlas.png", "Ibex_HairNormal.png" })
                if (!File.Exists(Art + "/" + name)) return false;
            return true;
        }

        [MenuItem("Tools/Goat Descent/Restore previous ibex appearance")]
        public static void RestorePrevious()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/IbexSkin.asset");
            if(!mesh || mesh.subMeshCount!=3)
                throw new InvalidOperationException("The preserved original ibex mesh is missing.");
            Directory.CreateDirectory(Art+"/LegacyAppearance");
            // Keep the detailed coats outside Resources; ordinary gameplay no
            // longer loads the portrait-sized skinned mesh through Skin11/12.
            foreach(int number in new[]{11,12})
            {
                var definition=GoatSkin.Definition(number);
                string backup=$"{Art}/BakedSkin{number:00}.asset";
                if(definition.mesh!=mesh && !File.Exists(backup))
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(definition),backup);
            }
            foreach(string name in new[]{"Ibex_Albedo","IbexWhite_Albedo","Ibex_Normal"})
            {
                string path=$"{Art}/LegacyAppearance/{name}.png";
                if(!File.Exists(path))
                {
                    string source=$"Builds/IbexAppearance/Before/{name}.png";
                    if(!File.Exists(source)) throw new FileNotFoundException("Original ibex texture backup is missing",source);
                    File.Copy(source,path);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=name=="Ibex_Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture=name!="Ibex_Normal";
                importer.maxTextureSize=2048;
                importer.mipmapEnabled=true;
                importer.SaveAndReimport();
            }
            var coat=LegacySurface("Ibex_Surface","Ibex_Albedo");
            var white=LegacySurface("IbexWhite_Surface","IbexWhite_Albedo");
            var horns=LegacySurface("Ibex_Horns","Ibex_Albedo");
            horns.color=new Color(2.65f,2.45f,2.1f,1f);
            horns.SetFloat("_Glossiness",.27f);
            var fibers=Material("LegacyAppearance/Ibex_Fibers","GoatDescent/Ibex Fibers");
            var whiteFibers=Material("LegacyAppearance/IbexWhite_Fibers","GoatDescent/Ibex Fibers");
            fibers.color=whiteFibers.color=Color.white;
            fibers.mainTexture=coat.mainTexture;
            whiteFibers.mainTexture=white.mainTexture;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/ReferenceIbexWhite.fbx")
                .GetComponentInChildren<SkinnedMeshRenderer>();
            foreach(int number in new[]{11,12})
            {
                var definition=GoatSkin.Definition(number);
                definition.mesh=mesh;
                definition.material=number==11 ? coat : white;
                definition.materials=new[]{definition.material,horns,number==11 ? fibers : whiteFibers};
                definition.boneNames=Array.ConvertAll(original.bones,b=>b.name);
                EditorUtility.SetDirty(definition);
            }
            SetBakedAppearanceEnabled(false);
            AssetDatabase.SaveAssets();
            Debug.Log($"IBEX_PREVIOUS_APPEARANCE_RESTORED triangles={mesh.triangles.Length/3} detailedSourcePreserved=True");
        }

        private static Material LegacySurface(string name,string albedo)
        {
            var value=Material("LegacyAppearance/"+name,"Standard");
            value.mainTexture=Texture("LegacyAppearance/"+albedo);
            value.SetTexture("_BumpMap",Texture("LegacyAppearance/Ibex_Normal"));
            value.SetFloat("_BumpScale",1f);
            value.SetFloat("_Glossiness",.11f);
            value.EnableKeyword("_NORMALMAP");
            return value;
        }

        private static void ConfigureTexture(string name,bool normal,bool srgb,int size,bool coverage=false)
        {
            string path=$"{Art}/{name}.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer) throw new FileNotFoundException("Missing Blender bake",path);
            importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=srgb;
            importer.maxTextureSize=size;
            importer.mipmapEnabled=true;
            importer.streamingMipmaps=false;
            importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaSource=coverage ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency=coverage;
            importer.mipMapsPreserveCoverage=coverage;
            importer.alphaTestReferenceValue=.35f;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/{name}.png");
        private static Material Material(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);
            if(!shader) throw new InvalidOperationException("Missing ibex shader "+shaderName);
            string path=$"{Art}/{name}.mat";
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value) {value=new Material(shader);AssetDatabase.CreateAsset(value,path);}
            value.shader=shader;
            value.shaderKeywords=Array.Empty<string>();
            value.color=Color.white;
            value.renderQueue=-1;
            EditorUtility.SetDirty(value);
            return value;
        }
        private static Material Surface(string name,string albedo)
        {
            var value=Material(name,"GoatDescent/Ibex Baked Surface");
            value.SetTexture("_MainTex",Texture(albedo));
            value.SetTexture("_BumpMap",Texture("Ibex_Normal"));
            value.SetTexture("_ParameterMap",Texture("Ibex_Parameters"));
            return value;
        }
        private static Material Hair(string name,string albedo)
        {
            var value=Material(name,"GoatDescent/Ibex Baked Hair");
            value.SetTexture("_HairMap",Texture("Ibex_HairAtlas"));
            value.SetTexture("_BumpMap",Texture("Ibex_HairNormal"));
            value.SetTexture("_CoatMap",Texture(albedo));
            value.SetFloat("_Cutoff",.35f);
            value.SetFloat("_Roughness",.52f);
            value.SetFloat("_RandomRoughness",.12f);
            return value;
        }
    }
}
