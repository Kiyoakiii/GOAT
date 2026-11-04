Shader "GoatDescent/TerracePeakFar"
{
    Properties
    {
        _BaseColor ("Base", Color) = (.46, .52, .6, 1)
        _HazeColor ("Haze", Color) = (.72, .82, .92, 1)
        _HazeStart ("Haze start", Float) = 700
        _HazeEnd ("Haze end", Float) = 2200
        _HazeStrength ("Haze strength", Range(0, 1)) = .7
        _Rim ("Atmospheric rim", Range(0, 1)) = .55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float4 _BaseColor, _HazeColor;
        float _HazeStart, _HazeEnd, _HazeStrength, _Rim;

        struct Input { float3 worldPos; float3 worldNormal; float4 color : COLOR; };

        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float Noise(float2 p)
        {
            float2 c = floor(p), t = frac(p);
            t = t * t * (3 - 2 * t);
            return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), t.x),
                lerp(Hash(c + float2(0, 1)), Hash(c + 1), t.x), t.y);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float3 n = normalize(IN.worldNormal);
            float up = saturate(n.y);
            float3 viewDir = normalize(_WorldSpaceCameraPos - p);
            float rim = pow(1.0 - saturate(dot(n, viewDir)), 3.0);
            float band = lerp(.78, 1.0, saturate((p.y + 120.0) / 520.0));
            float grain = Noise(p.xz * .0032 + p.y * .006);
            float ribs = .94 + .12 * Noise(p.xz * .011 + 4.1) + .05 * (grain - .5);
            float3 albedo = _BaseColor.rgb * band * ribs * (0.9 + .12 * up);
            albedo *= lerp(.8, 1.06, IN.color.r);
            float depth = distance(_WorldSpaceCameraPos, p);
            float haze = smoothstep(_HazeStart, _HazeEnd, depth) * _HazeStrength;
            float lum = dot(albedo, float3(.33, .5, .17));
            albedo = lerp(lerp(albedo, lum.xxx, haze * .6), _HazeColor.rgb, haze);
            albedo = lerp(albedo, _HazeColor.rgb, rim * _Rim * (.3 + .7 * saturate(depth / max(1.0, _HazeEnd))));
            o.Albedo = albedo;
            o.Emission = albedo * (.1 * (1.0 - rim * .5));
            o.Smoothness = .04;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
