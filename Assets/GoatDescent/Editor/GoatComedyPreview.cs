using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.Editor
{
    /// <summary>Authoring preview of imported clips, not a gameplay/physics test.</summary>
    public static class GoatComedyPreview
    {
        [MenuItem("Tools/Goat Descent/Render Comedy Poses")]
        public static void Render()
        {
            PairAnimationImporter.ImportComedy();
            var scene=EditorSceneManager.NewPreviewScene();
            var cameraObject=new GameObject("Comedy pose camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.15f,.19f);
            camera.orthographic=true; camera.orthographicSize=2.7f; camera.farClipPlane=50;
            camera.transform.position=new Vector3(4,2.5f,5);
            camera.transform.LookAt(new Vector3(0,.9f,0));
            var lamp=new GameObject("Comedy pose light");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp,scene);
            var light=lamp.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.25f;
            light.transform.rotation=Quaternion.Euler(45,-30,0);
            var fillObject=new GameObject("Comedy pose fill");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);
            var fill=fillObject.AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.6f;
            fill.transform.rotation=Quaternion.Euler(25,150,0);
            var materials=new List<Material>();
            var prefab=Resources.Load<GameObject>("GoatDuoRefined");
            var goat=Object.Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(goat,scene);
            var animation=goat.GetComponentInChildren<Animation>();
            animation["Goat_PlayDeadHold"].clip.SampleAnimation(animation.gameObject,.01f);
            Bounds bounds=goat.GetComponentInChildren<Renderer>().bounds;
            foreach(var renderer in goat.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            camera.transform.position=bounds.center+new Vector3(4,2.4f,5);
            camera.transform.LookAt(bounds.center);
            Capture(camera,"unity-feigned-death");
            // Idle does not key every torso bone. Start with a fresh model and
            // sample the recovery's final rest pose instead of retaining the previous roll.
            Object.DestroyImmediate(goat);
            goat=Object.Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(goat,scene);
            animation=goat.GetComponentInChildren<Animation>();
            animation["Goat_GetUp"].clip.SampleAnimation(animation.gameObject,1.04f);
            var rider=Object.Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rider,scene);
            rider.transform.position=new Vector3(0,.85f,-.16f);
            var riderAnimation=rider.GetComponentInChildren<Animation>();
            riderAnimation["Goat_BackRide"].clip.SampleAnimation(riderAnimation.gameObject,.01f);
            foreach(var renderer in rider.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    if(!slots[i] || !slots[i].name.Contains("hideWhite")) continue;
                    var mat=new Material(slots[i]); mat.color=new Color(.55f,.27f,.09f); materials.Add(mat); slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
            camera.transform.position=new Vector3(4,3.2f,5); camera.transform.LookAt(new Vector3(0,1.45f,0));
            Capture(camera,"unity-back-seat");
            foreach(var mat in materials) Object.DestroyImmediate(mat);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        private static void Capture(Camera camera,string name)
        {
            var render=new RenderTexture(1200,900,24); var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=render; camera.Render(); RenderTexture.active=render;
                texture.ReadPixels(new Rect(0,0,1200,900),0,0); texture.Apply();
                string output=Path.GetFullPath("Builds/ComedyEvidence/"+name+".png");
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output,texture.EncodeToPNG());
                Debug.Log("COMEDY_POSE_PREVIEW "+output);
            }
            finally
            {
                camera.targetTexture=null; RenderTexture.active=previous; render.Release();
                Object.DestroyImmediate(render); Object.DestroyImmediate(texture);
            }
        }
    }
}
