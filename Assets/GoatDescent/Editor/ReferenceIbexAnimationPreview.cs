using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace GoatDescent.Editor
{
    /// <summary>Artwork samples of the shared clips on the new skin, without a gameplay scene.</summary>
    public static class ReferenceIbexAnimationPreview
    {
        [MenuItem("Tools/Goat Descent/Render ibex animation poses")]
        public static void Render()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Ibex animation artwork");SceneManager.MoveGameObjectToScene(root,scene);
            var target=new RenderTexture(960,720,24);
            var texture=new Texture2D(960,720,TextureFormat.RGB24,false);
            var groundMaterial=new Material(Shader.Find("Standard")){color=new Color(.28f,.34f,.29f)};
            var baked=new Mesh();
            var previous=RenderTexture.active;
            try
            {
                var hero=new GameObject("Dark mountain goat");hero.transform.SetParent(root.transform,false);
                var reference=Object.Instantiate(Resources.Load<GameObject>("GoatDuoRefined"),hero.transform);
                reference.name="VisualRoot — replaceable goat model";
                hero.AddComponent<GoatSkin>().Set(GoatSkin.DefaultNumber);
                var animation=reference.GetComponentInChildren<Animation>();animation.Stop();animation.enabled=false;
                var renderer=hero.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.enabled);
                // Freeze each CPU-skinned sample for preview rendering. A hidden
                // Editor does not advance the GPU skinning pass between samples.
                var poseNode=new GameObject("Sampled mesh");poseNode.transform.SetParent(hero.transform,false);
                poseNode.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);
                poseNode.AddComponent<MeshFilter>().sharedMesh=baked;
                poseNode.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                renderer.enabled=false;
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);
                floor.transform.position=new Vector3(0,-.07f,0);floor.transform.localScale=new Vector3(12,.12f,12);
                floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                for(int i=0;i<2;i++)
                {
                    var node=new GameObject("Studio light");node.transform.SetParent(root.transform,false);
                    var light=node.AddComponent<Light>();light.type=LightType.Directional;light.intensity=i==0 ? 1.25f : .65f;
                    node.transform.rotation=Quaternion.Euler(40,i==0 ? -40 : 145,0);
                }
                var cameraNode=new GameObject("Pose camera");cameraNode.transform.SetParent(root.transform,false);
                var camera=cameraNode.AddComponent<Camera>();camera.scene=scene;camera.cameraType=CameraType.Preview;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.34f,.40f);
                camera.orthographic=true;camera.nearClipPlane=.05f;camera.targetTexture=target;
                const string output="Builds/ReferenceIbex/Animations";Directory.CreateDirectory(output);
                foreach(var clip in GoatSkin.Definition(GoatSkin.DefaultNumber).animations)
                {
                    animation.GetClip("Goat_Idle").SampleAnimation(animation.gameObject,0);
                    float time=clip.name=="Goat_Pee" || clip.name=="Goat_Poop" || clip.name=="Goat_EatGrass"
                        ? 1.4f : clip.name=="Goat_Walk" ? .28f : clip.length*.45f;
                    clip.SampleAnimation(animation.gameObject,Mathf.Min(time,clip.length));
                    renderer.BakeMesh(baked,false);
                    var bounds=baked.bounds;
                    // The original FBX's parent scale is already included in
                    // the baked vertices; apply only its orientation here.
                    Vector3 centre=renderer.transform.position+renderer.transform.rotation*bounds.center;
                    Vector3 size=bounds.size;
                    camera.transform.position=centre+new Vector3(4f,1.3f,3.3f);
                    camera.transform.LookAt(centre);
                    camera.orthographicSize=Mathf.Max(1.7f,size.magnitude*.45f);
                    camera.Render();RenderTexture.active=target;
                    texture.ReadPixels(new Rect(0,0,960,720),0,0);texture.Apply();
                    File.WriteAllBytes(output+"/"+clip.name+".png",texture.EncodeToPNG());
                }
                File.WriteAllLines(output+"/Clips.txt",GoatSkin.Definition(GoatSkin.DefaultNumber).animations.Select(c=>c.name));
                Debug.Log("IBEX_ANIMATION_ARTWORK_READY clips="+GoatSkin.Definition(GoatSkin.DefaultNumber).animations.Length);
            }
            finally
            {
                foreach(var camera in root.GetComponentsInChildren<Camera>()) camera.targetTexture=null;
                RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);
                Object.DestroyImmediate(baked);Object.DestroyImmediate(groundMaterial);EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
