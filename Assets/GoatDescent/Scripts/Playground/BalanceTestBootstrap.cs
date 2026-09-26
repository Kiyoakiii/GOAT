using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>One short, readable cliff descent: three jumps and a quick retry.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class BalanceTestBootstrap : MonoBehaviour
    {
        private readonly List<Transform> ledges = new List<Transform>();
        private GoatSlopeBalance balance;
        private GoatGroundDetector ground;
        private bool finished;
        private Transform looseRock;
        private readonly List<MeshCollider> looseColliders = new List<MeshCollider>();
        private Vector3 looseRockStart;
        private float looseRockTouchedAt = -100f;
        private GUIStyle title, text;

        private void Start()
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            QualitySettings.shadows = ShadowQuality.Disable;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .008f;
            RenderSettings.fogColor = new Color(.48f, .60f, .65f);
            RenderSettings.ambientLight = new Color(.48f, .52f, .54f);
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(.34f, .53f, .66f));
                sky.SetColor("_GroundColor", new Color(.22f, .35f, .40f));
                RenderSettings.skybox = sky;
            }
            var sun = new GameObject("Warm mountain daylight").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(35f, -38f, 0f);
            sun.color = new Color(1f, .94f, .82f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.None;

            var art = new MountainArt(gameObject);
            BuildCliffWall(art);
            var flatStone = Resources.Load<GameObject>("KenneyNature/stone_smallFlatA");
            var broadStone = Resources.Load<GameObject>("KenneyNature/rock_largeA");
            var brokenStone = Resources.Load<GameObject>("KenneyNature/rock_largeB");
            AddReadyLedge(flatStone, -3f, 12f, 2.5f, false, 0);
            AddReadyLedge(broadStone ? broadStone : flatStone, .5f, 9.7f, 1.65f, false, 1);
            AddReadyLedge(flatStone, 3.7f, 7.55f, 1.45f, true, 2);
            AddReadyLedge(brokenStone ? brokenStone : flatStone, 7f, 5.4f, 1.7f, false, 3);
            DressWithAssets();
            Physics.SyncTransforms();

            GoatPlayerFactory.Create(new Vector3(-2.8f, 12.22f, -3.3f), -18f);
            Camera.main?.GetComponent<ThirdPersonGoatCamera>()?.SetTestCliffView();
            balance = FindFirstObjectByType<GoatSlopeBalance>();
            ground = balance ? balance.GetComponent<GoatGroundDetector>() : null;
        }

        private void Update()
        {
            if (!balance || !ground) return;
            if (balance.transform.position.z < -2f)
            {
                finished = false;
                if (looseRockTouchedAt >= 0f) RestoreLooseRock();
            }
            if (looseRock)
            {
                if (looseRockTouchedAt < 0f && ground.IsGrounded
                    && ground.GroundHit.collider.transform.IsChildOf(looseRock))
                    looseRockTouchedAt = Time.time;
                if (looseRockTouchedAt >= 0f)
                {
                    float elapsed = Time.time - looseRockTouchedAt;
                    if (elapsed > 4f) RestoreLooseRock();
                    else if (elapsed > 1.2f)
                    {
                        foreach (var collider in looseColliders)
                            collider.enabled = false;
                        looseRock.position = looseRockStart + Vector3.down * Mathf.Min(10f, (elapsed - 1.2f) * 12f);
                    }
                }
            }
            if (!finished && ground.IsGrounded && ledges.Count == 4
                && ground.GroundHit.collider.transform.IsChildOf(ledges[3]))
            {
                finished = true;
                balance.GetComponent<GoatSpectacle>()?.Finish();
            }
        }

        private void RestoreLooseRock()
        {
            looseRock.position = looseRockStart;
            foreach (var collider in looseColliders) collider.enabled = true;
            looseRockTouchedAt = -100f;
        }

        private void BuildCliffWall(MountainArt art)
        {
            const int columns = 22, rows = 65;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(-9f, 13f, (float)row / rows);
                for (int col = 0; col <= columns; col++)
                {
                    float y = Mathf.Lerp(-16f, 19f, (float)col / columns);
                    float fractured = (Mathf.PerlinNoise(z * .65f + 8f, y * .43f + 11f) - .5f) * .85f;
                    float x = -4.2f + fractured + Mathf.Sin(z * 1.2f + y * .55f) * .18f;
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int a = row * (columns + 1) + col, b = a + columns + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(a);
                triangles.Add(b + 1); triangles.Add(b); triangles.Add(a + 1);
            }
            var wall = art.MeshPart(transform, "Solid steep cliff for hoof catches",
                art.MakeMesh("Fractured vertical cliff", vertices, triangles), Vector3.zero,
                art.Mat(new Color(.19f, .24f, .27f)), true);
            wall.GetComponent<MeshRenderer>().enabled = false;
            wall.AddComponent<MountainSlopeSurface>();
        }

        private void AddReadyLedge(GameObject source, float z, float top, float length,
            bool fragile, int index)
        {
            if (!source) return;
            var ledge = Instantiate(source, transform);
            ledge.name = "Real rock ledge " + index;
            ledge.transform.rotation = Quaternion.Euler(0f, index * 22f, 0f);
            var renderers = ledge.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Destroy(ledge); return; }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            ledge.transform.localScale *= length / Mathf.Max(.01f, bounds.size.z);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            ledge.transform.position += new Vector3(-3.35f - bounds.center.x,
                top - bounds.max.y, z - bounds.center.z);
            foreach (var existing in ledge.GetComponentsInChildren<Collider>()) existing.enabled = false;
            foreach (var filter in ledge.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh) continue;
                var surface = filter.gameObject.AddComponent<MeshCollider>();
                surface.sharedMesh = filter.sharedMesh;
                filter.gameObject.AddComponent<MountainSlopeSurface>();
                if (fragile) looseColliders.Add(surface);
            }
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (fragile) renderer.material.color = new Color(.75f, .45f, .25f);
                else renderer.material.color = new Color(.72f, .74f, .67f);
            }
            ledges.Add(ledge.transform);
            if (fragile)
            {
                looseRock = ledge.transform;
                looseRockStart = ledge.transform.position;
            }
        }

        private void DressWithAssets()
        {
            var tall = Resources.Load<GameObject>("MainModels/Cliff_Tall");
            var broken = Resources.Load<GameObject>("MainModels/Cliff_Broken");
            var wide = Resources.Load<GameObject>("MainModels/Cliff_Wide");
            var corner = Resources.Load<GameObject>("MainModels/Cliff_Corner");
            var rock = Resources.Load<GameObject>("MainModels/Rock_Large");
            var medium = Resources.Load<GameObject>("MainModels/Rock_Medium");
            var boulder = Resources.Load<GameObject>("MainModels/Mountain_Boulder");
            var pine = Resources.Load<GameObject>("MainModels/Tree_Pine_Windbent");
            if (tall)
            {
                PlaceAsset(tall, new Vector3(-6.6f, 8f, -6f), 11f, "Cliff model — upper wall");
                PlaceAsset(tall, new Vector3(-6.4f, 7f, 5f), 12f, "Cliff model — lower wall");
                PlaceAsset(tall, new Vector3(-6.7f, -4f, -5f), 14f, "Cliff model — deep drop");
                PlaceAsset(tall, new Vector3(-6.8f, -5f, 7f), 15f, "Cliff model — deep finish drop");
            }
            if (broken)
            {
                PlaceAsset(broken, new Vector3(-6.8f, 4f, -1f), 9f, "Broken cliff model");
                PlaceAsset(broken, new Vector3(-6.5f, 3f, 10f), 10f, "Broken cliff at finish");
                PlaceAsset(broken, new Vector3(-7.1f, -3f, 1f), 12f, "Broken rock below the jumps");
            }
            if (wide)
            {
                PlaceAsset(wide, new Vector3(-9f, 14f, -8f), 9f, "Wide rock shelf above start", 18f);
                PlaceAsset(wide, new Vector3(-9.5f, 12f, 8f), 10f, "Layered mountain wall", -17f);
            }
            if (corner)
            {
                PlaceAsset(corner, new Vector3(-7.8f, 12f, -2f), 8f, "Cracked mountain corner", 36f);
                PlaceAsset(corner, new Vector3(-8f, 10f, 12f), 8.5f, "Rock spur after finish", -24f);
            }
            if (rock)
            {
                PlaceAsset(rock, new Vector3(-4f, 10.6f, -3f), 2f, "Rock under first ledge");
                PlaceAsset(rock, new Vector3(-4f, 8.25f, .5f), 1.8f, "Rock under second ledge");
                PlaceAsset(rock, new Vector3(-4f, 6f, 3.7f), 1.7f, "Rock under third ledge");
                PlaceAsset(rock, new Vector3(-4f, 3.9f, 7f), 2f, "Rock under last ledge");
            }
            if (medium)
            {
                PlaceAsset(medium, new Vector3(-5.6f, 10f, -1f), 2.4f, "Jagged stone near first jump");
                PlaceAsset(medium, new Vector3(-5.8f, 7.5f, 2.9f), 2.2f, "Jagged stone near second jump");
            }
            if (boulder)
            {
                PlaceAsset(boulder, new Vector3(-10f, 19f, -5f), 4f, "Boulder on upper mountain");
                PlaceAsset(boulder, new Vector3(-11f, 18f, 7f), 4.5f, "Boulder over the finish");
            }
            if (pine)
            {
                PlaceAsset(pine, new Vector3(-15f, 22f, -5f), 6f, "Windbent pine above start");
                PlaceAsset(pine, new Vector3(-17f, 24f, 8f), 5.5f, "Windbent pine on high ridge");
            }
        }

        private void PlaceAsset(GameObject source, Vector3 center, float height, string name, float yaw = 0f)
        {
            var instance = Instantiate(source, transform);
            instance.name = name;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
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
            instance.transform.position += center - bounds.center;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (source.name.StartsWith("Cliff_"))
            {
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                {
                    if (!filter.sharedMesh) continue;
                    var surface = filter.gameObject.AddComponent<MeshCollider>();
                    surface.sharedMesh = filter.sharedMesh;
                    filter.gameObject.AddComponent<MountainSlopeSurface>();
                }
            }
        }

        private void OnGUI()
        {
            if (!balance) return;
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.98f, .86f, .62f) } };
            text ??= new GUIStyle(GUI.skin.label) { fontSize = 16,
                normal = { textColor = Color.white } };
            float z = balance.transform.position.z;
            string stage = finished ? "СКАЛА ПРОЙДЕНА!" : z < -.8f ? "ПРЫЖОК 1 / 3"
                : z < 2.2f ? "ПРЫЖОК 2 / 3" : "ПРЫЖОК 3 / 3";
            GUI.color = new Color(.05f, .09f, .12f, .78f);
            GUI.DrawTexture(new Rect(18f, 18f, 405f, 135f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32f, 28f, 380f, 35f), stage, title);
            GUI.Label(new Rect(32f, 66f, 380f, 24f), "Светлое — приземляйся    Рыжее — рыхлый камень", text);
            GUI.Label(new Rect(32f, 91f, 380f, 24f), "WASD — шаг   Space — прыжок   F — поймать стену", text);
            GUI.Label(new Rect(32f, 116f, 380f, 24f), "R — быстро повторить", text);
        }
    }
}
