using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicPostProcess : MonoBehaviour
    {
        private Material _material;
        private Material brightnessMaterial;

        private void Awake()
        {
            var shader = Shader.Find("GoatDescent/PostProcess");
            if (shader) _material = new Material(shader);
            var brightness = Resources.Load<Shader>("GoatBrightness");
            if (brightness) brightnessMaterial = new Material(brightness);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            bool colorEffects = _material && GoatGraphicsSettings.ColorEffects;
            float brightness = GoatGraphicsSettings.Brightness / 100f;
            if (!brightnessMaterial || Mathf.Approximately(brightness, 1f))
            {
                if (colorEffects) Graphics.Blit(source, destination, _material);
                else Graphics.Blit(source, destination);
                return;
            }
            brightnessMaterial.SetFloat("_Brightness", brightness);
            if (!colorEffects)
            {
                Graphics.Blit(source, destination, brightnessMaterial);
                return;
            }
            var temporary = RenderTexture.GetTemporary(source.descriptor);
            try
            {
                Graphics.Blit(source, temporary, _material);
                Graphics.Blit(temporary, destination, brightnessMaterial);
            }
            finally { RenderTexture.ReleaseTemporary(temporary); }
        }

        public void Apply(float exposure, float saturation, float contrast, float warmth, float vignette)
        {
            if (!_material) return;
            _material.SetFloat("_Exposure", exposure);
            _material.SetFloat("_Saturation", saturation);
            _material.SetFloat("_Contrast", contrast);
            _material.SetFloat("_Warmth", warmth);
            _material.SetFloat("_Vignette", vignette);
        }

        private void OnDestroy()
        {
            if (_material) Destroy(_material);
            if (brightnessMaterial) Destroy(brightnessMaterial);
        }
    }
}
