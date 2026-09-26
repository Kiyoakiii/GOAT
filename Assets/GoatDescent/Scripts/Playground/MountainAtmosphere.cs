using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    public static class MountainAtmosphere
    {
        public static void Build(Transform root, MeshCollider mountain, MountainArt art)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.55f, .65f, .73f);
            RenderSettings.fogDensity = .0022f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.42f,.50f,.62f);
            RenderSettings.ambientEquatorColor = new Color(.29f,.35f,.43f);
            RenderSettings.ambientGroundColor = new Color(.15f,.19f,.24f);
            RenderSettings.reflectionIntensity = .25f;
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader)
            {
                var sky = art.Own(new Material(skyShader));
                sky.SetColor("_SkyTint",new Color(.50f,.57f,.67f));
                sky.SetColor("_GroundColor",new Color(.46f,.54f,.61f));
                sky.SetFloat("_AtmosphereThickness",.85f); sky.SetFloat("_Exposure",.9f); sky.SetFloat("_SunSize",.035f);
                RenderSettings.skybox=sky;
            }
            bool hasSun = false;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(light.type==LightType.Directional)
                {
                    if (hasSun) { light.enabled = false; continue; }
                    hasSun = true;
                    light.transform.rotation=Quaternion.Euler(33f,-38f,0f);
                    light.color=new Color(1f,.88f,.72f); light.intensity=.9f;
                    light.shadows=LightShadows.Soft; light.shadowStrength=.7f; light.shadowBias=.03f;
                    RenderSettings.sun=light;
                }
            QualitySettings.shadowDistance=100f;
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowResolution=ShadowResolution.High;
            QualitySettings.shadowCascades=2;
            QualitySettings.antiAliasing=4;
            var shader=Resources.Load<Shader>("AlpineSurface");
            if(shader)
            {
                var surface=art.Own(new Material(shader));
                var renderer=mountain.GetComponent<Renderer>();
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++) materials[i]=surface;
                renderer.sharedMaterials=materials;
            }
            var pine=art.Mat(new Color(.10f,.24f,.21f));
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                string name=renderer.name.ToLowerInvariant();
                if(name.Contains("pine crown"))renderer.sharedMaterial=pine;
                else if(name.Contains("pine trunk"))renderer.sharedMaterial=art.Timber;
                else if(name.Contains("mountain boulder"))renderer.sharedMaterial=art.Stone;
            }
            var backdrop = new GameObject("Distant alpine ridgelines — no collision").transform;
            backdrop.SetParent(root,false);
            for(int side=-1;side<=1;side+=2) for(int i=0;i<5;i++)
            {
                float z=-65f+i*95f;
                Peak(backdrop,art,new Vector3(side*(190f+i%2*30f),SteepMountain.BaseHeight(z)-100f,z),
                    new Vector3(190f,110f+(i%3)*18f,160f),i+side*7);
            }
            Peak(backdrop,art,new Vector3(0f,-55f,450f),new Vector3(260f,130f,190f),22);
        }
        private static void Peak(Transform parent, MountainArt art, Vector3 p, Vector3 size, int seed)
        {
            var rockV=new List<Vector3>();var rockT=new List<int>();
            var snowV=new List<Vector3>();var snowT=new List<int>();
            var ring=new Vector3[4,10]; float[] heights={0f,.28f,.65f,1f};float[] radius={1f,.68f,.30f,0f};
            for(int r=0;r<4;r++)for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*.2f;
                float uneven=1f+Mathf.Sin(i*8.4f+seed)*.18f;
                ring[r,i]=Vector3.Scale(new Vector3(Mathf.Cos(a)*radius[r]*uneven*.5f+heights[r]*.1f,
                    heights[r],Mathf.Sin(a)*radius[r]*uneven*.5f),size);
            }
            for(int r=0;r<3;r++)for(int i=0;i<10;i++)
                MountainArt.Quad(r==2?snowV:rockV,r==2?snowT:rockT,ring[r,i],ring[r+1,i],ring[r+1,(i+1)%10],ring[r,(i+1)%10]);
            art.MeshPart(parent,"Distant crag",art.MakeMesh("Alpine ridge",rockV,rockT),p,art.Stone);
            art.MeshPart(parent,"Distant snow summit",art.MakeMesh("Summit snow",snowV,snowT),p,art.Snow);
        }
    }
}
