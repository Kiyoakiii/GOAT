Shader "GoatDescent/EmeraldPeak"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Rock", Color) = (.58, .53, .5, 1)
        _ForestTint ("Forest", Color) = (.10, .21, .13, 1)
        _SnowTint ("Snow", Color) = (.97, .98, 1, 1)
        _HazeTint ("Haze", Color) = (.72, .80, .88, 1)
        _Tiling ("Texture scale", Float) = .014
        _ForestTop ("Forest top", Float) = 265
        _ForestBlend ("Forest blend", Float) = 200
        _SnowStart ("Snow start", Float) = 505
        _SnowBlend ("Snow blend", Float) = 85
        _Dither ("Texture dither", Range(0, 1)) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _ForestTint, _SnowTint, _HazeTint;
        float _Tiling, _ForestTop, _ForestBlend, _SnowStart, _SnowBlend, _Dither;
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
            float2 dither = float2(Noise(p.xz * .05 + 3.1), Noise(p.zx * .07 + 17.7)) - .5;
            float2 shift = dither * _Dither;
            float3 tex = tex2D(_RockTex, p.zy * _Tiling + shift).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling + shift).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling + shift).rgb * weights.z;
            float macro = Noise(p.xz * .009 + p.y * .0015);
            float gully = Noise(p.xz * .045 + 5.1);
            float speck = Noise(p.xz * .16);
            float ridge = Noise(p.xz * .022 + 11.7);
            float3 rock = lerp(tex, dot(tex, float3(.3, .59, .11)).xxx, .62);
            rock *= _RockTint.rgb * lerp(.80, 1.16, macro) * lerp(.90, 1.08, gully) * lerp(.94, 1.05, speck);
            float forest = (1 - smoothstep(_ForestTop, _ForestTop + _ForestBlend, p.y)) * saturate(.55 + ridge * 1.1);
            float3 green = _ForestTint.rgb * lerp(.68, 1.3, macro) * lerp(.9, 1.1, speck);
            float snow = smoothstep(_SnowStart, _SnowStart + _SnowBlend, p.y) * saturate(n.y * .95 + .12);
            snow *= saturate(.55 + ridge * 1.05);
            float3 albedo = lerp(rock, green, forest);
            albedo = lerp(albedo, _SnowTint.rgb * lerp(.95, 1.03, speck), snow);
            float haze = smoothstep(1000, 2400, length(_WorldSpaceCameraPos - p));
            o.Albedo = lerp(albedo, _HazeTint.rgb * lerp(.9, 1.05, macro), haze * .5);
            o.Albedo = lerp(o.Albedo, dot(o.Albedo, float3(.3, .59, .11)).xxx, haze * .35);
            o.Smoothness = lerp(.05, .16, snow);
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
