using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace GoatDescent.Editor
{
    public static class ReferenceIbexImporter
    {
        private const string Art="Assets/GoatDescent/Art/ReferenceIbex";
        private const string ResourcesPath="Assets/GoatDescent/Resources/ReferenceIbex";

        [MenuItem("Tools/Goat Descent/Create small ibex tongue")]
        public static void BuildSmallTongue()
        {
            const string meshPath=Art+"/SmallTongue.asset";
            const string materialPath=Art+"/SmallTongue.mat";
            const string prefabPath=ResourcesPath+"/SmallTongue.prefab";
            const int rows=7,sides=12;
            float[] widths={.011f,.019f,.021f,.021f,.018f,.011f,.001f};
            var vertices=new Vector3[rows*sides];var triangles=new int[(rows-1)*sides*6];
            for(int row=0;row<rows;row++)
            {
                float t=row/(float)(rows-1);
                for(int side=0;side<sides;side++)
                {
                    float angle=side*Mathf.PI*2f/sides;
                    vertices[row*sides+side]=new Vector3(Mathf.Cos(angle)*widths[row],
                        Mathf.Sin(angle)*.0045f*Mathf.Lerp(1f,.15f,t*t)-Mathf.SmoothStep(0f,.009f,t),-.012f+t*.082f);
                }
            }
            int index=0;
            for(int row=0;row<rows-1;row++)
            for(int side=0;side<sides;side++)
            {
                int a=row*sides+side,b=row*sides+(side+1)%sides,c=a+sides,d=b+sides;
                triangles[index++]=a;triangles[index++]=b;triangles[index++]=c;
                triangles[index++]=b;triangles[index++]=d;triangles[index++]=c;
            }
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(!mesh) {mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}
            mesh.Clear();mesh.name="Short rounded ibex tongue";mesh.vertices=vertices;mesh.triangles=triangles;
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material) {material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,materialPath);}
            material.color=new Color(.72f,.26f,.33f);material.SetFloat("_Glossiness",.28f);EditorUtility.SetDirty(material);
            var node=new GameObject("Small pink tongue");
            node.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(node,prefabPath);Object.DestroyImmediate(node);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var reference=Resources.Load<GameObject>("GoatDuoRefined");
            var rig=reference.GetComponentsInChildren<Transform>(true).First(t=>t.name=="GoatRig_A");
            var head=rig.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");
            Vector3 authoredPoint=new Vector3(0f,-.592f,.887f);
            Vector3 world=new Vector3(-authoredPoint.x,authoredPoint.z,-authoredPoint.y)*2f;
            foreach(int number in new[]{11,12})
            {
                var definition=GoatSkin.Definition(number);if(!definition) continue;
                definition.tonguePrefab=prefab;
                definition.tongueOffset=Vector3.Scale(head.InverseTransformPoint(world),head.lossyScale);
                definition.tongueRotation=Quaternion.LookRotation(head.InverseTransformDirection(Vector3.forward),
                    head.InverseTransformDirection(Vector3.up));
                EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
            }
            AssetDatabase.SaveAssetIfDirty(mesh);AssetDatabase.SaveAssetIfDirty(material);
        }

        [MenuItem("Tools/Goat Descent/Import reference ibex and cliff")]
        public static void Build()
        {
            Directory.CreateDirectory(ResourcesPath);
            Directory.CreateDirectory(Art+"/Meshes");
            BuildSkins();
            var stone=Surface("CliffStone",.04f);
            stone.color=new Color(1.35f,1.35f,1.3f,1f);
            ImportCliff(stone);
            AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE_IBEX_READY skins=11,12 original=0,7,4 platform=IbexCliffStage modularStones=44");
        }

        [MenuItem("Tools/Goat Descent/Import reference ibex coats")]
        public static void BuildSkins()
        {
            if(File.Exists(Art+"/AppearanceManifest.json"))
            {
                if(ReferenceIbexAppearanceImporter.BakedAppearanceEnabled)
                    ReferenceIbexAppearanceImporter.Apply();
                else
                    ReferenceIbexAppearanceImporter.RestorePrevious();
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var coat=Surface("Ibex",.11f);
            var fibers=Material("Ibex_Fibers","GoatDescent/Ibex Fibers");
            fibers.color=Color.white;
            var horns=Material("Ibex_Horns","Standard");
            horns.CopyPropertiesFromMaterial(coat);
            horns.color=new Color(2.65f,2.45f,2.1f,1f);
            horns.SetFloat("_Glossiness",.27f);
            var whiteCoat=Surface("IbexWhite",.11f,"Ibex");
            var whiteFibers=Material("IbexWhite_Fibers","GoatDescent/Ibex Fibers");
            whiteFibers.color=new Color(8.4f,9.0f,9.6f,1f);
            ImportSkin(coat,fibers,horns,whiteCoat,whiteFibers);
            AssetDatabase.SaveAssets();
            GoatSkinPreview.Render(new[]{11,12});
            foreach(int number in new[]{11,12})
            {
                string destination=$"Assets/GoatDescent/Resources/GoatSkins/Preview{number:00}.png";
                File.Copy($"Builds/GoatSkins/Skin{number:00}.png",destination,true);
                AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
                var definition=GoatSkin.Definition(number);
                definition.preview=AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE_IBEX_COATS_READY skins=11,12 sharedMesh=True hornScale=0.6666667");
        }

        private static Material Material(string name,string shader)
        {
            string path=$"{Art}/{name}.mat";
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value) { value=new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(value,path); }
            value.shader=Shader.Find(shader);
            EditorUtility.SetDirty(value);
            return value;
        }

        private static Material Surface(string prefix,float gloss,string normalPrefix=null)
        {
            string normalPath=$"{Art}/{normalPrefix??prefix}_Normal.png";
            var normalImporter=(TextureImporter)AssetImporter.GetAtPath(normalPath);
            normalImporter.textureType=TextureImporterType.NormalMap;
            normalImporter.maxTextureSize=2048;
            normalImporter.SaveAndReimport();
            var albedoImporter=(TextureImporter)AssetImporter.GetAtPath($"{Art}/{prefix}_Albedo.png");
            albedoImporter.maxTextureSize=2048;
            albedoImporter.SaveAndReimport();
            var value=Material(prefix+"_Surface","Standard");
            value.color=Color.white;
            value.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/{prefix}_Albedo.png");
            value.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            value.SetFloat("_BumpScale",1f);
            value.SetFloat("_Glossiness",gloss);
            value.EnableKeyword("_NORMALMAP");
            return value;
        }

        private static GameObject Model(string filename,bool rigged)
        {
            string path=$"{Art}/{filename}.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false;
            importer.animationType=rigged ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable=true;
            importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.generateSecondaryUV=false;
            importer.SaveAndReimport();
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        internal static void ImportSkin(Material coat,Material fibers,Material horns,Material whiteCoat,Material whiteFibers,
            Material hair=null,Material whiteHair=null)
        {
            var reference=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"));
            var model=Model("ReferenceIbex",true);
            try
            {
                var rig=reference.GetComponentsInChildren<Transform>(true).First(t=>t.name=="GoatRig_A");
                var names=rig.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                model.transform.localScale=Vector3.one*2f;
                var renderer=model.GetComponentInChildren<SkinnedMeshRenderer>();
                if(!renderer) throw new InvalidOperationException("Reference ibex has no skinned renderer.");
                var mesh=Object.Instantiate(renderer.sharedMesh);
                mesh.name="Reference ibex on original elastic skeleton";
                // Blender joins UV layers by name. Accept older appearance
                // exports which kept the hair atlas in a third named layer.
                int hairSlot=Array.FindIndex(renderer.sharedMaterials,m=>m && m.name.Contains("HairCards"));
                if(hairSlot>=0 && mesh.uv3.Length==mesh.vertexCount)
                {
                    var surfaceUV=mesh.uv;
                    var hairUV=mesh.uv2;
                    foreach(int vertex in mesh.GetTriangles(hairSlot)) surfaceUV[vertex]=hairUV[vertex];
                    mesh.uv=surfaceUV;
                    mesh.uv2=mesh.uv3;
                    mesh.uv3=Array.Empty<Vector2>();
                    mesh.RecalculateTangents();
                }
                var matrix=rig.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                mesh.normals=mesh.normals.Select(v=>matrix.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                mesh.tangents=mesh.tangents.Select(t=>
                {
                    var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;
                    return new Vector4(v.x,v.y,v.z,t.w*Mathf.Sign(matrix.determinant));
                }).ToArray();
                mesh.bindposes=renderer.bones.Select(b=>names[b.name].worldToLocalMatrix*rig.localToWorldMatrix).ToArray();
                mesh.RecalculateBounds();
                mesh=hair ? StoreAppearanceMesh(mesh) : StoreMesh(mesh,Art+"/IbexSkin.asset");
                foreach(int number in new[]{11,12})
                {
                    string path=$"Assets/GoatDescent/Resources/GoatSkins/Skin{number:00}.asset";
                    var definition=AssetDatabase.LoadAssetAtPath<GoatSkinDefinition>(path);
                    if(!definition) { definition=ScriptableObject.CreateInstance<GoatSkinDefinition>(); AssetDatabase.CreateAsset(definition,path); }
                    var bodyMaterial=number==11 ? coat : whiteCoat;
                    var fiberMaterial=number==11 ? fibers : whiteFibers;
                    definition.number=number;
                    definition.displayName=number==11 ? "Горный козёл" : "Серебристый козёл";
                    definition.mesh=mesh;
                    definition.material=bodyMaterial;
                    definition.materials=renderer.sharedMaterials.Select(m=>m.name.Contains("HairCards") ? (number==11 ? hair : whiteHair)
                        : m.name.Contains("Fibers") ? fiberMaterial
                        : m.name.Contains("Horns") ? horns : bodyMaterial).ToArray();
                    definition.boneNames=renderer.bones.Select(b=>b.name).ToArray();
                    definition.preview=Resources.Load<Texture2D>($"GoatSkins/Preview{number:00}");
                    BindGameplay(definition,reference,names);
                    EditorUtility.SetDirty(definition);
                    Debug.Log($"REFERENCE_IBEX_SKIN number={number} vertices={mesh.vertexCount} submeshes={mesh.subMeshCount} bones={definition.boneNames.Length} bounds={mesh.bounds}");
                }
            }
            finally { Object.DestroyImmediate(reference); Object.DestroyImmediate(model); }
        }

        [MenuItem("Tools/Goat Descent/Bind ibex gameplay animations")]
        public static void BindGameplay()
        {
            var reference=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"));
            try
            {
                var rig=reference.GetComponentsInChildren<Transform>(true).First(t=>t.name=="GoatRig_A");
                var names=rig.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                foreach(int number in new[]{11,12})
                {
                    var definition=GoatSkin.Definition(number);
                    if(!definition) throw new InvalidOperationException("Missing ibex skin "+number);
                    BindGameplay(definition,reference,names);
                    EditorUtility.SetDirty(definition);
                    Debug.Log($"IBEX_GAMEPLAY_BOUND skin={number} clips={definition.animations.Length} bodyAttachments=true");
                }
                AssetDatabase.SaveAssets();
            }
            finally {Object.DestroyImmediate(reference);}
        }
        private static void BindGameplay(GoatSkinDefinition definition,GameObject reference,System.Collections.Generic.Dictionary<string,Transform> names)
        {
            var animation=reference.GetComponentInChildren<Animation>();
            definition.animations=animation.Cast<AnimationState>().Select(s=>s.clip).Distinct().ToArray();
            var grass=AdaptGrass(animation.GetClip("Goat_EatGrass"),animation,names["Neck"]);
            definition.animations=definition.animations.Select(c=>c.name=="Goat_EatGrass" ? grass : c).ToArray();
            Vector3 BoneLocal(string boneName,Vector3 authoredPoint)
            {
                var bone=names[boneName];
                var world=new Vector3(-authoredPoint.x,authoredPoint.z,-authoredPoint.y)*2f;
                return Vector3.Scale(bone.InverseTransformPoint(world),bone.lossyScale);
            }
            definition.useCustomHeadAttachments=true;
            definition.headCollisionOffset=BoneLocal("Head",new Vector3(0f,-.395f,1.00f));
            definition.noseOffset=BoneLocal("Head",new Vector3(0f,-.607f,.914f));
            definition.hornOffset=BoneLocal("Head",new Vector3(0f,-.352f,1.038f+(1.075f-1.038f)/1.5f));
            definition.useCustomBodyAttachments=true;
            definition.droppingsOffset=BoneLocal("Pelvis",new Vector3(0f,.55f,.47f));
            definition.urineOffset=BoneLocal("Pelvis",new Vector3(-.025f,.33f,.40f));
            definition.grassNeckBendDegrees=95f;
        }
        private static AnimationClip AdaptGrass(AnimationClip source,Animation animation,Transform neck)
        {
            const string directory=Art+"/Animations";
            Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            const string path=directory+"/Goat_EatGrass.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip) {clip=Object.Instantiate(source);AssetDatabase.CreateAsset(clip,path);}
            else EditorUtility.CopySerialized(source,clip);
            clip.name="Goat_EatGrass";clip.legacy=true;
            string bonePath=AnimationUtility.CalculateTransformPath(neck,animation.transform);
            var channels=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            int count=Mathf.CeilToInt(source.length*30f);
            Quaternion previous=Quaternion.identity;
            for(int frame=0;frame<=count;frame++)
            {
                float time=source.length*frame/count;
                source.SampleAnimation(animation.gameObject,time);
                float blend=Mathf.Min(Mathf.SmoothStep(0,1,time/.4f),Mathf.SmoothStep(0,1,(source.length-time)/.4f));
                // The ibex has a longer, higher neck than the original model.
                Quaternion rotation=Quaternion.AngleAxis(55f*blend,Vector3.right)*neck.localRotation;
                if(frame>0 && Quaternion.Dot(previous,rotation)<0)
                    rotation=new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
                previous=rotation;
                for(int axis=0;axis<4;axis++) channels[axis].AddKey(time,rotation[axis]);
            }
            for(int axis=0;axis<4;axis++)
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),channels[axis]);
            clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);
            animation.GetClip("Goat_Idle").SampleAnimation(animation.gameObject,0);
            return clip;
        }

        private static Mesh StoreMesh(Mesh value,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!existing) { AssetDatabase.CreateAsset(value,path); return value; }
            EditorUtility.CopySerialized(value,existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(value);
            return existing;
        }

        private static Mesh StoreAppearanceMesh(Mesh value)
        {
            const string path=Art+"/IbexAppearanceMesh.asset";
            var container=AssetDatabase.LoadAssetAtPath<IbexAppearanceMeshAsset>(path);
            if(!container)
            {
                container=ScriptableObject.CreateInstance<IbexAppearanceMeshAsset>();
                container.name="IbexAppearanceMesh";
                AssetDatabase.CreateAsset(container,path);
                AssetDatabase.AddObjectToAsset(value,container);
                container.mesh=value;
                EditorUtility.SetDirty(container);
                AssetDatabase.SaveAssetIfDirty(container);
                return value;
            }
            var existing=container.mesh;
            existing.Clear();
            existing.indexFormat=value.indexFormat;
            existing.name=value.name;
            existing.vertices=value.vertices;
            existing.normals=value.normals;
            existing.tangents=value.tangents;
            existing.colors32=value.colors32;
            existing.uv=value.uv;
            existing.uv2=value.uv2;
            existing.bindposes=value.bindposes;
            existing.boneWeights=value.boneWeights;
            existing.subMeshCount=value.subMeshCount;
            for(int i=0;i<value.subMeshCount;i++)
                existing.SetIndices(value.GetIndices(i),value.GetTopology(i),i,false);
            existing.bounds=value.bounds;
            EditorUtility.SetDirty(existing);
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssetIfDirty(container);
            Object.DestroyImmediate(value);
            return existing;
        }

        private static void ImportCliff(Material material)
        {
            var model=Model("IbexCliff",false);
            var stage=new GameObject("Ibex cliff stage");
            try
            {
                var renderers=model.GetComponentsInChildren<MeshRenderer>();
                var wall=renderers.Where(r=>r.name.StartsWith("CliffStone_") && int.Parse(r.name.Substring(11))>=2).ToArray();
                float centreX=wall.Average(r=>r.bounds.center.x);
                foreach(var renderer in renderers)
                {
                    var original=renderer.GetComponent<MeshFilter>().sharedMesh;
                    var mesh=Object.Instantiate(original);
                    var matrix=renderer.transform.localToWorldMatrix;
                    var vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                    var bounds=new Bounds(vertices[0],Vector3.zero);
                    foreach(var vertex in vertices) bounds.Encapsulate(vertex);
                    Vector3 centre=bounds.center;
                    mesh.vertices=vertices.Select(v=>v-centre).ToArray();
                    mesh.normals=mesh.normals.Select(v=>matrix.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                    if(matrix.determinant<0f)
                        for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
                        {
                            var triangles=mesh.GetTriangles(submesh);
                            for(int i=0;i<triangles.Length;i+=3)
                                (triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
                            mesh.SetTriangles(triangles,submesh,false);
                        }
                    mesh.RecalculateBounds(); mesh.RecalculateTangents();
                    mesh=StoreMesh(mesh,$"{Art}/Meshes/{renderer.name}.asset");
                    var piece=new GameObject(renderer.name);
                    piece.AddComponent<MeshFilter>().sharedMesh=mesh;
                    piece.AddComponent<MeshRenderer>().sharedMaterial=material;
                    piece.AddComponent<MeshCollider>().sharedMesh=mesh;
                    piece.isStatic=true;
                    PrefabUtility.SaveAsPrefabAsset(piece,$"{ResourcesPath}/{renderer.name}.prefab");
                    piece.transform.SetParent(stage.transform,false);
                    if(renderer.name=="LedgePlatform")
                    {
                        piece.transform.localScale=new Vector3(2.8f,2.2f,2.8f);
                        piece.transform.localPosition=Vector3.Scale(centre,piece.transform.localScale);
                    }
                    else
                    {
                        bool isWall=int.Parse(renderer.name.Substring(11))>=2;
                        float scale=isWall ? 1.55f : 2.8f;
                        piece.transform.localScale=Vector3.one*scale;
                        piece.transform.localPosition=centre*scale-(isWall ? Vector3.right*centreX*scale : Vector3.zero);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(stage,$"{ResourcesPath}/IbexCliffStage.prefab");
                Debug.Log("REFERENCE_IBEX_CLIFF pieces="+renderers.Length+" wallCentre="+centreX);
            }
            finally { Object.DestroyImmediate(model); Object.DestroyImmediate(stage); }
        }
    }
}
