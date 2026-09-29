Shader "GoatDescent/Valley Foliage"
{
    Properties { _Color ("Canopy tint", Color) = (.34,.45,.19,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float3 worldPos; float4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            float freckles=frac(sin(dot(floor(p*2.7),float3(12.9898,78.233,38.719)))*43758.5453);
            o.Albedo=IN.color.rgb*_Color.rgb*1.8*(.86+.21*freckles);
            o.Metallic=0; o.Smoothness=.035; o.Occlusion=.86;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
