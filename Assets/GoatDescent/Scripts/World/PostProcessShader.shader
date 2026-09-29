Shader "GoatDescent/PostProcess"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.0
        _Contrast ("Contrast", Range(0.5, 1.5)) = 1.1
        _Saturation ("Saturation", Range(0, 2)) = 1.15
        _Warmth ("Warmth", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            sampler2D _MainTex;
            float _Exposure;
            float _Contrast;
            float _Saturation;
            float _Warmth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed3 Sat(fixed3 c, float amount)
            {
                float luma = dot(c, fixed3(0.299, 0.587, 0.114));
                return lerp(fixed3(luma, luma, luma), c, amount);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 c = tex2D(_MainTex, i.uv).rgb * _Exposure;

                c = Sat(c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;

                c.r += _Warmth * 0.05;
                c.b -= _Warmth * 0.04;

                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
}