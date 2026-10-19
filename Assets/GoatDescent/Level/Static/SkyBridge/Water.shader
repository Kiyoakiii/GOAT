Shader "GoatDescent/SkyBridgeWater"
{
    Properties
    {
        _Shallow ("Shallow turquoise", Color) = (.34, .86, .8, 1)
        _Deep ("Deep turquoise", Color) = (.03, .4, .46, 1)
        _Foam ("Foam", Color) = (.92, .98, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = .8
        _Smoothness ("Smoothness", Range(0, 1)) = .9
        _FlowSpeed ("Flow speed", Float) = .8
        _WaveScale ("Wave scale", Float) = .22
        _HazeColor ("Aerial haze", Color) = (.53, .74, .91, 1)
        _HazeStart ("Haze start", Float) = 700
        _HazeEnd ("Haze end", Float) = 2600
        _HazeStrength ("Haze strength", Range(0, 1)) = .5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        float4 _Shallow, _Deep, _Foam;
        float _Opacity, _Smoothness, _FlowSpeed, _WaveScale;
        float4 _HazeColor;
        float _HazeStart, _HazeEnd, _HazeStrength;
        struct Input { float3 worldPos; float3 worldNormal; float3 viewDir; float4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float t = _Time.y * _FlowSpeed;
            float wave = sin(p.z * _WaveScale + p.x * _WaveScale * .6 + t * 2.1);
            float rippleWave = sin(p.z * _WaveScale * 2.4 - p.x * _WaveScale * 1.3 - t * 2.8 + 1.7);
            float ripple = wave * .55 + rippleWave * .3;
            float3 up = normalize(IN.worldNormal);
            float3 drift = normalize(float3(1, 0, .35));
            float3 n = normalize(up + drift * wave * .1 + float3(-.35, 0, 1) * rippleWave * .08);
            float3 view = normalize(IN.viewDir);
            float fresnel = pow(1 - saturate(dot(n, view)), 3);
            float edge = saturate(IN.color.r);
            float foam = saturate(edge * 1.25 + pow(saturate(wave * .5 + .5), 10) * .35);
            float3 albedo = lerp(_Deep.rgb, _Shallow.rgb, saturate(.42 + ripple * .3 + edge * .25));
            albedo = lerp(albedo, _Foam.rgb, foam * .9);
            float camDist = length(_WorldSpaceCameraPos - p);
            float haze = saturate((camDist - _HazeStart) / max(1, _HazeEnd - _HazeStart));
            haze *= haze;
            float lum = dot(albedo, float3(.3, .59, .11));
            albedo = lerp(albedo, lerp(float3(lum, lum, lum), _HazeColor.rgb, .8), haze * _HazeStrength);
            o.Albedo = albedo;
            o.Emission = _Shallow.rgb * (.02 + fresnel * .08) + _Foam.rgb * foam * .05;
            o.Smoothness = _Smoothness;
            o.Metallic = .02;
            o.Alpha = saturate(_Opacity + fresnel * .12 + foam * .12);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
