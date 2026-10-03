using UnityEngine;

namespace GoatDescent
{
    public sealed class MountainSceneBuilder : MonoBehaviour
    {
        public MountainSettings settings = new MountainSettings();

        private void Start()
        {
            if (FindFirstObjectByType<GoatController>() != null) return;
            BuildWorld();
        }

        public void BuildWorld()
        {
            ClearBuilt();
            SetupEnvironment(settings);

            var root = new GameObject("Procedural Mountain");
            root.transform.SetParent(transform, false);
            BuildSurface(root.transform);
            MountainDecor.Generate(root.transform, settings);
            MountainRouteProps.Generate(root.transform, settings);

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, .96f, .86f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .85f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = Camera.main ? Camera.main.gameObject : null;
            if (!cameraObject)
            {
                cameraObject = new GameObject("Goat Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
            }

            Vector3 spawn = MountainGenerator.SpawnPoint(settings);
            GoatPlayerFactory.Create(spawn, 0f, settings.CheckpointRespawn);
        }

        private static void SetupEnvironment(MountainSettings settings)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.25f / Mathf.Max(1f, settings.Radius);
            RenderSettings.fogColor = new Color(.78f, .85f, .91f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.7f, .73f, .76f);

            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.22f, .42f, .72f));
            sky.SetColor("_SkyHorizon", new Color(.75f, .88f, .95f));
            sky.SetColor("_GroundColor", new Color(.5f, .56f, .6f));
            sky.SetColor("_SunColor", new Color(1f, .94f, .78f));
            sky.SetVector("_SunDir", new Vector4(.5f, .72f, -.35f, 0f));
            RenderSettings.skybox = sky;
        }

        public void RebuildTerrain()
        {
            SetupEnvironment(settings);
            var root = transform.Find("Procedural Mountain");
            if (!root)
            {
                root = new GameObject("Procedural Mountain").transform;
                root.SetParent(transform, false);
            }
            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
            BuildSurface(root);
            MountainDecor.Generate(root, settings);
            MountainRouteProps.Generate(root, settings);
        }

        private void BuildSurface(Transform root)
        {
            var surface = new GameObject("Mountain Surface");
            surface.transform.SetParent(root, false);
            var filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = MountainGenerator.Build(settings);
            var renderer = surface.AddComponent<MeshRenderer>();
            var material = MountainMaterial.Get();
            MountainMaterial.Configure(material, settings);
            renderer.sharedMaterial = material;
            var collider = surface.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            surface.AddComponent<MountainSlopeSurface>();
        }

        public void ClearBuilt()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            var sun = FindFirstObjectByType<Light>();
            if (sun) DestroyImmediate(sun.gameObject);
        }
    }
}
