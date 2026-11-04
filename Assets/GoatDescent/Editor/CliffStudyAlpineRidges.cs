using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class CliffStudyAlpineRidges
    {
        private static readonly float[] Peaks={85,190,115,285,155,230,105,330,175,250,120,205,70};

        private static float Crest(float x,int layer)
        {
            float sample=(x+1500+layer*87)/250;
            int index=Mathf.Clamp(Mathf.FloorToInt(sample),0,Peaks.Length-2);
            float t=Mathf.Clamp01(sample-index);
            float height=Mathf.Lerp(Peaks[index],Peaks[index+1],t);
            float teeth=(1-Mathf.Abs(Mathf.Sin(x*.023f+layer)))*24;
            return (height+teeth+layer*35)*.78f;
        }

        private static float Height(float x,float z)
        {
            float height=-42;
            for(int layer=0;layer<3;layer++)
            {
                float center=810+layer*230+75*Mathf.Sin(x*.0047f+layer*1.9f);
                float width=180+layer*65;
                float cross=Mathf.Abs(Mathf.Abs(z)-center)/width;
                float profile=Mathf.Pow(Mathf.Clamp01(1-cross),1.2f);
                float spur=1+.16f*Mathf.Sin(x*.032f+z*.019f+layer*2);
                height=Mathf.Max(height,-42+profile*Crest(x+(Mathf.Abs(z)-center)*.28f,layer)*spur);
            }
            return height;
        }

        private static float SnowLine(Vector3 p)
            =>p.y-118-18*Mathf.Sin(p.x*.017f)-12*Mathf.Sin(p.z*.031f+p.x*.009f);

        private static void Face(CrownRockMesh target,Vector3 a,Vector3 b,Vector3 c,bool snow)
        {
            var source=new[]{a,b,c};var polygon=new List<Vector3>();
            for(int i=0;i<3;i++)
            {
                var p=source[i];var q=source[(i+1)%3];
                float fp=SnowLine(p)*(snow?1:-1),fq=SnowLine(q)*(snow?1:-1);
                if(fp>=0)polygon.Add(p);
                if((fp>=0)!=(fq>=0))polygon.Add(Vector3.Lerp(p,q,fp/(fp-fq)));
            }
            for(int i=1;i<polygon.Count-1;i++)
            {
                var normal=Vector3.Cross(polygon[i]-polygon[0],polygon[i+1]-polygon[0]).normalized;
                var tint=snow?Color.Lerp(new Color(.74f,.86f,.97f),Color.white,Mathf.Clamp01(normal.y)):Color.Lerp(new Color(.68f,.77f,.87f),Color.white,Mathf.PerlinNoise(a.x*.016f,a.z*.016f));
                target.Triangle(polygon[0],polygon[i],polygon[i+1],tint,tint,tint);
            }
        }

        internal static void Build(CrownRockMesh rock,CrownRockMesh snow)
        {
            const int step=14;
            for(int z=-1680;z<1680;z+=step)for(int x=-1512;x<1512;x+=step)
            {
                if(Mathf.Abs(z)<560)continue;
                var a=new Vector3(x,Height(x,z),z);var b=new Vector3(x,Height(x,z+step),z+step);
                var c=new Vector3(x+step,Height(x+step,z+step),z+step);var d=new Vector3(x+step,Height(x+step,z),z);
                Face(rock,a,b,c,false);Face(rock,a,c,d,false);
                Face(snow,a,b,c,true);Face(snow,a,c,d,true);
            }
        }
    }
}
