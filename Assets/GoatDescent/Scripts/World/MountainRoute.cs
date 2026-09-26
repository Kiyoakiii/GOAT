using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public struct LandingPlatform
    {
        public Vector3 Center;
    }

    public static class MountainRoute
    {
        public static List<LandingPlatform> Generate(MountainSettings s)
        {
            var platforms = new List<LandingPlatform>();
            int n = s.PlatformCount;
            if (!s.EnableRoute || n < 2) return platforms;

            float r0 = s.Radius * .08f;
            float r1 = s.Radius * .86f;
            float dr = (r1 - r0) / (n - 1);
            float heading = (s.Seed % 360) * Mathf.Deg2Rad;
            int dir = s.Seed % 2 == 1 ? -1 : 1;

            float totalDrop = s.SummitY - s.ValleyY;
            float dropPer = totalDrop / (n - 1);

            for (int i = 0; i < n; i++)
            {
                float r = r0 + dr * i;
                float y = s.SummitY - dropPer * i;
                float x = Mathf.Cos(heading) * r;
                float z = Mathf.Sin(heading) * r;

                float sway = Mathf.Sin(i * .9f + s.Seed) * s.Radius * .06f;
                Vector3 radial = new Vector3(x, 0f, z).normalized;
                Vector3 tangent = new Vector3(-z, 0f, x).normalized;
                x += tangent.x * sway;
                z += tangent.z * sway;

                platforms.Add(new LandingPlatform { Center = new Vector3(x, y, z) });

                float baseTheta = Mathf.Sqrt(Mathf.Max(s.PlatformStep * s.PlatformStep - dr * dr, .5f)) / Mathf.Max(r, 1f);
                float wander = (Mathf.PerlinNoise(i * .71f + s.Seed, s.Seed * .13f) - .5f) * 2f * .8f;
                heading += baseTheta * (1f + wander) * dir;
            }
            return platforms;
        }
    }
}