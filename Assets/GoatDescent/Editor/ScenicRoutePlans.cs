using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class ScenicRoutePlans
    {
        private const float Scale = 5f;

        public static Vector3[] SkyBridge()
        {
            var points = new List<Vector3>();
            for (int step = 0; step <= 3000; step++)
            {
                float z = Mathf.Lerp(-1100f,1100f,step / 3000f);
                float overlook = 170f * Mathf.Exp(-Mathf.Pow((z+720f)/200f,2f))
                    + 125f * Mathf.Exp(-Mathf.Pow((z-760f)/220f,2f));
                float x = SkyBridgeMesh.RiverCenter(z) + SkyBridgeMesh.RiverHalf(z) + 18f + overlook;
                float y = Mathf.Max(SkyBridgeMesh.WaterLevel+.6f,SkyBridgeMesh.TerrainHeight(x,z));
                points.Add(new Vector3(x,y,z)*Scale);
            }
            return points.ToArray();
        }

        public static Vector3[] FrozenRift()
        {
            var points = new List<Vector3>();
            for (int i = 0; i <= 2400; i++)
            {
                float t = i / 2400f;
                float z = Mathf.Lerp(-850,850,t);
                float fraction = Mathf.Lerp(1.04f,.05f,t);
                float distance = Mathf.Lerp(FrozenRiftTerrain.BedWidth(z),FrozenRiftTerrain.HalfWidth(z),fraction);
                float x = FrozenRiftTerrain.Channel(z) + distance;
                points.Add(new Vector3(x,FrozenRiftTerrain.Height(x,z),z)*Scale);
            }
            return points.ToArray();
        }

        public static Vector3[] EmeraldBasin()
        {
            var points = new List<Vector3>();
            float angle = 3.7f;
            for (int tier = 3; tier >= 1; tier--)
            {
                float a = angle;
                float outer = EmeraldBasinMesh.ShelfOuter(tier,a)-5f;
                float inner = EmeraldBasinMesh.WallRadius(tier,a,1f)-1f;
                for (int i = 0; i <= 120; i++)
                {
                    float radius = Mathf.Lerp(outer,inner,i/120f);
                    Add(points,a,radius,EmeraldBasinMesh.ShelfHeight(tier,a,radius));
                }
                for (int i = 1; i <= 1600; i++)
                {
                    float t = i / 1600f;
                    a = angle + t * 1.9f;
                    float v = 1f-t;
                    Add(points,a,EmeraldBasinMesh.WallRadius(tier,a,v)-1.6f,
                        EmeraldBasinMesh.WallHeight(tier,a,v)+.3f);
                }
                angle = a;
            }
            Vector3 last = points[points.Count-1]/Scale;
            Segment(points,new Vector2(last.x,last.z),Vector2.zero,p =>
            {
                float a = Mathf.Atan2(p.y,p.x), r = p.magnitude;
                return Mathf.Max(.9f,EmeraldBasinMesh.FloorHeight(a,r),EmeraldBasinMesh.IslandHeight(a,r));
            });
            return points.ToArray();
        }

        private static void Add(List<Vector3> points,float angle,float radius,float y)
            => points.Add(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius)*Scale);

        private static void Segment(List<Vector3> points,Vector2 from,Vector2 to,Func<Vector2,float> height)
        {
            int count = Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.8f));
            for (int i = 0; i <= count; i++)
            {
                Vector2 p = Vector2.Lerp(from,to,i/(float)count);
                points.Add(new Vector3(p.x,height(p),p.y)*Scale);
            }
        }
    }
}
