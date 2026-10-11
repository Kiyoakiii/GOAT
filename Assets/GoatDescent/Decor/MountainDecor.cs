using UnityEngine;

namespace GoatDescent
{
    public static class MountainDecor
    {
        public static void Generate(Transform parent, MountainSettings settings)
        {
            if (!settings.EnableDecor) return;

            var root = new GameObject("Decor");
            root.transform.SetParent(parent, false);
            var random = new System.Random(settings.Seed * 131 + 17);

            for (int i = 0; i < settings.BushCount; i++)
                if (TryPlaceVegetation(random, settings, out Vector3 position, out Vector3 normal, .62f, 32f, true, .9f))
                    CreateBush(root.transform, position, normal, random);
            for (int i = 0; i < settings.TreeCount; i++)
                if (TryPlaceVegetation(random, settings, out Vector3 position, out Vector3 normal, .42f, 28f, true, 1.6f))
                    CreateTreeCluster(root.transform, position, normal, settings, random);
            for (int i = 0; i < settings.RockCount; i++)
                if (TryPlaceRock(random, settings, out Vector3 position, out Vector3 normal, 1.4f))
                    CreateRock(root.transform, position, normal, settings, random);
            for (int i = 0; i < settings.FlowerCount; i++)
                if (TryPlaceFlower(random, settings, out Vector3 position, out Vector3 normal))
                    CreateFlower(root.transform, position, normal, random);
            for (int i = 0; i < settings.GrassTuftCount; i++)
                if (TryPlaceGroundFlora(random, settings, out Vector3 position, out Vector3 normal, .58f, 30f, .4f))
                    CreateGrassTuft(root.transform, position, normal, random);
        }

