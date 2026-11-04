Shader "GoatDescent/FrozenRiftFarRidges"
{
    Properties
    {
        _Color ("Snow", Color) = (.9, .93, .97, 1)
        _HazeColor ("Horizon", Color) = (.66, .8, .94, 1)
        _SunDir ("Sun direction", Vector) = (.5, .5, .5, 0)
        _Haze ("Aerial perspective", Range(0, 1)) = .62
        _Bright ("Lit amount", Range(0, 1)) = .24
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Color, _HazeColor, _SunDir;
            float _Haze, _Bright;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; float3 normal : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float light = saturate(dot(n, normalize(_SunDir.xyz)));
                float3 col = _Color.rgb * ((1 - _Bright) + _Bright * (.35 + .95 * light));
                col = lerp(col, col * float3(.72, .87, 1.2), saturate(1 - light * 2.2));
                float haze = saturate((length(i.worldPos.xz) - 900) / 1400) * _Haze;
                col = lerp(col, dot(col, float3(.3, .59, .11)).xxx, haze * .3);
                col = lerp(col, _HazeColor.rgb * float3(.96, .99, 1.04), haze);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
