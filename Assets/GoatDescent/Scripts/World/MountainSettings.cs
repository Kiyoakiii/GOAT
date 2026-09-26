using System;
using UnityEngine;

namespace GoatDescent
{
    [Serializable]
    public sealed class MountainSettings
    {
        [Header("Size")]
        public float Radius = 150f;
        public float SummitY = 280f;
        public float ValleyY = -20f;

        [Header("Mountain")]
        public int CouloirCount = 6;
        public int StrataCount = 12;
        public float NoiseAmplitude = 3f;
        public int Seed = 7;

        [Header("Landing")]
        public bool EnableRoute = true;
        public int PlatformCount = 30;
        public float PlatformStep = 5f;
        public float PlatformSize = 5f;

        [Header("Decor")]
        public bool EnableDecor = true;
        public int BushCount = 70;
        public int RockCount = 50;
        public int TreeCount = 60;

        [Header("Resolution")]
        public int Grid = 256;

        public const float WarpScale = 0.012f;
        public const float CouloirDepth = 26f;
        public const float CouloirWidth = 0.18f;
        public const float StrataSharpness = 6f;
        public const float StrataWaviness = 0.5f;
        public const float StrataStrength = 0.9f;
        public const float NoiseScale = 0.045f;
    }
}