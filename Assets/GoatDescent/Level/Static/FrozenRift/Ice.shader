Shader "GoatDescent/FrozenRiftIce"
{
    Properties
    {
        _Color ("Glacier blue", Color) = (.08, .55, .78, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        float4 _Color;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float wav = sin(p.y * .11 + p.z * .17) * .7;
            float crack = 1 - smoothstep(.015, .08, abs(sin(p.x * .34 + p.y * .08 + wav)));
            crack *= .45 + .55 * abs(sin(p.z * .17 + p.y * .13));
            float deep = .5 + .5 * sin(p.x * .06 + p.y * .035 + p.z * .07);
            o.Albedo = lerp(_Color.rgb * lerp(.7, 1.15, deep), float3(.7, .95, 1), crack * .7);
            o.Emission = _Color.rgb * .13 + float3(.12, .23, .26) * crack;
            o.Smoothness = .67;
            o.Metallic = .05;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
