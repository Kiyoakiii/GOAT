Shader "GoatDescent/DesertSandstone"
{
    Properties
    {
        _RockTex ("Rock", 2D) = "white" {}
        _RockTint ("Sandstone lit", Color) = (1.12, .56, .28)
        _SandTint ("Dry tops", Color) = (1.1, .74, .48)
        _SeamTint ("Strata seams", Color) = (.34, .15, .09)
        _ShadeTint ("Shadow terracotta", Color) = (.4, .24, .2)
        _MidTint ("Mid terracotta", Color) = (.95, .56, .38)
        _PlayaTint ("Playa sand", Color) = (1, .86, .72)
        _VarnishTint ("Desert varnish", Color) = (.2, .11, .07)
        _HazeTint ("Haze", Color) = (.72, .52, .38)
        _Tiling ("Texture scale", Float) = .035
        _StrataScale ("Strata scale", Float) = .072
        _Layers ("Strata strength", Range(0, 1)) = .3
        _Dust ("Dust on flats", Range(0, 1)) = .6
        _Mid ("Mid tone", Range(0, 1)) = .5
        _Swirl ("Axis rotation and offset", Vector) = (0, 0, 0, 0)
        _Shear ("Vertical shear", Float) = 0
        _Jitter ("Sampling jitter", Float) = 4
        _HazeStart ("Haze start", Float) = 420
        _HazeEnd ("Haze end", Float) = 2100
        _HazeStrength ("Haze strength", Range(0, 1)) = .45
        _HazeDesat ("Haze desaturation", Range(0, 1)) = .25
        _Varnish ("Varnish streaks", Range(0, 1)) = .55
        _Contact ("Contact shade", Range(0, 1)) = .5
        _DepthFloor ("Depth floor Y", Float) = -175
        _DepthSpan ("Depth span", Float) = 200
        _DepthShade ("Depth shade", Range(0, 1)) = .34
        _PlayaY ("Playa Y", Float) = -100
        _PlayaSpan ("Playa span", Float) = 34
        _Playa ("Playa strength", Range(0, 1)) = .55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _RockTex;
        float4 _RockTint, _SandTint, _SeamTint, _ShadeTint, _PlayaTint, _HazeTint, _VarnishTint, _MidTint, _Swirl;
        float _Tiling, _StrataScale, _Layers, _Dust, _Mid, _Shear, _Jitter;
        float _HazeStart, _HazeEnd, _HazeStrength, _HazeDesat, _Contact;
        float _DepthFloor, _DepthSpan, _DepthShade, _PlayaY, _PlayaSpan, _Playa, _Varnish;
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
            float3 w = IN.worldPos;
            float3 n = normalize(IN.worldNormal);
            float cs = cos(_Swirl.x);
            float sn = sin(_Swirl.x);
            float3 p = float3(w.x * cs - w.z * sn, w.y, w.x * sn + w.z * cs);
            p.xz += p.y * _Shear;
            p += float3(Noise(p.zy * .043 + 5.2), Noise(p.xy * .037 + 17.3), Noise(p.zy * .031 + 41.7)) * _Jitter;
            p += _Swirl.yzw;
            float3 weights = pow(abs(n), 4);
            weights /= max(.001, weights.x + weights.y + weights.z);
            float3 tex = tex2D(_RockTex, p.zy * _Tiling).rgb * weights.x
                + tex2D(_RockTex, p.xz * _Tiling).rgb * weights.y
                + tex2D(_RockTex, p.xy * _Tiling).rgb * weights.z;
            float macro = Noise(p.xz * .017 + p.y * .009) * .55 + Noise(p.zy * .021 + p.x * .012) * .45;
            float phase = p.y * _StrataScale
                + Noise(p.xz * .019 + p.y * .004) * 2
                + Noise(p.zy * .023 + p.x * .007) * 1.35
                + Noise(p.xz * .072 + p.y * .05) * .25;
            float band = 1 - abs(frac(phase) - .5) * 2;
            float layer = smoothstep(.05, .95, band);
            float coarse = 1 - abs(frac(phase * .37 + Noise(p.zy * .006 + 3.3) * .6) - .5) * 2;
            coarse = smoothstep(.12, .95, coarse);
            float grain = Noise(p.xz * .33 + p.y * .17) * .5 + Noise(p.zy * .37 + p.x * .21) * .5;
            float flake = Noise(p.xy * .9 + p.z * .6) * .5 + Noise(p.zy * 1.1 + p.x * .7) * .5;
            float top = smoothstep(.6, .96, n.y);
            float3 tint = lerp(_RockTint.rgb, _SandTint.rgb, top * _Dust);
            tint = lerp(tint, _SeamTint.rgb, (1 - layer) * _Layers * (1 - top) * .3);
            tint = lerp(tint, _SandTint.rgb, (1 - coarse) * _Layers * (1 - top) * .22);
            float varnish = smoothstep(.35, 1.0,
                Noise(float2(p.x * .11 + p.y * .05, p.z * .11)) * .72
                + Noise(float2(p.x * .021, p.y * .028 + p.z * .021)) * .45);
            tint = lerp(tint, _VarnishTint.rgb, varnish * (1 - top) * _Varnish * .75 * smoothstep(.02, .45, 1 - n.y));
            tint = lerp(tint, _MidTint.rgb, (1 - top) * _Mid);
            float3 base = lerp(tex, dot(tex, float3(.3, .59, .11)).xxx, .58) * tint
                * (lerp(.84, 1.1, macro) * lerp(.94, 1.06, grain) * lerp(.97, 1.03, flake));
            float depth = saturate((w.y - _DepthFloor) / _DepthSpan);
            float contact = (1 - saturate(IN.color.b)) * (1 - n.y * n.y);
            float ao = saturate(IN.color.r) * lerp(1 - _DepthShade, 1, depth);
            ao = saturate(ao * (1 - _Contact * contact));
            float under = 1 - smoothstep(-.6, .05, n.y);
            ao = saturate(ao + (1 - ao) * under * .6);
            float playa = saturate(1 - abs(w.y - _PlayaY) / _PlayaSpan) * smoothstep(.72, .96, n.y) * _Playa;
            base = lerp(base, _PlayaTint.rgb * base * 1.6, playa);
            float distance = length(_WorldSpaceCameraPos - w);
            float haze = saturate((distance - _HazeStart) / max(1, _HazeEnd - _HazeStart)) * _HazeStrength;
            float3 hazeColor = lerp(_HazeTint.rgb, dot(_HazeTint.rgb, float3(.3, .59, .11)).xxx, _HazeDesat);
            o.Albedo = lerp(lerp(base, base * _ShadeTint.rgb * 2.1, (1 - ao) * .8), hazeColor, haze);
            o.Occlusion = lerp(ao, 1, haze);
            o.Smoothness = .06 * (1 - haze);
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
