using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainRouteProps
    {
        private static Material _markerMaterial;
        private static Material _iceMaterial;
        private static PhysicsMaterial _icePhysics;
        private static Mesh _landingSlabMesh;

        public static void Generate(Transform parent, MountainSettings settings)
        {
            IReadOnlyList<LandingPlatform> route = MountainGenerator.CurrentRoute;
            if (route == null || route.Count == 0) return;

            var root = new GameObject("Route Markers and Hazards");
            root.transform.SetParent(parent, false);

            for (int i = 0; i < route.Count; i++)
            {
                LandingPlatform platform = route[i];
                Vector3 next = FindNextPosition(route, platform);
                if (settings.EnablePlatformTypes && platform.Type == LandingPlatformType.Ice)
                    CreateIcePlate(root.transform, platform);
                if (settings.EnablePlatformTypes && platform.Type == LandingPlatformType.Crumbling)
                    CreateCrumblingPlate(root.transform, platform);
                if (platform.IsWindy && settings.EnableWind)
                    CreateWind(root.transform, platform, next, settings.Difficulty);
            }
        }

        private static Vector3 FindNextPosition(IReadOnlyList<LandingPlatform> route, LandingPlatform platform)
        {
            if (platform.IsBranch)
            {
                for (int i = 0; i < route.Count; i++)
                    if (!route[i].IsBranch && route[i].Index == platform.MergeIndex)
                        return route[i].Center;
            }
            else
            {
                for (int i = 0; i < route.Count; i++)
                    if (!route[i].IsBranch && route[i].Index == platform.Index + 1)
                        return route[i].Center;
            }
            return platform.Center + Vector3.forward;
        }

        private static void CreateIcePlate(Transform parent, LandingPlatform platform)
        {
            var plate = CreateLandingSlab(parent, "Ice Landing", platform,
                platform.Radius * 1.9f, .11f, IceMaterial(), false);
            plate.GetComponent<MeshCollider>().sharedMaterial = IcePhysics();
        }

        private static void CreateCrumblingPlate(Transform parent, LandingPlatform platform)
        {
            var plate = CreateLandingSlab(parent, "Crumbling Landing", platform,
                platform.Radius * 1.9f, .32f, MarkerMaterial(), true);
            var body = plate.AddComponent<Rigidbody>();
            body.mass = 28f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            plate.AddComponent<CrumblingPlatform>().Configure(
                (platform.IsBranch ? "BRANCH-" : "MAIN-") + platform.Index);
        }

        private static GameObject CreateLandingSlab(Transform parent, string name, LandingPlatform platform,
            float width, float height, Material material, bool dynamic)
        {
            var slab = new GameObject(name);
            slab.transform.SetParent(parent, false);
            slab.transform.position = platform.Center + Vector3.down * height * .5f;
            slab.transform.localScale = new Vector3(width, height, width);
            slab.AddComponent<MeshFilter>().sharedMesh = LandingSlabMesh();
            slab.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = slab.AddComponent<MeshCollider>();
            collider.sharedMesh = LandingSlabMesh();
            collider.convex = dynamic;
            return slab;
        }

        private static void CreateWind(Transform parent, LandingPlatform platform, Vector3 next, float difficulty)
        {
            Vector3 direction = Vector3.ProjectOnPlane(next - platform.Center, Vector3.up).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            Vector3 windDirection = Vector3.Cross(Vector3.up, direction).normalized;
            float distance = Vector3.Distance(platform.Center, next);
            var gust = new GameObject("Crosswind Jump Hazard");
            gust.transform.SetParent(parent, false);
            gust.transform.position = Vector3.Lerp(platform.Center, next, .5f) + Vector3.up * 1.1f;
            gust.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var trigger = gust.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.8f, 3.8f, Mathf.Max(2f, distance));
            gust.AddComponent<WindGust>().Configure(windDirection, Mathf.Lerp(6f, 13f, Mathf.Clamp01(difficulty)));
        }

        private static Mesh LandingSlabMesh()
        {
            if (_landingSlabMesh) return _landingSlabMesh;
            const int sides = 8;
            float[] heights = { 0f, .16f, .82f };
            float[] radii = { .43f, .5f, .42f };
            var rings = new Vector3[heights.Length, sides];
            for (int ring = 0; ring < heights.Length; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    float angle = (i / (float)sides + .0625f) * Mathf.PI * 2f;
                    float irregular = 1f + Mathf.Sin(i * 5.3f + ring * 1.7f) * .035f;
                    float radius = radii[ring] * irregular;
                    rings[ring, i] = new Vector3(Mathf.Cos(angle) * radius, heights[ring], Mathf.Sin(angle) * radius);
                }
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int ring = 0; ring < heights.Length - 1; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    AddTriangle(vertices, triangles, rings[ring, i], rings[ring + 1, i], rings[ring, next]);
                    AddTriangle(vertices, triangles, rings[ring, next], rings[ring + 1, i], rings[ring + 1, next]);
                }
            }
            Vector3 top = new Vector3(.015f, 1f, -.01f);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddTriangle(vertices, triangles, rings[2, i], top, rings[2, next]);
                AddTriangle(vertices, triangles, Vector3.zero, rings[0, next], rings[0, i]);
            }
            _landingSlabMesh = new Mesh { name = "Beveled Stone Landing" };
            _landingSlabMesh.SetVertices(vertices);
            _landingSlabMesh.SetTriangles(triangles, 0);
            _landingSlabMesh.RecalculateNormals();
            _landingSlabMesh.RecalculateBounds();
            return _landingSlabMesh;
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private static Material MarkerMaterial()
        {
            if (!_markerMaterial)
                _markerMaterial = MakeMaterial(new Color(.48f, .48f, .45f), .16f);
            return _markerMaterial;
        }

        private static Material IceMaterial()
        {
            if (!_iceMaterial)
            {
                _iceMaterial = MakeMaterial(new Color(.37f, .76f, .91f), .94f);
                _iceMaterial.SetFloat("_Metallic", .18f);
            }
            return _iceMaterial;
        }

        private static PhysicsMaterial IcePhysics()
        {
            if (_icePhysics) return _icePhysics;
            _icePhysics = new PhysicsMaterial("Ice")
            {
                staticFriction = .025f,
                dynamicFriction = .015f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return _icePhysics;
        }

        private static Material MakeMaterial(Color color, float smoothness)
        {
            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Glossiness", smoothness);
            return material;
        }
    }
}
