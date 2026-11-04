Shader "GoatDescent/ScenicRock"
{
    Properties
    {
        _Color ("Stone", Color) = (.68,.53,.36,1)
        _CoverColor ("Shelf vegetation", Color) = (.25,.36,.12,1)
        _Cover ("Shelf coverage", Range(0,1)) = 0
        _Scale ("Rock pattern size", Float) = 65
        _Variation ("Stone variation", Range(0,1)) = .3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color, _CoverColor;
        float _Cover, _Scale, _Variation;
        struct Input { float3 worldPos; float3 worldNormal; };
        float hash(float3 p)
        {
            p = frac(p * .1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }
        float noise(float3 p)
        {
            float3 i = floor(p), f = frac(p);
            f = f * f * (3 - 2 * f);
            return lerp(lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x),
                lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                lerp(hash(i + float3(0,1,1)), hash(i + 1), f.x), f.y), f.z);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos / max(1, _Scale);
            float macro = noise(p * .65);
            float facets = noise(p * float3(2.4, 3.1, 2.2));
            float grain = noise(p * 31);
            float seams = smoothstep(.63,.77, noise(p * 2 + noise(p * 1.7)));
            float distanceFade = 1 - smoothstep(150, 900, distance(IN.worldPos, _WorldSpaceCameraPos));
            float tone = 1 + (macro - .5) * _Variation + (facets - .5) * .12;
            tone *= 1 - seams * .035;
            tone *= 1 + (grain - .5) * .12 * distanceFade;
            float cover = smoothstep(.65,.88, normalize(IN.worldNormal).y) * _Cover;
            cover *= smoothstep(.15,.6, noise(p * 1.4 + 13));
            o.Albedo = lerp(_Color.rgb * tone, _CoverColor.rgb * (.8 + .4 * macro), cover);
            o.Smoothness = .06;
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
