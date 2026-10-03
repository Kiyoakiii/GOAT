namespace GoatDescent.ProceduralWorld.Editor
{
    internal static class VistaAssetPaths
    {
        public const string SettingsPath = "Assets/GoatDescent/Vista/Data/WorldSettings_Prototype.asset";
        public const string VegetationSettingsPath = "Assets/GoatDescent/Vista/Data/VegetationGenerationSettings_Prototype.asset";
        public const string ScenePath = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldMilestone.unity";
        public const string GroundcoverPreviewScenePath = "Assets/GoatDescent/Vista/Scenes/ProceduralWorldGroundcoverPreview.unity";
        public const string TerrainDataPath = "Assets/GoatDescent/Vista/Generated/Terrain/ProceduralWorldTerrainData.asset";
        public const string MaterialRoot = "Assets/GoatDescent/Vista/Generated/Materials";
        public const string DebugRoot = "Assets/GoatDescent/Vista/Generated/Debug";
        public const string GrassRoot = "Assets/GoatDescent/Vista/Generated/Grass";
        public const string GrassMeshPath = GrassRoot + "/GrassClump.asset";
        public const string GrassPrefabPath = GrassRoot + "/GrassClump.prefab";
        public const float FbxUnitCompensation = 100f;

        public static readonly string[] GroundcoverMeshNames = { "MeadowGrass", "AlpineBloom", "MountainHeather", "FrostJuniper", "AlpineBerryShrub" };
        public static readonly string[] GroundcoverMeshPaths =
        {
            GrassMeshPath,
            GrassRoot + "/AlpineBloom.asset",
            GrassRoot + "/MountainHeather.asset",
            GrassRoot + "/FrostJuniper.asset",
            GrassRoot + "/AlpineBerryShrub.asset"
        };

        public static readonly string[] RockPaths =
        {
            "Assets/Art/Generated/Rocks/Rock_Small.fbx",
            "Assets/Art/Generated/Rocks/Rock_Medium.fbx",
            "Assets/Art/Generated/Rocks/Rock_Large.fbx",
            "Assets/Art/Generated/Rocks/Mountain_Boulder.fbx"
        };

        public static readonly string[] TreePaths =
        {
            "Assets/Art/Generated/Trees/Tree_Pine.fbx",
            "Assets/Art/Generated/Trees/Tree_Fir.fbx",
            "Assets/Art/Generated/Trees/Tree_Dead.fbx",
            "Assets/Art/Generated/Trees/Tree_Stump.fbx"
        };

        public static readonly string[] SummitTreePaths =
        {
            "Assets/Art/Generated/Trees/Tree_Pine.fbx",
            "Assets/Art/Generated/Trees/Tree_Pine_OldGrowth.fbx",
            "Assets/Art/Generated/Trees/Tree_Pine_Windbent.fbx",
            "Assets/Art/Generated/Trees/Tree_Fir.fbx"
        };

        public static readonly string[] CliffPaths =
        {
            "Assets/Art/Generated/Cliffs/Cliff_Straight.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Corner.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Tall.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Wide.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Broken.fbx",
            "Assets/Art/Generated/Cliffs/Cliff_Cap.fbx"
        };
    }
}