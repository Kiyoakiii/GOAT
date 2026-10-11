using System.IO;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainMaterial
    {
        private static Material _shared;

        public static Material Get(MountainSettings settings = null)
        {
            if (_shared) return _shared;
            var shader = Shader.Find("GoatDescent/Mountain");
            if (!shader)
            {
                Debug.LogError("Shader GoatDescent/Mountain was not found.");
                return null;
            }
            _shared = new Material(shader);
            int maxSize = settings != null ? settings.TextureSize : 0;
            _shared.SetTexture("_DirtTex", Load("dirt_color", maxSize));
            _shared.SetTexture("_GrassTex", Load("grass_color", maxSize));
            _shared.SetTexture("_RockTex", Load("rock_color", maxSize));
            _shared.SetTexture("_SnowTex", CreateSnowTexture(maxSize > 0 ? maxSize : 64));
            _shared.SetFloat("_HighlightTrack", 1f);
            return _shared;
        }

        public static void Configure(Material material, MountainSettings settings)
        {
            if (!material || settings == null) return;
            material.SetFloat("_HeightMin", settings.ValleyY - 5f);
            material.SetFloat("_HeightMax", settings.SummitY + 5f);
            material.SetColor("_AtmoColor", RenderSettings.fogColor);
            material.SetFloat("_AtmoDensity", 0.9f / Mathf.Max(1f, settings.Radius * 4f));
            material.SetFloat("_SnowStart", settings.SnowStart);
            material.SetFloat("_Fade", settings.SnowFade);
            material.SetFloat("_GrassEnd", settings.GrassEnd);
            material.SetFloat("_Steep", settings.Steepness);
            material.SetFloat("_Tiling", settings.Tiling);
            material.SetFloat("_RouteProgress", 0f);
            material.SetFloat("_RouteSpacing", 1f / Mathf.Max(1, MountainGenerator.MainRouteCount - 1));
        }

        public static void ResetCache()
        {
            if (_shared) Object.Destroy(_shared);
            _shared = null;
        }

        public static Texture2D Load(string name, int maxSize = 0)
        {
            var path = Path.Combine(Application.dataPath, "GoatDescent", "Textures", name + ".jpg");
            if (!File.Exists(path))
            {
                Debug.LogWarning("Mountain texture was not found: " + path);
                return Texture2D.whiteTexture;
            }
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            if (!UnityEngine.ImageConversion.LoadImage(tex, bytes))
            {
                Debug.LogWarning("Mountain texture could not be decoded: " + path);
                return Texture2D.whiteTexture;
            }
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Repeat;
            if (maxSize > 0 && Mathf.Max(tex.width, tex.height) > maxSize)
            {
                tex = Downscale(tex, maxSize);
                tex.name = name;
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 4;
            }
            else
            {
                tex.filterMode = FilterMode.Trilinear;
                tex.anisoLevel = 8;
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Downscale(Texture2D source, int maxSize)
        {
            int width = maxSize;
            int height = maxSize;
            if (source.width > source.height)
                height = Mathf.Max(1, Mathf.RoundToInt(maxSize * (float)source.height / source.width));
            else if (source.height > source.width)
                width = Mathf.Max(1, Mathf.RoundToInt(maxSize * (float)source.width / source.height));
            var rt = RenderTexture.GetTemporary(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var result = new Texture2D(width, height, TextureFormat.RGB24, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(source);
            return result;
        }

        private static Texture2D CreateSnowTexture(int size)
        {
            size = Mathf.Clamp(size, 8, 256);
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float lattice = SnowNoise(x, y, size, Mathf.Max(1, size / 4)) * 0.65f
                        + SnowNoise(x, y, size, Mathf.Max(1, size / 8)) * 0.35f;
                    float value = 0.93f + lattice * 0.05f + SnowHash(x, y, size) * 0.02f;
                    byte channel = (byte)(Mathf.Clamp01(value) * 255f);
                    int blue = channel + 4 > 255 ? 255 : channel + 4;
                    pixels[y * size + x] = new Color32(channel, channel, (byte)blue, 255);
                }
            }
            tex.SetPixels32(pixels);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }

        private static float SnowHash(int x, int y, int size)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + size * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        private static float SnowNoise(int x, int y, int size, int period)
        {
            float fx = (float)x / size * period;
            float fy = (float)y / size * period;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            int xa = ((x0 % period) + period) % period;
            int ya = ((y0 % period) + period) % period;
            int xb = (xa + 1) % period;
            int yb = (ya + 1) % period;
            float a = SnowHash(xa, ya, period);
            float b = SnowHash(xb, ya, period);
            float c = SnowHash(xa, yb, period);
            float d = SnowHash(xb, yb, period);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }
    }
}
