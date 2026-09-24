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
        _WindDirection ("Prevailing Wind Direction", Vector) = (0.82, 0, 0.57, 0)
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
        float4 _WindDirection;

        void windVertex(inout appdata_full v)
        {
            float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
            float2 windDirection = normalize(_WindDirection.xz);
            float2 crossDirection = float2(-windDirection.y, windDirection.x);
            float alongWind = dot(worldPosition.xz, windDirection);
            float acrossWind = dot(worldPosition.xz, crossDirection);
            float phase = _Time.y * _WindSpeed + alongWind * 0.075 + acrossWind * 0.023;
            float gustCycle = sin(_Time.y * _WindSpeed * 0.22 + alongWind * 0.016 - acrossWind * 0.011);
            float gustEnvelope = 0.76 + gustCycle * 0.18
                + sin(_Time.y * _WindSpeed * 0.11 + acrossWind * 0.019 + 1.7) * 0.06;
            float broadSway = sin(phase) * 0.58;
            float rollingSway = sin(phase * 0.47 - alongWind * 0.031 + acrossWind * 0.058 + 1.2) * 0.29;
            float fineSway = sin(phase * 1.83 + acrossWind * 0.19 + alongWind * 0.09) * 0.13;
            float sway = (broadSway + rollingSway + fineSway) * gustEnvelope;
            float crossSway = (cos(phase * 0.73 + acrossWind * 0.07) * 0.7
                + sin(phase * 1.31 + alongWind * 0.06) * 0.3) * (0.58 + gustCycle * 0.22);

            // Groundcover stores a normalized, root-relative bend weight in TEXCOORD3.
            // Tree meshes without this marker retain their original local-height response.
            float heightWeight = v.texcoord3.y > 0.5
                ? saturate(v.texcoord3.x)
                : saturate(max(v.vertex.y, 0.0) * _WindHeight);
            heightWeight = pow(heightWeight, 1.35);

            float3 windLocal = mul((float3x3)unity_WorldToObject, float3(windDirection.x, 0.0, windDirection.y));
            float3 crossLocal = mul((float3x3)unity_WorldToObject, float3(crossDirection.x, 0.0, crossDirection.y));
            v.vertex.xyz += (windLocal * sway + crossLocal * crossSway * 0.34)
                * _WindAmplitude * heightWeight;
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
