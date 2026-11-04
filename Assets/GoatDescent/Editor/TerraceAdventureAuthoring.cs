using System;
using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoatDescent.EditorTools
{
    public static class TerraceAdventureAuthoring
    {
        private const string Folder = "Assets/GoatDescent/Level/Static/TerracePeak";
        private static System.Random Random = new System.Random(41011);
        private static float R(float a, float b) => Mathf.Lerp(a,b,(float)Random.NextDouble());
        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Color color)
            => mesh.Triangle(a,b,c,color,color,color);
        private static void Quad(CrownRockMesh mesh, Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        { Tri(mesh,a,b,c,color); Tri(mesh,a,c,d,color); }
        private static void Slab(CrownRockMesh mesh, Vector3 center, Vector3 size, float lean, bool level = false)
        {
            const int sides=11, rings=7;
            var v=new Vector3[rings,sides];
            float phase=R(0,6.28f);
            for(int k=0;k<sides;k++)
            {
                float angle=k*Mathf.PI*2/sides+phase;
                float fracture=R(.82f,1.12f);
                for(int j=0;j<rings;j++)
                {
                    float t=j/(float)(rings-1);
                    float radius=fracture*(1f-.22f*t)+.08f*Mathf.Sin(k*2+j*1.7f);
                    v[j,k]=center+new Vector3(Mathf.Cos(angle)*size.x*radius+lean*t,
                        size.y*t+(level && j==rings-1 ? 0 : R(-.025f,.025f)*size.y),Mathf.Sin(angle)*size.z*radius);
                }
            }
            for(int k=0;k<sides;k++)
            {
                int n=(k+1)%sides;
                for(int j=0;j<rings-1;j++)
                {
                    Color color=Color.white*R(.80f,1.06f);color.a=1;
                    Quad(mesh,v[j,k],v[j+1,k],v[j+1,n],v[j,n],color);
                }
                Tri(mesh,center+new Vector3(lean,size.y,0),v[rings-1,n],v[rings-1,k],Color.white);
            }
        }
        private static Material Mat(string name,Color color, bool painted=true)
        {
            var m=new Material(Shader.Find(painted?"GoatDescent/TerracePaintedRock":"Standard")){name=name};
            m.color=color;
            if(!painted)m.SetFloat("_Glossiness",.06f);
            return ScenicWorldAuthoring.Save(m,Folder+"/Adventure-"+name+".mat");
        }
        private static void Add(Transform parent,string name,CrownRockMesh mesh,Material material,bool collision=false)
            => ScenicTrailAuthoring.Add(parent,name,mesh.Build(name),material,Folder+"/Adventure-"+name+".asset",collision);
        private static void Tree(CrownRockMesh foliage,CrownRockMesh trunks,Vector3 p,float height)
        {
            Slab(trunks,p,new Vector3(.18f,height*.75f,.18f),.15f,true);
            for(int layer=0;layer<5;layer++)
            {
                float y=height*(.15f+layer*.145f), radius=height*(.27f-layer*.038f);
                for(int k=0;k<9;k++)
                {
                    float a=k*Mathf.PI*2/9,b=(k+1)*Mathf.PI*2/9;
                    Tri(foliage,p+Vector3.up*(y+height*.42f),p+new Vector3(Mathf.Cos(b)*radius*(1+.18f*Mathf.Sin(k*2.7f+layer)),y,Mathf.Sin(b)*radius*(1+.18f*Mathf.Sin(k*2.7f+layer))),p+new Vector3(Mathf.Cos(a)*radius*(1+.18f*Mathf.Sin((k-1)*2.7f+layer)),y,Mathf.Sin(a)*radius*(1+.18f*Mathf.Sin((k-1)*2.7f+layer))),new Color(R(.7f,1.1f),1,R(.7f,1),1));
                }
            }
        }
        private static float Ground(float x,float z,DescentRock[] rocks)
        {
            float nearest=float.MaxValue,sum=0,weight=0;
            foreach(var rock in rocks)
            {
                float d=Vector2.Distance(new Vector2(x,z),new Vector2(rock.Center.x,rock.Center.z));
                nearest=Mathf.Min(nearest,d);
                float w=1f/Mathf.Pow(d*d+64,2);
                sum+=rock.Center.y*w;weight+=w;
            }
            float ridge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,45,nearest))*24;
            float height=Mathf.Min(320,sum/weight-34+ridge+3*Mathf.PerlinNoise(x*.027f+40,z*.027f));
            return Mathf.Lerp(height,-50,Mathf.SmoothStep(0,1,Mathf.InverseLerp(70,250,nearest)));
        }
        private static void Platform(CrownRockMesh mesh,DescentRock rock)
        {
            const int count=8;
            for(int k=0;k<count;k++)
            {
                float a=k*Mathf.PI*2/count,b=(k+1)*Mathf.PI*2/count;
                var p=rock.Center+new Vector3(Mathf.Cos(a)*rock.Radius,0,Mathf.Sin(a)*rock.Radius)*(1.02f+.035f*Mathf.Sin(k*2.1f+rock.Center.z));
                var q=rock.Center+new Vector3(Mathf.Cos(b)*rock.Radius,0,Mathf.Sin(b)*rock.Radius)*(1.02f+.035f*Mathf.Sin((k+1)*2.1f+rock.Center.z));
                p.y=rock.Height(p);q.y=rock.Height(q);
                var lowP=rock.Center+(p-rock.Center)*2.5f-Vector3.up*36;
                var lowQ=rock.Center+(q-rock.Center)*2.5f-Vector3.up*36;
                Tri(mesh,rock.Center,q,p,Color.white);
                Quad(mesh,p,q,q-Vector3.up*7,p-Vector3.up*7,Color.white);
                Quad(mesh,p-Vector3.up*7,q-Vector3.up*7,lowQ,lowP,Color.white);
            }
        }
        [MenuItem("Tools/GOAT/Живописные маршруты/TerracePeak — спуск по скалам")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Random=new System.Random(41011);
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/TerracePeak.unity");
            foreach(var course in Object.FindObjectsByType<ScenicGoatCourse>(FindObjectsSortMode.None))Object.DestroyImmediate(course.gameObject);
            foreach(string name in new[]{"Terrace Adventure","Terrace Descent","Крупные скальные пласты"})
            {var old=GameObject.Find(name);if(old)Object.DestroyImmediate(old);}
            var mountain=GameObject.Find("Уступчатый пик — статичная гора");
            if(mountain)
            {
                mountain.transform.localScale=Vector3.one*.7f;
                var retired=new List<GameObject>();
                foreach(Transform child in mountain.transform)
                    if(child.name!="Дальние снежные пики")retired.Add(child.gameObject);
                foreach(var child in retired)Object.DestroyImmediate(child);
            }
            var root=new GameObject("Terrace Descent").transform;
            var stone=Mat("Ochre",new Color(.74f,.60f,.39f));
            var light=Mat("Limestone",new Color(.87f,.74f,.49f));
            var grass=Mat("Meadow",new Color(.52f,.62f,.16f),false);
            var needles=Mat("Pine",new Color(.14f,.29f,.12f),false);
            var bark=Mat("Bark",new Color(.25f,.19f,.12f),false);
            var flowers=Mat("Flowers",new Color(.91f,.75f,.28f),false);
            var massif=new CrownRockMesh();var shelves=new CrownRockMesh();var greens=new CrownRockMesh();
            var trees=new CrownRockMesh();var trunks=new CrownRockMesh();var shrubs=new CrownRockMesh();var petals=new CrownRockMesh();
            var rocks=DescentCourseLayout.Create();
            float endZ=rocks[rocks.Length-1].Center.z;
            const int nx=160,nz=260;
            var ground=new Vector3[nx+1,nz+1];
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float px=Mathf.Lerp(-350,430,x/(float)nx),pz=Mathf.Lerp(-300,endZ+300,z/(float)nz);
                ground[x,z]=new Vector3(px,Ground(px,pz,rocks),pz);
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
                Quad(massif,ground[x,z],ground[x,z+1],ground[x+1,z+1],ground[x+1,z],Color.white);
            Quad(greens,new Vector3(-2500,-50,-2500),new Vector3(-2500,-50,2500),new Vector3(2500,-50,2500),new Vector3(2500,-50,-2500),Color.white);
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                var center=(ground[x,z]+ground[x+1,z+1])*.5f;
                float slope=Vector3.Cross(ground[x,z+1]-ground[x,z],ground[x+1,z]-ground[x,z]).normalized.y;
                if(slope>.65f && center.y>-40 && Mathf.PerlinNoise(center.x*.035f+20,center.z*.035f)>.48f)
                    Quad(greens,ground[x,z]+Vector3.up*.08f,ground[x,z+1]+Vector3.up*.08f,ground[x+1,z+1]+Vector3.up*.08f,ground[x+1,z]+Vector3.up*.08f,Color.white);
            }
            for(int i=0;i<650;i++)
            {
                float x=R(-180,260),z=R(-80,endZ+100),d=float.MaxValue;
                foreach(var rock in rocks)d=Mathf.Min(d,Vector2.Distance(new Vector2(x,z),new Vector2(rock.Center.x,rock.Center.z))-rock.Radius);
                float h=Ground(x,z,rocks);
                if(d>14 && h>-30 && Mathf.PerlinNoise(x*.035f+20,z*.035f)>.50f)
                    Tree(trees,trunks,new Vector3(x,h,z),R(3,8));
            }
            var takeoffs=new List<Vector3>();var landings=new List<Vector3>();var points=new List<Vector3>();var checkpoints=new List<int>();
            var radii=new List<float>();
            float length=0;int hard=0;
            for(int i=0;i<rocks.Length;i++)
            {
                var rock=rocks[i];Platform(shelves,rock);
                points.Add(rock.Center);radii.Add(rock.Radius*.8f);
                if(rock.Rest)checkpoints.Add(i);
                if(i<rocks.Length-1)
                {
                    takeoffs.Add(DescentCourseLayout.Takeoff(rock,rocks[i+1]));
                    landings.Add(DescentCourseLayout.Landing(rock,rocks[i+1]));
                    length+=Vector3.Distance(rock.Center,rocks[i+1].Center);
                    if(rocks[i+1].Difficult)hard++;
                }
                Vector3 forward=i<rocks.Length-1?Vector3.ProjectOnPlane(rocks[i+1].Center-rock.Center,Vector3.up).normalized:Vector3.forward;
                Vector3 side=Vector3.Cross(Vector3.up,forward);
                if(rock.Rest)
                {
                    var patch=rock.Center+side*3.5f;
                    Slab(greens,patch,new Vector3(1.6f,.12f,1.5f),0,true);
                    Tree(trees,trunks,patch+Vector3.up*.12f,R(4,7));
                    for(int tier=0;tier<3;tier++)Slab(shrubs,rock.Center-side*3.8f+Vector3.up*tier*.3f,new Vector3(.5f-tier*.1f,.3f,.4f-tier*.08f),0,true);
                }
                for(int j=0;j<14;j++)
                {
                    float a=j*Mathf.PI*2/14;
                    var edge=rock.Center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(rock.Radius-.15f);
                    edge.y=rock.Height(edge);
                    var tip=edge+Vector3.up*R(.18f,.4f);
                    Tri(greens,edge-side*.09f,tip,edge+side*.09f,Color.white);
                    Tri(greens,edge+side*.09f,tip,edge-side*.09f,Color.white);
                    if(j%4==0)Slab(petals,edge,new Vector3(.07f,.22f,.07f),0);
                }
            }
            for(int i=0;i<220;i++)
            {
                float x=R(-95,175),z=R(-80,endZ+90),distance=float.MaxValue;
                foreach(var rock in rocks)distance=Mathf.Min(distance,Vector2.Distance(new Vector2(x,z),new Vector2(rock.Center.x,rock.Center.z))-rock.Radius);
                if(distance<13)continue;
                float baseY=Ground(x,z,rocks),height=Mathf.Min(R(7,35),326-baseY);
                Slab(massif,new Vector3(x,baseY-6,z),new Vector3(R(3,8),height,R(3,8)),R(-3,3),i%3==0);
                if(i%3==0)
                {
                    var top=new Vector3(x,baseY-6+height,z);
                    Slab(greens,top,new Vector3(2,.25f,2),0,true);
                    Tree(trees,trunks,top+Vector3.up*.25f,R(4,9));
                }
            }
            Add(root,"Fractured massif",massif,stone,true);
            Add(root,"Isolated descent rocks",shelves,light,true);
            Add(root,"Living meadow edges",greens,grass);
            Add(root,"Pine crowns",trees,needles);
            Add(root,"Pine trunks",trunks,bark);
            Add(root,"Rest cairns",shrubs,stone);
            Add(root,"Wildflowers",petals,flowers);
            var gameplay=root.gameObject.AddComponent<ScenicGoatCourse>();
            gameplay.Configure(points.ToArray(),checkpoints.ToArray(),light);
            gameplay.ConfigureJumps(takeoffs.ToArray(),landings.ToArray());
            gameplay.ConfigureDescent(radii.ToArray());
            ScenicWorldAuthoring.Lighting("TerracePeak");
            RenderSettings.fogDensity=.0004f;QualitySettings.shadowDistance=650;
            var camera=Camera.main;
            camera.transform.position=new Vector3(390,370,endZ+200);
            camera.transform.LookAt(new Vector3(35,155,endZ*.44f));camera.fieldOfView=53;camera.farClipPlane=7000;
            Physics.SyncTransforms();int blocked=0,unsupported=0,filledGaps=0;
            foreach(var p in points)
            {
                if(!Physics.Raycast(p+Vector3.up*.6f,Vector3.down,1f))unsupported++;
                if(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.3f))blocked++;
            }
            for(int i=0;i<takeoffs.Count;i++)
            {
                var mid=(takeoffs[i]+landings[i])*.5f;
                if(Physics.Raycast(mid+Vector3.up,Vector3.down,5f))filledGaps++;
                if(landings[i].y>=takeoffs[i].y)throw new InvalidOperationException("Descent contains an uphill jump");
            }
            Debug.Log($"DESCENT_GEOMETRY unsupported={unsupported} blocked={blocked} filledGaps={filledGaps}");
            if(blocked>0||unsupported>0||filledGaps>0)throw new InvalidOperationException("Descent geometry validation failed");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            ScenicWorldAuthoring.Capture(camera,"TerracePeak-descent-overview");
            var pos=camera.transform.position;var rot=camera.transform.rotation;
            for(int i=0;i<7;i++)
            {
                int at=Mathf.RoundToInt((rocks.Length-4)*i/6f);
                camera.transform.position=points[at]-Vector3.ProjectOnPlane(points[at+1]-points[at],Vector3.up).normalized*3f+Vector3.up*4f;
                camera.transform.LookAt(points[at+2]+Vector3.up*.7f);
                ScenicWorldAuthoring.Capture(camera,"TerracePeak-descent-route-"+(i+1));
            }
            camera.transform.SetPositionAndRotation(pos,rot);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"TERRACE_DESCENT jumps={takeoffs.Count} easy={takeoffs.Count-hard} hard={hard} drop={points[0].y-points[points.Count-1].y:F1}m length={length:F0}m checkpoints={checkpoints.Count} target=420-600s");
        }
    }
}
