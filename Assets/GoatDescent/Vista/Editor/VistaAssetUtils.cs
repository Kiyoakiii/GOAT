using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaAssetUtils
    {
        internal static GameObject CreateChild(string name, Transform parent, Scene scene)
        {
            var result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, scene);
            result.transform.SetParent(parent);
            return result;
        }

        internal static void EnsureFolders()
        {
            EnsureFolder("Assets/GoatDescent/Vista");
            EnsureFolder("Assets/GoatDescent/Vista/Runtime");
            EnsureFolder("Assets/GoatDescent/Vista/Editor");
            EnsureFolder("Assets/GoatDescent/Vista/Data");
            EnsureFolder("Assets/GoatDescent/Vista/Generated");
            EnsureFolder("Assets/GoatDescent/Vista/Generated/Terrain");
            EnsureFolder(VistaAssetPaths.MaterialRoot);
            EnsureFolder(VistaAssetPaths.DebugRoot);
            EnsureFolder(VistaAssetPaths.GrassRoot);
            EnsureFolder("Assets/GoatDescent/Vista/Scenes");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        internal static void EnsureSceneInBuildSettings(string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.path == scenePath)
                    return;

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}