Shader "Goat/Alpine Surface"
{
    Properties
    {
        _SnowColor ("Snow", Color) = (0.49,0.60,0.66,1)
        _StoneColor ("Stone", Color) = (0.27,0.33,0.39,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; float3 worldNormal; };
        fixed4 _SnowColor, _StoneColor;
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float coarse=noise(IN.worldPos.xz*.19);
            float grain=noise(IN.worldPos.xz*8.5+IN.worldPos.y*.3);
            float rockBands=sin(IN.worldPos.y*2.1+coarse*5.0)*.035;
            float cover=smoothstep(.48,.84,normalize(IN.worldNormal).y+(coarse-.5)*.19);
            float3 stone=_StoneColor.rgb*(.87+coarse*.22+rockBands);
            float3 snow=_SnowColor.rgb*(.96+coarse*.05+(grain-.5)*.025);
            o.Albedo=lerp(stone,snow,cover);
            o.Metallic=0;
            o.Smoothness=lerp(.08,.17,cover);
            o.Occlusion=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
