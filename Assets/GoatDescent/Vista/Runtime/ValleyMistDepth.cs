using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    /// <summary>Provides opaque depth for the valley volume, including scene and runtime cameras.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ValleyMistDepth : MonoBehaviour
    {
        private void OnEnable() => Camera.onPreCull += PrepareCamera;
        private void OnDisable() => Camera.onPreCull -= PrepareCamera;
        private void PrepareCamera(Camera camera)
        {
            if (camera != null && camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
                camera.depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
