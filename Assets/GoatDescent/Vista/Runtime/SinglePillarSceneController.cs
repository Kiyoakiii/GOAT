using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class SinglePillarSceneController : MonoBehaviour
    {
        public const string RootName = "Single pillar playground";
        public const string PlatformName = "Summit platform";
        private const int FloorSectors = 72;

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
            Material bark = MakeBarkMaterial();
            runtimeAssets.Add(rock); runtimeAssets.Add(foliage); runtimeAssets.Add(bark);

            var sink = new RuntimeMeshSink(runtimeAssets);
            MistyPillarsVistaBuilder.CreateTower(root.transform,
                new MistyPillarsVistaBuilder.TowerSpec(Vector3.zero, height, rx, rz, seed, detail, terraceBoost, true),
                rock, foliage, bark, sink, false, Vector2.zero, platformRadius + 6f);

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

            var ground = new GameObject("Valley floor");
            ground.transform.SetParent(root.transform, false);
            const float floorRadius = 800f;
            const int floorRings = 18;
            var floorVertices = new List<Vector3>(1 + (floorRings + 1) * (FloorSectors + 1)) { new Vector3(0f, 0f, 0f) };
            var floorColors = new List<Color>(floorVertices.Capacity) { FloorTint(0f, 0f) };
            for (int r = 1; r <= floorRings; r++)
            {
                float ringRadius = floorRadius * r / (float)floorRings;
                for (int s = 0; s <= FloorSectors; s++)
                {
                    float a = s / (float)FloorSectors * Mathf.PI * 2f;
                    var point = new Vector3(Mathf.Cos(a) * ringRadius, 0f, Mathf.Sin(a) * ringRadius);
                    floorVertices.Add(point);
                    floorColors.Add(FloorTint(point.x, point.z));
                }
            }
            var floorTriangles = new List<int>(floorRings * FloorSectors * 6);
            for (int s = 0; s < FloorSectors; s++)
                floorTriangles.AddRange(new[] { 0, FloorRingIndex(s + 1, 1), FloorRingIndex(s, 1) });
            for (int r = 1; r < floorRings; r++)
            for (int s = 0; s < FloorSectors; s++)
            {
                int inner = FloorRingIndex(s, r), innerNext = FloorRingIndex(s + 1, r);
                int outer = FloorRingIndex(s, r + 1), outerNext = FloorRingIndex(s + 1, r + 1);
                floorTriangles.AddRange(new[] { inner, innerNext, outerNext, inner, outerNext, outer });
            }
            var groundMesh = new Mesh
            {
                vertices = floorVertices.ToArray(),
                colors = floorColors.ToArray(),
                triangles = floorTriangles.ToArray()
            };
            groundMesh.RecalculateNormals();
            groundMesh.RecalculateBounds();
            ground.AddComponent<MeshFilter>().sharedMesh = groundMesh;
            var groundCollider = ground.AddComponent<MeshCollider>();
            groundCollider.sharedMesh = groundMesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = rock;

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

        private static int FloorRingIndex(int sector, int ring) => 1 + (ring - 1) * (FloorSectors + 1) + sector;

        private static Color FloorTint(float x, float z)
        {
            float broad = Mathf.PerlinNoise(x * .006f + 3.7f, z * .006f + 9.1f);
            float fine = Mathf.PerlinNoise(x * .028f + 17.3f, z * .028f + 4.4f);
            float grass = Mathf.Lerp(.42f, .95f, broad * .75f + fine * .25f);
            return new Color(1f, grass, 0f, 0f);
        }

        private static Material MakeRockMaterial()
        {
            Shader shader = Shader.Find("GoatDescent/Mountain");
            var material = new Material(shader ? shader : Shader.Find("Standard"));
            if (material.shader.name != "GoatDescent/Mountain") return material;
            material.SetTexture("_DirtTex", GoatDescent.MountainMaterial.Load("rock_color"));
            material.SetTexture("_GrassTex", GoatDescent.MountainMaterial.Load("grass_color"));
            material.SetTexture("_RockTex", GoatDescent.MountainMaterial.Load("rock_color"));
            material.SetFloat("_Tiling", .08f);
            material.SetFloat("_DetailTiling", .35f);
            material.SetFloat("_BumpScale", 1.35f);
            material.SetFloat("_Steep", .53f);
            material.SetFloat("_Fade", .14f);
            material.SetFloat("_GrassEnd", 2f);
            material.SetFloat("_SnowStart", 2f);
            material.SetFloat("_GrassBlend", .72f);
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
            if (material.HasProperty("_SwayAmount")) material.SetFloat("_SwayAmount", .16f);
            return material;
        }

        private static Material MakeBarkMaterial()
        {
            Shader shader = Shader.Find("GoatDescent/Valley Foliage");
            var material = new Material(shader ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.2f, .14f, .1f));
            if (material.HasProperty("_LeafDark")) material.SetColor("_LeafDark", new Color(.1f, .075f, .055f));
            if (material.HasProperty("_LeafLight")) material.SetColor("_LeafLight", new Color(.32f, .22f, .13f));
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", new Color(1f, .85f, .6f));
            if (material.HasProperty("_RampSteps")) material.SetFloat("_RampSteps", 2f);
            if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength", .12f);
            if (material.HasProperty("_SwayAmount")) material.SetFloat("_SwayAmount", 0f);
            return material;
        }
    }
}
