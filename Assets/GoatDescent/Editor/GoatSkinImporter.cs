using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GoatDescent.Editor
{
    [InitializeOnLoad]
    public static class GoatSkinImporter
    {
        static GoatSkinImporter() { EditorApplication.update+=Poll; }
        private static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
            const string request="Builds/GoatSkinImport.request";
            if(!File.Exists(request)) return;
            File.Delete(request);
            try { Build(); File.WriteAllText("Builds/GoatSkinImport.result","Imported "+GoatSkin.Numbers.Length+" goat skins"); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText("Builds/GoatSkinImport.result",e.ToString()); }
        }
        [MenuItem("Tools/Goat Descent/Import selected goat skins")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var reference=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"));
            try
            {
                var rig=reference.GetComponentsInChildren<Transform>(true).First(t=>t.name=="GoatRig_A");
                var bones=rig.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                foreach(int number in GoatSkin.VariantNumbers)
                {
                    string path=$"Assets/GoatDescent/Art/Skins/Skin{number:00}.fbx";
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                    importer.animationType=ModelImporterAnimationType.Generic; importer.importAnimation=false;
                    importer.materialImportMode=ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
                    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    try
                    {
                        model.transform.localScale=Vector3.one*2;
                        var renderer=model.GetComponentInChildren<SkinnedMeshRenderer>();
                        if(!renderer) throw new InvalidOperationException("Skinned mesh missing: "+path);
                        var mesh=UnityEngine.Object.Instantiate(renderer.sharedMesh); mesh.name=$"GoatSkin{number:00}";
                        Debug.Log($"SKIN_SCALE {number} import={importer.globalScale} units={importer.useFileUnits} mesh={mesh.bounds.size} rig={rig.lossyScale} model={renderer.transform.lossyScale}");
                        var matrix=rig.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                        mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                        var normalMatrix=matrix.inverse.transpose;
                        mesh.normals=mesh.normals.Select(v=>normalMatrix.MultiplyVector(v).normalized).ToArray();
                        mesh.bindposes=renderer.bones.Select(b=>bones[b.name].worldToLocalMatrix*rig.localToWorldMatrix).ToArray();
                        mesh.RecalculateBounds(); mesh.RecalculateTangents();
                        string meshPath=$"Assets/GoatDescent/Art/Skins/Skin{number:00}.asset";
                        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(existing) { CopyMesh(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
                        else AssetDatabase.CreateAsset(mesh,meshPath);
                        string materialPath=$"Assets/GoatDescent/Art/Skins/Skin{number:00}.mat";
                        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if(!material) { material=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material,materialPath); }
                        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/GoatDescent/Art/Skins/Skin{number:00}_Albedo.png");
                        material.color=Color.white; material.SetFloat("_Glossiness",.06f);
                        string assetPath=$"Assets/GoatDescent/Resources/GoatSkins/Skin{number:00}.asset";
                        var definition=AssetDatabase.LoadAssetAtPath<GoatSkinDefinition>(assetPath);
                        if(!definition) { definition=ScriptableObject.CreateInstance<GoatSkinDefinition>(); AssetDatabase.CreateAsset(definition,assetPath); }
                        definition.number=number;
                        definition.displayName=number switch {1=>"Бочонок",2=>"Гривистый",4=>"Грудастый",5=>"Кудрявый",7=>"Такса",_=>"Снежная грива"};
                        definition.mesh=mesh; definition.material=material;
                        definition.boneNames=renderer.bones.Select(b=>b.name).ToArray();
                        definition.preview=Resources.Load<Texture2D>($"GoatSkins/Preview{number:00}");
                        EditorUtility.SetDirty(definition); EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(material);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(model); }
                }
                AssetDatabase.SaveAssets(); Debug.Log("GOAT_SKINS_READY "+string.Join(",",GoatSkin.Numbers));
            }
            finally { UnityEngine.Object.DestroyImmediate(reference); }
        }
        private static void CopyMesh(Mesh source, Mesh target)
        {
            // Rebuild native mesh buffers when a skin's topology changes.
            // CopySerialized can leave a renderer using the old vertex stride.
            target.Clear(false); target.indexFormat=source.indexFormat;
            target.vertices=source.vertices; target.normals=source.normals;
            target.tangents=source.tangents; target.colors32=source.colors32;
            for(int channel=0;channel<8;channel++)
            {
                var uv=new System.Collections.Generic.List<Vector4>(); source.GetUVs(channel,uv);
                if(uv.Count>0) target.SetUVs(channel,uv);
            }
            target.bindposes=source.bindposes; target.boneWeights=source.boneWeights;
            target.subMeshCount=source.subMeshCount;
            for(int i=0;i<source.subMeshCount;i++)
                target.SetIndices(source.GetIndices(i),source.GetTopology(i),i,false);
            var vertices=new Vector3[source.vertexCount];var normals=new Vector3[source.vertexCount];var tangents=new Vector3[source.vertexCount];
            for(int shape=0;shape<source.blendShapeCount;shape++)
                for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
                {
                    source.GetBlendShapeFrameVertices(shape,frame,vertices,normals,tangents);
                    target.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),vertices,normals,tangents);
                }
            target.bounds=source.bounds;target.name=source.name;
            target.UploadMeshData(false);
        }
    }
}
