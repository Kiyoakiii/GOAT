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

        private static Material BushMat() => _bush ??= Mat(new Color(.30f, .52f, .24f), .25f);
        private static Material BushDarkMat() => _bushDark ??= Mat(new Color(.16f, .32f, .16f), .3f);
        private static Material RockMat() => _rock ??= Mat(new Color(.54f, .55f, .58f), .06f);
        private static Material RockDarkMat() => _rockDark ??= Mat(new Color(.34f, .36f, .40f), .05f);
        private static Material TrunkMat() => _trunk ??= Mat(new Color(.30f, .20f, .11f), .05f);
        private static Material PineMat() => _pine ??= Mat(new Color(.10f, .30f, .16f), .3f);
        private static Material PineDarkMat() => _pineDark ??= Mat(new Color(.07f, .22f, .13f), .3f);

        private static Material Mat(Color color, float gloss)
        {
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Glossiness", gloss);
            return m;
        }

        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        public static void Generate(Transform parent, MountainSettings s)
        {
            if (!s.EnableDecor) return;

            var root = new GameObject("Decor");
            root.transform.SetParent(parent, false);

            var rng = new System.Random(s.Seed * 131 + 17);
            for (int i = 0; i < s.BushCount; i++)
                if (TryPlaceVegetation(rng, s, out Vector3 pos, out Vector3 normal, .7f, 26f))
                    CreateBush(root.transform, pos, normal, rng);
            for (int i = 0; i < s.TreeCount; i++)
                if (TryPlaceVegetation(rng, s, out Vector3 pos, out Vector3 normal, .55f, 32f))
                    CreateTree(root.transform, pos, normal, rng);
            for (int i = 0; i < s.RockCount; i++)
                if (TryPlaceRock(rng, s, out Vector3 pos, out Vector3 normal))
                    CreateRock(root.transform, pos, normal, rng);
        }

        private static bool TryPlaceVegetation(System.Random rng, MountainSettings s, out Vector3 pos, out Vector3 normal, float heightLimit01, float maxSlope)
        {
            pos = default; normal = Vector3.up;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float radial = (float)rng.NextDouble() * .92f;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * s.Radius * radial;
                float z = Mathf.Sin(angle) * s.Radius * radial;

                float y = MountainGenerator.HeightAt(s, x, z);
                float h01 = Mathf.InverseLerp(s.ValleyY, s.SummitY, y);
                if (h01 > heightLimit01 || h01 < .03f || y < s.ValleyY + 1.5f) continue;

                normal = MountainGenerator.NormalAt(s, x, z);
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope > maxSlope) continue;

                pos = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static bool TryPlaceRock(System.Random rng, MountainSettings s, out Vector3 pos, out Vector3 normal)
        {
            pos = default; normal = Vector3.up;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float radial = .1f + (float)rng.NextDouble() * .85f;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * s.Radius * radial;
                float z = Mathf.Sin(angle) * s.Radius * radial;

                float y = MountainGenerator.HeightAt(s, x, z);
                if (y < s.ValleyY + 2f) continue;

                normal = MountainGenerator.NormalAt(s, x, z);
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope < 5f || slope > 85f) continue;

                pos = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static void CreateBush(Transform parent, Vector3 pos, Vector3 normal, System.Random rng)
        {
            var bush = new GameObject("Bush");
            bush.transform.SetParent(parent, false);
            bush.transform.position = pos;
            bush.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            float r = .6f + (float)rng.NextDouble() * .7f;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(bush.transform, false);
            trunk.transform.localPosition = new Vector3(0f, r * .25f, 0f);
            trunk.transform.localScale = new Vector3(r * .09f, r * .25f, r * .09f);
            trunk.GetComponent<Renderer>().sharedMaterial = TrunkMat();
            DestroySafe(trunk.GetComponent<Collider>());

            int clumps = 3 + rng.Next(3);
            for (int i = 0; i < clumps; i++)
            {
                var clump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                clump.name = "Foliage";
                clump.transform.SetParent(bush.transform, false);
                float cx = (float)(rng.NextDouble() - .5f) * r * .9f;
                float cz = (float)(rng.NextDouble() - .5f) * r * .9f;
                float cy = r * (.55f + (float)rng.NextDouble() * .45f);
                clump.transform.localPosition = new Vector3(cx, cy, cz);
                float sc = r * (.4f + (float)rng.NextDouble() * .3f);
                clump.transform.localScale = new Vector3(sc, sc * .85f, sc);
                clump.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? BushMat() : BushDarkMat();
                DestroySafe(clump.GetComponent<Collider>());
            }
        }

        private static void CreateTree(Transform parent, Vector3 pos, Vector3 normal, System.Random rng)
        {
            var tree = new GameObject("Pine");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            float h = 3f + (float)rng.NextDouble() * 3f;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, h * .14f, 0f);
            trunk.transform.localScale = new Vector3(h * .08f, h * .14f, h * .08f);
            trunk.GetComponent<Renderer>().sharedMaterial = TrunkMat();
            DestroySafe(trunk.GetComponent<Collider>());

            int tiers = 4;
            for (int i = 0; i < tiers; i++)
            {
                var cone = new GameObject("Foliage");
                cone.transform.SetParent(tree.transform, false);
                float ty = h * (.28f + i * .22f);
                float w = h * (.42f - i * .07f);
                float th = h * (.30f - i * .03f);
                cone.transform.localPosition = new Vector3(0f, ty, 0f);
                cone.AddComponent<MeshFilter>().sharedMesh = ConeMesh(w, th);
                var r = cone.AddComponent<MeshRenderer>();
                r.sharedMaterial = i % 2 == 0 ? PineMat() : PineDarkMat();
            }
        }

        private static Mesh ConeMesh(float radius, float height)
        {
            const int segments = 12;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            verts.Add(Vector3.zero);
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(0); tris.Add(i + 2); tris.Add(i + 1);
            }
            var mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateRock(Transform parent, Vector3 pos, Vector3 normal, System.Random rng)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos;
            rock.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(rng.Next(360), rng.Next(360), rng.Next(360));
            float s = .8f + (float)rng.NextDouble() * 2.2f;
            rock.transform.localScale = new Vector3(
                s * (.65f + (float)rng.NextDouble() * .6f),
                s * (.55f + (float)rng.NextDouble() * .4f),
                s * (.65f + (float)rng.NextDouble() * .6f));
            rock.GetComponent<Renderer>().sharedMaterial = rng.Next(3) == 0 ? RockDarkMat() : RockMat();
            DestroySafe(rock.GetComponent<Collider>());

            if (rng.Next(3) != 0) return;
            var pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pebble.name = "Pebble";
            pebble.transform.SetParent(rock.transform, false);
            pebble.transform.localPosition = new Vector3(s * .55f, s * .1f, s * .1f);
            pebble.transform.localScale = Vector3.one * s * .4f;
            pebble.GetComponent<Renderer>().sharedMaterial = RockDarkMat();
            DestroySafe(pebble.GetComponent<Collider>());
        }
    }
}