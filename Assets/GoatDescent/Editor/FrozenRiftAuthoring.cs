using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent.EditorTools
{
    public static class FrozenRiftAuthoring
    {
        private const string Folder = "Assets/GoatDescent/Level/Static/FrozenRift";
        private const string Scene = "Assets/GoatDescent/Scenes/FrozenRift.unity";

        private static Material rock, bed, ridgeRock, snow, ridgeSnow, ice, needles, wood, mist;
        private static Mesh spike, shard, block, cluster, boulder, needleMesh, crownMesh, trunkMesh, mistMesh;
        private static Vector3 trailEnd;
        private static Vector3 viewEye;

        [MenuItem("Tools/GOAT/Статичные горы/Ледяной разлом — открыть")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Tools/GOAT/Статичные горы/Ледяной разлом — создать сцену")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var random = new System.Random(20261019);
            viewEye = RiftPoint(-1, 1.02f, -520f) + Vector3.up * 40f;

            rock = RockMaterial("ColdRock", new Color(1.92f, 1.8f, 1.62f), new Color(.36f, .47f, .6f), .3f, .03f);
            bed = RockMaterial("GlacierBed", new Color(1.66f, 1.72f, 1.8f), new Color(.35f, .5f, .64f), .55f, .03f);
            ridgeRock = RidgeMaterial("FarRidgesRock", new Color(.78f, .84f, .93f), .55f);
            snow = SnowMaterial("Snow", new Color(.9f, .94f, .985f), .8f);
            ridgeSnow = RidgeMaterial("FarRidgesSnow", new Color(.88f, .93f, 1.03f), .62f);
            ice = IceMaterial();
            needles = PlainMaterial("FirNeedles", new Color(.1f, .15f, .11f), .06f);
            wood = PlainMaterial("FirTrunk", new Color(.22f, .18f, .14f), .05f);
            mist = MistMaterial();
            var trailPacked = PlainMaterial("GoatTrailPacked", new Color(.65f, .69f, .76f), .05f);
            var trailWorn = PlainMaterial("GoatTrailWorn", new Color(.48f, .51f, .56f), .04f);

            spike = Save(FrozenRiftMesh.Crystal(3), "IceCrystal-0.asset");
            shard = Save(FrozenRiftMesh.Crystal(13), "IceCrystal-1.asset");
            block = Save(FrozenRiftMesh.IceBlock(5), "IceBlock.asset");
            cluster = Save(FrozenRiftMesh.CrystalCluster(2), "IceCluster.asset");
            boulder = Save(FrozenRiftMesh.Boulder(7), "FrostBoulder.asset");
            needleMesh = Save(FrozenRiftMesh.Pine(false), "Fir.asset");
            crownMesh = Save(FrozenRiftMesh.Pine(true), "FirCrown.asset");
            trunkMesh = Save(Object.Instantiate(DecorMeshLibrary.TrunkMesh()), "FirTrunk.asset");
            mistMesh = Save(FrozenRiftMesh.MistBank(), "MistBank.asset");

            var root = new GameObject("Ледяной разлом — статичная сцена").transform;
            FrozenRiftTerrain.Build(out var rockMesh, out var snowMesh, out var bedMesh);
            var relief = new GameObject("Рельеф разлома").transform; relief.SetParent(root, false);
            Place(relief, "Скальные стены и ярусы", Save(rockMesh, "RiftRock.asset"), rock, Vector3.zero, Vector3.one, true);
            Place(relief, "Снежные плато и карнизы", Save(snowMesh, "RiftSnow.asset"), snow, Vector3.zero, Vector3.one, true);
            Place(relief, "Ледяное дно разлома", Save(bedMesh, "RiftBed.asset"), bed, Vector3.zero, Vector3.one, true);

            var horizon = new GameObject("Дальние снежные гряды").transform; horizon.SetParent(root, false);
            FrozenRiftTerrain.Ring(out var ringSnow, out var ringRock);
            Place(horizon, "Снежные гряды", Save(ringSnow, "FarRidgesSnow.asset"), ridgeSnow, Vector3.zero, Vector3.one);
            Place(horizon, "Скальные рёбра гряд", Save(ringRock, "FarRidgesRock.asset"), ridgeRock, Vector3.zero, Vector3.one);

            var largeIce = new FrozenRiftBatch();
            var firNeedles = new FrozenRiftBatch(true);
            var firSnow = new FrozenRiftBatch(true);
            var firTrunks = new FrozenRiftBatch(true);
            var slabs = new FrozenRiftBatch();
            var avalanche = new FrozenRiftBatch();
            RiftIce(largeIce, random);
            RiftFirs(firNeedles, firSnow, firTrunks, random);
            RiftSlabs(slabs, random);
            Talus(slabs, random);
            Tables(slabs, random);
            WallHeroes(largeIce, random);
            Avalanche(avalanche, slabs, random, -1, 190f);
            Avalanche(avalanche, slabs, random, 1, -350f);
            Place(relief, "Лавинные языки со склонов", Save(avalanche.Build("Лавинные снежные языки"), "AvalancheSnow.asset"),
                snow, Vector3.zero, Vector3.one);

            var iceRoot = new GameObject("Ледяные образования разлома").transform; iceRoot.SetParent(root, false);
            Place(iceRoot, "Друзы кромок, стен и дна", Save(largeIce.Build("Крупные ледяные друзы разлома"), "RiftIceLarge.asset"),
                ice, Vector3.zero, Vector3.one);

            var forest = new GameObject("Заснеженные ели").transform; forest.SetParent(root, false);
            Place(forest, "Хвоя", Save(firNeedles.Build("Хвоя заснеженных елей"), "FirNeedles.asset"), needles, Vector3.zero, Vector3.one);
            Place(forest, "Снег на ветвях", Save(firSnow.Build("Снежные шапки на елях"), "FirSnow.asset"), snow, Vector3.zero, Vector3.one);
            Place(forest, "Стволы", Save(firTrunks.Build("Стволы елей"), "FirTrunks.asset"), wood, Vector3.zero, Vector3.one);
            Place(root, "Осыпи, обломки и столы-глыбы", Save(slabs.Build("Морозные осыпи и скальные обломки"), "FrostBoulders.asset"),
                rock, Vector3.zero, Vector3.one);

            var mistRoot = new GameObject("Туман в разломе").transform; mistRoot.SetParent(root, false);
            for (int i = 0; i < 20; i++)
            {
                float z = Range(random, -620f, 620f);
                float x = FrozenRiftTerrain.BedX(z, Range(random, -.7f, .7f));
                float y = FrozenRiftTerrain.BedLevel(z) + Range(random, 10f, 55f);
                Bank(mistRoot, mistMesh, mist, random, new Vector3(x, y, z), Range(random, 160f, 320f), Range(random, 60f, 120f));
            }
            for (int i = 0; i < 14; i++)
            {
                float z = Range(random, -650f, 650f);
                float x = FrozenRiftTerrain.BedX(z, Range(random, -.5f, .5f));
                float y = FrozenRiftTerrain.RimHeight(z) - Range(random, 90f, 130f);
                Bank(mistRoot, mistMesh, mist, random, new Vector3(x, y, z), Range(random, 190f, 340f), Range(random, 60f, 100f));
            }

            RouteGuide(root);
            var trailRoot = new GameObject("Козья тропа").transform; trailRoot.SetParent(root, false);
            GoatTrail(trailRoot, trailPacked, trailWorn, random);
            var jumpSnow = new FrozenRiftBatch();
            var jumpIce = new FrozenRiftBatch();
            JumpSpots(jumpSnow, jumpIce, random);
            Place(relief, "Прыжковые уступы и жёлобы тропы", Save(jumpSnow.Build("Снежные уступы тропы"), "TrailJumpsSnow.asset"),
                snow, Vector3.zero, Vector3.one);
            Place(iceRoot, "Ледяные мосты тропы", Save(jumpIce.Build("Ледяные мосты тропы"), "TrailBridgesIce.asset"),
                ice, Vector3.zero, Vector3.one);
            MainDruse(iceRoot, trailEnd, random);
            Environment();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 52f; camera.farClipPlane = 3400f;
            var post = camera.gameObject.AddComponent<GoatDescent.GoatScenePost>();
            post.coolTint = new Color(.93f, .985f, 1.06f);
            post.warmTint = Color.white;
            post.saturation = 1.2f;
            PlaceCamera(camera, new Vector3(FrozenRiftTerrain.Channel(-900f) - 560f, 640f, -1150f),
                new Vector3(FrozenRiftTerrain.Channel(420f) + 60f, -40f, 460f));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene);
            PlaceCamera(camera, new Vector3(FrozenRiftTerrain.Channel(-860f) - 480f, 620f, -1080f),
                new Vector3(FrozenRiftTerrain.Channel(380f) + 40f, -20f, 420f));
            Render(camera, "FrozenRift-overview");
            PlaceCamera(camera, viewEye,
                new Vector3(FrozenRiftTerrain.Channel(200f), FrozenRiftTerrain.BedLevel(200f) + 60f, 200f));
            Render(camera, "FrozenRift-canyon");
            PlaceCamera(camera, new Vector3(FrozenRiftTerrain.RimX(-300f, -1) - 190f, 250f, -300f),
                new Vector3(FrozenRiftTerrain.RimX(-120f, -1) - 40f, 30f, -120f));
            Render(camera, "FrozenRift-rim");
            PlaceCamera(camera, new Vector3(FrozenRiftTerrain.RimX(-620f, -1) - 520f, 380f, -620f),
                new Vector3(FrozenRiftTerrain.RimX(380f, -1) - 160f, 90f, 380f));
            Render(camera, "FrozenRift-terrace");
            PlaceCamera(camera, new Vector3(620f, 460f, 520f), new Vector3(60f, 430f, 2200f));
            Render(camera, "FrozenRift-mountains");
            PlaceCamera(camera, new Vector3(-620f, 640f, 700f), new Vector3(30f, 60f, -40f));
            Render(camera, "FrozenRift-back");
            Debug.Log("FROZEN_RIFT_STATIC_SAVED scene=" + Scene);
        }

        private static void PlaceCamera(Camera camera, Vector3 position, Vector3 target)
        {
            float ground = FrozenRiftTerrain.Height(position.x, position.z) + 10f;
            if (ground > position.y) position.y = ground;
            camera.transform.position = position;
            camera.transform.LookAt(target);
        }

        private static void Bank(Transform parent, Mesh mesh, Material material, System.Random random, Vector3 position, float width, float height)
        {
            var bank = new GameObject("Полоса тумана").transform; bank.SetParent(parent, false);
            bank.position = position;
            bank.rotation = Quaternion.Euler(0f, Range(random, 0f, 180f), 0f);
            bank.localScale = new Vector3(width, height, width);
            bank.gameObject.isStatic = true;
            bank.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            bank.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Vector3 RiftPoint(int side, float u, float z)
        {
            float half = FrozenRiftTerrain.HalfWidth(z), bedHalf = FrozenRiftTerrain.BedWidth(z);
            float d = u <= 0f ? (1f + u) * bedHalf : u <= 1f ? bedHalf + (half - bedHalf) * u : half + (u - 1f) * 520f;
            float x = FrozenRiftTerrain.Channel(z) + side * d;
            return new Vector3(x, FrozenRiftTerrain.Height(x, z), z);
        }

        private static void UpQuad(FrozenRiftBatch batch, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            if (Vector3.Cross(b - a, c - a).y < 0f) batch.Quad(d, c, b, a);
            else batch.Quad(a, b, c, d);
        }

        private static List<Vector3> trail;

        private static void GoatTrail(Transform parent, Material packed, Material worn, System.Random random)
        {
            var route = TrailRoute();
            trail = route;
            float length = 0f;
            for (int i = 0; i < route.Count - 1; i++) length += Vector3.Distance(route[i], route[i + 1]);
            GoatTrailBuilder.Build(parent, "Тропа козы — утоптанный снег", p => FrozenRiftTerrain.Height(p.x, p.z),
                packed, route, 7f, .3f, 7f, true, new Color(.56f, .56f, .54f));
            GoatTrailBuilder.Build(parent, "Тропа козы — протоптанный след", p => FrozenRiftTerrain.Height(p.x, p.z),
                worn, route, 3.6f, .42f, 0f, true, null);
            int[] marks = { 2, 10, 17, 24, 29 };
            string[] names = { "старт на плато", "траверс карниза", "средняя полка", "нижний ярус", "финиш у главной друзы" };
            var checkpoints = new GameObject("Чекпоинты тропы").transform; checkpoints.SetParent(parent, false);
            for (int i = 0; i < marks.Length; i++)
            {
                Vector3 at = route[marks[i]];
                Vector3 ahead = route[Mathf.Min(marks[i] + 1, route.Count - 1)];
                Vector3 behind = route[Mathf.Max(marks[i] - 1, 0)];
                Vector3 beside = Vector3.Cross(Vector3.up, (ahead - behind).normalized).normalized * Range(random, 4.5f, 7f);
                var position = at + beside;
                float size = Range(random, 5f, 8.5f);
                position.y = FrozenRiftTerrain.Height(position.x, position.z) - size * .35f;
                var item = new GameObject("Чекпоинт " + (i + 1) + " — " + names[i]);
                item.transform.SetParent(checkpoints, false); item.transform.position = position;
                item.transform.rotation = Quaternion.Euler(Range(random, -5f, 5f), Range(random, 0f, 360f), Range(random, -5f, 5f));
                item.transform.localScale = new Vector3(size, size * .8f, size * .9f);
                item.AddComponent<MeshFilter>().sharedMesh = boulder;
                item.AddComponent<MeshRenderer>().sharedMaterial = rock;
                item.isStatic = true;
            }
            trailEnd = route[route.Count - 1];
            Debug.Log("GOAT_TRAIL FrozenRift waypoints=" + route.Count + " length~" + Mathf.RoundToInt(length) + "м");
        }

        private static List<Vector3> TrailRoute()
        {
            var u = new[] { 1.34f, 1.26f, 1.18f, 1.11f, 1.05f, 1.01f, .99f, .86f, .845f, .845f, .845f, .845f,
                .79f, .755f, .74f, .60f, .56f, .52f, .48f, .34f, .30f, .27f, .22f, .08f, .04f, .02f,
                -.06f, -.02f, .03f, -.05f, -.55f };
            var z = new[] { 770f, 730f, 694f, 660f, 628f, 600f, 566f, 392f, 390f, 360f, 330f, 305f,
                285f, 270f, 250f, 110f, 85f, 60f, 40f, -100f, -125f, -150f, -170f, -305f, -330f, -355f,
                -375f, -400f, -425f, -450f, -470f };
            var route = new List<Vector3>(u.Length);
            for (int i = 0; i < u.Length; i++) route.Add(RiftPoint(1, u[i], z[i]));
            return route;
        }

        private static void JumpSpots(FrozenRiftBatch snowBatch, FrozenRiftBatch iceBatch, System.Random random)
        {
            if (trail == null || trail.Count < 2) return;
            Ledge(snowBatch, random, 4, 3, 7f, 2.6f);
            Ledge(snowBatch, random, 12, 2, 8f, 3.2f);
            Ledge(snowBatch, random, 17, 3, 6.5f, 2.8f);
            Ledge(snowBatch, random, 22, 2, 7.5f, 2.4f);
            Ledge(snowBatch, random, 26, 3, 6f, 3f);
            Bridge(iceBatch, random, 6, 5.2f);
            Bridge(iceBatch, random, 15, 4.2f);
            Bridge(iceBatch, random, 23, 4.8f);
            Chute(snowBatch, random, 29, 9f);
            Chute(snowBatch, random, 9, 7f);
        }

        private static Vector3 RouteAt(int index, float t, out Vector3 tangent)
        {
            int last = trail.Count - 1;
            float f = Mathf.Clamp(index + t, 0f, last);
            int i = Mathf.Min((int)f, last - 1);
            Vector3 a = trail[i], b = trail[i + 1];
            tangent = (b - a).normalized;
            return Vector3.Lerp(a, b, f - i);
        }

        private static void Ledge(FrozenRiftBatch batch, System.Random random, int index, int segments, float width, float rise)
        {
            const int steps = 8;
            Vector3 previousInner = Vector3.zero, previousOuter = Vector3.zero, previousLip = Vector3.zero;
            bool first = true;
            for (int s = 0; s <= segments; s++)
                for (int k = 0; k <= steps; k++)
                {
                    float t = (s * steps + k) / (float)(segments * steps);
                    Vector3 tangent;
                    Vector3 centre = RouteAt(Mathf.RoundToInt(index + t * segments), 0f, out tangent);
                    Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                    float swell = Mathf.Sin(t * Mathf.PI);
                    float w = width * (.55f + .55f * swell + .18f * Mathf.Sin(t * 9f + index * 2.3f));
                    var inner = centre - side * 3.5f + Vector3.up * .35f;
                    var outer = centre + side * w + Vector3.up * (rise * swell + .35f);
                    outer.x += Range(random, -1.4f, 1.4f); outer.z += Range(random, -1.4f, 1.4f);
                    var lip = outer + side * Range(random, 2.2f, 4.2f) - Vector3.up * (rise * swell + Range(random, 3.5f, 5.5f));
                    if (!first)
                    {
                        UpQuad(batch, previousInner, inner, outer, previousOuter);
                        UpQuad(batch, previousOuter, outer, lip, previousLip);
                    }
                    previousInner = inner; previousOuter = outer; previousLip = lip; first = false;
                }
        }

        private static void Bridge(FrozenRiftBatch batch, System.Random random, int index, float width)
        {
            const int steps = 12;
            const float arc = 5.5f;
            Vector3 previousLeft = Vector3.zero, previousRight = Vector3.zero;
            bool first = true;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                Vector3 tangent;
                Vector3 centre = RouteAt(index, t, out tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float sag = Mathf.Sin(t * Mathf.PI);
                float w = width * (.7f + .45f * sag + .14f * Mathf.Sin(t * 8.2f + index));
                float lift = Mathf.Lerp(0f, 1.2f, t) + arc * sag;
                var left = centre - side * w + Vector3.up * lift;
                var right = centre + side * w + Vector3.up * lift;
                left.x += Range(random, -.9f, .9f); left.z += Range(random, -.9f, .9f);
                right.x += Range(random, -.9f, .9f); right.z += Range(random, -.9f, .9f);
                if (!first) UpQuad(batch, previousLeft, left, right, previousRight);
                previousLeft = left; previousRight = right; first = false;
            }
            for (int k = 0; k <= steps; k += 3)
            {
                float t = k / (float)steps;
                Vector3 tangent;
                Vector3 centre = RouteAt(index, t, out tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float sag = Mathf.Sin(t * Mathf.PI);
                Vector3 basePoint = centre + Vector3.up * (Mathf.Lerp(0f, 1.2f, t) + arc * sag) - Vector3.up * (2.4f + sag * 1.6f);
                Ice(batch, random, basePoint + side * width * .6f,
                    (Vector3.down * .6f + side * .8f + tangent * Range(random, -.3f, .3f)).normalized,
                    Range(random, 4f, 8f), Range(random, 1.2f, 2.4f), .12f);
            }
        }

        private static void Chute(FrozenRiftBatch batch, System.Random random, int index, float width)
        {
            const int steps = 10;
            Vector3 previousLeft = Vector3.zero, previousRight = Vector3.zero;
            bool first = true;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                Vector3 tangent;
                Vector3 centre = RouteAt(index, t, out tangent);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float w = width * (.6f + .5f * t + .15f * Mathf.Sin(t * 7.4f + index * 1.7f));
                float drop = -Mathf.Lerp(0f, 9f, t * t);
                var left = centre - side * w + Vector3.up * (drop + 1.6f * (1f - t));
                var right = centre + side * w + Vector3.up * (drop + 1.6f * (1f - t));
                left.x += Range(random, -1.6f, 1.6f); left.z += Range(random, -1.6f, 1.6f);
                right.x += Range(random, -1.6f, 1.6f); right.z += Range(random, -1.6f, 1.6f);
                if (!first) UpQuad(batch, previousLeft, left, right, previousRight);
                previousLeft = left; previousRight = right; first = false;
            }
        }

        private static void MainDruse(Transform parent, Vector3 foot, System.Random random)
        {
            var batch = new FrozenRiftBatch();
            float core = Range(random, 56f, 70f);
            batch.Add(cluster, Matrix4x4.TRS(foot, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                new Vector3(core, core * 1.25f, core)));
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f + Range(random, -.22f, .22f);
                var at = foot + new Vector3(Mathf.Cos(angle) * Range(random, 16f, 34f), 0f, Mathf.Sin(angle) * Range(random, 16f, 34f));
                at.y = FrozenRiftTerrain.Height(at.x, at.z);
                Vector3 lean = (Vector3.up * Range(random, .5f, .78f)
                    + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Range(random, .4f, .75f)).normalized;
                float height = Range(random, 48f, 85f);
                batch.Add(random.Next(2) == 0 ? spike : shard, Matrix4x4.TRS(at - lean * (height * .1f),
                    Quaternion.FromToRotation(Vector3.up, lean) * Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                    new Vector3(height * .26f, height, height * .24f)));
            }
            var item = new GameObject("Главная друза"); item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = Save(batch.Build("Главная ледяная друза"), "RiftIceMain.asset");
            item.AddComponent<MeshRenderer>().sharedMaterial = ice;
            item.isStatic = true;
        }

        private static void Avalanche(FrozenRiftBatch snowBatch, FrozenRiftBatch rubble, System.Random random, int side, float z)
        {
            const int steps = 16;
            var left = Vector3.zero;
            var right = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = Mathf.Lerp(.48f, -.03f, t);
                float spread = 8f + t * 58f + Mathf.Sin(t * 4.7f) * 6f;
                Vector3 a = RiftPoint(side, u, z - spread * .5f);
                Vector3 b = RiftPoint(side, u, z + spread * .5f);
                a.y += 2.8f - t;
                b.y += 2.8f - t;
                if (i > 0) UpQuad(snowBatch, left, a, b, right);
                if (i > 1 && i % 2 == 0)
                {
                    float size = Range(random, 1.4f, 4.6f);
                    var position = Vector3.Lerp(a, b, Range(random, .12f, .88f));
                    position.y += Range(random, .6f, 2.4f);
                    rubble.Add(boulder, Matrix4x4.TRS(position,
                        Quaternion.Euler(Range(random, -14f, 14f), Range(random, 0f, 360f), Range(random, -14f, 14f)),
                        new Vector3(size, size * Range(random, .5f, .9f), size)));
                }
                left = a; right = b;
            }
        }

        private static void Talus(FrozenRiftBatch batch, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
                for (int tier = 0; tier < FrozenRiftTerrain.Tiers; tier++)
                    for (float z = -700f; z <= 700f; z += 150f)
                    {
                        float zz = z + Range(random, -30f, 30f);
                        var centre = RiftPoint(side, (tier + .4f) / FrozenRiftTerrain.Tiers, zz);
                        int stones = 3 + random.Next(3);
                        for (int i = 0; i < stones; i++)
                        {
                            float size = Range(random, 1.6f, 5.4f);
                            var position = centre + new Vector3(Range(random, -14f, 14f), 0f, Range(random, -18f, 18f));
                            position.y = FrozenRiftTerrain.Height(position.x, position.z) - size * .35f;
                            batch.Add(boulder, Matrix4x4.TRS(position,
                                Quaternion.Euler(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f)),
                                new Vector3(size, size * Range(random, .6f, 1f), size)));
                        }
                    }
        }

        private static void Tables(FrozenRiftBatch batch, System.Random random)
        {
            for (int i = 0; i < 9; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                var at = RiftPoint(side, Range(random, 1.2f, 1.8f), Range(random, -700f, 700f));
                float size = Range(random, 4f, 8f);
                batch.Add(boulder, Matrix4x4.TRS(at - Vector3.up * (size * .1f),
                    Quaternion.Euler(Range(random, -6f, 6f), Range(random, 0f, 360f), Range(random, -6f, 6f)),
                    new Vector3(size * .5f, size * .9f, size * .5f)));
                batch.Add(boulder, Matrix4x4.TRS(at + Vector3.up * (size * .8f),
                    Quaternion.Euler(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f)),
                    new Vector3(size, size * .45f, size * .85f)));
            }
        }

        private static void RiftIce(FrozenRiftBatch large, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -740f; z <= 740f; z += 52f)
                {
                    float zz = z + Range(random, -14f, 14f);
                    double roll = random.NextDouble();
                    if (roll < .05) continue;
                    var ground = RiftPoint(side, Range(random, .76f, .84f), zz);
                    ground.y -= 2f;
                    Vector3 spine = (Vector3.up * Range(random, .5f, .72f) + Vector3.right * (-side) * Range(random, .55f, .95f)).normalized;
                    if (roll < .13)
                        spine = Quaternion.AngleAxis(Range(random, 5f, 15f),
                            new Vector3(Range(random, -1f, 1f), 0f, Range(random, -1f, 1f)).normalized) * spine;
                    Druse(large, random, ground, spine, 5 + random.Next(4), Range(random, 40f, 54f), Range(random, 10f, 19f));
                }
            }
            for (int i = 0; i < 44; i++)
            {
                float z = Range(random, -600f, 600f);
                var ground = new Vector3(FrozenRiftTerrain.BedX(z, Range(random, -.9f, .9f)), 0f, z);
                ground.y = FrozenRiftTerrain.Height(ground.x, ground.z) - 2f;
                float size = Range(random, 30f, 50f);
                large.Add(cluster, Matrix4x4.TRS(ground, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                    new Vector3(size, size * Range(random, 1.1f, 2f), size)));
                Druse(large, random, ground, Vector3.up, 3 + random.Next(3), Range(random, 30f, 44f), Range(random, 10f, 20f));
            }
        }

        private static void Druse(FrozenRiftBatch batch, System.Random random, Vector3 ground, Vector3 spine, int count, float size, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                float height = Mathf.Min(86f, size * Range(random, .7f, 1.05f) * Heavy(random));
                Vector3 lean = (spine + new Vector3(Range(random, -.42f, .42f), Range(random, -.06f, .28f), Range(random, -.42f, .42f))).normalized;
                var origin = ground + new Vector3(Range(random, -spread, spread), Range(random, -1.5f, .5f), Range(random, -spread, spread));
                Ice(batch, random, origin, lean, height, height * Range(random, .24f, .38f), .1f);
            }
        }

        private static float Heavy(System.Random random) => Mathf.Min(2.5f, Mathf.Pow((float)random.NextDouble(), -.6f));

        private static bool NearView(float x, float z)
        {
            float dx = x - viewEye.x, dz = z - viewEye.z;
            return dx * dx + dz * dz < 190f * 190f;
        }

        private static void WallHeroes(FrozenRiftBatch batch, System.Random random)
        {
            var side = new[] { -1, -1, -1, 1, 1 };
            var tier = new[] { 3, 2, 3, 3, 2 };
            var at = new[] { -330f, -150f, 60f, 470f, 700f };
            for (int i = 0; i < at.Length; i++)
            {
                float z = at[i] + Range(random, -22f, 22f);
                float x = FrozenRiftTerrain.TierFaceX(z, side[i], tier[i]) - side[i] * Range(random, 3f, 10f);
                var ground = new Vector3(x, FrozenRiftTerrain.TierY(z, tier[i]) - Range(random, 8f, 55f), z);
                float height = Range(random, 85f, 150f);
                Vector3 spine = (Vector3.up * Range(random, .82f, .96f)
                    + Vector3.right * (-side[i]) * Range(random, .26f, .5f)).normalized;
                int shards = 3 + random.Next(3);
                for (int k = 0; k < shards; k++)
                {
                    float shard = height * Range(random, .55f, 1f);
                    Vector3 lean = (spine + new Vector3(Range(random, -.4f, .4f), Range(random, -.22f, .22f), Range(random, -.4f, .4f))).normalized;
                    var start = ground + new Vector3(Range(random, -9f, 9f), Range(random, -8f, 12f), Range(random, -9f, 9f));
                    Ice(batch, random, start, lean, shard, shard * Range(random, .2f, .32f), .06f);
                }
            }
        }

        private static bool FirGap(float x, float z) =>
            Mathf.PerlinNoise(x * .0016f + 3.7f, z * .0016f + 11.3f) * Mathf.PerlinNoise(x * .0043f + 21.1f, z * .0043f + 5.9f) < .3f;

        private static void Ice(FrozenRiftBatch batch, System.Random random, Vector3 ground, Vector3 lean, float height, float width, float sink)
        {
            var mesh = height > 26f ? spike : random.Next(3) == 0 ? block : shard;
            var matrix = Matrix4x4.TRS(ground - lean * (height * sink),
                Quaternion.FromToRotation(Vector3.up, lean) * Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                new Vector3(width, height, width * Range(random, .95f, 1.15f)));
            batch.Add(mesh, matrix);
        }

        private static void RiftFirs(FrozenRiftBatch needlesBatch, FrozenRiftBatch snowBatch, FrozenRiftBatch trunksBatch, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -740f; z <= 740f; z += 40f)
                {
                    float zz = z + Range(random, -12f, 12f);
                    float x = FrozenRiftTerrain.RimX(zz, side) + side * Range(random, 4f, 40f);
                    if (FrozenRiftTerrain.Slope(x, zz) > .6f || NearView(x, zz)) continue;
                    int trees = 5 + random.Next(5);
                    for (int t = 0; t < trees; t++) Sprout(needlesBatch, snowBatch, trunksBatch, random, x + Range(random, -18f, 18f), zz + Range(random, -18f, 18f), 16f, 26f);
                }
                for (int i = 0; i < 8; i++)
                {
                    float z = 540f - i * 150f + Range(random, -30f, 30f);
                    float x = FrozenRiftTerrain.RimX(z, side) + side * Range(random, 10f, 30f);
                    if (NearView(x, z)) continue;
                    Fir(needlesBatch, snowBatch, trunksBatch, random, new Vector3(x, FrozenRiftTerrain.Height(x, z) - .8f, z), Range(random, 46f, 64f));
                }
                for (int tier = 1; tier <= 3; tier++)
                    for (int i = 0; i < 12; i++)
                    {
                        float z = Range(random, -700f, 700f);
                        float x = FrozenRiftTerrain.TierShelfX(z, side, tier, Range(random, .1f, .8f));
                        if (NearView(x, z)) continue;
                        Fir(needlesBatch, snowBatch, trunksBatch, random, new Vector3(x, FrozenRiftTerrain.Height(x, z) - .8f, z),
                            Mathf.Min(56f, Range(random, 18f, 30f) * Heavy(random)));
                    }
                for (int group = 0; group < 30; group++)
                {
                    float z = Range(random, -740f, 740f);
                    float x = FrozenRiftTerrain.RimX(z, side) + side * Range(random, 60f, 440f);
                    if (Mathf.Abs(x) > FrozenRiftTerrain.Extent - 60f) continue;
                    if (FrozenRiftTerrain.Slope(x, z) > .5f || FirGap(x, z) || NearView(x, z)) continue;
                    int trees = 2 + random.Next(4);
                    for (int t = 0; t < trees; t++) Sprout(needlesBatch, snowBatch, trunksBatch, random, x + Range(random, -26f, 26f), z + Range(random, -26f, 26f), 14f, 22f);
                }
            }
        }

        private static void Sprout(FrozenRiftBatch needlesBatch, FrozenRiftBatch snowBatch, FrozenRiftBatch trunksBatch, System.Random random,
            float x, float z, float minHeight, float maxHeight)
        {
            if (Mathf.Abs(x) > FrozenRiftTerrain.Extent - 20f || Mathf.Abs(z) > 800f) return;
            if (FrozenRiftTerrain.Slope(x, z) > .6f || FirGap(x, z) || NearView(x, z)) return;
            Fir(needlesBatch, snowBatch, trunksBatch, random, new Vector3(x, FrozenRiftTerrain.Height(x, z) - .6f, z),
                Mathf.Min(56f, Range(random, minHeight, maxHeight) * Heavy(random)));
        }

        private static void Fir(FrozenRiftBatch needlesBatch, FrozenRiftBatch snowBatch, FrozenRiftBatch trunksBatch, System.Random random, Vector3 position, float height)
        {
            float width = height * Range(random, .72f, .92f);
            var rotation = Quaternion.Euler(Range(random, -4f, 4f), Range(random, 0f, 360f), Range(random, -4f, 4f));
            var crown = Matrix4x4.TRS(position, rotation, new Vector3(width, height, width));
            needlesBatch.Add(needleMesh, crown);
            snowBatch.Add(crownMesh, crown);
            trunksBatch.Add(trunkMesh, Matrix4x4.TRS(position, rotation, new Vector3(height * .045f, height * .68f, height * .045f)));
        }

        private static void RiftSlabs(FrozenRiftBatch batch, System.Random random)
        {
            for (int i = 0; i < 110; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float z = Range(random, -780f, 780f);
                float x = FrozenRiftTerrain.RimX(z, side) + side * Range(random, -50f, 400f);
                if (Mathf.Abs(x) > FrozenRiftTerrain.Extent - 30f) continue;
                float size = Range(random, 5f, 18f);
                var position = new Vector3(x, FrozenRiftTerrain.Height(x, z) - size * .45f, z);
                batch.Add(boulder, Matrix4x4.TRS(position,
                    Quaternion.Euler(Range(random, -9f, 9f), Range(random, 0f, 360f), Range(random, -9f, 9f)),
                    new Vector3(size, size * Range(random, .5f, .9f), size)));
            }
        }

        private static void RouteGuide(Transform parent)
        {
            string[] names = { "Старт", "Снежное плато", "Карниз над разломом", "Верхний ярус", "Средний ярус",
                "Нижний ярус", "Ледяное дно", "Восточный ярус", "Восточный карниз", "Восточное плато",
                "Дальнее плато", "Снежный гребень", "Предполагаемый финиш" };
            int[] sides = { -1, -1, -1, -1, -1, -1, 0, 1, 1, 1, 1, 1, 1 };
            float[] offsets = { 210f, 130f, 40f, .78f, .5f, .22f, 0f, .32f, .68f, 60f, 180f, 320f, 470f };
            var guide = new GameObject("Маршрут — авторские ориентиры, игровая настройка отложена").transform;
            guide.SetParent(parent, false);
            for (int i = 0; i < names.Length; i++)
            {
                float z = -300f + i * 50f;
                float x = sides[i] == 0
                    ? FrozenRiftTerrain.BedX(z, 0f)
                    : Mathf.Abs(offsets[i]) < 1.2f
                        ? FrozenRiftTerrain.Channel(z) + sides[i] * Mathf.Lerp(FrozenRiftTerrain.BedWidth(z), FrozenRiftTerrain.HalfWidth(z), offsets[i])
                        : FrozenRiftTerrain.RimX(z, sides[i]) + sides[i] * offsets[i];
                var marker = new GameObject(names[i]).transform; marker.SetParent(guide, false);
                marker.position = new Vector3(x, FrozenRiftTerrain.Height(x, z) + 2f, z);
            }
        }

        private static Transform Place(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, bool collider = false)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false); item.transform.localPosition = position;
            item.transform.localScale = scale; item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material; item.isStatic = true;
            if (collider) item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item.transform;
        }

        private static Material RockMaterial(string name, Color tint, Color deep, float depth, float tiling)
        {
            var material = new Material(Shader.Find("GoatDescent/FrozenRiftRock")) { name = name };
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_RockTint", tint);
            material.SetColor("_DeepTint", deep);
            material.SetColor("_SnowTint", new Color(.9f, .93f, .97f));
            material.SetColor("_HazeColor", new Color(.66f, .8f, .94f));
            material.SetFloat("_Tiling", tiling);
            material.SetFloat("_Layers", .62f);
            material.SetFloat("_StrataStep", .035f);
            material.SetFloat("_Depth", depth);
            material.SetFloat("_Haze", .8f);
            return Save(material, name + ".mat");
        }

        private static Material RidgeMaterial(string name, Color color, float haze)
        {
            var material = new Material(Shader.Find("GoatDescent/FrozenRiftFarRidges")) { name = name };
            material.SetColor("_Color", color);
            material.SetColor("_HazeColor", new Color(.66f, .8f, .94f));
            material.SetFloat("_Haze", haze);
            material.SetFloat("_Bright", .24f);
            return Save(material, name + ".mat");
        }

        private static Material SnowMaterial(string name, Color color, float haze)
        {
            var material = new Material(Shader.Find("GoatDescent/FrozenRiftSnow")) { name = name };
            material.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/rock_color.jpg"));
            material.SetColor("_SnowColor", color);
            material.SetColor("_RockColor", new Color(.64f, .58f, .5f));
            material.SetColor("_IceTint", new Color(.68f, .86f, .98f));
            material.SetColor("_ShadowTint", new Color(.74f, .82f, .94f));
            material.SetColor("_HazeColor", new Color(.66f, .8f, .94f));
            material.SetFloat("_Tiling", .06f);
            material.SetFloat("_Gloss", .2f);
            material.SetFloat("_Lift", .12f);
            material.SetFloat("_Haze", haze);
            return Save(material, name + ".mat");
        }

        private static Material IceMaterial()
        {
            var material = new Material(Shader.Find("GoatDescent/FrozenRiftIce")) { name = "GlacierIce" };
            material.SetColor("_Color", new Color(.5f, .74f, .96f));
            material.SetColor("_CoreColor", new Color(.22f, .37f, .55f));
            material.SetColor("_EdgeColor", new Color(.82f, .96f, 1f));
            material.SetColor("_HazeColor", new Color(.66f, .8f, .94f));
            material.SetFloat("_Alpha", .78f);
            material.SetFloat("_Fresnel", 2.2f);
            material.SetFloat("_Emissive", .42f);
            material.SetFloat("_Gloss", .88f);
            material.SetFloat("_Haze", .7f);
            return Save(material, "GlacierIce.mat");
        }

        private static Material PlainMaterial(string name, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", smoothness);
            return Save(material, name + ".mat");
        }

        private static Material MistMaterial()
        {
            var material = new Material(Shader.Find("GoatDescent/FrozenRiftMist")) { name = "RiftMist" };
            material.SetColor("_Color", new Color(.84f, .88f, .94f));
            material.SetFloat("_Strength", .5f);
            return Save(material, "RiftMist.mat");
        }

        private static T Save<T>(T asset, string name) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(Folder + "/" + name);
            if (!existing) { AssetDatabase.CreateAsset(asset, Folder + "/" + name); return asset; }
            if (asset is Mesh source && existing is Mesh destination)
            {
                destination.Clear(); destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices; destination.triangles = source.triangles;
                if (source.normals.Length == source.vertexCount) destination.normals = source.normals;
                if (source.colors.Length == source.vertexCount) destination.colors = source.colors;
                if (source.uv.Length == source.vertexCount) destination.uv = source.uv;
                destination.RecalculateBounds();
            }
            else if (asset is Material sourceMaterial && existing is Material destinationMaterial)
            {
                destinationMaterial.shader = sourceMaterial.shader;
                EditorUtility.CopySerialized(sourceMaterial, destinationMaterial);
            }
            else EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); return existing;
        }

        private static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.68f, .76f, .88f); RenderSettings.ambientEquatorColor = new Color(.6f, .68f, .79f);
            RenderSettings.ambientGroundColor = new Color(.5f, .53f, .62f); RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.64f, .74f, .87f); RenderSettings.fogDensity = .00015f;
            var sun = new GameObject("Холодное полярное солнце").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1f, .96f, .88f); sun.intensity = .58f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -45f, 0f); RenderSettings.sun = sun;
            var sky = new Material(Shader.Find("GoatDescent/Sky"));
            sky.SetColor("_SkyZenith", new Color(.4f, .66f, .94f)); sky.SetColor("_SkyHorizon", new Color(.58f, .76f, .94f));
            sky.SetColor("_GroundColor", new Color(.68f, .8f, .93f)); sky.SetColor("_CloudColor", new Color(.95f, .97f, 1f));
            sky.SetColor("_SunColor", new Color(1f, .99f, .96f)); sky.SetFloat("_SunSize", .045f); sky.SetFloat("_CloudDensity", .5f);
            sky.SetVector("_SunDir", -sun.transform.forward); RenderSettings.skybox = Save(sky, "WinterSky.mat");
            ridgeSnow.SetVector("_SunDir", -sun.transform.forward);
            ridgeRock.SetVector("_SunDir", -sun.transform.forward);
        }

        private static void Render(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0); texture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }

        private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
