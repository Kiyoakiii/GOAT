Shader "GoatDescent/PineFoliage"
{
    Properties
    {
        _Color ("Vertex Color Tint", Color) = (1, 1, 1, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.16
        _FoliageGlow ("Shaded Foliage Lift", Range(0, 0.4)) = 0.20
        _FrostAmount ("Alpine Frost Amount", Range(0, 1)) = 0
        _FrostTint ("Alpine Frost Tint", Color) = (0.78, 0.88, 0.95, 1)
        _WindAmplitude ("Wind Bend", Range(0, 0.45)) = 0.20
        _WindHeight ("Wind Height Weight", Range(0, 2)) = 0.06
        _WindSpeed ("Wind Speed", Range(0, 4)) = 0.72
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:windVertex
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _FrostTint;
        half _Glossiness;
        half _FoliageGlow;
        half _FrostAmount;
        half _WindAmplitude;
        half _WindHeight;
        half _WindSpeed;

        void windVertex(inout appdata_full v)
        {
            float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
            float phase = _Time.y * _WindSpeed + worldPosition.x * 0.115 + worldPosition.z * 0.087;
            float gust = sin(phase) * 0.72 + sin(phase * 0.43 + worldPosition.z * 0.035) * 0.28;
            float heightWeight = saturate(max(v.vertex.y, 0.0) * _WindHeight);
            v.vertex.x += gust * _WindAmplitude * heightWeight;
            v.vertex.z += cos(phase * 0.71) * _WindAmplitude * 0.22 * heightWeight;
        }

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
