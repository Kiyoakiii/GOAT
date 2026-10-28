using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace GoatDescent.Editor
{
    [InitializeOnLoad]
    public static class GoatSkinPreview
    {
        static GoatSkinPreview()
        {
            if(SessionState.GetBool("GoatSkinCapture",false))
            { captureAt=EditorApplication.timeSinceStartup+8; EditorApplication.update+=CaptureMenuFrame; }
        }
        private static double captureAt;
        private static bool opened,captured;
        public static void ImportAndRender() { GoatSkinImporter.Build(); Render(); }
        public static void CaptureMenu()
        {
            SessionState.SetBool("GoatSkinCapture",true);
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/GoatPlayground.unity");
            var gameType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView=EditorWindow.GetWindow(gameType); gameView.position=new Rect(0,0,1280,760); gameView.Show();
            captureAt=EditorApplication.timeSinceStartup+10;
            EditorApplication.update+=CaptureMenuFrame;
            EditorApplication.isPlaying=true;
        }
        private static void CaptureMenuFrame()
        {
            if(EditorApplication.timeSinceStartup<captureAt || !EditorApplication.isPlaying) return;
            if(!opened)
            {
                var menu=Object.FindFirstObjectByType<GoatSkinMenu>(); if(!menu) return;
                menu.Open(); opened=true; captureAt=EditorApplication.timeSinceStartup+2; return;
            }
            if(!captured)
            {
                Directory.CreateDirectory("Builds/GoatSkins");
                ScreenCapture.CaptureScreenshot("Builds/GoatSkins/Menu.png");
                captured=true; captureAt=EditorApplication.timeSinceStartup+3; return;
            }
            Debug.Log("GOAT_SKIN_MENU_AUTHORING_CAPTURE_READY");
            SessionState.SetBool("GoatSkinCapture",false);
            EditorApplication.update-=CaptureMenuFrame; EditorApplication.isPlaying=false; EditorApplication.Exit(0);
        }
        [MenuItem("Tools/Goat Descent/Render selected goat skins")]
        public static void Render() => Render(GoatSkin.Numbers);
        public static void Render(int[] numbers)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Goat skins art preview"); SceneManager.MoveGameObjectToScene(root,scene);
            var cameraNode=new GameObject("Skin preview camera"); cameraNode.transform.SetParent(root.transform);
            var camera=cameraNode.AddComponent<Camera>(); camera.scene=scene;
            camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.14f,.19f,.23f);
            for(int i=0;i<2;i++)
            {
                var lamp=new GameObject("Studio light"); lamp.transform.SetParent(root.transform);
                var light=lamp.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=i==0 ? 1.2f : .6f;
                lamp.transform.rotation=Quaternion.Euler(35,i==0 ? -30 : 150,0);
            }
            foreach(int number in numbers)
            {
                var hero=new GameObject("Goat "+number); hero.transform.SetParent(root.transform);
                var reference=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"),hero.transform);
                reference.name="VisualRoot — replaceable goat model";
                if(number==0)
                {
                    var animation=reference.GetComponentInChildren<Animation>();
                    var idle=animation ? animation.GetClip("Goat_Idle") : null;
                    if(idle) idle.SampleAnimation(animation.gameObject,0f);
                }
                hero.AddComponent<GoatSkin>().Set(number);
                var renderer=hero.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach(var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if(r.enabled && (!renderer.enabled || r.bounds.size.sqrMagnitude>renderer.bounds.size.sqrMagnitude)) renderer=r;
                var local=renderer.sharedMesh.bounds;
                var bounds=new Bounds(renderer.transform.TransformPoint(local.center),Vector3.Scale(local.size,renderer.transform.lossyScale));
                // The original FBX has unreliable pre-frame renderer bounds. Frame its authored hoof origin.
                if(number==0) bounds=new Bounds(hero.transform.position+Vector3.up,new Vector3(3f,2.8f,2f));
                camera.transform.position=bounds.center+new Vector3(4,2.1f,4);
                camera.transform.LookAt(bounds.center);
                camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.magnitude*.62f)*.65f;
                var target=new RenderTexture(900,800,24); var image=new Texture2D(900,800,TextureFormat.RGB24,false);
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,900,800),0,0); image.Apply();
                Directory.CreateDirectory("Builds/GoatSkins"); File.WriteAllBytes($"Builds/GoatSkins/Skin{number:00}.png",image.EncodeToPNG());
                camera.targetTexture=null; RenderTexture.active=null; target.Release();
                Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(hero);
            }
            EditorSceneManager.ClosePreviewScene(scene);
            Debug.Log("GOAT_SKIN_ART_PREVIEW_READY");
        }
    }
}
