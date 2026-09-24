using LifeDay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LifeDay.EditorTools
{
    /// <summary>
    /// Builds localized ray-marched cloud objects. Each cube is merely the boundary of a
    /// volume; the visible cloud is an internally generated 3D density field with soft edges.
    /// No 2D sky texture or billboard is used.
    /// </summary>
    public static class VolumetricCloudscapeBuilder
    {
        private const string RootName = "LifeDay Procedural Volumetric Cloudscape";
        private const string GeneratedName = "Generated Raymarched Cloud Volumes";
        private const string MaterialFolder = "Assets/GoatDescent/Art/LifeDaySky/Materials";
        private const string TextureFolder = "Assets/GoatDescent/Art/LifeDaySky/Density";
        private const int TextureResolution = 88;

        private enum CloudProfile
        {
            HeroTower,
            SunsetShelf,
            HighWisps,
            DistantStorm,
            CumulusField,
            LayeredAnvil,
            Lenticular,
            RainCurtain,
            CirrusRibbon
        }

        [MenuItem("Tools/Goat Descent/Build LifeDay Volumetric Cloudscape")]
        public static void Build()
        {
            Shader shader = Shader.Find("LifeDay/Localized Volumetric Cloud");
            if (shader == null)
            {
                Debug.LogError("LIFEDAY_VOLUMETRIC_CLOUDS: shader has not compiled yet.");
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create procedural volumetric cloudscape");
            }

            Transform oldGenerated = root.transform.Find(GeneratedName);
            if (oldGenerated != null)
            {
                Undo.DestroyObjectImmediate(oldGenerated.gameObject);
            }

            root.transform.position = new Vector3(0f, 180f, 500f);
            root.transform.rotation = Quaternion.identity;
            CloudBankFollower follower = root.GetComponent<CloudBankFollower>();
            if (follower == null)
            {
                follower = Undo.AddComponent<CloudBankFollower>(root);
            }
            follower.Configure(500f, 180f);

            // Preserve older passes so they remain recoverable from Hierarchy.
            SetVisible("CloudBank_Sunset_01", false);
            SetVisible("Cloud_Low_01", false);
            SetVisible("Hand-Painted Cloudscape", false);

            GameObject generated = new GameObject(GeneratedName);
            generated.transform.SetParent(root.transform, false);

            // These are density fields, not painted cards. Several physical clouds can
            // share one field but get a different scale, rotation and place in the sky.
            // That keeps the asset budget sensible while avoiding a repeated "stamp".
            Material hero = CreateMaterial(shader, "M_VolumetricCloud_Hero", "CloudDensity_Hero", CloudProfile.HeroTower,
                new Color(0.055f, 0.15f, 0.31f), new Color(0.56f, 0.70f, 0.83f), new Color(1.0f, 0.78f, 0.58f), new Color(1.85f, 0.56f, 0.29f), 1.8f, 2.35f, 2.65f, 96);
            Material shelf = CreateMaterial(shader, "M_VolumetricCloud_Shelf", "CloudDensity_Shelf", CloudProfile.SunsetShelf,
                new Color(0.04f, 0.11f, 0.23f), new Color(0.38f, 0.57f, 0.70f), new Color(0.95f, 0.70f, 0.51f), new Color(1.38f, 0.38f, 0.18f), 0.7f, 2.0f, 2.4f, 72);
            Material wisps = CreateMaterial(shader, "M_VolumetricCloud_Wisps", "CloudDensity_Wisps", CloudProfile.HighWisps,
                new Color(0.12f, 0.25f, 0.36f), new Color(0.54f, 0.68f, 0.75f), new Color(1.0f, 0.84f, 0.69f), new Color(1.25f, 0.49f, 0.22f), 1.2f, 1.65f, 1.7f, 68);
            Material storm = CreateMaterial(shader, "M_VolumetricCloud_Storm", "CloudDensity_Storm", CloudProfile.DistantStorm,
                new Color(0.012f, 0.035f, 0.10f), new Color(0.11f, 0.20f, 0.34f), new Color(0.50f, 0.42f, 0.60f), new Color(0.92f, 0.25f, 0.16f), 2.25f, 2.7f, 3.4f, 64);
            Material field = CreateMaterial(shader, "M_VolumetricCloud_Field", "CloudDensity_Field", CloudProfile.CumulusField,
                new Color(0.045f, 0.12f, 0.27f), new Color(0.48f, 0.64f, 0.77f), new Color(1.0f, 0.77f, 0.57f), new Color(1.55f, 0.48f, 0.25f), 1.28f, 2.05f, 2.25f, 68);
            Material anvil = CreateMaterial(shader, "M_VolumetricCloud_Anvil", "CloudDensity_Anvil", CloudProfile.LayeredAnvil,
                new Color(0.055f, 0.12f, 0.25f), new Color(0.45f, 0.59f, 0.69f), new Color(0.95f, 0.75f, 0.59f), new Color(1.45f, 0.42f, 0.22f), 1.02f, 2.15f, 2.48f, 68);
            Material lenticular = CreateMaterial(shader, "M_VolumetricCloud_Lenticular", "CloudDensity_Lenticular", CloudProfile.Lenticular,
                new Color(0.08f, 0.15f, 0.28f), new Color(0.51f, 0.65f, 0.75f), new Color(1.0f, 0.81f, 0.64f), new Color(1.38f, 0.43f, 0.23f), 1.0f, 1.78f, 2.2f, 60);
            Material rain = CreateMaterial(shader, "M_VolumetricCloud_Rain", "CloudDensity_Rain", CloudProfile.RainCurtain,
                new Color(0.01f, 0.035f, 0.10f), new Color(0.12f, 0.22f, 0.36f), new Color(0.42f, 0.44f, 0.60f), new Color(0.58f, 0.18f, 0.20f), 1.5f, 2.4f, 3.25f, 60);
            Material cirrus = CreateMaterial(shader, "M_VolumetricCloud_Cirrus", "CloudDensity_Cirrus", CloudProfile.CirrusRibbon,
                new Color(0.10f, 0.22f, 0.34f), new Color(0.59f, 0.70f, 0.77f), new Color(1.0f, 0.84f, 0.69f), new Color(1.28f, 0.48f, 0.23f), 0.92f, 1.52f, 1.58f, 56);

            CreateVolume(
                generated.transform,
                "01 Hero Cumulus Tower",
                CloudProfile.HeroTower,
                new Vector3(12f, 0f, 0f),
                new Vector3(105f, 160f, 110f),
                hero);

            CreateVolume(generated.transform, "02 Sunward Cumulus Wing", CloudProfile.HeroTower,
                new Vector3(-73f, -28f, 42f), new Vector3(152f, 88f, 116f), new Vector3(0f, 14f, 0f), hero);
            CreateVolume(generated.transform, "03 West Cumulus Field", CloudProfile.CumulusField,
                new Vector3(-138f, -44f, 118f), new Vector3(178f, 56f, 122f), new Vector3(0f, -13f, 0f), field);
            CreateVolume(generated.transform, "04 East Cumulus Field", CloudProfile.CumulusField,
                new Vector3(128f, -36f, 102f), new Vector3(151f, 50f, 98f), new Vector3(0f, 27f, 0f), field);
            CreateVolume(generated.transform, "05 Far Cumulus Horizon", CloudProfile.CumulusField,
                new Vector3(12f, -47f, 168f), new Vector3(202f, 44f, 92f), new Vector3(0f, 7f, 0f), field);

            CreateVolume(generated.transform, "06 High Layered Anvil", CloudProfile.LayeredAnvil,
                new Vector3(-25f, 72f, 96f), new Vector3(248f, 46f, 126f), new Vector3(0f, -8f, 0f), anvil);
            CreateVolume(generated.transform, "07 Eastern Layered Anvil", CloudProfile.LayeredAnvil,
                new Vector3(144f, 50f, 178f), new Vector3(158f, 38f, 92f), new Vector3(0f, 23f, 0f), anvil);

            CreateVolume(generated.transform, "08 Low Sunset Shelf", CloudProfile.SunsetShelf,
                new Vector3(-62f, -48f, 52f), new Vector3(205f, 26f, 78f), new Vector3(0f, 5f, 0f), shelf);
            CreateVolume(generated.transform, "09 Distant Sunset Shelf", CloudProfile.SunsetShelf,
                new Vector3(83f, -13f, 164f), new Vector3(175f, 23f, 76f), new Vector3(0f, -18f, 0f), shelf);

            CreateVolume(generated.transform, "10 High Broken Wisps", CloudProfile.HighWisps,
                new Vector3(-42f, 98f, 40f), new Vector3(270f, 28f, 94f), new Vector3(0f, 9f, 0f), wisps);
            CreateVolume(generated.transform, "11 Eastern Broken Wisps", CloudProfile.HighWisps,
                new Vector3(128f, 116f, 126f), new Vector3(175f, 22f, 70f), new Vector3(0f, -28f, 0f), wisps);
            CreateVolume(generated.transform, "12 Pearl Lenticular Stack", CloudProfile.Lenticular,
                new Vector3(104f, 78f, 52f), new Vector3(158f, 39f, 96f), new Vector3(0f, -12f, 0f), lenticular);
            CreateVolume(generated.transform, "13 Far Lenticular Stack", CloudProfile.Lenticular,
                new Vector3(-115f, 57f, 198f), new Vector3(130f, 31f, 78f), new Vector3(0f, 18f, 0f), lenticular);
            CreateVolume(generated.transform, "14 Distant Storm Head", CloudProfile.DistantStorm,
                new Vector3(-205f, -15f, 66f), new Vector3(150f, 96f, 116f), new Vector3(0f, -9f, 0f), storm);
            CreateVolume(generated.transform, "15 Storm Rain Curtain", CloudProfile.RainCurtain,
                new Vector3(-181f, -62f, 112f), new Vector3(124f, 118f, 82f), new Vector3(0f, 11f, 0f), rain);
            CreateVolume(generated.transform, "16 Upper Cirrus Ribbon", CloudProfile.CirrusRibbon,
                new Vector3(25f, 133f, 128f), new Vector3(280f, 23f, 78f), new Vector3(0f, 4f, 0f), cirrus);
            CreateVolume(generated.transform, "17 Western Cirrus Ribbon", CloudProfile.CirrusRibbon,
                new Vector3(-164f, 111f, 92f), new Vector3(158f, 18f, 64f), new Vector3(0f, 31f, 0f), cirrus);

            GameObject automaticField = new GameObject("Automatic Cloud Field (seeded)");
            automaticField.transform.SetParent(generated.transform, false);
            BuildAutomaticCloudField(automaticField.transform, hero, shelf, wisps, field, anvil, lenticular, storm, rain, cirrus);

            EditorSceneManager.MarkSceneDirty(root.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("LIFEDAY_VOLUMETRIC_CLOUDS_READY: generated a hand-shaped hero bank plus 14 seeded automatic 3D cloud objects across nine distinct density profiles.");
        }

        private static void BuildAutomaticCloudField(
            Transform parent, Material hero, Material shelf, Material wisps, Material field, Material anvil,
            Material lenticular, Material storm, Material rain, Material cirrus)
        {
            // A deterministic seed means the sky is procedural without becoming
            // impossible to reproduce: every rebuild gives the same composition until
            // this one number is changed.
            var random = new System.Random(260827);
            // Match the 14 seeded auxiliary volumes in LifeDay's saved scene.
            // The source generator currently uses 140, but that is not what the
            // user sees in the source project and would be excessive here.
            for (int i = 0; i < 14; i++)
            {
                float angle = NextFloat(random, 0f, Mathf.PI * 2f);
                float radius = NextFloat(random, 132f, 258f);
                float y = NextFloat(random, -48f, 126f);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float yaw = NextFloat(random, -42f, 42f);
                int variant = random.Next(0, 6);

                CloudProfile profile;
                Material material;
                Vector3 scale;
                switch (variant)
                {
                    case 0:
                        profile = CloudProfile.CumulusField;
                        material = field;
                        scale = new Vector3(NextFloat(random, 96f, 166f), NextFloat(random, 34f, 58f), NextFloat(random, 70f, 124f));
                        break;
                    case 1:
                        profile = CloudProfile.LayeredAnvil;
                        material = anvil;
                        scale = new Vector3(NextFloat(random, 132f, 226f), NextFloat(random, 24f, 46f), NextFloat(random, 78f, 136f));
                        y = NextFloat(random, 22f, 96f);
                        break;
                    case 2:
                        profile = CloudProfile.SunsetShelf;
                        material = shelf;
                        scale = new Vector3(NextFloat(random, 128f, 220f), NextFloat(random, 18f, 31f), NextFloat(random, 58f, 104f));
                        y = NextFloat(random, -52f, 24f);
                        break;
                    case 3:
                        profile = CloudProfile.HighWisps;
                        material = wisps;
                        scale = new Vector3(NextFloat(random, 190f, 290f), NextFloat(random, 15f, 28f), NextFloat(random, 58f, 102f));
                        y = NextFloat(random, 76f, 142f);
                        break;
                    case 4:
                        profile = CloudProfile.Lenticular;
                        material = lenticular;
                        scale = new Vector3(NextFloat(random, 86f, 164f), NextFloat(random, 18f, 36f), NextFloat(random, 48f, 86f));
                        y = NextFloat(random, 48f, 118f);
                        break;
                    default:
                        profile = CloudProfile.CirrusRibbon;
                        material = cirrus;
                        scale = new Vector3(NextFloat(random, 180f, 292f), NextFloat(random, 13f, 24f), NextFloat(random, 54f, 92f));
                        y = NextFloat(random, 94f, 152f);
                        break;
                }

                CreateVolume(parent, $"Auto Cloud {i + 1:00} — {profile}", profile,
                    new Vector3(x, y, z), scale, new Vector3(0f, yaw, 0f), material);
            }
        }

        private static float NextFloat(System.Random random, float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private static void CreateVolume(Transform parent, string name, CloudProfile profile, Vector3 localPosition, Vector3 localScale, Material material)
        {
            CreateVolume(parent, name, profile, localPosition, localScale, Vector3.zero, material);
        }

        private static void CreateVolume(Transform parent, string name, CloudProfile profile, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles, Material material)
        {
            GameObject volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = name;
            volume.transform.SetParent(parent, false);
            volume.transform.localPosition = localPosition;
            volume.transform.localRotation = Quaternion.Euler(localEulerAngles);
            volume.transform.localScale = localScale;
            Collider collider = volume.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            MeshRenderer renderer = volume.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static Material CreateMaterial(
            Shader shader, string materialName, string textureName, CloudProfile profile,
            Color bottom, Color middle, Color top, Color sunset,
            float density, float absorption, float lightAbsorption, int steps)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;

            material.SetTexture("_DensityTex", GetOrCreateDensityTexture(textureName, profile));
            material.SetColor("_BottomColor", bottom);
            material.SetColor("_MiddleColor", middle);
            material.SetColor("_TopColor", top);
            material.SetColor("_SunColor", sunset);
            material.SetFloat("_Density", density);
            material.SetFloat("_Absorption", absorption);
            material.SetFloat("_LightAbsorption", lightAbsorption);
            material.SetFloat("_StepCount", steps);
            material.SetFloat("_LightStep", profile == CloudProfile.HeroTower ? 0.072f : profile == CloudProfile.CirrusRibbon ? 0.12f : 0.10f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture3D GetOrCreateDensityTexture(string name, CloudProfile profile)
        {
            string path = $"{TextureFolder}/{name}.asset";
            Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            if (texture == null)
            {
                texture = new Texture3D(TextureResolution, TextureResolution, TextureResolution, TextureFormat.RGBA32, false, true)
                {
                    name = name,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0
                };
                AssetDatabase.CreateAsset(texture, path);
            }

            var pixels = new Color32[TextureResolution * TextureResolution * TextureResolution];
            int index = 0;
            int seed = 173 + (int)profile * 491;
            for (int z = 0; z < TextureResolution; z++)
            for (int y = 0; y < TextureResolution; y++)
            for (int x = 0; x < TextureResolution; x++)
            {
                Vector3 p = new Vector3(
                    x / (TextureResolution - 1f) * 2f - 1f,
                    y / (TextureResolution - 1f) * 2f - 1f,
                    z / (TextureResolution - 1f) * 2f - 1f);
                byte density = (byte)Mathf.RoundToInt(Mathf.Clamp01(BuildDensity(p, profile, seed)) * 255f);
                pixels[index++] = new Color32(density, density, density, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static float BuildDensity(Vector3 p, CloudProfile profile, int seed)
        {
            // Push the density around before sampling it. This is what breaks the
            // manufactured-sphere silhouette: every face receives a different soft
            // erosion pattern, while the underlying shape remains one coherent cloud.
            Vector3 warped = p + new Vector3(
                Fbm(p * 1.75f + new Vector3(4.1f, 1.2f, 8.3f), seed + 17, 3) - 0.5f,
                Fbm(p * 1.55f + new Vector3(2.6f, 7.9f, 3.2f), seed + 29, 3) - 0.5f,
                Fbm(p * 1.92f + new Vector3(9.4f, 2.8f, 1.5f), seed + 43, 3) - 0.5f) * 0.13f;
            float macro = Fbm(warped * 2.35f + new Vector3(7.3f, 1.8f, 4.6f), seed, 4);
            float detail = Fbm(warped * 13.8f + new Vector3(3.7f, 9.1f, 1.4f), seed + 61, 3);
            float shape;
            switch (profile)
            {
                case CloudProfile.HeroTower:
                    shape = HeroTowerShape(warped);
                    break;
                case CloudProfile.SunsetShelf:
                    shape = SunsetShelfShape(warped, macro);
                    break;
                case CloudProfile.HighWisps:
                    shape = HighWispShape(warped, macro);
                    break;
                case CloudProfile.CumulusField:
                    shape = CumulusFieldShape(warped);
                    break;
                case CloudProfile.LayeredAnvil:
                    shape = LayeredAnvilShape(warped, macro);
                    break;
                case CloudProfile.Lenticular:
                    shape = LenticularShape(warped, macro);
                    break;
                case CloudProfile.RainCurtain:
                    shape = RainCurtainShape(warped, macro);
                    break;
                case CloudProfile.CirrusRibbon:
                    shape = CirrusRibbonShape(warped, macro);
                    break;
                default:
                    shape = StormShape(warped, macro);
                    break;
            }

            float edgeFactor = SmoothThreshold(0.02f, 0.86f, shape);
            float erosion = Mathf.Lerp(0.34f, 0.055f, edgeFactor);
            float density = shape + (macro - 0.5f) * 0.26f - (1f - detail) * erosion;

            // Every density texture is forced to zero before the bounds of its cube.
            // It removes the tell-tale rectangular veil while leaving a long, gradual
            // transparent fringe for the ray marcher to render.
            float boundary = 1f - Mathf.Max(Mathf.Abs(p.x), Mathf.Max(Mathf.Abs(p.y), Mathf.Abs(p.z)));
            float containerFade = SmoothThreshold(0.0f, 0.20f, boundary);
            // The voxel field needs a genuinely dense core. The former threshold left
            // the entire texture below 12% density, which made correctly placed cloud
            // objects read as an almost invisible grey haze in Game View.
            bool thinCloud = profile == CloudProfile.HighWisps || profile == CloudProfile.CirrusRibbon || profile == CloudProfile.Lenticular;
            float thresholdStart = thinCloud ? 0.045f : profile == CloudProfile.RainCurtain ? 0.12f : 0.18f;
            float thresholdEnd = thinCloud ? 0.33f : profile == CloudProfile.RainCurtain ? 0.42f : 0.48f;
            return Mathf.Pow(SmoothThreshold(thresholdStart, thresholdEnd, density), 1.35f) * containerFade;
        }

        private static float HeroTowerShape(Vector3 p)
        {
            float height = Mathf.Clamp01((p.y + 1f) * 0.5f);
            float centerX = 0.11f * Mathf.Sin(height * 5.1f - 0.8f) - 0.08f * height;
            float radiusX = Mathf.Lerp(0.58f, 0.15f, Mathf.Pow(height, 0.84f)) + 0.055f * Mathf.Sin(height * 10f);
            float radiusZ = Mathf.Lerp(0.52f, 0.14f, Mathf.Pow(height, 0.76f));
            float core = 1f - Mathf.Sqrt(
                Mathf.Pow((p.x - centerX) / Mathf.Max(0.05f, radiusX), 2f) +
                Mathf.Pow((p.z + 0.03f) / Mathf.Max(0.05f, radiusZ), 2f));
            core *= SmoothThreshold(0.01f, 0.13f, height) * (1f - SmoothThreshold(0.94f, 1f, height));

            // Four different super-ellipsoid lobes create an asymmetric plume. Their
            // profiles are deliberately not circular, then the density erosion above
            // turns their joins into continuous vapor rather than visible round balls.
            float baseLobe = SoftLobe(p, new Vector3(-0.18f, -0.45f, 0.03f), new Vector3(0.70f, 0.31f, 0.51f), 2.55f);
            float leftShoulder = SoftLobe(p, new Vector3(-0.48f, -0.16f, 0.00f), new Vector3(0.41f, 0.39f, 0.45f), 2.15f);
            float sunwardRise = SoftLobe(p, new Vector3(0.26f, 0.10f, -0.08f), new Vector3(0.39f, 0.53f, 0.39f), 2.65f);
            float crown = SoftLobe(p, new Vector3(0.00f, 0.63f, 0.02f), new Vector3(0.27f, 0.35f, 0.26f), 2.45f);
            float anvil = SoftLobe(p, new Vector3(-0.09f, 0.43f, 0.04f), new Vector3(0.48f, 0.14f, 0.30f), 3.20f);
            return Mathf.Max(core, Mathf.Max(baseLobe, Mathf.Max(leftShoulder, Mathf.Max(sunwardRise, Mathf.Max(crown, anvil)))));
        }

        private static float SunsetShelfShape(Vector3 p, float macro)
        {
            float waviness = Mathf.Sin(p.x * 3.4f) * 0.09f + Mathf.Sin(p.x * 8.1f + p.z * 2.2f) * 0.035f;
            float thickness = 0.25f + (macro - 0.5f) * 0.10f;
            float band = 1f - Mathf.Abs(p.y + 0.15f + waviness) / Mathf.Max(0.08f, thickness);
            float depth = 1f - Mathf.Abs(p.z) / 0.84f;
            float sides = 1f - Mathf.Pow(Mathf.Abs(p.x), 3.4f);
            return Mathf.Min(band, Mathf.Min(depth, sides));
        }

        private static float HighWispShape(Vector3 p, float macro)
        {
            float path = 0.16f * Mathf.Sin(p.x * 3.8f) + 0.08f * Mathf.Sin(p.x * 8.5f + 1.7f);
            float thickness = 0.105f + macro * 0.075f;
            float strand = 1f - Mathf.Abs(p.y - path) / thickness;
            float depth = 1f - Mathf.Abs(p.z + 0.15f * Mathf.Sin(p.x * 4.1f)) / (0.34f + macro * 0.20f);
            float broken = SmoothThreshold(0.38f, 0.68f, macro);
            return Mathf.Min(strand, depth) * broken;
        }

        private static float StormShape(Vector3 p, float macro)
        {
            float centerX = -0.14f + 0.10f * Mathf.Sin(p.y * 3f);
            float radiusX = 0.68f - Mathf.Max(0f, p.y) * 0.24f;
            float radiusZ = 0.64f - Mathf.Max(0f, p.y) * 0.20f;
            float core = 1f - Mathf.Sqrt(
                Mathf.Pow((p.x - centerX) / Mathf.Max(0.14f, radiusX), 2f) +
                Mathf.Pow((p.z + 0.05f) / Mathf.Max(0.14f, radiusZ), 2f));
            float vertical = 1f - Mathf.Pow(Mathf.Max(0f, p.y - 0.35f), 2f) * 1.8f;
            return core * vertical + (macro - 0.5f) * 0.08f;
        }

        private static float CumulusFieldShape(Vector3 p)
        {
            // A field is intentionally wide and low, with several irregular turrets
            // joined by a soft vapor floor rather than a row of visible spheres.
            float vaporFloor = SoftLobe(p, new Vector3(-0.04f, -0.43f, 0.02f), new Vector3(0.91f, 0.19f, 0.70f), 3.4f) * 0.86f;
            float west = SoftLobe(p, new Vector3(-0.46f, -0.18f, -0.10f), new Vector3(0.32f, 0.36f, 0.41f), 2.7f);
            float middle = SoftLobe(p, new Vector3(-0.08f, -0.07f, 0.07f), new Vector3(0.39f, 0.45f, 0.46f), 2.4f);
            float east = SoftLobe(p, new Vector3(0.40f, -0.20f, -0.04f), new Vector3(0.30f, 0.31f, 0.36f), 2.9f);
            float sunwardCap = SoftLobe(p, new Vector3(0.18f, 0.30f, -0.14f), new Vector3(0.28f, 0.24f, 0.29f), 2.35f);
            return Mathf.Max(vaporFloor, Mathf.Max(west, Mathf.Max(middle, Mathf.Max(east, sunwardCap))));
        }

        private static float LayeredAnvilShape(Vector3 p, float macro)
        {
            float waveA = Mathf.Sin(p.x * 3.1f + p.z * 1.8f) * 0.09f + Mathf.Sin(p.x * 8.7f) * 0.026f;
            float waveB = Mathf.Sin(p.x * 4.4f - p.z * 1.3f + 1.1f) * 0.065f;
            float lowerLayer = 1f - Mathf.Abs(p.y + 0.18f + waveA) / (0.15f + macro * 0.05f);
            float upperLayer = 1f - Mathf.Abs(p.y - 0.15f + waveB) / (0.115f + macro * 0.05f);
            float crownLayer = 1f - Mathf.Abs(p.y - 0.43f + waveA * 0.5f) / 0.11f;
            float depth = 1f - Mathf.Abs(p.z) / (0.72f + macro * 0.12f);
            float sides = 1f - Mathf.Pow(Mathf.Abs(p.x), 3.2f);
            float deck = Mathf.Max(lowerLayer, Mathf.Max(upperLayer * 0.84f, crownLayer * 0.50f));
            float distantMound = SoftLobe(p, new Vector3(-0.18f, 0.18f, 0.05f), new Vector3(0.56f, 0.31f, 0.52f), 3.2f) * 0.64f;
            return Mathf.Max(Mathf.Min(deck, Mathf.Min(depth, sides)), distantMound);
        }

        private static float LenticularShape(Vector3 p, float macro)
        {
            float sway = Mathf.Sin(p.x * 4.2f) * 0.035f + Mathf.Sin(p.x * 9.1f + p.z * 2.3f) * 0.018f;
            float lowerLens = SoftLobe(p, new Vector3(-0.03f, -0.10f + sway, 0.02f), new Vector3(0.90f, 0.115f, 0.43f), 3.2f);
            float upperLens = SoftLobe(p, new Vector3(0.06f, 0.15f + sway, -0.02f), new Vector3(0.72f, 0.095f, 0.34f), 3.45f);
            float crownLens = SoftLobe(p, new Vector3(-0.08f, 0.33f + sway * 0.5f, 0.03f), new Vector3(0.47f, 0.070f, 0.25f), 3.7f);
            float broken = SmoothThreshold(0.24f, 0.62f, macro) * 0.35f + 0.65f;
            return Mathf.Max(lowerLens, Mathf.Max(upperLens * 0.88f, crownLens * 0.56f)) * broken;
        }

        private static float RainCurtainShape(Vector3 p, float macro)
        {
            float path = -0.10f + 0.13f * Mathf.Sin(p.y * 4.1f) + 0.05f * Mathf.Sin(p.z * 5.3f);
            float width = 0.55f + macro * 0.12f;
            float curtain = 1f - Mathf.Abs(p.x - path) / width;
            float depth = 1f - Mathf.Abs(p.z + 0.08f * Mathf.Sin(p.y * 6.0f)) / 0.62f;
            float vertical = 1f - Mathf.Pow(Mathf.Abs(p.y + 0.06f), 2f) * 0.30f;
            float torn = SmoothThreshold(0.22f, 0.72f, macro);
            return Mathf.Min(curtain, Mathf.Min(depth, vertical)) * (0.42f + torn * 0.58f);
        }

        private static float CirrusRibbonShape(Vector3 p, float macro)
        {
            float pathA = 0.18f * Mathf.Sin(p.x * 3.0f + 0.8f) + 0.055f * Mathf.Sin(p.x * 10.0f);
            float pathB = -0.24f + 0.13f * Mathf.Sin(p.x * 4.3f - 1.6f);
            float strandA = 1f - Mathf.Abs(p.y - pathA) / (0.065f + macro * 0.055f);
            float strandB = 1f - Mathf.Abs(p.y - pathB) / (0.045f + macro * 0.035f);
            float depth = 1f - Mathf.Abs(p.z + 0.17f * Mathf.Sin(p.x * 3.7f)) / (0.30f + macro * 0.18f);
            float broken = SmoothThreshold(0.33f, 0.71f, macro);
            return Mathf.Max(strandA, strandB * 0.72f) * Mathf.Min(depth, broken);
        }

        private static float SoftLobe(Vector3 p, Vector3 center, Vector3 radius, float exponent)
        {
            Vector3 q = new Vector3((p.x - center.x) / radius.x, (p.y - center.y) / radius.y, (p.z - center.z) / radius.z);
            float poweredDistance = Mathf.Pow(Mathf.Abs(q.x), exponent) + Mathf.Pow(Mathf.Abs(q.y), exponent) + Mathf.Pow(Mathf.Abs(q.z), exponent);
            return 1f - Mathf.Pow(poweredDistance, 1f / exponent);
        }

        private static float Fbm(Vector3 p, int seed, int octaves)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                value += ValueNoise(p * frequency, seed + i * 131) * amplitude;
                frequency *= 2.03f;
                amplitude *= 0.5f;
            }
            return value / 0.9375f;
        }

        private static float ValueNoise(Vector3 p, int seed)
        {
            int x0 = Mathf.FloorToInt(p.x);
            int y0 = Mathf.FloorToInt(p.y);
            int z0 = Mathf.FloorToInt(p.z);
            float tx = Smooth(p.x - x0);
            float ty = Smooth(p.y - y0);
            float tz = Smooth(p.z - z0);
            float a = Mathf.Lerp(Hash(x0, y0, z0, seed), Hash(x0 + 1, y0, z0, seed), tx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1, z0, seed), Hash(x0 + 1, y0 + 1, z0, seed), tx);
            float c = Mathf.Lerp(Hash(x0, y0, z0 + 1, seed), Hash(x0 + 1, y0, z0 + 1, seed), tx);
            float d = Mathf.Lerp(Hash(x0, y0 + 1, z0 + 1, seed), Hash(x0 + 1, y0 + 1, z0 + 1, seed), tx);
            return Mathf.Lerp(Mathf.Lerp(a, b, ty), Mathf.Lerp(c, d, ty), tz);
        }

        private static float Smooth(float value) => value * value * (3f - 2f * value);

        private static float SmoothThreshold(float edgeStart, float edgeEnd, float value)
        {
            float t = Mathf.Clamp01((value - edgeStart) / Mathf.Max(0.0001f, edgeEnd - edgeStart));
            return t * t * (3f - 2f * t);
        }

        private static float Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                int n = x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 265443576;
                n = (n ^ (n >> 13)) * 1274126177;
                return (n & 0x7fffffff) / 2147483647f;
            }
        }

        private static void SetVisible(string objectName, bool visible)
        {
            GameObject objectToToggle = GameObject.Find(objectName);
            if (objectToToggle != null) objectToToggle.SetActive(visible);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
