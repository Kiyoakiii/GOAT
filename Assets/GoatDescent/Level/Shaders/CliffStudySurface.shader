Shader "GoatDescent/CliffStudySurface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _DetailTex ("Stone grain", 2D) = "white" {}
        _DetailStrength ("Grain strength", Range(0,1)) = .12
        _DetailScale ("Grain scale", Float) = .3
        _Weathering ("Weathering", Range(0,1)) = 0
        _CoverTex ("Meadow grain", 2D) = "white" {}
        _GroundCover ("Ground vegetation", Float) = 0
        _Wind ("Wind movement", Float) = 0
        _Smoothness ("Smoothness", Range(0,1)) = .08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        sampler2D _DetailTex, _CoverTex;
        fixed4 _Color;
        float _DetailStrength, _DetailScale, _Smoothness, _Weathering, _Wind, _GroundCover;
        void vert(inout appdata_full v)
        {
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            float gust=sin(_Time.y*1.8+p.x*.6+p.z*.4)+.4*sin(_Time.y*3.2+p.y*1.3);
            v.vertex.xz+=float2(gust,gust*.35)*_Wind;
        }
        float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
        float noise(float3 p)
        {
            float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
        }
        struct Input { float3 worldPos; float3 worldNormal; float4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 w=pow(abs(normalize(IN.worldNormal)),4);w/=max(.001,w.x+w.y+w.z);
            float3 p=IN.worldPos*_DetailScale;
            float3 detail=tex2D(_DetailTex,p.yz).rgb*w.x+tex2D(_DetailTex,p.xz).rgb*w.y+tex2D(_DetailTex,p.xy).rgb*w.z;
            float grain=dot(detail,float3(.3,.59,.11));
            o.Albedo=_Color.rgb*IN.color.rgb*lerp(float3(1,1,1),detail,_DetailStrength);
            float macro=noise(IN.worldPos*.16);
            float chips=smoothstep(.6,.85,noise(IN.worldPos*2.8));
            float stratum=abs(frac(IN.worldPos.y*.27+IN.worldPos.x*.09+floor(noise(IN.worldPos*.22)*3)*.14)-.5);
            float seams=(1-smoothstep(.002,.012,stratum))*smoothstep(.35,.6,macro);
            float3 weathered=lerp(o.Albedo*.88,o.Albedo*1.08,macro);
            weathered=lerp(weathered,o.Albedo*1.1,chips*.12);
            weathered*=1-seams*.12;
            o.Albedo=lerp(o.Albedo,weathered,_Weathering);
            float groundMacro=noise(IN.worldPos*.13);
            float groundFine=noise(IN.worldPos*3.4);
            float coverGrain=dot(tex2D(_CoverTex,IN.worldPos.xz*.45).rgb,float3(.3,.59,.11));
            float3 meadow=_Color.rgb*IN.color.rgb*(.81+groundMacro*.3+groundFine*.13)*(.8+coverGrain*.45);
            meadow=lerp(meadow,meadow*float3(1.12,.96,.8),smoothstep(.45,.72,groundMacro)*.4);
            o.Albedo=lerp(o.Albedo,meadow,_GroundCover);
            o.Emission=o.Albedo*(1-_Weathering)*.06;
            o.Smoothness=_Smoothness;o.Occlusion=1;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
