using UnityEngine;

namespace GoatDescent
{
    /// <summary>Twenty percent grade means 0.2 m of depth per metre of height.</summary>
    public static class CliffSlope
    {
        public const float Grade = .20f;
        public const float LedgeDepth = .21f;
        public static float AngleDegrees => Mathf.Atan(Grade) * Mathf.Rad2Deg;

        // Front face of the rotated 0.5 m thick test wall at a world-space height.
        public static float WallZ(float y) => Grade * (y - 9f) - .25f * Mathf.Sqrt(1f + Grade * Grade);
        public static float LedgeFrontZ(float y) => WallZ(y) - LedgeDepth;
        public static float StandingCenterZ(float bodyY) => WallZ(bodyY - .80f) - .13f;
    }
}
