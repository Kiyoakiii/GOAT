using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Thin stone lips with upward-facing tops for upright hooves.</summary>
    public sealed class CliffVeinField : MonoBehaviour
    {
        public struct Support
        {
            public Vector3 Point;
            public float Quality;
            public float Span;
            public int VeinIndex;
        }

        private struct Vein
        {
            public Vector2 A, B;
            public float Thickness, Quality;
        }

        private readonly List<Vein> veins = new List<Vein>();
        private Material strongStone;
        private Material weakStone;
        public int Count => veins.Count;

        public void Build()
        {
            strongStone = MakeStone("Rough gripping seam", new Color(.44f, .44f, .43f));
            weakStone = MakeStone("Smooth dark seam", new Color(.36f, .37f, .38f));
            // All four hooves start on the same upper lip, beneath the standing body.
            Add(new Vector2(-.78f, 17.65f), new Vector2(.78f, 17.65f), .045f, 1f);

            // Left: close, rough seams for a cautious descent.
            for (int i = 0; i < 28; i++)
            {
                float y = 17.45f - i * .60f;
                float x = -2.0f + Mathf.Sin(i * .37f) * .34f;
                Add(new Vector2(x - .55f, y + .06f), new Vector2(x + .48f, y - .06f), .04f, .93f);
            }
            // Middle: quicker rhythm with alternating diagonals.
            for (int i = 0; i < 21; i++)
            {
                float y = 17.35f - i * .80f;
                float x = Mathf.Sin(i * .62f) * .50f;
                float slope = i % 2 == 0 ? .16f : -.16f;
                Add(new Vector2(x - .42f, y - slope), new Vector2(x + .42f, y + slope), .035f, .76f);
            }
            // Right: sparse, smoother holds. Releasing both groups saves time but demands a catch.
            for (int i = 0; i < 14; i++)
            {
                float y = 17.15f - i * 1.22f;
                float x = 2.0f + Mathf.Sin(i * .88f) * .46f;
                Add(new Vector2(x - .33f, y - .15f), new Vector2(x + .33f, y + .15f), .03f, .60f);
            }
            // Cross-links offer a choice of route without turning the wall into broad platforms.
            for (int i = 0; i < 5; i++)
            {
                float y = 15.8f - i * 3.55f;
                Add(new Vector2(-1.85f, y), new Vector2(-.95f, y - .15f), .035f, .84f);
                Add(new Vector2(-.82f, y - .19f), new Vector2(.12f, y - .3f), .035f, .79f);
                Add(new Vector2(.55f, y - .38f), new Vector2(1.42f, y - .61f), .03f, .68f);
            }
            var random = new System.Random(47319);
            for (int i = 0; i < 10; i++)
            {
                float x = (float)random.NextDouble() * 6.4f - 3.2f;
                float y = (float)random.NextDouble() * 16.5f + .7f;
                float dx = .20f + (float)random.NextDouble() * .32f;
                float dy = ((float)random.NextDouble() - .5f) * .24f;
                Add(new Vector2(x - dx, y - dy), new Vector2(x + dx, y + dy), .025f, .64f);
            }
            // A final stone threshold supports all four hooves at the safe cave entrance.
            Add(new Vector2(-1.08f, .44f), new Vector2(1.08f, .44f), .045f, 1f);
        }

        private static Material MakeStone(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", 0f);
            return material;
        }

        private void Add(Vector2 a, Vector2 b, float thickness, float quality)
        {
            int index = veins.Count;
            veins.Add(new Vein { A = a, B = b, Thickness = thickness, Quality = quality });
            // The top is horizontal across the hoof depth; the seam rises only 2–5 cm.
            // Its back enters the cliff and its front reaches just beyond a hoof.
            float frontA = CliffSlope.LedgeFrontZ(a.y);
            float frontB = CliffSlope.LedgeFrontZ(b.y);
            float backA = CliffSlope.WallZ(a.y) + .005f;
            float backB = CliffSlope.WallZ(b.y) + .005f;
            var mesh = new Mesh
            {
                name = "Rock seam",
                vertices = new[]
                {
                    new Vector3(a.x, a.y, frontA),
                    new Vector3(b.x, b.y, frontB),
                    new Vector3(a.x, a.y - thickness, frontA - CliffSlope.Grade * thickness),
                    new Vector3(b.x, b.y - thickness, frontB - CliffSlope.Grade * thickness),
                    new Vector3(a.x, a.y, backA),
                    new Vector3(b.x, b.y, backB),
                    new Vector3(a.x, a.y - thickness, backA - CliffSlope.Grade * thickness),
                    new Vector3(b.x, b.y - thickness, backB - CliffSlope.Grade * thickness)
                },
                triangles = new[]
                {
                    0, 4, 1, 1, 4, 5, // upward-facing surface
                    0, 1, 2, 1, 3, 2, // outer edge
                    2, 3, 6, 3, 7, 6, // underside
                    4, 6, 5, 5, 6, 7, // cliff side
                    0, 2, 4, 2, 6, 4, 1, 5, 3, 3, 5, 7
                }
            };
            mesh.RecalculateNormals();
            var line = new GameObject($"Rock vein {index + 1:000}");
            line.transform.SetParent(transform, false);
            line.AddComponent<MeshFilter>().sharedMesh = mesh;
            line.AddComponent<MeshRenderer>().sharedMaterial = quality >= .8f ? strongStone : weakStone;
        }

        public bool FindNearest(Vector3 desired, float radius, out Support support, float maxY = float.PositiveInfinity)
        {
            support = default;
            float best = radius * radius;
            bool found = false;
            for (int i = 0; i < veins.Count; i++)
            {
                Vein vein = veins[i];
                Vector2 span = vein.B - vein.A;
                Vector2 inPlane = new Vector2(desired.x, desired.y);
                float t = Mathf.Clamp01(Vector2.Dot(inPlane - vein.A, span) / span.sqrMagnitude);
                Vector2 onLine = vein.A + span * t;
                if (onLine.y > desired.y + .22f || onLine.y > maxY) continue;
                float z = Mathf.Clamp(desired.z,
                    CliffSlope.LedgeFrontZ(onLine.y) + .02f, CliffSlope.WallZ(onLine.y) - .02f);
                Vector3 point = new Vector3(onLine.x, onLine.y, z);
                float distance = (desired - point).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                support = new Support { Point = point, Quality = vein.Quality,
                    Span = span.magnitude, VeinIndex = i };
                found = true;
            }
            return found;
        }
    }
}
