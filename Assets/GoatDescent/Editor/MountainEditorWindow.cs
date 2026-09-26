using UnityEditor;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public sealed class MountainEditorWindow : EditorWindow
    {
        private const string PrefsKey = "GOAT.MountainSettings.v1";

        private MountainSettings settings = new MountainSettings();
        private GameObject previewRoot;
        private Vector2 scrollPos;

        [MenuItem("Tools/GOAT/Mountain Editor")]
        public static void Open()
        {
            GetWindow<MountainEditorWindow>("Mountain Editor");
        }

        private void OnEnable()
        {
            settings = LoadOrCreate();
            var builder = Object.FindFirstObjectByType<MountainSceneBuilder>();
            if (builder) builder.settings = settings;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Генератор горы", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            bool changed = false;

            EditorGUILayout.HelpBox(
                "Гора строится вокруг маршрута: крутой хребет спускается от вершины до долины, " +
                "в него врезаны площадки. «Число площадок» = сколько уступов на всём спуске, " +
                "«Дальность прыжка» = горизонтальный шаг между ними. Декор (деревья, кусты, камни) " +
                "размещается в логичных зонах по крутизне и высоте.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Площадки (главное)", EditorStyles.boldLabel);
            changed |= Toggle("Включить маршрут", ref settings.EnableRoute);
            changed |= IntDrag("Число площадок", ref settings.PlatformCount, 6, 60);
            changed |= Drag("Дальность прыжка (м)", ref settings.PlatformStep, 3f, 12f);
            changed |= Drag("Размер уступа", ref settings.PlatformSize, 2f, 12f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Размер", EditorStyles.boldLabel);
            changed |= Drag("Радиус", ref settings.Radius, 60f, 500f);
            changed |= Drag("Высота вершины", ref settings.SummitY, 80f, 600f);
            changed |= Drag("Высота долины", ref settings.ValleyY, -50f, 50f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Форма", EditorStyles.boldLabel);
            changed |= IntDrag("Число кулуаров", ref settings.CouloirCount, 0, 10);
            changed |= IntDrag("Число поясов", ref settings.StrataCount, 3, 30);
            changed |= IntDrag("Сидер (случайность)", ref settings.Seed, 0, 100000);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Декор", EditorStyles.boldLabel);
            changed |= Toggle("Включить декор", ref settings.EnableDecor);
            changed |= IntDrag("Число кустов", ref settings.BushCount, 0, 200);
            changed |= IntDrag("Число деревьев", ref settings.TreeCount, 0, 200);
            changed |= IntDrag("Число камней", ref settings.RockCount, 0, 160);

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
                if (builder)
                {
                    builder.settings = settings;
                    builder.RebuildTerrain();
                }
                RebuildPreview();
            }
            EditorGUILayout.EndScrollView();
        }

        private static MountainSettings LoadOrCreate()
        {
            string json = EditorPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new MountainSettings();
            try
            {
                var loaded = JsonUtility.FromJson<MountainSettings>(json);
                return loaded ?? new MountainSettings();
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
            material.SetFloat("_HeightMin", settings.ValleyY - 5f);
            material.SetFloat("_HeightMax", settings.SummitY + 5f);
            surface.AddComponent<MeshRenderer>().sharedMaterial = material;

            var decor = new GameObject("Preview Decor");
            decor.transform.SetParent(previewRoot.transform, false);
            MountainDecor.Generate(decor.transform, settings);

            SceneView.RepaintAll();
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