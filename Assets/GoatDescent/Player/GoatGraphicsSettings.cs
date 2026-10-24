using System;
using UnityEngine;

namespace GoatDescent
{
    public static class GoatGraphicsSettings
    {
        private const string PreferencesKey = "Goat.Graphics.V1";
        public const int MinFirstPersonFov = 70;
        public const int MaxFirstPersonFov = 120;
        public static readonly int[] FrameLimits = { -1, 30, 60, 90, 120, 144, 165, 240 };
        public static readonly int[] AntialiasingLevels = { 0, 2, 4, 8 };

        [Serializable]
        private sealed class Values
        {
            public int quality, shadows, antialiasing, textureLimit, frameLimit;
            public int brightness = 100, firstPersonFov = 78;
            public bool vSync, colorEffects = true;
        }

        private static Values current, defaults;
        private static bool dirty;
        public static int Quality { get { Load(); return current.quality; } }
        public static int Shadows { get { Load(); return current.shadows; } }
        public static int Antialiasing { get { Load(); return current.antialiasing; } }
        public static int TextureLimit { get { Load(); return current.textureLimit; } }
        public static int FrameLimit { get { Load(); return current.frameLimit; } }
        public static int Brightness { get { Load(); return current.brightness; } }
        public static int FirstPersonFov { get { Load(); return current.firstPersonFov; } }
        public static bool VSync { get { Load(); return current.vSync; } }
        public static bool ColorEffects { get { Load(); return current.colorEffects; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { current = defaults = null; dirty = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() { Load(); Apply(); }

        private static Values Capture()
        {
            return new Values
            {
                quality = QualitySettings.GetQualityLevel(),
                shadows = QualitySettings.shadows == ShadowQuality.Disable ? 0 : (int)QualitySettings.shadowResolution + 1,
                antialiasing = QualitySettings.antiAliasing,
                textureLimit = QualitySettings.globalTextureMipmapLimit,
                frameLimit = Array.IndexOf(FrameLimits, Application.targetFrameRate) >= 0 ? Application.targetFrameRate : -1,
                vSync = QualitySettings.vSyncCount > 0
            };
        }

        private static Values Copy(Values source) => JsonUtility.FromJson<Values>(JsonUtility.ToJson(source));

        private static void Load()
        {
            if (current != null) return;
            defaults = Capture();
            current = Copy(defaults);
            string saved = PlayerPrefs.GetString(PreferencesKey, "");
            if (saved.Length > 0)
            {
                try { JsonUtility.FromJsonOverwrite(saved, current); }
                catch (ArgumentException) { current = Copy(defaults); }
            }
            current.quality = Mathf.Clamp(current.quality, 0, QualitySettings.names.Length - 1);
            current.shadows = Mathf.Clamp(current.shadows, 0, 4);
            current.brightness = Mathf.Clamp(current.brightness, 50, 150);
            current.firstPersonFov = Mathf.Clamp(current.firstPersonFov, MinFirstPersonFov, MaxFirstPersonFov);
            current.textureLimit = Mathf.Clamp(current.textureLimit, 0, 2);
            if (Array.IndexOf(AntialiasingLevels, current.antialiasing) < 0) current.antialiasing = defaults.antialiasing;
            if (Array.IndexOf(FrameLimits, current.frameLimit) < 0) current.frameLimit = -1;
        }

        public static void Apply()
        {
            Load();
            if (QualitySettings.GetQualityLevel() != current.quality)
                QualitySettings.SetQualityLevel(current.quality, true);
            if ((int)QualitySettings.skinWeights < 4) QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.shadows = current.shadows == 0 ? ShadowQuality.Disable : ShadowQuality.All;
            if (current.shadows > 0) QualitySettings.shadowResolution = (ShadowResolution)(current.shadows - 1);
            QualitySettings.antiAliasing = current.antialiasing;
            QualitySettings.globalTextureMipmapLimit = current.textureLimit;
            QualitySettings.vSyncCount = current.vSync ? 1 : 0;
            Application.targetFrameRate = current.frameLimit;
        }

        private static void Remember()
        {
            PlayerPrefs.SetString(PreferencesKey, JsonUtility.ToJson(current));
            dirty = true;
        }

        public static void Flush()
        {
            if (!dirty) return;
            PlayerPrefs.Save();
            dirty = false;
        }

        public static void SetQuality(int value)
        {
            Load();
            current.quality = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(current.quality, true);
            current.shadows = QualitySettings.shadows == ShadowQuality.Disable ? 0 : (int)QualitySettings.shadowResolution + 1;
            current.antialiasing = QualitySettings.antiAliasing;
            current.textureLimit = QualitySettings.globalTextureMipmapLimit;
            Apply(); Remember();
        }

        public static void SetShadows(int value) { Load(); current.shadows = Mathf.Clamp(value, 0, 4); Apply(); Remember(); }
        public static void SetAntialiasing(int value)
        {
            Load();
            if (Array.IndexOf(AntialiasingLevels, value) < 0) return;
            current.antialiasing = value; Apply(); Remember();
        }
        public static void SetTextureLimit(int value) { Load(); current.textureLimit = Mathf.Clamp(value, 0, 2); Apply(); Remember(); }
        public static void SetFrameLimit(int value)
        {
            Load();
            if (Array.IndexOf(FrameLimits, value) < 0) return;
            current.frameLimit = value; Apply(); Remember();
        }
        public static void SetVSync(bool value) { Load(); current.vSync = value; Apply(); Remember(); }
        public static void SetColorEffects(bool value) { Load(); current.colorEffects = value; Remember(); }
        public static void SetBrightness(int value) { Load(); current.brightness = Mathf.Clamp(value, 50, 150); Remember(); }
        public static void SetFirstPersonFov(int value)
        {
            Load(); current.firstPersonFov = Mathf.Clamp(value, MinFirstPersonFov, MaxFirstPersonFov); Remember();
        }
        public static void RestoreDefaults() { Load(); current = Copy(defaults); Apply(); Remember(); Flush(); }
    }
}
