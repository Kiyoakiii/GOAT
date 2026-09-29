using UnityEngine;

namespace GoatDescent
{
    internal static class GoatShapeMesh
    {
        public static Mesh ForPart(string name)
        {
            if (name == "Rounded body")
                return Sculpt(new[] { -.5f, -.36f, -.10f, .18f, .42f, .5f },
                    new[] { .15f, .40f, .50f, .47f, .30f, .08f },
                    new[] { .24f, .43f, .48f, .46f, .36f, .14f });
            if (name == "Head")
                return Sculpt(new[] { -.5f, -.32f, -.05f, .26f, .5f },
                    new[] { .17f, .42f, .48f, .37f, .17f },
                    new[] { .20f, .40f, .44f, .36f, .18f });
            if (name == "Left ear" || name == "Right ear") return Ear();
            if (name == "Horn base" || name == "Horn tip") return Horn();
            return null;
        }

        private static Mesh Sculpt(float[] z, float[] width, float[] height)
        {
            const int around = 10;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int ring = 0; ring < z.Length - 1; ring++)
            {
                for (int side = 0; side < around; side++)
                {
                    float a = side * Mathf.PI * 2f / around;
                    float b = (side + 1) * Mathf.PI * 2f / around;
                    int start = vertices.Count;
                    vertices.Add(new Vector3(Mathf.Cos(a) * width[ring], Mathf.Sin(a) * height[ring], z[ring]));
                    vertices.Add(new Vector3(Mathf.Cos(b) * width[ring], Mathf.Sin(b) * height[ring], z[ring]));
                    vertices.Add(new Vector3(Mathf.Cos(a) * width[ring + 1], Mathf.Sin(a) * height[ring + 1], z[ring + 1]));
                    vertices.Add(new Vector3(Mathf.Cos(b) * width[ring + 1], Mathf.Sin(b) * height[ring + 1], z[ring + 1]));
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start + 1); triangles.Add(start + 3); triangles.Add(start + 2);
                }
            }
            int backCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, z[0]));
            int frontCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, z[z.Length - 1]));
            for (int side = 0; side < around; side++)
            {
                float a = side * Mathf.PI * 2f / around;
                float b = (side + 1) * Mathf.PI * 2f / around;
                int start = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a) * width[0], Mathf.Sin(a) * height[0], z[0]));
                vertices.Add(new Vector3(Mathf.Cos(b) * width[0], Mathf.Sin(b) * height[0], z[0]));
                vertices.Add(new Vector3(Mathf.Cos(a) * width[width.Length - 1], Mathf.Sin(a) * height[height.Length - 1], z[z.Length - 1]));
                vertices.Add(new Vector3(Mathf.Cos(b) * width[width.Length - 1], Mathf.Sin(b) * height[height.Length - 1], z[z.Length - 1]));
                triangles.Add(backCenter); triangles.Add(start + 1); triangles.Add(start);
                triangles.Add(frontCenter); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            var mesh = new Mesh { name = "Sculpted goat shape" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Ear()
        {
            var mesh = new Mesh { name = "Pointed goat ear" };
            mesh.vertices = new[]
            {
                new Vector3(-.5f, 0f, 0f), new Vector3(0f, .43f, -.15f),
                new Vector3(.5f, 0f, 0f), new Vector3(0f, -.43f, -.15f),
                new Vector3(0f, 0f, .28f)
            };
            mesh.triangles = new[] { 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4, 1, 0, 3, 1, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh Horn()
        {
            const int sides = 8;
            var vertices = new Vector3[sides + 2];
            var triangles = new int[sides * 6];
            vertices[0] = new Vector3(0f, -.5f, 0f);
            vertices[1] = new Vector3(.3f, .5f, -.15f);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices[i + 2] = new Vector3(Mathf.Cos(angle) * .5f, -.5f, Mathf.Sin(angle) * .5f);
                int next = (i + 1) % sides;
                triangles[i * 6] = 1;
                triangles[i * 6 + 1] = next + 2;
                triangles[i * 6 + 2] = i + 2;
                triangles[i * 6 + 3] = 0;
                triangles[i * 6 + 4] = i + 2;
                triangles[i * 6 + 5] = next + 2;
            }
            var mesh = new Mesh { name = "Curved cartoon horn", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
