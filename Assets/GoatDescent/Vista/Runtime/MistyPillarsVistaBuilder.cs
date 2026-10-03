using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.ProceduralWorld
{
    public static class MistyPillarsVistaBuilder
    {
        public const string RootName = "Misty forest pillars vista";
        public const string RockObjectName = "Stratified sandstone";
        public const string ForestObjectName = "Dense broadleaf forest and ledge shrubs";
        public const string BranchObjectName = "Tree trunks and branches";
        public const string SpawnMarkerName = "Pillar Goat Spawn";
        public const int SpawnTowerSeed = 21;
        public const int DefaultRandomSeed = 28471;
        public const int DefaultTowerCount = 26;
        public const int ForegroundTowerCount = 7;
        public const int KindClassic = 0;
        public const int KindSpire = 1;
        public const int KindMesa = 2;
        public const int KindLeaning = 3;
        public static readonly Vector3 RootPosition = new Vector3(400f, 0f, 1200f);
        public static readonly Quaternion RootRotation = Quaternion.Euler(0f, -90f, 0f);

        public readonly struct TowerSpec
        {
            public readonly Vector3 position;
            public readonly float height;
            public readonly float rx;
            public readonly float rz;
            public readonly int seed;
            public readonly int detail;
            public readonly float terraceBoost;
            public readonly bool neutralRockColors;

            public TowerSpec(Vector3 position, float height, float rx, float rz, int seed, int detail)
                : this(position, height, rx, rz, seed, detail, 0f, false) { }

            public TowerSpec(Vector3 position, float height, float rx, float rz, int seed, int detail, float terraceBoost, bool neutralRockColors)
            {
                this.position = position;
                this.height = height;
                this.rx = rx;
                this.rz = rz;
                this.seed = seed;
                this.detail = detail;
                this.terraceBoost = terraceBoost;
                this.neutralRockColors = neutralRockColors;
            }
        }

        public interface IMeshSink
        {
            GameObject Emit(Transform parent, string objectName, string assetName, PillarMeshBuilder builder, Material material, bool flatten);
        }

        public static List<TowerSpec> BuildLayout(int towerCount, int randomSeed, float heightMin, float heightMax, float radiusMin, float radiusMax)
        {
            var towers = new List<TowerSpec>
            {
                new TowerSpec(new Vector3(-194f, -55f, -20f), 420f, 65f, 42f, 7, 1),
                new TowerSpec(new Vector3(193f, -45f, 34f), 369f, 70f, 53f, 13, 1),
                new TowerSpec(new Vector3(-66f, -34f, 165f), 335f, 62f, 47f, SpawnTowerSeed, 0),
                new TowerSpec(new Vector3(-140f, -40f, 440f), 321f, 39f, 34f, 27, 2),
                new TowerSpec(new Vector3(95f, -40f, 565f), 392f, 62f, 42f, 34, 2),
                new TowerSpec(new Vector3(168f, -42f, 340f), 300f, 38f, 31f, 42, 2),
                new TowerSpec(new Vector3(22f, -40f, 380f), 251f, 28f, 24f, 51, 2)
            };
            var random = new System.Random(randomSeed);
            int background = Mathf.Max(0, towerCount - ForegroundTowerCount);
            for (int i = 0; i < background; i++)
            {
                float z = 660f + i / 5 * 210f + Next(random, -65f, 65f);
                float x = (i % 5 - 2) * 165f + Next(random, -45f, 45f);
                float h = Next(random, heightMin, heightMax);
                float radius = Next(random, radiusMin, radiusMax);
                towers.Add(new TowerSpec(new Vector3(x, -45f, z), h, radius, radius * Next(random, .65f, 1.1f), 100 + i, 3));
            }
            return towers;
        }

        public static Transform CreateTower(Transform parent, in TowerSpec spec, Material rock, Material foliage, Material bark, IMeshSink sink, bool strictSpawnMarker)
        {
            Vector2 legacyClearing = spec.seed == SpawnTowerSeed ? new Vector2(0f, 25f) : default;
            return CreateTower(parent, spec, rock, foliage, bark, sink, strictSpawnMarker, legacyClearing, spec.seed == SpawnTowerSeed ? 26f : 0f);
        }

        public static Transform CreateTower(Transform parent, in TowerSpec spec, Material rock, Material foliage, Material bark, IMeshSink sink, bool strictSpawnMarker,
            Vector2 clearingCenter, float clearingRadius)
        {
            int kind = SilhouetteKindOf(spec.seed);
            int vegetation = VegetationKindOf(spec.seed);
            var tower = new GameObject("Forest pillar " + spec.seed);
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = spec.position;
            tower.transform.localRotation = Quaternion.Euler(0f, spec.seed * 19f % 360f, 0f);

            var stone = new PillarMeshBuilder();
            AddRock(stone, Vector3.zero, spec.height, spec.rx, spec.rz, spec.seed, kind, spec.terraceBoost, spec.detail > 1 ? 38 : 58, spec.detail > 1 ? 38 : 66);
            var random = new System.Random(spec.seed * 197 + 41);
            for (int i = 0; i < (spec.detail > 1 ? 2 : 4); i++)
            {
                float a = i * 2.39996f + spec.seed;
                Vector3 offset = new Vector3(Mathf.Cos(a) * spec.rx * .62f, -4f, Mathf.Sin(a) * spec.rz * .62f);
                AddRock(stone, offset, spec.height * Next(random, .46f, .82f), spec.rx * Next(random, .22f, .4f), spec.rz * Next(random, .24f, .44f), spec.seed + i * 71, KindClassic, spec.terraceBoost, 28, 30);
            }
            if (spec.neutralRockColors)
                for (int i = 0; i < stone.colors.Count; i++) stone.colors[i] = new Color(0f, 0f, 0f, 0f);
            var stoneObject = sink.Emit(tower.transform, RockObjectName, "Cliff_" + spec.seed, stone, rock, true);
            var surface = stoneObject.AddComponent<MeshCollider>();
            surface.sharedMesh = stoneObject.GetComponent<MeshFilter>().sharedMesh;
            stoneObject.AddComponent<GoatDescent.MountainSlopeSurface>();
            Transform marker = spec.seed == SpawnTowerSeed ? CreateSpawnMarker(tower.transform, surface, spec.height, strictSpawnMarker) : null;

            var leaves = new PillarMeshBuilder();
            var wood = new PillarMeshBuilder();
            float treeFactor = vegetation == 2 ? .3f : vegetation == 1 ? .65f : 1f;
            float shrubFactor = vegetation == 2 ? .4f : vegetation == 1 ? .75f : 1f;
            float patchFactor = vegetation == 2 ? .5f : vegetation == 1 ? .8f : 1f;
            int treeCount = Mathf.Max(4, Mathf.RoundToInt((spec.detail == 3 ? 26 : spec.detail == 2 ? 48 : 105) * treeFactor));
            Vector2 topLean = kind == KindClassic ? Vector2.zero : LeanAt(kind, spec.seed, spec.rx, spec.rz, 1f);
            float treeScale = TopTreeScale(kind);
            for (int i = 0; i < treeCount; i++)
            {
                float a = i * 2.399963f, r = Mathf.Sqrt((i + .5f) / treeCount) * .82f;
                float x = Mathf.Cos(a) * spec.rx * r * treeScale + topLean.x;
                float z = Mathf.Sin(a) * spec.rz * r * treeScale + topLean.y;
                float y = spec.height - 3f + (1f - r) * 7f + Noise(x * .035f, z * .035f, spec.seed) * 2f;
                float treeHeight = Next(random, 8f, spec.detail == 3 ? 18f : 29f) * (1.25f - r * .5f);
                if (clearingRadius > 0f && new Vector2(x - clearingCenter.x, z - clearingCenter.y).magnitude < clearingRadius) continue;
                AddTree(leaves, wood, new Vector3(x, y, z), treeHeight, random, spec.detail > 1);
                float undergrowth = Next(random, 3f, 6.5f);
                leaves.AddCrown(new Vector3(x, y + 1f, z), new Vector3(undergrowth * 1.5f, undergrowth * .7f, undergrowth), spec.seed + i, CanopyTint(random), 9, 6);
            }

            int shrubs = Mathf.Max(6, Mathf.RoundToInt((spec.detail > 1 ? 35 : 135) * shrubFactor));
            for (int i = 0; i < shrubs; i++)
            {
                float t = Next(random, .25f, .99f), angle = Next(random, 0f, Mathf.PI * 2f);
                Vector3 p = RockPoint(t, angle, spec.height, spec.rx, spec.rz, spec.seed, kind, spec.terraceBoost);
                p.x *= .965f; p.z *= .965f;
                float size = Next(random, 1.8f, 3.8f) * Mathf.Lerp(.7f, 1.4f, t);
                for (int lobe = 0; lobe < 4; lobe++)
                {
                    Vector3 offset = new Vector3(Next(random, -size, size), Next(random, -size * .6f, size * .6f), Next(random, -size, size));
                    leaves.AddCrown(p + offset, new Vector3(size, size * .85f, size), spec.seed + i * 7 + lobe, CanopyTint(random), 8, 5);
                }
            }

            int patchCount = Mathf.Max(12, Mathf.RoundToInt((spec.detail > 1 ? 105 : 330) * patchFactor));
            for (int i = 0; i < patchCount; i++)
            {
                int fissure = i % 5;
                float t = Next(random, .64f, 1f);
                float angle = fissure * 1.256637f + spec.seed * .41f + Mathf.Sin(t * 12f + fissure) * .13f + Next(random, -.22f, .22f);
                Vector3 p = RockPoint(t, angle, spec.height, spec.rx, spec.rz, spec.seed, kind, spec.terraceBoost);
                p.x *= .975f; p.z *= .975f;
                float size = Next(random, 3f, 6.2f) * Mathf.Lerp(.7f, 1.2f, (t - .64f) / .36f);
                Color tint = CanopyTint(random);
                for (int lobe = 0; lobe < 3; lobe++)
                {
                    Vector3 offset = new Vector3(Next(random, -size, size), Next(random, -size, size), Next(random, -size, size));
                    leaves.AddCrown(p + offset, new Vector3(size, size * .82f, size * .9f), spec.seed * 43 + i * 5 + lobe, tint, 8, 5);
                }
            }

            sink.Emit(tower.transform, ForestObjectName, "Forest_" + spec.seed, leaves, foliage, false);
            sink.Emit(tower.transform, BranchObjectName, "Branches_" + spec.seed, wood, bark, false);
            return marker;
        }

        private static Transform CreateSpawnMarker(Transform tower, MeshCollider surface, float height, bool strict)
        {
            var marker = new GameObject(SpawnMarkerName).transform;
            marker.SetParent(tower, false);
            Vector3 probe = tower.TransformPoint(new Vector3(0f, height + 25f, 25f));
            Physics.SyncTransforms();
            if (surface.Raycast(new Ray(probe, Vector3.down), out RaycastHit hit, 60f))
            {
                marker.position = hit.point + Vector3.up * .12f;
                marker.rotation = Quaternion.LookRotation(tower.TransformDirection(Vector3.forward), Vector3.up);
                return marker;
            }
            if (strict)
            {
                UnityEngine.Object.DestroyImmediate(marker.gameObject);
                throw new InvalidOperationException("Pillar spawn must hit its rendered rock surface.");
            }
            Debug.LogWarning("MISTY_PILLARS spawn raycast missed " + tower.name + "; using tower top fallback.");
            marker.position = tower.TransformPoint(new Vector3(0f, height + 7.5f, 0f));
            marker.rotation = Quaternion.LookRotation(tower.TransformDirection(Vector3.forward), Vector3.up);
            return marker;
        }

        private static Color CanopyTint(System.Random random) { float f = Next(random, .68f, 1.25f); return new Color(.48f * f, .65f * f, .31f * f, 1f); }

        private static void AddTree(PillarMeshBuilder leaves, PillarMeshBuilder wood, Vector3 origin, float height, System.Random random, bool distant)
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

        private static void AddRock(PillarMeshBuilder builder, Vector3 origin, float height, float rx, float rz, int seed, int kind, float terraceBoost, int sides, int rings)
        {
            int first = builder.vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    builder.AddVertex(origin + RockPoint(t, a, height, rx, rz, seed, kind, terraceBoost), new Color(.75f + .2f * Noise(a, t * 3f, seed), t, .5f, 1f));
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

        public static float Noise(float x, float y, int seed) => Mathf.PerlinNoise(x + seed * 3.71f, y + seed * 1.13f) * 2f - 1f;
        public static float Next(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        public static int SilhouetteKindOf(int seed) => seed == SpawnTowerSeed ? KindClassic : (int)(((uint)seed * 2654435761u) >> 30);
        public static int VegetationKindOf(int seed) => seed == SpawnTowerSeed ? 0 : Mathf.Min((int)(((uint)seed * 374761393u) >> 30), 2);

        private static Vector2 LeanAt(int kind, int seed, float rx, float rz, float t)
        {
            float phase = seed * .73f;
            float lx = Mathf.Sin(t * 2.5f + phase) * rx * .10f;
            float lz = Mathf.Cos(t * 3.1f + phase) * rz * .09f;
            if (kind == KindLeaning)
            {
                float dir = ((uint)seed & 1u) == 0u ? 1f : -1f;
                lx += dir * rx * .24f * t;
                lz += dir * rz * .11f * t;
            }
            return new Vector2(lx, lz);
        }

        private static float TopTreeScale(int kind) => kind == KindSpire ? .30f : kind == KindMesa ? 1.04f : 1f;

        public static Vector3 RockPoint(float t, float angle, float height, float rx, float rz, int seed, int kind, float terraceBoost = 0f)
        {
            float phase = seed * .73f;
            float outline = 1f + .14f * Mathf.Sin(angle * 3f + phase) + .09f * Mathf.Sin(angle * 7f - phase * 1.7f);
            float grooves = .075f * Mathf.Sin(angle * 19f + phase) + .04f * Mathf.Sin(angle * 37f - phase);
            float erosion = .045f * Noise(Mathf.Cos(angle) * 2.5f + t * .8f, Mathf.Sin(angle) * 2.5f + t * 7f, seed);
            float profile;
            switch (kind)
            {
                case KindSpire:
                    profile = 1.06f - .10f * t + .03f * Mathf.Sin(t * 17f + phase) - .05f * Mathf.Repeat(t * 13f + phase, 1f);
                    profile += .15f * Mathf.Pow(1f - t, 4f);
                    profile *= Mathf.Lerp(1f, .30f, Mathf.Pow(t, 1.7f));
                    break;
                case KindMesa:
                    profile = 1.02f + .045f * Mathf.Sin(t * 21f + phase);
                    profile *= Mathf.Lerp(.86f, 1.06f, Mathf.Pow(t, .7f));
                    if (t > .88f) profile *= Mathf.Lerp(1f, .965f, (t - .88f) / .12f);
                    break;
                default:
                    profile = 1.10f - .12f * t + .035f * Mathf.Sin(t * 13f + phase) - .055f * Mathf.Repeat(t * 11f + phase, 1f);
                    profile += .18f * Mathf.Pow(1f - t, 4f);
                    if (t > .94f) profile *= Mathf.Lerp(1f, .82f, (t - .94f) / .06f);
                    break;
            }
            float radius = profile * (outline + grooves + erosion);
            if (terraceBoost > 0f)
            {
                float ft = t * 8f;
                ft -= Mathf.Floor(ft);
                float ledge = Mathf.SmoothStep(.10f, .30f, ft) * (1f - Mathf.SmoothStep(.60f, .85f, ft));
                radius *= 1f + .27f * terraceBoost * ledge;
            }
            Vector2 lean = LeanAt(kind, seed, rx, rz, t);
            float terrace = (Mathf.Sin(angle * 4f + phase) * 1.5f + Noise(t * 18f, angle * 3f, seed) * 2.4f) * (1f + terraceBoost * .5f);
            return new Vector3(Mathf.Cos(angle) * rx * radius + lean.x, height * t + terrace * Mathf.SmoothStep(0f, 1f, t * 8f), Mathf.Sin(angle) * rz * radius + lean.y);
        }
    }
}
