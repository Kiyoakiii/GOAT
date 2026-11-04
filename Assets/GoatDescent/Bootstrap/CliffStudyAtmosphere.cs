using UnityEngine;

namespace GoatDescent
{
    public sealed class CliffStudyAtmosphere : MonoBehaviour
    {
        private Transform goat;
        private GoatController controller;
        private Rigidbody body;
        private AudioSource wind;
        private AudioSource steps;
        private AudioClip impact;
        private ParticleSystem dust;
        private Material dustMaterial;
        private AudioClip windClip;
        private Texture2D dustTexture;
        public int LandingPuffs { get; private set; }
        public int Footsteps { get; private set; }
        public bool WindPlaying => wind && wind.isPlaying;
        private bool grounded;
        private float lastStep;
        private float fallSpeed;

        private void Start()
        {
            wind=gameObject.AddComponent<AudioSource>();
            windClip=Noise("Cliff wind",4,.045f,true);
            wind.clip=windClip;wind.loop=true;wind.volume=.45f;wind.Play();
            steps=gameObject.AddComponent<AudioSource>();
            impact=Noise("Stone hooves",.16f,.3f,false);
            var obj=new GameObject("Landing dust");obj.transform.SetParent(transform);
            dust=obj.AddComponent<ParticleSystem>();
            dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=dust.main;main.playOnAwake=false;main.loop=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.05f,.16f);main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,1.1f);main.gravityModifier=.2f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.65f,.56f,.39f,.32f);
            var emission=dust.emission;emission.enabled=false;
            var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=65;shape.radius=.25f;
            var color=dust.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.35f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            dustMaterial=new Material(Shader.Find("Particles/Standard Unlit"));
            dustTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft limestone dust",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float radius=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;
                pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2));
            }
            dustTexture.SetPixels(pixels);dustTexture.Apply();dustMaterial.mainTexture=dustTexture;
            dustMaterial.SetFloat("_Mode",2);dustMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);dustMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);dustMaterial.SetInt("_ZWrite",0);
            dustMaterial.EnableKeyword("_ALPHABLEND_ON");dustMaterial.SetOverrideTag("RenderType","Transparent");dustMaterial.renderQueue=3000;
            dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=dustMaterial;
        }

        private static AudioClip Noise(string name,float seconds,float volume,bool smooth)
        {
            const int rate=22050;var data=new float[Mathf.RoundToInt(seconds*rate)];var random=new System.Random(917);float value=0;
            for(int i=0;i<data.Length;i++)
            {
                float sample=(float)random.NextDouble()*2-1;
                value=Mathf.Lerp(value,sample,smooth?.015f:.45f);
                float envelope=smooth?.7f+.3f*Mathf.Sin(i*2*Mathf.PI/data.Length):Mathf.Pow(1-i/(float)data.Length,3);
                data[i]=value*volume*envelope;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }

        private void Update()
        {
            if(!goat)
            {
                controller=FindFirstObjectByType<GoatController>();
                if(!controller)return;
                goat=controller.transform;body=controller.GetComponent<Rigidbody>();
            }
            if(!body)return;
            float speed=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude;
            bool support=controller.Grounded;
            if(support&&!grounded&&fallSpeed>1.5f)
            {
                LandingPuffs++;
                dust.transform.position=goat.position+Vector3.down*.3f;dust.transform.rotation=Quaternion.Euler(-90,0,0);dust.Emit(Mathf.Clamp(Mathf.RoundToInt(fallSpeed*2),8,28));
                steps.pitch=Random.Range(.75f,.95f);steps.PlayOneShot(impact,.65f);
            }
            if(support&&speed>.7f&&Time.time-lastStep>Mathf.Clamp(.55f/speed,.16f,.4f))
            {
                Footsteps++;lastStep=Time.time;steps.pitch=Random.Range(.9f,1.2f);steps.PlayOneShot(impact,.22f);
            }
            grounded=support;fallSpeed=Mathf.Max(0,-body.linearVelocity.y);
            wind.volume=.3f+.08f*Mathf.Sin(Time.time*.23f);
        }

        private void OnDestroy()
        {
            if(dustTexture)Destroy(dustTexture);if(windClip)Destroy(windClip);if(impact)Destroy(impact);if(dustMaterial)Destroy(dustMaterial);
        }
    }
}
