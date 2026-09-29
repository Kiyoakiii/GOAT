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

        private void OnDestroy()
        {
            if (_material) Destroy(_material);
        }
    }
}