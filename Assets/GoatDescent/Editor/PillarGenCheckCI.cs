using System;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent.EditorTools
{
    public static class PillarGenCheckCI
    {
        private sealed class VistaStats
        {
            public int roots;
            public int stone;
            public int forest;
            public int branches;
            public int markers;
            public int stoneTris;
            public int forestTris;
            public int controllerTowers;
        }

        public static void Check()
        {
            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int errors = 0;

            var controller = UnityEngine.Object.FindFirstObjectByType<MistyPillarsSceneController>(FindObjectsInactive.Include);
            if (!controller)
            {
                Debug.LogError("PILLAR_CHECK: controller not found");
                throw new MissingReferenceException("MistyPillarsSceneController missing from milestone scene");
            }
            if (controller.GenerateOnStart)
            {
                Debug.LogError("PILLAR_CHECK: generateOnStart must be off while a baked vista root exists");
                errors++;
            }

            errors += ExpectBakedVista(CollectStats(), "baked");

            controller.ClearGenerated();
            controller.Generate();
            VistaStats generated = CollectStats();
            if (generated.controllerTowers != controller.TowerCount)
            {
                Debug.LogError($"PILLAR_CHECK: generated {generated.controllerTowers} towers under controller, expected {controller.TowerCount}");
                errors++;
            }
            if (generated.markers != 2)
            {
                Debug.LogError($"PILLAR_CHECK: expected baked+generated spawn markers after Generate, got {generated.markers}");
                errors++;
            }

            controller.ClearGenerated();
            VistaStats restored = CollectStats();
            errors += ExpectBakedVista(restored, "restored");
            if (restored.controllerTowers != 0)
            {
                Debug.LogError("PILLAR_CHECK: ClearGenerated left towers under the controller");
                errors++;
            }

            Debug.Log($"PILLAR_CHECK errors={errors} roots={restored.roots} stone={restored.stone} forest={restored.forest} " +
                      $"branches={restored.branches} markers={restored.markers} stoneTris={restored.stoneTris} forestTris={restored.forestTris}");
            if (errors > 0) throw new InvalidOperationException("PILLAR_CHECK failed with " + errors + " errors");
        }

        private static int ExpectBakedVista(VistaStats stats, string stage)
        {
            int errors = 0;
            int expected = MistyPillarsVistaBuilder.DefaultTowerCount;
            if (stats.roots != 1)
            {
                Debug.LogError($"PILLAR_CHECK ({stage}): expected 1 baked vista root, got {stats.roots}");
                errors++;
            }
            if (stats.stone != expected || stats.forest != expected || stats.branches != expected)
            {
                Debug.LogError($"PILLAR_CHECK ({stage}): expected {expected} stone/forest/branch meshes, got stone={stats.stone} forest={stats.forest} branches={stats.branches}");
                errors++;
            }
            if (stats.markers != 1)
            {
                Debug.LogError($"PILLAR_CHECK ({stage}): expected 1 spawn marker, got {stats.markers}");
                errors++;
            }
            return errors;
        }

        private static VistaStats CollectStats()
        {
            var stats = new VistaStats();
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == MistyPillarsVistaBuilder.RootName) stats.roots++;
                if (root.name == "Misty Scene Controller")
                    foreach (Transform child in root.transform)
                        if (child.name == MistyPillarsVistaBuilder.RootName)
                            stats.controllerTowers += child.childCount;
            }
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!filter.sharedMesh) continue;
                if (filter.name == MistyPillarsVistaBuilder.RockObjectName)
                {
                    stats.stone++;
                    stats.stoneTris += (int)filter.sharedMesh.GetIndexCount(0) / 3;
                }
                else if (filter.name == MistyPillarsVistaBuilder.ForestObjectName)
                {
                    stats.forest++;
                    stats.forestTris += (int)filter.sharedMesh.GetIndexCount(0) / 3;
                }
                else if (filter.name == MistyPillarsVistaBuilder.BranchObjectName) stats.branches++;
            }
            foreach (var marker in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (marker.name == MistyPillarsVistaBuilder.SpawnMarkerName) stats.markers++;
            return stats;
        }
    }
}
