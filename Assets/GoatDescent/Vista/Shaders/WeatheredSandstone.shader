Shader "GoatDescent/Weathered Sandstone"
{
    Properties
    {
        _StoneColor ("Warm sandstone", Color) = (.80,.62,.42,1)
        _ShadowStone ("Weathered seams", Color) = (.34,.26,.18,1)
        _MossColor ("Climbing moss", Color) = (.30,.42,.16,1)
        _RampSteps ("Light Steps", Range(2, 6)) = 3
        _RimStrength ("Rim Light", Range(0, 1)) = 0.5
        _RimColor ("Rim Color", Color) = (1,.9,.72,1)
        _MossAmount ("Moss Amount", Range(0, 1)) = 0.55
        _StratumStrength ("Strata Strength", Range(0, 1)) = 0.45
        _ShadowTint ("Shadow Tint", Color) = (.62,.47,.32,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0
        fixed4 _StoneColor, _ShadowStone, _MossColor, _RimColor, _ShadowTint;
        float _RampSteps, _RimStrength, _MossAmount, _StratumStrength;
        struct Input { float3 worldPos; float4 color : COLOR; };

        float hash31(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
        float noise3(float3 p)
        {
            float3 i=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+1),f.x),f.y),f.z);
        }

        half4 LightingToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = saturate(dot(s.Normal, lightDir));
            half steps = max(2.0, _RampSteps);
            half ramp = floor(ndl * steps) / (steps - 1.0);
            half3 direct = _LightColor0.rgb * ramp * atten;
            half3 ambient = unity_AmbientSky.rgb * 0.85 + unity_AmbientGround.rgb * 0.15;
            half fresnel = pow(1.0 - saturate(dot(s.Normal, viewDir)), 2.5);
            half3 rim = _RimColor.rgb * fresnel * _RimStrength * saturate(ndl * 2.0);
            half3 lit = s.Albedo * (direct + ambient * 0.7);
            half3 shadowed = s.Albedo * _ShadowTint.rgb * (ambient * 0.55);
            half3 col = lerp(shadowed, lit, ramp) + rim;
            return half4(col, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 p=IN.worldPos;
            float broad=noise3(p*.035), seam=noise3(p*float3(.22,.014,.22)), fine=noise3(p*.48);
            float layer=sin(p.y*.42+noise3(p*.025)*5), band=smoothstep(.72,.94,layer);
            float stain=smoothstep(.3,.68,seam);

            float3 stone=lerp(_ShadowStone.rgb,_StoneColor.rgb,stain*.78+.22);
            stone*=0.82+broad*.28+fine*.14;
            stone=lerp(stone, stone*.85, band*_StratumStrength);

            float heightTone=IN.color.g;
            stone=lerp(stone, _StoneColor.rgb*1.12, smoothstep(.55,.85,heightTone));
            stone=lerp(stone, _ShadowStone.rgb*1.25, smoothstep(.15,.0,heightTone));

            float mossField=noise3(p*.064)*.6+noise3(p*.19)*.23;
            float moss=smoothstep(.52,.74,mossField+max(0,IN.color.g-.55)*.25)*_MossAmount;
            float3 mossTint=_MossColor.rgb*(.72+broad*.5);
            o.Albedo=lerp(stone, mossTint, moss);

            o.Normal=float3(0,0,1);
            o.Alpha=1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}