Shader "Goat Descent/Eagle Wind Ribbon"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Varying { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR;
                float3 world:TEXCOORD1; UNITY_FOG_COORDS(2) };
            Varying vert(Input v)
            {
                Varying o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex); return o;
            }
            fixed4 frag(Varying i):SV_Target
            {
                float edge=pow(saturate(1-abs(i.uv.y*2-1)),1.4);
                float ends=smoothstep(0,.12,i.uv.x)*(1-smoothstep(.72,1,i.uv.x));
                fixed4 c=i.color;
                c.a*=edge*ends*smoothstep(.2,.8,distance(i.world,_WorldSpaceCameraPos));
                UNITY_APPLY_FOG(i.fogCoord,c); return c;
            }
            ENDCG
        }
    }
}
