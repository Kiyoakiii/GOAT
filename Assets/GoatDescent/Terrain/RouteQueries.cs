using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class RouteQueries
    {
        private static List<LandingPlatform> _route;

        public static IReadOnlyList<LandingPlatform> CurrentRoute => _route;

        public static int MainRouteCount
        {
            get
            {
                if (_route == null) return 0;
                int count = 0;
                for (int i = 0; i < _route.Count; i++)
                    if (!_route[i].IsBranch) count++;
                return count;
            }
        }

        internal static void SetRoute(List<LandingPlatform> route) => _route = route;

        internal static void FindNearestPlatform(float x, float z, out float dist, out LandingPlatform platform)
        {
            dist = float.MaxValue;
            platform = default;
            if (_route == null || _route.Count == 0) return;
            for (int i = 0; i < _route.Count; i++)
            {
                float dx = x - _route[i].Center.x;
                float dz = z - _route[i].Center.z;
                float d = dx * dx + dz * dz;
                if (d < dist)
                {
                    dist = d;
                    platform = _route[i];
                }
            }
            dist = Mathf.Sqrt(dist);
        }

        public static float PlatformMaskAt(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count < 2) return 0f;
            FindNearestPlatform(x, z, out float best, out LandingPlatform platform);
            float platformRadius = Mathf.Max(.5f, platform.Radius);
            if (best <= platformRadius * .8f) return 1f;
            if (best >= platformRadius) return 0f;
            return Mathf.InverseLerp(platformRadius, platformRadius * .8f, best);
        }

        public static bool TryGetPlatformAt(float x, float z, out LandingPlatform platform)
        {
            FindNearestPlatform(x, z, out float distance, out platform);
            return platform.Radius > 0f && distance <= platform.Radius;
        }

        public static float DistanceToRoute(MountainSettings s, float x, float z)
        {
            if (_route == null || _route.Count == 0) return float.MaxValue;
            FindNearestPlatform(x, z, out float distance, out _);
            return distance;
        }

        public static bool IsClearOfRoute(MountainSettings s, float x, float z, float footprintRadius)
        {
            if (_route == null || _route.Count == 0) return true;
            FindNearestPlatform(x, z, out float distance, out LandingPlatform platform);
            float radius = Mathf.Max(.5f, platform.Radius);
            return distance > radius + s.PlatformGap + footprintRadius + .5f;
        }
    }
}