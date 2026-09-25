Shader "GoatDescent/Soft Valley Mist"
{
    Properties { _Color ("Mist Color", Color) = (0.88,0.93,0.96,0.38) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.position = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2;
                float softEdge = saturate(1 - dot(p, p));
                float ripple = 0.82 + 0.18 * sin(i.uv.x * 19 + i.uv.y * 11);
                return fixed4(_Color.rgb, _Color.a * softEdge * softEdge * ripple);
            }
            ENDCG
        }
    }
}
