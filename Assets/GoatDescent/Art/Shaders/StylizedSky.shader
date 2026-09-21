Shader "GoatDescent/Stylized Sky"
{
    Properties
    {
        _HorizonColor ("Horizon", Color) = (0.38, 0.68, 0.86, 1)
        _ZenithColor ("Zenith", Color) = (0.06, 0.22, 0.48, 1)
        _SunColor ("Sun Glow", Color) = (1.0, 0.72, 0.38, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.4, 0.6, 0.5, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            Cull Front
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _HorizonColor, _ZenithColor, _SunColor;
            float4 _SunDirection;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.direction = normalize(v.vertex.xyz);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float height = smoothstep(.22, .9, saturate(i.direction.y * .5 + .5));
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, height);
                float sun = pow(saturate(dot(normalize(i.direction), normalize(_SunDirection.xyz))), 72.0);
                float halo = pow(saturate(dot(normalize(i.direction), normalize(_SunDirection.xyz))), 8.0) * .22;
                sky += _SunColor.rgb * (sun + halo);
                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
