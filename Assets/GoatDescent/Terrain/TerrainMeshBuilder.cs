using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public static class TerrainMeshBuilder
    {
        public static Mesh Build(MountainSettings s)
        {
            MountainGenerator.PrepareRoute(s);
            int cols = s.Grid + 1;
            int rows = s.Grid + 1;
            float step = (s.Radius * 2f) / s.Grid;
            float zStart = -s.Radius;
            float floorY = s.ValleyY - 40f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();

            for (int rz = 0; rz < rows; rz++)
            {
                float z = zStart + rz * step;
                for (int rx = 0; rx < cols; rx++)
                {
                    float x = -s.Radius + rx * step;
                    float y = TerrainHeightField.HeightAt(s, x, z);
                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)rx / s.Grid, Mathf.InverseLerp(s.ValleyY, s.SummitY, y)));
                    float mask = RouteQueries.PlatformMaskAt(s, x, z);
                    float edge = Mathf.Clamp01((1f - mask) * 3f);
                    RouteQueries.FindNearestPlatform(x, z, out _, out LandingPlatform platform);
                    float type = (int)platform.Type / 5f;
                    colors.Add(new Color(mask, edge, type, platform.Progress));
                }
            }
            for (int rz = 0; rz < s.Grid; rz++)
                for (int rx = 0; rx < s.Grid; rx++)
                {
                    int a = rz * cols + rx;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }

            AddSideWall(verts, uvs, colors, tris, 0, step, rows, cols, floorY, zStart);
            AddSideWall(verts, uvs, colors, tris, cols - 1, step, rows, cols, floorY, zStart);
            AddEndWall(verts, uvs, colors, tris, 0, step, cols, floorY, zStart);
            AddEndWall(verts, uvs, colors, tris, rows - 1, step, cols, floorY, zStart + (rows - 1) * step);

            while (uvs.Count < verts.Count) uvs.Add(Vector2.zero);
            while (colors.Count < verts.Count) colors.Add(new Color(0f, 0f, 0f, 1f));

            var mesh = new Mesh { name = "Procedural Mesa" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSideWall(List<Vector3> verts, List<Vector2> uvs, List<Color> colors, List<int> tris, int col, float step, int rows, int cols, float floorY, float zStart)
        {
            for (int rz = 0; rz < rows - 1; rz++)
            {
                int i0 = rz * cols + col;
                int i1 = (rz + 1) * cols + col;
                int baseIdx = verts.Count;
                verts.Add(new Vector3(verts[i0].x, verts[i0].y, verts[i0].z));
                verts.Add(new Vector3(verts[i0].x, floorY, verts[i0].z));
                verts.Add(new Vector3(verts[i1].x, verts[i1].y, verts[i1].z));
                verts.Add(new Vector3(verts[i1].x, floorY, verts[i1].z));
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                colors.Add(Color.black); colors.Add(Color.black);
                colors.Add(Color.black); colors.Add(Color.black);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }

        private static void AddEndWall(List<Vector3> verts, List<Vector2> uvs, List<Color> colors, List<int> tris, int row, float step, int cols, float floorY, float z)
        {
            for (int rx = 0; rx < cols - 1; rx++)
            {
                int i0 = row * cols + rx;
                int i1 = i0 + 1;
                int baseIdx = verts.Count;
                verts.Add(new Vector3(verts[i0].x, verts[i0].y, z));
                verts.Add(new Vector3(verts[i0].x, floorY, z));
                verts.Add(new Vector3(verts[i1].x, verts[i1].y, z));
                verts.Add(new Vector3(verts[i1].x, floorY, z));
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.one);
                colors.Add(Color.black); colors.Add(Color.black);
                colors.Add(Color.black); colors.Add(Color.black);
                tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }
        }
    }
}