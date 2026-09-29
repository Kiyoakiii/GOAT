using UnityEditor;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public sealed class MountainEditorWindow : EditorWindow
    {
        private const string PrefsKey = "GOAT.MountainSettings.v2";
        private const string PreviousPrefsKey = "GOAT.MountainSettings.v1";

        private MountainSettings settings = new MountainSettings();
        private GameObject previewRoot;
        private Vector2 scrollPos;
        private double rebuildDeadline;
        private bool rebuildPending;

        [MenuItem("Tools/GOAT/Mountain Editor")]
        public static void Open()
        {
            GetWindow<MountainEditorWindow>("Mountain Editor");
        }

        private void OnEnable()
        {
            settings = LoadOrCreate();
            Save(settings);
            var builder = Object.FindFirstObjectByType<MountainSceneBuilder>();
            if (builder) builder.settings = settings;
        }

        private void OnDisable()
        {
            if (rebuildPending) EditorApplication.update -= ProcessScheduledRebuild;
            rebuildPending = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Генератор горы", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            bool changed = false;

            EditorGUILayout.HelpBox(
                "Маршрут собран из зигзагов, стенок, коротких перебежек и дальних прыжков. " +
                "Генератор добавит ступени, если выбранного числа недостаточно для безопасной длины спуска.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Площадки (главное)", EditorStyles.boldLabel);
            changed |= Toggle("Включить маршрут", ref settings.EnableRoute);
            changed |= IntDrag("Базовое число площадок", ref settings.PlatformCount, 6, 100);
            changed |= Drag("Дальность прыжка (м)", ref settings.PlatformStep, 3f, 12f);
            changed |= Drag("Размер уступа", ref settings.PlatformSize, 2f, 8f);
            changed |= Drag("Провал между (м)", ref settings.PlatformGap, 0f, 6f);
            changed |= Drag("Сложность маршрута", ref settings.Difficulty, 0f, 1f);
            changed |= Toggle("Альтернативные ветки", ref settings.EnableBranches);
            changed |= Toggle("Особые площадки", ref settings.EnablePlatformTypes);
            changed |= Toggle("Боковой ветер", ref settings.EnableWind);
            changed |= Toggle("Чекпоинты на полках отдыха", ref settings.CheckpointRespawn);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Размер", EditorStyles.boldLabel);
            changed |= Drag("Радиус", ref settings.Radius, 30f, 500f);
            changed |= Drag("Высота вершины", ref settings.SummitY, 80f, 600f);
            changed |= Drag("Высота долины", ref settings.ValleyY, -50f, 50f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Форма", EditorStyles.boldLabel);
            changed |= IntDrag("Число кулуаров", ref settings.CouloirCount, 0, 10);
            changed |= IntDrag("Число поясов", ref settings.StrataCount, 3, 30);
            changed |= Drag("Искривление формы", ref settings.WarpAmplitude, 0f, 60f);
            changed |= Drag("Отроги (асимметрия)", ref settings.Asymmetry, 0f, 40f);
            changed |= Drag("Шероховатость", ref settings.NoiseAmplitude, 0f, 12f);
            changed |= Drag("Смещение вершины", ref settings.PeakOffset, 0f, settings.Radius * .5f);
            changed |= Drag("Вытянутость массива", ref settings.Stretch, 0f, 1.5f);
            changed |= Drag("Скалистость хребтов", ref settings.RidgeFraction, 0f, 0.4f);
            changed |= Drag("Сила водной эрозии", ref settings.ErosionStrength, 0f, 2f);
            changed |= Drag("Сила осыпей", ref settings.TalusStrength, 0f, 2f);
            changed |= Drag("Угол осыпания", ref settings.TalusAngle, 25f, 55f);
            changed |= IntDrag("Сидер (случайность)", ref settings.Seed, 0, 100000);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Декор", EditorStyles.boldLabel);
            changed |= Toggle("Включить декор", ref settings.EnableDecor);
            changed |= IntDrag("Число кустов", ref settings.BushCount, 0, 800);
            changed |= IntDrag("Число деревьев", ref settings.TreeCount, 0, 700);
            changed |= IntDrag("Число камней", ref settings.RockCount, 0, 400);
            changed |= IntDrag("Число цветов", ref settings.FlowerCount, 0, 1000);
            changed |= IntDrag("Число травы", ref settings.GrassTuftCount, 0, 2500);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Разрешение", EditorStyles.boldLabel);
            changed |= IntDrag("Сетка", ref settings.Grid, 64, 320);

            EditorGUILayout.Space();
            if (GUILayout.Button("Случайная гора"))
            {
                settings.Seed = Random.Range(0, 100000);
                changed = true;
            }
            if (GUILayout.Button("Обновить превью"))
            {
                CancelScheduledRebuild();
                RebuildPreview();
                changed = false;
            }
            if (GUILayout.Button("Убрать превью"))
            {
                ClearPreview();
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Настройки сохраняются автоматически и восстанавливаются при открытии.", MessageType.Info);

            if (changed)
            {
                Save(settings);
                var builder = Object.FindFirstObjectByType<MountainSceneBuilder>();
                if (builder) builder.settings = settings;
                ScheduleRebuild();
            }
            EditorGUILayout.EndScrollView();
        }

        private static MountainSettings LoadOrCreate()
        {
            string json = EditorPrefs.GetString(PrefsKey, "");
            bool migrating = string.IsNullOrEmpty(json);
            if (migrating) json = EditorPrefs.GetString(PreviousPrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new MountainSettings();
            try
            {
                var loaded = JsonUtility.FromJson<MountainSettings>(json);
                if (loaded == null) return new MountainSettings();
                var defaults = new MountainSettings();
                if (migrating && loaded.SummitY < 100f)
                    loaded.SummitY = Mathf.Max(loaded.SummitY * 1.8f, loaded.ValleyY + 120f);
                if (!json.Contains("\"RidgeFraction\"")) loaded.RidgeFraction = defaults.RidgeFraction;
                if (!json.Contains("\"ErosionStrength\"")) loaded.ErosionStrength = defaults.ErosionStrength;
                if (!json.Contains("\"TalusStrength\"")) loaded.TalusStrength = defaults.TalusStrength;
                if (!json.Contains("\"TalusAngle\"")) loaded.TalusAngle = defaults.TalusAngle;
                if (!json.Contains("\"Difficulty\"")) loaded.Difficulty = defaults.Difficulty;
                if (!json.Contains("\"EnableBranches\"")) loaded.EnableBranches = defaults.EnableBranches;
                if (!json.Contains("\"EnablePlatformTypes\"")) loaded.EnablePlatformTypes = defaults.EnablePlatformTypes;
                if (!json.Contains("\"EnableWind\"")) loaded.EnableWind = defaults.EnableWind;
                if (!json.Contains("\"CheckpointRespawn\"")) loaded.CheckpointRespawn = defaults.CheckpointRespawn;
                return loaded;
            }
            catch
            {
                return new MountainSettings();
            }
        }

        private static void Save(MountainSettings s)
        {
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(s));
        }

        private static bool Drag(string label, ref float value, float min, float max)
        {
            EditorGUILayout.LabelField(label);
            float v = EditorGUILayout.Slider(value, min, max);
            if (Mathf.Abs(v - value) > 0.0001f) { value = v; return true; }
            return false;
        }

        private static bool IntDrag(string label, ref int value, int min, int max)
        {
            EditorGUILayout.LabelField(label);
            int v = EditorGUILayout.IntSlider(value, min, max);
            if (v != value) { value = v; return true; }
            return false;
        }

        private static bool Toggle(string label, ref bool value)
        {
            bool v = EditorGUILayout.Toggle(label, value);
            if (v != value) { value = v; return true; }
            return false;
        }

        private void RebuildPreview()
        {
            var builder = Object.FindFirstObjectByType<MountainSceneBuilder>();
            if (builder)
            {
                builder.settings = settings;
                builder.RebuildTerrain();
                SceneView.RepaintAll();
                return;
            }

            if (!previewRoot)
            {
                previewRoot = new GameObject("Mountain Preview (Editor)");
                previewRoot.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }
            previewRoot.transform.position = Vector3.zero;

            for (int i = previewRoot.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(previewRoot.transform.GetChild(i).gameObject);

            var surface = new GameObject("Preview Surface");
            surface.transform.SetParent(previewRoot.transform, false);
            var filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = MountainGenerator.Build(settings);
            var material = MountainMaterial.Get();
            MountainMaterial.Configure(material, settings);
            surface.AddComponent<MeshRenderer>().sharedMaterial = material;

            var decor = new GameObject("Preview Decor");
            decor.transform.SetParent(previewRoot.transform, false);
            MountainDecor.Generate(decor.transform, settings);
            MountainRouteProps.Generate(previewRoot.transform, settings);

            SceneView.RepaintAll();
        }

        private void ScheduleRebuild()
        {
            rebuildDeadline = EditorApplication.timeSinceStartup + 0.3;
            if (rebuildPending) return;
            rebuildPending = true;
            EditorApplication.update += ProcessScheduledRebuild;
        }

        private void ProcessScheduledRebuild()
        {
            if (EditorApplication.timeSinceStartup < rebuildDeadline) return;
            CancelScheduledRebuild();
            RebuildPreview();
        }

        private void CancelScheduledRebuild()
        {
            if (rebuildPending) EditorApplication.update -= ProcessScheduledRebuild;
            rebuildPending = false;
        }

        private void ClearPreview()
        {
            if (previewRoot)
            {
                DestroyImmediate(previewRoot);
                previewRoot = null;
            }
        }
    }
}
