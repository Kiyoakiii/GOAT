Shader "GoatDescent/FrozenRiftSnow"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _SnowColor ("Snow lit", Color) = (.9, .94, .985, 1)
        _RockColor ("Slate", Color) = (.5, .48, .45, 1)
        _IceTint ("Ice pores", Color) = (.66, .84, .97, 1)
        _ShadowTint ("Snow shadow", Color) = (.74, .82, .94, 1)
        _HazeColor ("Horizon", Color) = (.66, .8, .94, 1)
        _Tiling ("Texture scale", Float) = .06
        _Gloss ("Smoothness", Range(0, 1)) = .2
        _Lift ("Sky fill", Range(0, 1)) = .25
        _Haze ("Aerial perspective", Range(0, 1)) = .8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _SnowColor, _RockColor, _IceTint, _ShadowTint, _HazeColor;
        float _Tiling, _Gloss, _Lift, _Haze;
        struct Input { float3 worldPos; float3 worldNormal; };
        float Hash(float2 p)
        {
            p = frac(p * float2(127.31, 311.7));
            p += dot(p, p + 34.23);
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
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float3 tex = tex2D(_RockTex, p.zy * _Tiling).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling).rgb * weights.z;
            float slope = 1 - saturate(n.y);
            float pores = Noise(p.xz * 1.7 + Noise(p.xy * .21) * 3.1);
            float drift = Noise(p.xz * .055 + p.y * .02);
            float driftB = Noise(p.xz * .18 + 4.7);
            float3 base = lerp(_SnowColor.rgb, _IceTint.rgb, saturate((.6 - pores) * .7) * .16);
            float macro = Noise(p.xz * .011 + 7.3);
            base *= lerp(.87, 1.05, macro);
            base *= lerp(.92, 1.05, drift);
            float hollow = saturate(1.15 - drift * 1.25 - driftB * .55);
            base = lerp(base, _ShadowTint.rgb, hollow * .55 * (1 - slope * .3));
            base = lerp(base, base * float3(.8, .89, 1.16), hollow * .45);
            base = lerp(base, _RockColor.rgb * (1.05 + tex.g * .3), smoothstep(.5, .95, slope) * .6);
            float contact = (1 - smoothstep(.02, .13, abs(frac(p.y / 62.0 + .5) - .5))) * smoothstep(.35, .8, slope);
            base *= 1 - contact * .22;
            float sun = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
            float haze = saturate((length(p.xz) - 900) / 1400) * _Haze;
            base = lerp(base, dot(base, float3(.3, .59, .11)).xxx, haze * .5);
            o.Albedo = lerp(base, _HazeColor.rgb, haze * .8);
            o.Emission = base * (1 - sun) * _Lift * float3(.86, .94, 1.12);
            o.Smoothness = lerp(_Gloss, .7, pores * .3);
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
