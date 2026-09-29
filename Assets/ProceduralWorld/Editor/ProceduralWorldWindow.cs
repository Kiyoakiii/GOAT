using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GoatDescent.ProceduralWorld.Editor
{
    public sealed class ProceduralWorldWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/ProceduralWorld/Editor/ProceduralWorldWindow.uxml";
        private WorldSettings settings;
        private VegetationGenerationSettings vegetation;
        private Label statusLabel;

        [MenuItem("Tools/Procedural World/Open Generator")]
        public static void Open()
        {
            var window = GetWindow<ProceduralWorldWindow>();
            window.titleContent = new GUIContent("Procedural World");
            window.minSize = new Vector2(380f, 660f);
        }

        public void CreateGUI()
        {
            settings = ProceduralWorldEditorActions.GetOrCreateSettings();
            vegetation = ProceduralWorldEditorActions.GetOrCreateVegetationSettings();
            rootVisualElement.Clear();
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (layout == null)
            {
                rootVisualElement.Add(new Label("Procedural World UI asset is missing. Reimport the project."));
                return;
            }
            layout.CloneTree(rootVisualElement);
            statusLabel = rootVisualElement.Q<Label>("statusLabel");
            BindFields();
            BindButtons();
            SetStatus("Ready — density changes are saved to VegetationGenerationSettings.");
        }

        private void BindFields()
        {
            BindInteger("seedField", () => settings.seed, value => settings.seed = value);
            BindFloat("worldSizeField", () => settings.worldSize, value => settings.worldSize = Mathf.Max(512f, value));
            BindFloat("heightScaleField", () => settings.heightScale, value => settings.heightScale = Mathf.Max(64f, value));
            BindFloat("seaLevelField", () => settings.seaLevel, value => settings.seaLevel = Mathf.Clamp01(value));
            BindFloat("mountainStrengthField", () => settings.mountainStrength, value => settings.mountainStrength = Mathf.Clamp01(value));
            BindFloat("mountainFrequencyField", () => settings.mountainFrequency, value => settings.mountainFrequency = Mathf.Max(0.0001f, value));
            BindFloat("biomeScaleField", () => settings.biomeScale, value => settings.biomeScale = Mathf.Max(0.0001f, value));
            BindFloat("erosionStrengthField", () => settings.erosionStrength, value => settings.erosionStrength = Mathf.Clamp01(value));
            BindFloat("snowLineField", () => settings.snowLine, value => settings.snowLine = Mathf.Clamp(value, 0.55f, 0.95f));
            BindFloat("snowTemperatureThresholdField", () => settings.snowTemperatureThreshold, value => settings.snowTemperatureThreshold = Mathf.Clamp01(value));
            BindFloat("snowCoverageField", () => settings.snowCoverage, value => settings.snowCoverage = Mathf.Clamp01(value));
            BindVegetationFloat("treeDensityField", () => vegetation.treeDensity, value => vegetation.treeDensity = Mathf.Clamp01(value));
            BindVegetationInteger("summitTreeCountField", () => vegetation.summitTreeCount, value => vegetation.summitTreeCount = Mathf.Clamp(value, 0, 200));
            BindVegetationFloat("grassDensityField", () => vegetation.grassDensity, value => vegetation.grassDensity = Mathf.Clamp(value, 0f, 2f));
            BindFloat("grassSlopeLimitField", () => settings.grassSlopeLimit, value => settings.grassSlopeLimit = Mathf.Clamp(value, 5f, 55f));
        }

        private void BindButtons()
        {
            rootVisualElement.Q<Button>("generateWorldButton").clicked += () => Execute("World generated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("terrainButton").clicked += () => Execute("Terrain regenerated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("biomesButton").clicked += () => Execute("Biomes regenerated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("vegetationButton").clicked += () => Execute("Vegetation regenerated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("rocksButton").clicked += () => Execute("Rocks regenerated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("cliffsButton").clicked += () => Execute("Cliffs regenerated", () => ProceduralWorldEditorActions.GenerateWorld(settings));
            rootVisualElement.Q<Button>("debugButton").clicked += () => Execute("Debug maps generated", ProceduralWorldEditorActions.GenerateDebugMapsFromMenu);
            rootVisualElement.Q<Button>("clearButton").clicked += () => Execute("Generated objects cleared", ProceduralWorldEditorActions.ClearGeneratedObjectsFromMenu);
            rootVisualElement.Q<Button>("randomSeedButton").clicked += RandomizeSeed;
        }

        private void BindInteger(string name, System.Func<int> getter, System.Action<int> setter)
        {
            IntegerField field = rootVisualElement.Q<IntegerField>(name);
            field.SetValueWithoutNotify(getter());
            field.RegisterValueChangedCallback(change => ChangeSettings(() => setter(change.newValue)));
        }

        private void BindFloat(string name, System.Func<float> getter, System.Action<float> setter)
        {
            FloatField field = rootVisualElement.Q<FloatField>(name);
            field.SetValueWithoutNotify(getter());
            field.RegisterValueChangedCallback(change => ChangeSettings(() => setter(change.newValue)));
        }

        private void BindVegetationInteger(string name, System.Func<int> getter, System.Action<int> setter)
        {
            IntegerField field = rootVisualElement.Q<IntegerField>(name);
            field.SetValueWithoutNotify(getter());
            field.RegisterValueChangedCallback(change => ChangeVegetationSettings(() => setter(change.newValue)));
        }

        private void BindVegetationFloat(string name, System.Func<float> getter, System.Action<float> setter)
        {
            FloatField field = rootVisualElement.Q<FloatField>(name);
            field.SetValueWithoutNotify(getter());
            field.RegisterValueChangedCallback(change => ChangeVegetationSettings(() => setter(change.newValue)));
        }

        private void ChangeSettings(System.Action change)
        {
            Undo.RecordObject(settings, "Adjust procedural world settings");
            change();
            EditorUtility.SetDirty(settings);
            SetStatus("Settings changed — press Generate World to apply.");
        }

        private void ChangeVegetationSettings(System.Action change)
        {
            Undo.RecordObject(vegetation, "Adjust vegetation generation settings");
            change();
            EditorUtility.SetDirty(vegetation);
            SetStatus("Vegetation density changed — press Generate World to apply.");
        }

        private void RandomizeSeed()
        {
            Undo.RecordObject(settings, "Randomize procedural world seed");
            settings.seed = System.Environment.TickCount & int.MaxValue;
            EditorUtility.SetDirty(settings);
            rootVisualElement.Q<IntegerField>("seedField").SetValueWithoutNotify(settings.seed);
            SetStatus($"Seed {settings.seed} is ready to generate.");
        }

        private void Execute(string success, System.Action action)
        {
            try
            {
                action();
                SetStatus(success);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                SetStatus($"Failed: {exception.Message}");
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
