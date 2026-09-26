using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>A short sculpted cliff for learning the real four-hoof balance controller.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class BalanceTestBootstrap : MonoBehaviour
    {
        private const float StartZ = -20f, EndZ = 40f;
        private static readonly float[] LedgePositions = { -20f, -11f, -9f, -7.5f, -4.4f, -2.8f,
            .6f, 2.1f, 5.5f, 7.4f, 10.4f, 12.1f, 16f, 19f, 40f };
        private static readonly float[] LedgeHeights = { 39f, 36f, 35.7f, 29.6f, 29.1f, 22.9f,
            22.4f, 16.1f, 15.6f, 9.8f, 9.4f, 4.1f, 3.6f, 1f, 1f };
        private GoatSlopeBalance balance;
        private Rigidbody goatBody;
        private GoatGroundDetector ground;
        private GUIStyle heading, body;
        private bool caveReached;
        private Light caveLight;
        private readonly List<FragileStone> fragileStones = new List<FragileStone>();

        private sealed class FragileStone
        {
            public Transform Transform;
            public MeshCollider Collider;
            public MeshRenderer Renderer;
            public Vector3 Start;
            public Color Color;
            public float TouchedAt = -100f;
        }

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
            BuildScenery(art);
            BuildCaveApproach(art);
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
            UpdateFragileStones();
            if (goatBody && goatBody.position.z < -10f && caveReached)
            {
                caveReached = false;
                if (caveLight) caveLight.intensity = 2f;
            }
            if (goatBody && !caveReached && goatBody.position.z > 34.5f
                && Mathf.Abs(goatBody.position.x) < 1.3f && goatBody.position.y > .6f)
                ReachCave();
            if (!goatBody || !ground || !ground.IsGrounded || goatBody.isKinematic) return;
            Vector3 tangent = Vector3.ProjectOnPlane(goatBody.linearVelocity, ground.GroundNormal);
            bool braking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float limit = braking ? 3.2f : Input.GetKey(KeyCode.W) ? 8f : 5.5f;
            if (tangent.magnitude > limit)
                goatBody.linearVelocity += tangent.normalized * (limit - tangent.magnitude);
        }

        private void UpdateFragileStones()
        {
            foreach (var stone in fragileStones)
            {
                if (stone.TouchedAt < 0f && ground && ground.IsGrounded
                    && ground.GroundHit.collider == stone.Collider)
                    stone.TouchedAt = Time.time;
                if (stone.TouchedAt < 0f) continue;
                float elapsed = Time.time - stone.TouchedAt;
                if (elapsed >= 4.5f)
                {
                    stone.Transform.position = stone.Start;
                    stone.Collider.enabled = true;
                    stone.Renderer.material.color = stone.Color;
                    stone.TouchedAt = -100f;
                    continue;
                }
                stone.Renderer.material.color = Color.Lerp(stone.Color, new Color(.85f, .43f, .24f),
                    Mathf.PingPong(elapsed * 5f, 1f));
                if (elapsed < 1.15f) continue;
                stone.Collider.enabled = false;
                stone.Transform.position = stone.Start + Vector3.down * Mathf.Min(14f, (elapsed - 1.15f) * 12f);
            }
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
            // Broad places to land alternate with short, steep rock faces.
            // Keep this a single heightfield so every ledge is solid under the hooves.
            for (int i = 1; i < LedgePositions.Length; i++)
                if (z <= LedgePositions[i])
                    return Mathf.Lerp(LedgeHeights[i - 1], LedgeHeights[i],
                        Mathf.InverseLerp(LedgePositions[i - 1], LedgePositions[i], z));
            return 1f;
        }

        private static float WidthAt(float z)
        {
            float width = Mathf.Lerp(5.2f, 2.25f, Smooth(-12f, -2f, z));
            width = Mathf.Lerp(width, .84f, Smooth(-2f, 5f, z));
            width = Mathf.Lerp(width, 1.45f, Smooth(10f, 18f, z));
            float shelf = .7f * Lobe(0f, z, 0f, -5.4f, 1f, 1.3f)
                + .65f * Lobe(0f, z, 0f, -.5f, 1f, 1.3f)
                + .55f * Lobe(0f, z, 0f, 7.1f, 1f, 1.3f)
                + .8f * Lobe(0f, z, 0f, 11.5f, 1f, 1.5f);
            return Mathf.Lerp(width, 5f, Smooth(18f, 23f, z))
                + Mathf.Sin(z * .69f) * .18f + shelf;
        }

        public static float HeightAt(float x, float z)
        {
            float center = Smooth(7f, 18f, z) * Mathf.Sin((z - 7f) * .28f) * 1.15f;
            float across = x - center;
            float width = WidthAt(z);
            float protrusions = .95f * Lobe(x, z, 1.15f, -5f, .85f, 1.35f)
                + .72f * Lobe(x, z, -.38f, 3.5f, .62f, 1.0f)
                + .92f * Lobe(x, z, .75f, 12.8f, .88f, 1.4f)
                + .48f * Lobe(x, z, -1.6f, 16f, 1.0f, 1.35f);
            // The middle cuts straight down. Alternating side shelves give a slower
            // zigzag line and the low rock noses can be jumped from deliberately.
            float chute = -.65f * Lobe(x, z, 0f, -7.7f, .8f, 1.2f)
                - .8f * Lobe(x, z, 0f, -3f, .75f, 1.1f)
                - .7f * Lobe(x, z, 0f, 4f, .68f, 1.2f)
                - .75f * Lobe(x, z, 0f, 9.2f, .75f, 1.1f);
            float sideShelves = .48f * Lobe(x, z, -1.5f, -5.4f, .85f, 1.15f)
                + .48f * Lobe(x, z, 1.2f, -.5f, .8f, 1.1f)
                + .4f * Lobe(x, z, -1.0f, 7.1f, .65f, 1.05f)
                + .45f * Lobe(x, z, 1.25f, 11.5f, .75f, 1.15f);
            float bank = Smooth(8f, 12f, z) * (1f - Smooth(18f, 22f, z)) * across * .34f;
            float rough = (Mathf.PerlinNoise(x * .65f + 13f, z * .48f + 7f) - .5f) * .23f
                * Smooth(-11f, -7f, z) * (1f - Smooth(19f, 23f, z));
            float weathering = (Mathf.PerlinNoise(x * .19f + 27f, z * .13f + 4f) - .5f) * 1.15f
                + Mathf.Sin(z * .37f + x * .17f) * .21f;
            float mountainSide = Mathf.Max(0f, -across - width);
            float cliffSide = Mathf.Max(0f, across - width);
            float highRidge = Smooth(0f, 3.5f, mountainSide) * 10f
                + Smooth(3f, 14f, mountainSide) * 11f
                + Smooth(2f, 10f, mountainSide) * Mathf.Sin(z * .42f + x * .29f) * 2.8f;
            float brokenFace = Smooth(0f, 2.8f, cliffSide) * 14f
                + cliffSide * .44f
                - Smooth(2f, 10f, cliffSide) * Mathf.Sin(z * .52f - x * .24f) * 2f;
            float staggeredZ = z + Mathf.Sin(x * .7f + z * .31f) * .3f
                + (Mathf.PerlinNoise(x * .48f + 19f, z * .22f + 13f) - .5f) * .22f;
            float abyss = Smooth(18.5f, 19.5f, z) * (1f - Smooth(29.2f, 30.2f, z));
            return BaseHeight(staggeredZ) + protrusions + chute + sideShelves + bank + rough
                + weathering * Smooth(width, width + 4f, Mathf.Abs(across))
                + (highRidge - brokenFace) * (1f - abyss) - abyss * 24f;
        }

        private void BuildCliff(MountainArt art)
        {
            const int columns = 176, rows = 288;
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
            var mesh = art.MakeMesh("Test cliff with staggered rock ledges", vertices, triangles);
            var shader = Resources.Load<Shader>("AlpineSurface");
            var material = shader ? art.Own(new Material(shader)) : art.Stone;
            if (shader)
            {
                material.SetColor("_SnowColor", new Color(.53f, .59f, .60f));
                material.SetColor("_StoneColor", new Color(.28f, .34f, .38f));
            }
            var surface = art.MeshPart(transform, "BALANCE TEST — five solid rock ledges", mesh,
                Vector3.zero, material, true);
            surface.AddComponent<MountainSlopeSurface>();
        }

        private void BuildScenery(MountainArt art)
        {
            var pine = Resources.Load<GameObject>("MainModels/Tree_Fir");
            if (pine)
            {
                PlaceScenery(pine, -9f, -7f, 5f, "Fir beside first ledge");
                PlaceScenery(pine, 9.5f, -1f, 4.8f, "Fir beside second ledge");
                PlaceScenery(pine, -8.7f, 7f, 5.5f, "Fir beside lower ledge");
                PlaceScenery(pine, 9f, 15f, 5f, "Fir before the chasm");
            }
            var cliff = Resources.Load<GameObject>("MainModels/Cliff_Broken");
            if (cliff)
            {
                PlaceScenery(cliff, -15f, -5f, 13f, "Broken mountain buttress");
                PlaceScenery(cliff, -13f, 8f, 11f, "Jagged rock face");
            }
        }

        private void BuildCaveApproach(MountainArt art)
        {
            // Separate, solid stone shelves above the void. The gaps require short,
            // deliberate hops rather than a single long jump.
            AddLanding(art, -1.15f, 20.2f, 1.9f, 2.25f, 1);
            AddLanding(art, .8f, 22.7f, 1.65f, 2.15f, 2);
            AddLanding(art, -1.05f, 25.15f, 1.4f, 2.1f, 3);
            AddLanding(art, .75f, 27.6f, 1.25f, 2.25f, 4);
            AddLanding(art, 0f, 29.7f, 1.1f, 2.7f, 5);

            var caveStone = art.Mat(new Color(.25f, .29f, .32f));
            var caveDark = art.Mat(new Color(.035f, .055f, .06f));
            art.Rock(transform, new Vector3(-2.1f, 2f, 32.2f), new Vector3(2.3f, 4f, 2.3f), 22, caveStone, false);
            art.Rock(transform, new Vector3(2.1f, 2f, 32.2f), new Vector3(2.3f, 4f, 2.3f), 23, caveStone, false);
            art.Rock(transform, new Vector3(0f, 4f, 32.2f), new Vector3(4.4f, 2f, 2.5f), 24, caveStone, false);
            BuildCaveShell(art, caveStone);
            var mouth = new List<Vector3> { new Vector3(-1.3f, 1f, 38f), new Vector3(1.3f, 1f, 38f),
                new Vector3(1.3f, 3.35f, 38f), new Vector3(0f, 3.65f, 38f), new Vector3(-1.3f, 3.35f, 38f) };
            art.MeshPart(transform, "Dark cave back wall", art.MakeMesh("Cave depth", mouth,
                new List<int> { 0, 2, 1, 0, 3, 2, 0, 4, 3 }), Vector3.zero, caveDark, true);
            var crystal = art.Mat(new Color(.3f, .8f, .87f), .7f);
            crystal.EnableKeyword("_EMISSION");
            crystal.SetColor("_EmissionColor", new Color(.08f, .55f, .7f));
            AddCrystal(art, crystal, new Vector3(-1.5f, 1.7f, 34.4f), 1.25f);
            AddCrystal(art, crystal, new Vector3(1.45f, 1.65f, 35.2f), 1.05f);
            AddCrystal(art, crystal, new Vector3(-.8f, 3.35f, 36f), .8f);
            AddCrystal(art, crystal, new Vector3(.8f, 3.4f, 36.7f), .7f);
            caveLight = new GameObject("Crystal cave glow").AddComponent<Light>();
            caveLight.transform.SetParent(transform, false);
            caveLight.transform.position = new Vector3(0f, 2.7f, 34.8f);
            caveLight.type = LightType.Point;
            caveLight.range = 9f;
            caveLight.intensity = 2f;
            caveLight.color = new Color(.23f, .77f, .92f);
            caveLight.shadows = LightShadows.None;
        }

        private void BuildCaveShell(MountainArt art, Material stone)
        {
            const int lengthSegments = 10, archSegments = 12;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int row = 0; row <= lengthSegments; row++)
            {
                float z = Mathf.Lerp(32f, 38f, (float)row / lengthSegments);
                for (int side = 0; side <= archSegments; side++)
                {
                    float angle = Mathf.PI * side / archSegments;
                    float jag = 1f + .06f * Mathf.Sin(row * 2.4f + side * 3.1f);
                    vertices.Add(new Vector3(Mathf.Cos(angle) * 2.35f * jag,
                        1f + Mathf.Sin(angle) * 3.15f * jag, z));
                }
            }
            for (int row = 0; row < lengthSegments; row++)
            for (int side = 0; side < archSegments; side++)
            {
                int a = row * (archSegments + 1) + side, b = a + archSegments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(a);
                triangles.Add(b + 1); triangles.Add(b); triangles.Add(a + 1);
            }
            art.MeshPart(transform, "Walk-in rock cave", art.MakeMesh("Rough cave vault", vertices, triangles),
                Vector3.zero, stone, true);
        }

        private void AddCrystal(MountainArt art, Material material, Vector3 position, float height)
        {
            var vertices = new List<Vector3> { new Vector3(0f, height, 0f),
                new Vector3(-.25f, 0f, -.2f), new Vector3(.24f, 0f, -.16f),
                new Vector3(.2f, 0f, .23f), new Vector3(-.2f, 0f, .2f) };
            var triangles = new List<int> { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
                1, 2, 3, 1, 3, 4 };
            art.MeshPart(transform, "Glowing cave crystal", art.MakeMesh("Cave crystal", vertices, triangles),
                position, material);
        }

        private void AddLanding(MountainArt art, float x, float z, float top, float diameter, int seed)
        {
            const int sides = 9;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            vertices.Add(new Vector3(x, top, z));
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float radius = diameter * .5f * (1f + Mathf.Sin(seed * 2.3f + i * 3.7f) * .1f);
                vertices.Add(new Vector3(x + Mathf.Cos(angle) * radius, top, z + Mathf.Sin(angle) * radius));
                vertices.Add(new Vector3(x + Mathf.Cos(angle) * radius * 1.18f, top - 1.35f,
                    z + Mathf.Sin(angle) * radius * 1.18f));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = 1 + i * 2, b = 1 + ((i + 1) % sides) * 2;
                triangles.Add(0); triangles.Add(b); triangles.Add(a);
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            var rock = art.MeshPart(transform, "Jumpable stone shelf " + seed,
                art.MakeMesh("Irregular landing stone", vertices, triangles), Vector3.zero,
                art.Own(new Material(art.Stone)), true);
            rock.AddComponent<MountainSlopeSurface>();
            if (seed >= 3 && seed <= 4)
            {
                var renderer = rock.GetComponent<MeshRenderer>();
                fragileStones.Add(new FragileStone { Transform = rock.transform,
                    Collider = rock.GetComponent<MeshCollider>(), Renderer = renderer,
                    Start = rock.transform.position, Color = renderer.material.color });
            }
        }

        private void PlaceScenery(GameObject source, float x, float z, float height, string name)
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
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            instance.transform.position += new Vector3(x - bounds.center.x,
                HeightAt(x, z) - bounds.min.y - height * .06f, z - bounds.center.z);
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }

        public void ReachCave()
        {
            caveReached = true;
            if (caveLight) caveLight.intensity = 6f;
            balance?.GetComponent<GoatJumpController>()?.AnnounceTrick("THE GOAT FOUND THE CRYSTAL CAVE!");
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
            GUI.DrawTexture(new Rect(18f, 18f, width, 246f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 28f, width - 25f, 33f), "ЛАБОРАТОРИЯ БАЛАНСА", heading);
            float z = balance.transform.position.z;
            string section = z < -9f ? "СТАРТОВАЯ ПОЛКА" : z < -4.4f ? "ПЕРВЫЙ УСТУП"
                : z < .6f ? "ВТОРОЙ УСТУП" : z < 5.5f ? "УЗКИЙ ТРЕТИЙ УСТУП"
                : z < 10.4f ? "ЧЕТВЁРТЫЙ УСТУП" : z < 18f ? "НИЖНИЕ ПОЛКИ"
                : caveReached ? "ПЕЩЕРА ДОСТИГНУТА!" : "ПРЫГАЙ ПО КАМНЯМ К ПЕЩЕРЕ";
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
            GUI.Label(new Rect(34f, 181f, 420f, 26f), z > 18f
                ? "Камни осыпаются! Space — короткий прыжок   R — снова"
                : "A/D — ищи полки   Space — прыгай   Ctrl — тормози   R — снова", body);
            var grip = balance.GetComponent<GoatGripController>();
            if (grip)
            {
                GUI.Label(new Rect(34f, 211f, 420f, 24f), "F — СУПЕРКОПЫТА: цепляйся за скалу при срыве", body);
                GUI.color = new Color(.19f, .28f, .31f);
                GUI.DrawTexture(new Rect(34f, 240f, 404f, 9f), Texture2D.whiteTexture);
                GUI.color = new Color(.38f, .92f, .85f);
                GUI.DrawTexture(new Rect(34f, 240f, 404f * grip.SuperCharge01, 9f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }

}
