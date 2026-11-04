Shader "GoatDescent/SkyBridgeRock"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Rock tint", Color) = (.74, .68, .58, 1)
        _GrassTint ("Vegetation", Color) = (.22, .31, .12, 1)
        _SnowTint ("Snow", Color) = (.88, .93, .99, 1)
        _SnowLine ("Snow line", Float) = 100000
        _GreenLine ("Vegetation line", Float) = -100000
        _Occlusion ("Baked contact shadow", Range(0, 1)) = 0
        _HazeColor ("Aerial haze", Color) = (.53, .74, .91, 1)
        _HazeStart ("Haze start", Float) = 700
        _HazeEnd ("Haze end", Float) = 2600
        _HazeStrength ("Haze strength", Range(0, 1)) = .55
        _Tiling ("Texture scale", Float) = .03
        _Layers ("Strata strength", Range(0, 1)) = .34
        _Grass ("Vegetation amount", Range(0, 1)) = .6
        _ShadowFloor ("Shadow floor", Range(0, 1)) = .35
        _FillTint ("Fill light", Color) = (.36, .42, .38, 1)
        _TexAmount ("Texture strength", Range(0, 1)) = 1
        _Detail ("Detail strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _GrassTint, _SnowTint;
        float _SnowLine, _Tiling, _Layers, _Grass, _GreenLine;
        float4 _HazeColor;
        float _Occlusion, _HazeStart, _HazeEnd, _HazeStrength;
        float _ShadowFloor, _TexAmount, _Detail;
        float4 _FillTint;
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
            float camDist = length(_WorldSpaceCameraPos - p);
            float detail = saturate((320.0 - camDist) * .005);
            float warpFade = smoothstep(2.0, 45.0, camDist) * saturate((300.0 - camDist) * .01);
            float microFade = saturate((220.0 - camDist) * .01);
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float2 sway = float2(Noise(p.xy * .017 + 7.3), Noise(p.zy * .019 + 2.9)) * 1.6 * warpFade;
            float2 drift = float2(Noise(p.xz * .043 + 11.1), Noise(p.yz * .037 + 5.7)) * .5 * warpFade;
            float2 vwarp = float2(Noise(p.y * .013 + p.x * .004 + 5.1),
                Noise(p.y * .0095 + p.z * .004 + 2.3)) * 1.7 * warpFade;
            float2 shear = float2(p.y * .0042, p.y * .0015) * warpFade;
            float3 tex = tex2D(_RockTex, p.zy * _Tiling + sway + vwarp + shear).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling * 1.17 + sway.yx + drift).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling * 1.31 + sway + drift.yx + vwarp + shear.yx).rgb * weights.z;
            float seamless = Noise(p.xz * .031 + 4.7) * .5 + Noise(p.xz * .093 + 12.3) * .3
                + Noise(p.xz * .31 + 8.1) * .2;
            float3 seamlessTex = float3(.5, .47, .43) * lerp(.68, 1.3, seamless);
            tex = lerp(seamlessTex, tex, _TexAmount);
            float texLum = dot(tex, float3(.3, .59, .11));
            tex = lerp(lerp(float3(texLum, texLum, texLum), tex, .22), tex, lerp(.12, 1, detail));
            float grain = .6 + .6 * Noise(p.xz * .009 + p.y * .002 + 3.7);
            float macro = Noise(p.xz * .012 + p.y * .003);
            float warm = Noise(p.xz * .0055 + 21.3);
            float micro = lerp(.5, Noise(p.xy * .11 + p.z * .07), microFade);
            float strata = p.y * .13 + Noise(p.xz * .02) * .8;
            float band = lerp(.5f, .5f + .5f * sin(strata * 6.28318), detail);
            float seam = (1 - smoothstep(.03, .16, abs(frac(strata) - .5))) * detail;
            float fracture = abs(sin(p.x * .21 + p.z * .17 + Noise(p.xy * .05) * 3.1));
            float cracks = (1 - smoothstep(.012, .05, fracture)) * smoothstep(.3, .7, Noise(p.zy * .06)) * detail * _Detail;
            float vertMask = smoothstep(.3, .85, 1 - abs(n.y));
            float flute = abs(sin(p.x * .031 + p.z * .024 + p.y * .0045 + Noise(p.xz * .024) * 7.5
                + Noise(p.yz * .012 + 3.3) * 4.0));
            float grooves = (1 - smoothstep(.02, .1, flute)) * vertMask * detail * _Detail;
            float fissure = abs(sin(p.y * .052 + Noise(p.xz * .03 + 9.1) * 5.4));
            float verticals = (1 - smoothstep(.015, .07, fissure)) * vertMask * detail * _Detail;
            float shade = lerp(.48, 1.34, macro) * lerp(.88, 1.08, micro) * lerp(.94, 1.08, grain);
            shade *= 1 - cracks * .2;
            shade *= 1 - grooves * .13 - verticals * .1;
            shade *= 1 - _Layers * (seam * .18 + (1 - band) * .08) * _Detail;
            float3 albedo = lerp(tex, dot(tex, float3(.3, .59, .11)).xxx, .82) * _RockTint.rgb * shade;
            albedo *= lerp(float3(.95, .91, .86), float3(1.06, 1.02, .95), warm);
            float softXZ = Noise(p.xz * .011 + p.y * .003 + 41.7);
            float softXY = Noise(p.xy * .0105 + 13.1);
            float softZY = Noise(p.zy * .0095 + 27.9);
            float softSpots = softXZ * weights.y + softXY * weights.z + softZY * weights.x;
            float blotchXZ = Noise(p.xz * .034 + p.y * .011 + 5.3);
            float blotchXY = Noise(p.xy * .031 + 19.7);
            float blotchZY = Noise(p.zy * .029 + 11.3);
            float softBlotch = blotchXZ * weights.y + blotchXY * weights.z + blotchZY * weights.x;
            albedo *= lerp(.86, 1.14, softSpots) * lerp(.95, 1.06, softBlotch);
            float up = saturate(n.y);
            float patch = Noise(p.xz * .055 + p.y * .02);
            float leafy = saturate(1 - smoothstep(_GreenLine - 40, _GreenLine + 30, p.y))
                * smoothstep(.08, .42, up);
            float grass = smoothstep(.62, .92, up) * _Grass * saturate(patch * 1.7) * smoothstep(-2, 30, p.y);
            grass = saturate(grass + leafy * _Grass * saturate(patch * 1.4));
            float snowy = smoothstep(_SnowLine - 12, _SnowLine + 26, p.y + patch * 22);
            float snowMask = saturate(snowy * (.45 + .55 * up));
            albedo = lerp(albedo, _GrassTint.rgb * lerp(.82, 1.12, patch), grass);
            albedo = lerp(albedo, _SnowTint.rgb * lerp(.92, 1.04, patch), snowMask);
            float contact = lerp(1, IN.color.r, _Occlusion);
            contact = max(contact, _ShadowFloor * .85f);
            albedo *= contact;
            float3 fill = (albedo * .5 + _FillTint.rgb * .5) * _ShadowFloor
                * .22 * lerp(.6, 1.1, macro) * lerp(.8, 1.15, micro);
            o.Emission = fill;
            float haze = saturate((camDist - _HazeStart) / max(1, _HazeEnd - _HazeStart));
            haze *= haze;
            float lum = dot(albedo, float3(.3, .59, .11));
            albedo = lerp(albedo, lerp(float3(lum, lum, lum), _HazeColor.rgb, .8), haze * _HazeStrength);
            o.Albedo = albedo;
            o.Occlusion = saturate((1 - seam * _Layers * .3) * contact);
            o.Smoothness = lerp(.07, .34, snowMask);
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
