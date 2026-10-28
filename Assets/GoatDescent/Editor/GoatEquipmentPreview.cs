using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace GoatDescent.Editor
{
    /// <summary>Art authoring preview; does not simulate gameplay.</summary>
    public static class GoatEquipmentPreview
    {
        private static Transform Bone(GameObject goat,string name)
        { foreach(var bone in goat.GetComponentsInChildren<Transform>()) if(bone.name==name) return bone; return null; }
        [MenuItem("Tools/Goat Descent/Preview climbing equipment")]
        public static void Render()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Equipment preview"); SceneManager.MoveGameObjectToScene(root,scene);
            var goat=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined")); goat.transform.SetParent(root.transform,false);
            var animation=goat.GetComponentInChildren<Animation>();
            var lower=Bone(goat,"FrontLower.R");
            Vector3 tipLocal=lower.InverseTransformPoint(lower.position+Vector3.down*.39f);
            animation["Goat_RightPunch"].clip.SampleAnimation(animation.gameObject,.54f);
            var steel=new Material(Shader.Find("Standard")) { color=new Color(.75f,.82f,.86f) };
            steel.SetFloat("_Metallic",.8f); steel.SetFloat("_Glossiness",.55f);
            var rubber=new Material(Shader.Find("Standard")) { color=new Color(.055f,.07f,.085f) };
            var strap=new Material(Shader.Find("Sprites/Default"));
            var axe=GoatClimbingEquipment.CreateAxe(steel,rubber); axe.SetParent(root.transform,true);
            Vector3 hoof=lower.TransformPoint(tipLocal);
            axe.SetPositionAndRotation(hoof,Quaternion.FromToRotation(new Vector3(.42f,.3f,.11f),Vector3.forward*.45f+Vector3.down*.2f));
            var wrist=GoatClimbingEquipment.Line(axe,"Hoof loop",strap,.025f,new Color(.08f,.12f,.14f),25);
            GoatClimbingEquipment.Loop(wrist,hoof,lower.right,lower.forward,.075f,.095f);
            Vector3 rear=Bone(goat,"Pelvis").position,front=Bone(goat,"ElasticChest").position;
            Vector3 axis=(front-rear).normalized, right=Vector3.Cross(Vector3.up,axis).normalized,up=Vector3.Cross(axis,right).normalized;
            rear+=axis*.12f; front-=axis*.12f;
            for(int i=0;i<2;i++)
            {
                var belt=GoatClimbingEquipment.Line(root.transform,"Orange harness",strap,.065f,new Color(1,.42f,.1f),41);
                GoatClimbingEquipment.Loop(belt,i==0 ? rear : front,right,up,i==0 ? .43f : .39f,i==0 ? .34f : .3f);
                var bridge=GoatClimbingEquipment.Line(root.transform,"Side strap",strap,.065f,new Color(1,.42f,.1f),2);
                float side=i==0 ? -1 : 1; bridge.SetPosition(0,rear+right*side*.43f); bridge.SetPosition(1,front+right*side*.39f);
            }
            Vector3 clipPoint=rear+right*.46f+up*.015f;
            var clip=GoatClimbingEquipment.Line(root.transform,"Locking carabiner",strap,.032f,new Color(1,.8f,.24f),25);
            GoatClimbingEquipment.Loop(clip,clipPoint,axis,up,.08f,.12f);
            var cameraObject=new GameObject("Equipment camera"); cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=1.55f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.1f,.13f,.17f);
            camera.transform.position=new Vector3(hoof.x<0 ? -4 : 4,2.5f,4); camera.transform.LookAt((rear+front)*.5f);
            for(int i=0;i<2;i++) { var lamp=new GameObject("Light"); lamp.transform.SetParent(root.transform,false); var light=lamp.AddComponent<Light>();
                light.type=LightType.Directional; light.intensity=i==0 ? 1.2f : .5f; lamp.transform.rotation=Quaternion.Euler(40,i==0 ? -30 : 150,0); }
            Capture(camera,"axe-and-goat");
            axe.gameObject.SetActive(false);
            var rope=GoatClimbingEquipment.Line(root.transform,"Safety rope",strap,.04f,new Color(1,.55f,.08f),49);
            Vector3 anchor=clipPoint+Vector3.right*1.7f+Vector3.up*.6f;
            for(int i=0;i<49;i++) { float t=i/48f; rope.SetPosition(i,Vector3.Lerp(anchor,clipPoint,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*.15f); }
            camera.transform.position=new Vector3(4,2.7f,-4); camera.transform.LookAt((rear+front)*.5f+Vector3.right*.3f); camera.orthographicSize=1.85f;
            Capture(camera,"harness-and-rope");
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(steel); Object.DestroyImmediate(rubber); Object.DestroyImmediate(strap);
            Debug.Log("EQUIPMENT_AUTHORING_PREVIEW_READY");
        }
        private static void Capture(Camera camera,string label)
        {
            var target=new RenderTexture(1200,900,24); var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1200,900),0,0); texture.Apply();
            string folder="A:/GameDev/Projects/goat/Builds/GearStyle"; Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,label+".png"),texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
        }
    }
}
