using System.IO;
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
            if (!shader)
            {
                Debug.LogError("Shader GoatDescent/Mountain was not found.");
                return null;
            }
            _shared = new Material(shader);
            _shared.SetTexture("_DirtTex", Load("dirt_color"));
            _shared.SetTexture("_GrassTex", Load("grass_color"));
            _shared.SetTexture("_RockTex", Load("rock_color"));
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
            material.SetFloat("_RouteProgress", 0f);
            material.SetFloat("_RouteSpacing", 1f / Mathf.Max(1, MountainGenerator.MainRouteCount - 1));
        }

        private static Texture2D Load(string name)
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
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply();
            return tex;
        }
    }
}
