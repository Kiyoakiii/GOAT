Shader "GoatDescent/Beer Puddle"
{
    Properties
    {
        _Color("Water tint",Color)=(.12,.64,.39,.8)
        _SurfaceStyle("Surface: water / foam / border",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _SurfaceStyle;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata v) {v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float r=length(p);
                float fade=1-smoothstep(.84,1,r);
                if(_SurfaceStyle>1.5) return fixed4(_Color.rgb,_Color.a*fade);
                if(_SurfaceStyle>.5)
                {
                    float pearl=pow(saturate(1-length(p-float2(-.25,.25))),3);
                    return fixed4(lerp(_Color.rgb,1,pearl*.35),_Color.a*fade*.8);
                }
                float flow=sin(p.x*5+_Time.y*.22)*cos(p.y*6-_Time.y*.18);
                float ripple=pow(saturate(sin(r*25-_Time.y*.9+flow*.35)),18);
                float sheen=pow(saturate(1-length((p-float2(-.3,.25))*float2(.85,1.5))),3);
                float rim=smoothstep(.65,.9,r)*(1-smoothstep(.9,1,r));
                float3 deep=_Color.rgb*lerp(.74,1.03,smoothstep(.05,.9,r));
                float3 light=lerp(_Color.rgb,1,.65);
                float3 water=lerp(deep,light,.1+flow*.025+ripple*.13+sheen*.23+rim*.18);
                return fixed4(water,_Color.a*fade);
            }
            ENDCG
        }
    }
}
