Shader "GoatDescent/Overcast Valley Sky"
{
    Properties { _Horizon ("Horizon", Color)=(.82,.88,.91,1) _Zenith ("Zenith",Color)=(.43,.57,.69,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Horizon, _Zenith;
            struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            v2f vert(float4 v:POSITION) { v2f o; o.pos=UnityObjectToClipPos(v); o.direction=v.xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);
                float blend=pow(saturate(d.y),.6);
                float clouds=sin(d.x*8+d.z*7)*sin(d.z*13-d.y*12)*.015;
                return float4(lerp(_Horizon.rgb,_Zenith.rgb,blend)+clouds,1);
            }
            ENDCG
        }
    }
}
