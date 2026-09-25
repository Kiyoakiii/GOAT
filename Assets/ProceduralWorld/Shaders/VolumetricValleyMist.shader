Shader "GoatDescent/Volumetric Valley Mist"
{
    Properties
    {
        _FogLight ("Cloud light",Color)=(.88,.92,.93,1)
        _FogShadow ("Cloud shadow",Color)=(.50,.61,.66,1)
        _Density ("Density per metre",Range(.001,.05))=.024
        _Top ("Cloud top height",Float)=225
        _NoiseScale ("Billow scale",Float)=.009
    }
    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" }
        Pass
        {
            Cull Front ZWrite Off ZTest Always
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _FogLight, _FogShadow;
            float _Density, _Top, _NoiseScale;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float4 screen:TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.screen=ComputeScreenPos(o.pos); return o; }
            float hash31(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
            float noise3(float3 p)
            {
                float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+1),f.x),f.y),f.z);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 origin=_WorldSpaceCameraPos, ray=normalize(i.world-origin);
                float3 ro=mul(unity_WorldToObject,float4(origin,1)).xyz;
                float3 rd=mul((float3x3)unity_WorldToObject,ray);
                rd=sign(rd+1e-9)*max(abs(rd),1e-7);
                float3 ta=(-.5-ro)/rd, tb=(.5-ro)/rd, lo=min(ta,tb),hi=max(ta,tb);
                float entry=max(0,max(lo.x,max(lo.y,lo.z))), leave=min(hi.x,min(hi.y,hi.z));
                float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
                float sceneDistance=LinearEyeDepth(raw)/max(.001,-mul((float3x3)UNITY_MATRIX_V,ray).z);
                leave=min(leave,sceneDistance);
                if(leave<=entry) return 0;
                float stepSize=(leave-entry)/72;
                float jitter=frac(sin(dot(floor(i.pos.xy),float2(12.9898,78.233)))*43758.5453);
                float t=entry+stepSize*(.4+jitter*.2), transmittance=1;
                float3 sum=0;
                [loop] for(int s=0;s<72;s++)
                {
                    float3 p=origin+ray*t;
                    float3 q=p*_NoiseScale+float3(_Time.y*.001,0,0);
                    float n=noise3(q)*.67+noise3(q*2.13+7.3)*.33;
                    float top=_Top+(noise3(float3(p.x*.005,4,p.z*.005))-.5)*175;
                    float heightMask=saturate((top-p.y)/65), bottomFill=saturate((135-p.y)/80);
                    float d=max(bottomFill*.8,saturate((n-.40)*3.8)*heightMask);
                    float3 local=mul(unity_WorldToObject,float4(p,1)).xyz;
                    float edge=saturate((.5-abs(local.x))*12)*saturate((.5-abs(local.z))*12);
                    float opacity=1-exp(-d*_Density*stepSize*edge);
                    float light=saturate(.72+(p.y-140)*.003+n*.15);
                    sum+=transmittance*opacity*lerp(_FogShadow.rgb,_FogLight.rgb,light);
                    transmittance*=1-opacity;
                    if(transmittance<.008) break;
                    t+=stepSize;
                }
                return float4(sum,1-transmittance);
            }
            ENDCG
        }
    }
}
