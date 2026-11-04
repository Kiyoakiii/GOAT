Shader "GoatDescent/FrozenRiftRock"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Warm light slate", Color) = (1.84, 1.72, 1.54, 1)
        _SnowTint ("Snow dust", Color) = (.9, .93, .97, 1)
        _DeepTint ("Rift depth tint", Color) = (.33, .45, .58, 1)
        _HazeColor ("Horizon", Color) = (.66, .8, .94, 1)
        _Tiling ("Texture scale", Float) = .05
        _Layers ("Strata strength", Range(0, 1)) = .62
        _StrataStep ("Strata step", Float) = .035
        _Depth ("Depth tint strength", Range(0, 1)) = .3
        _Haze ("Aerial perspective", Range(0, 1)) = .8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _SnowTint, _DeepTint, _HazeColor;
        float _Tiling, _Layers, _Depth, _Haze, _StrataStep;
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
            float grey = dot(tex, float3(.3, .59, .11));
            float macro = Noise(p.xz * .018 + p.y * .003);
            float plate = Noise(p.xz * .0045 + p.y * .0012);
            float zone = Noise(p.xz * .0016 + p.y * .0004);
            float strata = p.y * _StrataStep + Noise(p.xz * .017) * .18;
            float band = .5 + .5 * sin(strata * 6.28318);
            float seam = 1 - smoothstep(.008, .032, abs(frac(strata) - .5));
            seam *= smoothstep(.34, .68, Noise(p.xz * .008 + 3.0));
            float up = saturate(n.y);
            float steep = smoothstep(.4, .8, 1 - up);
            float flat = smoothstep(.42, .72, up);
            float shade = lerp(.62, 1.16, macro) * lerp(.84, 1.16, plate) * lerp(.9, 1.1, zone);
            float fracture = abs(sin(p.x * .28 + p.z * .23 + Noise(p.xy * .06) * 2.8));
            float cracks = (1 - smoothstep(.014, .055, fracture)) * smoothstep(.25, .65, Noise(p.zy * .075));
            shade *= 1 - cracks * .38;
            shade *= 1 - _Layers * (seam * .06 + (1 - band) * .04);
            float3 base = lerp(tex, grey.xxx, .96) * _RockTint.rgb * shade;
            float depth = saturate((40 - p.y) / 200) * _Depth;
            base = lerp(base, _DeepTint.rgb * (1.15 + .8 * macro), depth);
            float contact = (1 - smoothstep(.02, .13, abs(frac(p.y / 85.0 + .5) - .5))) * steep;
            base *= 1 - contact * .4;
            base = lerp(base, _SnowTint.rgb, cracks * .3);
            float frost = flat * (.34 + .42 * macro) * (1 - depth * .6);
            base = lerp(base, _SnowTint.rgb, frost * .38);
            float haze = saturate((length(p.xz) - 900) / 1400) * _Haze;
            base = lerp(base, dot(base, float3(.3, .59, .11)).xxx, haze * .5);
            o.Albedo = lerp(base, _HazeColor.rgb, haze * .8);
            o.Occlusion = 1 - seam * _Layers * .3;
            o.Smoothness = .06 + frost * .1;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
