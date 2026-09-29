using UnityEngine;

namespace GoatDescent
{
    public static class GoatSurfaceZones
    {
        public const float StandMaxAngle = 62f;
        public const float HoofMaxAngle = 73f;
        public const float GripMaxAngle = 86f;

        public static bool CanStand(float slopeAngle) => slopeAngle <= StandMaxAngle;
        public static bool CanGrip(float slopeAngle) => slopeAngle <= GripMaxAngle;
        public static bool WillFall(float slopeAngle) => slopeAngle > GripMaxAngle;

        public static bool NormalSupportsHooves(Vector3 normal) => normal.y > Mathf.Cos(HoofMaxAngle * Mathf.Deg2Rad);
        public static bool NormalSupportsGrip(Vector3 normal) => normal.y > Mathf.Cos(GripMaxAngle * Mathf.Deg2Rad);
        public static bool NormalIsWallLike(Vector3 normal) => normal.y < Mathf.Cos(StandMaxAngle * Mathf.Deg2Rad);
    }
}
