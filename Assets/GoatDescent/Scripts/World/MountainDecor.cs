using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class MountainDecor
    {
        private static Material _bush;
        private static Material _bushDark;
        private static Material _rock;
        private static Material _rockDark;
        private static Material _trunk;
        private static Material _pine;
        private static Material _pineDark;
        private static Material _flowerWhite;
        private static Material _flowerYellow;
        private static Material _flowerRed;
        private static Material _grass;
        private static Material _grassDark;
        private static Mesh[] _rockMeshes;
        private static Mesh _trunkMesh;
        private static Mesh _pineMesh;
        private static Mesh _octahedronMesh;
        private static Mesh _grassBladeMesh;
        private static Mesh _petalMesh;

        private static Material BushMat() => _bush ??= Mat(new Color(.30f, .52f, .24f), .25f);
        private static Material BushDarkMat() => _bushDark ??= Mat(new Color(.16f, .32f, .16f), .3f);
        private static Material RockMat() => _rock ??= Mat(new Color(.54f, .55f, .58f), .06f);
        private static Material RockDarkMat() => _rockDark ??= Mat(new Color(.34f, .36f, .40f), .05f);
        private static Material TrunkMat() => _trunk ??= Mat(new Color(.30f, .20f, .11f), .05f);
        private static Material PineMat() => _pine ??= Mat(new Color(.10f, .30f, .16f), .3f);
        private static Material PineDarkMat() => _pineDark ??= Mat(new Color(.07f, .22f, .13f), .3f);
        private static Material FlowerWhiteMat() => _flowerWhite ??= Mat(new Color(.96f, .95f, .92f), .4f);
        private static Material FlowerYellowMat() => _flowerYellow ??= Mat(new Color(.98f, .84f, .28f), .4f);
        private static Material FlowerRedMat() => _flowerRed ??= Mat(new Color(.88f, .32f, .30f), .4f);
        private static Material GrassMat() => _grass ??= Mat(new Color(.34f, .58f, .24f), .15f);
        private static Material GrassDarkMat() => _grassDark ??= Mat(new Color(.22f, .42f, .18f), .15f);

        private static Material Mat(Color color, float gloss)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", gloss);
            return material;
        }

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
            AddMesh(bush.transform, "Bush Stem", TrunkMesh(), new Vector3(0f, 0f, 0f),
                Quaternion.identity, new Vector3(size * .16f, size * .58f, size * .16f), TrunkMat());

            int clusters = 5 + random.Next(4);
            Mesh leafMesh = OctahedronMesh();
            for (int i = 0; i < clusters; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float spread = size * (.16f + (float)random.NextDouble() * .28f);
                float height = size * (.42f + (float)random.NextDouble() * .52f);
                float scale = size * (.35f + (float)random.NextDouble() * .34f);
                AddMesh(bush.transform, "Angular Foliage", leafMesh,
                    new Vector3(Mathf.Cos(angle) * spread, height, Mathf.Sin(angle) * spread),
                    Quaternion.Euler(Range(random, -18f, 18f), Range(random, 0f, 360f), Range(random, -18f, 18f)),
                    new Vector3(scale, scale * .82f, scale), i % 2 == 0 ? BushMat() : BushDarkMat());
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
            AddMesh(tree.transform, "Tapered Trunk", TrunkMesh(), Vector3.zero, Quaternion.identity,
                new Vector3(trunkWidth, height * .7f, trunkWidth), TrunkMat());

            int tiers = 4;
            Mesh foliage = PineMesh();
            for (int i = 0; i < tiers; i++)
            {
                float tierRadius = height * (.38f - i * .065f);
                float tierHeight = height * (.39f - i * .025f);
                float tierY = height * (.24f + i * .185f);
                var tier = AddMesh(tree.transform, "Pine Branch Tier", foliage,
                    new Vector3(0f, tierY, 0f), Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                    new Vector3(tierRadius * 2f, tierHeight, tierRadius * 2f),
                    i % 2 == 0 ? PineMat() : PineDarkMat());
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
            EnsureRockMeshes();
            Mesh mesh = _rockMeshes[random.Next(_rockMeshes.Length)];
            AddMesh(rock.transform, "Faceted Stone", mesh, Vector3.zero, Quaternion.identity,
                new Vector3(width, height, width * (.7f + (float)random.NextDouble() * .6f)),
                random.Next(3) == 0 ? RockDarkMat() : RockMat());

            if (random.Next(3) != 0) return;
            float pebbleWidth = width * .34f;
            Vector3 pebblePosition = new Vector3(Range(random, -width * .52f, width * .52f), 0f,
                Range(random, -width * .52f, width * .52f));
            AddMesh(rock.transform, "Stone Fragment", _rockMeshes[random.Next(_rockMeshes.Length)],
                pebblePosition, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                new Vector3(pebbleWidth, height * .35f, pebbleWidth), RockDarkMat());
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
            Mesh bladeMesh = GrassBladeMesh();
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
                    new Vector3(width, height, width), i % 3 == 0 ? GrassDarkMat() : GrassMat());
            }
        }

        private static void CreateFlower(Transform parent, Vector3 position, Vector3 normal, System.Random random)
        {
            var flower = CreateRoot(parent, "Alpine Flower", position, normal);
            float height = .3f + (float)random.NextDouble() * .22f;
            AddMesh(flower.transform, "Stem", TrunkMesh(), Vector3.zero, Quaternion.identity,
                new Vector3(.045f, height, .045f), BushMat());

            Material petalMaterial = random.Next(3) == 0 ? FlowerWhiteMat()
                : random.Next(2) == 0 ? FlowerYellowMat() : FlowerRedMat();
            Mesh petal = PetalMesh();
            int petals = 5 + random.Next(3);
            for (int i = 0; i < petals; i++)
            {
                float angle = i * (360f / petals) + Range(random, -8f, 8f);
                AddMesh(flower.transform, "Petal", petal, new Vector3(0f, height * .9f, 0f),
                    Quaternion.Euler(0f, angle, 0f), new Vector3(.24f, .045f, .3f), petalMaterial);
            }
            AddMesh(flower.transform, "Flower Center", OctahedronMesh(), new Vector3(0f, height * .98f, 0f),
                Quaternion.identity, Vector3.one * .085f, FlowerYellowMat());
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

        private static void EnsureRockMeshes()
        {
            if (_rockMeshes != null) return;
            _rockMeshes = new Mesh[5];
            for (int i = 0; i < _rockMeshes.Length; i++)
                _rockMeshes[i] = CreateRockMesh(i * 7919 + 53);
        }

        private static Mesh CreateRockMesh(int seed)
        {
            var random = new System.Random(seed);
            const int segments = 7;
            float[,] levelHeights = { { 0f, .3f, .72f }, { .36f, .5f, .34f } };
            var rings = new Vector3[3, segments];
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = (i / (float)segments + (float)random.NextDouble() * .035f) * Mathf.PI * 2f;
                    float radius = levelHeights[1, ring] * Mathf.Lerp(.82f, 1.16f, (float)random.NextDouble());
                    rings[ring, i] = new Vector3(Mathf.Cos(angle) * radius, levelHeights[0, ring], Mathf.Sin(angle) * radius);
                }
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    AddTriangle(vertices, triangles, rings[ring, i], rings[ring + 1, i], rings[ring, next]);
                    AddTriangle(vertices, triangles, rings[ring, next], rings[ring + 1, i], rings[ring + 1, next]);
                }
            }

            Vector3 peak = new Vector3(Range(random, -.08f, .08f), 1f, Range(random, -.08f, .08f));
            for (int i = 0; i < segments; i++)
            {
                AddTriangle(vertices, triangles, Vector3.zero, rings[0, i], rings[0, (i + 1) % segments]);
                AddTriangle(vertices, triangles, rings[2, i], peak, rings[2, (i + 1) % segments]);
            }

            var mesh = new Mesh { name = "Faceted Grounded Boulder" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh TrunkMesh()
        {
            if (_trunkMesh) return _trunkMesh;
            const int sides = 7;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 bottom0 = new Vector3(Mathf.Cos(a0) * .5f, 0f, Mathf.Sin(a0) * .5f);
                Vector3 bottom1 = new Vector3(Mathf.Cos(a1) * .5f, 0f, Mathf.Sin(a1) * .5f);
                Vector3 top0 = new Vector3(Mathf.Cos(a0) * .32f, 1f, Mathf.Sin(a0) * .32f);
                Vector3 top1 = new Vector3(Mathf.Cos(a1) * .32f, 1f, Mathf.Sin(a1) * .32f);
                AddTriangle(vertices, triangles, bottom0, top0, bottom1);
                AddTriangle(vertices, triangles, bottom1, top0, top1);
                AddTriangle(vertices, triangles, Vector3.zero, bottom0, bottom1);
            }
            _trunkMesh = MakeMesh("Tapered Grounded Stem", vertices, triangles);
            return _trunkMesh;
        }

        private static Mesh PineMesh()
        {
            if (_pineMesh) return _pineMesh;
            const int sides = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 apex = new Vector3(0f, 1f, 0f);
            Vector3 center = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * .5f, 0f, Mathf.Sin(a0) * .5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * .5f, 0f, Mathf.Sin(a1) * .5f);
                AddTriangle(vertices, triangles, p0, apex, p1);
                AddTriangle(vertices, triangles, center, p1, p0);
            }
            _pineMesh = MakeMesh("Low Poly Pine Cone", vertices, triangles);
            return _pineMesh;
        }

        private static Mesh OctahedronMesh()
        {
            if (_octahedronMesh) return _octahedronMesh;
            Vector3 top = new Vector3(0f, .5f, 0f);
            Vector3 bottom = new Vector3(0f, -.5f, 0f);
            Vector3[] ring =
            {
                new Vector3(.5f, 0f, 0f), new Vector3(0f, 0f, .5f),
                new Vector3(-.5f, 0f, 0f), new Vector3(0f, 0f, -.5f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                AddTriangle(vertices, triangles, top, ring[i], ring[next]);
                AddTriangle(vertices, triangles, bottom, ring[next], ring[i]);
            }
            _octahedronMesh = MakeMesh("Low Poly Foliage Cluster", vertices, triangles);
            return _octahedronMesh;
        }

        private static Mesh GrassBladeMesh()
        {
            if (_grassBladeMesh) return _grassBladeMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 leftBase = new Vector3(-.5f, 0f, 0f);
            Vector3 rightBase = new Vector3(.5f, 0f, 0f);
            Vector3 leftMid = new Vector3(-.26f, .56f, .08f);
            Vector3 rightMid = new Vector3(.26f, .56f, .08f);
            Vector3 tip = new Vector3(.10f, 1f, .18f);
            AddDoubleSidedTriangle(vertices, triangles, leftBase, leftMid, rightBase);
            AddDoubleSidedTriangle(vertices, triangles, rightBase, leftMid, rightMid);
            AddDoubleSidedTriangle(vertices, triangles, leftMid, tip, rightMid);
            _grassBladeMesh = MakeMesh("Curved Alpine Grass Blade", vertices, triangles);
            return _grassBladeMesh;
        }

        private static Mesh PetalMesh()
        {
            if (_petalMesh) return _petalMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 root = Vector3.zero;
            Vector3 left = new Vector3(-.5f, .12f, .3f);
            Vector3 tip = new Vector3(0f, .04f, 1f);
            Vector3 right = new Vector3(.5f, .12f, .3f);
            AddDoubleSidedTriangle(vertices, triangles, root, tip, left);
            AddDoubleSidedTriangle(vertices, triangles, root, right, tip);
            _petalMesh = MakeMesh("Faceted Flower Petal", vertices, triangles);
            return _petalMesh;
        }

        private static Mesh MakeMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDoubleSidedTriangle(List<Vector3> vertices, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, c, b, a);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
