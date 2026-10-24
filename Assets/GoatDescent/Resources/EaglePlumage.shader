Shader "Goat Descent/Eagle Plumage"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        struct Input { float4 featherColor; };
        fixed4 _Color;
        half _Glossiness;
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.featherColor = v.color;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.featherColor.rgb * _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
