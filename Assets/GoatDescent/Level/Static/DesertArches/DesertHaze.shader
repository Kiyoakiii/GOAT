Shader "GoatDescent/DesertHaze"
{
    Properties
    {
        _HazeColor ("Haze Color", Color) = (.96, .89, .78, 1)
        _Strength ("Strength", Range(0, 1)) = .5
        _DistanceScale ("Distance scale", Float) = 1
    }
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
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float depth : TEXCOORD0;
            };
            fixed4 _HazeColor;
            float _Strength;
            float _DistanceScale;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.depth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float span = i.depth / max(.001, _DistanceScale);
                float near = saturate((span - 40.0) / 200.0);
                float far = saturate(1.0 - (span - 700.0) / 1000.0);
                return fixed4(_HazeColor.rgb, saturate(i.color.a * _Strength * near * far));
            }
            ENDCG
        }
    }
    FallBack Off
}
