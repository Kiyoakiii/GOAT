Shader "GoatDescent/Sky"
{
    Properties
    {
        _SkyZenith ("Zenith Color", Color) = (0.36, 0.58, 0.85, 1)
        _SkyHorizon ("Horizon Color", Color) = (0.82, 0.9, 0.97, 1)
        _GroundColor ("Ground Color", Color) = (0.55, 0.6, 0.65, 1)
        _SunColor ("Sun Color", Color) = (1, 0.93, 0.72, 1)
        _SunDir ("Sun Direction", Vector) = (0.5, 0.8, 0.3, 0)
        _SunSize ("Sun Size", Range(0, 1)) = 0.09
        _CloudColor ("Cloud Color", Color) = (1, 0.98, 0.94, 1)
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            float4 _SkyZenith;
            float4 _SkyHorizon;
            float4 _GroundColor;
            float4 _SunColor;
            float4 _SunDir;
            float4 _CloudColor;
            float _SunSize;
            float _CloudDensity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash12(float2 p) { p = frac(p * 0.1031); p += dot(p, p.yx + 33.33); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash12(i), b = hash12(i + float2(1, 0));
                float c = hash12(i + float2(0, 1)), d = hash12(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float up = dir.y;

                fixed3 skyCol = lerp(_SkyHorizon.rgb, _SkyZenith.rgb, pow(saturate(up), 0.6));
                fixed3 groundCol = _GroundColor.rgb;
                fixed3 col = up >= 0.0 ? skyCol : groundCol;

                float sunDot = saturate(dot(normalize(dir), normalize(_SunDir.xyz)));
                float sunCore = pow(sunDot, 300.0) * _SunColor.rgb;
                float glow = pow(sunDot, 6.0) * _SunColor.rgb * 0.4;
                col += sunCore + glow;

                float cloudBand = saturate(1.0 - abs(up) * 4.0);
                float2 cloudUv = dir.xz / max(0.05, abs(dir.y)) * 0.4 + float2(3.7, 9.2);
                float cloudShape = vnoise(cloudUv) * 0.6 + vnoise(cloudUv * 3.1) * 0.4;
                float clouds = smoothstep(0.52, 0.75, cloudShape) * cloudBand * _CloudDensity;
                col = lerp(col, _CloudColor.rgb, clouds * 0.85);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}