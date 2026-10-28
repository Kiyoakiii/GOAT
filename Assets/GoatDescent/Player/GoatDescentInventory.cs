using UnityEngine;
namespace GoatDescent
{
    [DefaultExecutionOrder(40)]
    public sealed class GoatDescentInventory : MonoBehaviour
    {
        private readonly int[] items=new int[3];
        private GoatController motor; private Rigidbody body; private GoatPhysicalBody rig;
        private int selected;
        private bool browsing;
        private float chuteUntil,axeUntil,openedAt,messageUntil;
        private string message="";
        private Transform canopy,axe,pack;
        private LineRenderer[] suspension;
        private Material cloth,metal,strap;
        private Material steel,rubber;
        private Transform harnessRoot;
        private LineRenderer[] belts;
        private LineRenderer clip,wrist;
        private float axePoseBlend;
        public float AxePoseBlend => axePoseBlend;
        public Vector3 SafetyClipPosition { get; private set; }
        public bool Parachuting => Time.time<chuteUntil;
        public bool UsingAxe => Time.time<axeUntil;
        private float axeGroundedAt = -10f;
        public bool AxeGrounded => UsingAxe && Time.time - axeGroundedAt < .15f;
        public Vector3 AxeContactPoint { get; private set; }
        public Vector3 AxeTipPosition => axe ? axe.TransformPoint(Vector3.up * .35f) : transform.position + Vector3.up;
        private static readonly string[] Names={"Длинная страховка","Ледоруб","Парашют"};
        private void Awake() { motor=GetComponent<GoatController>(); body=GetComponent<Rigidbody>(); rig=GetComponent<GoatPhysicalBody>(); }
        private void Tell(string text) { message=text; messageUntil=Time.time+3f; }
        private bool Available() => GetComponent<RespawnController>()?.IsDead != true && !motor.IsPredatorCarried
            && GetComponent<GoatComedyActions>()?.Busy != true && GetComponent<GoatPunchBuff>()?.PerformingPair != true
            && GetComponent<GoatDescentTricks>()?.Busy != true;
        public bool Give(int item)
        {
            if(item<0 || item>2 || items[item]>=1 || !Available()) return false;
            items[item]++; selected=item; Tell("ВЗЯЛ: "+Names[item]); return true;
        }
        public void ToggleChest()
        {
            if(!Available() || !DescentSupplyChest.Current || !DescentSupplyChest.Current.InReach(motor))
            { Tell("Подойди к сундуку с бирюзовыми полосами"); return; }
            browsing=!browsing;
        }
        public void SelectOrTake(int item)
        {
            if(!MountainAuthority.IsHost || item<0 || item>2) return;
            if(browsing && DescentSupplyChest.Current && DescentSupplyChest.Current.InReach(motor))
            {
                if(!DescentSupplyChest.Current.Take(motor,item)) Tell(items[item]>0 ? "Этот предмет уже есть в рюкзаке" : "Предмет закончился или козёл занят");
            }
            else { selected=item; Tell(Names[item]); }
        }
        public bool Use()
        {
            if(!MountainAuthority.IsHost || !Available()) return false;
            if(items[selected]<=0) { Tell("В рюкзаке нет: "+Names[selected]); return false; }
            if(selected==0)
            {
                var tricks=GetComponent<GoatDescentTricks>();
                if(tricks.Anchored) { Tell("Страховка уже закреплена"); return false; }
                if(!tricks.TryAnchor(30f,6f)) { Tell("Для страховки нужна опора рядом"); return false; }
            }
            else if(selected==1)
            {
                if(!FindSurface(out _)) { Tell("Подойди к склону: ледорубу нужна поверхность"); return false; }
                axeUntil=Time.time+8f;
            }
            else
            {
                if(motor.Grounded) { Tell("Парашют раскрывается в воздухе · сначала прыгни"); return false; }
                if(Parachuting) return false;
                chuteUntil=Time.time+12f; openedAt=Time.time;
            }
            items[selected]--; browsing=false; Tell("ПРИМЕНИЛ: "+Names[selected]); return true;
        }
        private bool FindSurface(out RaycastHit surface)
        {
            var ground=GetComponent<GoatGroundDetector>();
            if(ground.IsGrounded && ground.SlopeAngle>20f) { surface=ground.GroundHit; return surface.collider; }
            surface=default; float nearest=1.5f;
            Vector3 origin=body.position+Vector3.up*.85f;
            for(int i=0;i<8;i++)
                foreach(var hit in Physics.RaycastAll(origin,Quaternion.Euler(0,i*45,0)*transform.forward,1.5f,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.GetComponentInParent<GoatController>() || hit.collider.GetComponentInParent<GoatPhysicsPart>()
                        || hit.distance>=nearest || hit.normal.y<-.2f) continue;
                    surface=hit; nearest=hit.distance;
                }
            return surface.collider;
        }
        private void Update()
        {
            axePoseBlend=Mathf.MoveTowards(axePoseBlend,UsingAxe ? 1f : 0f,Time.deltaTime*4f);
            if(GetComponent<RespawnController>()?.IsDead == true) { StopUse(); return; }
            if(MountainAuthority.IsHost)
            {
                if(!Available() && (Parachuting || UsingAxe)) StopUse();
                if(Parachuting && motor.Grounded && Time.time-openedAt>.35f) chuteUntil=0;
                if(browsing && (!DescentSupplyChest.Current || !DescentSupplyChest.Current.InReach(motor))) browsing=false;
                if(!motor.NetworkOwnedByRemote && GoatLocalControl.AllowsInput(this))
                {
                    if(Input.GetKeyDown(KeyCode.I)) ToggleChest();
                    if(Input.GetKeyDown(KeyCode.Alpha1)) SelectOrTake(0);
                    if(Input.GetKeyDown(KeyCode.Alpha2)) SelectOrTake(1);
                    if(Input.GetKeyDown(KeyCode.Alpha3)) SelectOrTake(2);
                    if(Input.GetKeyDown(KeyCode.U)) Use();
                }
            }
        }
        private void FixedUpdate()
        {
            if(!MountainAuthority.IsHost) return;
            if(Parachuting)
            {
                float lift=Mathf.Clamp(Physics.gravity.magnitude*.9f+Mathf.Max(0,-body.linearVelocity.y-4.5f)*5f,0,38f);
                body.AddForce(Vector3.up*lift,ForceMode.Acceleration);
                Vector3 horizontal=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);
                body.AddForce(-horizontal*.65f,ForceMode.Acceleration);
            }
            if(UsingAxe && FindSurface(out var hit))
            {
                axeGroundedAt = Time.time;
                AxeContactPoint = hit.point;
                Vector3 down=Vector3.ProjectOnPlane(Vector3.down,hit.normal).normalized;
                float sliding=Vector3.Dot(body.linearVelocity,down);
                if(sliding>0) body.AddForce(-down*Mathf.Min(38f,sliding*12f+Physics.gravity.magnitude),ForceMode.Acceleration);
                body.AddForce(-hit.normal*2.5f,ForceMode.Acceleration);
            }
        }
        public DescentInventoryState Capture() => new DescentInventoryState { rope=items[0],axe=items[1],parachute=items[2],selected=selected,
            browsing=browsing,chuteRemaining=Mathf.Max(0,chuteUntil-Time.time),axeRemaining=Mathf.Max(0,axeUntil-Time.time) };
        public void Apply(DescentInventoryState state)
        {
            if(MountainAuthority.IsHost) return;
            items[0]=state.rope; items[1]=state.axe; items[2]=state.parachute; selected=Mathf.Clamp(state.selected,0,2); browsing=state.browsing;
            chuteUntil=Time.time+state.chuteRemaining; axeUntil=Time.time+state.axeRemaining;
        }
        public void StopUse() { chuteUntil=axeUntil=0; browsing=false; }
        private void EnsureVisuals()
        {
            if(pack) return;
            cloth=new Material(Shader.Find("Standard")) { color=new Color(.1f,.8f,.85f) };
            metal=new Material(Shader.Find("Standard")) { color=new Color(.9f,.64f,.15f) };
            strap=new Material(Shader.Find("Sprites/Default"));
            steel=new Material(Shader.Find("Standard")) { color=new Color(.75f,.82f,.86f) };
            steel.SetFloat("_Metallic",.8f); steel.SetFloat("_Glossiness",.55f);
            rubber=new Material(Shader.Find("Standard")) { color=new Color(.055f,.07f,.085f) };
            pack=Piece("Supplies backpack",PrimitiveType.Cube,new Vector3(.5f,.3f,.4f),cloth);
            axe=GoatClimbingEquipment.CreateAxe(steel,rubber);
            wrist=GoatClimbingEquipment.Line(axe,"Hoof safety loop",strap,.025f,new Color(.08f,.12f,.14f),25);
            harnessRoot=new GameObject("Fitted goat climbing harness").transform;
            belts=new LineRenderer[4];
            for(int i=0;i<4;i++) belts[i]=GoatClimbingEquipment.Line(harnessRoot,"Woven harness strap "+i,strap,.065f,new Color(1f,.42f,.1f),i<2 ? 41 : 2);
            clip=GoatClimbingEquipment.Line(harnessRoot,"Harness locking carabiner",strap,.032f,new Color(1f,.8f,.24f),25);
            canopy=new GameObject("Goat parachute").transform;
            var mesh=new Mesh(); var vertices=new Vector3[26]; var triangles=new int[72];
            vertices[0]=Vector3.up*.8f;
            for(int i=0;i<12;i++) { float angle=i*Mathf.PI/6; vertices[i+1]=new Vector3(Mathf.Cos(angle)*1.7f,0,Mathf.Sin(angle)*1.7f);
                triangles[i*3]=0; triangles[i*3+1]=(i+1)%12+1; triangles[i*3+2]=i+1; }
            for(int i=0;i<13;i++) vertices[i+13]=vertices[i];
            for(int i=0;i<12;i++) { triangles[36+i*3]=13; triangles[36+i*3+1]=i+14; triangles[36+i*3+2]=(i+1)%12+14; }
            mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals();
            canopy.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            canopy.gameObject.AddComponent<MeshRenderer>().sharedMaterial=cloth;
            suspension=new LineRenderer[4];
            for(int i=0;i<4;i++) { var line=new GameObject("Parachute cord").AddComponent<LineRenderer>(); line.transform.SetParent(canopy,false);
                line.sharedMaterial=strap; line.positionCount=2; line.widthMultiplier=.025f; line.startColor=line.endColor=Color.white; suspension[i]=line; }
        }
        private Transform Piece(string name,PrimitiveType type,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=name; obj.transform.localScale=scale; Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial=material; return obj.transform;
        }
        private void LateUpdate()
        {
            bool anchored=GetComponent<GoatDescentTricks>()?.Anchored == true;
            if(items[0]+items[1]+items[2]==0 && !Parachuting && !UsingAxe && !anchored && !pack) return;
            EnsureVisuals(); Vector3 harness=rig.HarnessPosition;
            pack.gameObject.SetActive(items[0]+items[1]+items[2]>0);
            pack.SetPositionAndRotation(harness+Vector3.up*.4f,transform.rotation);
            Vector3 axis=(rig.HarnessChest-harness).normalized;
            if(axis.sqrMagnitude<.1f) axis=transform.forward;
            Vector3 right=Vector3.Cross(Vector3.up,axis).normalized;
            if(right.sqrMagnitude<.1f) right=transform.right;
            Vector3 up=Vector3.Cross(axis,right).normalized;
            harnessRoot.gameObject.SetActive(anchored || Parachuting || items[0]>0 || items[2]>0);
            Vector3 rear=harness+axis*.12f, front=rig.HarnessChest-axis*.12f;
            GoatClimbingEquipment.Loop(belts[0],rear,right,up,.43f,.34f);
            GoatClimbingEquipment.Loop(belts[1],front,right,up,.39f,.3f);
            for(int i=0;i<2;i++) { float side=i==0 ? -1 : 1; belts[i+2].SetPosition(0,rear+right*side*.43f); belts[i+2].SetPosition(1,front+right*side*.39f); }
            SafetyClipPosition=rear+right*.46f+up*.015f;
            GoatClimbingEquipment.Loop(clip,SafetyClipPosition,axis,up,.08f,.12f);
            axe.gameObject.SetActive(UsingAxe);
            if(UsingAxe)
            {
                rig.TryGetRightHoof(out var hoof,out var hoofRotation);
                Vector3 target=hoof+transform.forward*.45f+Vector3.down*.2f;
                if(FindSurface(out var surface))
                {
                    // Aim at the surface beneath the raised hoof rather than
                    // the ground detector's point underneath the body capsule.
                    if(surface.collider.Raycast(new Ray(hoof+surface.normal*.1f,-surface.normal),out var contact,2f))
                        target=contact.point+contact.normal*.025f;
                    else target=surface.point+surface.normal*.025f;
                }
                Vector3 aim=target-hoof;
                if(aim.sqrMagnitude<.01f) aim=transform.forward;
                axe.SetPositionAndRotation(hoof,Quaternion.FromToRotation(new Vector3(.42f,.3f,.11f),aim));
                axe.localScale=Vector3.one*Mathf.Clamp(aim.magnitude/.528f,.85f,1.25f);
                GoatClimbingEquipment.Loop(wrist,hoof,hoofRotation*Vector3.right,hoofRotation*Vector3.forward,.075f,.095f);
            }
            canopy.gameObject.SetActive(Parachuting);
            if(Parachuting)
            {
                canopy.position=harness+Vector3.up*3.3f+new Vector3(Mathf.Sin(Time.time*2)*.15f,0,0);
                for(int i=0;i<4;i++) { float angle=i*Mathf.PI*.5f; suspension[i].SetPosition(0,canopy.position+new Vector3(Mathf.Cos(angle)*1.6f,0,Mathf.Sin(angle)*1.6f)); suspension[i].SetPosition(1,harness); }
            }
        }
        private void OnGUI()
        {
            if(GoatSkinMenu.IsOpen) return;
            if(!GoatLocalControl.AllowsInput(this)) return;
            if(browsing) GUI.Box(new Rect(25,330,820,40),$"ПРИПАСЫ · страховка [{DescentSupplyChest.Current?.Stock(0)}] · ледоруб [{DescentSupplyChest.Current?.Stock(1)}] · парашют [{DescentSupplyChest.Current?.Stock(2)}]");
            else if(Time.time<messageUntil) GUI.Box(new Rect(25,330,650,30),message);
        }
        private void OnDisable()=>StopUse();
        private void OnDestroy()
        {
            if(pack) Destroy(pack.gameObject);
            if(axe)
            {
                foreach(var filter in axe.GetComponentsInChildren<MeshFilter>())
                    if(filter.sharedMesh && filter.sharedMesh.name=="Goat forged ice axe head") Destroy(filter.sharedMesh);
                Destroy(axe.gameObject);
            }
            if(harnessRoot) Destroy(harnessRoot.gameObject);
            if(canopy) { var mesh=canopy.GetComponent<MeshFilter>().sharedMesh; Destroy(mesh); Destroy(canopy.gameObject); }
            if(cloth) Destroy(cloth); if(metal) Destroy(metal); if(strap) Destroy(strap);
            if(steel) Destroy(steel); if(rubber) Destroy(rubber);
        }
    }
}
