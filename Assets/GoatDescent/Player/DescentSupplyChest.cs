using UnityEngine;
namespace GoatDescent
{
    public sealed class DescentSupplyChest : MonoBehaviour
    {
        public static DescentSupplyChest Current { get; private set; }
        private readonly int[] stock = { 2, 2, 2 };
        private Transform lid;
        private float openUntil;
        public bool InReach(GoatController goat) => goat && Vector3.Distance(goat.transform.position,transform.position)<3f;
        public int Stock(int item) => item>=0 && item<3 ? stock[item] : 0;
        public static void Ensure(GoatController goat)
        {
            if(Current || !goat) return;
            Vector3 origin=goat.transform.position;
            for(int radius=3;radius<=7;radius++)
            for(int i=0;i<12;i++)
            {
                Vector3 point=origin+Quaternion.Euler(0,i*30,0)*Vector3.right*radius;
                RaycastHit best=default; float nearest=float.MaxValue;
                foreach(var hit in Physics.RaycastAll(point+Vector3.up*6,Vector3.down,14,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.GetComponentInParent<GoatController>() || hit.collider.GetComponentInParent<GoatPhysicsPart>()
                        || Vector3.Angle(hit.normal,Vector3.up)>40 || hit.distance>=nearest) continue;
                    best=hit; nearest=hit.distance;
                }
                if(!best.collider || (MadnessChest.Current && Vector3.Distance(best.point,MadnessChest.Current.transform.position)<2.5f)) continue;
                var chest=new GameObject("Descent supplies chest");
                chest.transform.SetPositionAndRotation(best.point+best.normal*.04f,Quaternion.FromToRotation(Vector3.up,best.normal));
                chest.AddComponent<DescentSupplyChest>(); return;
            }
            Debug.LogWarning("SUPPLY_CHEST_NO_SAFE_SPAWN");
        }
        private void Awake()
        {
            Current=this;
            var wood=new Material(Shader.Find("Standard")) { color=new Color(.28f,.14f,.06f) };
            var trim=new Material(Shader.Find("Standard")) { color=new Color(.05f,.75f,.8f) };
            Piece("Supplies",transform,new Vector3(0,.32f,0),new Vector3(1.3f,.64f,.85f),wood,true);
            foreach(float x in new[]{-.52f,.52f}) Piece("Turquoise strap",transform,new Vector3(x,.33f,0),new Vector3(.09f,.68f,.9f),trim);
            lid=new GameObject("Lid").transform; lid.SetParent(transform,false); lid.localPosition=new Vector3(0,.64f,-.42f);
            Piece("Cover",lid,new Vector3(0,.06f,.42f),new Vector3(1.36f,.14f,.9f),wood);
            Piece("Supply cross",transform,new Vector3(0,.36f,.45f),new Vector3(.32f,.09f,.025f),trim);
            Piece("Supply cross vertical",transform,new Vector3(0,.36f,.45f),new Vector3(.09f,.32f,.025f),trim);
        }
        private void Piece(string name,Transform parent,Vector3 point,Vector3 size,Material material,bool solid=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
            obj.transform.SetParent(parent,false); obj.transform.localPosition=point; obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material; if(!solid) Destroy(obj.GetComponent<Collider>());
        }
        public bool Take(GoatController goat,int item)
        {
            if(!MountainAuthority.IsHost || !InReach(goat) || item<0 || item>2 || stock[item]<=0) return false;
            if(!goat.GetComponent<GoatDescentInventory>().Give(item)) return false;
            stock[item]--; openUntil=Time.time+5f;
            ComicImpactRing.Create(transform.position+Vector3.up*.7f,new Color(.1f,.85f,.9f),.6f);
            return true;
        }
        public SupplyChestState Capture() => new SupplyChestState { exists=true,position=transform.position,rotation=transform.rotation,
            rope=stock[0],axe=stock[1],parachute=stock[2],openRemaining=Mathf.Max(0,openUntil-Time.time) };
        public static void Apply(SupplyChestState state)
        {
            if(!state.exists) return;
            if(!Current) new GameObject("Descent supplies chest").AddComponent<DescentSupplyChest>();
            Current.transform.SetPositionAndRotation(state.position,state.rotation);
            Current.stock[0]=state.rope; Current.stock[1]=state.axe; Current.stock[2]=state.parachute;
            Current.openUntil=Time.time+state.openRemaining;
        }
        private void Update() { lid.localRotation=Quaternion.Slerp(lid.localRotation,Quaternion.Euler(Time.time<openUntil ? -105 : 0,0,0),1-Mathf.Exp(-8*Time.deltaTime)); }
        private void OnDestroy()
        {
            if(Current==this) Current=null;
            foreach(var renderer in GetComponentsInChildren<Renderer>()) if(renderer.sharedMaterial) Destroy(renderer.sharedMaterial);
        }
    }
}
