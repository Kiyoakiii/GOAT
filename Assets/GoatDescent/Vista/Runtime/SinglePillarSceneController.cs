using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class SinglePillarSceneController : MonoBehaviour
    {
        public const string RootName = "Single pillar playground";
        public const string PlatformName = "Summit platform";

        [Header("Pillar")]
        [SerializeField] private float height = 335f;
        [SerializeField] private float rx = 62f;
        [SerializeField] private float rz = 47f;
        [SerializeField] private int seed = 33;
        [SerializeField, Range(0, 3)] private int detail = 0;
        [SerializeField, Range(0, 2)] private float terraceBoost = 1f;

        [Header("Platform")]
        [SerializeField] private float platformRadius = 14f;
        [SerializeField] private float platformThickness = 14f;

        [Header("Play Mode")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private bool spawnGoat = true;

        [SerializeField] private PillarPreset preset;

        private readonly List<Object> runtimeAssets = new List<Object>();
        private Transform spawnMarker;

        public bool GenerateOnStart => generateOnStart;
        public Transform SpawnMarker => spawnMarker;
        public float PlatformTopLocalY => height + 9f;
        public float Height => height;
        public PillarPreset Preset => preset;

        public void SetGenerateOnStart(bool value) => generateOnStart = value;

        public void ApplyPreset(PillarPreset value)
        {
            preset = value;
            height = value.height;
            rx = value.rx;
            rz = value.rz;
            seed = value.seed;
            detail = value.detail;
            terraceBoost = value.terraceBoost;
            platformRadius = value.platformRadius;
            platformThickness = value.platformThickness;
            generateOnStart = value.generateOnStart;
            spawnGoat = value.spawnGoat;
        }

        public void ConfigureDefaultLayout()
        {
            height = 335f;
            rx = 62f;
            rz = 47f;
            seed = 33;
            detail = 0;
            terraceBoost = 1f;
            platformRadius = 14f;
            platformThickness = 14f;
            generateOnStart = false;
            spawnGoat = true;
        }

        private void Start()
        {
            if (generateOnStart) Generate();
            if (spawnGoat) SpawnGoat();
        }

        public void Generate()
        {
            DestroyGeneratedRoot();
            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);

            Material rock = MakeRockMaterial();
            Material foliage = MakeFoliageMaterial();
            Material bark = MakePlainMaterial(new Color(.25f, .18f, .12f));
            runtimeAssets.Add(rock); runtimeAssets.Add(foliage); runtimeAssets.Add(bark);

            var sink = new RuntimeMeshSink(runtimeAssets);
            MistyPillarsVistaBuilder.CreateTower(root.transform,
                new MistyPillarsVistaBuilder.TowerSpec(Vector3.zero, height, rx, rz, seed, detail, terraceBoost, true),
                rock, foliage, bark, sink, false, Vector2.zero, platformRadius + 14f);

            var platformObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platformObject.name = PlatformName;
            platformObject.transform.SetParent(root.transform, false);
            float top = PlatformTopLocalY;
            platformObject.transform.localPosition = new Vector3(0f, top - platformThickness * .5f, 0f);
            platformObject.transform.localScale = new Vector3(platformRadius * 2f, platformThickness * .5f, platformRadius * 2f);
            Object.DestroyImmediate(platformObject.GetComponent<Collider>());
            var platformFilter = platformObject.GetComponent<MeshFilter>();
            var platformMesh = platformFilter.mesh;
            var platformColors = new Color[platformMesh.vertexCount];
            for (int i = 0; i < platformColors.Length; i++) platformColors[i] = new Color(1f, .45f, 0f, 0f);
            platformMesh.colors = platformColors;
            var collider = platformObject.AddComponent<MeshCollider>();
            collider.sharedMesh = platformMesh;
            platformObject.GetComponent<MeshRenderer>().sharedMaterial = rock;
            platformObject.AddComponent<GoatDescent.MountainSlopeSurface>();
            Physics.SyncTransforms();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "Valley floor";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -.5f, 0f);
            ground.transform.localScale = new Vector3(1600f, .5f, 1600f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            var groundFilter = ground.GetComponent<MeshFilter>();
            var groundMesh = groundFilter.mesh;
            var groundColors = new Color[groundMesh.vertexCount];
            for (int i = 0; i < groundColors.Length; i++) groundColors[i] = new Color(1f, .7f, 0f, 0f);
            groundMesh.colors = groundColors;
            var groundCollider = ground.AddComponent<MeshCollider>();
            groundCollider.sharedMesh = groundMesh;
            ground.GetComponent<MeshRenderer>().sharedMaterial = rock;

            spawnMarker = new GameObject(MistyPillarsVistaBuilder.SpawnMarkerName).transform;
            spawnMarker.SetParent(root.transform, false);
            spawnMarker.localPosition = new Vector3(0f, top + .12f, 0f);
            spawnMarker.rotation = Quaternion.LookRotation(root.transform.TransformDirection(Vector3.forward), Vector3.up);
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        public void ClearGenerated()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            var goat = FindFirstObjectByType<GoatDescent.GoatController>(FindObjectsInactive.Include);
            if (goat) DestroyImmediate(goat.gameObject);
            DestroyRuntimeAssets();
            spawnMarker = null;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private void DestroyGeneratedRoot()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).name == RootName)
                    DestroyImmediate(transform.GetChild(i).gameObject);
            DestroyRuntimeAssets();
            spawnMarker = null;
        }

        private void DestroyRuntimeAssets()
        {
            foreach (Object asset in runtimeAssets) if (asset) DestroyImmediate(asset);
            runtimeAssets.Clear();
        }

        public void SpawnGoat()
        {
            Transform marker = ResolveSpawnMarker();
            Vector3 position = marker ? marker.position : transform.position;
            float yaw = marker ? marker.rotation.eulerAngles.y : 0f;
            GoatDescent.GoatPlayerFactory.Create(position, yaw, true);
        }

        private Transform ResolveSpawnMarker()
        {
            if (spawnMarker) return spawnMarker;
            GameObject found = GameObject.Find(MistyPillarsVistaBuilder.SpawnMarkerName);
            if (found) spawnMarker = found.transform;
            return spawnMarker;
        }

        private static Material MakePlainMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            var material = new Material(shader ? shader : Shader.Find("Legacy Shaders/Diffuse"));
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .08f);
            return material;
        }

        private static Material MakeRockMaterial()
        {
            Shader shader = Shader.Find("GoatDescent/Mountain");
            var material = new Material(shader ? shader : Shader.Find("Standard"));
            if (material.shader.name != "GoatDescent/Mountain") return material;
            material.SetTexture("_DirtTex", GoatDescent.MountainMaterial.Load("rock_color"));
            material.SetTexture("_GrassTex", GoatDescent.MountainMaterial.Load("grass_color"));
            material.SetTexture("_RockTex", GoatDescent.MountainMaterial.Load("rock_color"));
            material.SetFloat("_Tiling", .045f);
            material.SetFloat("_DetailTiling", .12f);
            material.SetFloat("_Steep", .53f);
            material.SetFloat("_Fade", .14f);
            material.SetFloat("_GrassEnd", 2f);
            material.SetFloat("_SnowStart", 2f);
            material.SetFloat("_GrassBlend", .82f);
            material.SetFloat("_HeightMin", 0f);
            material.SetFloat("_HeightMax", 400f);
            material.SetFloat("_BumpScale", .6f);
            material.SetFloat("_HighlightTrack", 0f);
            material.SetColor("_AtmoColor", RenderSettings.fogColor);
            material.SetFloat("_AtmoDensity", .0006f);
            return material;
        }

        private static Material MakeFoliageMaterial()
        {
            Shader shader = Shader.Find("GoatDescent/Valley Foliage");
            var material = new Material(shader ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.24f, .34f, .22f));
            if (material.HasProperty("_LeafDark")) material.SetColor("_LeafDark", new Color(.14f, .26f, .12f));
            if (material.HasProperty("_LeafLight")) material.SetColor("_LeafLight", new Color(.55f, .78f, .30f));
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", new Color(1f, .9f, .72f));
            if (material.HasProperty("_ShadowTint")) material.SetColor("_ShadowTint", new Color(.30f, .38f, .28f));
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .06f);
            if (material.HasProperty("_RampSteps")) material.SetFloat("_RampSteps", 3f);
            if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength", .4f);
            return material;
        }
    }
}
