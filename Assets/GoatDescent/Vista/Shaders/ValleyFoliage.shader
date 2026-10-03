Shader "GoatDescent/Valley Foliage"
{
    Properties
    {
        _Color ("Canopy tint", Color) = (.34,.55,.20,1)
        _LeafDark ("Shaded Leaves", Color) = (.18,.30,.13,1)
        _LeafLight ("Sunlit Leaves", Color) = (.55,.78,.30,1)
        _RampSteps ("Light Steps", Range(2, 6)) = 2
        _RimStrength ("Rim Light", Range(0, 1)) = 0.35
        _RimColor ("Rim Color", Color) = (1,.95,.72,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf ToonFoliage fullforwardshadows
        #pragma target 3.0
        fixed4 _Color, _LeafDark, _LeafLight, _RimColor;
        float _RampSteps, _RimStrength;
        struct Input { float3 worldPos; float4 color : COLOR; };

        half4 LightingToonFoliage(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = saturate(dot(s.Normal, lightDir));
            half steps = max(2.0, _RampSteps);
            half ramp = floor(ndl * steps) / (steps - 1.0);
            half3 direct = _LightColor0.rgb * ramp * atten;
            half3 ambient = unity_AmbientSky.rgb * 0.8 + unity_AmbientGround.rgb * 0.2;
            half fresnel = pow(1.0 - saturate(dot(s.Normal, viewDir)), 2.0);
            half3 rim = _RimColor.rgb * fresnel * _RimStrength * saturate(ndl * 1.5);
            half3 lit = s.Albedo * (direct + ambient * 0.65);
            half3 shadowed = s.Albedo * _LeafDark.rgb / max(0.01, _LeafDark.rgb) * (ambient * 0.5);
            half3 col = lerp(shadowed, lit, ramp) + rim;
            return half4(col, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 p=IN.worldPos;
            float freckles=frac(sin(dot(floor(p*2.7),float3(12.9898,78.233,38.719)))*43758.5453);
            float vertical=IN.color.b;
            float3 base=lerp(_LeafDark.rgb, _LeafLight.rgb, saturate(vertical*1.4));
            o.Albedo=IN.color.rgb*base*1.5*(.9+.14*freckles);
            o.Normal=float3(0,0,1);
            o.Alpha=1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}