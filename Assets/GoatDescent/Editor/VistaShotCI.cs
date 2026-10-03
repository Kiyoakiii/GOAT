using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace GoatDescent.EditorTools
{
    public static class VistaShotCI
    {
        public static void Shoot()
        {
            string path = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int stoneTris = 0, forestTris = 0, branchTris = 0, renderers = 0, shadowCasters = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var filter = r.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;
                int tris = (int)filter.sharedMesh.GetIndexCount(0) / 3;
                renderers++;
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowCasters++;
                if (r.name == "Stratified sandstone") stoneTris += tris;
                else if (r.name.StartsWith("Dense broadleaf")) forestTris += tris;
                else if (r.name.StartsWith("Tree trunks")) branchTris += tris;
            }
            Debug.Log($"VISTA_STATS renderers={renderers} shadowCasters={shadowCasters} stoneTris={stoneTris} forestTris={forestTris} branchTris={branchTris}");
            string suffix = System.Environment.GetEnvironmentVariable("VISTA_SHOT_SUFFIX") ?? "before";
            string root = "C:/Users/Вадим/Unity Projects/GOAT";
            Shoot("MistyPillarsVista", root + "/vista-" + suffix + ".png");
            Shoot("MistyPillarsClose", root + "/vista-close-" + suffix + ".png");
            EditorApplication.Exit(0);
        }

        public static void ShootSinglePillar()
        {
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/Testing/SinglePillarPlayground.unity", OpenSceneMode.Single);
            var camGo = new GameObject("shot-cam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(150f, 250f, -150f);
            cam.transform.LookAt(new Vector3(0f, 160f, 0f));
            cam.fieldOfView = 50f;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes("C:/Users/Вадим/Unity Projects/GOAT/pillar-side.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
            Debug.Log("VISTA_SHOT saved pillar-side.png");
            EditorApplication.Exit(0);
        }

        private static void Shoot(string cameraName, string outPath)
        {
            var camGo = GameObject.Find(cameraName);
            if (!camGo)
            {
                Debug.LogError("VISTA_SHOT: no camera " + cameraName);
                return;
            }
            var cam = camGo.GetComponent<Camera>();
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var clock = Stopwatch.StartNew();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            clock.Stop();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Debug.Log("VISTA_SHOT saved " + outPath + " renderMs=" + clock.ElapsedMilliseconds);
        }
    }
}