        private static bool TryPlaceVegetation(System.Random random, MountainSettings settings, out Vector3 position,
            out Vector3 normal, float heightLimit01, float maxSlope, bool requireGreen, float footprintRadius)
        {
            position = default;
            normal = Vector3.up;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float radial = (float)random.NextDouble() * .92f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * settings.Radius * radial;
                float z = Mathf.Sin(angle) * settings.Radius * radial;
                float y = MountainGenerator.HeightAt(settings, x, z);
                float height01 = Mathf.InverseLerp(settings.ValleyY, settings.SummitY, y);
                if (height01 > heightLimit01 || height01 < .05f || y < settings.ValleyY + 1.5f) continue;
                if (requireGreen && height01 > .42f) continue;
                if (!MountainGenerator.IsClearOfRoute(settings, x, z, footprintRadius)) continue;

                normal = MountainGenerator.NormalAt(settings, x, z);
                if (Vector3.Angle(normal, Vector3.up) > maxSlope) continue;
                if (maxSlope <= 24f && !IsFlatShelf(settings, x, z)) continue;

                position = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static bool IsFlatShelf(MountainSettings settings, float x, float z)
        {
            const float sampleDistance = 1.5f;
            float center = MountainGenerator.HeightAt(settings, x, z);
            float north = MountainGenerator.HeightAt(settings, x, z + sampleDistance);
            float south = MountainGenerator.HeightAt(settings, x, z - sampleDistance);
            float east = MountainGenerator.HeightAt(settings, x + sampleDistance, z);
            float west = MountainGenerator.HeightAt(settings, x - sampleDistance, z);
            float maximumDelta = Mathf.Max(Mathf.Abs(center - north), Mathf.Abs(center - south));
            maximumDelta = Mathf.Max(maximumDelta, Mathf.Abs(center - east));
            maximumDelta = Mathf.Max(maximumDelta, Mathf.Abs(center - west));
            return maximumDelta < .8f;
        }

        private static bool TryPlaceRock(System.Random random, MountainSettings settings, out Vector3 position, out Vector3 normal, float footprintRadius)
        {
            position = default;
            normal = Vector3.up;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float radial = .1f + (float)random.NextDouble() * .85f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * settings.Radius * radial;
                float z = Mathf.Sin(angle) * settings.Radius * radial;
                float y = MountainGenerator.HeightAt(settings, x, z);
                if (y < settings.ValleyY + 2f) continue;
                if (!MountainGenerator.IsClearOfRoute(settings, x, z, footprintRadius)) continue;

                normal = MountainGenerator.NormalAt(settings, x, z);
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope < 25f || slope > 78f) continue;
                position = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static bool TryPlaceGroundFlora(System.Random random, MountainSettings settings, out Vector3 position,
            out Vector3 normal, float heightLimit01, float maxSlope, float footprintRadius)
        {
            position = default;
            normal = Vector3.up;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float radial = (float)random.NextDouble() * .95f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * settings.Radius * radial;
                float z = Mathf.Sin(angle) * settings.Radius * radial;
                float y = MountainGenerator.HeightAt(settings, x, z);
                float height01 = Mathf.InverseLerp(settings.ValleyY, settings.SummitY, y);
                if (height01 > heightLimit01 || height01 < .04f || y < settings.ValleyY + 1.5f) continue;
                if (!MountainGenerator.IsClearOfRoute(settings, x, z, footprintRadius)) continue;

                normal = MountainGenerator.NormalAt(settings, x, z);
                if (Vector3.Angle(normal, Vector3.up) > maxSlope) continue;
                position = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static bool TryPlaceFlower(System.Random random, MountainSettings settings, out Vector3 position, out Vector3 normal)
        {
            position = default;
            normal = Vector3.up;
            for (int attempt = 0; attempt < 28; attempt++)
            {
                float radial = .05f + (float)random.NextDouble() * .9f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * settings.Radius * radial;
                float z = Mathf.Sin(angle) * settings.Radius * radial;
                if (MountainGenerator.PlatformMaskAt(settings, x, z) < .6f) continue;
                if (!MountainGenerator.TryGetPlatformAt(x, z, out LandingPlatform platform)) continue;
                if (platform.Type == LandingPlatformType.Ice || platform.Type == LandingPlatformType.Crumbling
                    || platform.Type == LandingPlatformType.Precise) continue;

                float y = MountainGenerator.HeightAt(settings, x, z);
                if (y < settings.ValleyY + 1f) continue;
                normal = MountainGenerator.NormalAt(settings, x, z);
                if (Vector3.Angle(normal, Vector3.up) > 18f) continue;
                position = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static void CreateBush(Transform parent, Vector3 position, Vector3 normal, System.Random random)
        {
            var bush = CreateRoot(parent, "Bush", position, normal);
            float size = .65f + (float)random.NextDouble() * .65f;
            AddMesh(bush.transform, "Bush Stem", DecorMeshLibrary.TrunkMesh(), new Vector3(0f, 0f, 0f),
                Quaternion.identity, new Vector3(size * .16f, size * .58f, size * .16f), DecorMaterials.Trunk);

            int clusters = 5 + random.Next(4);
            Mesh leafMesh = DecorMeshLibrary.OctahedronMesh();
            for (int i = 0; i < clusters; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float spread = size * (.16f + (float)random.NextDouble() * .28f);
                float height = size * (.42f + (float)random.NextDouble() * .52f);
                float scale = size * (.35f + (float)random.NextDouble() * .34f);
                AddMesh(bush.transform, "Angular Foliage", leafMesh,
                    new Vector3(Mathf.Cos(angle) * spread, height, Mathf.Sin(angle) * spread),
                    Quaternion.Euler(Range(random, -18f, 18f), Range(random, 0f, 360f), Range(random, -18f, 18f)),
                    new Vector3(scale, scale * .82f, scale), i % 2 == 0 ? DecorMaterials.Bush : DecorMaterials.BushDark);
            }
        }

        private static void CreateTreeCluster(Transform parent, Vector3 position, Vector3 normal, MountainSettings settings, System.Random random)
        {
            CreateTree(parent, position, normal, random);
            int extra = 2 + random.Next(3);
            for (int i = 0; i < extra; i++)
            {
                float dx = Range(random, -2.8f, 2.8f);
                float dz = Range(random, -2.8f, 2.8f);
                float x = position.x + dx;
                float z = position.z + dz;
                if (x * x + z * z > settings.Radius * settings.Radius * .86f) continue;
                if (!MountainGenerator.IsClearOfRoute(settings, x, z, 1.2f)) continue;
                float y = MountainGenerator.HeightAt(settings, x, z);
                if (y < settings.ValleyY + 1f || Mathf.InverseLerp(settings.ValleyY, settings.SummitY, y) > .5f) continue;
                Vector3 extraNormal = MountainGenerator.NormalAt(settings, x, z);
                if (Vector3.Angle(extraNormal, Vector3.up) > 24f || !IsFlatShelf(settings, x, z)) continue;
                CreateTree(parent, new Vector3(x, y, z), extraNormal, random);
            }
        }

        private static void CreateTree(Transform parent, Vector3 position, Vector3 normal, System.Random random)
        {
            var tree = CreateRoot(parent, "Mountain Pine", position, normal);
            float height = 3.2f + (float)random.NextDouble() * 3.1f;
            float trunkWidth = height * .11f;
            AddMesh(tree.transform, "Tapered Trunk", DecorMeshLibrary.TrunkMesh(), Vector3.zero, Quaternion.identity,
                new Vector3(trunkWidth, height * .7f, trunkWidth), DecorMaterials.Trunk);

            int tiers = 4;
            Mesh foliage = DecorMeshLibrary.PineMesh();
            for (int i = 0; i < tiers; i++)
            {
                float tierRadius = height * (.38f - i * .065f);
                float tierHeight = height * (.39f - i * .025f);
                float tierY = height * (.24f + i * .185f);
                var tier = AddMesh(tree.transform, "Pine Branch Tier", foliage,
                    new Vector3(0f, tierY, 0f), Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                    new Vector3(tierRadius * 2f, tierHeight, tierRadius * 2f),
                    i % 2 == 0 ? DecorMaterials.Pine : DecorMaterials.PineDark);
                tier.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private static void CreateRock(Transform parent, Vector3 position, Vector3 normal, MountainSettings settings, System.Random random)
        {
            float width = .75f + (float)random.NextDouble() * 1.8f;
            float height = .55f + (float)random.NextDouble() * 1.5f;
            float footprint = Mathf.Max(width, height) * .5f;
            float baseY = TerrainFloorHeight(settings, position, footprint);
            var rock = CreateRoot(parent, "Angular Boulder", new Vector3(position.x, baseY, position.z), normal);
            rock.transform.localRotation *= Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
            Mesh[] rockMeshes = DecorMeshLibrary.RockMeshes();
            Mesh mesh = rockMeshes[random.Next(rockMeshes.Length)];
            AddMesh(rock.transform, "Faceted Stone", mesh, Vector3.zero, Quaternion.identity,
                new Vector3(width, height, width * (.7f + (float)random.NextDouble() * .6f)),
                random.Next(3) == 0 ? DecorMaterials.RockDark : DecorMaterials.Rock);

            if (random.Next(3) != 0) return;
            float pebbleWidth = width * .34f;
            Vector3 pebblePosition = new Vector3(Range(random, -width * .52f, width * .52f), 0f,
                Range(random, -width * .52f, width * .52f));
            AddMesh(rock.transform, "Stone Fragment", rockMeshes[random.Next(rockMeshes.Length)],
                pebblePosition, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                new Vector3(pebbleWidth, height * .35f, pebbleWidth), DecorMaterials.RockDark);
        }

        private static float TerrainFloorHeight(MountainSettings settings, Vector3 position, float radius)
        {
            float lowest = MountainGenerator.HeightAt(settings, position.x, position.z);
            int samples = 8;
            for (int i = 0; i < samples; i++)
            {
                float angle = i / (float)samples * Mathf.PI * 2f;
                float x = position.x + Mathf.Cos(angle) * radius;
                float z = position.z + Mathf.Sin(angle) * radius;
                lowest = Mathf.Min(lowest, MountainGenerator.HeightAt(settings, x, z));
            }
            return lowest;
        }

        private static void CreateGrassTuft(Transform parent, Vector3 position, Vector3 normal, System.Random random)
        {
            var tuft = CreateRoot(parent, "Alpine Grass Tuft", position, normal);
            int blades = 6 + random.Next(5);
            Mesh bladeMesh = DecorMeshLibrary.GrassBladeMesh();
            for (int i = 0; i < blades; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float height = .32f + (float)random.NextDouble() * .62f;
                float width = .085f + (float)random.NextDouble() * .065f;
                float spread = (float)random.NextDouble() * .16f;
                Vector3 localPosition = new Vector3(Mathf.Cos(angle) * spread, .005f, Mathf.Sin(angle) * spread);
                Quaternion rotation = Quaternion.Euler(Range(random, -12f, 16f), angle * Mathf.Rad2Deg,
                    Range(random, -20f, 20f));
                AddMesh(tuft.transform, "Leaf Blade", bladeMesh, localPosition, rotation,
                    new Vector3(width, height, width), i % 3 == 0 ? DecorMaterials.GrassDark : DecorMaterials.Grass);
            }
        }

        private static void CreateFlower(Transform parent, Vector3 position, Vector3 normal, System.Random random)
        {
            var flower = CreateRoot(parent, "Alpine Flower", position, normal);
            float height = .3f + (float)random.NextDouble() * .22f;
            AddMesh(flower.transform, "Stem", DecorMeshLibrary.TrunkMesh(), Vector3.zero, Quaternion.identity,
                new Vector3(.045f, height, .045f), DecorMaterials.Bush);

            Material petalMaterial = random.Next(3) == 0 ? DecorMaterials.FlowerWhite
                : random.Next(2) == 0 ? DecorMaterials.FlowerYellow : DecorMaterials.FlowerRed;
            Mesh petal = DecorMeshLibrary.PetalMesh();
            int petals = 5 + random.Next(3);
            for (int i = 0; i < petals; i++)
            {
                float angle = i * (360f / petals) + Range(random, -8f, 8f);
                AddMesh(flower.transform, "Petal", petal, new Vector3(0f, height * .9f, 0f),
                    Quaternion.Euler(0f, angle, 0f), new Vector3(.24f, .045f, .3f), petalMaterial);
            }
            AddMesh(flower.transform, "Flower Center", DecorMeshLibrary.OctahedronMesh(), new Vector3(0f, height * .98f, 0f),
                Quaternion.identity, Vector3.one * .085f, DecorMaterials.FlowerYellow);
        }

        private static GameObject CreateRoot(Transform parent, string name, Vector3 position, Vector3 normal)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal.normalized);
            return root;
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            return AddMesh(parent, name, mesh, localPosition, Quaternion.identity, localScale, material);
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Vector3 localPosition,
            Quaternion localRotation, Vector3 localScale, Material material)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}