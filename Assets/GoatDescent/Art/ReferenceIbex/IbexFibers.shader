Shader "GoatDescent/Ibex Fibers"
{
    Properties
    {
        _Color("Coat tint", Color) = (1,1,1,1)
        _MainTex("Coat color at strand root", 2D) = "white" {}
        _FurLayer("Fur layer", Float) = 1
        _FurGrowth("Coat regrowth", Range(0,1)) = 1
        _StaticCharge("Static charge", Range(0,1)) = 0
        _StaticFurOffset("Local bristle offset", Float) = 0
    }
    SubShader
    {
        // The underlying coat and horns supply the goat's shadow.
        // Thousands of sub-pixel fibers are too expensive in every shadow cascade.
        Tags { "RenderType"="Opaque" "ForceNoShadowCasting"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "../../Resources/GoatFurShading.cginc"
        struct Input { float4 tint; float2 uv_MainTex; };
        sampler2D _MainTex;
        fixed4 _Color;
        float _FurGrowth, _StaticCharge, _StaticFurOffset;
        void vert(inout appdata_full vertex, out Input result)
        {
            UNITY_INITIALIZE_OUTPUT(Input, result);
            result.tint = vertex.color;
            result.tint.a = vertex.texcoord1.x;
            vertex.vertex.xyz += vertex.normal * _StaticFurOffset;
        }
        void surf(Input input, inout SurfaceOutputStandard output)
        {
            if (_FurGrowth < .99999)
            {
                clip(_FurGrowth - .00001);
                clip(_FurGrowth - min(.9999, frac(dot(input.tint.rgb, float3(131.1, 47.7, 91.3)))));
            }
            output.Albedo = GoatFurColor(tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb, input.tint.rgb, input.tint.a);
            output.Metallic = 0;
            output.Smoothness = .18;
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
