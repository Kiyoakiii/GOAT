Shader "GoatDescent/SkyBridgeMist"
{
    Properties
    {
        _Color ("Mist light", Color) = (.95, .97, 1, 1)
        _Shadow ("Mist shade", Color) = (.66, .75, .82, 1)
        _Opacity ("Opacity", Range(0, 1)) = .3
        _NoiseScale ("Billow scale", Float) = .035
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Color, _Shadow;
            float _Opacity, _NoiseScale;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float hash31(float3 p) { p = frac(p * .1031); p += dot(p, p.yzx + 33.33); return frac((p.x + p.y) * p.z); }
            float noise3(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash31(i), hash31(i + float3(1, 0, 0)), f.x),
                    lerp(hash31(i + float3(0, 1, 0)), hash31(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(hash31(i + float3(0, 0, 1)), hash31(i + float3(1, 0, 1)), f.x),
                    lerp(hash31(i + float3(0, 1, 1)), hash31(i + 1), f.x), f.y), f.z);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float facing = saturate(dot(normalize(i.normal), view));
                float3 q = i.world * _NoiseScale + float3(_Time.y * .01, 0, 0);
                float n = noise3(q) * .65 + noise3(q * 2.3) * .35;
                float alpha = _Opacity * pow(facing, .55) * saturate(.35 + n * .9);
                float3 col = lerp(_Shadow.rgb, _Color.rgb, saturate(facing * .6 + n * .3));
                return fixed4(col, saturate(alpha));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
