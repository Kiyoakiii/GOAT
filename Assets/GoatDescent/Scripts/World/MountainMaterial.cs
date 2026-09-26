using UnityEngine;

namespace GoatDescent
{
    public static class MountainMaterial
    {
        private static Material _shared;

        public static Material Get()
        {
            if (_shared) return _shared;
            var shader = Shader.Find("GoatDescent/Mountain");
            _shared = new Material(shader);
            _shared.SetTexture("_GrassTex", Grass());
            _shared.SetTexture("_RockTex", Rock());
            _shared.SetTexture("_SnowTex", Snow());
            return _shared;
        }

        private const int Size = 128;

        private static Texture2D Grass()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var baseCol = new Color(.34f, .52f, .26f);
            var darkCol = new Color(.22f, .38f, .18f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x / (float)Size, fy = y / (float)Size;
                    float n1 = Mathf.PerlinNoise(fx * 24f, fy * 24f);
                    float n2 = Mathf.PerlinNoise(fx * 6f + 3f, fy * 6f + 3f);
                    float clump = Mathf.SmoothStep(.55f, .95f, n1);
                    var c = Color.Lerp(baseCol, darkCol, clump * n2);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D Rock()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var lightCol = new Color(.58f, .55f, .50f);
            var darkCol = new Color(.30f, .28f, .26f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x / (float)Size, fy = y / (float)Size;
                    float n1 = Mathf.PerlinNoise(fx * 16f, fy * 16f);
                    float n2 = Mathf.PerlinNoise(fx * 4f + 7f, fy * 4f + 7f);
                    float crack = Mathf.Pow(Mathf.Abs(Mathf.PerlinNoise(fx * 40f + 1f, fy * 40f + 1f) - .5f) * 2f, 3f);
                    var c = Color.Lerp(lightCol, darkCol, Mathf.Clamp01(n1 * .7f + crack * .5f)) * (0.85f + n2 * .3f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D Snow()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var baseCol = new Color(.95f, .97f, 1f);
            var shadeCol = new Color(.82f, .87f, .93f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x / (float)Size, fy = y / (float)Size;
                    float n1 = Mathf.PerlinNoise(fx * 20f, fy * 20f);
                    float n2 = Mathf.PerlinNoise(fx * 5f + 11f, fy * 5f + 11f);
                    var c = Color.Lerp(baseCol, shadeCol, n1) * (0.92f + n2 * .16f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }
    }
}