using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace GoatDescent.Editor
{
    public static class GoatToiletAssets
    {
        private const string ResourcesPath="Assets/GoatDescent/Resources/";
        [MenuItem("Tools/Goat Descent/Import toilet actions and effects")]
        public static void Create()
        {
            PairAnimationImporter.ImportToilet();
            Material("GoatDroppingMaterial","Standard",new Color(.22f,.085f,.025f));
            Material("GoatUrine","Sprites/Default",Color.white);
            string meshPath=ResourcesPath+"GoatDropping.asset";
            if(!AssetDatabase.LoadAssetAtPath<Mesh>(meshPath))
            {
                var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var mesh=Object.Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);
                mesh.name="Small oval goat dropping";
                mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(v,new Vector3(.86f,1.18f,.8f))).ToArray();
                mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
                Object.DestroyImmediate(sphere);
            }
            AssetDatabase.SaveAssets();
            Render();
            Debug.Log("GOAT_TOILET_ASSETS_READY clips=Poop,Pee effects=droppings,urine heal=10");
        }
        public static void Render()
        {
            Render("Goat_Poop",GoatToiletKind.Poop);
            Render("Goat_Pee",GoatToiletKind.Pee);
        }
        private static void Material(string name,string shader,Color color)
        {
            string path=ResourcesPath+name+".mat";
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value) {value=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(value,path);}
            value.color=color;
            if(value.HasProperty("_Glossiness")) value.SetFloat("_Glossiness",.14f);
            EditorUtility.SetDirty(value);
        }
        private static void Render(string clipName,GoatToiletKind kind)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Toilet action artwork");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var groundMaterial=new Material(Shader.Find("Standard")) {color=new Color(.32f,.40f,.25f)};
            var target=new RenderTexture(1200,900,24);var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);
            var baked=new Mesh();
            try
            {
                var rig=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"),root.transform);
                rig.name="VisualRoot — replaceable goat model";
                root.AddComponent<GoatSkin>().Set(GoatSkin.DefaultNumber);
                foreach(var t in rig.GetComponentsInChildren<Transform>(true)) if(t.name=="GoatRig_B") t.gameObject.SetActive(false);
                var animation=rig.GetComponentInChildren<Animation>();animation[clipName].clip.SampleAnimation(animation.gameObject,1.4f);
                var renderer=rig.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.enabled);
                renderer.BakeMesh(baked,false);renderer.enabled=false;
                var pose=new GameObject("Sampled toilet pose");pose.transform.SetParent(root.transform,false);
                pose.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);
                pose.AddComponent<MeshFilter>().sharedMesh=baked;pose.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                var pelvis=rig.GetComponentsInChildren<Transform>().First(t=>t.name=="Pelvis");
                var chest=rig.GetComponentsInChildren<Transform>().First(t=>t.name=="ElasticChest");
                // The authoring origin is the hoof level; no level generator is needed.
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform,false);
                ground.transform.localPosition=new Vector3(0,-.10f,0);ground.transform.localScale=new Vector3(9,.2f,9);
                ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                root.AddComponent<GoatToiletAction>().RenderArtPreview(kind,1.65f,pelvis.position,chest.position);
                var lamp=new GameObject("Preview sun");lamp.transform.SetParent(root.transform,false);
                var sun=lamp.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.shadows=LightShadows.Soft;lamp.transform.rotation=Quaternion.Euler(48,-30,0);
                var cameraObject=new GameObject("Preview camera");cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.cameraType=CameraType.Preview;camera.scene=scene;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.65f,.72f);
                camera.transform.position=new Vector3(4.3f,2.2f,-2.1f);camera.transform.LookAt(new Vector3(0,.85f,0));
                camera.fieldOfView=46;camera.nearClipPlane=.1f;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
                Directory.CreateDirectory("Builds/ToiletActions");File.WriteAllBytes("Builds/ToiletActions/Unity_"+clipName+".png",texture.EncodeToPNG());
                camera.targetTexture=null;
            }
            finally
            {
                RenderTexture.active=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);
                Object.DestroyImmediate(groundMaterial);EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(baked);
            }
        }
    }
}
