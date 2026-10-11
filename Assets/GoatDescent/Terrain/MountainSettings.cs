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

        [Header("Style — Atmosphere")]
        public Color FogColor = ValheimFogColor;
        public float FogStrength = 1.8f;
        public Color SunColor = ValheimSunColor;
        public float SunIntensity = 1.15f;
        public Color AmbientColor = ValheimAmbientColor;
        public Color SkyZenith = ValheimSkyZenith;
        public Color SkyHorizon = ValheimSkyHorizon;

        [Header("Style — Surfaces")]
        public int TextureSize = 64;
        public float SnowStart = 0.72f;
        public float SnowFade = 0.16f;
        public float GrassEnd = 0.42f;
        public float Steepness = 0.35f;
        public float Tiling = 0.025f;

        [Header("Style — Post Process")]
        public float Exposure = 1.05f;
        public float Saturation = 1.25f;
        public float Contrast = 1.12f;
        public float Warmth = 0.3f;
        public float Vignette = 0.35f;

        public static readonly Color ValheimFogColor = new Color(.73f, .79f, .80f);
        public static readonly Color ValheimSunColor = new Color(1f, .92f, .72f);
        public static readonly Color ValheimAmbientColor = new Color(.55f, .60f, .68f);
        public static readonly Color ValheimSkyZenith = new Color(.30f, .50f, .78f);
        public static readonly Color ValheimSkyHorizon = new Color(.80f, .89f, .93f);

        public static readonly Color ClassicFogColor = new Color(.78f, .85f, .91f);
        public static readonly Color ClassicSunColor = new Color(1f, .96f, .86f);
        public static readonly Color ClassicAmbientColor = new Color(.70f, .73f, .76f);
        public static readonly Color ClassicSkyZenith = new Color(.22f, .42f, .72f);
        public static readonly Color ClassicSkyHorizon = new Color(.75f, .88f, .95f);

        public void ApplyValheimStyle()
        {
            FogColor = ValheimFogColor;
            FogStrength = 1.8f;
            SunColor = ValheimSunColor;
            SunIntensity = 1.15f;
            AmbientColor = ValheimAmbientColor;
            SkyZenith = ValheimSkyZenith;
            SkyHorizon = ValheimSkyHorizon;
            TextureSize = 64;
            SnowStart = 0.72f;
            SnowFade = 0.16f;
            GrassEnd = 0.42f;
            Steepness = 0.35f;
            Tiling = 0.025f;
            Exposure = 1.05f;
            Saturation = 1.25f;
            Contrast = 1.12f;
            Warmth = 0.3f;
            Vignette = 0.35f;
        }

        public void ApplyClassicStyle()
        {
            FogColor = ClassicFogColor;
            FogStrength = 1f;
            SunColor = ClassicSunColor;
            SunIntensity = 1.2f;
            AmbientColor = ClassicAmbientColor;
            SkyZenith = ClassicSkyZenith;
            SkyHorizon = ClassicSkyHorizon;
            TextureSize = 0;
            SnowStart = 0.86f;
            SnowFade = 0.12f;
            GrassEnd = 0.42f;
            Steepness = 0.35f;
            Tiling = 0.02f;
            Exposure = 1f;
            Saturation = 1.15f;
            Contrast = 1.1f;
            Warmth = 0.25f;
            Vignette = 0f;
        }

        public const float CouloirDepth = 24f;
        public const float CouloirWidth = 0.2f;
        public const float StrataSharpness = 2.5f;
        public const float StrataWaviness = 0.4f;
        public const float StrataStrength = 0.4f;
        public const float NoiseScale = 0.03f;
        public const float WarpScale = 0.012f;
    }
}
