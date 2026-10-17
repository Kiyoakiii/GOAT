using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class FrozenRiftAuthoring
    {
        private const string Folder = "Assets/GoatDescent/Level/Static/FrozenRift";
        private const string Scene = "Assets/GoatDescent/Scenes/FrozenRift.unity";

        [MenuItem("Tools/GOAT/Статичные горы/Ледяной разлом — открыть")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Ледяной разлом — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Ледяной разлом — статичная гора").transform;
            var cliffs = new GameObject("Скальные массивы и снежные полки").transform; cliffs.SetParent(root);
            var iceRoot = new GameObject("Ледяные образования в разломах").transform; iceRoot.SetParent(root);
            var forest = new GameObject("Заснеженные ели").transform; forest.SetParent(root);
            var rock = new Material(Shader.Find("GoatDescent/FrozenRiftRock"));
            rock.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            rock.SetColor("_RockTint", new Color(.88f, .9f, .95f)); 
             rock.SetFloat("_Tiling", .055f);
            rock = Save(rock, "ColdRock.mat");
            var snow = Material("Snow", new Color(.67f, .77f, .89f), .16f);
            var pine = Material("FirNeedles", new Color(.075f, .15f, .17f), .05f);
            var wood = Material("FirTrunk", new Color(.19f, .16f, .14f), .05f);
            var ice = Save(new Material(Shader.Find("GoatDescent/FrozenRiftIce")), "GlacierIce.mat");
            var crystal = Save(FrozenRiftMesh.Crystal(), "GlacierCrystal.asset");
            var needles = Save(FrozenRiftMesh.Pine(false), "Fir.asset");
            var pineSnow = Save(FrozenRiftMesh.Pine(true), "FirSnow.asset");
            var trunk = Save(Object.Instantiate(DecorMeshLibrary.TrunkMesh()), "FirTrunk.asset");
            var stone = Save(DesertArchesMesh.Fragment(), "RockFragment.asset");
            for (int bank = 0; bank < 2; bank++)
            {
                int side = bank == 0 ? -1 : 1;
                for (int tier = 0; tier < 4; tier++)
                {
                    Place(cliffs, "Скальная стена " + bank + "-" + tier,
                        Save(FrozenRiftMesh.Shelf(side, tier, false), "Cliff-" + bank + "-" + tier + ".asset"), rock, Vector3.zero, Vector3.one, true);
                    Place(cliffs, "Снежный ярус " + bank + "-" + tier,
                        Save(FrozenRiftMesh.Shelf(side, tier, true), "Snow-" + bank + "-" + tier + ".asset"), snow, Vector3.zero, Vector3.one);
                    for (int tree = 0; tree < 12; tree++)
                    {
                        float z = -195f + (tree / 3) * 143f + (tree % 3) * 13f + Mathf.Sin(tree * 1.7f + tier) * 11f;
                        float x = FrozenRiftMesh.Channel(z) + side * (62f + (3 - tier) * 57f + 18f + Mathf.Sin(tree * 2.1f + tier) * 12f);
                        float y = FrozenRiftMesh.ShelfHeight(tier, z) + .6f;
                        float size = 15f + (tree + tier * 3) % 5 * 3f;
                        var group = new GameObject("Заснеженная ель").transform; group.SetParent(forest); group.position = new Vector3(x, y, z);
                        Place(group, "Хвоя", needles, pine, Vector3.zero, Vector3.one * size);
                        Place(group, "Снег на ветвях", pineSnow, snow, Vector3.zero, Vector3.one * size);
                        Place(group, "Ствол", trunk, wood, Vector3.zero, new Vector3(.6f, size * .7f, .6f));
                    }
                    for (int chip = 0; chip < 15; chip++)
                    {
                        float z = -220f + chip * 36f;
                        float x = FrozenRiftMesh.Channel(z) + side * (70f + (3 - tier) * 57f);
                        float y = FrozenRiftMesh.ShelfHeight(tier, z);
                        Place(cliffs, "Скальный скол", stone, rock, new Vector3(x, y, z), new Vector3(2f + chip % 3, 3f, 2f));
                        Place(cliffs, "Снег на сколе", stone, snow, new Vector3(x, y + .4f, z), new Vector3(2f + chip % 3, .6f, 2f));
                    }
                }
            }
            for (int group = 0; group < 8; group++)
            {
                float z = -200f + group * 70f;
                int side = group % 2 == 0 ? -1 : 1;
                for (int shard = 0; shard < 4; shard++)
                {
                    float x = FrozenRiftMesh.Channel(z) + side * (28f + shard * 7f);
                    float baseY = -90f + shard * 8f;
                    float height = 85f + (group * 13 + shard * 27) % 100;
                    var item = Place(iceRoot, "Ледяной пик", crystal, ice, new Vector3(x, baseY, z + shard * 9f),
                        new Vector3(12f + shard * 3f, height, 11f + shard * 2f), true);
                    item.localRotation = Quaternion.Euler(side * 4f, group * 31f + shard * 17f, side * -9f);
                }
            }
            for (int bank = 0; bank < 2; bank++)
            {
                float x = bank == 0 ? -260f : 260f;
                Place(cliffs, "Основание горного гребня", Save(DesertArchesMesh.Mesa(115f, 92f, 260f, -115f, bank + 31),
                    "RidgeBase-" + bank + ".asset"), rock, new Vector3(x, 0f, 295f), Vector3.one, true);
            }
            for (int peak = 0; peak < 6; peak++)
            {
                float x = peak < 3 ? -270f - peak * 25f : 250f + (peak - 3) * 27f;
                float z = 260f + peak % 3 * 27f;
                float height = 200f + peak % 3 * 35f;
                Place(cliffs, "Задний скальный гребень", crystal, rock, new Vector3(x, 245f, z),
                    new Vector3(55f, height, 58f), true);
            }
            Place(cliffs, "Дно ледяного ущелья", Save(DesertArchesMesh.Ground(), "Ground.asset"), snow, Vector3.down * 35f, Vector3.one);
            var route = new GameObject("Ориентиры маршрута — игровая настройка отложена").transform; route.SetParent(root);
            for (int point = 0; point < 12; point++)
            {
                float z = 295f - point * 43f;
                var marker = new GameObject(point == 0 ? "Старт" : point == 11 ? "Предполагаемый финиш" : "Полка " + point).transform;
                marker.SetParent(route); marker.position = new Vector3(FrozenRiftMesh.Channel(z) - 85f, FrozenRiftMesh.ShelfHeight(3, z), z);
            }
            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 58f; camera.farClipPlane = 2400f;
            camera.transform.position = new Vector3(250f, 280f, -520f); camera.transform.LookAt(new Vector3(0f, 150f, 100f));
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene);
            Render(camera, "FrozenRift-overview");
            camera.transform.position = new Vector3(0f, 230f, -510f); camera.transform.LookAt(new Vector3(0f, 175f, 100f)); Render(camera, "FrozenRift-canyon");
            camera.transform.position = new Vector3(-94f, 102f, -151f); camera.transform.LookAt(new Vector3(35f, 45f, 35f)); Render(camera, "FrozenRift-terrace");
            camera.transform.position = new Vector3(-430f, 320f, 560f); camera.transform.LookAt(new Vector3(0f, 135f, 60f)); Render(camera, "FrozenRift-back");
            Debug.Log("FROZEN_RIFT_STATIC_SAVED scene=" + Scene);
        }

        private static Transform Place(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, bool collider = false)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false); item.transform.localPosition = position;
            item.transform.localScale = scale; item.AddComponent<MeshFilter>().sharedMesh = mesh; item.AddComponent<MeshRenderer>().sharedMaterial = material;
            item.isStatic = true; if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item.transform;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", smoothness); return Save(material, name + ".mat");
        }

        private static T Save<T>(T asset, string name) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(Folder + "/" + name);
            if (!existing) { AssetDatabase.CreateAsset(asset, Folder + "/" + name); return asset; }
            if (asset is Mesh source && existing is Mesh destination)
            {
                destination.Clear(); destination.indexFormat = source.indexFormat; destination.vertices = source.vertices;
                destination.triangles = source.triangles; destination.normals = source.normals; destination.colors = source.colors;
                destination.uv = source.uv; destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); return existing;
        }

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.46f, .61f, .8f); RenderSettings.ambientEquatorColor = new Color(.34f, .43f, .57f);
            RenderSettings.ambientGroundColor = new Color(.18f, .26f, .38f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.68f, .83f, .94f); RenderSettings.fogDensity = .00065f;
            var sky = new Material(Shader.Find("GoatDescent/Sky")); sky.SetColor("_SkyZenith", new Color(.12f, .39f, .72f));
            sky.SetColor("_SkyHorizon", new Color(.75f, .86f, .96f)); sky.SetColor("_GroundColor", new Color(.75f, .86f, .96f));
            RenderSettings.skybox = Save(sky, "WinterSky.mat");
            var sun = new GameObject("Зимнее солнце").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(.92f, .96f, 1f); sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f); RenderSettings.sun = sun;
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name + ".png", texture.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(texture); Object.DestroyImmediate(target); }
        }
    }
}
