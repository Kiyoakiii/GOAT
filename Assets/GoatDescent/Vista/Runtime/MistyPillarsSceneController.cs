using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public sealed class MistyPillarsSceneController : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 rootPosition = MistyPillarsVistaBuilder.RootPosition;
        [SerializeField] private Quaternion rootRotation = MistyPillarsVistaBuilder.RootRotation;
        [SerializeField, Range(7, 40)] private int towerCount = MistyPillarsVistaBuilder.DefaultTowerCount;
        [SerializeField] private int randomSeed = MistyPillarsVistaBuilder.DefaultRandomSeed;
        [SerializeField] private float heightMin = 260f;
        [SerializeField] private float heightMax = 415f;
        [SerializeField] private float radiusMin = 20f;
        [SerializeField] private float radiusMax = 44f;

        [Header("Play Mode")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool applyDefaultLighting = false;
        [SerializeField] private bool spawnGoat = true;
        [SerializeField] private bool createCameraIfMissing = true;

        private readonly List<Object> runtimeAssets = new List<Object>();
        private Transform spawnMarker;

        public bool GenerateOnStart => generateOnStart;
        public int TowerCount => towerCount;
        public Transform SpawnMarker => spawnMarker;

        public void SetGenerateOnStart(bool value) => generateOnStart = value;

        public void ConfigureDefaultLayout()
        {
            rootPosition = MistyPillarsVistaBuilder.RootPosition;
            rootRotation = MistyPillarsVistaBuilder.RootRotation;
            towerCount = MistyPillarsVistaBuilder.DefaultTowerCount;
            randomSeed = MistyPillarsVistaBuilder.DefaultRandomSeed;
            heightMin = 260f;
            heightMax = 415f;
            radiusMin = 20f;
            radiusMax = 44f;
            generateOnStart = true;
            applyDefaultLighting = false;
            spawnGoat = true;
            createCameraIfMissing = true;
        }

        private void Start()
        {
            if (applyDefaultLighting) SetupSkybox();
            if (generateOnStart) Generate();
            if (spawnGoat) SpawnGoat();
            if (createCameraIfMissing) EnsureCamera();
        }

        private static void SetupSkybox()
        {
            if (RenderSettings.skybox && RenderSettings.skybox.shader != null &&
                RenderSettings.skybox.shader.name == "GoatDescent/Sky")
            {
                RenderSettings.skybox.SetColor("_SkyZenith", new Color(.36f, .58f, .85f));
                RenderSettings.skybox.SetColor("_SkyHorizon", new Color(.82f, .9f, .97f));
                RenderSettings.skybox.SetColor("_SunColor", new Color(1f, .93f, .72f));
                RenderSettings.skybox.SetFloat("_SunSize", .09f);
                RenderSettings.skybox.SetColor("_CloudColor", new Color(1f, .98f, .94f));
                RenderSettings.skybox.SetFloat("_CloudDensity", .5f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .70f, .78f);
            RenderSettings.ambientEquatorColor = new Color(.44f, .48f, .52f);
            RenderSettings.ambientGroundColor = new Color(.30f, .27f, .24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .00012f;
            RenderSettings.fogColor = new Color(.72f, .81f, .87f);
        }

        public void Generate()
        {
            DestroyGeneratedRoot();
            var root = new GameObject(MistyPillarsVistaBuilder.RootName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = rootPosition;
            root.transform.localRotation = rootRotation;
            root.AddComponent<ValleyMistDepth>();

            Material rock = MakeMaterial("GoatDescent/Weathered Sandstone", new Color(.52f, .45f, .35f));
            Material foliage = MakeMaterial("GoatDescent/Valley Foliage", new Color(.19f, .27f, .19f));
            Material bark = MakeMaterial("Standard", new Color(.17f, .14f, .10f));
            runtimeAssets.Add(rock); runtimeAssets.Add(foliage); runtimeAssets.Add(bark);

            var sink = new RuntimeMeshSink(runtimeAssets);
            foreach (var spec in MistyPillarsVistaBuilder.BuildLayout(towerCount, randomSeed, heightMin, heightMax, radiusMin, radiusMax))
            {
                var marker = MistyPillarsVistaBuilder.CreateTower(root.transform, spec, rock, foliage, bark, sink, false);
                if (marker) spawnMarker = marker;
            }
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
                if (transform.GetChild(i).name == MistyPillarsVistaBuilder.RootName)
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

        private void EnsureCamera()
        {
            Camera main = Camera.main;
            if (main)
            {
                if (!main.GetComponent<AudioListener>()) main.gameObject.AddComponent<AudioListener>();
                return;
            }
            var obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            var camera = obj.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 2600f;
            obj.AddComponent<AudioListener>();
            obj.transform.position = new Vector3(215f, 315f, 1090f);
        }

        private static Material MakeMaterial(string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            var material = new Material(shader ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_StoneColor")) material.SetColor("_StoneColor", color);
            if (material.HasProperty("_ShadowStone")) material.SetColor("_ShadowStone", new Color(.17f, .205f, .20f));
            if (material.HasProperty("_MossColor")) material.SetColor("_MossColor", new Color(.11f, .20f, .073f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_LeafDark")) material.SetColor("_LeafDark", new Color(.18f, .30f, .13f));
            if (material.HasProperty("_LeafLight")) material.SetColor("_LeafLight", new Color(.55f, .78f, .30f));
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", new Color(1f, .9f, .72f));
            if (material.HasProperty("_ShadowTint")) material.SetColor("_ShadowTint", new Color(.62f, .47f, .32f));
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .06f);
            if (material.HasProperty("_RampSteps")) material.SetFloat("_RampSteps", 3f);
            if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength", .5f);
            if (material.HasProperty("_MossAmount")) material.SetFloat("_MossAmount", .55f);
            if (material.HasProperty("_StratumStrength")) material.SetFloat("_StratumStrength", .45f);
            return material;
        }
    }
}
