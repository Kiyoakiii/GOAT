Shader "GoatDescent/Sky"
{
    Properties
    {
        _SkyZenith ("Zenith Color", Color) = (0.3, 0.5, 0.8, 1)
        _SkyHorizon ("Horizon Color", Color) = (0.75, 0.87, 0.95, 1)
        _GroundColor ("Ground Color", Color) = (0.55, 0.6, 0.65, 1)
        _SunColor ("Sun Color", Color) = (1, 0.95, 0.8, 1)
        _SunDir ("Sun Direction", Vector) = (0.5, 0.8, 0.3, 0)
        _SunSize ("Sun Size", Range(0, 1)) = 0.08
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
            float _SunSize;

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

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float up = dir.y;

                fixed3 skyCol = lerp(_SkyHorizon.rgb, _SkyZenith.rgb, pow(saturate(up), 0.7));
                fixed3 groundCol = _GroundColor.rgb;
                fixed3 col = up >= 0.0 ? skyCol : groundCol;

                float sunDot = saturate(dot(normalize(dir), normalize(_SunDir.xyz)));
                float sun = pow(sunDot, 2000.0) * _SunColor.rgb;
                float glow = pow(sunDot, 8.0) * _SunColor.rgb * 0.35;
                col += sun + glow;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}