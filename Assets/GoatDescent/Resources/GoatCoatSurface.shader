Shader "GoatDescent/Coat Regrowth Surface"
{
    Properties
    {
        _MainTex ("Original coat", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal scale", Float) = 1
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .11
        _GlossMapScale ("Smoothness map", Float) = 1
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Float) = 1
        _CoatMask ("Coat, excluding eyes and hooves", 2D) = "white" {}
        _CoatSurface ("Coat surface", Float) = 1
        _FurGrowth ("Coat regrowth", Range(0,1)) = 1
        _SkinColor ("Bare skin", Color) = (.88,.66,.55,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_local _ _NORMALMAP
        #pragma multi_compile_local _ _METALLICGLOSSMAP
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _OcclusionMap, _CoatMask;
        fixed4 _Color, _SkinColor;
        float _FurGrowth, _BumpScale, _Metallic, _Glossiness, _GlossMapScale, _OcclusionStrength;
        struct Input { float2 uv_MainTex; };
        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed3 original = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb;
            float bare = (1 - saturate(_FurGrowth)) * tex2D(_CoatMask,input.uv_MainTex).r;
            output.Albedo = lerp(original, _SkinColor.rgb, bare);
            #ifdef _NORMALMAP
            output.Normal = UnpackScaleNormal(tex2D(_BumpMap,input.uv_MainTex),_BumpScale * (1-bare));
            #endif
            #ifdef _METALLICGLOSSMAP
            half4 metal = tex2D(_MetallicGlossMap,input.uv_MainTex);
            output.Metallic = metal.r * (1-bare); output.Smoothness = lerp(metal.a*_GlossMapScale,.32,bare);
            #else
            output.Metallic = _Metallic * (1-bare); output.Smoothness = lerp(_Glossiness,.32,bare);
            #endif
            output.Occlusion = lerp(1,tex2D(_OcclusionMap,input.uv_MainTex).g,_OcclusionStrength * (1-bare));
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
