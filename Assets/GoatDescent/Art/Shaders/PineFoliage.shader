Shader "GoatDescent/PineFoliage"
{
    Properties
    {
        _Color ("Vertex Color Tint", Color) = (1, 1, 1, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.16
        _FoliageGlow ("Shaded Foliage Lift", Range(0, 0.4)) = 0.20
        _FrostAmount ("Alpine Frost Amount", Range(0, 1)) = 0
        _FrostTint ("Alpine Frost Tint", Color) = (0.78, 0.88, 0.95, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _FrostTint;
        half _Glossiness;
        half _FoliageGlow;
        half _FrostAmount;

        struct Input
        {
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard output)
        {
            float3 foliage = max(IN.color.rgb, 0.0) * _Color.rgb;
            float frost = saturate(IN.color.a * _FrostAmount);
            output.Albedo = lerp(foliage, _FrostTint.rgb, frost);
            output.Emission = output.Albedo * _FoliageGlow;
            output.Metallic = 0.0;
            output.Smoothness = _Glossiness;
            output.Occlusion = 1.0;
            output.Alpha = 1.0;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
