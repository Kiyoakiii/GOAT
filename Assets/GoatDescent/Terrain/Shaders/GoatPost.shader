Shader "GoatDescent/ScenePost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _BloomThreshold ("Bloom Threshold", Range(0, 1)) = .75
        _BloomIntensity ("Bloom Intensity", Range(0, 2)) = .55
        _Vignette ("Vignette", Range(0, 1)) = .35
        _Saturation ("Saturation", Range(0, 2)) = 1.15
        _Contrast ("Contrast", Range(.5, 1.5)) = 1.08
        _WarmTint ("Warm Tint", Color) = (1.04, 1, .93, 1)
        _CoolTint ("Cool Tint", Color) = (.95, .99, 1.05, 1)
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBright
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _BloomThreshold;
            fixed4 fragBright(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float brightness = max(c.r, max(c.g, c.b));
                float contribution = max(0.0, brightness - _BloomThreshold) / max(brightness, .0001);
                return c * contribution;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBlur
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Horizontal;
            fixed4 fragBlur(v2f_img i) : SV_Target
            {
                float2 dir = _Horizontal > .5 ? float2(_MainTex_TexelSize.x, 0) : float2(0, _MainTex_TexelSize.y);
                fixed4 c = tex2D(_MainTex, i.uv) * .227;
                c += tex2D(_MainTex, i.uv + dir * 1.384) * .316;
                c += tex2D(_MainTex, i.uv - dir * 1.384) * .316;
                c += tex2D(_MainTex, i.uv + dir * 3.23) * .070;
                c += tex2D(_MainTex, i.uv - dir * 3.23) * .070;
                return c;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragCompose
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BloomTex;
            float _BloomIntensity;
            float _Vignette;
            float _Saturation;
            float _Contrast;
            fixed4 _WarmTint;
            fixed4 _CoolTint;
            fixed4 fragCompose(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed3 bloom = tex2D(_BloomTex, i.uv).rgb;
                c.rgb += bloom * _BloomIntensity;
                float luminance = dot(c.rgb, float3(.299, .587, .114));
                c.rgb = lerp(luminance.xxx, c.rgb, _Saturation);
                c.rgb = (c.rgb - .5) * _Contrast + .5;
                float tone = saturate((luminance - .35) / .4);
                c.rgb *= lerp(_CoolTint.rgb, _WarmTint.rgb, tone);
                float2 delta = i.uv - .5;
                c.rgb *= 1 - _Vignette * pow(dot(delta, delta) * 2.2, 1.4);
                return c;
            }
            ENDCG
        }
    }
}
