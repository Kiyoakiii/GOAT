Shader "GoatDescent/Mountain"
{
    Properties
    {
        _GrassTex ("Grass", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        _SnowTex ("Snow", 2D) = "white" {}
        _Tiling ("Texture Tiling", Float) = 0.02
        _GrassEnd ("Grass Fades Below Height", Range(0, 1)) = 0.18
        _SnowStart ("Snow Starts Above Height", Range(0, 1)) = 0.72
        _Fade ("Blend Softness", Range(0.01, 0.4)) = 0.1
        _Steep ("Steepness Cutoff", Range(0, 1)) = 0.25
        _HeightMin ("Height Min (world)", Float) = -10
        _HeightMax ("Height Max (world)", Float) = 260
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _GrassTex;
        sampler2D _RockTex;
        sampler2D _SnowTex;
        float _Tiling;
        float _GrassEnd;
        float _SnowStart;
        float _Fade;
        float _Steep;
        float _HeightMin;
        float _HeightMax;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float h = saturate((IN.worldPos.y - _HeightMin) / (_HeightMax - _HeightMin));
            float3 n = normalize(IN.worldNormal);
            float slope = 1.0 - saturate(n.y);

            float snow = smoothstep(_SnowStart, _SnowStart + _Fade, h);
            float steep = smoothstep(_Steep, _Steep + 0.2, slope);
            float grass = (1.0 - smoothstep(_GrassEnd, _GrassEnd + _Fade, h)) * (1.0 - steep);
            float rock = 1.0 - snow - grass;
            rock = saturate(rock);

            float2 uv = IN.worldPos.xz * _Tiling;
            float3 grassCol = tex2D(_GrassTex, uv).rgb;
            float3 rockCol = tex2D(_RockTex, uv).rgb;
            float3 snowCol = tex2D(_SnowTex, uv).rgb;

            o.Albedo = grassCol * grass + rockCol * rock + snowCol * snow;
            o.Metallic = 0.0;
            o.Smoothness = rock * 0.2 + grass * 0.1 + snow * 0.6;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}