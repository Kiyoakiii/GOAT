Shader "GoatDescent/First Person Body"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal scale", Float) = 1
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Glossiness("Smoothness", Range(0,1)) = .11
        _GlossMapScale("Smoothness map scale", Range(0,1)) = 1
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 1
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionColor("Emission tint", Color) = (0,0,0,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = .5
        _UseVertexColor("Vertex coat color", Float) = 0
        _Cull("Culling", Float) = 2
        _BodyFadeNear("Hide close surfaces", Float) = .6
        _BodyFadeFar("Show far surfaces", Float) = .95
        _FurGrowth("Coat regrowth", Range(0,1)) = 1
        _FurLayer("Fur layer", Float) = 0
        _CoatSurface("Coat surface", Float) = 0
        _CoatMask("Coat mask", 2D) = "white" {}
        _SkinColor("Bare skin", Color) = (.88,.66,.55,1)
        _StaticCharge("Static charge", Float) = 0
        _StaticFurOffset("Local bristle offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull [_Cull]
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 4.0
        #pragma multi_compile_local _ _NORMALMAP
        #pragma multi_compile_local _ _METALLICGLOSSMAP
        #pragma multi_compile_local _ _ALPHATEST_ON
        #pragma multi_compile_local _ _EMISSION
        #include "UnityStandardUtils.cginc"
        #include "GoatFurShading.cginc"
        sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _OcclusionMap, _EmissionMap;
        sampler2D _CoatMask;
        fixed4 _SkinColor;
        float _FurGrowth, _FurLayer, _CoatSurface, _StaticCharge, _StaticFurOffset;
        fixed4 _Color, _EmissionColor;
        half _BumpScale, _Metallic, _Glossiness, _GlossMapScale, _OcclusionStrength, _Cutoff;
        float _BodyFadeNear, _BodyFadeFar, _UseVertexColor;
        struct Input { float2 uv_MainTex; float3 worldPos; float4 screenPos; float4 tint; float limb; };
        void vert(inout appdata_full vertex, out Input result)
        {
            UNITY_INITIALIZE_OUTPUT(Input, result);
            result.tint = vertex.color;
            result.limb = vertex.texcoord2.x;
            result.tint.a = vertex.texcoord1.x;
            vertex.vertex.xyz += vertex.normal * (_StaticFurOffset * _FurLayer);
        }
        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float visibility = smoothstep(_BodyFadeNear, _BodyFadeFar,
                distance(input.worldPos, _WorldSpaceCameraPos));
            visibility = lerp(visibility, 1, step(.35, input.limb));
            float2 pixel = floor(input.screenPos.xy / input.screenPos.w * _ScreenParams.xy);
            float threshold = frac(52.9829189 * frac(dot(pixel, float2(.06711056, .00583715))));
            clip(visibility - lerp(.0001, .9999, threshold));
            if (_FurLayer > .5 && _FurGrowth < .99999)
            {
                clip(_FurGrowth - .00001);
                clip(_FurGrowth - min(.9999, frac(dot(input.tint.rgb, float3(131.1,47.7,91.3)))));
            }
            fixed4 color = tex2D(_MainTex, input.uv_MainTex) * _Color;
            #ifdef _ALPHATEST_ON
                clip(color.a - _Cutoff);
            #endif
            output.Albedo = lerp(color.rgb, GoatFurColor(color.rgb, input.tint.rgb, input.tint.a), _UseVertexColor);
            #ifdef _NORMALMAP
                output.Normal = UnpackScaleNormal(tex2D(_BumpMap, input.uv_MainTex), _BumpScale);
            #endif
            #ifdef _METALLICGLOSSMAP
                half4 metal = tex2D(_MetallicGlossMap, input.uv_MainTex);
                output.Metallic = metal.r;
                output.Smoothness = metal.a * _GlossMapScale;
            #else
                output.Metallic = _Metallic;
                output.Smoothness = _Glossiness;
            #endif
            output.Occlusion = lerp(1, tex2D(_OcclusionMap, input.uv_MainTex).g, _OcclusionStrength);
            #ifdef _EMISSION
                output.Emission = tex2D(_EmissionMap, input.uv_MainTex).rgb * _EmissionColor.rgb;
            #endif
            float bare = (1-saturate(_FurGrowth)) * _CoatSurface * tex2D(_CoatMask,input.uv_MainTex).r;
            output.Albedo = lerp(output.Albedo,_SkinColor.rgb,bare);
            output.Normal = normalize(lerp(output.Normal,float3(0,0,1),bare));
            output.Metallic *= 1-bare;
            output.Smoothness = lerp(output.Smoothness,.32,bare);
            output.Occlusion = lerp(output.Occlusion,1,bare);
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
