Shader "GoatDescent/TerracePeakWater"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (.2, .64, .7, 1)
        _Deep ("Deep", Color) = (.06, .32, .42, 1)
        _Foam ("Glints", Color) = (.68, .8, .8, 1)
        _HazeColor ("Haze", Color) = (.80, .80, .75, 1)
        _Haze ("Haze strength", Range(0, 1)) = .85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float4 _Shallow, _Deep, _Foam, _HazeColor;
        float _Haze;

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
            float3 p = IN.worldPos;
            float macro = Noise(p.xz * .009);
            float fine = Noise(p.xz * .038 + 3.7);
            float3 color = lerp(_Deep.rgb, _Shallow.rgb, saturate(macro * .5 + fine * .22 + .34));
            color = lerp(color, _Foam.rgb, smoothstep(.74, .95, fine) * .12);
            float depth = distance(_WorldSpaceCameraPos, p);
            float haze = smoothstep(1400.0, 3600.0, depth) * _Haze;
            float lum = dot(color, float3(.33, .5, .17));
            color = lerp(lerp(color, lum.xxx, haze * .5), _HazeColor.rgb, haze);
            o.Albedo = color;
            o.Emission = _Shallow.rgb * .08;
            o.Smoothness = .72;
            o.Metallic = .05;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
