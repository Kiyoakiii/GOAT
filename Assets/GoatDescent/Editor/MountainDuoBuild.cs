#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class MountainDuoBuild
    {
        [MenuItem("Tools/Goat Descent/Build two-PC mountain demo")]
        public static void BuildWindows()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/MountainDuo"));
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/GoatDescent/Scenes/GoatDescentPrototype.unity",
                    "Assets/ProceduralWorld/Scenes/ProceduralWorldMilestone.unity"
                },
                locationPathName = Path.Combine(folder, "GoatDescent.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            bool success = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"MOUNTAIN_DUO_BUILD result={report.summary.result} output={folder} bytes={report.summary.totalSize}");
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
