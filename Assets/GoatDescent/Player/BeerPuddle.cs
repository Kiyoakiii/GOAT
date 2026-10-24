using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>A placeable puddle. All drinking/distance checks use its own local plane;
    /// it has no dependency on a terrain generator, scene name or biome.</summary>
    [DisallowMultipleComponent]
    public sealed class BeerPuddle : MonoBehaviour
    {
        [SerializeField] private BeerPuddleSettings settings;
        [SerializeField] private MeshRenderer waterSurface,foamSurface,wetBorder;
        [SerializeField] private bool purple;
        public bool IsContaminated => purple;
        public void ApplyDrink(GoatController goat)
        {
            if(!MountainAuthority.IsHost || !settings || !goat || goat.GetComponent<RespawnController>()?.IsDead==true) return;
            if(purple)
            {
                goat.GetComponent<GoatFurEffect>()?.ShedCoat();
                goat.GetComponent<GoatHealth>()?.Damage(settings.contaminatedDrinkDamage);
            }
            else
            {
                goat.GetComponent<GoatHealth>()?.HealFully();
                goat.GetComponent<GoatBeerEffect>()?.Clear();
                goat.GetComponent<GoatJumpController>()?.AnnounceTrick("ЦЕЛЕБНЫЙ ГЛОТОК! 100 HP");
            }
        }
        public static BeerPuddle FindDrinkable(GoatController goat, float reach)
        {
            BeerPuddle nearest = null; float distance = float.PositiveInfinity;
            foreach (var candidate in activePuddles)
            {
                if (!candidate || !candidate.CanDrink(goat, reach)) continue;
                float current = (candidate.transform.position - goat.transform.position).sqrMagnitude;
                if (current < distance) { nearest = candidate; distance = current; }
            }
            return nearest;
        }
        public bool CanDrink(GoatController goat, float reach)
        {
            if (!settings || !goat || !isActiveAndEnabled) return false;
            Vector3 local = transform.InverseTransformPoint(goat.transform.position);
            if (Mathf.Abs(Vector3.Dot(goat.transform.position - transform.position, transform.up)) > .8f) return false;
            float ellipse = Mathf.Sqrt(Ellipse(local));
            Vector3 nearest = new Vector3(local.x, settings.surfaceOffset, local.z);
            if (ellipse > 1) { nearest.x /= ellipse; nearest.z /= ellipse; }
            Vector3 water = transform.TransformPoint(nearest);
            return Vector3.ProjectOnPlane(water - goat.transform.position, transform.up).sqrMagnitude <= reach * reach;
        }
        private static readonly HashSet<BeerPuddle> activePuddles=new HashSet<BeerPuddle>();
        private MaterialPropertyBlock waterProperties;
        private Gradient aromaGradient;
        private readonly GradientColorKey[] aromaColors=new GradientColorKey[2];
        private readonly GradientAlphaKey[] aromaAlphas=new GradientAlphaKey[4];
        private float colorBlend;
        private sealed class Visitor
        {
            public GoatController Goat;
            public LineRenderer[] Lines;
            public Transform NoseBone;
        }
        private readonly Dictionary<GoatController,Visitor> visitors=new Dictionary<GoatController,Visitor>();
        private readonly List<GoatController> removed=new List<GoatController>();
        private readonly Vector3[] points=new Vector3[28];
        private LineRenderer[] sky;
        private Material aromaMaterial;
        private float nextScan;
        public BeerPuddleSettings Settings => settings;
        public bool IsPurple => purple;
        public void Configure(BeerPuddleSettings value) => settings=value;
        public void ConfigureVisuals(MeshRenderer water,MeshRenderer foam,MeshRenderer border)
        {waterSurface=water;foamSurface=foam;wetBorder=border;}
        private void OnEnable() => activePuddles.Add(this);
        private void OnDisable() => activePuddles.Remove(this);

        // The stream uses these same trajectory segments for its visible line.
        // Intersect the puddle's local water plane, including rotated/scaled instances.
        public static void ReceiveUrineSegment(Vector3 from,Vector3 to)
        {
            if(!MountainAuthority.IsHost) return;
            foreach(var puddle in activePuddles)
            {
                if(!puddle || !puddle.settings || puddle.purple) continue;
                Vector3 a=puddle.transform.InverseTransformPoint(from),b=puddle.transform.InverseTransformPoint(to);
                float plane=puddle.settings.surfaceOffset;
                if(a.y<plane || b.y>plane || a.y-b.y<.00001f) continue;
                Vector3 impact=Vector3.Lerp(a,b,(a.y-plane)/(a.y-b.y));
                if(puddle.Ellipse(impact)<=.94f) puddle.purple=true;
            }
        }
        public static PuddleState[] CaptureAll()
        {
            var states=new PuddleState[activePuddles.Count];int index=0;
            foreach(var puddle in activePuddles)
                states[index++]=new PuddleState {position=puddle.transform.position,rotation=puddle.transform.rotation,
                    scale=puddle.transform.lossyScale,purple=puddle.purple};
            return states;
        }
        public static void ApplyAll(PuddleState[] states)
        {
            if(MountainAuthority.IsHost || states==null) return;
            foreach(var state in states)
            {
                BeerPuddle target=null;
                foreach(var puddle in activePuddles)
                    if(puddle && (puddle.transform.position-state.position).sqrMagnitude<.0625f) {target=puddle;break;}
                if(!target)
                {
                    var prefab=Resources.Load<GameObject>("BeerPuddle");if(!prefab) continue;
                    target=Instantiate(prefab,state.position,state.rotation).GetComponent<BeerPuddle>();
                    target.transform.localScale=state.scale;
                }
                target.purple=state.purple;
            }
        }
        public static void EnsureNear(GoatController goat)
        {
            if(!goat || FindFirstObjectByType<BeerPuddle>()) return;
            var prefab=Resources.Load<GameObject>("BeerPuddle");
            if(!prefab) return;
            int mask=~LayerMask.GetMask("Player");
            var config=prefab.GetComponent<BeerPuddle>().settings;
            if(!config) config=Resources.Load<BeerPuddleSettings>("BeerPuddleSettings");
            if(!config) return;
            Physics.SyncTransforms();
            // Find a supported footprint rather than placing a puddle beyond
            // the ledge when the forward probe happens to be over open air.
            Vector2[] offsets={new Vector2(2.3f,1f),new Vector2(-2.3f,1f),new Vector2(0,3f),
                new Vector2(1.5f,-1.2f),new Vector2(-1.5f,-1.2f),new Vector2(0,-1.8f),
                new Vector2(1.8f,0),new Vector2(-1.8f,0)};
            foreach(float heightTolerance in new[]{.45f,1.5f})
            foreach(float scale in new[]{1f,.72f,.5f})
            foreach(var offset in offsets)
            {
                Vector3 probe=goat.transform.position+goat.transform.right*offset.x+goat.transform.forward*offset.y+Vector3.up*8f;
                if(!Physics.Raycast(probe,Vector3.down,out var hit,16f,mask,QueryTriggerInteraction.Ignore)
                    || hit.normal.y<.7f || Mathf.Abs(hit.point.y-goat.transform.position.y)>heightTolerance) continue;
                Vector3 tangent=Vector3.ProjectOnPlane(goat.transform.forward,hit.normal).normalized;
                if(tangent.sqrMagnitude<.01f) tangent=Vector3.ProjectOnPlane(Vector3.forward,hit.normal).normalized;
                var rotation=Quaternion.LookRotation(tangent,hit.normal);
                if(!SupportedFootprint(hit.point,rotation,config.radii*scale,mask)) continue;
                var container=new GameObject("Puddle placement");container.SetActive(false);
                var puddle=Instantiate(prefab,container.transform);
                puddle.transform.SetPositionAndRotation(hit.point,rotation);
                puddle.transform.localScale=Vector3.one*scale;
                puddle.transform.SetParent(null,true);
                Destroy(container);
                return;
            }
        }
        private static bool SupportedFootprint(Vector3 centre,Quaternion rotation,Vector2 radii,int mask)
        {
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4f;
                Vector3 edge=centre+rotation*new Vector3(Mathf.Cos(angle)*radii.x,0,Mathf.Sin(angle)*radii.y);
                if(!Physics.Raycast(edge+Vector3.up*.6f,Vector3.down,out var support,1.2f,mask,QueryTriggerInteraction.Ignore)
                    || support.normal.y<.7f || Mathf.Abs(Vector3.Dot(support.point-edge,rotation*Vector3.up))>.15f) return false;
            }
            return true;
        }
        private void Awake()
        {
            if(!settings) settings=Resources.Load<BeerPuddleSettings>("BeerPuddleSettings");
            if(!settings) {enabled=false;return;}
            // A script reload keeps the live scene but loses the cached line references.
            // Replace only this puddle's generated aroma groups when rebuilding them.
            foreach(Transform child in transform)
                if(child.name=="Healing water aroma" || child.name.StartsWith("Aroma to "))
                {
                    var line=child.GetComponentInChildren<LineRenderer>();
                    if(line && line.sharedMaterial && !aromaMaterial)
                        aromaMaterial=line.sharedMaterial;
                    if(Application.isPlaying) Destroy(child.gameObject);else DestroyImmediate(child.gameObject);
                }
            if(aromaMaterial)
            {
                if(Application.isPlaying) Destroy(aromaMaterial);else DestroyImmediate(aromaMaterial);
            }
            waterProperties ??= new MaterialPropertyBlock();
            aromaGradient ??= new Gradient();
            aromaMaterial=new Material(Shader.Find("GoatDescent/Beer Aroma"));
            aromaMaterial.name="Healing water aroma runtime";
            // Five for the default area; a larger scaled puddle emits more.
            float area=Mathf.PI*settings.radii.x*settings.radii.y*Mathf.Abs(transform.lossyScale.x*transform.lossyScale.z);
            int count=Mathf.Clamp(Mathf.RoundToInt(settings.skyLines*area/(Mathf.PI*1.6f*1.15f)),2,20);
            colorBlend=purple ? 1f : 0f;
            UpdateColors();
            sky=MakeLines("Healing water aroma",count);
        }
        private void UpdateColors()
        {
            Color water=Color.Lerp(settings.healingColor,settings.purpleColor,colorBlend);
            Color foam=Color.Lerp(settings.foamColor,settings.purpleFoamColor,colorBlend);
            Color aroma=Color.Lerp(settings.aromaColor,settings.purpleAromaColor,colorBlend);
            SetWaterColor(waterSurface,water);
            SetWaterColor(foamSurface,foam);
            SetWaterColor(wetBorder,new Color(water.r*.4f,water.g*.4f,water.b*.4f,.2f));
            aromaColors[0]=new GradientColorKey(aroma,0f);
            aromaColors[1]=new GradientColorKey(Color.Lerp(aroma,Color.white,.65f),1f);
            aromaAlphas[0]=new GradientAlphaKey(0f,0f);
            aromaAlphas[1]=new GradientAlphaKey(aroma.a,.12f);
            aromaAlphas[2]=new GradientAlphaKey(aroma.a*.75f,.6f);
            aromaAlphas[3]=new GradientAlphaKey(0f,1f);
            aromaGradient.SetKeys(aromaColors,aromaAlphas);
            if(sky!=null) foreach(var line in sky) if(line) line.colorGradient=aromaGradient;
            foreach(var visitor in visitors.Values)
                foreach(var line in visitor.Lines) if(line) line.colorGradient=aromaGradient;
        }
        private void SetWaterColor(MeshRenderer renderer,Color color)
        {
            if(!renderer) return;
            renderer.GetPropertyBlock(waterProperties);waterProperties.SetColor("_Color",color);
            renderer.SetPropertyBlock(waterProperties);
        }
        private LineRenderer[] MakeLines(string label,int count)
        {
            var result=new LineRenderer[count];
            var group=new GameObject(label);group.transform.SetParent(transform,false);
            for(int i=0;i<count;i++)
            {
                var node=new GameObject("Aroma "+(i+1));node.transform.SetParent(group.transform,false);
                var line=node.AddComponent<LineRenderer>();
                line.sharedMaterial=aromaMaterial;line.useWorldSpace=true;line.positionCount=points.Length;
                line.numCornerVertices=3;line.numCapVertices=4;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                line.widthMultiplier=settings.aromaLineWidth;
                line.widthCurve=new AnimationCurve(new Keyframe(0f,1f),new Keyframe(.35f,.8f),new Keyframe(1f,.08f));
                line.colorGradient=aromaGradient;result[i]=line;
            }
            return result;
        }
        private void Scan()
        {
            foreach(var goat in FindObjectsByType<GoatController>(FindObjectsSortMode.None))
            {
                if(!goat || visitors.ContainsKey(goat)) continue;
                visitors.Add(goat,new Visitor {Goat=goat,Lines=MakeLines("Aroma to "+goat.name,settings.noseLines)});
            }
        }
        private float Ellipse(Vector3 local)
            => local.x*local.x/(settings.radii.x*settings.radii.x)+local.z*local.z/(settings.radii.y*settings.radii.y);
        private void Update()
        {
            if(!settings) return;
            if(sky==null) Awake();
            float nextBlend=Mathf.MoveTowards(colorBlend,purple ? 1f : 0f,Time.deltaTime/settings.colorChangeSeconds);
            if(nextBlend!=colorBlend) {colorBlend=nextBlend;UpdateColors();}
            if(Time.time>=nextScan) {Scan();nextScan=Time.time+.5f;}
            DrawSky();removed.Clear();
            foreach(var visitor in visitors.Values)
            {
                if(!visitor.Goat) {removed.Add(visitor.Goat);foreach(var line in visitor.Lines) if(line) Destroy(line.gameObject);continue;}
                DrawNose(visitor);
            }
            foreach(var goat in removed) visitors.Remove(goat);
        }
        private void DrawSky()
        {
            for(int i=0;i<sky.Length;i++)
            {
                float angle=i*2.39996f;
                float radius=.32f+.3f*(i%3)/2f;
                var origin=new Vector3(Mathf.Cos(angle)*settings.radii.x*radius,settings.surfaceOffset,Mathf.Sin(angle)*settings.radii.y*radius);
                for(int p=0;p<points.Length;p++)
                {
                    float t=p/(float)(points.Length-1),phase=Time.time*1.7f-t*8f+i*1.9f;
                    var offset=new Vector3(Mathf.Sin(phase)*.1f*t,t*settings.skyLineHeight,Mathf.Cos(phase*.85f)*.065f*t);
                    points[p]=transform.TransformPoint(origin+offset);
                }
                sky[i].SetPositions(points);
            }
        }
        private void DrawNose(Visitor visitor)
        {
            var goat=visitor.Goat;
            Vector3 local=transform.InverseTransformPoint(goat.transform.position);
            float horizontal=Mathf.Sqrt(local.x*local.x+local.z*local.z);
            float edge=horizontal>0f ? horizontal/Mathf.Sqrt(Mathf.Max(.01f,Ellipse(local))) : Mathf.Min(settings.radii.x,settings.radii.y);
            float worldDistance=Vector3.ProjectOnPlane(goat.transform.position-transform.position,transform.up).magnitude;
            float worldEdge=horizontal>0f ? edge*worldDistance/horizontal : edge;
            bool visible=worldDistance-worldEdge<=settings.nearbyNoseRadius
                && Mathf.Abs(Vector3.Dot(goat.transform.position-transform.position,transform.up))<2.4f
                && goat.GetComponent<RespawnController>()?.IsDead!=true;
            var physical=goat.GetComponent<GoatPhysicalBody>();
            if(!physical && !visitor.NoseBone)
                foreach(var bone in goat.GetComponentsInChildren<Transform>())
                    if(bone.name=="Head") {visitor.NoseBone=bone;break;}
            Vector3 nose=physical ? physical.NosePosition : visitor.NoseBone
                ? visitor.NoseBone.position+visitor.NoseBone.rotation*new Vector3(0f,-.015f,.34f) : goat.transform.position+Vector3.up*1.5f;
            var skin=goat.GetComponent<GoatSkin>();
            if(!physical && skin && skin.HeadAttachments)
                skin.TryGetAttachment("Head",skin.HeadAttachments.noseOffset,out nose);
            Vector3 toward=Vector3.ProjectOnPlane(nose-transform.position,transform.up).normalized;
            Vector3 side=Vector3.Cross(transform.up,toward).normalized;
            for(int i=0;i<visitor.Lines.Length;i++)
            {
                var line=visitor.Lines[i];line.enabled=visible;if(!visible) continue;
                float offset=(i-(visitor.Lines.Length-1)*.5f)*.18f;
                Vector3 from=transform.position+toward*(edge*.65f)+side*offset+transform.up*settings.surfaceOffset;
                for(int p=0;p<points.Length;p++)
                {
                    float t=p/(float)(points.Length-1),taper=1f-t;
                    float envelope=Mathf.Sin(t*Mathf.PI)*taper;
                    float phase=t*12f-Time.time*2.5f+i*2.1f;
                    points[p]=Vector3.Lerp(from,nose,t)
                        +transform.up*(Mathf.Sin(t*Mathf.PI)*.22f+Mathf.Sin(phase)*.12f*envelope)
                        +side*(Mathf.Cos(phase)*.11f*envelope+offset*taper*.35f);
                }
                line.SetPositions(points);
            }
        }
        private void OnDestroy()
        {
            if(!aromaMaterial) return;
            if(Application.isPlaying) Destroy(aromaMaterial);else DestroyImmediate(aromaMaterial);
        }
        public void RefreshAromaPreview(params GoatController[] goats)
        {
            if(sky==null) Awake();
            if(!settings) return;
            colorBlend=purple ? 1f : 0f;
            UpdateColors();
            foreach(var goat in goats) if(goat && !visitors.ContainsKey(goat))
                visitors.Add(goat,new Visitor {Goat=goat,Lines=MakeLines("Aroma to "+goat.name,settings.noseLines)});
            Scan();DrawSky();
            foreach(var visitor in visitors.Values) if(visitor.Goat) DrawNose(visitor);
        }
    }
}
