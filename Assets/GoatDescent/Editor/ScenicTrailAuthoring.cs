using System;
using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class ScenicTrailAuthoring
    {
        public static Vector3[] Build(string scene, string folder, Material stone, Dictionary<int,float> repairs = null, int attempt = 0)
        {
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (transform && transform.name.StartsWith("Прыжковые места")) UnityEngine.Object.DestroyImmediate(transform.gameObject);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string label = renderer.name.ToLowerInvariant();
                if (label.Contains("полотно тропы") || label.Contains("козья тропа по своду") || label.Contains("прыжковые"))
                {
                    renderer.enabled = false;
                    if (renderer.TryGetComponent<Collider>(out var collider)) collider.enabled = false;
                }
            }
            Physics.SyncTransforms();
            Vector3[] source = scene switch
            {
                "TerracePeak" => TerracePeakAuthoring.PlayRoute(),
                "SkyBridge" => ScenicRoutePlans.SkyBridge(),
                "FrozenRift" => ScenicRoutePlans.FrozenRift(),
                "EmeraldBasin" => ScenicRoutePlans.EmeraldBasin(),
                "DesertArches" => DesertArchesAuthoring.ScenicRoute(),
                _ => throw new ArgumentException(scene)
            };
            for (int pass = 0; pass < 5; pass++)
            {
                var smooth = (Vector3[])source.Clone();
                for (int i = 1; i < source.Length-1; i++) smooth[i] = source[i-1]*.25f + source[i]*.5f + source[i+1]*.25f;
                source = smooth;
            }
            var samples = new List<Vector3>();
            foreach (var p in source)
            {
                if (samples.Count > 0 && ScenicRouteMath.Horizontal(samples[samples.Count - 1], p) < .25f) continue;
                Vector3 point = p;
                if (Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out var hit, 35f, ~0, QueryTriggerInteraction.Ignore))
                    point.y = Mathf.Max(point.y, hit.point.y);
                point.y += .18f;
                samples.Add(point);
            }
            if (scene == "DesertArches" && samples.Count > 50) samples.RemoveRange(samples.Count - 45,45);
            var ground = samples.ToArray();
            var route = ScenicRouteMath.RaiseToGrade(ground, 32f);
            if (scene == "DesertArches")
            {
                float startHeight = route[12].y;
                for (int i = 0; i <= 12; i++) startHeight = Mathf.Max(startHeight, route[i].y);
                for (int i = 0; i <= 12; i++) route[i].y = startHeight;
            }
            repairs ??= new Dictionary<int,float>();
            foreach (var repair in repairs) if (repair.Key < route.Length) route[repair.Key].y = Mathf.Max(route[repair.Key].y,repair.Value);
            route = ScenicRouteMath.RaiseToGrade(route,32f);
            for (int pass = 0; pass < 16; pass++)
            {
                bool changed = false;
                for (int i = 0; i < route.Length; i++)
                {
                    if (!Physics.Raycast(route[i] + Vector3.up * .3f, Vector3.up, out var ceiling, 1.6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (Physics.Raycast(route[i] + Vector3.up * 8f, Vector3.down, out var top, 8f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        route[i].y = Mathf.Max(route[i].y, top.point.y + .2f);
                        changed = true;
                    }
                    else
                    {
                        Vector3 outward = new Vector3(route[i].x, 0f, route[i].z).normalized;
                        Vector3 offset = outward * 12f;
                        for (int k = Mathf.Max(0,i-12); k <= Mathf.Min(route.Length-1,i+12); k++)
                            route[k] += offset * Mathf.SmoothStep(0f,1f,1f - Mathf.Abs(k-i)/13f);
                        changed = true;
                    }
                }
                if (!changed) break;
                route = ScenicRouteMath.RaiseToGrade(route, 32f);
            }
            int highestLift = 0;
            for (int i = 1; i < route.Length; i++)
                if (route[i].y-ground[i].y > route[highestLift].y-ground[highestLift].y) highestLift = i;
            Debug.Log($"SCENIC_LIFT {scene} index={highestLift} ground={ground[highestLift]} route={route[highestLift]} before={ground[Mathf.Max(0,highestLift-1)]} after={ground[Mathf.Min(ground.Length-1,highestLift+1)]}");
            var checkpointIndices = new List<int> { 0 };
            float length = 0f, sinceRest = 0f, maxRaise = 0f;
            for (int i = 1; i < route.Length; i++)
            {
                float distance = Vector3.Distance(route[i], route[i - 1]);
                length += distance;
                sinceRest += distance;
                maxRaise = Mathf.Max(maxRaise, route[i].y - ground[i].y);
                if (sinceRest > 320f && ScenicRouteMath.Grade(route[i - 1], route[i]) < 18f)
                {
                    checkpointIndices.Add(i);
                    sinceRest = 0f;
                }
            }
            checkpointIndices.Add(route.Length - 1);
            var root = new GameObject("Маршрут козы — старт, привалы, финиш");
            var course = root.AddComponent<GoatDescent.ScenicGoatCourse>();
            var earth = new Material(stone) { name = "ScenicTrail" };
            earth.SetColor("_Color", scene == "FrozenRift" ? new Color(.49f,.57f,.61f) : new Color(.49f,.37f,.23f));
            earth.SetFloat("_Cover", 0f);
            earth.SetFloat("_Scale", 5f);
            earth = ScenicWorldAuthoring.Save(earth, folder + "/ScenicTrail.mat");
            course.Configure(route, checkpointIndices.ToArray(), earth);
            var deck = new CrownRockMesh();
            var skirts = new CrownRockMesh();
            var cairns = new CrownRockMesh();
            var random = new System.Random(721);
            var left = new Vector3[route.Length];
            var right = new Vector3[route.Length];
            var lowerLeft = new Vector3[route.Length];
            var lowerRight = new Vector3[route.Length];
            int rest = 0;
            for (int i = 0; i < route.Length; i++)
            {
                Vector3 tangent = route[Mathf.Min(i + 1, route.Length - 1)] - route[Mathf.Max(0, i - 1)];
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                while (rest < checkpointIndices.Count - 1 && i > checkpointIndices[rest] + 4) rest++;
                bool clearing = Mathf.Abs(i - checkpointIndices[rest]) < 4;
                float half = clearing ? 4.5f : Mathf.Lerp(1.9f, 3f, .5f + .5f * Mathf.Sin(i * .031f));
                left[i] = route[i] - side * half;
                right[i] = route[i] + side * half;
                float bottom = Mathf.Min(ground[i].y - 4f, route[i].y - 5f);
                lowerLeft[i] = left[i] - side * 3f; lowerLeft[i].y = bottom;
                lowerRight[i] = right[i] + side * 3f; lowerRight[i].y = bottom;
                if (i == checkpointIndices[rest])
                {
                    Vector3 marker = route[i] + side * (half + 1.1f);
                    for (int tier = 0; tier < 3; tier++)
                        cairns.RockBlock(marker, Vector2.one * (1.1f - tier * .23f), marker.y + .65f + tier * .58f,
                            marker.y + tier * .58f, random);
                }
                if (i % 7 == 0 && !clearing)
                {
                    Vector3 pebble = route[i] + side * ((i % 2 == 0 ? 1f : -1f) * (half + .35f));
                    float size = Mathf.Lerp(.18f,.52f,(float)random.NextDouble());
                    cairns.RockBlock(pebble, new Vector2(size,size * .7f), pebble.y + size * .6f, pebble.y - size, random);
                }
                if (i == 0) continue;
                Quad(deck, left[i - 1], left[i], right[i], right[i - 1]);
                Quad(skirts, left[i - 1], lowerLeft[i - 1], lowerLeft[i], left[i]);
                Quad(skirts, right[i], lowerRight[i], lowerRight[i - 1], right[i - 1]);
            }
            Add(root.transform, "Проходимая каменная тропа", deck.Build("ScenicTrail"), earth, folder + "/ScenicTrail.asset", true);
            Add(root.transform, "Скальная опора тропы", skirts.Build("ScenicTrailSupport"), stone, folder + "/ScenicTrailSupport.asset", false);
            Add(root.transform, "Каменные туры привалов", cairns.Build("ScenicCairns"), stone, folder + "/ScenicCairns.asset", false);
            Physics.SyncTransforms();
            int unsupported = 0, obstructed = 0;
            float maxGrade = 0f;
            for (int i = 0; i < route.Length; i++)
            {
                if (!Physics.Raycast(route[i] + Vector3.up * .7f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore)) unsupported++;
                if (Physics.Raycast(route[i] + Vector3.up * .3f, Vector3.up, out var obstacle, 1.6f, ~0, QueryTriggerInteraction.Ignore)) { obstructed++; if (obstacle.collider.name == "Проходимая каменная тропа") repairs[i] = obstacle.point.y + .35f; Debug.LogWarning($"SCENIC_BLOCKED {scene} index={i} collider={obstacle.collider.name} point={route[i]}"); }
                if (i > 0) maxGrade = Mathf.Max(maxGrade, ScenicRouteMath.Grade(route[i - 1], route[i]));
            }
            Debug.Log($"SCENIC_ROUTE {scene} points={route.Length} length={length:F0}m checkpoints={checkpointIndices.Count} maxGrade={maxGrade:F1} maxRaised={maxRaise:F1} unsupported={unsupported} overhead={obstructed}");
            if (obstructed > 0 && repairs.Count > 0 && attempt < 6)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return Build(scene,folder,stone,repairs,attempt+1);
            }
            if (unsupported > 0 || obstructed > 0) throw new InvalidOperationException($"Route clearance failed: {scene}, support={unsupported}, overhead={obstructed}");
            return route;
        }

        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            mesh.Triangle(a, b, c, Color.white, Color.white, Color.white);
            mesh.Triangle(a, c, d, Color.white, Color.white, Color.white);
        }

        internal static void Add(Transform root, string name, Mesh mesh, Material material, string path, bool collider)
        {
            mesh = ScenicWorldAuthoring.Save(mesh, path);
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
        }
    }
}
