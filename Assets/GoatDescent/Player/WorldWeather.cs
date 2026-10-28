using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public enum WeatherPreset : byte { Clear, Rain, Wind, Storm }

    /// <summary>Host-owned weather. Only daylight is dimmed; local lights and emission stay intact.</summary>
    [DefaultExecutionOrder(-60)]
    public sealed class WorldWeather : MonoBehaviour
    {
        public static WorldWeather Current { get; private set; }
        public WeatherPreset Preset { get; private set; }
        public float Rain { get; private set; }
        public float Wetness { get; private set; }
        public float Wind { get; private set; }
        public float Clock { get; private set; }
        public Vector3 WindDirection { get; private set; } = new Vector3(.8f, 0, .6f);
        public float Gust => .72f + .18f * Mathf.Sin(Clock * .73f) + .1f * Mathf.Sin(Clock * 1.91f + 1.2f);
        public Vector3 WindVelocity => WindDirection * (Wind * Gust * 1.5f);
        private float rainTarget, windTarget;
        private Vector3 directionTarget = new Vector3(.8f, 0, .6f);
        private bool lightingReady, oldFog;
        private float oldAmbientIntensity, oldReflection, oldFogDensity, oldFogStart, oldFogEnd;
        private FogMode oldFogMode;
        private Color oldAmbient, oldSky, oldEquator, oldGround, oldFogColor;
        private Material originalSkybox, weatherSkybox;
        private readonly Dictionary<string, Color> skyColors = new Dictionary<string, Color>();
        private readonly Dictionary<Light, float> sunlight = new Dictionary<Light, float>();
        private float nextLightScan, skyExposure;
        private static readonly RaycastHit[] shelterHits = new RaycastHit[32];

        public static WorldWeather Ensure()
        {
            if (!Current) Current = FindFirstObjectByType<WorldWeather>();
            if (!Current) Current = new GameObject("World weather — F1/F2/F3/F4").AddComponent<WorldWeather>();
            return Current;
        }

        private void Awake()
        {
            if (Current && Current != this) { Destroy(gameObject); return; }
            Current = this;
        }

        private IEnumerator Start()
        {
            // Let the procedural level finish assigning its sky and sun first.
            yield return null;
            CaptureLighting();
            gameObject.AddComponent<WorldWindRibbons>();
            gameObject.AddComponent<WeatherRain>();
        }

        public void SetPreset(WeatherPreset preset, bool instant = false)
        {
            if (!MountainAuthority.IsHost) return;
            Preset = preset;
            rainTarget = preset == WeatherPreset.Rain || preset == WeatherPreset.Storm ? 1f : 0f;
            windTarget = preset == WeatherPreset.Wind || preset == WeatherPreset.Storm ? 1f : 0f;
            if (instant) { Rain = rainTarget; Wind = windTarget; Wetness = Rain; }
        }

        public void SetWindDirection(Vector3 direction, bool instant = false)
        {
            if (!MountainAuthority.IsHost) return;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude < .001f) return;
            directionTarget = direction.normalized;
            if (instant) WindDirection = directionTarget;
        }

        private void Update()
        {
            Clock += Time.deltaTime;
            if (MountainAuthority.IsHost)
            {
                if (!GoatSettingsMenu.BlocksInput && !GoatSkinMenu.IsOpen)
                {
                    if (Input.GetKeyDown(KeyCode.F1)) SetPreset(WeatherPreset.Clear);
                    if (Input.GetKeyDown(KeyCode.F2)) SetPreset(WeatherPreset.Rain);
                    if (Input.GetKeyDown(KeyCode.F3))
                    {
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            SetWindDirection(Quaternion.Euler(0, 45, 0) * directionTarget);
                        else SetPreset(WeatherPreset.Wind);
                    }
                    if (Input.GetKeyDown(KeyCode.F4)) SetPreset(WeatherPreset.Storm);
                }
                Rain = Mathf.MoveTowards(Rain, rainTarget, Time.deltaTime * .2f);
                Wind = Mathf.MoveTowards(Wind, windTarget, Time.deltaTime * .22f);
                Wetness = Mathf.MoveTowards(Wetness, Rain, Time.deltaTime * (Rain > Wetness ? .12f : .035f));
                WindDirection = Vector3.RotateTowards(WindDirection, directionTarget, Time.deltaTime * .6f, 0).normalized;
            }
            if (lightingReady) UpdateLighting();
        }

        public WeatherState Capture() => new WeatherState
        {
            active = true, preset = (byte)Preset, rain = Rain, wetness = Wetness,
            wind = Wind, direction = WindDirection, clock = Clock
        };

        public void Apply(WeatherState state)
        {
            if (MountainAuthority.IsHost || !state.active) return;
            Preset = (WeatherPreset)state.preset;
            Rain = rainTarget = Mathf.Clamp01(state.rain); Wetness = Mathf.Clamp01(state.wetness);
            Wind = windTarget = Mathf.Clamp01(state.wind); Clock = state.clock;
            if (state.direction.sqrMagnitude > .1f) WindDirection = directionTarget = state.direction.normalized;
        }

        public static bool ClearAir(Vector3 position, Vector3 direction, float distance)
        {
            int count = Physics.RaycastNonAlloc(position, direction, shelterHits, distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == shelterHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var collider = shelterHits[i].collider;
                if (!collider || collider.GetComponentInParent<GoatController>()
                    || collider.GetComponentInParent<GoatPhysicsPart>()) continue;
                return false;
            }
            return true;
        }

        private void CaptureLighting()
        {
            oldAmbient = RenderSettings.ambientLight; oldSky = RenderSettings.ambientSkyColor;
            oldEquator = RenderSettings.ambientEquatorColor; oldGround = RenderSettings.ambientGroundColor;
            oldAmbientIntensity = RenderSettings.ambientIntensity; oldReflection = RenderSettings.reflectionIntensity;
            oldFog = RenderSettings.fog; oldFogMode = RenderSettings.fogMode; oldFogDensity = RenderSettings.fogDensity;
            oldFogColor = RenderSettings.fogColor; oldFogStart = RenderSettings.fogStartDistance; oldFogEnd = RenderSettings.fogEndDistance;
            originalSkybox = RenderSettings.skybox;
            if (originalSkybox)
            {
                weatherSkybox = new Material(originalSkybox) { name = "Weather sky (runtime)" };
                foreach (string property in new[] { "_Horizon", "_Zenith", "_SkyZenith", "_SkyHorizon", "_GroundColor", "_SunColor", "_CloudColor", "_Tint", "_SkyTint" })
                    if (weatherSkybox.HasProperty(property)) skyColors[property] = weatherSkybox.GetColor(property);
                if (weatherSkybox.HasProperty("_Exposure")) skyExposure = weatherSkybox.GetFloat("_Exposure");
                RenderSettings.skybox = weatherSkybox;
            }
            ScanSunlight(); lightingReady = true;
        }

        private void ScanSunlight()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional && !light.GetComponent<WeatherLightExempt>() && !sunlight.ContainsKey(light))
                    sunlight.Add(light, light.intensity);
            nextLightScan = Time.time + 2f;
        }

        private void UpdateLighting()
        {
            if (Time.time >= nextLightScan) ScanSunlight();
            float ambient = Mathf.Lerp(1f, .46f, Rain);
            foreach (var entry in sunlight)
                if (entry.Key) entry.Key.intensity = entry.Value * Mathf.Lerp(1f, .3f, Rain);
            RenderSettings.ambientLight = oldAmbient * ambient;
            RenderSettings.ambientSkyColor = oldSky * ambient;
            RenderSettings.ambientEquatorColor = oldEquator * ambient;
            RenderSettings.ambientGroundColor = oldGround * ambient;
            RenderSettings.ambientIntensity = oldAmbientIntensity * Mathf.Lerp(1f, .65f, Rain);
            RenderSettings.reflectionIntensity = oldReflection * Mathf.Lerp(1f, .65f, Rain);
            RenderSettings.fog = oldFog || Rain > .01f;
            RenderSettings.fogMode = Rain > .01f ? FogMode.ExponentialSquared : oldFogMode;
            RenderSettings.fogDensity = Mathf.Lerp(oldFogDensity, Mathf.Max(oldFogDensity, .0025f), Rain);
            RenderSettings.fogColor = Color.Lerp(oldFogColor, new Color(.22f, .27f, .33f), Rain * .8f);
            if (weatherSkybox)
            {
                foreach (var entry in skyColors)
                    weatherSkybox.SetColor(entry.Key, Color.Lerp(entry.Value, entry.Value * .34f, Rain));
                if (weatherSkybox.HasProperty("_Exposure")) weatherSkybox.SetFloat("_Exposure", skyExposure * Mathf.Lerp(1f, .7f, Rain));
            }
            Shader.SetGlobalVector("_GoatWeatherWind", new Vector4(WindDirection.x, 0, WindDirection.z, Wind * Gust));
        }

        private void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            Shader.SetGlobalVector("_GoatWeatherWind", Vector4.zero);
            if (!lightingReady) return;
            foreach (var entry in sunlight) if (entry.Key) entry.Key.intensity = entry.Value;
            RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientSkyColor = oldSky;
            RenderSettings.ambientEquatorColor = oldEquator; RenderSettings.ambientGroundColor = oldGround;
            RenderSettings.ambientIntensity = oldAmbientIntensity; RenderSettings.reflectionIntensity = oldReflection;
            RenderSettings.fog = oldFog; RenderSettings.fogMode = oldFogMode; RenderSettings.fogDensity = oldFogDensity;
            RenderSettings.fogColor = oldFogColor; RenderSettings.fogStartDistance = oldFogStart; RenderSettings.fogEndDistance = oldFogEnd;
            if (RenderSettings.skybox == weatherSkybox) RenderSettings.skybox = originalSkybox;
            if (weatherSkybox) Destroy(weatherSkybox);
        }
    }
}
