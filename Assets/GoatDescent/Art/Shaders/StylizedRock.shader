Shader "GoatDescent/Stylized Rock"
{
    Properties
    {
        _BaseColor ("Shadow Rock", Color) = (0.12, 0.19, 0.24, 1)
        _AccentColor ("Sunlit Rock", Color) = (0.43, 0.48, 0.47, 1)
        _SnowColor ("High Snow", Color) = (0.78, 0.86, 0.86, 1)
        _SnowHeight ("Snow Height", Float) = 112
        _SnowBlend ("Snow Blend", Float) = 15
        _MossColor ("Moss Color", Color) = (0.19, 0.35, 0.08, 1)
        _MossStrength ("Moss Strength", Range(0, 1)) = 0.35
        _MossLow ("Moss Low Height", Float) = 5
        _MossHigh ("Moss High Height", Float) = 95
        _MossFade ("Moss Edge Softness", Float) = 8
        _RimStrength ("Cold Rim Strength", Range(0, 1)) = 0.12
        _DetailTex ("Pixel Detail", 2D) = "gray" {}
        _DetailScale ("Pixel Detail Scale", Float) = 0.08
        _DetailStrength ("Pixel Detail Strength", Range(0, 1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _BaseColor, _AccentColor, _SnowColor, _MossColor;
            float _SnowHeight, _SnowBlend, _MossStrength, _MossLow, _MossHigh, _MossFade, _RimStrength, _DetailScale, _DetailStrength;
            sampler2D _DetailTex;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Derivatives deliberately create a faceted normal even when the source mesh shares vertices.
                float3 normal = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));
                float3 viewDirection = _WorldSpaceCameraPos - i.worldPos;
                if (dot(normal, viewDirection) < 0.0)
                    normal = -normal;
                float up = saturate(normal.y * .55 + .45);
                // Coarse world-space cells suggest low-resolution painted detail without importing another game's textures.
                float grain = frac(sin(dot(floor(i.worldPos.xz * .38), float2(12.9898, 78.233))) * 43758.5453);
                float3 albedo = lerp(_BaseColor.rgb, _AccentColor.rgb, saturate(up * .82 + grain * .16));
                float mossRise = smoothstep(_MossLow, _MossLow + _MossFade, i.worldPos.y);
                float mossFall = 1.0 - smoothstep(_MossHigh - _MossFade, _MossHigh, i.worldPos.y);
                float moss = mossRise * mossFall * smoothstep(.30, .88, normal.y) * _MossStrength;
                albedo = lerp(albedo, _MossColor.rgb, moss);
                float snow = smoothstep(_SnowHeight - _SnowBlend, _SnowHeight + _SnowBlend, i.worldPos.y) * smoothstep(.42, .78, normal.y);
                albedo = lerp(albedo, _SnowColor.rgb, snow);
                float pixelDetail = tex2D(_DetailTex, i.worldPos.xz * _DetailScale).r;
                albedo *= lerp(1.0, .78 + pixelDetail * .44, _DetailStrength);
                float3 sun = normalize(_WorldSpaceLightPos0.xyz);
                float diffuse = saturate(dot(normal, sun)) * .7 + .3;
                diffuse = floor(diffuse * 4.0) / 4.0 + .16;
                float rim = pow(1.0 - saturate(dot(normal, normalize(viewDirection))), 3.0) * _RimStrength;
                return fixed4(albedo * diffuse + rim * _AccentColor.rgb * .28, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
