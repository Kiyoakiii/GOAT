Shader "GoatDescent/FrozenRiftMist"
{
    Properties
    {
        _Color ("Mist", Color) = (.87, .92, .99, 1)
        _Strength ("Strength", Range(0, 1)) = .46
        _Softness ("Softness", Range(.1, 1)) = .62
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-30" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Color;
            float _Strength, _Softness;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
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
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float radial = saturate(1 - dot(p, p));
                radial = pow(radial, _Softness);
                float puff = Noise(i.uv * 4.2) * .45 + Noise(i.uv * 11.5) * .3 + .35;
                float alpha = saturate(radial * puff) * _Strength;
                fixed4 color = fixed4(_Color.rgb, alpha);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }            ENDCG
        }
    }
    FallBack "Diffuse"
}
