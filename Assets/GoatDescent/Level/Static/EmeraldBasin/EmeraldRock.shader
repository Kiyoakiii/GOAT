Shader "GoatDescent/EmeraldRock"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Rock", Color) = (1.4, 1.24, 1, 1)
        _OliveTint ("Olive patches", Color) = (.44, .42, .35, 1)
        _SandTint ("Sand flats", Color) = (.82, .70, .51, 1)
        _SandDark ("Sand dark", Color) = (.73, .62, .42, 1)
        _GrassTint ("Grass dark", Color) = (.22, .32, .20, 1)
        _GrassMid ("Grass mid", Color) = (.29, .38, .22, 1)
        _GrassKhaki ("Grass khaki", Color) = (.39, .41, .28, 1)
        _HazeTint ("Haze", Color) = (.66, .75, .78, 1)
        _Tiling ("Texture scale", Float) = .07
        _Layers ("Strata strength", Range(0, 1)) = .3
        _GrassAmount ("Grass amount", Range(0, 1)) = .9
        _Flute ("Vertical fluting", Range(0, 1)) = .5
        _GrassSlope ("Grass slope start", Range(0, 1)) = .3
        _GrassSlopeBlend ("Grass slope blend", Range(0, 1)) = .34
        _Dither ("Texture dither", Range(0, 1)) = .35
        _Contact ("Contact shadow", Range(0, 1)) = .45
        _HazeStart ("Haze start", Float) = 300
        _HazeEnd ("Haze end", Float) = 1500
        _HazeAmount ("Haze amount", Range(0, 1)) = .78
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _OliveTint, _SandTint, _SandDark, _GrassTint, _GrassMid, _GrassKhaki, _HazeTint;
        float _Tiling, _Layers, _GrassAmount, _Flute, _GrassSlope, _GrassSlopeBlend, _Dither;
        float _Contact, _HazeStart, _HazeEnd, _HazeAmount;
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
        float Gully(float ang, float centre)
        {
            float d = abs(atan2(sin(ang - centre), cos(ang - centre)));
            return saturate(1 - d / .18);
        }
        float ContactShade(float3 p)
        {
            float band = 1 - saturate(abs(p.y - 1.2) / 2.4);
            band = max(band, 1 - saturate(abs(p.y - 20) / 2.6));
            band = max(band, 1 - saturate(abs(p.y - 190) / 2.6));
            band = max(band, 1 - saturate(abs(p.y - 360) / 2.6));
            band = max(band, 1 - saturate(abs(p.y - 530) / 2.6));
            float ang = atan2(p.z, p.x);
            float gully = Gully(ang, .10);
            gully = max(gully, Gully(ang, .55));
            gully = max(gully, Gully(ang, 1.80));
            gully = max(gully, Gully(ang, 3.90));
            gully = max(gully, Gully(ang, 4.60));
            gully *= 1 - saturate(p.y / 90);
            return 1 - _Contact * (band * .07 + gully * .11);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos, n = normalize(IN.worldNormal);
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float2 dither = float2(Noise(p.xz * .06 + 3.1), Noise(p.zx * .08 + 17.7)) - .5;
            float2 shift = dither * _Dither;
            float3 tex = tex2D(_RockTex, p.zy * _Tiling + shift).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling + shift).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling + shift).rgb * weights.z;
            float macro = Noise(p.xz * .02 + p.y * .004);
            float grain = Noise(p.xz * .42 + p.y * .06);
            float blotch = Noise(p.xz * .011 + 5.3);
            float blotch2 = Noise(p.xz * .031 + 21.7);
            float ang = atan2(p.z, p.x);
            float flute = .5 + .5 * sin(ang * 26.0 + p.y * .012);
            float flute2 = .5 + .5 * sin(ang * 63.0 + 2.1);
            float strataWarp = (Noise(p.xz * .009 + 9.9) - .5) * 2.6 + (blotch2 - .5) * 1.4;
            strataWarp += (Noise(p.xz * .0035 + 2.7 + Noise(p.xz * .0016 + 61.1) * 2.0) - .5) * 3.4;
            float strataFreq = lerp(.55, 1.7, Noise(p.xz * .0045 + 33.3 + ang * .8));
            strataFreq *= lerp(.8, 1.2, Noise(float2(ang * 2.3, p.y * .0035) + 8.8));
            float strata = (p.y * .2 * strataFreq + Noise(p.xz * .04) * 1.15 + strataWarp) * lerp(.7, 1.25, Noise(p.xz * .006 + 44.2));
            strata += Noise(float2(ang * 5.1 + p.y * .001, 4.4)) * 1.6 - .8;
            float band = .5 + .5 * sin(strata * 6.28318 + Noise(p.xz * .09 + p.y * .01) * 3.4);
            float seamGate = lerp(.55, 1.25, Noise(p.xz * .021 + 77.5));
            float seam = (1 - smoothstep(.02, .26, abs(frac(strata) - .5))) * seamGate;
            float bare = saturate(Noise(p.xz * .008 + p.y * .0022 + 71.3) * 2.4 - 1.15);
            bare = max(bare, saturate(Noise(p.xz * .019 + 55.5) * 2.6 - 1.7));
            float flatUp = smoothstep(_GrassSlope, _GrassSlope + _GrassSlopeBlend, n.y);
            float strataMask = (1 - bare * .92) * (1 - flatUp);
            float patch = saturate(Noise(p.xz * .06 + 11.3) * 1.9 - .3);
            float grass = flatUp * _GrassAmount * lerp(.85, 1.0, patch);
            float3 grey = lerp(tex, dot(tex, float3(.3, .59, .11)).xxx, .5);
            float3 rock = grey * _RockTint.rgb * lerp(.80, 1.14, macro);
            rock *= lerp(float3(1.02, .99, .94), float3(1.15, 1.10, 1.01), macro * macro);
            float olive = saturate(Noise(p.xz * .045 + 31.1) * 2.1 - .82);
            olive = max(olive, saturate(Noise(p.xz * .017 + 63.7) * 2.2 - .95) * .8);
            rock = lerp(rock, _OliveTint.rgb * lerp(.78, 1.06, macro), olive);
            rock *= 1 - _Flute * (flute * .28 + flute2 * .12) * (1 - flatUp);
            rock *= 1 - _Layers * (seam * .16 + (1 - band) * .06) * strataMask;
            float3 sand = _SandTint.rgb * lerp(.9, 1.1, macro) * lerp(.88, 1.08, grain);
            sand = lerp(sand, _SandDark.rgb, saturate(Noise(p.xz * .08 + 41.3) * 1.6 - .5));
            float3 flats = lerp(sand, rock, saturate(Noise(p.xz * .05 + 3.7) * 1.4 - .35));
            float3 meadow = _GrassTint.rgb;
            meadow = lerp(meadow, _GrassMid.rgb, saturate(blotch * 1.7 - .25));
            meadow = lerp(meadow, _GrassKhaki.rgb, saturate(blotch2 * 1.6 - .55));
            meadow *= lerp(.9, 1.1, macro) * lerp(.94, 1.06, grain);
            float3 albedo = lerp(flats, meadow, grass) * ContactShade(p);
            float haze = smoothstep(_HazeStart, _HazeEnd, length(_WorldSpaceCameraPos - p)) * _HazeAmount;
            albedo = lerp(albedo, _HazeTint.rgb * .72, haze);
            albedo = lerp(albedo, dot(albedo, float3(.3, .59, .11)).xxx, haze * .45);
            o.Albedo = albedo;
            o.Occlusion = 1 - _Layers * seam * .14 * strataMask;
            o.Smoothness = .05 + .05 * flatUp;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
