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

            var root = new GameObject("Procedural Mountain");
            root.transform.SetParent(transform, false);
            BuildSurface(root.transform);
            MountainDecor.Generate(root.transform, settings);

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = Camera.main ? Camera.main.gameObject : null;
            if (!cameraObject)
            {
                cameraObject = new GameObject("Goat Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
            }

            Vector3 spawn = MountainGenerator.SpawnPoint(settings);
            GoatPlayerFactory.Create(spawn);
        }

        public void RebuildTerrain()
        {
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
        }

        private void BuildSurface(Transform root)
        {
            var surface = new GameObject("Mountain Surface");
            surface.transform.SetParent(root, false);
            var filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = MountainGenerator.Build(settings);
            var renderer = surface.AddComponent<MeshRenderer>();
            var material = MountainMaterial.Get();
            material.SetFloat("_HeightMin", settings.ValleyY - 5f);
            material.SetFloat("_HeightMax", settings.SummitY + 5f);
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