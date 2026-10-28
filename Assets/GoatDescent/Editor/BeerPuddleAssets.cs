using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace GoatDescent.Editor
{
    public static class BeerPuddleAssets
    {
        private const string Art="Assets/GoatDescent/Art/BeerPuddle";
        [MenuItem("Tools/Goat Descent/Create beer puddle assets")]
        public static void Create()
        {
            RefreshArtwork();
            Render();
        }
        public static void RefreshArtwork()
        {
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var mechanics=Asset<GoatMechanicsSettings>("Assets/GoatDescent/Resources/GoatMechanics.asset");
            var settings=Asset<BeerPuddleSettings>("Assets/GoatDescent/Resources/BeerPuddleSettings.asset");
            var beer=Material("Beer","GoatDescent/Beer Puddle",settings.healingColor);
            var rim=Material("Wet border","GoatDescent/Beer Puddle",new Color(.045f,.25f,.15f,.2f));
            beer.SetFloat("_SurfaceStyle",0f);rim.SetFloat("_SurfaceStyle",2f);
            var obj=new GameObject("Healing puddle");
            var puddle=obj.AddComponent<BeerPuddle>();puddle.Configure(settings);
            var borderRenderer=AddMesh(obj,"Wet outline",Outline(settings.radii*1.035f,settings.surfaceOffset-.012f),rim);
            var waterRenderer=AddMesh(obj,"Emerald water",Outline(settings.radii,settings.surfaceOffset),beer);
            puddle.ConfigureVisuals(waterRenderer,null,borderRenderer);
            PrefabUtility.SaveAsPrefabAsset(obj,"Assets/GoatDescent/Resources/BeerPuddle.prefab");
            Object.DestroyImmediate(obj);
            EditorUtility.SetDirty(mechanics);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("HEALING_PUDDLE_ASSETS_READY prefab=BeerPuddle mechanics=GoatMechanics");
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        {
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(!value) {value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}
            return value;
        }
        private static Material Material(string name,string shader,Color color)
        {
            string path=Art+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_Color",color);
            material.renderQueue=name=="Foam" ? 3010 : name=="Wet border" ? 2999 : 3000;
            EditorUtility.SetDirty(material);return material;
        }
        private static MeshRenderer AddMesh(GameObject root,string name,Mesh source,Material material)
        {
            // Reuse the existing assets/GUIDs after changing their visible names.
            string assetName=name=="Emerald water" ? "Amber beer" : name=="Pearl foam" ? "Foam bubbles" : name;
            string path=Art+"/"+assetName+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh) {mesh=source;AssetDatabase.CreateAsset(mesh,path);}
            else
            {
                mesh.Clear();mesh.vertices=source.vertices;mesh.uv=source.uv;mesh.triangles=source.triangles;
                mesh.RecalculateNormals();mesh.RecalculateBounds();Object.DestroyImmediate(source);EditorUtility.SetDirty(mesh);
            }
            var node=new GameObject(name);node.transform.SetParent(root.transform,false);
            node.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            return renderer;
        }
        private static Mesh Outline(Vector2 radii,float y)
        {
            const int count=96;
            var vertices=new Vector3[count+1];var uv=new Vector2[count+1];var triangles=new int[count*3];
            vertices[0]=Vector3.up*y;uv[0]=Vector2.one*.5f;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                float wobble=1f+Mathf.Sin(angle*3f+.4f)*.055f+Mathf.Sin(angle*7f)*.027f;
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radii.x*wobble,y,Mathf.Sin(angle)*radii.y*wobble);
                uv[i+1]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.5f+Vector2.one*.5f;
                triangles[i*3]=0;triangles[i*3+1]=(i+1)%count+1;triangles[i*3+2]=i+1;
            }
            var mesh=new Mesh {name="Organic puddle outline",vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Foam(BeerPuddleSettings settings)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var random=new System.Random(7104);
            for(int bubble=0;bubble<settings.foamBubbles;bubble++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                float r=bubble%4==0 ? (float)random.NextDouble()*.68f : .72f+(float)random.NextDouble()*.19f;
                float size=.025f+(float)random.NextDouble()*.07f;
                Vector3 center=new Vector3(Mathf.Cos(angle)*settings.radii.x*r,settings.surfaceOffset+.006f,Mathf.Sin(angle)*settings.radii.y*r);
                int start=vertices.Count;vertices.Add(center);uv.Add(Vector2.one*.5f);
                for(int i=0;i<12;i++)
                {
                    float theta=i*Mathf.PI*2/12;
                    vertices.Add(center+new Vector3(Mathf.Cos(theta)*size,0,Mathf.Sin(theta)*size));
                    uv.Add(new Vector2(Mathf.Cos(theta),Mathf.Sin(theta))*.5f+Vector2.one*.5f);
                    triangles.Add(start);triangles.Add(start+(i+1)%12+1);triangles.Add(start+i+1);
                }
            }
            var mesh=new Mesh {name="Beer foam islands"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static void Render()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Beer puddle art preview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var puddle=Object.Instantiate(Resources.Load<GameObject>("BeerPuddle"),root.transform);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform,false);
            ground.transform.localPosition=new Vector3(0,-.13f,0);ground.transform.localScale=new Vector3(12,.2f,12);
            var groundMaterial=new Material(Shader.Find("Standard"));groundMaterial.color=new Color(.28f,.37f,.21f);
            ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            var goat=new GameObject("Goat sniffing beer");goat.transform.SetParent(root.transform,false);goat.transform.position=new Vector3(2.5f,.04f,1.3f);
            goat.transform.rotation=Quaternion.LookRotation(-goat.transform.position+Vector3.up*.04f);
            var rig=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"),goat.transform);
            rig.name="VisualRoot — replaceable goat model";
            goat.AddComponent<GoatSkin>().Set(GoatSkin.DefaultNumber);
            foreach(var t in rig.GetComponentsInChildren<Transform>(true)) if(t.name=="GoatRig_B") t.gameObject.SetActive(false);
            var motor=goat.AddComponent<GoatController>();
            puddle.GetComponent<BeerPuddle>().RefreshAromaPreview(motor);
            var light=new GameObject("Preview sun");light.transform.SetParent(root.transform,false);
            var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;light.transform.rotation=Quaternion.Euler(42,-25,0);
            var cameraNode=new GameObject("Preview camera");cameraNode.transform.SetParent(root.transform,false);
            var camera=cameraNode.AddComponent<Camera>();camera.cameraType=CameraType.Preview;
            camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.5f,.67f,.72f);
            camera.transform.position=new Vector3(5.5f,4.5f,-5.5f);camera.transform.LookAt(new Vector3(.2f,.7f,0f));
            camera.fieldOfView=48f;camera.nearClipPlane=.1f;
            var target=new RenderTexture(1400,1000,24);var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();
            Directory.CreateDirectory("Builds/BeerPuddle");File.WriteAllBytes("Builds/BeerPuddle/Preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;target.Release();
            Object.DestroyImmediate(image);Object.DestroyImmediate(target);Object.DestroyImmediate(groundMaterial);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
