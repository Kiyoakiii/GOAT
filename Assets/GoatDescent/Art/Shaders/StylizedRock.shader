Shader "GoatDescent/Stylized Rock"
{
    Properties
    {
        _BaseColor ("Shadow Rock", Color) = (0.12, 0.19, 0.24, 1)
        _AccentColor ("Sunlit Rock", Color) = (0.43, 0.48, 0.47, 1)
        _SnowColor ("High Snow", Color) = (0.78, 0.86, 0.86, 1)
        _SnowHeight ("Snow Height", Float) = 112
        _SnowBlend ("Snow Blend", Float) = 15
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

            fixed4 _BaseColor, _AccentColor, _SnowColor;
            float _SnowHeight, _SnowBlend;
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
                float grain = frac(sin(dot(floor(i.worldPos.xz * .23), float2(12.9898, 78.233))) * 43758.5453);
                float3 albedo = lerp(_BaseColor.rgb, _AccentColor.rgb, saturate(up * .85 + grain * .12));
                float snow = smoothstep(_SnowHeight - _SnowBlend, _SnowHeight + _SnowBlend, i.worldPos.y) * smoothstep(.42, .78, normal.y);
                albedo = lerp(albedo, _SnowColor.rgb, snow);
                float3 sun = normalize(_WorldSpaceLightPos0.xyz);
                float diffuse = saturate(dot(normal, sun)) * .7 + .3;
                return fixed4(albedo * diffuse, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
