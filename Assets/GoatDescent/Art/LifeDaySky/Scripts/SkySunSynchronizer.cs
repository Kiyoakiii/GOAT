using UnityEngine;

namespace LifeDay.World
{
    /// <summary>
    /// Keeps the visual sun inside a custom skybox aligned with the real directional light.
    /// The light illuminates geometry and water, while the sky shader draws the matching disc.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkySunSynchronizer : MonoBehaviour
    {
        [SerializeField] private Light sourceLight;

        public void Configure(Light light)
        {
            sourceLight = light;
            ApplyToSkybox();
        }

        private void Awake()
        {
            if (sourceLight == null)
            {
                sourceLight = GetComponent<Light>();
            }

            ApplyToSkybox();
        }

        private void LateUpdate()
        {
            ApplyToSkybox();
        }

        private void ApplyToSkybox()
        {
            if (sourceLight == null)
            {
                return;
            }

            // A directional light points in the direction its rays travel. The sun disc
            // must be drawn in the opposite direction: from the world toward the light.
            Vector3 sunDirection = -sourceLight.transform.forward;
            RenderSettings.sun = sourceLight;
            Shader.SetGlobalVector("_LifeDaySunDirection", new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
            Shader.SetGlobalColor("_LifeDaySunColor", sourceLight.color);

            Material skybox = RenderSettings.skybox;
            if (skybox == null || !skybox.HasProperty("_SunDirection"))
            {
                return;
            }

            skybox.SetVector("_SunDirection", new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
            if (skybox.HasProperty("_SunColor"))
            {
                skybox.SetColor("_SunColor", sourceLight.color);
            }

            if (skybox.HasProperty("_SunIntensity"))
            {
                skybox.SetFloat("_SunIntensity", sourceLight.intensity);
            }
        }
    }
}
