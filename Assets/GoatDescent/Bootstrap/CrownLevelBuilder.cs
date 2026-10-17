using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent
{
    public static class CrownLevelBuilder
    {
        public static LevelGeneratedResources Generate(MountainSceneBuilder owner, LevelRecipe recipe, bool players)
        {
            if (Application.isPlaying && owner.GetComponentInChildren<LevelGeneratedResources>())
                throw new System.InvalidOperationException("Перезапустите Play Mode для пересборки уровня.");
            LevelRoute route = LevelRouteGenerator.Generate(recipe);
            Clear(owner);
            var root = new GameObject(recipe.LevelName);
            if (!Application.isPlaying) root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            root.transform.SetParent(owner.transform, true);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var resources = root.AddComponent<LevelGeneratedResources>();
            resources.Configure(route);
            StoneCrownGeometry.Build(resources, recipe);
            foreach (var collider in resources.GetComponentsInChildren<MeshCollider>())
                collider.gameObject.AddComponent<MountainSlopeSurface>();
            Environment(resources, recipe);
            if (players) Spawn(owner, resources, recipe);
            Debug.Log("STONE_CROWN_BUILT seed=" + recipe.Seed + " supports=" + route.Platforms.Length
                + " targetSeconds=" + route.TargetSeconds + " signature=" + route.Signature);
            return resources;
        }

        public static void Clear(MountainSceneBuilder owner)
        {
            foreach (var resources in owner.GetComponentsInChildren<LevelGeneratedResources>())
            {
                if (Application.isPlaying) Object.Destroy(resources.gameObject);
                else Object.DestroyImmediate(resources.gameObject);
            }
        }

        private static void Environment(LevelGeneratedResources resources, LevelRecipe recipe)
        {
            resources.CaptureEnvironment();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = recipe.Biome ? recipe.Biome.FogDensity : .0012f;
            RenderSettings.fogColor = recipe.Biome ? recipe.Biome.FogColor : new Color(.72f, .82f, .88f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.65f, .73f, .83f);
            RenderSettings.ambientEquatorColor = new Color(.48f, .53f, .56f);
            RenderSettings.ambientGroundColor = new Color(.3f, .27f, .22f);
            var sky = resources.Own(new Material(Shader.Find("GoatDescent/Sky")));
            sky.SetColor("_SkyZenith", new Color(.24f, .48f, .8f));
            sky.SetColor("_SkyHorizon", new Color(.8f, .9f, .96f));
            sky.SetColor("_GroundColor", new Color(.8f, .9f, .96f));
            sky.SetColor("_SunColor", new Color(1f, .94f, .76f));
            sky.SetVector("_SunDir", new Vector4(.5f, .72f, -.35f, 0f));
            resources.GeneratedSky = sky;
            RenderSettings.skybox = sky;
            var sun = new GameObject("Солнце короны");
            sun.transform.SetParent(resources.transform, false);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional; light.color = new Color(1f, .86f, .65f);
            light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
        }

        private static void Spawn(MountainSceneBuilder owner, LevelGeneratedResources resources, LevelRecipe recipe)
        {
            LevelRoute route = resources.Route;
            Material material = StoneCrownGeometry.RouteMaterial(resources);
            Vector3 forward = Vector3.ProjectOnPlane(route.Platforms[1].Center - route.Platforms[0].Center, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            var first = GoatPlayerFactory.Create(route.Spawn - side * .85f, yaw, true, true, route.Platforms, material);
            var second = GoatPlayerFactory.Create(route.Spawn + side * .85f, yaw, true, false, route.Platforms, material);
            first.transform.SetParent(resources.transform, true); second.transform.SetParent(resources.transform, true);
            first.name = "Mountain Goat A"; second.name = "Mountain Goat B";
            first.GetComponent<RespawnController>().SetKillPlane(recipe.ValleyHeight - 18f);
            second.GetComponent<RespawnController>().SetKillPlane(recipe.ValleyHeight - 18f);
            var camera = Camera.main;
            var pair = owner.GetComponent<LocalGoatPair>() ?? owner.gameObject.AddComponent<LocalGoatPair>();
            pair.Configure(first, second, camera.GetComponent<ThirdPersonGoatCamera>(), route.Spawn, yaw, null, route.Platforms);
            camera.farClipPlane = Mathf.Max(900f, recipe.Radius * 6f);
            camera.GetComponent<CinematicPostProcess>().Apply(1.08f, 1.12f, 1.05f, .2f, .15f);
            var run = resources.gameObject.AddComponent<LevelRunTracker>();
            run.Configure(recipe, first, second);
            var session = owner.GetComponent<MountainSessionState>() ?? owner.gameObject.AddComponent<MountainSessionState>();
            session.Initialize(route.Signature, route.Platforms);
            if (!owner.GetComponent<MountainNetSession>()) owner.gameObject.AddComponent<MountainNetSession>();
        }

    }
}
