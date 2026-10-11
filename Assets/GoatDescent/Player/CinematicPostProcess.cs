using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicPostProcess : MonoBehaviour
    {
        private Material _material;

        private void Awake()
        {
            var shader = Shader.Find("GoatDescent/PostProcess");
            if (shader) _material = new Material(shader);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material)
                Graphics.Blit(source, destination, _material);
            else
                Graphics.Blit(source, destination);
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
        }
    }
}