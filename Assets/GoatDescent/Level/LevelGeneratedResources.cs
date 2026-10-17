using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.Levels
{
    [DisallowMultipleComponent]
    public sealed class LevelGeneratedResources : MonoBehaviour
    {
        [SerializeField, HideInInspector] private List<Object> owned = new List<Object>();
        public LevelRoute Route { get; private set; }
        public Material GeneratedSky { get; set; }
        private Material previousSky;
        private Light previousSun;
        private Light[] previousLights;
        private bool[] lightEnabled;
        private bool previousFog;
        private FogMode previousFogMode;
        private float previousDensity;
        private Color previousFogColor;
        private UnityEngine.Rendering.AmbientMode previousAmbientMode;
        private Color previousSkyColor, previousEquatorColor, previousGroundColor, previousAmbient;
        public void CaptureEnvironment()
        {
            previousSky = RenderSettings.skybox; previousSun = RenderSettings.sun;
            previousFog = RenderSettings.fog; previousFogMode = RenderSettings.fogMode;
            previousDensity = RenderSettings.fogDensity; previousFogColor = RenderSettings.fogColor;
            previousAmbientMode = RenderSettings.ambientMode; previousAmbient = RenderSettings.ambientLight;
            previousSkyColor = RenderSettings.ambientSkyColor; previousEquatorColor = RenderSettings.ambientEquatorColor;
            previousGroundColor = RenderSettings.ambientGroundColor;
            previousLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            lightEnabled = new bool[previousLights.Length];
            for (int i = 0; i < previousLights.Length; i++)
            {
                lightEnabled[i] = previousLights[i].enabled;
                if (previousLights[i].type == LightType.Directional) previousLights[i].enabled = false;
            }
        }
        public void Configure(LevelRoute route) => Route = route;
        public T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private void OnDestroy()
        {
            if (GeneratedSky && RenderSettings.skybox == GeneratedSky)
            {
                RenderSettings.skybox = previousSky; RenderSettings.sun = previousSun;
                RenderSettings.fog = previousFog; RenderSettings.fogMode = previousFogMode;
                RenderSettings.fogDensity = previousDensity; RenderSettings.fogColor = previousFogColor;
                RenderSettings.ambientMode = previousAmbientMode; RenderSettings.ambientLight = previousAmbient;
                RenderSettings.ambientSkyColor = previousSkyColor; RenderSettings.ambientEquatorColor = previousEquatorColor;
                RenderSettings.ambientGroundColor = previousGroundColor;
                for (int i = 0; i < previousLights.Length; i++)
                    if (previousLights[i]) previousLights[i].enabled = lightEnabled[i];
            }
            foreach (Object value in owned)
                if (value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
            owned.Clear();
        }
    }
}
