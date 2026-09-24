using UnityEngine;

namespace LifeDay.World
{
    /// <summary>
    /// Keeps inter-cloud lighting up to date while the cloud bank stays fixed in world space.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CloudBankFollower : MonoBehaviour
    {
        private const int MaxCloudShadowVolumes = 32;
        private static readonly int CloudShadowWorldToLocalId = Shader.PropertyToID("_CloudShadowWorldToLocal");
        private static readonly int CloudShadowCountId = Shader.PropertyToID("_CloudShadowCount");
        private static readonly int CloudShadowStrengthId = Shader.PropertyToID("_CloudShadowStrength");
        private static readonly int CloudShadowSelfIndexId = Shader.PropertyToID("_CloudShadowSelfIndex");

        [SerializeField, Min(1f)] private float distance = 220f;
        [SerializeField] private float heightAboveCamera = 66f;

        private readonly Matrix4x4[] shadowWorldToLocal = new Matrix4x4[MaxCloudShadowVolumes];
        private MaterialPropertyBlock shadowPropertyBlock;

        private void Awake()
        {
            shadowPropertyBlock = new MaterialPropertyBlock();
        }

        public void Configure(float targetDistance, float targetHeightAboveCamera)
        {
            distance = Mathf.Max(1f, targetDistance);
            heightAboveCamera = targetHeightAboveCamera;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateInterCloudShadows();
        }

        private void UpdateInterCloudShadows()
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            int cloudCount = 0;
            for (int i = 0; i < allRenderers.Length && cloudCount < MaxCloudShadowVolumes; i++)
            {
                Renderer cloudRenderer = allRenderers[i];
                Material sharedMaterial = cloudRenderer.sharedMaterial;
                if (sharedMaterial == null || sharedMaterial.shader == null ||
                    sharedMaterial.shader.name != "LifeDay/Localized Volumetric Cloud")
                {
                    continue;
                }

                shadowWorldToLocal[cloudCount] = cloudRenderer.transform.worldToLocalMatrix;
                shadowPropertyBlock.Clear();
                shadowPropertyBlock.SetFloat(CloudShadowSelfIndexId, cloudCount);
                cloudRenderer.SetPropertyBlock(shadowPropertyBlock);
                cloudCount++;
            }

            Shader.SetGlobalMatrixArray(CloudShadowWorldToLocalId, shadowWorldToLocal);
            Shader.SetGlobalFloat(CloudShadowCountId, cloudCount);
            Shader.SetGlobalFloat(CloudShadowStrengthId, 1.8f);
        }
    }
}
