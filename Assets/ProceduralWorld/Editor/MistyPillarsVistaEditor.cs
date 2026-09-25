using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GoatDescent.ProceduralWorld.Editor
{
    /// <summary>A layered forest of weathered sandstone towers on the western coast.</summary>
    public static class MistyPillarsVistaEditor
    {
        private const string ScenePath = "Assets/ProceduralWorld/Scenes/ProceduralWorldMilestone.unity";
        private const string AssetRoot = "Assets/ProceduralWorld/Generated/MistyPillars";
        private const string RootName = "Misty forest pillars vista";

        [MenuItem("Tools/Procedural World/Apply Misty Pillars Vista")]
        public static void ApplyToSavedWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding the vista.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                ApplyToScene(scene);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        public static void ApplyToScene(Scene scene)
        {
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
                if (!AssetDatabase.IsValidFolder(AssetRoot)) { Directory.CreateDirectory(AssetRoot); AssetDatabase.Refresh(); }
                foreach (GameObject oldRoot in scene.GetRootGameObjects())
                    if (oldRoot.name == RootName) Object.DestroyImmediate(oldRoot);
                Material rock = GetMaterial("M_PillarStone", "GoatDescent/Weathered Sandstone");
                rock.SetColor("_StoneColor", new Color(.52f, .45f, .35f));
                rock.SetColor("_ShadowStone", new Color(.17f, .205f, .20f));
                rock.SetColor("_MossColor", new Color(.11f, .20f, .073f));
                Material foliage = GetMaterial("M_ForestCanopy", "GoatDescent/Valley Foliage");
                foliage.SetColor("_Color", new Color(.19f, .27f, .19f));
                Material bark = GetMaterial("M_ValleyBark", "Standard");
                bark.color = new Color(.17f, .14f, .10f); bark.SetFloat("_Glossiness", .06f);
                Material fog = GetMaterial("M_ValleyVolume", "GoatDescent/Volumetric Valley Mist");
                fog.SetColor("_FogLight", new Color(.88f, .92f, .93f));
                fog.SetColor("_FogShadow", new Color(.50f, .61f, .66f));
                fog.SetFloat("_Density", .038f); fog.SetFloat("_Top", 250f); fog.SetFloat("_NoiseScale", .009f);
                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(new Vector3(400f, 0f, 1200f), Quaternion.Euler(0f, -90f, 0f));
                root.AddComponent<ValleyMistDepth>();
                // Front cliffs frame the hero. Background groups overlap instead of forming a grid.
                CreateTower(root.transform, new Vector3(-194f, -55f, -20f), 420f, 65f, 42f, 7, 1, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(193f, -45f, 34f), 369f, 70f, 53f, 13, 1, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(-66f, -34f, 165f), 335f, 62f, 47f, 21, 0, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(-140f, -40f, 440f), 321f, 39f, 34f, 27, 2, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(95f, -40f, 565f), 392f, 62f, 42f, 34, 2, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(168f, -42f, 340f), 300f, 38f, 31f, 42, 2, rock, foliage, bark);
                CreateTower(root.transform, new Vector3(22f, -40f, 380f), 251f, 28f, 24f, 51, 2, rock, foliage, bark);
                var random = new System.Random(28471);
                for (int i = 0; i < 19; i++)
                {
                    float z = 660f + i / 5 * 210f + Next(random, -65f, 65f);
                    float x = (i % 5 - 2) * 165f + Next(random, -45f, 45f);
                    float h = Next(random, 260f, 415f), radius = Next(random, 20f, 44f);
                    CreateTower(root.transform, new Vector3(x, -45f, z), h, radius, radius * Next(random, .65f, 1.1f), 100 + i, 3, rock, foliage, bark);
                }
                var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
                volume.name = "Rolling volumetric valley cloud";
                volume.transform.SetParent(root.transform, false);
                volume.transform.localPosition = new Vector3(0f, 150f, 590f);
                volume.transform.localScale = new Vector3(1800f, 600f, 2250f);
                Object.DestroyImmediate(volume.GetComponent<Collider>());
                var renderer = volume.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = fog; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                CreateCamera(root.transform, "MistyPillarsVista", new Vector3(0f, 285f, -300f), new Vector3(-12f, 235f, 235f), 39f);
                CreateCamera(root.transform, "MistyPillarsClose", new Vector3(35f, 305f, -130f), new Vector3(-63f, 242f, 170f), 42f);
                ApplyLighting(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("MISTY_PILLARS_READY towers=26 volumetricFog=true scene=" + scene.path);
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        private static void CreateCamera(Transform parent, string name, Vector3 position, Vector3 target, float fov)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.LookAt(parent.TransformPoint(target));
            Camera camera = obj.AddComponent<Camera>();
            camera.fieldOfView = fov; camera.nearClipPlane = .5f; camera.farClipPlane = 2600f;
            camera.clearFlags = CameraClearFlags.Skybox; camera.depthTextureMode = DepthTextureMode.Depth;
            camera.allowHDR = true; camera.allowMSAA = true; camera.enabled = false;
        }

        private static void ApplyLighting(Scene scene)
        {
            Material sky = GetMaterial("M_PillarDaylightSky", "GoatDescent/Overcast Valley Sky");
            sky.SetColor("_Horizon", new Color(.88f, .92f, .94f)); sky.SetColor("_Zenith", new Color(.72f, .81f, .87f));
            RenderSettings.skybox = sky;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.71f, .80f, .84f); RenderSettings.fogDensity = .0008f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.67f, .73f, .77f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .50f, .50f);
            RenderSettings.ambientGroundColor = new Color(.17f, .20f, .17f);
            RenderSettings.reflectionIntensity = .12f;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == "LifeDay Procedural Volumetric Cloudscape") sceneRoot.SetActive(false);
                foreach (Light light in sceneRoot.GetComponentsInChildren<Light>(true))
                {
                    if (light.name == "LifeDay Summit Sun Rays")
                    {
                        light.color = new Color(1f, .96f, .86f); light.intensity = 1.2f;
                        light.transform.rotation = Quaternion.Euler(52f, -125f, 0f);
                        light.shadows = LightShadows.Soft; light.shadowStrength = .7f; RenderSettings.sun = light;
                    }
                    if (light.name == "LifeDay Cold Directional Fill") light.intensity = .20f;
                }
            }
            QualitySettings.shadowDistance = 750f; QualitySettings.shadowResolution = ShadowResolution.High; QualitySettings.antiAliasing = 4;
        }

        private static void CreateTower(Transform parent, Vector3 position, float height, float rx, float rz, int seed, int detail, Material rock, Material foliage, Material bark)
        {
            var tower = new GameObject("Forest pillar " + seed);
            tower.transform.SetParent(parent, false); tower.transform.localPosition = position;
            tower.transform.localRotation = Quaternion.Euler(0f, seed * 19f % 360f, 0f);
            var stone = new MeshBuilder();
            AddRock(stone, Vector3.zero, height, rx, rz, seed, detail > 1 ? 38 : 58, detail > 1 ? 38 : 66);
            var random = new System.Random(seed * 197 + 41);
            for (int i = 0; i < (detail > 1 ? 2 : 4); i++)
            {
                float a = i * 2.39996f + seed;
                Vector3 offset = new Vector3(Mathf.Cos(a) * rx * .62f, -4f, Mathf.Sin(a) * rz * .62f);
                AddRock(stone, offset, height * Next(random, .46f, .82f), rx * Next(random, .22f, .4f), rz * Next(random, .24f, .44f), seed + i * 71, 28, 30);
            }
            MeshObject(tower.transform, "Stratified sandstone", stone, rock, "Cliff_" + seed);
            var stoneObject = tower.transform.Find("Stratified sandstone");
            var surface = stoneObject.gameObject.AddComponent<MeshCollider>();
            surface.sharedMesh = stoneObject.GetComponent<MeshFilter>().sharedMesh;
            if (seed == 21)
            {
                var marker = new GameObject("Pillar Goat Spawn").transform;
                marker.SetParent(tower.transform, false);
                Vector3 probe = tower.transform.TransformPoint(new Vector3(0f, height + 25f, 25f));
                Physics.SyncTransforms();
                if (!surface.Raycast(new Ray(probe, Vector3.down), out RaycastHit hit, 60f))
                    throw new InvalidOperationException("Pillar spawn must hit its rendered rock surface.");
                marker.position = hit.point + Vector3.up * .12f;
                marker.rotation = Quaternion.LookRotation(tower.transform.TransformDirection(Vector3.forward), Vector3.up);
            }
            var leaves = new MeshBuilder(); var wood = new MeshBuilder();
            int treeCount = detail == 3 ? 26 : detail == 2 ? 48 : 105;
            for (int i = 0; i < treeCount; i++)
            {
                float a = i * 2.399963f, r = Mathf.Sqrt((i + .5f) / treeCount) * .82f;
                float x = Mathf.Cos(a) * rx * r, z = Mathf.Sin(a) * rz * r;
                float y = height - 3f + (1f - r) * 7f + Noise(x * .035f, z * .035f, seed) * 2f;
                float treeHeight = Next(random, 8f, detail == 3 ? 18f : 29f) * (1.25f - r * .5f);
                if (seed == 21 && new Vector2(x, z - 25f).magnitude < 26f) continue;
                AddTree(leaves, wood, new Vector3(x, y, z), treeHeight, random, detail > 1);
                float undergrowth = Next(random, 3f, 6.5f);
                leaves.AddCrown(new Vector3(x, y + 1f, z), new Vector3(undergrowth * 1.5f, undergrowth * .7f, undergrowth), seed + i, CanopyTint(random), 9, 6);
            }
            int shrubs = detail > 1 ? 35 : 135;
            for (int i = 0; i < shrubs; i++)
            {
                float t = Next(random, .25f, .99f), angle = Next(random, 0f, Mathf.PI * 2f);
                Vector3 p = RockPoint(t, angle, height, rx, rz, seed);
                p.x *= .965f; p.z *= .965f;
                float size = Next(random, 1.8f, 3.8f) * Mathf.Lerp(.7f, 1.4f, t);
                for (int lobe = 0; lobe < 4; lobe++)
                {
                    Vector3 offset = new Vector3(Next(random, -size, size), Next(random, -size * .6f, size * .6f), Next(random, -size, size));
                    leaves.AddCrown(p + offset, new Vector3(size, size * .85f, size), seed + i * 7 + lobe, CanopyTint(random), 8, 5);
                }
            }
            // Connected vegetation curtains follow a few fissures below the crown.
            // This keeps the upper cliff forested without covering every face uniformly.
            int patchCount = detail > 1 ? 105 : 330;
            for (int i = 0; i < patchCount; i++)
            {
                int fissure = i % 5;
                float t = Next(random, .64f, 1f);
                float angle = fissure * 1.256637f + seed * .41f + Mathf.Sin(t * 12f + fissure) * .13f + Next(random, -.22f, .22f);
                Vector3 p = RockPoint(t, angle, height, rx, rz, seed);
                p.x *= .975f; p.z *= .975f;
                float size = Next(random, 3f, 6.2f) * Mathf.Lerp(.7f, 1.2f, (t - .64f) / .36f);
                Color tint = CanopyTint(random);
                for (int lobe = 0; lobe < 3; lobe++)
                {
                    Vector3 offset = new Vector3(Next(random, -size, size), Next(random, -size, size), Next(random, -size, size));
                    leaves.AddCrown(p + offset, new Vector3(size, size * .82f, size * .9f), seed * 43 + i * 5 + lobe, tint, 8, 5);
                }
            }
            MeshObject(tower.transform, "Dense broadleaf forest and ledge shrubs", leaves, foliage, "Forest_" + seed);
            MeshObject(tower.transform, "Tree trunks and branches", wood, bark, "Branches_" + seed);
        }

        private static Color CanopyTint(System.Random random) { float f = Next(random, .68f, 1.25f); return new Color(.48f * f, .65f * f, .31f * f, 1f); }
        private static void AddTree(MeshBuilder leaves, MeshBuilder wood, Vector3 origin, float height, System.Random random, bool distant)
        {
            Vector3 bend = new Vector3(Next(random, -2f, 2f), height * .73f, Next(random, -2f, 2f));
            wood.AddBranch(origin, origin + bend, height * .035f, height * .012f, 5);
            for (int b = 0; b < (distant ? 4 : 6); b++)
            {
                float angle = b * 2.39996f + Next(random, -.3f, .3f), span = height * Next(random, .17f, .32f);
                Vector3 crown = origin + bend + new Vector3(Mathf.Cos(angle) * span, Next(random, -height * .1f, height * .17f), Mathf.Sin(angle) * span);
                wood.AddBranch(origin + bend * .6f, crown, height * .016f, height * .005f, 4);
                float size = height * Next(random, .20f, .31f);
                leaves.AddCrown(crown, new Vector3(size * 1.25f, size * .85f, size), random.Next(100000), CanopyTint(random), distant ? 8 : 11, distant ? 5 : 7);
            }
        }

        private static Vector3 RockPoint(float t, float angle, float height, float rx, float rz, int seed)
        {
            float phase = seed * .73f;
            float outline = 1f + .14f * Mathf.Sin(angle * 3f + phase) + .09f * Mathf.Sin(angle * 7f - phase * 1.7f);
            float grooves = .075f * Mathf.Sin(angle * 19f + phase) + .04f * Mathf.Sin(angle * 37f - phase);
            float erosion = .045f * Noise(Mathf.Cos(angle) * 2.5f + t * .8f, Mathf.Sin(angle) * 2.5f + t * 7f, seed);
            float profile = 1.10f - .12f * t + .035f * Mathf.Sin(t * 13f + phase) - .055f * Mathf.Repeat(t * 11f + phase, 1f);
            profile += .18f * Mathf.Pow(1f - t, 4f);
            if (t > .94f) profile *= Mathf.Lerp(1f, .82f, (t - .94f) / .06f);
            float radius = profile * (outline + grooves + erosion);
            float leanX = Mathf.Sin(t * 2.5f + phase) * rx * .10f, leanZ = Mathf.Cos(t * 3.1f + phase) * rz * .09f;
            float terrace = Mathf.Sin(angle * 4f + phase) * 1.5f + Noise(t * 18f, angle * 3f, seed) * 2.4f;
            return new Vector3(Mathf.Cos(angle) * rx * radius + leanX, height * t + terrace * Mathf.SmoothStep(0f, 1f, t * 8f), Mathf.Sin(angle) * rz * radius + leanZ);
        }

        private static void AddRock(MeshBuilder builder, Vector3 origin, float height, float rx, float rz, int seed, int sides, int rings)
        {
            int first = builder.vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    builder.AddVertex(origin + RockPoint(t, a, height, rx, rz, seed), new Color(.75f + .2f * Noise(a, t * 3f, seed), t, .5f, 1f));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < sides; s++)
            {
                int a = first + r * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                builder.Triangle(a, c, b); builder.Triangle(b, c, d);
            }
            int top = builder.AddVertex(origin + new Vector3(0f, height + 7f, 0f), new Color(.9f, 1f, .5f, 1f));
            for (int s = 0; s < sides; s++) builder.Triangle(top, first + rings * (sides + 1) + s + 1, first + rings * (sides + 1) + s);
        }

        private static float Noise(float x, float y, int seed) => Mathf.PerlinNoise(x + seed * 3.71f, y + seed * 1.13f) * 2f - 1f;
        private static float Next(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private static Material GetMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Missing or unsupported shader: " + shaderName);
            string path = AssetRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            else material.shader = shader;
            EditorUtility.SetDirty(material); return material;
        }
        private static void MeshObject(Transform parent, string name, MeshBuilder builder, Material material, string assetName)
        {
            string path = AssetRoot + "/" + assetName + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = assetName }; AssetDatabase.CreateAsset(mesh, path); }
            if (assetName.StartsWith("Cliff_")) builder.FlattenFaces();
            mesh.Clear(); mesh.indexFormat = builder.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(builder.vertices); mesh.SetColors(builder.colors); mesh.SetTriangles(builder.triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private sealed class MeshBuilder
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Color> colors = new List<Color>();
            public readonly List<int> triangles = new List<int>();
            public int AddVertex(Vector3 p, Color color) { int i = vertices.Count; vertices.Add(p); colors.Add(color); return i; }
            public void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            public void FlattenFaces()
            {
                var positions = new List<Vector3>(triangles.Count);
                var tints = new List<Color>(triangles.Count);
                for (int i = 0; i < triangles.Count; i++) { positions.Add(vertices[triangles[i]]); tints.Add(colors[triangles[i]]); triangles[i] = i; }
                vertices.Clear(); vertices.AddRange(positions); colors.Clear(); colors.AddRange(tints);
            }
            public void AddCrown(Vector3 center, Vector3 scale, int seed, Color tint, int sides, int rings)
            {
                int first = vertices.Count;
                for (int r = 0; r <= rings; r++)
                {
                    float lat = r / (float)rings * Mathf.PI;
                    for (int s = 0; s <= sides; s++)
                    {
                        float angle = s / (float)sides * Mathf.PI * 2f;
                        Vector3 u = new Vector3(Mathf.Sin(lat) * Mathf.Cos(angle), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(angle));
                        float wobble = 1f + .18f * Noise(u.x * 4f + u.y, u.z * 4f + u.y * 3f, seed);
                        float shade = .8f + .2f * u.y + .1f * Noise(u.x * 8f, u.z * 8f, seed);
                        AddVertex(center + Vector3.Scale(u, scale) * wobble, new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f));
                    }
                }
                for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = first + r * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                    Triangle(a, b, c); Triangle(b, d, c);
                }
            }
            public void AddBranch(Vector3 start, Vector3 end, float r0, float r1, int sides)
            {
                Vector3 dir = (end - start).normalized;
                Vector3 right = Vector3.Cross(dir, Mathf.Abs(dir.y) > .9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 forward = Vector3.Cross(dir, right); int first = vertices.Count;
                for (int r = 0; r < 2; r++)
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    AddVertex((r == 0 ? start : end) + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * (r == 0 ? r0 : r1), Color.white);
                }
                for (int s = 0; s < sides; s++)
                {
                    int a = first + s, b = first + (s + 1) % sides;
                    Triangle(a, b, a + sides); Triangle(b, b + sides, a + sides);
                }
            }
        }
    }
}
