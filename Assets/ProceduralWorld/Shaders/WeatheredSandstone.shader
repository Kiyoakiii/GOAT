Shader "GoatDescent/Weathered Sandstone"
{
    Properties
    {
        _StoneColor ("Warm sandstone", Color) = (.49,.43,.34,1)
        _ShadowStone ("Weathered seams", Color) = (.17,.205,.20,1)
        _MossColor ("Climbing moss", Color) = (.11,.20,.073,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _StoneColor, _ShadowStone, _MossColor;
        struct Input { float3 worldPos; float3 worldNormal; float4 color : COLOR; };
        float hash31(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
        float noise3(float3 p)
        {
            float3 i=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+1),f.x),f.y),f.z);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            float broad=noise3(p*.035), seam=noise3(p*float3(.22,.014,.22)), fine=noise3(p*.48);
            float layer=sin(p.y*.42+noise3(p*.025)*5), band=smoothstep(.74,.94,layer);
            float stain=smoothstep(.25,.69,seam);
            float3 stone=lerp(_ShadowStone.rgb,_StoneColor.rgb,stain*.72+.22);
            stone*=.8+broad*.35+fine*.17; stone*=1-band*.16;
            float mossField=noise3(p*.064)*.6+noise3(p*.19)*.23;
            float moss=smoothstep(.55,.76,mossField+max(0,IN.color.g-.55)*.22+max(0,IN.worldNormal.y)*.18);
            o.Albedo=lerp(stone,_MossColor.rgb*(.70+broad*.65),moss*.91);
            o.Metallic=0; o.Smoothness=.055; o.Occlusion=.78+.22*stain;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
