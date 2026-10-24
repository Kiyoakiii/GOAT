Shader "GoatDescent/Ibex Fibers"
{
    Properties
    {
        _Color("Coat tint", Color) = (1,1,1,1)
        _FurLayer("Fur layer", Float) = 1
        _FurGrowth("Coat regrowth", Range(0,1)) = 1
        _StaticCharge("Static charge", Range(0,1)) = 0
        _StaticFurOffset("Local bristle offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        struct Input { float4 tint; };
        fixed4 _Color;
        float _FurGrowth, _StaticCharge, _StaticFurOffset;
        void vert(inout appdata_full vertex, out Input result)
        {
            UNITY_INITIALIZE_OUTPUT(Input, result);
            result.tint = vertex.color;
            vertex.vertex.xyz += vertex.normal * _StaticFurOffset;
        }
        void surf(Input input, inout SurfaceOutputStandard output)
        {
            if (_FurGrowth < .99999)
            {
                clip(_FurGrowth - .00001);
                clip(_FurGrowth - min(.9999, frac(dot(input.tint.rgb, float3(131.1, 47.7, 91.3)))));
            }
            output.Albedo = input.tint.rgb * _Color.rgb;
            output.Metallic = 0;
            output.Smoothness = 0.08;
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
