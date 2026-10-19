Shader "GoatDescent/EmeraldWater"
{
    Properties
    {
        _ShallowColor ("Shallow water", Color) = (.05, .30, .24, 1)
        _DeepColor ("Deep water", Color) = (.01, .09, .10, 1)
        _FoamColor ("Sky reflection", Color) = (.62, .78, .80, 1)
        _Center ("Basin center", Vector) = (0, 0, 0, 0)
        _Radius ("Lake radius", Float) = 172
        _Smoothness ("Smoothness", Range(0, 1)) = .55
        _WaveScale ("Wave scale", Float) = .07
        _WaveSpeed ("Wave speed", Float) = .6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        float4 _ShallowColor, _DeepColor, _FoamColor, _Center;
        float _Radius, _Smoothness, _WaveScale, _WaveSpeed;
        struct Input { float3 worldPos; float3 worldNormal; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float d = saturate(distance(p.xz, _Center.xz) / max(1.0, _Radius));
            float swirl = sin(p.x * _WaveScale + p.z * _WaveScale * .8 + _Time.y * _WaveSpeed);
            float depth = smoothstep(.50, .96, d);
            float3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, depth * depth);
            col *= 1 + swirl * .04;
            float3 n = normalize(IN.worldNormal);
            float ndv = saturate(dot(n, normalize(IN.viewDir)));
            float fresnel = pow(1 - ndv, 5);
            float3 refl = _FoamColor.rgb * .22;
            col = lerp(col, _FoamColor.rgb, saturate(1 - depth * 8) * .16);
            o.Albedo = col;
            o.Emission = lerp(col * .03, refl, fresnel);
            o.Smoothness = .45 + .30 * fresnel;
            o.Metallic = 0;
            o.Alpha = .97;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
