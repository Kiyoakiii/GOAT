using System;
using UnityEngine;

namespace GoatDescent
{
    [Serializable]
    public sealed class MountainSettings
    {
        [Header("Size")]
        public float Radius = 55f;
        public float SummitY = 130f;
        public float ValleyY = -6f;

        [Header("Mountain")]
        public int CouloirCount = 4;
        public int StrataCount = 8;
        public float NoiseAmplitude = 3.2f;
        public float WarpAmplitude = 8f;
        public float Asymmetry = 6f;
        public float PeakOffset = 8f;
        public float Stretch = .4f;
        public int Seed = 7;

        [Header("Realism")]
        public float RidgeFraction = 0.16f;
        public float ErosionStrength = 1f;
        public float TalusStrength = 1f;
        public float TalusAngle = 38f;

        [Header("Landing")]
        public bool EnableRoute = true;
        public int PlatformCount = 24;
        public float PlatformStep = 5f;
        public float PlatformSize = 4f;
        public float PlatformGap = .8f;
        public float Difficulty = .5f;
        public bool EnableBranches = true;
        public bool EnablePlatformTypes = true;
        public bool EnableWind = true;
        public bool CheckpointRespawn = true;

        [Header("Decor")]
        public bool EnableDecor = true;
        public int BushCount = 260;
        public int RockCount = 90;
        public int TreeCount = 220;
        public int FlowerCount = 300;
        public int GrassTuftCount = 700;

        [Header("Resolution")]
        public int Grid = 200;

        public const float CouloirDepth = 24f;
        public const float CouloirWidth = 0.2f;
        public const float StrataSharpness = 2.5f;
        public const float StrataWaviness = 0.4f;
        public const float StrataStrength = 0.4f;
        public const float NoiseScale = 0.03f;
        public const float WarpScale = 0.012f;
    }
}
