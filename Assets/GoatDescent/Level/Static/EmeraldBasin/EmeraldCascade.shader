Shader "GoatDescent/EmeraldCascade"
{
    Properties
    {
        _Color ("Water", Color) = (.95, .99, 1, 1)
        _FoamColor ("Foam", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = .95
        _Flow ("Flow speed", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        float4 _Color, _FoamColor;
        float _Opacity, _Flow;
        struct Input { float3 worldPos; float3 worldNormal; float3 viewDir; float4 color : COLOR; };
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
            float crest = saturate(IN.color.r);
            float solid = saturate(IN.color.g * 1.2);
            float ang = atan2(p.z, p.x);
            float streak = Noise(float2(ang * 90.0, p.y * .32 - _Time.y * _Flow));
            float fine = .5 + .5 * sin(p.y * 1.9 + streak * 11.0 - _Time.y * _Flow * 3.2);
            float body = saturate(streak * 1.5 - .2);
            float foam = body * (.5 + .5 * fine);
            float edge = saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal)) * .55 + .55);
            float3 albedo = lerp(_Color.rgb, _FoamColor.rgb, foam);
            o.Albedo = albedo;
            o.Emission = albedo * (.12 + foam * .3 + crest * crest * 1.3) * (.4 + .6 * solid) + crest * crest * .3;
            o.Smoothness = .6;
            o.Metallic = 0;
            o.Alpha = saturate(_Opacity * (.2 + .28 * foam + .8 * crest) * (.75 + .25 * edge) * (.25 + .75 * solid));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
