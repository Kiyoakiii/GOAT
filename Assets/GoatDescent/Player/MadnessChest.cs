using UnityEngine;

namespace GoatDescent
{
    public sealed class MadnessChest : MonoBehaviour
    {
        public static MadnessChest Current { get; private set; }
        private Transform lid;
        private float readyAt;
        private Texture2D vignette;
        public bool CanOpen(GoatController goat) => goat && Time.time >= readyAt
            && Vector3.Distance(goat.transform.position, transform.position) < 2.7f
            && goat.GetComponent<GoatDescentTricks>()?.Busy != true;

        public static void Ensure(GoatController goat)
        {
            if (Current || !goat) return;
            Vector3 spawn = goat.transform.position;
            Vector3 best = spawn + Vector3.forward * 3f;
            Vector3 normal = Vector3.up;
            bool found = false;
            var teammate = LocalGoatPair.Instance?.Secondary;
            Vector3 away = teammate ? Vector3.ProjectOnPlane(spawn-teammate.transform.position,Vector3.up).normalized : Vector3.back;
            if(away.sqrMagnitude<.1f) away=Vector3.back;
            for (int radius=3;radius<=7 && !found;radius++)
            for (int angle=0;angle<12 && !found;angle++)
            {
                float a=(angle%2==0 ? angle/2 : -(angle+1)/2)*30f;
                Vector3 point=spawn+Quaternion.AngleAxis(a,Vector3.up)*away*radius;
                foreach(var hit in Physics.RaycastAll(point+Vector3.up*8f,Vector3.down,20f,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.GetComponentInParent<GoatController>() || hit.collider.GetComponentInParent<GoatPhysicsPart>()
                        || Vector3.Angle(hit.normal,Vector3.up)>50f) continue;
                    best=hit.point+hit.normal*.04f; normal=hit.normal; found=true; break;
                }
            }
            if (!found) return;
            var chest=new GameObject("Suspicious mountain chest");
            chest.transform.SetPositionAndRotation(best,Quaternion.FromToRotation(Vector3.up,normal));
            chest.AddComponent<MadnessChest>();
        }
        private void Awake()
        {
            Current=this;
            Build();
        }
        private Material Material(Color color)
        {
            var material=new Material(Shader.Find("Standard"));
            material.color=color; material.SetFloat("_Glossiness",.4f);
            return material;
        }
        private void Piece(string label,Transform parent,Vector3 center,Vector3 size,Material material,bool solid=false)
        {
            var piece=GameObject.CreatePrimitive(PrimitiveType.Cube); piece.name=label;
            piece.transform.SetParent(parent,false); piece.transform.localPosition=center; piece.transform.localScale=size;
            piece.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid) Destroy(piece.GetComponent<Collider>());
        }
        private void Build()
        {
            var wood=Material(new Color(.16f,.045f,.26f));
            var gold=Material(new Color(.92f,.62f,.15f)); gold.SetFloat("_Metallic",.6f);
            var gem=Material(new Color(.1f,.95f,.7f)); gem.EnableKeyword("_EMISSION"); gem.SetColor("_EmissionColor",new Color(.03f,.5f,.3f));
            Piece("Purple wooden chest",transform,new Vector3(0,.35f,0),new Vector3(1.35f,.7f,.9f),wood,true);
            foreach(float x in new[]{-.56f,.56f})
                Piece("Brass binding",transform,new Vector3(x,.36f,0),new Vector3(.08f,.74f,.94f),gold);
            Piece("Gold lock",transform,new Vector3(0,.49f,.47f),new Vector3(.22f,.28f,.07f),gold);
            Piece("Emerald keyhole",transform,new Vector3(0,.5f,.515f),new Vector3(.08f,.11f,.025f),gem);
            lid=new GameObject("Lid hinge").transform; lid.SetParent(transform,false); lid.localPosition=new Vector3(0,.7f,-.45f);
            Piece("Lid",lid,new Vector3(0,.06f,.45f),new Vector3(1.4f,.16f,.98f),wood);
            foreach(float x in new[]{-.56f,.56f})
                Piece("Lid brass binding",lid,new Vector3(x,.15f,.45f),new Vector3(.08f,.04f,.99f),gold);
        }
        public bool TryOpen(GoatController goat)
        {
            if(!MountainAuthority.IsHost || !CanOpen(goat)) return false;
            var pair=LocalGoatPair.Instance;
            var teammate=pair ? (goat==pair.Primary ? pair.Secondary : pair.Primary) : null;
            Vector3 direction=teammate ? teammate.transform.position-goat.transform.position : goat.transform.position-transform.position;
            Vector3 away=Vector3.ProjectOnPlane(goat.transform.position-transform.position,Vector3.up).normalized;
            if(Vector3.Dot(Vector3.ProjectOnPlane(direction,Vector3.up).normalized,away)<.2f) direction=away;
            if(!goat.GetComponent<GoatDescentTricks>().StartMadness(direction)) return false;
            readyAt=Time.time+20f;
            ComicImpactRing.Create(transform.position+Vector3.up*.65f,new Color(.7f,.18f,1f),1.3f);
            Debug.Log("MADNESS_CHEST_OPEN goat="+goat.name);
            return true;
        }
        public ChestState Capture() => new ChestState { exists=true,position=transform.position,rotation=transform.rotation,cooldown=Mathf.Max(0,readyAt-Time.time) };
        public static void Apply(ChestState state)
        {
            if(!state.exists) return;
            if(!Current) new GameObject("Suspicious mountain chest").AddComponent<MadnessChest>();
            Current.transform.SetPositionAndRotation(state.position,state.rotation);
            Current.readyAt=Time.time+state.cooldown;
        }
        private void Update()
        {
            bool open=Time.time<readyAt && readyAt-Time.time>14f;
            lid.localRotation=Quaternion.Slerp(lid.localRotation,Quaternion.Euler(open ? -110f : 0f,0,0),1f-Mathf.Exp(-8f*Time.deltaTime));
        }
        private void OnGUI()
        {
            if(GoatSkinMenu.IsOpen || GoatSettingsMenu.IsOpen) return;
            var viewer=LocalGoatPair.Instance?.Active;
            if(!viewer) return;
            var pair=LocalGoatPair.Instance;
            var other=viewer==pair.Primary ? pair.Secondary : pair.Primary;
            if(!other || other.GetComponent<GoatDescentTricks>()?.MadnessActive != true) return;
            float distance=Vector3.Distance(viewer.transform.position,other.transform.position);
            float strength=Mathf.Clamp01(1f-distance/14f);
            if(strength<=0) return;
            if(!vignette)
            {
                vignette=new Texture2D(64,1,TextureFormat.RGBA32,false);
                for(int x=0;x<64;x++) vignette.SetPixel(x,0,new Color(.7f,.04f,.85f,Mathf.Pow(1f-x/63f,2f)));
                vignette.Apply();
            }
            Color previous=GUI.color;
            GUI.color=new Color(1,1,1,strength*(.45f+Mathf.Sin(Time.time*9f)*.1f));
            float width=Screen.width*.18f;
            GUI.DrawTexture(new Rect(0,0,width,Screen.height),vignette);
            GUI.DrawTextureWithTexCoords(new Rect(Screen.width-width,0,width,Screen.height),vignette,new Rect(1,0,-1,1));
            GUI.color=Color.white;
            GUI.Box(new Rect(Screen.width*.5f-180,Screen.height*.2f,360,38),"БЕЗУМИЕ БЛИЗКО! НАПАРНИК ОТКРЫЛ СУНДУК!");
            GUI.color=previous;
        }
        private void OnDestroy() { if(Current==this) Current=null; if(vignette) Destroy(vignette); }
    }
}
