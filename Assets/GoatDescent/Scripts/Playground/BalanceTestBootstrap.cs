using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>A short sculpted cliff for learning the real four-hoof balance controller.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class BalanceTestBootstrap : MonoBehaviour
    {
        private const float StartZ = -20f, EndZ = 30f;
        private GoatSlopeBalance balance;
        private Rigidbody goatBody;
        private GoatGroundDetector ground;
        private GUIStyle heading, body;

        private void Start()
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            RenderSettings.fog = true;
            QualitySettings.shadows = ShadowQuality.Disable;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .007f;
            RenderSettings.fogColor = new Color(.58f, .67f, .72f);
            RenderSettings.ambientLight = new Color(.48f, .52f, .56f);
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(.43f, .57f, .68f));
                sky.SetColor("_GroundColor", new Color(.34f, .45f, .52f));
                sky.SetFloat("_Exposure", 1f);
                RenderSettings.skybox = sky;
            }
            var sun = new GameObject("Warm test light").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(42f, -45f, 0f);
            sun.color = new Color(1f, .91f, .77f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.None;

            var art = new MountainArt(gameObject);
            BuildCliff(art);
            BuildDistantRidges(art);
            BuildScenery(art);
            Physics.SyncTransforms();
            GoatPlayerFactory.Create(new Vector3(0f, HeightAt(0f, -11.4f) + .22f, -11.4f));
            Camera.main?.GetComponent<ThirdPersonGoatCamera>()?.SetTestCliffView();
            balance = FindFirstObjectByType<GoatSlopeBalance>();
            goatBody = balance ? balance.GetComponent<Rigidbody>() : null;
            ground = balance ? balance.GetComponent<GoatGroundDetector>() : null;
            gameObject.AddComponent<MovementTestTimeControls>();
        }

        private void FixedUpdate()
        {
            if (!goatBody || !ground || !ground.IsGrounded || goatBody.isKinematic) return;
            Vector3 tangent = Vector3.ProjectOnPlane(goatBody.linearVelocity, ground.GroundNormal);
            bool braking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float limit = braking ? 3.2f : Input.GetKey(KeyCode.W) ? 8f : 5.5f;
            if (tangent.magnitude > limit)
                goatBody.linearVelocity += tangent.normalized * (limit - tangent.magnitude);
        }

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
        private static float Lobe(float x, float z, float centerX, float centerZ, float widthX, float widthZ)
            => Mathf.Exp(-Mathf.Pow((x - centerX) / widthX, 2f) - Mathf.Pow((z - centerZ) / widthZ, 2f));

        private static float BaseHeight(float z)
        {
            if (z < -10f) return 36f + (-10f - z) * .35f;
            if (z < -2f) return 36f - (z + 10f) * 1.375f;
            if (z < 9f) return 25f - (z + 2f) * 1.273f;
            if (z < 18f) return 11f - (z - 9f) * 1.111f;
            return 1f;
        }

        private static float WidthAt(float z)
        {
            float width = Mathf.Lerp(5.2f, 2.25f, Smooth(-12f, -2f, z));
            width = Mathf.Lerp(width, .84f, Smooth(-2f, 5f, z));
            width = Mathf.Lerp(width, 1.45f, Smooth(10f, 18f, z));
            return Mathf.Lerp(width, 5f, Smooth(18f, 23f, z)) + Mathf.Sin(z * .69f) * .18f;
        }

        public static float HeightAt(float x, float z)
        {
            float center = Smooth(7f, 18f, z) * Mathf.Sin((z - 7f) * .28f) * 1.15f;
            float across = x - center;
            float edge = Smooth(WidthAt(z), WidthAt(z) + 2.35f, Mathf.Abs(across));
            float protrusions = .95f * Lobe(x, z, 1.15f, -5f, .85f, 1.35f)
                + .72f * Lobe(x, z, -.38f, 3.5f, .62f, 1.0f)
                + .92f * Lobe(x, z, .75f, 12.8f, .88f, 1.4f)
                + .48f * Lobe(x, z, -1.6f, 16f, 1.0f, 1.35f);
            float bank = Smooth(8f, 12f, z) * (1f - Smooth(18f, 22f, z)) * across * .34f;
            float rough = (Mathf.PerlinNoise(x * .65f + 13f, z * .48f + 7f) - .5f) * .23f
                * Smooth(-11f, -7f, z) * (1f - Smooth(19f, 23f, z));
            float weathering = (Mathf.PerlinNoise(x * .19f + 27f, z * .13f + 4f) - .5f) * 1.15f
                + Mathf.Sin(z * .37f + x * .17f) * .21f;
            float flanks = Mathf.Max(0f, Mathf.Abs(across) - WidthAt(z) - 2.35f) * .48f;
            return BaseHeight(z) + protrusions + bank + rough
                + weathering * Smooth(WidthAt(z), WidthAt(z) + 4f, Mathf.Abs(across))
                - edge * 7.5f - flanks;
        }

        private void BuildCliff(MountainArt art)
        {
            const int columns = 176, rows = 180;
            var vertices = new List<Vector3>((columns + 1) * (rows + 1));
            var triangles = new List<int>(columns * rows * 6);
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(StartZ, EndZ, (float)row / rows);
                for (int col = 0; col <= columns; col++)
                {
                    float x = Mathf.Lerp(-28f, 28f, (float)col / columns);
                    vertices.Add(new Vector3(x, HeightAt(x, z), z));
                }
            }
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int a = row * (columns + 1) + col;
                int b = a + columns + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            var mesh = art.MakeMesh("Test cliff with real rock noses and narrow spine", vertices, triangles);
            var shader = Resources.Load<Shader>("AlpineSurface");
            var material = shader ? art.Own(new Material(shader)) : art.Stone;
            if (shader)
            {
                material.SetColor("_SnowColor", new Color(.53f, .59f, .60f));
                material.SetColor("_StoneColor", new Color(.28f, .34f, .38f));
            }
            var surface = art.MeshPart(transform, "BALANCE TEST — sculpted tilted cliff", mesh,
                Vector3.zero, material, true);
            surface.AddComponent<MountainSlopeSurface>();
        }

        private void BuildScenery(MountainArt art)
        {
            var pine = Resources.Load<GameObject>("MainModels/Tree_Pine_Windbent");
            foreach (float z in new[] { 14f, 22f })
            foreach (float side in new[] { -1f, 1f })
            {
                if (pine) PlaceScenery(pine, side * 21f, z + 2f, 9f,
                    "Wind-bent alpine pine", art.Mat(new Color(.16f, .29f, .26f)));
            }
        }

        private void BuildDistantRidges(MountainArt art)
        {
            const int columns = 64, rows = 30;
            var vertices = new List<Vector3>((columns + 1) * (rows + 1));
            var triangles = new List<int>(columns * rows * 6);
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(29f, 160f, (float)row / rows);
                for (int col = 0; col <= columns; col++)
                {
                    float x = Mathf.Lerp(-125f, 125f, (float)col / columns);
                    float peak = 34f * Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - 68f) / 25f, 2f));
                    float ridge = Mathf.Sin(z * .055f + x * .08f) * 7f
                        + (Mathf.PerlinNoise(x * .035f + 3f, z * .027f + 9f) - .5f) * 12f;
                    float y = -15f + peak + ridge;
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int a = row * (columns + 1) + col;
                int b = a + columns + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            var mesh = art.MakeMesh("Beyond the test cliff", vertices, triangles);
            var shader = Resources.Load<Shader>("AlpineSurface");
            var material = shader ? art.Own(new Material(shader)) : art.Stone;
            art.MeshPart(transform, "Distant alpine basin — scenery", mesh, Vector3.zero, material);
        }

        private void PlaceScenery(GameObject source, float x, float z, float height,
            string name, Material material)
        {
            var instance = Instantiate(source, transform);
            instance.name = name;
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Destroy(instance); return; }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            instance.transform.localScale *= height / Mathf.Max(.01f, bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            instance.transform.position += new Vector3(x - bounds.center.x,
                HeightAt(x, z) - bounds.min.y - height * .13f, z - bounds.center.z);
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }

        private void OnGUI()
        {
            if (!balance) return;
            heading ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.99f, .85f, .57f) } };
            body ??= new GUIStyle(GUI.skin.label) { fontSize = 16,
                normal = { textColor = Color.white } };
            float width = 450f;
            GUI.color = new Color(.04f, .08f, .11f, .84f);
            GUI.DrawTexture(new Rect(18f, 18f, width, 203f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 28f, width - 25f, 33f), "ЛАБОРАТОРИЯ БАЛАНСА", heading);
            float z = balance.transform.position.z;
            string section = z < -9f ? "ШИРОКИЙ СТАРТ" : z < -2f ? "НАКЛОН И КАМЕННЫЙ ВЫСТУП"
                : z < 9f ? "УЗКИЙ ГРЕБЕНЬ" : z < 18f ? "БОКОВОЙ УКЛОН" : "ФИНИШНАЯ ПОЛКА";
            GUI.Label(new Rect(34f, 65f, 415f, 25f), section, body);
            GUI.Label(new Rect(34f, 92f, 415f, 25f),
                $"Баланс {balance.Balance01 * 100f:0}%   •   Опора {balance.HoovesHolding}/4", body);
            GUI.color = new Color(.22f, .29f, .32f);
            GUI.DrawTexture(new Rect(34f, 122f, 404f, 15f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(.95f, .31f, .24f), new Color(.45f, .83f, .59f), balance.Balance01);
            GUI.DrawTexture(new Rect(34f, 122f, 404f * balance.Balance01, 15f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string[] feet = { "ПЛ", "ПП", "ЗЛ", "ЗП" };
            for (int i = 0; i < 4; i++)
            {
                GUI.color = balance.HoofHolds(i) ? new Color(.57f, .93f, .65f) : new Color(.95f, .49f, .37f);
                GUI.Label(new Rect(35f + i * 98f, 145f, 85f, 23f), feet[i] + (balance.HoofHolds(i) ? " ●" : " ○"), body);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 181f, 420f, 26f), "WASD — шаг   Ctrl — медленно   A/D — выправить   R — снова", body);
        }
    }
}
