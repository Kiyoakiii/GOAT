using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent
{
    public sealed class MountainSceneBuilder : MonoBehaviour
    {
        public MountainSettings settings = new MountainSettings();
        [SerializeField] private LevelRecipe levelRecipe;
        public LevelRecipe LevelRecipe => levelRecipe;
        public void SetLevelRecipe(LevelRecipe value) => levelRecipe = value;

        private void Start()
        {
            if (FindFirstObjectByType<GoatController>() != null) return;
            BuildWorld();
        }

        public void BuildWorld()
        {
            if (levelRecipe)
            {
                CrownLevelBuilder.Generate(this, levelRecipe, true);
                return;
            }
            ClearBuilt();
            SetupEnvironment(settings);

            var root = new GameObject("Procedural Mountain");
            root.transform.SetParent(transform, false);
            BuildSurface(root.transform);
            MountainDecor.Generate(root.transform, settings);
            MountainRouteProps.Generate(root.transform, settings);

            var cameraObject = Camera.main ? Camera.main.gameObject : null;
            if (!cameraObject)
            {
                cameraObject = new GameObject("Goat Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
            }

            Vector3 spawn = MountainGenerator.SpawnPoint(settings);
            var route = MountainGenerator.CurrentRoute;
            Vector3 forward = route != null && route.Count > 1
                ? Vector3.ProjectOnPlane(route[1].Center - route[0].Center, Vector3.up).normalized
                : Vector3.forward;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            var first = GoatPlayerFactory.Create(spawn - side * .85f, yaw,
                settings.CheckpointRespawn);
            var second = GoatPlayerFactory.Create(spawn + side * .85f, yaw,
                settings.CheckpointRespawn, false);
            first.name = "Mountain Goat A";
            second.name = "Mountain Goat B";
            var follow = Camera.main.GetComponent<ThirdPersonGoatCamera>();
            gameObject.AddComponent<LocalGoatPair>().Configure(first, second, follow,
                spawn, yaw, null);
            ApplyPost(settings);
            if (route != null && route.Count > 0)
            {
                var hazardObject = new GameObject("Goat interaction hazards");
                hazardObject.transform.SetParent(root.transform, false);
                hazardObject.AddComponent<MountainHazardDirector>().Initialize(route,
                    MountainMaterial.Get(settings), root.transform.Find("Route Markers and Hazards"));
                Vector3 birdCenter = route[Mathf.Min(3, route.Count - 1)].Center;
                Vector3 birdOutward = Vector3.ProjectOnPlane(birdCenter, Vector3.up).normalized;
                var flock = new GameObject("Five mountain eagles");
                flock.transform.SetParent(root.transform, false);
                flock.AddComponent<SkyPredatorEpisode>().Initialize(birdCenter,
                    birdOutward, settings.SummitY);
            }
            gameObject.AddComponent<MountainSessionState>().Initialize();
            gameObject.AddComponent<MountainNetSession>();
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument == "--goat-verify")
                {
                    gameObject.AddComponent<MountainIntegrationProbe>();
                    break;
                }
        }

        private static void SetupEnvironment(MountainSettings settings)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.25f * Mathf.Max(0f, settings.FogStrength) / Mathf.Max(1f, settings.Radius);
            RenderSettings.fogColor = settings.FogColor;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = settings.AmbientColor;

            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", settings.SkyZenith);
            sky.SetColor("_SkyHorizon", settings.SkyHorizon);
            sky.SetColor("_GroundColor", new Color(.5f, .56f, .6f));
            sky.SetColor("_SunColor", settings.SunColor);
            sky.SetVector("_SunDir", new Vector4(.5f, .72f, -.35f, 0f));
            RenderSettings.skybox = sky;

            ApplySun(settings);
            ApplyPost(settings);
        }

        private static void ApplySun(MountainSettings settings)
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (!light || light.type != LightType.Directional)
            {
                var sunObject = new GameObject("Sun");
                light = sunObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .85f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            light.intensity = settings.SunIntensity;
            light.color = settings.SunColor;
        }

        private static void ApplyPost(MountainSettings settings)
        {
            var post = Object.FindFirstObjectByType<CinematicPostProcess>();
            if (post) post.Apply(settings.Exposure, settings.Saturation, settings.Contrast,
                settings.Warmth, settings.Vignette);
        }

        public void RebuildTerrain()
        {
            if (levelRecipe)
            {
                if (Application.isPlaying)
                {
                    Debug.LogWarning("Остановите Play Mode перед пересборкой маршрута уровня.", this);
                    return;
                }
                CrownLevelBuilder.Generate(this, levelRecipe, false);
                return;
            }
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
            var material = MountainMaterial.Get(settings);
            MountainMaterial.Configure(material, settings);
            renderer.sharedMaterial = material;
            var collider = surface.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            surface.AddComponent<MountainSlopeSurface>();
        }

        public void ClearBuilt()
        {
            if (levelRecipe)
            {
                CrownLevelBuilder.Clear(this);
                return;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            var sun = FindFirstObjectByType<Light>();
            if (sun) DestroyImmediate(sun.gameObject);
        }
    }
}
