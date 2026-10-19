Shader "GoatDescent/SkyBridgeWaterfall"
{
    Properties
    {
        _Color ("Water", Color) = (.9, .97, 1, 1)
        _Core ("Sparkle", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = .9
        _Speed ("Fall speed", Float) = 2.2
        _Streaks ("Streak count", Float) = 9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Color, _Core;
            float _Opacity, _Speed, _Streaks;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float hash12(float2 p) { p = frac(p * 123.34); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash12(i), hash12(i + float2(1, 0)), f.x),
                    lerp(hash12(i + float2(0, 1)), hash12(i + 1), f.x), f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float fall = frac(i.uv.y * 1.2 - _Time.y * _Speed);
                float streak = vnoise(float2(i.uv.x * _Streaks * 3.5, i.uv.y * 3.2 - _Time.y * _Speed * 2.2));
                float stream = vnoise(float2(i.uv.x * _Streaks, i.uv.y * 1.5 + _Time.y * .3));
                float edge = smoothstep(0, .14, i.uv.x) * (1 - smoothstep(.86, 1, i.uv.x));
                float alpha = _Opacity * edge * saturate(.45 + streak * .7) * saturate(.6 + stream * .5);
                alpha *= lerp(1, .68, i.uv.y);
                float3 col = lerp(_Color.rgb, _Core.rgb, saturate(streak * .8 + (1 - fall) * .25));
                return fixed4(col, saturate(alpha));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
