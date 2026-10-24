using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace GoatDescent.Editor
{
    /// <summary>Produces presentation frames from the actual game scene, without saving a staged scene.</summary>
    [InitializeOnLoad]
    public static class ReferenceIbexPresentation
    {
        private const string CaptureKey="ReferenceIbex.Presentation";
        private static double captureAt;
        private static bool prepared;

        static ReferenceIbexPresentation()
        {
            if(SessionState.GetBool(CaptureKey,false))
            {
                captureAt=EditorApplication.timeSinceStartup+5;
                EditorApplication.update+=Capture;
            }
        }

        [MenuItem("Tools/Goat Descent/Show reference ibex in game")]
        public static void ShowInGame()
        {
            ReferenceIbexImporter.BuildSkins();
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/GoatPlayground.unity");
            SessionState.SetBool(CaptureKey,true);
            captureAt=EditorApplication.timeSinceStartup+5;
            prepared=false;
            EditorApplication.update-=Capture;
            EditorApplication.update+=Capture;
            EditorApplication.isPlaying=true;
        }

        private static void Capture()
        {
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<captureAt) return;
            var pair=LocalGoatPair.Instance;
            if(!pair || !pair.Primary || !Camera.main) return;
            if(!prepared)
            {
                pair.Primary.GetComponent<GoatSkin>().Set(11);
                pair.Secondary.GetComponent<GoatSkin>().Set(12);
                var follow=Camera.main.GetComponent<ThirdPersonGoatCamera>();
                if(follow) follow.enabled=false;
                Cursor.lockState=CursorLockMode.None;
                Cursor.visible=true;
                prepared=true;
                captureAt=EditorApplication.timeSinceStartup+3;
                return;
            }
            var goat=pair.Primary;
            var stage=GameObject.Find("Ibex stone platform and cliff");
            Vector3 forward=stage ? stage.transform.forward : goat.GetComponent<GoatVisualController>().Facing;
            forward=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 centre=goat.transform.position+Vector3.up*1.3f;
            var camera=Camera.main;
            camera.transform.position=centre+forward*6f-right*4.1f+Vector3.up*4.3f;
            camera.transform.LookAt(centre+forward*.1f);
            camera.fieldOfView=42f;
            Directory.CreateDirectory("Builds/ReferenceIbex");
            Frame(camera,"Builds/ReferenceIbex/InGame.png",1600,1000);
            Frame(camera,"Builds/ReferenceIbex/ColorVariants.png",1600,1000);
            camera.transform.position=centre+forward*3.5f-right*2.4f+Vector3.up*2.1f;
            camera.transform.LookAt(centre+Vector3.up*.25f);
            camera.fieldOfView=43f;
            Frame(camera,"Builds/ReferenceIbex/CloseUp.png",1400,1000);
            Debug.Log("REFERENCE_IBEX_PRESENTATION_READY skin="+goat.GetComponent<GoatSkin>().Number+
                " secondarySkin="+pair.Secondary.GetComponent<GoatSkin>().Number+
                " stage="+(stage!=null)+" staticColliders="+(stage ? stage.GetComponentsInChildren<MeshCollider>().Length : 0));
            SessionState.SetBool(CaptureKey,false);
            EditorApplication.update-=Capture;
            if(Application.isBatchMode) { EditorApplication.isPlaying=false; EditorApplication.Exit(0); }
        }

        private static void Frame(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0);
            image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=null;
            RenderTexture.active=previous;
            target.Release();
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }
    }
}
