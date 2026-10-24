Shader "GoatDescent/Brightness"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Brightness ("Brightness", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Brightness;
            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                color.rgb = pow(max(color.rgb, 0), 1.0 / max(_Brightness, 0.01));
                return color;
            }
            ENDCG
        }
    }
}
