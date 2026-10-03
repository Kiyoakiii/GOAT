using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GoatDescent.ProceduralWorld.Editor
{
    public static class ProceduralWorldEditorActions
    {
        [MenuItem("Tools/Procedural World/Generate World")]
        public static void GenerateWorldFromMenu()
        {
            GenerateWorld(GetOrCreateSettings());
        }

        [MenuItem("Tools/Procedural World/Generate Groundcover Preview (Safe)")]
        public static void GenerateGroundcoverPreviewFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Groundcover preview was not generated. Stop Play Mode first; the active scene will remain untouched.");
                return;
            }

            GenerateWorld(GetOrCreateSettings(), VistaAssetPaths.GroundcoverPreviewScenePath, false);
        }

        [MenuItem("Tools/Procedural World/Regenerate Terrain")]
        public static void RegenerateTerrainFromMenu()
        {
            GenerateWorld(GetOrCreateSettings());
        }

        [MenuItem("Tools/Procedural World/Generate Debug Maps")]
        public static void GenerateDebugMapsFromMenu()
        {
            WorldSettings settings = GetOrCreateSettings();
            TerrainBuildResult result = TerrainGenerator.Build(settings);
            VistaDebugTexturesEditor.GenerateDebugTextures(settings, result);
            AssetDatabase.SaveAssets();
            Debug.Log("PROCEDURAL_WORLD_DEBUG_MAPS_OK modes=Height,MountainMask,Biomes,Temperature,Humidity,Slope,ForestDensity,RockDensity,CliffMask,SnowMask,GrassDensity");
        }

        [MenuItem("Tools/Procedural World/Clear Generated Objects")]
        public static void ClearGeneratedObjectsFromMenu()
        {
            if (!File.Exists(VistaAssetPaths.ScenePath))
                return;
            Scene activeScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(VistaAssetPaths.ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.name == "Procedural World Milestone")
                        UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (activeScene.IsValid())
                    SceneManager.SetActiveScene(activeScene);
            }
            Debug.Log($"PROCEDURAL_WORLD_CLEAR_OK scene={VistaAssetPaths.ScenePath}");
        }

        public static WorldSettings GetOrCreateSettings()
        {
            VistaAssetUtils.EnsureFolders();
            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(VistaAssetPaths.SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WorldSettings>();
                AssetDatabase.CreateAsset(settings, VistaAssetPaths.SettingsPath);
            }
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static VegetationGenerationSettings GetOrCreateVegetationSettings()
        {
            VistaAssetUtils.EnsureFolders();
            VegetationGenerationSettings vegetation = AssetDatabase.LoadAssetAtPath<VegetationGenerationSettings>(VistaAssetPaths.VegetationSettingsPath);
            if (vegetation == null)
            {
                vegetation = ScriptableObject.CreateInstance<VegetationGenerationSettings>();
                AssetDatabase.CreateAsset(vegetation, VistaAssetPaths.VegetationSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return vegetation;
        }

        public static void GenerateWorld(WorldSettings settings)
        {
            GenerateWorld(settings, VistaAssetPaths.ScenePath, true);
        }

        private static void GenerateWorld(WorldSettings settings, string outputScenePath, bool includeInBuildSettings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(outputScenePath))
                throw new ArgumentException("An output scene path is required.", nameof(outputScenePath));
            VistaAssetUtils.EnsureFolders();
            AssetDatabase.Refresh();
            VegetationGenerationSettings vegetation = GetOrCreateVegetationSettings();
            var stopwatch = Stopwatch.StartNew();
            TerrainData terrainData = GetOrCreateTerrainData();
            TerrainBuildResult terrainResult = TerrainGenerator.Build(settings);
            int[,] grassDensityMap = VistaTerrainEditor.ConfigureTerrainData(terrainData, terrainResult, settings, vegetation);
            VistaMaterialsEditor.MaterialSet materials = VistaMaterialsEditor.GetOrCreateMaterials();
            GameObject[] rocks = VistaWorldBuilderEditor.LoadPrefabs(VistaAssetPaths.RockPaths);
            GameObject[] trees = VistaWorldBuilderEditor.LoadPrefabs(VistaAssetPaths.TreePaths);
            GameObject[] summitTrees = VistaWorldBuilderEditor.LoadPrefabs(VistaAssetPaths.SummitTreePaths);
            GameObject[] cliffs = VistaWorldBuilderEditor.LoadPrefabs(VistaAssetPaths.CliffPaths);
            if (rocks.Length == 0 || trees.Length == 0 || summitTrees.Length < 4 || cliffs.Length == 0)
                throw new InvalidOperationException("Generated Blender FBX assets are missing. Run Tools/Blender/generate_world_asset_pack.py first.");

            Scene activeScene = SceneManager.GetActiveScene();
            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var worldRoot = new GameObject("Procedural World Milestone");
                SceneManager.MoveGameObjectToScene(worldRoot, previewScene);
                var generator = worldRoot.AddComponent<WorldGenerator>();
                generator.Configure(settings, worldRoot.transform);

                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainObject.name = $"Terrain — seed {settings.seed}";
                SceneManager.MoveGameObjectToScene(terrainObject, previewScene);
                terrainObject.transform.SetParent(worldRoot.transform);
                Terrain terrain = terrainObject.GetComponent<Terrain>();
                TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>() ?? terrainObject.AddComponent<TerrainCollider>();
                terrainCollider.terrainData = terrainData;
                terrain.drawInstanced = true;
                terrain.detailObjectDistance = 150f;
                terrain.detailObjectDensity = 0.9f;
                terrain.heightmapPixelError = 5;

                VistaWorldBuilderEditor.CreateWater(worldRoot.transform, previewScene, settings, materials.water);
                List<WorldChunk> chunks = VistaWorldBuilderEditor.CreateChunks(worldRoot.transform, previewScene, settings);
                VistaWorldBuilderEditor.SpawnWorldObjects(chunks, previewScene, terrain, terrainResult, settings, vegetation, rocks, trees, summitTrees, cliffs, materials);
                VistaGroundcoverEditor.CreateGrassMeshes(worldRoot.transform, previewScene, terrain, grassDensityMap, terrainResult, settings);
                VistaWorldBuilderEditor.CreateLighting(worldRoot.transform, previewScene);
                MistyPillarsVistaEditor.ApplyToScene(previewScene);
                VistaCamerasEditor.CreateCaptureCameras(worldRoot.transform, previewScene, terrain, terrainResult, grassDensityMap, settings);
                VistaDebugTexturesEditor.GenerateDebugTextures(settings, terrainResult);
                EditorSceneManager.SaveScene(previewScene, outputScenePath);
                if (includeInBuildSettings)
                    VistaAssetUtils.EnsureSceneInBuildSettings(outputScenePath);
                AssetDatabase.SaveAssets();
                stopwatch.Stop();
                Debug.Log($"PROCEDURAL_WORLD_MILESTONE_OK seed={settings.seed} terrain={settings.worldSize:F0}m chunks={chunks.Count} scene={outputScenePath} buildScene={includeInBuildSettings} ms={stopwatch.ElapsedMilliseconds} screenshots={GetScreenshotRoot()}");
            }
            finally
            {
                EditorSceneManager.CloseScene(previewScene, true);
                if (activeScene.IsValid())
                    SceneManager.SetActiveScene(activeScene);
            }
        }

        private static TerrainData GetOrCreateTerrainData()
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(VistaAssetPaths.TerrainDataPath);
            if (data != null)
                return data;
            data = new TerrainData { name = "ProceduralWorldTerrainData" };
            AssetDatabase.CreateAsset(data, VistaAssetPaths.TerrainDataPath);
            return data;
        }

        private static string GetScreenshotRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "goat-procedural-world-milestone");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}