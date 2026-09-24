using LifeDay.EditorTools;
using LifeDay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GoatDescent.ProceduralWorld.Editor
{
    /// <summary>
    /// Applies the actual LifeDay sky material, sun and 3D cloud profiles to the
    /// generated Goat world without migrating its terrain/vegetation to URP.
    /// </summary>
    public static class LifeDayAtmosphereEditor
    {
        private const string SkyPath = "Assets/GoatDescent/Art/LifeDaySky/Materials/M_Sky_Custom.mat";
        private const string WorldPath = "Assets/ProceduralWorld/Scenes/ProceduralWorldMilestone.unity";
        private const string CloudRootName = "LifeDay Procedural Volumetric Cloudscape";

        [MenuItem("Tools/Goat Descent/Apply LifeDay Sky, Sun and Clouds to World")]
        public static void ApplyToSavedWorld()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene world = SceneManager.GetSceneByPath(WorldPath);
            bool openedHere = !world.isLoaded;
            if (openedHere)
                world = EditorSceneManager.OpenScene(WorldPath, OpenSceneMode.Additive);

            try
            {
                ApplyToScene(world, true);
                EditorSceneManager.SaveScene(world);
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(world, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        public static void ApplyToScene(Scene scene, bool buildClouds, Vector3? anchor = null)
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null || sky.shader == null || !sky.shader.isSupported)
                throw new System.InvalidOperationException("LifeDay sky material or shader is missing or unsupported: " + SkyPath);

            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.26f, 0.34f, 0.42f);
                RenderSettings.ambientEquatorColor = new Color(0.17f, 0.22f, 0.29f);
                RenderSettings.ambientGroundColor = new Color(0.075f, 0.09f, 0.12f);
                RenderSettings.reflectionIntensity = 0.34f;
                // LifeDay's 0.034 density is sized for its compact arena. Scale fog
                // to this multi-kilometre mountain so the new sky stays visible.
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.008f, 0.018f, 0.03f);
                RenderSettings.fogDensity = 0.00045f;

                Light sun = FindSun(scene);
                if (sun == null)
                {
                    var sunObject = new GameObject("LifeDay Summit Sun Rays");
                    SceneManager.MoveGameObjectToScene(sunObject, scene);
                    sun = sunObject.AddComponent<Light>();
                }
                sun.gameObject.name = "LifeDay Summit Sun Rays";
                sun.type = LightType.Directional;
                sun.intensity = 1.65f;
                sun.color = new Color(1f, 0.57f, 0.30f);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.72f;
                Vector3 sourceSunPosition = new Vector3(-18f, 16f, 122f);
                sun.transform.rotation = Quaternion.LookRotation(-sourceSunPosition.normalized, Vector3.up);
                SkySunSynchronizer sync = sun.GetComponent<SkySunSynchronizer>() ?? sun.gameObject.AddComponent<SkySunSynchronizer>();
                sync.Configure(sun);

                // LifeDay also has a Cold Directional Fill. Its original 0.18
                // intensity is lifted for Goat's much larger, backlit mountain.
                Light fill = FindNamedLight(scene, "LifeDay Cold Directional Fill");
                if (fill == null)
                {
                    var fillObject = new GameObject("LifeDay Cold Directional Fill");
                    SceneManager.MoveGameObjectToScene(fillObject, scene);
                    fill = fillObject.AddComponent<Light>();
                }
                fill.type = LightType.Directional;
                fill.color = new Color(0.34f, 0.48f, 0.72f);
                fill.intensity = 0.8f;
                fill.shadows = LightShadows.None;
                fill.transform.rotation = new Quaternion(0.4660289f, -0.24107787f, 0.13363165f, 0.8407384f);

                Camera playerView = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                {
                    camera.clearFlags = CameraClearFlags.Skybox;
                    if (camera.name == "PlayerView") playerView = camera;
                }

                if (buildClouds)
                {
                    GameObject cloudRoot = FindCloudRoot(scene);
                    if (cloudRoot == null)
                    {
                        VolumetricCloudscapeBuilder.Build();
                        cloudRoot = FindCloudRoot(scene);
                    }
                    if (cloudRoot != null && cloudRoot.scene == scene)
                    {
                        CloudBankFollower follower = cloudRoot.GetComponent<CloudBankFollower>();
                        if (follower != null) follower.Configure(500f, 180f);
                        Vector3 viewPosition = playerView != null ? playerView.transform.position : anchor ?? Vector3.zero;
                        Vector3 viewForward = playerView != null
                            ? Vector3.ProjectOnPlane(playerView.transform.forward, Vector3.up).normalized
                            : Vector3.forward;
                        cloudRoot.transform.position = viewPosition + viewForward * 500f + Vector3.up * 180f;
                        cloudRoot.transform.rotation = Quaternion.LookRotation(viewForward, Vector3.up);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("GOAT_LIFEDAY_SKY_APPLIED scene=" + scene.path + " clouds=" + buildClouds);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        private static Light FindSun(Scene scene)
        {
            Light fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type != LightType.Directional || light.name == "LifeDay Cold Directional Fill")
                    continue;
                if (light.name == "LifeDay Summit Sun Rays")
                    return light;
                fallback ??= light;
            }
            return fallback;
        }

        private static Light FindNamedLight(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root.GetComponent<Light>();
            return null;
        }

        private static GameObject FindCloudRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == CloudRootName)
                    return root;
            return null;
        }
    }
}
