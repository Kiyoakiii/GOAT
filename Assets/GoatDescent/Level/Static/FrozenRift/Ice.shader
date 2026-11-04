Shader "GoatDescent/FrozenRiftIce"
{
    Properties
    {
        _Color ("Glacier blue", Color) = (.45, .69, .92, 1)
        _CoreColor ("Deep blue", Color) = (.2, .34, .52, 1)
        _EdgeColor ("Edge glow", Color) = (.78, .95, 1, 1)
        _HazeColor ("Horizon", Color) = (.66, .8, .94, 1)
        _Alpha ("Core alpha", Range(0, 1)) = .78
        _Fresnel ("Rim power", Range(.5, 8)) = 2.2
        _Emissive ("Inner glow", Range(0, 2)) = .36
        _Gloss ("Smoothness", Range(0, 1)) = .88
        _Haze ("Aerial perspective", Range(0, 1)) = .7
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows
        #pragma target 3.0
        float4 _Color, _CoreColor, _EdgeColor, _HazeColor;
        float _Alpha, _Fresnel, _Emissive, _Gloss, _Haze;
        struct Input { float3 worldPos; float3 worldNormal; float3 viewDir; };
        float Hash(float2 p)
        {
            p = frac(p * float2(151.27, 231.19));
            p += dot(p, p + 41.13);
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
            float3 p = IN.worldPos, n = normalize(IN.worldNormal);
            float fresnel = pow(1 - saturate(dot(n, normalize(IN.viewDir))), _Fresnel);
            float wave = sin(p.y * .11 + p.z * .17) * .7;
            float crack = 1 - smoothstep(.015, .08, abs(sin(p.x * .34 + p.y * .08 + wave)));
            crack *= .45 + .55 * abs(sin(p.z * .17 + p.y * .13));
            float deep = .5 + .5 * sin(p.x * .06 + p.y * .035 + p.z * .07);
            float streak = smoothstep(.55, .95, Noise(p.xz * .08 + p.y * .02));
            float3 body = lerp(_CoreColor.rgb, _Color.rgb, deep * .8 + .15);
            float3 shell = lerp(body, _EdgeColor.rgb, saturate(fresnel * .8 + crack * .45 + streak * .18));
            float haze = saturate((length(p.xz) - 900) / 1400) * _Haze;
            shell = lerp(shell, dot(shell, float3(.3, .59, .11)).xxx, haze * .45);
            o.Albedo = lerp(shell, _HazeColor.rgb, haze * .7);
            o.Emission = _CoreColor.rgb * _Emissive * .6 + _Color.rgb * _Emissive
                + _EdgeColor.rgb * (fresnel * .28 + crack * .16) * _Emissive * 2;
            o.Smoothness = _Gloss;
            o.Metallic = .05;
            o.Alpha = saturate(_Alpha + fresnel * .26 + crack * .12 + streak * .08);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
