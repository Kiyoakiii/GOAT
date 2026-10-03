using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GoatDescent.ProceduralWorld.Editor
{
    /// <summary>A layered forest of weathered sandstone towers on the western coast.</summary>
    public static class MistyPillarsVistaEditor
    {
        private const string ScenePath = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
        private const string AssetRoot = "Assets/GoatDescent/Vista/Generated/MistyPillars";
        private const string RootName = MistyPillarsVistaBuilder.RootName;

        [MenuItem("Tools/Procedural World/Apply Misty Pillars Vista")]
        public static void ApplyToSavedWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding the vista.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                ApplyToScene(scene);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        public static void ApplyToScene(Scene scene)
        {
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
                if (!AssetDatabase.IsValidFolder(AssetRoot)) { Directory.CreateDirectory(AssetRoot); AssetDatabase.Refresh(); }
                foreach (GameObject oldRoot in scene.GetRootGameObjects())
                    if (oldRoot.name == RootName) Object.DestroyImmediate(oldRoot);
                Material rock = GetMaterial("M_PillarStone", "GoatDescent/Weathered Sandstone");
                rock.SetColor("_StoneColor", new Color(.52f, .45f, .35f));
                rock.SetColor("_ShadowStone", new Color(.17f, .205f, .20f));
                rock.SetColor("_MossColor", new Color(.11f, .20f, .073f));
                Material foliage = GetMaterial("M_ForestCanopy", "GoatDescent/Valley Foliage");
                foliage.SetColor("_Color", new Color(.19f, .27f, .19f));
                Material bark = GetMaterial("M_ValleyBark", "Standard");
                bark.color = new Color(.17f, .14f, .10f); bark.SetFloat("_Glossiness", .06f);
                Material fog = GetMaterial("M_ValleyVolume", "GoatDescent/Volumetric Valley Mist");
                fog.SetColor("_FogLight", new Color(.88f, .92f, .93f));
                fog.SetColor("_FogShadow", new Color(.50f, .61f, .66f));
                fog.SetFloat("_Density", .038f); fog.SetFloat("_Top", 250f); fog.SetFloat("_NoiseScale", .009f);
                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(MistyPillarsVistaBuilder.RootPosition, MistyPillarsVistaBuilder.RootRotation);
                root.AddComponent<ValleyMistDepth>();
                var sink = new AssetMeshSink();
                foreach (var spec in MistyPillarsVistaBuilder.BuildLayout(MistyPillarsVistaBuilder.DefaultTowerCount, MistyPillarsVistaBuilder.DefaultRandomSeed, 260f, 415f, 20f, 44f))
                    MistyPillarsVistaBuilder.CreateTower(root.transform, spec, rock, foliage, bark, sink, true);
                var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
                volume.name = "Rolling volumetric valley cloud";
                volume.transform.SetParent(root.transform, false);
                volume.transform.localPosition = new Vector3(0f, 150f, 590f);
                volume.transform.localScale = new Vector3(1800f, 600f, 2250f);
                Object.DestroyImmediate(volume.GetComponent<Collider>());
                var renderer = volume.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = fog; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                CreateCamera(root.transform, "MistyPillarsVista", new Vector3(0f, 285f, -300f), new Vector3(-12f, 235f, 235f), 39f);
                CreateCamera(root.transform, "MistyPillarsClose", new Vector3(35f, 305f, -130f), new Vector3(-63f, 242f, 170f), 42f);
                ApplyLighting(scene);
                DisableRuntimeGeneration(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("MISTY_PILLARS_READY towers=" + MistyPillarsVistaBuilder.DefaultTowerCount + " volumetricFog=true scene=" + scene.path);
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        private static void DisableRuntimeGeneration(Scene scene)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (var controller in sceneRoot.GetComponentsInChildren<MistyPillarsSceneController>(true))
                    controller.SetGenerateOnStart(false);
        }

        private static void CreateCamera(Transform parent, string name, Vector3 position, Vector3 target, float fov)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.LookAt(parent.TransformPoint(target));
            Camera camera = obj.AddComponent<Camera>();
            camera.fieldOfView = fov; camera.nearClipPlane = .5f; camera.farClipPlane = 2600f;
            camera.clearFlags = CameraClearFlags.Skybox; camera.depthTextureMode = DepthTextureMode.Depth;
            camera.allowHDR = true; camera.allowMSAA = true; camera.enabled = false;
        }

        private static void ApplyLighting(Scene scene)
        {
            Material sky = GetMaterial("M_PillarDaylightSky", "GoatDescent/Overcast Valley Sky");
            sky.SetColor("_Horizon", new Color(.88f, .92f, .94f)); sky.SetColor("_Zenith", new Color(.72f, .81f, .87f));
            RenderSettings.skybox = sky;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.71f, .80f, .84f); RenderSettings.fogDensity = .0008f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.67f, .73f, .77f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .50f, .50f);
            RenderSettings.ambientGroundColor = new Color(.17f, .20f, .17f);
            RenderSettings.reflectionIntensity = .12f;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == "LifeDay Procedural Volumetric Cloudscape") sceneRoot.SetActive(false);
                foreach (Light light in sceneRoot.GetComponentsInChildren<Light>(true))
                {
                    if (light.name == "LifeDay Summit Sun Rays")
                    {
                        light.color = new Color(1f, .96f, .86f); light.intensity = 1.2f;
                        light.transform.rotation = Quaternion.Euler(52f, -125f, 0f);
                        light.shadows = LightShadows.Soft; light.shadowStrength = .7f; RenderSettings.sun = light;
                    }
                    if (light.name == "LifeDay Cold Directional Fill") light.intensity = .20f;
                }
            }
        }

        private static Material GetMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Missing or unsupported shader: " + shaderName);
            string path = AssetRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            else material.shader = shader;
            EditorUtility.SetDirty(material); return material;
        }

        private sealed class AssetMeshSink : MistyPillarsVistaBuilder.IMeshSink
        {
            public GameObject Emit(Transform parent, string objectName, string assetName, PillarMeshBuilder builder, Material material, bool flatten)
            {
                string path = AssetRoot + "/" + assetName + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh != null && !mesh.isReadable)
                {
                    AssetDatabase.DeleteAsset(path);
                    mesh = null;
                }
                if (mesh == null) { mesh = new Mesh { name = assetName }; AssetDatabase.CreateAsset(mesh, path); }
                if (flatten) builder.FlattenFaces();
                mesh.Clear(); mesh.indexFormat = builder.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(builder.vertices); mesh.SetColors(builder.colors); mesh.SetTriangles(builder.triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(true); EditorUtility.SetDirty(mesh);
                var obj = new GameObject(objectName); obj.transform.SetParent(parent, false);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
                return obj;
            }
        }
    }
}
