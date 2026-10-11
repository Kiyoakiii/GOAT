using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainGenerator
    {
        public static IReadOnlyList<LandingPlatform> CurrentRoute => RouteQueries.CurrentRoute;

        public static int MainRouteCount => RouteQueries.MainRouteCount;

        public static void PrepareRoute(MountainSettings s)
        {
            var route = MountainRoute.Generate(s);
            RouteQueries.SetRoute(route);
            TerrainHeightField.EnsureBoulders(s.Seed);

            int n = route.Count;
            var radii = new float[n];
            var heights = new float[n];
            for (int i = 0; i < n; i++)
            {
                radii[i] = Mathf.Sqrt(route[i].Center.x * route[i].Center.x + route[i].Center.z * route[i].Center.z);
                heights[i] = route[i].Center.y;
            }
            TerrainHeightField.SetProfile(radii, heights);

            TerrainHeightField.BakeHeightField(s);
            MountainRoute.AdjustForVisibility(s, route, (x, z) => TerrainHeightField.RawHeight(s, x, z));
            MountainRoute.AddBranches(s, route);

            for (int i = 0; i < n; i++)
            {
                Vector3 p = route[i].Center;
                float ground = TerrainHeightField.RawHeight(s, p.x, p.z);
                LandingPlatform platform = route[i];
                platform.Center = new Vector3(p.x, ground + MountainRoute.PlatformHeightOffset(platform.Type), p.z);
                route[i] = platform;
            }

            for (int i = n; i < route.Count; i++)
            {
                LandingPlatform platform = route[i];
                float ground = TerrainHeightField.RawHeight(s, platform.Center.x, platform.Center.z);
                platform.Center = new Vector3(platform.Center.x, ground + MountainRoute.PlatformHeightOffset(platform.Type), platform.Center.z);
                route[i] = platform;
            }
        }

        public static Vector3 SpawnPoint(MountainSettings s)
        {
            if (CurrentRoute != null && CurrentRoute.Count > 0)
                return new Vector3(CurrentRoute[0].Center.x, CurrentRoute[0].Center.y + 1.5f, CurrentRoute[0].Center.z);
            return new Vector3(0f, s.SummitY + 1.5f, 0f);
        }

        public static float HeightAt(MountainSettings s, float x, float z) => TerrainHeightField.HeightAt(s, x, z);

        public static float RawHeight(MountainSettings s, float x, float z) => TerrainHeightField.RawHeight(s, x, z);

        public static Vector3 NormalAt(MountainSettings s, float x, float z) => TerrainHeightField.NormalAt(s, x, z);

        public static float PlatformMaskAt(MountainSettings s, float x, float z) => RouteQueries.PlatformMaskAt(s, x, z);

        public static bool TryGetPlatformAt(float x, float z, out LandingPlatform platform) => RouteQueries.TryGetPlatformAt(x, z, out platform);

        public static float DistanceToRoute(MountainSettings s, float x, float z) => RouteQueries.DistanceToRoute(s, x, z);

        public static bool IsClearOfRoute(MountainSettings s, float x, float z, float footprintRadius) => RouteQueries.IsClearOfRoute(s, x, z, footprintRadius);

        public static Mesh Build(MountainSettings s) => TerrainMeshBuilder.Build(s);
    }
}