using UnityEngine;

namespace GoatDescent
{
    [RequireComponent(typeof(Camera))]
    public sealed class GoatScenePost : MonoBehaviour
    {
        private static readonly int BloomTex = Shader.PropertyToID("_BloomTex");
        private static readonly int BloomThreshold = Shader.PropertyToID("_BloomThreshold");
        private static readonly int BloomIntensity = Shader.PropertyToID("_BloomIntensity");
        private static readonly int Vignette = Shader.PropertyToID("_Vignette");
        private static readonly int Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int Contrast = Shader.PropertyToID("_Contrast");
        private static readonly int WarmTint = Shader.PropertyToID("_WarmTint");
        private static readonly int CoolTint = Shader.PropertyToID("_CoolTint");
        private static readonly int TexelSize = Shader.PropertyToID("_TexelSize");
        private static readonly int Horizontal = Shader.PropertyToID("_Horizontal");

        [Range(0f, 1f)] public float bloomThreshold = .75f;
        [Range(0f, 2f)] public float bloomIntensity = .55f;
        [Range(0f, 1f)] public float vignette = .35f;
        [Range(0f, 2f)] public float saturation = 1.15f;
        [Range(.5f, 1.5f)] public float contrast = 1.08f;
        public Color warmTint = new Color(1.04f, 1f, .93f);
        public Color coolTint = new Color(.95f, .99f, 1.05f);

        private Material material;

        private Material Material
        {
            get
            {
                if (!material) material = new Material(Shader.Find("GoatDescent/ScenePost"));
                return material;
            }
        }

        public void CopyFrom(GoatScenePost other)
        {
            bloomThreshold = other.bloomThreshold;
            bloomIntensity = other.bloomIntensity;
            vignette = other.vignette;
            saturation = other.saturation;
            contrast = other.contrast;
            warmTint = other.warmTint;
            coolTint = other.coolTint;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture destination)
        {
            var m = Material;
            int w = src.width / 2, h = src.height / 2;
            var rtA = RenderTexture.GetTemporary(w, h, 0, src.format);
            var rtB = RenderTexture.GetTemporary(w, h, 0, src.format);
            m.SetFloat(BloomThreshold, bloomThreshold);
            m.SetFloat(BloomIntensity, bloomIntensity);
            m.SetFloat(Vignette, vignette);
            m.SetFloat(Saturation, saturation);
            m.SetFloat(Contrast, contrast);
            m.SetColor(WarmTint, warmTint);
            m.SetColor(CoolTint, coolTint);
            m.SetVector(TexelSize, new Vector4(1f / w, 1f / h, w, h));
            Graphics.Blit(src, rtA, m, 0);
            m.SetFloat(Horizontal, 1f);
            Graphics.Blit(rtA, rtB, m, 1);
            m.SetFloat(Horizontal, 0f);
            Graphics.Blit(rtB, rtA, m, 1);
            m.SetTexture(BloomTex, rtA);
            Graphics.Blit(src, destination, m, 2);
            RenderTexture.ReleaseTemporary(rtA);
            RenderTexture.ReleaseTemporary(rtB);
        }
    }
}
