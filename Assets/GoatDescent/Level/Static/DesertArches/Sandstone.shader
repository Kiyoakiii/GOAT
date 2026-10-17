Shader "GoatDescent/DesertSandstone"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Sandstone", Color) = (1.48, .79, .42, 1)
        _SandTint ("Dry tops", Color) = (1.3, .96, .59, 1)
        _Tiling ("Texture scale", Float) = .035
        _Layers ("Strata strength", Range(0, 1)) = .3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _SandTint;
        float _Tiling, _Layers;
        struct Input { float3 worldPos; float3 worldNormal; };
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
            float3 p = IN.worldPos, n = normalize(IN.worldNormal);
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float3 tex = tex2D(_RockTex, p.zy * _Tiling).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling).rgb * weights.z;
            float macro = Noise(p.xz * .018 + p.y * .003);
            float strata = p.y * .14 + Noise(p.xz * .026) * .65;
            float band = .5 + .5 * sin(strata * 6.28318);
            float seam = 1 - smoothstep(.025, .13, abs(frac(strata) - .5));
            float top = smoothstep(.65, .95, n.y);
            float3 tint = lerp(_RockTint.rgb, _SandTint.rgb, top * .65);
            float shade = lerp(.84, 1.12, macro);
            shade *= 1 - _Layers * (seam * .38 + (1 - band) * .16) * (1 - top);
            o.Albedo = lerp(tex, dot(tex, float3(.3, .59, .11)).xxx, .55) * tint * shade;
            o.Occlusion = 1 - seam * _Layers * .35 * (1 - top);
            o.Smoothness = .07;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
