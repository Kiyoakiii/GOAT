using System;
using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class CliffStudyWedgeMesh
    {
        private sealed class Face
        {
            internal List<Vector3> Points;
            internal bool IsTop;
            internal Face(List<Vector3> points,bool isTop=false){Points=points;IsTop=isTop;}
        }

        private static void Clip(List<Face> faces,Vector3 normal,float distance,bool top=false)
        {
            normal.Normalize();
            var cap=new List<Vector3>();
            for(int i=faces.Count-1;i>=0;i--)
            {
                var polygon=faces[i].Points;var output=new List<Vector3>();
                for(int k=0;k<polygon.Count;k++)
                {
                    var a=polygon[k];var b=polygon[(k+1)%polygon.Count];
                    float da=Vector3.Dot(normal,a)-distance,db=Vector3.Dot(normal,b)-distance;
                    if(da<=.00001f)output.Add(a);
                    if((da<0)!=(db<0))
                    {
                        var intersection=Vector3.Lerp(a,b,da/(da-db));output.Add(intersection);
                        bool exists=false;foreach(var p in cap)if((p-intersection).sqrMagnitude<.000001f){exists=true;break;}
                        if(!exists)cap.Add(intersection);
                    }
                }
                if(output.Count<3)faces.RemoveAt(i);else faces[i].Points=output;
            }
            if(cap.Count<3)return;
            var center=Vector3.zero;foreach(var point in cap)center+=point;center/=cap.Count;
            var u=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized;
            var v=Vector3.Cross(normal,u);
            cap.Sort((a,b)=>Mathf.Atan2(Vector3.Dot(a-center,v),Vector3.Dot(a-center,u)).CompareTo(Mathf.Atan2(Vector3.Dot(b-center,v),Vector3.Dot(b-center,u))));
            faces.Add(new Face(cap,top));
        }

        private static List<Face> Cube()
        {
            const float e=250;
            var points=new[]{new Vector3(-e,-e,-e),new Vector3(e,-e,-e),new Vector3(e,-e,e),new Vector3(-e,-e,e),new Vector3(-e,e,-e),new Vector3(e,e,-e),new Vector3(e,e,e),new Vector3(-e,e,e)};
            var indices=new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7}};
            var faces=new List<Face>();
            foreach(var face in indices){var polygon=new List<Vector3>();foreach(int index in face)polygon.Add(points[index]);faces.Add(new Face(polygon));}
            return faces;
        }

        private static List<Vector2> Cut(List<Vector2> polygon,Vector2 normal,float distance)
        {
            var result=new List<Vector2>();
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Count];
                float da=Vector2.Dot(a,normal)-distance,db=Vector2.Dot(b,normal)-distance;
                if(da<=0)result.Add(a);
                if((da<0)!=(db<0))result.Add(Vector2.Lerp(a,b,da/(da-db)));
            }
            return result;
        }

        private static void Emit(CrownRockMesh mesh,Vector3 a,Vector3 b,Vector3 c,Color tint,List<Vector3> surface)
        {
            mesh.Triangle(a,b,c,tint,tint,tint);
            var normal=Vector3.Cross(b-a,c-a).normalized;
            if(surface!=null&&normal.y>.45f){surface.Add(a);surface.Add(b);surface.Add(c);}
        }

        private static void Facet(CrownRockMesh mesh,Face face,Vector3 offset,Color tint,System.Random random,List<Vector3> surface)
        {
            float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            var origin=face.Points[0];var normal=Vector3.zero;
            for(int i=1;i<face.Points.Count-1&&normal.sqrMagnitude<.0001f;i++)normal=Vector3.Cross(face.Points[i]-origin,face.Points[i+1]-origin);
            if(normal.sqrMagnitude<.0001f)return;normal.Normalize();
            var u=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized;var v=Vector3.Cross(normal,u);
            var polygon=new List<Vector2>();var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
            foreach(var p in face.Points)
            {
                var q=new Vector2(Vector3.Dot(p-origin,u),Vector3.Dot(p-origin,v));polygon.Add(q);min=Vector2.Min(min,q);max=Vector2.Max(max,q);
            }
            float area=0;for(int i=0;i<polygon.Count;i++)area+=polygon[i].x*polygon[(i+1)%polygon.Count].y-polygon[i].y*polygon[(i+1)%polygon.Count].x;
            if(area<0){polygon.Reverse();area=-area;}
            Vector3 World(Vector2 p)=>offset+origin+u*p.x+v*p.y;
            if(area<2)
            {
                for(int i=1;i<polygon.Count-1;i++)Emit(mesh,World(polygon[0]),World(polygon[i]),World(polygon[i+1]),tint,surface);
                return;
            }
            int cols=Mathf.Max(1,Mathf.CeilToInt((max.x-min.x)/2.2f)),rows=Mathf.Max(1,Mathf.CeilToInt((max.y-min.y)/2.6f));
            var sites=new List<Vector2>();
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)sites.Add(new Vector2(Mathf.Lerp(min.x,max.x,(x+Range(.05f,.95f))/cols),Mathf.Lerp(min.y,max.y,(y+Range(.05f,.95f))/rows)));
            for(int cell=0;cell<sites.Count;cell++)
            {
                var section=new List<Vector2>(polygon);var site=sites[cell];
                for(int other=0;other<sites.Count&&section.Count>0;other++)
                {
                    if(other==cell)continue;var delta=sites[other]-site;if(delta.sqrMagnitude>100)continue;
                    section=Cut(section,delta,(sites[other].sqrMagnitude-site.sqrMagnitude)*.5f);
                }
                if(section.Count<3)continue;
                var center=Vector2.zero;foreach(var p in section)center+=p;center/=section.Count;
                var raised=World(center)+normal*Range(.025f,.13f);var color=tint*Range(.94f,1.02f);
                for(int k=0;k<section.Count;k++)
                {
                    var a=World(section[k]);var b=World(section[(k+1)%section.Count]);
                    Emit(mesh,a,b,raised,color,surface);
                }
            }
        }

        private static void Volume(CrownRockMesh mesh,Vector3 center,Vector2[] outline,Vector2 tilt,Vector3 lean,float depth,float topOffset,int seed,bool rugged,List<Vector3> surface)
        {
            var random=new System.Random(seed);
            float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            var faces=Cube();
            var top=new Vector3(-tilt.x,1,-tilt.y);Clip(faces,top,topOffset/top.magnitude,true);
            if(rugged)
            {
                var first=new Vector3(-tilt.x+.14f,1,-tilt.y-.09f);Clip(faces,first,(topOffset+1.1f)/first.magnitude,true);
                var second=new Vector3(-tilt.x-.1f,1,-tilt.y+.12f);Clip(faces,second,(topOffset+1.3f)/second.magnitude,true);
            }
            var bottom=new Vector3(Range(-.12f,.12f),-1,Range(-.12f,.12f));Clip(faces,bottom,depth/bottom.magnitude);
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i];var edge=outline[(i+1)%outline.Length]-a;
                var outward=new Vector2(edge.y,-edge.x).normalized;
                var normal=new Vector3(outward.x,Vector2.Dot(outward,new Vector2(lean.x,lean.z))/depth+Range(-.09f,.09f),outward.y);
                float distance=Vector2.Dot(outward,a);
                Clip(faces,normal,distance/normal.magnitude);
            }
            var corners=new List<Vector3>();
            foreach(var face in faces)foreach(var point in face.Points)
            {
                bool exists=false;foreach(var other in corners)if((other-point).sqrMagnitude<.00001f){exists=true;break;}
                if(!exists)corners.Add(point);
            }
            foreach(var point in corners)
            {
                var normal=new Vector3(point.x,point.y+depth*.45f,point.z).normalized;
                if(normal.sqrMagnitude<.1f)continue;
                Clip(faces,normal,Vector3.Dot(normal,point)-Range(.04f,.16f));
            }
            if(faces.Count<4)throw new InvalidOperationException("Empty sculpted cliff wedge");
            foreach(var face in faces)
            {
                var tint=Color.Lerp(new Color(.80f,.83f,.84f),new Color(1,.96f,.84f),Range(.25f,.85f));
                Facet(mesh,face,center,tint,random,face.IsTop?surface:null);
            }
        }

        internal static void Mass(CrownRockMesh mesh,Vector3 center,Vector2[] outline,Vector2 tilt,Vector3 foot,float depth,bool rugged,List<Vector3> surface)
        {
            int seed=Mathf.RoundToInt(center.x*131+center.z*317+center.y*17);
            float down=0;int i=0;
            while(down<depth)
            {
                float step=depth<12?depth:9.5f+2*Mathf.Sin(i*1.7f+seed);
                float t=down/depth;
                float scale=i==0?1.035f:1.02f+.55f*t*t;
                var shape=new Vector2[outline.Length];for(int k=0;k<shape.Length;k++)shape[k]=outline[k]*scale;
                var offset=foot*t-Vector3.up*down;
                if(i>0)offset+=new Vector3(Mathf.Sin(i*2.1f+seed)*.45f,0,Mathf.Cos(i*1.7f+seed)*.35f);
                float thickness=Mathf.Min(step+8,depth-down);
                var slope=i==0?tilt:tilt+new Vector2(.10f*Mathf.Sin(i*1.7f+seed),.13f*Mathf.Cos(i*2.3f+seed));
                Volume(mesh,center+offset,shape,slope,foot*(thickness/depth),thickness,i==0?0f:.4f,seed+i*7919,i==0&&rugged,i==0?surface:null);
                down+=step;i++;
            }
        }
    }
}
