using UnityEngine;

namespace GoatDescent
{
    public enum GoatToiletKind:byte {None,Pee,Poop}
    [System.Serializable]
    public struct GoatToiletState {public byte kind;public float elapsed;}

    [DefaultExecutionOrder(150)]
    public sealed class GoatToiletAction:MonoBehaviour
    {
        private GoatController motor;
        private GoatVisualController visual;
        private GoatPhysicalBody physical;
        private float started,duration,nextPellet,nextDroplet;
        private GoatToiletKind kind;
        private LineRenderer stream;
        private ParticleSystem pellets,droplets;
        private GameObject effects;
        private readonly Vector3[] streamPoints=new Vector3[24];
        public bool Active => kind!=GoatToiletKind.None;
        public float Elapsed => Active ? Mathf.Max(0f,Time.time-started) : 0f;
        public float Crouch01 => kind==GoatToiletKind.Poop ? PoseBlend : 0f;
        private float PoseBlend => Mathf.Min(Mathf.SmoothStep(0,1,Elapsed/.75f),Mathf.SmoothStep(0,1,(duration-Elapsed)/.7f));
        public bool TryStart(GoatToiletKind action)
        {
            if(action!=GoatToiletKind.Pee && action!=GoatToiletKind.Poop) return false;
            motor ??= GetComponent<GoatController>();visual ??= GetComponent<GoatVisualController>();
            if(!MountainAuthority.IsHost || Active || GetComponent<GoatWaterDrinking>()?.Active == true || !motor || !motor.Grounded || motor.IsPredatorCarried
                || motor.Velocity.magnitude>1.7f || GetComponent<GoatGroundDetector>().SlopeAngle>55f
                || GetComponent<RespawnController>()?.IsDead==true || GetComponent<GoatInteraction>()?.IsBusy==true
                || GetComponent<GoatComedyActions>()?.Busy==true || GetComponent<GoatComedyActions>()?.HasPassenger==true
                || GetComponent<GoatDescentTricks>()?.Busy==true || GetComponent<GoatPunchBuff>()?.PerformingPair==true) return false;
            string clip=action==GoatToiletKind.Pee ? "Goat_Pee" : "Goat_Poop";
            var animation=GetComponentInChildren<Animation>();
            if(!animation || animation[clip]==null) return false;
            kind=action;started=Time.time;duration=animation[clip].length;
            nextPellet=.95f;nextDroplet=.8f;
            motor.ClearInput();GetComponent<GoatJumpController>()?.ResetInput();
            visual.PlayInteraction(clip,duration,visual.Facing);
            return true;
        }
        private void Update()
        {
            if(!Active || !MountainAuthority.IsHost) return;
            motor ??= GetComponent<GoatController>();
            if(GetComponent<RespawnController>()?.IsDead==true || !motor.Grounded || motor.IsPredatorCarried
                || GetComponent<GoatInteraction>()?.IsBusy==true || GetComponent<GoatComedyActions>()?.Busy==true
                || GetComponent<GoatDescentTricks>()?.Busy==true || GetComponent<GoatPunchBuff>()?.PerformingPair==true
                || GetComponent<GoatSlopeBalance>()?.IsSlipping==true) {Cancel();return;}
            if(Elapsed>=duration)
            {
                bool completedPoop=kind==GoatToiletKind.Poop;
                Cancel();
                if(completedPoop)
                {
                    GetComponent<GoatHealth>()?.Heal(10f);
                    GetComponent<GoatJumpController>()?.AnnounceTrick("ОБЛЕГЧЕНИЕ! +10 HP");
                }
            }
        }
        public void Cancel()
        {
            kind=GoatToiletKind.None;
            if(stream) stream.enabled=false;
            GetComponent<GoatVisualController>()?.EndToiletPose();
        }
        public GoatToiletState Capture() => new GoatToiletState {kind=(byte)kind,elapsed=Elapsed};
        public void Apply(GoatToiletState state)
        {
            if(MountainAuthority.IsHost) return;
            var incoming=(GoatToiletKind)state.kind;
            if(incoming!=kind) {nextPellet=state.elapsed+.08f;nextDroplet=state.elapsed;}
            kind=incoming;started=Time.time-Mathf.Max(0f,state.elapsed);
            duration=kind==GoatToiletKind.Poop ? 4f : 3.5f;
            if(!Active && stream) stream.enabled=false;
        }
        private void PrepareEffects()
        {
            if(effects) return;
            effects=new GameObject(name+" toilet effects");effects.transform.SetParent(transform,false);
            var line=new GameObject("Urine stream");line.transform.SetParent(effects.transform,false);
            stream=line.AddComponent<LineRenderer>();stream.useWorldSpace=true;stream.numCapVertices=3;
            stream.numCornerVertices=2;stream.widthMultiplier=.026f;
            stream.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(1,.45f));
            stream.sharedMaterial=Resources.Load<Material>("GoatUrine");
            stream.startColor=new Color(1,.84f,.1f,.88f);stream.endColor=new Color(1,.86f,.22f,.5f);
            stream.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;stream.enabled=false;
            pellets=Particles("Falling goat droppings",true);
            droplets=Particles("Urine droplets",false);
        }
        private ParticleSystem Particles(string label,bool poop)
        {
            var node=new GameObject(label);node.transform.SetParent(effects.transform,false);
            var system=node.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=false;main.playOnAwake=false;main.duration=1;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=poop ? 48 : 160;
            main.startLifetime=poop ? 12f : 1.25f;main.startSpeed=0;main.startSize=poop ? .105f : .027f;
            main.gravityModifier=1f;main.startColor=poop ? new Color(.23f,.105f,.025f) : new Color(1,.84f,.15f,.75f);
            var emission=system.emission;emission.enabled=false;
            var shape=system.shape;shape.enabled=false;
            var collision=system.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;
            collision.mode=ParticleSystemCollisionMode.Collision3D;collision.collidesWith=~LayerMask.GetMask("Player");
            collision.bounce=poop ? .14f : 0f;collision.dampen=poop ? .72f : 1f;
            collision.lifetimeLoss=poop ? 0f : 1f;collision.enableDynamicColliders=false;
            collision.colliderForce=0;collision.quality=ParticleSystemCollisionQuality.Medium;
            var renderer=system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=Resources.Load<Material>(poop ? "GoatDroppingMaterial" : "GoatUrine");
            if(poop) {renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=Resources.Load<Mesh>("GoatDropping");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
            var rotation=system.rotationOverLifetime;rotation.enabled=poop;rotation.z=.65f;
            var fade=system.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.8f),new GradientAlphaKey(0,1)});fade.color=gradient;
            system.Play();return system;
        }
        private void LateUpdate()
        {
            if(!Active) {if(stream) stream.enabled=false;return;}
            PrepareEffects();physical ??= GetComponent<GoatPhysicalBody>();
            Vector3 rear=physical ? physical.HarnessPosition : transform.position+Vector3.up*1.1f;
            Vector3 forward=physical ? (physical.HarnessChest-rear).normalized : transform.forward;
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            RefreshEffects(kind,Elapsed,duration,forward,side,
                physical ? physical.DroppingsPosition : rear-forward*.34f+Vector3.down*.17f,
                physical ? physical.UrinePosition : rear+forward*.16f+Vector3.down*.28f+side*.12f);
        }
        private void RefreshEffects(GoatToiletKind action,float elapsed,float actionDuration,Vector3 forward,Vector3 side,Vector3 droppingsOrigin,Vector3 urineOrigin)
        {
            if(action==GoatToiletKind.Poop)
            {
                stream.enabled=false;
                if(elapsed>=nextPellet && elapsed<actionDuration-.8f)
                {
                    var particle=new ParticleSystem.EmitParams {
                        position=droppingsOrigin,
                        velocity=-forward*.12f+Vector3.down*.2f+side*Mathf.Sin(elapsed*11f)*.035f,
                        startSize=.095f+Mathf.Sin(elapsed*17f)*.012f,rotation=elapsed*60f};
                    pellets.Emit(particle,1);nextPellet=elapsed+.26f;
                }
                return;
            }
            bool flowing=elapsed>.75f && elapsed<actionDuration-.65f;
            stream.enabled=flowing;if(!flowing) return;
            Vector3 origin=urineOrigin;
            Vector3 velocity=side*.28f+forward*.13f+Vector3.down*.65f;
            int count=1;streamPoints[0]=origin;
            for(int i=1;i<streamPoints.Length;i++)
            {
                float t=i*.025f;
                Vector3 point=origin+velocity*t+Physics.gravity*t*t*.5f;
                Vector3 span=point-streamPoints[count-1];
                if(Physics.Raycast(streamPoints[count-1],span.normalized,out var hit,span.magnitude,~LayerMask.GetMask("Player"),QueryTriggerInteraction.Ignore))
                {
                    BeerPuddle.ReceiveUrineSegment(streamPoints[count-1],hit.point);
                    streamPoints[count++]=hit.point;break;
                }
                BeerPuddle.ReceiveUrineSegment(streamPoints[count-1],point);
                streamPoints[count++]=point;
            }
            stream.positionCount=count;for(int i=0;i<count;i++) stream.SetPosition(i,streamPoints[i]);
            if(elapsed>=nextDroplet)
            {
                droplets.Emit(new ParticleSystem.EmitParams {position=origin,velocity=velocity,startSize=.023f},2);
                nextDroplet=elapsed+.06f;
            }
        }
#if UNITY_EDITOR
        // Sample the artwork in a preview scene without starting a gameplay action.
        public void RenderArtPreview(GoatToiletKind action,float time,Vector3 rear,Vector3 chest)
        {
            PrepareEffects();nextPellet=.95f;nextDroplet=.8f;
            Vector3 forward=(chest-rear).normalized;Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            Vector3 poop=rear-forward*.34f+Vector3.down*.17f,pee=rear+forward*.16f+Vector3.down*.28f+side*.12f;
            var skin=GetComponent<GoatSkin>();var attachments=skin ? skin.BodyAttachments : null;
            if(attachments)
            {
                skin.TryGetAttachment("Pelvis",attachments.droppingsOffset,out poop);
                skin.TryGetAttachment("Pelvis",attachments.urineOffset,out pee);
            }
            for(float sample=0;sample<=time;sample+=.05f)
            {
                RefreshEffects(action,sample,action==GoatToiletKind.Poop ? 4f : 3.5f,forward,side,poop,pee);
                pellets.Simulate(.05f,false,false,true);droplets.Simulate(.05f,false,false,true);
            }
        }
#endif
        private void OnDisable() {if(MountainAuthority.IsHost) Cancel();}
    }
}
