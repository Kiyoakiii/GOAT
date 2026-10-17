using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class FrozenRiftMesh
    {
        private static readonly Color Rock = new Color(0f, 0f, 0f, 1f);
        public static float Channel(float z) => Mathf.Sin(z * .009f) * 24f + Mathf.Sin(z * .021f) * 7f;
        public static float ShelfHeight(int tier, float z) => 65f + (3 - tier) * 46f + Mathf.Floor((z + 230f) / 95f) * 15f + Mathf.Sin(z * .014f + tier * 1.3f) * 6f;

        public static Mesh Shelf(int side, int tier, bool snow)
        {
            var mesh = new CrownRockMesh();
            const int steps = 90;
            Vector3 Point(int index, bool outer)
            {
                float z = Mathf.Lerp(-230f, 340f, index / (float)steps);
                float distance = 62f + (3 - tier) * 57f + (outer ? 60f : 0f);
                float fracture = Mathf.Sin(index * 1.73f + tier * 1.4f + side) * 2f + Mathf.Sin(index * .29f + tier * 2f + side) * 11f;
                float x = Channel(z) + side * (distance + fracture);
                float y = ShelfHeight(tier, z) + (snow ? .45f : 0f);
                y += Mathf.Sin(index * .71f + tier) * (snow ? .7f : .35f);
                return new Vector3(x, y, z);
            }
            for (int i = 0; i < steps; i++)
            {
                Vector3 a = Point(i, false), b = Point(i + 1, false), c = Point(i, true), d = Point(i + 1, true);
                if (side < 0) { Tri(mesh, a, c, b); Tri(mesh, c, d, b); }
                else { Tri(mesh, a, b, c); Tri(mesh, c, b, d); }
                if (snow)
                {
                    Vector3 ab = a + Vector3.down * (1.1f + Mathf.Sin(i * 1.7f) * .6f);
                    Vector3 bb = b + Vector3.down * (1.1f + Mathf.Sin((i + 1) * 1.7f) * .6f);
                    if (side < 0) Quad(mesh, a, b, bb, ab); else Quad(mesh, b, a, ab, bb);
                    continue;
                }
                const int layers = 14;
                for (int layer = 0; layer < layers; layer++)
                {
                    Vector3 Wall(Vector3 top, int ring, int index)
                    {
                        float t = ring / (float)layers;
                        top.y = Mathf.Lerp(top.y, -115f, t);
                        top.x += side * (Mathf.Sin(ring * .9f + index * .35f) * 3.5f + Mathf.Sin(index * .53f + ring * .24f) * 2f + t * 9f);
                        return top;
                    }
                    Vector3 aa = Wall(a, layer, i), bb = Wall(b, layer, i + 1);
                    Vector3 ab = Wall(a, layer + 1, i), ba = Wall(b, layer + 1, i + 1);
                    if (side < 0) Quad(mesh, aa, bb, ba, ab); else Quad(mesh, bb, aa, ab, ba);
                    aa = Wall(c, layer, i); bb = Wall(d, layer, i + 1);
                    ab = Wall(c, layer + 1, i); ba = Wall(d, layer + 1, i + 1);
                    if (side < 0) Quad(mesh, bb, aa, ab, ba); else Quad(mesh, aa, bb, ba, ab);
                }
                if (i == 0 || i == steps - 1)
                {
                    Vector3 left = i == 0 ? a : b, right = i == 0 ? c : d;
                    Vector3 lb = new Vector3(left.x + side * 9f, -115f, left.z), rb = new Vector3(right.x + side * 9f, -115f, right.z);
                    if ((i == 0) == (side > 0)) Quad(mesh, right, rb, lb, left); else Quad(mesh, left, lb, rb, right);
                }
            }
            return mesh.Build(snow ? "Снежная полка" : "Скальная стена разлома");
        }

        public static Mesh Crystal()
        {
            var mesh = new CrownRockMesh();
            const int sides = 6;
            var lower = new Vector3[sides]; var shoulder = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                lower[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                shoulder[i] = new Vector3(Mathf.Cos(a) * .66f + .13f, .68f + Mathf.Sin(i * 1.7f) * .06f, Mathf.Sin(a) * .7f);
            }
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Quad(mesh, lower[i], shoulder[i], shoulder[next], lower[next]);
                Tri(mesh, shoulder[i], new Vector3(.28f, 1f, -.08f), shoulder[next]);
                Tri(mesh, Vector3.zero, lower[i], lower[next]);
            }
            return mesh.Build("Гранёный ледяной массив");
        }

        public static Mesh Pine(bool snow)
        {
            var mesh = new CrownRockMesh();
            const int sides = 10;
            for (int tier = 0; tier < 6; tier++)
            {
                float bottom = .15f + tier * .115f, top = bottom + .31f;
                float width = .25f * (1f - tier * .12f);
                var peak = new Vector3(.008f * tier, top + (snow ? .008f : 0f), 0f);
                for (int i = 0; i < sides; i++)
                {
                    Vector3 Rim(int index)
                    {
                        float a = index * Mathf.PI * 2f / sides;
                        float r = width * (1f + Mathf.Sin(index * 2.3f + tier) * .08f) * (snow ? .83f : 1f);
                        return new Vector3(Mathf.Cos(a) * r, bottom + (snow ? .06f : 0f) + Mathf.Sin(index * 1.9f + tier) * .025f, Mathf.Sin(a) * r);
                    }
                    Tri(mesh, peak, Rim(i + 1), Rim(i));
                    if (!snow) Tri(mesh, new Vector3(0f, bottom, 0f), Rim(i), Rim(i + 1));
                }
            }
            return mesh.Build(snow ? "Снег на еловых лапах" : "Еловые лапы");
        }

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c) => mesh.Triangle(a, b, c, Rock, Rock, Rock);
        private static void Quad(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Tri(mesh, a, b, c); Tri(mesh, a, c, d); }
    }
}
