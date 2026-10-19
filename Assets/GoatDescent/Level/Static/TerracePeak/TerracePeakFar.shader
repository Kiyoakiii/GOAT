Shader "GoatDescent/TerracePeakFar"
{
    Properties
    {
        _BaseColor ("Base", Color) = (.46, .52, .6, 1)
        _HazeColor ("Haze", Color) = (.72, .82, .92, 1)
        _HazeStart ("Haze start", Float) = 700
        _HazeEnd ("Haze end", Float) = 2200
        _HazeStrength ("Haze strength", Range(0, 1)) = .7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float4 _BaseColor, _HazeColor;
        float _HazeStart, _HazeEnd, _HazeStrength;

        struct Input { float3 worldPos; };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float3 albedo = _BaseColor.rgb * lerp(.92, 1.08, saturate(p.y / 420));
            float depth = distance(_WorldSpaceCameraPos, p);
            float haze = smoothstep(_HazeStart, _HazeEnd, depth) * _HazeStrength;
            float lum = dot(albedo, float3(.33, .5, .17));
            albedo = lerp(lerp(albedo, lum.xxx, haze * .6), _HazeColor.rgb, haze);
            o.Albedo = albedo;
            o.Emission = albedo * (_HazeStrength * .22f);
            o.Smoothness = .04;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
