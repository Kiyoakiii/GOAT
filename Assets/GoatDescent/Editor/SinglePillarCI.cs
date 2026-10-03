using System;
using GoatDescent.ProceduralWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class SinglePillarCI
    {
        public static void Check()
        {
            string path = "Assets/GoatDescent/Scenes/Testing/SinglePillarPlayground.unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int errors = 0;

            var controller = UnityEngine.Object.FindFirstObjectByType<SinglePillarSceneController>(FindObjectsInactive.Include);
            if (!controller)
            {
                Debug.LogError("SINGLE_PILLAR: controller not found");
                throw new MissingReferenceException("SinglePillarSceneController missing from scene");
            }
            if (controller.GenerateOnStart)
            {
                Debug.LogError("SINGLE_PILLAR: generateOnStart must be off for a baked scene");
                errors++;
            }

            controller.ClearGenerated();
            controller.Generate();

            int rock = 0, forest = 0, branches = 0, platforms = 0, markers = 0, roots = 0;
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!filter.sharedMesh) continue;
                if (filter.name == MistyPillarsVistaBuilder.RockObjectName) rock++;
                else if (filter.name == MistyPillarsVistaBuilder.ForestObjectName) forest++;
                else if (filter.name == MistyPillarsVistaBuilder.BranchObjectName) branches++;
                else if (filter.name == SinglePillarSceneController.PlatformName) platforms++;
            }
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == MistyPillarsVistaBuilder.SpawnMarkerName) markers++;
                if (t.name == SinglePillarSceneController.RootName && t.parent == controller.transform) roots++;
            }

            if (roots != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 generated root, got {roots}"); errors++; }
            if (rock != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 rock mesh, got {rock}"); errors++; }
            if (forest != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 forest mesh, got {forest}"); errors++; }
            if (branches != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 branch mesh, got {branches}"); errors++; }
            if (platforms != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 platform, got {platforms}"); errors++; }
            if (markers != 1) { Debug.LogError($"SINGLE_PILLAR: expected 1 spawn marker, got {markers}"); errors++; }

            var rockRenderer = GameObject.Find(MistyPillarsVistaBuilder.RockObjectName);
            if (!rockRenderer || !rockRenderer.GetComponent<MeshCollider>() || !rockRenderer.GetComponent<GoatDescent.MountainSlopeSurface>())
            {
                Debug.LogError("SINGLE_PILLAR: rock must have MeshCollider + MountainSlopeSurface");
                errors++;
            }

            var platform = GameObject.Find(SinglePillarSceneController.PlatformName);
            if (!platform || !platform.GetComponent<MeshCollider>() || !platform.GetComponent<GoatDescent.MountainSlopeSurface>())
            {
                Debug.LogError("SINGLE_PILLAR: platform must have MeshCollider + MountainSlopeSurface");
                errors++;
            }
            else
            {
                float top = controller.PlatformTopLocalY;
                if (Physics.Raycast(new Vector3(0f, top + 10f, 0f), Vector3.down, out RaycastHit hit, 40f))
                {
                    if (hit.collider.gameObject != platform)
                    {
                        Debug.LogError("SINGLE_PILLAR: raycast above platform hit " + hit.collider.name);
                        errors++;
                    }
                    if (Mathf.Abs(hit.point.y - top) > .01f)
                    {
                        Debug.LogError($"SINGLE_PILLAR: platform top {hit.point.y:F2}, expected {top:F2}");
                        errors++;
                    }
                    if (hit.normal.y < .999f)
                    {
                        Debug.LogError($"SINGLE_PILLAR: platform normal not flat: {hit.normal}");
                        errors++;
                    }
                }
                else
                {
                    Debug.LogError("SINGLE_PILLAR: no raycast hit above platform");
                    errors++;
                }
                if (!controller.SpawnMarker || Mathf.Abs(controller.SpawnMarker.position.y - (top + .12f)) > .01f)
                {
                    Debug.LogError("SINGLE_PILLAR: spawn marker not on platform top");
                    errors++;
                }
            }

            Debug.Log($"SINGLE_PILLAR errors={errors} rock={rock} forest={forest} branches={branches} platforms={platforms} markers={markers}");
            if (errors > 0) throw new InvalidOperationException("SINGLE_PILLAR failed with " + errors + " errors");
        }
    }
}
