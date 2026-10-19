Shader "GoatDescent/TerracePeakRock"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Mid rock", Color) = (.76, .6, .42, 1)
        _SunTint ("Sunlit rock", Color) = (.92, .73, .48, 1)
        _ShadowTint ("Cool shadow", Color) = (.43, .40, .38, 1)
        _DeepTint ("Deep shadow", Color) = (.32, .33, .32, 1)
        _MossTint ("Grass shelf", Color) = (.40, .47, .35, 1)
        _HazeColor ("Haze", Color) = (.80, .80, .75, 1)
        _SunDir ("Sun direction", Vector) = (-.38, .62, -.68, 0)
        _Tiling ("Texture scale", Float) = .022
        _Layers ("Strata strength", Range(0, 1)) = .45
        _Moss ("Grass on shelves", Range(0, 1)) = .85
        _Contact ("Contact shadow", Range(0, 1)) = .55
        _Haze ("Haze strength", Range(0, 1)) = .85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _RockTex;
        float4 _RockTint, _SunTint, _ShadowTint, _DeepTint, _MossTint, _HazeColor, _SunDir;
        float _Tiling, _Layers, _Moss, _Contact, _Haze;

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
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float3 tex = tex2D(_RockTex, p.zy * _Tiling).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling).rgb * weights.z;
            float macro = Noise(p.xz * .009 + p.y * .003);
            float fine = Noise(p.xz * .05 + p.y * .022);
            float patch = Noise(p.xz * .011 + p.y * .0035);
            float facingUp = smoothstep(.35, .72, n.y);
            float top = smoothstep(.62, .9, n.y);
            float grass = top * smoothstep(.2, .66, Noise(p.xz * .03 + 5.1)) * _Moss;
            float angle = atan2(p.z, p.x);
            float arc = angle * length(p.xz) - p.y * .04;
            float cutFreq = arc / 26.0;
            float jitter = Noise(float2(floor(cutFreq) * .31, 7.3)) * .35;
            float withinCut = frac(cutFreq + jitter);
            float cut = 1 - smoothstep(0, .16, min(withinCut, 1 - withinCut));
            cut *= 1 - facingUp * .7;
            float facetTone = .9 + .2 * Hash(float2(floor(cutFreq) * .53, 4.7));
            float groove = pow(saturate(sin(angle * 12 + p.y * .002)), 10) * (1 - facingUp * .6);
            float plate = lerp(6.5, 10.5, Noise(p.xz * .0035 + 3.1));
            float within = frac(p.y / plate);
            float slot = (1 - smoothstep(0, .085, min(within, 1 - within))) * facingUp;
            float bedTone = 1 + (Hash(float2(floor(p.y / plate) * 3.7, 11.3)) - .5) * .34 * facingUp;
            float facing = saturate(dot(n, normalize(_SunDir.xyz)));
            float bounce = saturate(-n.y);
            float lit = max(smoothstep(.04, .62, facing), bounce * .5 + bounce * bounce * .25);
            float3 warm = lerp(_RockTint.rgb, _SunTint.rgb, saturate(macro * .7 + top * .3 + bounce * .2));
            warm *= lerp(.8, 1.08, fine) * bedTone;
            warm *= lerp(.82, 1.12, patch) * facetTone;
            warm *= 1 - cut * .8;
            warm *= 1 - groove * .42;
            float fracture = abs(sin(p.x * .09 + p.z * .08 + patch * 3.4));
            float cracks = (1 - smoothstep(.03, .13, fracture)) * smoothstep(.25, .65, fine);
            warm *= 1 - cracks * .3 * (1 - top);
            warm *= 1 - slot * .78 * (1 - top);
            float3 shadowTint = lerp(_ShadowTint.rgb, _ShadowTint.rgb * 1.6 + float3(.16, .12, .06), saturate(-n.y) * .9);
            float3 stone = lerp(shadowTint, warm, lit);
            float contact = lerp(1.0, IN.color.r, _Contact);
            stone = lerp(_DeepTint.rgb * (1.0 + macro * .5), stone, contact);
            float3 moss = _MossTint.rgb * lerp(.82, 1.18, fine);
            float3 albedo = lerp(stone, moss, saturate(grass) * lerp(.6, 1.0, lit));
            float depth = distance(_WorldSpaceCameraPos, p);
            float haze = smoothstep(1400.0, 3600.0, depth) * _Haze;
            float lum = dot(albedo, float3(.33, .5, .17));
            albedo = lerp(lerp(albedo, lum.xxx, haze * .5), _HazeColor.rgb, haze);
            o.Albedo = albedo;
            o.Occlusion = (1 - (slot * .5 + cut * .5 + groove * .4) * _Layers) * contact;
            o.Smoothness = .04;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
