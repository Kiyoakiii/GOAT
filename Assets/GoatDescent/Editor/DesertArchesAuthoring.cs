using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class DesertArchesAuthoring
    {
        private const string AssetsPath = "Assets/GoatDescent/Level/Static/DesertArches";
        private const string ScenePath = "Assets/GoatDescent/Scenes/DesertArches.unity";
        private static Transform rocks, plants;
        private static Material sandstone, lightRock, sand, wood, scrub;
        private static Mesh stem, leaves, shard;
        private static int objectIndex;

        [MenuItem("Tools/GOAT/Статичные горы/Пустынные арки — открыть")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Debug.LogWarning("Сначала создайте статичную сцену Пустынных арок."); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Пустынные арки — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AssetsPath); AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            objectIndex = 0;
            sandstone = RockMaterial("Sandstone", new Color(1.48f, .79f, .42f));
            lightRock = RockMaterial("SunlitSandstone", new Color(1.62f, .99f, .57f));
            sand = RockMaterial("Sand", new Color(1.55f, 1.2f, .72f));
            wood = Save(new Material(Shader.Find("Standard")) { color = new Color(.28f, .19f, .1f) }, "DryWood.mat");
            scrub = Save(new Material(Shader.Find("Standard")) { color = new Color(.45f, .36f, .14f) }, "DryScrub.mat");
            stem = Save(Object.Instantiate(DecorMeshLibrary.TrunkMesh()), "DryStem.asset");
            leaves = Save(Object.Instantiate(DecorMeshLibrary.OctahedronMesh()), "Scrub.asset");
            shard = Save(DesertArchesMesh.Fragment(), "SandstoneShard.asset");
            var root = new GameObject("Пустынные арки — статичная композиция");
            rocks = new GameObject("Скалы и естественные террасы").transform; rocks.SetParent(root.transform);
            plants = new GameObject("Сухая растительность").transform; plants.SetParent(root.transform);
            Emit("Дно каньона", DesertArchesMesh.Ground(), Vector3.zero, sand);
            Emit("Главная природная арка", DesertArchesMesh.Arch(1f), new Vector3(-45f, 0f, 30f), sandstone);
            Emit("Вторая арка в глубине", DesertArchesMesh.Arch(.78f), new Vector3(165f, 0f, 130f), sandstone);
            Mesa("Верхний левый массив", -145f, 43f, 43f, 49f, 211f);
            Mesa("Гребень над главной аркой", -112f, 66f, 29f, 37f, 229f);
            Mesa("Левый карниз у старта", -171f, -6f, 35f, 31f, 185f);
            Mesa("Верхняя сухая терраса", -176f, -43f, 33f, 28f, 161f);
            Mesa("Скальная полка западной стены", -151f, -76f, 38f, 30f, 137f);
            Mesa("Широкая терраса над каньоном", -125f, -104f, 43f, 36f, 111f);
            Mesa("Полка с сухим деревом", -88f, -130f, 41f, 35f, 85f);
            Mesa("Нижняя терраса", -39f, -143f, 39f, 32f, 59f);
            Mesa("Край каньона", 9f, -139f, 33f, 30f, 35f);
            Mesa("Левый скальный фундамент", -204f, 15f, 53f, 66f, 82f);
            Mesa("Основание западной стены", -178f, -105f, 55f, 47f, 48f);
            Mesa("Правое основание главной арки", 38f, 38f, 29f, 40f, 59f);
            Mesa("Правый выступ главной арки", 59f, 70f, 21f, 29f, 104f);
            Mesa("Скальный шпиль", 53f, 97f, 13f, 20f, 202f);
            Mesa("Основание второго массива", 248f, 146f, 44f, 52f, 52f);
            Mesa("Правая вершина второй арки", 240f, 158f, 26f, 34f, 191f);
            Mesa("Верхняя восточная терраса", 269f, 105f, 30f, 32f, 143f);
            Mesa("Восточная средняя терраса", 273f, 56f, 36f, 34f, 103f);
            Mesa("Восточная нижняя терраса", 257f, 5f, 38f, 37f, 62f);
            Mesa("Скала под дальней аркой", 104f, 142f, 22f, 29f, 45f);
            Mesa("Дальний столб каньона", 60f, 270f, 21f, 25f, 109f);
            Mesa("Дальняя скала", -46f, 335f, 38f, 30f, 123f);
            Mesa("Дальний шпиль", 151f, 333f, 14f, 19f, 93f);
            Mesa("Передняя полка", -160f, -203f, 60f, 39f, 25f);
            Mesa("Передняя скала", 75f, -202f, 37f, 43f, 15f);
            DryTree(new Vector3(-93f, 85f, -132f), 8f);
            DryTree(new Vector3(-174f, 161f, -39f), 6f);
            DryTree(new Vector3(270f, 103f, 60f), 6.5f);
            RouteGuide(root.transform);
            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 48f; camera.farClipPlane = 2000f;
            camera.transform.position = new Vector3(310f, 195f, -435f);
            camera.transform.LookAt(new Vector3(0f, 98f, 40f));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Render(camera, "DesertArches-overview");
            camera.transform.position = new Vector3(-245f, 178f, -305f); camera.transform.LookAt(new Vector3(-35f, 108f, 43f));
            Render(camera, "DesertArches-side");
            camera.transform.position = new Vector3(-45f, 62f, -190f); camera.transform.LookAt(new Vector3(-40f, 117f, 45f));
            Render(camera, "DesertArches-opening");
            camera.transform.position = new Vector3(45f, 130f, 440f); camera.transform.LookAt(new Vector3(-40f, 104f, 20f));
            Render(camera, "DesertArches-back");
            camera.transform.position = new Vector3(-106f, 115f, -131f); camera.transform.LookAt(new Vector3(-28f, 77f, -141f));
            Render(camera, "DesertArches-terrace");
            Debug.Log("DESERT_ARCHES_STATIC_SAVED scene=" + ScenePath);
        }

        private static void Mesa(string name, float x, float z, float width, float depth, float top)
        {
            int detail = objectIndex + 11;
            Emit(name, DesertArchesMesh.Mesa(width, depth, top, -105f, detail), new Vector3(x, 0f, z),
                detail % 3 == 0 ? lightRock : sandstone);
            for (int edge = 0; edge < 9; edge++)
            {
                float angle = detail * .41f + edge * .17f;
                float shape = 1f + Mathf.Sin(angle * 28f / (2f * Mathf.PI) * .79f + detail) * .09f;
                var chip = Part(plants, "Разрушенный край пласта", shard, sandstone,
                    new Vector3(x + Mathf.Cos(angle) * width * shape * .98f, top - .6f,
                        z + Mathf.Sin(angle) * depth * shape * .98f),
                    new Vector3(2.2f + edge % 3, 2.5f + edge % 4, 1.8f + edge % 2));
                chip.localRotation = Quaternion.Euler(0f, edge * 63f, 0f);
            }
            for (int pebble = 0; pebble < 22; pebble++)
            {
                var stone = Part(plants, "Осколок песчаника", shard, lightRock,
                    new Vector3(x + Mathf.Sin(detail + pebble % 3 * 1.71f) * width * .68f + Mathf.Sin(pebble * 2.3f) * 4f, top + .3f,
                        z + Mathf.Cos(detail * .8f + pebble % 3 * 2.3f) * depth * .68f + Mathf.Cos(pebble * 1.7f) * 4f),
                    new Vector3(.6f + pebble % 4 * .6f, .4f + pebble % 3 * .35f, .7f + pebble % 3 * .5f));
                stone.localRotation = Quaternion.Euler(0f, pebble * 71f, 8f);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector3 position = new Vector3(x + Mathf.Sin(detail + i % 3 * 1.71f) * width * .68f + Mathf.Sin(i * 1.3f) * 5f,
                    top + .5f, z + Mathf.Cos(detail * .8f + i % 3 * 2.3f) * depth * .68f + Mathf.Cos(i * 1.7f) * 5f);
                for (int twig = 0; twig < 7; twig++)
                {
                    float angle = twig * 2.4f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * 1.6f, .7f + twig % 3 * .45f, Mathf.Sin(angle) * 1.6f);
                    Part(plants, "Ветка сухого куста", leaves, scrub, position + offset,
                        new Vector3(1.4f, .75f, 1.3f));
                    var branch = Part(plants, "Стебель куста", stem, wood, position,
                        new Vector3(.13f, 2.3f, .13f));
                    branch.localRotation = Quaternion.Euler(35f, twig * 137f, 25f);
                }
                for (int blade = 0; blade < 3; blade++)
                    Part(plants, "Сухая трава", stem, scrub, position + Vector3.right * (2f + blade * .4f),
                        new Vector3(.13f, 1.2f + blade * .2f, .13f));
            }
        }

        private static void DryTree(Vector3 position, float height)
        {
            var tree = new GameObject("Сухое дерево").transform; tree.SetParent(plants); tree.position = position;
            Part(tree, "Ствол", stem, wood, Vector3.zero, new Vector3(.8f, height, .8f));
            for (int i = 0; i < 5; i++)
            {
                var branch = Part(tree, "Изогнутая ветвь", stem, wood, Vector3.up * height * (.35f + i * .12f),
                    new Vector3(.35f, height * .45f, .35f));
                branch.localRotation = Quaternion.Euler(30f + i * 9f, i * 137f, 35f);
            }
        }

        private static Transform Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false); item.transform.localPosition = position;
            item.transform.localScale = scale; item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material; return item.transform;
        }

        private static void RouteGuide(Transform parent)
        {
            var guide = new GameObject("Маршрут — авторские ориентиры, игровая настройка отложена").transform;
            guide.SetParent(parent, false);
            Vector3[] points = { new Vector3(-155f, 211f, 30f), new Vector3(-171f, 185f, -6f),
                new Vector3(-176f, 161f, -43f), new Vector3(-151f, 137f, -76f), new Vector3(-125f, 111f, -104f),
                new Vector3(-88f, 85f, -130f), new Vector3(-39f, 59f, -143f), new Vector3(9f, 35f, -139f) };
            var archGuide = new GameObject("Ветвь маршрута по главной арке — требует игровой настройки").transform;
            archGuide.SetParent(guide, false);
            for (int step = 0; step <= 12; step++)
            {
                float angle = Mathf.PI - step * Mathf.PI / 12f;
                var point = new GameObject("Свод " + step).transform; point.SetParent(archGuide, false);
                point.localPosition = new Vector3(-45f + Mathf.Cos(angle) * 88f,
                    63f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(angle)), .48f) * 137f, 30f);
            }
            for (int i = 0; i < points.Length; i++)
            {
                var point = new GameObject(i == 0 ? "Старт" : i == points.Length - 1 ? "Предполагаемый финиш" : "Терраса " + i);
                point.transform.SetParent(guide, false); point.transform.localPosition = points[i];
            }
        }

        private static void Emit(string name, Mesh mesh, Vector3 position, Material material)
        {
            mesh = Save(mesh, "Rock-" + (objectIndex++).ToString("D2") + ".asset");
            var item = Part(rocks, name, mesh, material, position, Vector3.one);
            item.gameObject.isStatic = true; item.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Material RockMaterial(string name, Color tint)
        {
            var material = new Material(Shader.Find("GoatDescent/DesertSandstone")); material.name = name;
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", tint);
            material.SetColor("_SandTint", new Color(1.45f, 1.08f, .67f));
            material.SetFloat("_Tiling", .035f);
            material.SetFloat("_Layers", name == "Sand" ? 0f : .27f);
            return Save(material, name + ".mat");
        }

        private static T Save<T>(T asset, string name) where T : Object
        {
            string path = AssetsPath + "/" + name;
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(asset, path); return asset; }
            if (asset is Mesh source && existing is Mesh destination)
            {
                destination.Clear(); destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices; destination.triangles = source.triangles;
                destination.normals = source.normals; destination.colors = source.colors;
                destination.uv = source.uv; destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); return existing;
        }

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.39f, .48f, .64f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .28f, .22f);
            RenderSettings.ambientGroundColor = new Color(.28f, .21f, .14f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.78f, .78f, .72f); RenderSettings.fogDensity = .0009f;
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.16f, .45f, .83f)); sky.SetColor("_SkyHorizon", new Color(.82f, .88f, .91f));
            sky.SetColor("_GroundColor", new Color(.82f, .88f, .91f)); RenderSettings.skybox = Save(sky, "DesertSky.mat");
            var sun = new GameObject("Пустынное солнце").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1f, .89f, .73f); sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -52f, 0f); RenderSettings.sun = sun;
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0); texture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }
    }
}
