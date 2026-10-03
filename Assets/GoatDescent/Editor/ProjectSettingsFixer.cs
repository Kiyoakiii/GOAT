using UnityEditor;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class ProjectSettingsFixer
    {
        public static void Apply()
        {
            PlayerSettings.bakeCollisionMeshes = true;
            PlayerSettings.showUnitySplashScreen = false;
            PlayerSettings.stripUnusedMeshComponents = true;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                QualitySettings.globalTextureMipmapLimit = 0;
            AssetDatabase.SaveAssets();
            Debug.Log("PROJECT_SETTINGS_FIXED");
        }
    }
}