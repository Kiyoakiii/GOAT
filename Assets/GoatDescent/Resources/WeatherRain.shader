Shader "Goat Descent/Weather Rain"
{
    Properties { _Splash ("Square splash", Float) = 0 }
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
            float _Splash;
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
                float2 p=i.uv*2-1;
                float shape;
                if (_Splash > .5)
                    shape=1-smoothstep(.82,1,max(abs(p.x),abs(p.y)));
                else
                    shape=pow(saturate(1-abs(p.x)),1.5)*pow(saturate(1-abs(p.y)),.6);
                fixed4 c=i.color;
                c.a*=shape*smoothstep(.35,1.2,distance(i.world,_WorldSpaceCameraPos));
                UNITY_APPLY_FOG(i.fogCoord,c); return c;
            }
            ENDCG
        }
    }
}
