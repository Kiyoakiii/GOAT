using System;
using System.IO;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class CrownLevelCI
    {
        private const string RecipePath = "Assets/GoatDescent/Level/Recipes/StoneCrown.asset";
        private const string BiomePath = "Assets/GoatDescent/Level/Recipes/MountainForest.asset";
        private const string ScenePath = "Assets/GoatDescent/Scenes/GoatPlayground.unity";

        [MenuItem("Tools/GOAT/Каменная корона/Подключить к игровой сцене")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play Mode перед изменением сцены.");
            Directory.CreateDirectory("Assets/GoatDescent/Level/Recipes"); AssetDatabase.Refresh();
            var biome = AssetDatabase.LoadAssetAtPath<BiomeProfile>(BiomePath);
            if (!biome) { biome = ScriptableObject.CreateInstance<BiomeProfile>(); AssetDatabase.CreateAsset(biome, BiomePath); }
            var recipe = AssetDatabase.LoadAssetAtPath<LevelRecipe>(RecipePath);
            if (!recipe) { recipe = ScriptableObject.CreateInstance<LevelRecipe>(); recipe.Configure(7, biome); AssetDatabase.CreateAsset(recipe, RecipePath); }
            EditorSceneManager.OpenScene(ScenePath);
            var builder = UnityEngine.Object.FindFirstObjectByType<MountainSceneBuilder>();
            Require(builder, "В игровой сцене отсутствует MountainSceneBuilder");
            builder.SetLevelRecipe(recipe);
            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            EditorSceneManager.SaveScene(builder.gameObject.scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Check();
            Debug.Log("STONE_CROWN_INSTALLED recipe=" + RecipePath);
        }

        [MenuItem("Tools/GOAT/Каменная корона/Проверить маршрут и геометрию")]
        public static void Check()
        {
            EnsureShortRecipe();
            var recipe = AssetDatabase.LoadAssetAtPath<LevelRecipe>(RecipePath);
            Require(recipe, "Сначала подключите рецепт Каменной короны");
            var host = new GameObject("Crown CI");
            var builder = host.AddComponent<MountainSceneBuilder>();
            builder.enabled = false;
            int seed = recipe.Seed;
            var savedRoute = MountainGenerator.CurrentRoute;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                foreach (int testSeed in new[] { 7, 21, 33, 28471, 847291 })
                {
                    var copy = UnityEngine.Object.Instantiate(recipe);
                    copy.Configure(testSeed, recipe.Biome);
                    try
                    {
                        LevelRoute first = LevelRouteGenerator.Generate(copy), second = LevelRouteGenerator.Generate(copy);
                        Require(first.Signature == second.Signature, "Маршрут недетерминирован: " + testSeed);
                        Require(first.Platforms.Length > 100, "Маршрут слишком короткий");
                        Require(first.TargetSeconds >= 300f && first.TargetSeconds <= 480f, "Целевой хронометраж вне 5–8 минут");
                        for (int i = 0; i < first.Platforms.Length; i++)
                            Require(first.Platforms[i].Center == second.Platforms[i].Center, "Опора изменилась: " + i);
                        var level = CrownLevelBuilder.Generate(builder, copy, false);
                        Physics.SyncTransforms();
                        CheckSurfaces(first);
                        if (testSeed == seed) Capture(first);
                        Debug.Log("STONE_CROWN_SEED_OK seed=" + testSeed + " points=" + first.Platforms.Length);
                        CrownLevelBuilder.Clear(builder);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(copy); }
                }
                var shortRecipe = AssetDatabase.LoadAssetAtPath<LevelRecipe>("Assets/GoatDescent/Level/Recipes/StoneCrownTrail.asset");
                var shortRoute = LevelRouteGenerator.Generate(shortRecipe);
                CrownLevelBuilder.Generate(builder, shortRecipe, false);
                Physics.SyncTransforms(); CheckSurfaces(shortRoute); Capture(shortRoute, "StoneCrown-short");
                CrownLevelBuilder.Clear(builder);
                Debug.Log("STONE_CROWN_SHORT_OK points=" + shortRoute.Platforms.Length);
                Debug.Log("STONE_CROWN_CHECK_OK ms=" + timer.ElapsedMilliseconds);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                MountainGenerator.UseRoute(savedRoute);
            }
        }

        private static void EnsureShortRecipe()
        {
            const string path = "Assets/GoatDescent/Level/Recipes/StoneCrownTrail.asset";
            if (AssetDatabase.LoadAssetAtPath<LevelRecipe>(path)) return;
            var value = ScriptableObject.CreateInstance<LevelRecipe>();
            value.Configure(7, AssetDatabase.LoadAssetAtPath<BiomeProfile>(BiomePath));
            JsonUtility.FromJsonOverwrite("{\"levelName\":\"Каменная корона — пробный участок\",\"radius\":100,\"summitHeight\":72,\"valleyHeight\":8}", value);
            var serialized = new SerializedObject(value);
            var episodes = serialized.FindProperty("episodes"); episodes.arraySize = 5;
            int[] challenges = { 0, 1, 3, 4, 5 };
            for (int i = 0; i < 5; i++)
            {
                var episode = episodes.GetArrayElementAtIndex(i);
                episode.FindPropertyRelative("challenge").enumValueIndex = challenges[i];
                episode.FindPropertyRelative("targetSeconds").floatValue = i == 2 || i == 4 ? 20f : 15f;
                episode.FindPropertyRelative("difficulty").floatValue = .4f;
                episode.FindPropertyRelative("title").stringValue = new[] { "Стартовая терраса", "Узкий карниз", "Разлом", "Передышка", "Финиш" }[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(value, path); AssetDatabase.SaveAssets();
        }

        private static void CheckSurfaces(LevelRoute route)
        {
            var movement = new GameObject("Traversal limits");
            movement.SetActive(false);
            float speed = movement.AddComponent<GoatController>().MaxGroundSpeed;
            float jumpVelocity = movement.AddComponent<GoatJumpController>().ChargedJumpVelocity;
            UnityEngine.Object.DestroyImmediate(movement);
            foreach (var point in route.Platforms)
            {
                Require(Physics.Raycast(point.Center + Vector3.up * 2f, Vector3.down, out var hit, 3f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), "Нет опоры под площадкой " + point.Index);
                if (Mathf.Abs(hit.point.y - point.Center.y) >= .15f)
                    for (int j = Mathf.Max(0, point.Index - 4); j <= Mathf.Min(route.Platforms.Length - 1, point.Index + 4); j++)
                        Debug.Log("NEAR_SUPPORT index=" + j + " point=" + route.Platforms[j].Center + " radius=" + route.Platforms[j].Radius);
                Require(Mathf.Abs(hit.point.y - point.Center.y) < .15f, "Перекрытая площадка " + point.Index + " center=" + point.Center + " hit=" + hit.point + " object=" + hit.collider.name);
                Require(hit.normal.y > .95f, "Площадка наклонена " + point.Index + " normal=" + hit.normal + " hit=" + hit.point + " object=" + hit.collider.name);
            }
            for (int i = 0; i < route.Connections.Length; i++)
            {
                var a = route.Platforms[i]; var b = route.Platforms[i + 1];
                if (route.Connections[i] == RouteConnection.Walk)
                {
                    for (int s = 1; s < 8; s++)
                    {
                        Vector3 point = Vector3.Lerp(a.Center, b.Center, s / 8f);
                        Require(Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out _, 4f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), "Разрыв прохода " + i);
                    }
                }
                else
                {
                    Vector3 direction = Vector3.ProjectOnPlane(b.Center - a.Center, Vector3.up).normalized;
                    Vector3 start = a.Center + direction * (a.Radius - .5f) + Vector3.up * .6f;
                    Vector3 end = b.Center - direction * (b.Radius - .5f) + Vector3.up * .6f;
                    float gravity = -Physics.gravity.y;
                    Require(gravity > 0f, "Проверке прыжков нужна направленная вниз гравитация");
                    float duration = (jumpVelocity + Mathf.Sqrt(jumpVelocity * jumpVelocity + 2f * gravity * (start.y - end.y))) / gravity;
                    float distance = Vector3.ProjectOnPlane(end - start, Vector3.up).magnitude;
                    Require(distance / duration <= speed * .76f, "Переход " + i + ": не хватает скорости реальной козы");
                    Vector3 previous = start;
                    for (int s = 1; s <= 24; s++)
                    {
                        float t = duration * s / 24f;
                        Vector3 point = start + direction * (distance * t / duration)
                            + Vector3.up * (jumpVelocity * t - .5f * gravity * t * t);
                        Require(!Physics.SphereCast(previous, .34f, (point - previous).normalized, out var obstacle,
                            Vector3.Distance(previous, point), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore),
                            "Скала перекрывает прыжок " + i + " sample=" + s + " position=" + point
                            + " obstacle=" + (obstacle.collider ? obstacle.collider.name : ""));
                        previous = point;
                    }
                }
            }
        }

        [MenuItem("Tools/GOAT/Каменная корона/Снимки формы без игровых проверок")]
        public static void RenderPreview()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<LevelRecipe>(RecipePath);
            var host = new GameObject("Crown visual preview");
            var builder = host.AddComponent<MountainSceneBuilder>(); builder.enabled = false;
            try
            {
                var level = CrownLevelBuilder.Generate(builder, recipe, false);
                Capture(level.Route);
                Debug.Log("STONE_CROWN_VISUAL_RENDER_OK");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        private static void Capture(LevelRoute route, string name = "StoneCrown")
        {
            var cameraObject = new GameObject("Crown capture");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.farClipPlane = 1800f; camera.fieldOfView = 48f;
                float scale = route.Platforms[0].Center.y / 215f;
                camera.transform.position = new Vector3(240f * Mathf.Max(.7f, scale), 210f * scale, -420f * Mathf.Max(.7f, scale));
                camera.transform.LookAt(new Vector3(0f, 110f * scale, -50f));
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); texture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name + "-overview.png", texture.EncodeToPNG());
                camera.transform.position = new Vector3(-320f * Mathf.Max(.7f, scale), 230f * scale, -350f * Mathf.Max(.7f, scale));
                camera.transform.LookAt(new Vector3(0f, 110f * scale, 0f));
                camera.Render(); texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); texture.Apply();
                File.WriteAllBytes("Logs/" + name + "-side.png", texture.EncodeToPNG());
                camera.transform.position = new Vector3(100f * Mathf.Max(.7f, scale), 180f * scale, -260f * Mathf.Max(.7f, scale));
                camera.transform.LookAt(new Vector3(0f, 150f * scale, 0f));
                camera.Render(); texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); texture.Apply();
                File.WriteAllBytes("Logs/" + name + "-detail.png", texture.EncodeToPNG());
                Vector3 forward = (route.Platforms[1].Center - route.Platforms[0].Center).normalized;
                camera.transform.position = route.Spawn - forward * 8f + Vector3.up * 4f;
                camera.transform.LookAt(route.Platforms[2].Center + Vector3.up);
                camera.Render(); texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); texture.Apply();
                File.WriteAllBytes("Logs/" + name + "-route.png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
