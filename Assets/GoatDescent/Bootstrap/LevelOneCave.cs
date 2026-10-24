using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>A hollow, walkable shelter attached to the final cliff terrace.</summary>
    public sealed class LevelOneCave : MonoBehaviour
    {
        public static LevelOneCave Current {get;private set;}
        public Vector3 Approach => transform.TransformPoint(new Vector3(0,0,-3f));
        public Vector3 Entrance => transform.position;
        public Vector3 FinishPoint => transform.TransformPoint(new Vector3(0,0,8f));
        public bool Completed {get;private set;}
        public bool FirstArrived {get;private set;}
        public bool SecondArrived {get;private set;}
        private float nextScan;
        private readonly float[] settled=new float[2];
        private readonly List<Object> owned=new List<Object>();

        private void Awake() => Current=this;
        private void OnDestroy()
        {
            if(Current==this) Current=null;
            foreach(var asset in owned) if(asset) Destroy(asset);
        }

        public static LevelOneCave Build(Transform parent,Vector3 lastWall,Vector3 outward,Material stone)
        {
            var root=new GameObject("Level 1 — shelter cave finish");
            root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(lastWall+outward*7f-Vector3.up*2f,
                Quaternion.LookRotation(outward,Vector3.up));
            var cave=root.AddComponent<LevelOneCave>();
            cave.BuildGeometry(stone);
            return cave;
        }

        private void BuildGeometry(Material stone)
        {
            var inside=new Material(stone) {name="Cave weathered inner stone"};owned.Add(inside);
            if(inside.HasProperty("_StoneColor")) inside.SetColor("_StoneColor",new Color(.43f,.38f,.29f));
            if(inside.HasProperty("_ShadowStone")) inside.SetColor("_ShadowStone",new Color(.18f,.21f,.18f));
            if(inside.HasProperty("_RimStrength")) inside.SetFloat("_RimStrength",.06f);
            if(inside.HasProperty("_MossAmount")) inside.SetFloat("_MossAmount",.22f);
            // Standard lighting inside avoids adding the sandstone toon shader's
            // ambient term once again for every local point light.
            var detail=stone.HasProperty("_RockTex") ? stone.GetTexture("_RockTex") : null;
            inside.shader=Shader.Find("Standard");inside.color=new Color(.31f,.34f,.29f);
            inside.SetFloat("_Glossiness",.08f);
            if(detail) inside.SetTexture("_MainTex",detail);

            // The floor grows out of the pillar and has a solid, tapered underside.
            Vector3[] floorVertices={
                new Vector3(-6.9f,0,-9f),new Vector3(6.9f,0,-9f),
                new Vector3(-7.2f,0,17.5f),new Vector3(7.2f,0,17.5f),
                new Vector3(-4f,-9f,-9f),new Vector3(4f,-9f,-9f),
                new Vector3(-3.5f,-6f,15f),new Vector3(3.5f,-6f,15f)};
            int[] floorTriangles={0,2,1,1,2,3,0,4,2,2,4,6,3,7,1,1,7,5,
                2,6,3,3,6,7,4,5,6,5,7,6,0,1,4,1,5,4};
            var floor=MeshObject("Rooted cave terrace",floorVertices,floorTriangles,inside);
            var support=floor.AddComponent<BoxCollider>();support.center=new Vector3(0,-.4f,4.25f);
            support.size=new Vector3(13.6f,.8f,26.5f);
            floor.AddComponent<MountainSlopeSurface>();

            Vector2[] profile={new Vector2(-4.2f,0),new Vector2(-4.65f,1.6f),
                new Vector2(-4.3f,3.8f),new Vector2(-2.8f,5.55f),new Vector2(0,6.35f),
                new Vector2(2.8f,5.55f),new Vector2(4.3f,3.8f),new Vector2(4.65f,1.6f),new Vector2(4.2f,0)};
            float[] depths={0f,2.5f,6f,10f,14.5f};
            var inner=new List<Vector3>();var outer=new List<Vector3>();
            for(int ring=0;ring<depths.Length;ring++)
            for(int j=0;j<profile.Length;j++)
            {
                float taper=1f-ring*.045f;
                Vector2 point=profile[j]*taper;
                // Fixed, subtle irregularity; geometry is identical on both PCs.
                float rough=ring==0 ? 0 : Mathf.Sin(ring*2.7f+j*1.93f)*.14f;
                point.x+=rough;point.y=Mathf.Max(0,point.y+rough*.4f);
                inner.Add(new Vector3(point.x,point.y,depths[ring]));
                outer.Add(new Vector3(point.x+Mathf.Sign(point.x)*1.7f,point.y+1.7f,depths[ring]));
            }
            var innerTriangles=new List<int>();var outerTriangles=new List<int>();
            int stride=profile.Length;
            for(int ring=0;ring<depths.Length-1;ring++)
            for(int j=0;j<stride-1;j++)
            {
                int a=ring*stride+j,b=a+1,c=a+stride,d=c+1;
                innerTriangles.AddRange(new[]{a,b,c,b,d,c});
                outerTriangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var tunnel=MeshObject("Hollow cave — inner collision shell",inner.ToArray(),innerTriangles.ToArray(),inside);
            var wallUV=new Vector2[inner.Count];
            for(int ring=0;ring<depths.Length;ring++) for(int j=0;j<stride;j++)
                wallUV[ring*stride+j]=new Vector2(j*1.3f,depths[ring]*.3f);
            tunnel.GetComponent<MeshFilter>().sharedMesh.uv=wallUV;
            tunnel.AddComponent<MeshCollider>().sharedMesh=tunnel.GetComponent<MeshFilter>().sharedMesh;
            var roof=MeshObject("Cave outer roof",outer.ToArray(),outerTriangles.ToArray(),stone);
            roof.AddComponent<MeshCollider>().sharedMesh=roof.GetComponent<MeshFilter>().sharedMesh;
            var lipVertices=new List<Vector3>();var lipTriangles=new List<int>();
            for(int j=0;j<stride;j++) {lipVertices.Add(inner[j]);lipVertices.Add(outer[j]);}
            for(int j=0;j<stride-1;j++)
            {
                int a=j*2;lipTriangles.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});
            }
            var lip=MeshObject("Thick rocky cave mouth",lipVertices.ToArray(),lipTriangles.ToArray(),stone);
            lip.AddComponent<MeshCollider>().sharedMesh=lip.GetComponent<MeshFilter>().sharedMesh;
            Box("Closed rear rock wall",new Vector3(0,2.8f,15f),new Vector3(10f,5.6f,1f),inside);

            // Authored stones break up the silhouette without putting colliders
            // into the opening. The hollow shell above supplies the wall contacts.
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<5;i++)
                Rock("Cave flank stone",new Vector3(side*(6.25f+i%2*.25f),1.1f+i%3*.35f,i*3.35f),
                    new Vector3(2.8f,3.8f+i%2,4.7f),i+(side+1)*5,stone);
            for(int i=0;i<5;i++)
                Rock("Cave crown stone",new Vector3((i-2)*2.4f,7.7f-Mathf.Abs(i-2)*.4f,1f),
                    new Vector3(3.4f,2.7f,4.1f),14+i,stone);
            for(int i=0;i<5;i++)
                Rock("Cave bedrock",new Vector3((i-2)*2.8f,-4.5f,i%2*6f),
                    new Vector3(5f,7f,13f),20+i,stone);

            var glow=new Material(Shader.Find("Standard")) {name="Shelter amber crystals",color=new Color(.95f,.68f,.25f)};
            glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.8f,.32f,.055f)*1.2f);
            owned.Add(glow);
            for(int i=0;i<2;i++)
            {
                Vector3 point=new Vector3(i==0 ? -3.6f : 3.3f,1.2f,i==0 ? 3.5f : 10f);
                for(int j=0;j<3;j++)
                {
                    var crystal=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crystal.name="Warm shelter crystal";crystal.transform.SetParent(transform,false);
                    crystal.transform.localPosition=point+new Vector3((j-1)*.2f,j*.14f,0);
                    crystal.transform.localScale=new Vector3(.19f,.48f+j*.12f,.2f);
                    crystal.GetComponent<Renderer>().sharedMaterial=glow;
                    Destroy(crystal.GetComponent<Collider>());
                }
                var lamp=new GameObject("Local cave glow");lamp.transform.SetParent(transform,false);
                lamp.transform.localPosition=point+Vector3.up*.5f;
                var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.68f,.32f);
                light.intensity=1.5f;light.range=6f;light.shadows=LightShadows.None;
            }
        }

        private GameObject MeshObject(string label,Vector3[] vertices,int[] triangles,Material material)
        {
            var mesh=new Mesh {name=label,vertices=vertices,triangles=triangles};
            var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++) uv[i]=new Vector2(vertices[i].x*.3f,vertices[i].z*.3f);
            mesh.uv=uv;
            var colors=new Color[vertices.Length];for(int i=0;i<colors.Length;i++) colors[i]=new Color(.5f,.48f,.5f,1);
            mesh.colors=colors;mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var obj=new GameObject(label);obj.transform.SetParent(transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            return obj;
        }

        private void Box(string label,Vector3 point,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=label;
            obj.transform.SetParent(transform,false);obj.transform.localPosition=point;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material;
        }

        private void Rock(string label,Vector3 point,Vector3 size,int variant,Material material)
        {
            var prefab=Resources.Load<GameObject>($"ReferenceIbex/CliffStone_{variant%44:00}");
            if(!prefab) return;
            var obj=Instantiate(prefab,transform);obj.name=label;
            // Prefabs have different authored dimensions. Normalize each instance
            // from its mesh bounds before placing it around the hollow tunnel.
            obj.transform.localPosition=Vector3.zero;obj.transform.localRotation=Quaternion.identity;
            obj.transform.localScale=Vector3.one;
            var filters=obj.GetComponentsInChildren<MeshFilter>();if(filters.Length==0) return;
            Bounds bounds=default;bool first=true;
            foreach(var filter in filters)
            {
                if(!filter.sharedMesh) continue;
                Bounds meshBounds=filter.sharedMesh.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 sign=new Vector3((corner&1)==0 ? -1:1,(corner&2)==0 ? -1:1,(corner&4)==0 ? -1:1);
                    Vector3 local=obj.transform.InverseTransformPoint(filter.transform.TransformPoint(meshBounds.center+Vector3.Scale(meshBounds.extents,sign)));
                    if(first) {bounds=new Bounds(local,Vector3.zero);first=false;}else bounds.Encapsulate(local);
                }
            }
            if(first) return;
            Vector3 dimensions=bounds.size;
            obj.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,dimensions.x),size.y/Mathf.Max(.001f,dimensions.y),size.z/Mathf.Max(.001f,dimensions.z));
            obj.transform.localPosition=point-Vector3.Scale(bounds.center,obj.transform.localScale);
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial=material;
            foreach(var collider in obj.GetComponentsInChildren<Collider>()) Destroy(collider);
        }

        private void Update()
        {
            if(!MountainAuthority.IsHost || Time.time<nextScan || Completed) return;
            nextScan=Time.time+.2f;
            var pair=LocalGoatPair.Instance;if(!pair) return;
            Observe(pair.Primary,0);Observe(pair.Secondary,1);
            if(FirstArrived && SecondArrived) SetCompleted();
        }

        private void Observe(GoatController goat,int index)
        {
            if(!goat || (index==0 ? FirstArrived : SecondArrived)) return;
            Vector3 point=transform.InverseTransformPoint(goat.transform.position);
            var life=goat.GetComponent<RespawnController>();var ground=goat.GetComponent<GoatGroundDetector>();
            bool safe=!(life && life.IsDead) && !goat.IsPredatorCarried && ground && ground.IsGrounded &&
                Mathf.Abs(point.x)<3.1f && point.y>-.2f && point.y<1.8f && point.z>3f && point.z<13.8f;
            if(!safe) {settled[index]=0;return;}
            settled[index]+=.2f;if(settled[index]<1f) return;
            if(index==0) FirstArrived=true;else SecondArrived=true;
            if(!(FirstArrived && SecondArrived)) NotifyArrival(index==0 ? "A" : "B");
        }

        private void NotifyArrival(string label)
        {
            var pair=LocalGoatPair.Instance;
            (pair ? pair.Active : null)?.GetComponent<GoatSpectacle>()?.Shelter(label);
        }

        private void SetCompleted()
        {
            if(Completed) return;Completed=true;
            var pair=LocalGoatPair.Instance;
            (pair ? pair.Active : null)?.GetComponent<GoatSpectacle>()?.Finish("ПЕРВЫЙ СПУСК ПРОЙДЕН!");
            Debug.Log("LEVEL1_COMPLETE both goats reached the cave");
        }

        public PlayerMountainProgress[] CapturePlayers()
        {
            int end=Mathf.Max(0,(MountainGenerator.CurrentRoute?.Count??1)-1);
            return new[]{new PlayerMountainProgress {id="A",finished=FirstArrived,ledgeIndex=FirstArrived ? end : -1,checkpointIndex=-1},
                new PlayerMountainProgress {id="B",finished=SecondArrived,ledgeIndex=SecondArrived ? end : -1,checkpointIndex=-1}};
        }

        public void Apply(PlayerMountainProgress[] players,bool complete)
        {
            if(MountainAuthority.IsHost) return;
            string arrival=null;
            if(players!=null) foreach(var state in players)
            {
                if(state.finished && ((state.id=="A" && !FirstArrived) || (state.id=="B" && !SecondArrived))) arrival=state.id;
                if(state.id=="A") FirstArrived=state.finished;
                if(state.id=="B") SecondArrived=state.finished;
            }
            if(complete) SetCompleted();else Completed=false;
            if(!complete && arrival!=null) NotifyArrival(arrival);
        }
    }
}
