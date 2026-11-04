Shader "GoatDescent/TerracePeakMeadow"
{
    Properties
    {
        _GrassTint ("Meadow", Color) = (.40, .47, .35, 1)
        _GrassLight ("Sunlit meadow", Color) = (.48, .53, .51, 1)
        _EarthTint ("Earth", Color) = (.42, .40, .34, 1)
        _HazeColor ("Haze", Color) = (.80, .80, .75, 1)
        _SunDir ("Sun direction", Vector) = (-.38, .62, -.68, 0)
        _Tiling ("Patch scale", Float) = .0055
        _Contact ("Contact shadow", Range(0, 1)) = .7
        _Haze ("Haze strength", Range(0, 1)) = .85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float4 _GrassTint, _GrassLight, _EarthTint, _HazeColor, _SunDir;
        float _Tiling, _Contact, _Haze;

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
            float macro = Noise(p.xz * _Tiling);
            float fine = Noise(p.xz * _Tiling * 5.3 + 7.2);
            float micro = Noise(p.xz * _Tiling * 34.0 + 3.1);
            float grain = Noise(p.xz * _Tiling * 71.0 + 17.3);
            float slope = 1 - smoothstep(.68, .93, n.y);
            float facing = saturate(dot(n, normalize(_SunDir.xyz)));
            float dither = (micro - .5) * .22 + (grain - .5) * .2 + (Noise(p.xz * _Tiling * 9.0 + 11.7) - .5) * .12;
            float lit = smoothstep(.02, .78, facing + dither);
            float3 dark = _GrassTint.rgb * .84;
            float3 bright = lerp(_GrassTint.rgb, _GrassLight.rgb, saturate(macro * .6 + fine * .3 + micro * .2));
            float3 albedo = lerp(dark, bright, lit);
            albedo = lerp(albedo, _EarthTint.rgb, smoothstep(.2, .8, slope + dither * .35) * .65);
            albedo *= lerp(.9, 1.06, fine) * lerp(.94, 1.04, micro) * lerp(.97, 1.03, grain);
            float contact = lerp(1.0, IN.color.r, _Contact * .8);
            albedo *= lerp(.72, 1.0, contact);
            float depth = distance(_WorldSpaceCameraPos, p);
            float haze = smoothstep(700.0, 2600.0, depth) * _Haze;
            float lum = dot(albedo, float3(.33, .5, .17));
            albedo = lerp(lerp(albedo, lum.xxx, haze * .5), _HazeColor.rgb, haze);
            o.Albedo = albedo;
            o.Smoothness = .05;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
