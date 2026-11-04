using System;
using System.Collections.Generic;
using System.IO;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoatDescent.EditorTools
{
    public static class TerraceCliffStudyAuthoring
    {
        private const string Folder="Assets/GoatDescent/Level/Static/TerraceCliffStudy";
        private static System.Random random;
        private static float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        private sealed class Rock
        {
            public readonly Vector3 Center;
            public readonly Vector2[] Outline;
            private Vector2[] surfaceOutline;
            public Vector2[] Rim=>surfaceOutline??Outline;
            public readonly Vector2 Tilt;
            public Vector3 Foot;
            public readonly List<Vector3> CapTriangles=new List<Vector3>();
            public readonly float Depth;
            public Rock(Vector3 center,Vector2[] outline,Vector2 tilt,Vector3 foot,float depth)
            {Center=center;Outline=outline;Tilt=tilt;Foot=foot;Depth=depth;}
            public void UpdateOutline()
            {
                var points=new List<Vector2>();
                foreach(var vertex in CapTriangles)points.Add(new Vector2(vertex.x-Center.x,vertex.z-Center.z));
                points.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
                var unique=new List<Vector2>();foreach(var point in points)if(unique.Count==0||(unique[unique.Count-1]-point).sqrMagnitude>.000001f)unique.Add(point);
                if(unique.Count<3)throw new InvalidOperationException("Missing cliff top surface");
                float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
                var hull=new List<Vector2>();
                foreach(var point in unique)
                {
                    while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=0)hull.RemoveAt(hull.Count-1);
                    hull.Add(point);
                }
                int lower=hull.Count;
                for(int i=unique.Count-2;i>=0;i--)
                {
                    var point=unique[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=0)hull.RemoveAt(hull.Count-1);
                    hull.Add(point);
                }
                hull.RemoveAt(hull.Count-1);surfaceOutline=hull.ToArray();
            }
            public Vector3 RawSurface(Vector2 local)
            {
                float relief=.065f*Mathf.Sin(local.x*1.7f+local.y*.6f)*Mathf.SmoothStep(0,1,local.magnitude/1.5f)
                    +.09f*Mathf.Sin(local.x*.7f)*Mathf.Sin(local.y*.8f);
                return Center+new Vector3(local.x,Vector2.Dot(local,Tilt)+relief,local.y);
            }
            public Vector3 Surface(Vector2 local)
            {
                var point=new Vector2(Center.x+local.x,Center.z+local.y);
                for(int i=0;i<CapTriangles.Count;i+=3)
                {
                    var a=CapTriangles[i];var b=CapTriangles[i+1];var c=CapTriangles[i+2];
                    var ab=new Vector2(b.x-a.x,b.z-a.z);var ac=new Vector2(c.x-a.x,c.z-a.z);var ap=point-new Vector2(a.x,a.z);
                    float divisor=ab.x*ac.y-ab.y*ac.x;
                    if(Mathf.Abs(divisor)<.00001f)continue;
                    float v=(ap.x*ac.y-ap.y*ac.x)/divisor,w=(ab.x*ap.y-ab.y*ap.x)/divisor;
                    if(v>=-.0001f&&w>=-.0001f&&v+w<=1.0001f)return new Vector3(point.x,a.y*(1-v-w)+b.y*v+c.y*w,point.y);
                }
                return RawSurface(local);
            }
            public bool Contains(Vector2 point)
            {
                for(int i=0;i<Rim.Length;i++)
                {
                    var edge=Rim[(i+1)%Rim.Length]-Rim[i];var delta=point-Rim[i];
                    if(edge.x*delta.y-edge.y*delta.x<-.001f)return false;
                }
                return true;
            }
            public Vector3 Edge(Vector3 direction)
            {
                var d=new Vector2(direction.x,direction.z).normalized;
                float nearest=float.MaxValue;
                for(int i=0;i<Rim.Length;i++)
                {
                    var a=Rim[i];var e=Rim[(i+1)%Rim.Length]-a;
                    float divisor=d.x*e.y-d.y*e.x;
                    if(Mathf.Abs(divisor)<.0001f)continue;
                    float t=(a.x*e.y-a.y*e.x)/divisor,u=(a.x*d.y-a.y*d.x)/divisor;
                    if(t>0 && u>=0 && u<=1)nearest=Mathf.Min(nearest,t);
                }
                return Surface(d*(nearest-.35f));
            }
        }
        private static Vector2[] Shape(params float[] xy)
        {var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
        private static Rock[] Course()=>new[]
        {
            new Rock(new Vector3(-14,38,-6),Shape(-6,-4,-2,-5,4,-4,6,-1,5,3,1,5,-4,4,-6,1),Vector2.zero,new Vector3(-5,0,-12),70),
            new Rock(new Vector3(-7,35,4),Shape(-4,-3,0,-4,4,-2,4,1,2,3,-2,3.5f,-4,1),Vector2.zero,new Vector3(-4,0,-9),67),
            new Rock(new Vector3(1,32,10),Shape(-3,-2.7f,1,-3.5f,3,-1,2.6f,2,-1,3,-3,1),new Vector2(-.09f,-.08f),new Vector3(-4,0,-8),64),
            new Rock(new Vector3(10,28,13),Shape(-2.7f,-2,0,-2.8f,2.6f,-1,2.4f,2,-1,2.7f,-2.8f,.5f),Vector2.zero,new Vector3(2,0,-10),60),
            new Rock(new Vector3(13,25,21),Shape(-4.5f,-3,-1,-4.5f,4,-3,5,1,3,4,-2,4,-4.5f,1),Vector2.zero,new Vector3(3,0,-10),57),
            new Rock(new Vector3(6,22,29),Shape(-2.5f,-3,1,-3.3f,3,-1,2.5f,2.5f,-1,3,-3,.5f),Vector2.zero,new Vector3(1,0,-10),54),
            new Rock(new Vector3(-2,19,33),Shape(-4,-2.8f,0,-3.3f,3.4f,-1.5f,3.6f,2,-1,3.2f,-4,1),new Vector2(.06f,-.06f),new Vector3(-3,0,-10),51),
            new Rock(new Vector3(-8,16,42),Shape(-6,-4,-1,-5,5,-3,6,1,3,5,-2,5.5f,-6,2),Vector2.zero,new Vector3(-5,0,-12),48)
        };
        private static void Tri(CrownRockMesh mesh,Vector3 a,Vector3 b,Vector3 c,Color color)=>mesh.Triangle(a,b,c,color,color,color);
        private static void Quad(CrownRockMesh mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        {Tri(mesh,a,b,c,color);Tri(mesh,a,c,d,color);}
        private static List<Vector2> CutFace(List<Vector2> polygon,Vector2 normal,float offset)
        {
            var output=new List<Vector2>();
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Count];
                float da=Vector2.Dot(a,normal)-offset,db=Vector2.Dot(b,normal)-offset;
                if(da<=0)output.Add(a);
                if((da<0)!=(db<0))output.Add(Vector2.Lerp(a,b,da/(da-db)));
            }
            return output;
        }
        private static void Cliff(CrownRockMesh mesh,Rock rock,bool cap=true,bool rugged=false)
        {
            rock.CapTriangles.Clear();
            CliffStudyWedgeMesh.Mass(mesh,rock.Center,rock.Outline,rock.Tilt,rock.Foot,rock.Depth,rugged,cap?rock.CapTriangles:null);
            if(cap)rock.UpdateOutline();
        }
        private static void Cluster(CrownRockMesh mesh,Vector3 p,Vector3 scale,Color color,float angle=0)
        {
            var rotate=Quaternion.Euler(0,angle,0);
            const int rings=3,segments=7;
            var vertices=new Vector3[rings+1,segments+1];
            for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++)
            {
                float latitude=Mathf.PI*y/rings,longitude=2*Mathf.PI*x/segments;
                float ripple=1+.12f*Mathf.Sin(longitude*3+latitude*5)+.07f*Mathf.Cos(longitude*5-latitude*3);
                var v=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude));
                vertices[y,x]=p+rotate*Vector3.Scale(v*ripple,scale);
            }
            for(int y=0;y<rings;y++)for(int x=0;x<segments;x++)
                Quad(mesh,vertices[y,x],vertices[y,x+1],vertices[y+1,x+1],vertices[y+1,x],color*Mathf.Lerp(.79f,1.04f,1-y/(float)rings));
        }
        private static void Shrub(CrownRockMesh mesh,Vector3 p,Vector3 scale,Color color)
        {
            for(int i=0;i<9;i++)
            {
                float angle=i*2.39996f,radius=Mathf.Sqrt((i+.5f)/9);
                var offset=new Vector3(Mathf.Cos(angle)*scale.x*radius,scale.y*(1-radius)*.7f,Mathf.Sin(angle)*scale.z*radius);
                Cluster(mesh,p+offset,new Vector3(scale.x*.34f,scale.y*.48f,scale.z*.34f),color*R(.8f,1.1f),R(0,360));
            }
        }
        private static void Pine(CrownRockMesh needles,CrownRockMesh bark,Vector3 p,float height,float lean)
        {
            var tip=p+new Vector3(lean,height,0);
            for(int k=0;k<7;k++)
            {
                float a=k*Mathf.PI*2/7,b=(k+1)*Mathf.PI*2/7;
                Quad(bark,p+new Vector3(Mathf.Cos(a)*height*.025f,0,Mathf.Sin(a)*height*.025f),tip,tip,p+new Vector3(Mathf.Cos(b)*height*.025f,0,Mathf.Sin(b)*height*.025f),Color.white);
            }
            int tiers=p.z>70?5:10;
            for(int tier=0;tier<tiers;tier++)
            {
                float t=.2f+tier*.72f/tiers;
                var c=Vector3.Lerp(p,tip,t);
                float radius=height*.27f*Mathf.Pow(1-t,.88f)*(1+.13f*Mathf.Sin(tier*1.7f+p.x*.4f+p.z*.2f));
                int count=p.z>70?9:18;
                for(int k=0;k<count;k++)
                {
                    float a=k*Mathf.PI*2/count+tier*1.71f,b=(k+1)*Mathf.PI*2/count+tier*1.71f;
                    float ra=radius*(.87f+.13f*Mathf.Sin(k*2.7f+tier));
                    float rb=radius*(.87f+.13f*Mathf.Sin((k+1)*2.7f+tier));
                    var va=c+new Vector3(Mathf.Cos(a)*ra,-height*.035f,Mathf.Sin(a)*ra);
                    var vb=c+new Vector3(Mathf.Cos(b)*rb,-height*.035f,Mathf.Sin(b)*rb);
                    var notch=c+new Vector3(Mathf.Cos((a+b)*.5f)*radius*.72f,height*.025f,Mathf.Sin((a+b)*.5f)*radius*.72f);
                    var crown=c+Vector3.up*height*.18f;
                    var tone=Color.Lerp(new Color(.47f,.62f,.25f),new Color(.86f,.91f,.44f),R(.15f,.7f));
                    Tri(needles,crown,vb,notch,tone);Tri(needles,crown,notch,va,tone*.95f);
                    Tri(needles,c-Vector3.up*height*.05f,va,notch,tone*.67f);
                    Tri(needles,c-Vector3.up*height*.05f,notch,vb,tone*.67f);
                }
            }
        }
        private static float MeadowField(Vector2 p,Vector2 size,Vector2 offset)
        {
            float radius=new Vector2(p.x/size.x,p.y/size.y).magnitude;
            return 1-radius+(Mathf.PerlinNoise((p.x+offset.x)*1.1f,(p.y+offset.y)*1.1f)-.5f)*.6f;
        }
        private static void Patch(CrownRockMesh sod,CrownRockMesh grass,CrownRockMesh flowers,Rock rock,Vector2 center,Vector2 size)
        {
            float step=.3f;
            for(float z=-size.y*1.25f;z<size.y*1.25f;z+=step)for(float x=-size.x*1.25f;x<size.x*1.25f;x+=step)
            {
                var corners=new[]{new Vector2(x,z),new Vector2(x,z+step),new Vector2(x+step,z+step),new Vector2(x+step,z)};
                for(int tri=0;tri<2;tri++)
                {
                    var source=tri==0?new[]{corners[0],corners[1],corners[2]}:new[]{corners[0],corners[2],corners[3]};
                    var polygon=new List<Vector2>();
                    for(int i=0;i<3;i++)
                    {
                        var a=source[i];var b=source[(i+1)%3];
                        float fa=MeadowField(a,size,center),fb=MeadowField(b,size,center);
                        if(fa>0)polygon.Add(a);
                        if((fa>0)!=(fb>0))polygon.Add(Vector2.Lerp(a,b,fa/(fa-fb)));
                    }
                    for(int side=0;side<rock.Rim.Length&&polygon.Count>0;side++)
                    {
                        var edge=rock.Rim[(side+1)%rock.Rim.Length]-rock.Rim[side];
                        var normal=new Vector2(edge.y,-edge.x);
                        polygon=CutFace(polygon,normal,Vector2.Dot(rock.Rim[side]-center,normal));
                    }
                    for(int i=1;i<polygon.Count-1;i++)
                    {
                        Color tint=Color.Lerp(new Color(.88f,.81f,.53f),new Color(.85f,1,.72f),Mathf.PerlinNoise(x*1.3f+10,z*1.3f+20));
                        Color CoverTint(Vector2 local)=>Color.Lerp(new Color(.88f,.81f,.53f),new Color(.85f,1,.72f),Mathf.PerlinNoise(local.x*.7f+10,local.y*.7f+20));
                        sod.Triangle(rock.Surface(center+polygon[0])+Vector3.up*.035f,rock.Surface(center+polygon[i])+Vector3.up*.035f,rock.Surface(center+polygon[i+1])+Vector3.up*.035f,CoverTint(polygon[0]),CoverTint(polygon[i]),CoverTint(polygon[i+1]));
                    }
                }
            }
            int density=Mathf.RoundToInt(size.x*size.y*90);
            for(int i=0;i<density;i++)
            {
                var local=new Vector2(R(-size.x*1.2f,size.x*1.2f),R(-size.y*1.2f,size.y*1.2f));
                if(!rock.Contains(center+local))continue;
                float field=MeadowField(local,size,center);
                if(field<R(.01f,.5f))continue;
                var foot=rock.Surface(center+local)+Vector3.up*.04f;
                for(int blade=0;blade<4;blade++)
                {
                    var right=Quaternion.Euler(0,R(0,360),0)*Vector3.right;
                    float h=R(.08f,.25f);var mid=foot+Vector3.up*h*.6f;var tip=foot+Vector3.up*h+right*.08f;
                    Color tint=Color.Lerp(new Color(.76f,.81f,.34f),new Color(.98f,.94f,.5f),R(0,1));
                    Quad(grass,foot-right*.011f,mid-right*.007f,mid+right*.007f,foot+right*.011f,tint);
                    Tri(grass,mid-right*.007f,tip,mid+right*.007f,tint);
                }
                if(i%37==0)Cluster(flowers,foot+Vector3.up*.15f,new Vector3(.045f,.025f,.045f),new Color(1,.87f,.46f));
            }
        }
        private static Material Material(string name,Color color,bool detail=false)
        {
            var m=new Material(Shader.Find("GoatDescent/CliffStudySurface")){name=name};m.color=color;
            m.SetFloat("_GroundCover",name=="Meadow"||name=="Valley vegetation"?1:0);
            m.SetFloat("_Wind",name=="Pine foliage"?.075f:name=="Grass tips"?.035f:0);m.SetFloat("_DetailScale",.12f);m.SetFloat("_DetailStrength",detail?.92f:0);m.SetFloat("_Weathering",detail?1f:0);
            if(name=="Meadow"||name=="Valley vegetation")m.SetTexture("_CoverTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/grass_color.jpg"));
            if(detail)m.SetTexture("_DetailTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoatDescent/Textures/CliffOchrePainted.png"));
            return ScenicWorldAuthoring.Save(m,Folder+"/"+name+".mat");
        }
        private static void Add(Transform root,string name,CrownRockMesh mesh,Material mat,bool collision=false,bool smooth=false)
        {
            var built=mesh.Build(name);
            if(smooth)
            {
                var positions=built.vertices;var normals=built.normals;
                var sums=new Dictionary<Vector3,Vector3>();
                for(int i=0;i<positions.Length;i++)
                {
                    sums.TryGetValue(positions[i],out var sum);sums[positions[i]]=sum+normals[i];
                }
                for(int i=0;i<positions.Length;i++)normals[i]=sums[positions[i]].normalized;
                built.normals=normals;
            }
            ScenicTrailAuthoring.Add(root,name,built,mat,Folder+"/"+name+".asset",collision);
        }
        private static float PlanarSegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            var edge=Vector3.ProjectOnPlane(b-a,Vector3.up);var offset=Vector3.ProjectOnPlane(p-a,Vector3.up);
            float t=Mathf.Clamp01(Vector3.Dot(offset,edge)/Mathf.Max(.001f,edge.sqrMagnitude));
            return (offset-edge*t).magnitude;
        }
        private static float RidgeWidth(int i)=>170+50*Mathf.Sin(i*2.3f)*Mathf.Sin(i*2.3f);
        private static float Valley(float x,float z)
        {
            float river=90+25*Mathf.Sin(z*.012f);
            float h=-34+17*Mathf.Sin(x*.012f)*Mathf.Cos(z*.014f)+5*(Mathf.PerlinNoise(x*.03f+30,z*.03f+20)-.5f);
            return Mathf.Lerp(-38,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,25,Mathf.Abs(x-river))));
        }
        [MenuItem("Tools/GOAT/Образец скалы — 7 переходов")]
        public static void Build()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            random=new System.Random(7351);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Authored cliff study").transform;
            var stone=Material("Warm limestone",new Color(.92f,.91f,.88f),true);
            var green=Material("Meadow",new Color(.55f,.61f,.25f));
            var foliage=Material("Pine foliage",new Color(.36f,.47f,.19f));
            var trunk=Material("Bark",new Color(.29f,.24f,.17f));
            var water=Material("Turquoise water",new Color(.12f,.55f,.57f));
            var farMat=Material("Distant blue rock",new Color(.43f,.57f,.67f));
            var rockMesh=new CrownRockMesh();var sod=new CrownRockMesh();var grass=new CrownRockMesh();var flowers=new CrownRockMesh();
            var needles=new CrownRockMesh();var bark=new CrownRockMesh();var valley=new CrownRockMesh();var river=new CrownRockMesh();var distant=new CrownRockMesh();
            var course=Course();
            var lowerMasses=new[]
            {
                new Rock(new Vector3(26,13,31),Shape(-6,-6,0,-9,6,-5,8,0,4,6,-3,7,-7,2),new Vector2(.25f,-.28f),new Vector3(-10,0,-18),44),
                new Rock(new Vector3(10,5,47),Shape(-6,-4,0,-7,7,-3,8,2,3,6,-4,5),new Vector2(-.23f,-.18f),new Vector3(-9,0,-17),36),
                new Rock(new Vector3(-18,10,28),Shape(-5,-5,1,-7,6,-2,5,4,-2,6,-6,1),new Vector2(.34f,-.21f),new Vector3(-15,0,-17),43),
                new Rock(new Vector3(-4,-3,59),Shape(-7,-5,1,-7,8,-2,6,5,-3,6,-8,1),new Vector2(-.27f,-.24f),new Vector3(-9,0,-17),29),
                new Rock(new Vector3(30,24,0),Shape(-4,-5,0,-6,5,-2,4,4,-2,5),new Vector2(.24f,-.23f),new Vector3(-9,0,-17),55)
            };
            float Bedrock(float x,float z)
            {
                float radius=new Vector2((x+14)/82,(z-4)/92).magnitude;
                float h=-38+80*Mathf.Pow(Mathf.Clamp01(1-radius),.72f);
                h+=(Mathf.PerlinNoise(x*.1f+42,z*.1f+17)-.5f)*3*Mathf.Clamp01(1-radius);
                foreach(var ledge in course)
                {
                    float distance=Vector2.Distance(new Vector2(x,z),new Vector2(ledge.Center.x,ledge.Center.z));
                    h=Mathf.Min(h,ledge.Center.y-9+Mathf.Max(0,distance-5)*.8f);
                }
                foreach(var mass in lowerMasses)
                {
                    float distance=Vector2.Distance(new Vector2(x,z),new Vector2(mass.Center.x,mass.Center.z));
                    h=Mathf.Min(h,mass.Center.y-3+Mathf.Max(0,distance-5)*1.2f);
                }
                float phase=x*.19f-z*.14f;
                float band=(h+phase)/6.5f;
                float fraction=band-Mathf.Floor(band);
                float profile=fraction<.78f?fraction*.15f:.117f+(fraction-.78f)/.22f*.883f;
                h=(Mathf.Floor(band)+profile)*6.5f-phase;
                float fracture=Mathf.Abs(Mathf.Sin((x+z*.42f)*.23f));
                h-=Mathf.Pow(Mathf.Clamp01(1-fracture/.12f),2)*1.6f*Mathf.Clamp01(1-radius);
                return h;
            }
            for(int z=-90;z<102;z+=2)for(int x=-100;x<84;x+=2)
            {
                var a=new Vector3(x,Bedrock(x,z),z);var b=new Vector3(x,Bedrock(x,z+2),z+2);
                var c=new Vector3(x+2,Bedrock(x+2,z+2),z+2);var d=new Vector3(x+2,Bedrock(x+2,z),z);
                Quad(rockMesh,a,b,c,d,new Color(.84f,.86f,.85f));
            }
            for(int i=0;i<course.Length;i++)
            {
                var rock=course[i];
                rock.Foot=new Vector3(rock.Foot.x*1.7f,0,rock.Foot.z*2.1f);
                Cliff(rockMesh,rock);
                var shoulder=rock.Center+new Vector3(-4,-7,-8);
                Cliff(rockMesh,new Rock(shoulder,Shape(-5,-5,0,-7,5,-3,6,1,2,4,-4,3),new Vector2(.18f,-.24f),new Vector3(-7,0,-12),22+i*2));
            }
            var back=new[]
            {
                new Rock(new Vector3(-18,55,-23),Shape(-15,-11,-4,-15,9,-11,14,-3,10,9,0,11,-12,7),new Vector2(.35f,-.19f),new Vector3(-22,0,-16),90),
                new Rock(new Vector3(-28,44,3),Shape(-8,-8,0,-11,8,-5,7,4,0,9,-8,5),new Vector2(-.3f,.14f),new Vector3(-14,0,-8),79),
                new Rock(new Vector3(-23,30,23),Shape(-8,-8,0,-10,8,-5,7,4,0,8,-8,5),Vector2.zero,new Vector3(-12,0,-8),65),
                new Rock(new Vector3(24,43,-5),Shape(-7,-8,1,-11,7,-4,6,5,-1,9,-7,3),new Vector2(.32f,-.14f),new Vector3(14,0,-14),78),
                new Rock(new Vector3(21,34,10),Shape(-4,-5,0,-7,5,-2,4,4,-1,5,-4,2),Vector2.zero,new Vector3(7,0,-6),69),
                new Rock(new Vector3(17,35,18),Shape(-5,-2,0,-3,5,-2,4,1,-2,1,-5,0),new Vector2(.03f,-.08f),new Vector3(1,0,-1),4)
            };
            foreach(var mass in back)Cliff(rockMesh,mass,true,mass.Depth>10);
            Cliff(rockMesh,new Rock(new Vector3(-6,43,-15),Shape(-9,-4,-2,-7,7,-4,10,0,5,4,-5,3,-10,0),new Vector2(.42f,-.25f),new Vector3(-19,0,-25),78),true,true);
            Cliff(rockMesh,new Rock(new Vector3(-17,32,18),Shape(-4,-6,1,-7,5,-3,4,2,0,6,-4,4),new Vector2(-.32f,-.18f),new Vector3(-16,0,-22),67),true,true);
            foreach(int bi in new[]{0,1,2,3})
            {
                var mass=back[bi];
                Patch(sod,grass,flowers,mass,new Vector2(-1,0),new Vector2(3,2));
                Pine(needles,bark,mass.Surface(new Vector2(-2,0))+Vector3.up*.1f,R(5,9),R(-.3f,.3f));
            }
            Patch(sod,grass,flowers,course[0],new Vector2(-2,-.8f),new Vector2(3.6f,3.1f));
            Patch(sod,grass,flowers,course[1],new Vector2(-2,0),new Vector2(1.5f,2.1f));
            Patch(sod,grass,flowers,course[4],new Vector2(2.7f,-.8f),new Vector2(1.6f,2.7f));
            Patch(sod,grass,flowers,course[6],new Vector2(-2,0),new Vector2(1.1f,1.5f));
            Patch(sod,grass,flowers,course[7],new Vector2(-1,0),new Vector2(4,3.5f));
            Patch(sod,grass,flowers,back[4],new Vector2(1,0),new Vector2(1.5f,2));
            Patch(sod,grass,flowers,back[5],new Vector2(1,-1),new Vector2(2,1.6f));
            foreach(int ri in new[]{0,1,4,7})
            {
                var p=course[ri].Surface(new Vector2(-1,-1));
                Shrub(needles,p+Vector3.up*.4f,new Vector3(.8f,.5f,.7f),new Color(.85f,.95f,.55f));
                Cluster(rockMesh,course[ri].Surface(new Vector2(-2.2f,-1.8f))+Vector3.up*.22f,new Vector3(.4f,.28f,.3f),new Color(.8f,.82f,.77f));
            }
            var treeSites=new[]{course[0].Surface(new Vector2(-3,-2)),course[4].Surface(new Vector2(3,2.3f)),course[7].Surface(new Vector2(-3,2)),back[4].Surface(new Vector2(1,0))};
            foreach(var p in treeSites)Pine(needles,bark,p+Vector3.up*.05f,R(3.8f,5.5f),R(-.4f,.4f));
            Pine(needles,bark,course[0].Surface(new Vector2(-4,-1))+Vector3.up*.05f,2.8f,-.2f);
            Pine(needles,bark,course[7].Surface(new Vector2(-4,1))+Vector3.up*.05f,3.4f,.2f);
            for(int i=0;i<lowerMasses.Length;i++)
            {
                var mass=lowerMasses[i];Cliff(rockMesh,mass,true,true);
                Patch(sod,grass,flowers,mass,new Vector2(-1,-1),new Vector2(2.4f,1.8f));
                Pine(needles,bark,mass.Surface(new Vector2(-1,-1))+Vector3.up*.1f,R(3,5),R(-.3f,.3f));
            }
            for(int z=0;z<150;z++)for(int x=0;x<150;x++)
            {
                float px=-750+x*10,pz=-650+z*10;
                var a=new Vector3(px,Valley(px,pz),pz);var b=new Vector3(px,Valley(px,pz+10),pz+10);
                var c=new Vector3(px+10,Valley(px+10,pz+10),pz+10);var d=new Vector3(px+10,Valley(px+10,pz),pz);
                Quad(valley,a,b,c,d,Color.white);
            }
            for(int z=-150;z<740;z+=5)
            {
                float x=90+25*Mathf.Sin(z*.012f),next=90+25*Mathf.Sin((z+5)*.012f);
                Quad(river,new Vector3(x-9,-37.8f,z),new Vector3(next-9,-37.8f,z+5),new Vector3(next+9,-37.8f,z+5),new Vector3(x+9,-37.8f,z),Color.white);
            }
            for(int i=0;i<2600;i++)
            {
                float x=R(-440,440),z=R(-650,650),h=Valley(x,z);
                float band=Mathf.PerlinNoise(x*.013f+12,z*.013f);
                if(h>-36&&band>.43f&&(Mathf.Abs(z)>60||Mathf.Abs(x)>80))Pine(needles,bark,new Vector3(x,h,z),R(4,13),R(-.5f,.5f));
            }
            var snow=new CrownRockMesh();
            CliffStudyAlpineRidges.Build(distant,snow);
            var talus=new Rock(new Vector3(-10,-20,5),Shape(-34,-20,-8,-38,28,-23,42,1,23,26,-5,45,-35,27),new Vector2(.1f,-.25f),new Vector3(-13,0,-8),30);
            Cliff(rockMesh,talus,true,true);
            foreach(var rock in course)
            {
                for(int i=0;i<28;i++)
                {
                    var direction=Quaternion.Euler(0,R(0,360),0)*Vector3.forward;
                    var p=Vector3.Lerp(rock.Center,rock.Edge(direction),R(.58f,.95f));
                    bool clear=Vector3.ProjectOnPlane(p-rock.Center,Vector3.up).magnitude>1.6f;
                    int at=Array.IndexOf(course,rock);
                    if(at>0&&PlanarSegmentDistance(p,rock.Center,rock.Edge(course[at-1].Center-rock.Center))<1.4f)clear=false;
                    if(at<course.Length-1&&PlanarSegmentDistance(p,rock.Center,rock.Edge(course[at+1].Center-rock.Center))<1.4f)clear=false;
                    if(!clear)continue;
                    float radius=R(.12f,.45f);
                    Cluster(rockMesh,p+Vector3.up*radius*.25f,new Vector3(radius,radius*.55f,radius*.8f),new Color(.92f,.94f,.9f),R(0,360));
                    if(i%7==0)Shrub(needles,p+Vector3.up*.18f,new Vector3(.38f,.28f,.3f),new Color(.91f,.98f,.58f));
                }
            }
            var plates=new CrownRockMesh();
            Cliff(plates,new Rock(new Vector3(-26,35,4),Shape(-3,-2,1,-3,3,-1,2,2,-2,2),new Vector2(-.35f,.1f),new Vector3(-16,0,-8),62));
            Cliff(plates,new Rock(new Vector3(-17,47,-10),Shape(-2,-2,1,-3,3,0,1,2,-2,1),new Vector2(.25f,.1f),new Vector3(-20,0,-12),75));
            Cliff(plates,new Rock(new Vector3(26,28,20),Shape(-3,-2,1,-3,4,-1,2,2,-2,3),new Vector2(.3f,-.1f),new Vector3(12,0,-9),56));
            Add(root,"Leaning fracture plates",plates,stone);
            Add(root,"Hand-shaped cliff",rockMesh,stone,true);Add(root,"Grass soil",sod,green,false,true);
            Add(root,"Grass blades",grass,Material("Grass tips",new Color(.57f,.64f,.28f)));Add(root,"Wildflowers",flowers,Material("Petals",Color.white));
            Add(root,"Pine branches",needles,foliage);Add(root,"Tree trunks",bark,trunk);
            root.Find("Hand-shaped cliff").gameObject.AddComponent<MountainSlopeSurface>();
            Add(root,"Valley",valley,Material("Valley vegetation",new Color(.39f,.50f,.24f)),false,true);Add(root,"River",river,water);Add(root,"Far ridges",distant,farMat,false,true);Add(root,"Alpine snow",snow,Material("Distant snow",new Color(.83f,.89f,.94f)),false,true);
            var points=new Vector3[course.Length];var radii=new float[course.Length];var takes=new Vector3[course.Length-1];var lands=new Vector3[takes.Length];
            for(int i=0;i<course.Length;i++)
            {
                points[i]=course[i].Center;radii[i]=i==0||i==4||i==7?3f:1.8f;
                if(i==course.Length-1)continue;
                var direction=course[i+1].Center-course[i].Center;
                takes[i]=course[i].Edge(direction);lands[i]=course[i+1].Edge(-direction);
            }
            root.gameObject.AddComponent<CliffStudyAtmosphere>();
            var gameplay=root.gameObject.AddComponent<ScenicGoatCourse>();gameplay.Configure(points,new[]{0,4,7},stone);gameplay.ConfigureJumps(takes,lands);gameplay.ConfigureDescent(radii);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();
            camera.transform.position=new Vector3(58,48,77);camera.transform.LookAt(new Vector3(-2,25,12));camera.fieldOfView=46;camera.nearClipPlane=.1f;camera.farClipPlane=2200;
            var sun=new GameObject("Afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(42,-145,0);sun.color=new Color(1,.91f,.76f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;sun.shadowBias=.15f;sun.shadowNormalBias=.8f;sun.shadowCustomResolution=4096;
            RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.40f,.54f,.69f);RenderSettings.ambientEquatorColor=new Color(.29f,.35f,.43f);RenderSettings.ambientGroundColor=new Color(.23f,.22f,.19f);
            var ambient=new UnityEngine.Rendering.SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.34f,.42f,.52f));RenderSettings.ambientProbe=ambient;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0011f;RenderSettings.fogColor=new Color(.55f,.72f,.86f);
            var sky=new Material(Shader.Find("GoatDescent/Sky"));sky.SetColor("_SkyZenith",new Color(.12f,.43f,.81f));sky.SetColor("_SkyHorizon",new Color(.62f,.80f,.93f));sky.SetFloat("_CloudDensity",.4f);sky.SetVector("_SunDir",-sun.transform.forward);RenderSettings.skybox=ScenicWorldAuthoring.Save(sky,Folder+"/Sky.mat");
            QualitySettings.shadowDistance=180;QualitySettings.shadowCascades=4;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
            Physics.SyncTransforms();
            int easyJumps=0,hardJumps=0;
            for(int i=0;i<points.Length;i++)
            {
                if(!Physics.Raycast(points[i]+Vector3.up*.6f,Vector3.down,out var support,1))throw new Exception("Missing footing "+i);
                if(!support.collider.GetComponent<MountainSlopeSurface>())throw new Exception("Missing hoof support surface "+i);
                if(Physics.CheckCapsule(points[i]+Vector3.up*.5f,points[i]+Vector3.up*1.6f,.35f))throw new Exception("Blocked platform "+i);
                if(i<takes.Length)
                {
                    float gap=Vector3.ProjectOnPlane(lands[i]-takes[i],Vector3.up).magnitude,drop=takes[i].y-lands[i].y;
                    if(gap<1.6f||gap>5f||drop<1.5f||drop>5f)throw new Exception("Invalid descent transition "+i);
                    if(gap>3.4f)hardJumps++;else easyJumps++;
                    Debug.Log($"CLIFF_STUDY_LINK {i} gap={gap:F2} drop={drop:F2}");
                }
            }
            if(easyJumps<2||hardJumps<2)throw new Exception("Missing jump variety");
            Debug.Log($"CLIFF_JUMP_VARIETY easy={easyJumps} hard={hardJumps}");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/GoatDescent/Scenes/TerraceCliffStudy.unity");
            ScenicWorldAuthoring.Capture(camera,"CliffStudy-overview");
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            for(int i=0;i<3;i++)
            {
                int at=new[]{0,3,5}[i];var direction=Vector3.ProjectOnPlane(points[at+1]-points[at],Vector3.up).normalized;
                camera.transform.position=points[at]-direction*2.5f+Vector3.up*2.4f;camera.transform.LookAt(points[at+1]+Vector3.up*.7f);camera.fieldOfView=62;
                ScenicWorldAuthoring.Capture(camera,"CliffStudy-route-"+(i+1));
            }
            camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=46;
            var clay=new Material(Shader.Find("Standard")){color=new Color(.6f,.6f,.6f)};
            var renderers=root.GetComponentsInChildren<MeshRenderer>();var saved=new Material[renderers.Length];
            for(int i=0;i<renderers.Length;i++){saved[i]=renderers[i].sharedMaterial;renderers[i].sharedMaterial=clay;}
            ScenicWorldAuthoring.Capture(camera,"CliffStudy-clay");
            for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterial=saved[i];Object.DestroyImmediate(clay);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if(ShaderUtil.ShaderHasError(stone.shader))throw new Exception("Cliff study shader error");
            Debug.Log("CLIFF_STUDY_SAVED links=7 drop=22m");
        }
    }
}
