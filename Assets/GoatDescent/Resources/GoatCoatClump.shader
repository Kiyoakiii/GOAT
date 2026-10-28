Shader "GoatDescent/Coat Clump"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct In { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Out { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            Out vert(In input) { Out result; result.pos=UnityObjectToClipPos(input.vertex); result.color=input.color; result.uv=input.uv; return result; }
            fixed4 frag(Out input):SV_Target
            {
                float2 p=input.uv*2-1;
                float x=p.x+.14*sin(p.y*4);
                float fibres=exp(-pow((x-.22)*14,2))+exp(-pow(x*16,2))+exp(-pow((x+.22)*14,2));
                float clump=exp(-dot(p*float2(2.3,1.3),p*float2(2.3,1.3)))*.5;
                input.color.a*=saturate(fibres+clump)*(1-smoothstep(.3,1,abs(p.y)));
                return input.color;
            }
            ENDCG
        }
    }
}
