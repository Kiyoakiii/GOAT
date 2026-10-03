using UnityEngine;

namespace GoatDescent
{
    public struct LandingPlatform
    {
        public Vector3 Center;
        public float Radius;
        public float Progress;
        public int Index;
        public int MergeIndex;
        public LandingPlatformType Type;
        public bool IsBranch;
        public bool IsWindy;
        public bool IsCheckpoint;
    }
}