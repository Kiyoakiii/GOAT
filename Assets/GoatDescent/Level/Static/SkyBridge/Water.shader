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
        float Hash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }
        float Noise(float2 p)
        {
            float2 c = floor(p), t = frac(p);
            t = t * t * (3 - 2 * t);
            return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), t.x),
                lerp(Hash(c + float2(0, 1)), Hash(c + 1), t.x), t.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float camDist = length(_WorldSpaceCameraPos - p);
            float t = _Time.y * _FlowSpeed;
            float2 flow = normalize(float2(.35, 1));
            float travel = dot(p.xz, flow);
            float broad = Noise(p.xz * .013 + float2(t * .04, -t * .025));
            float medium = Noise(p.xz * .058 - float2(t * .08, t * .045));
            float fine = Noise(p.xz * .22 + t * .1);
            float wave = sin(travel * _WaveScale + broad * 6.28318 + t * 2.1);
            float rippleWave = sin(travel * _WaveScale * 2.4 + medium * 6.28318 - t * 2.8 + 1.7);
            float clarity = saturate(1.0 - camDist / 2600.0);
            clarity *= clarity;
            float ripple = (wave * .38 + rippleWave * .2) * clarity;
            float3 up = normalize(IN.worldNormal);
            float3 drift = normalize(float3(1, 0, .35));
            float3 n = normalize(up + drift * wave * .05 * clarity + float3(-.35, 0, 1) * rippleWave * .03 * clarity);
            float3 view = normalize(IN.viewDir);
            float fresnel = pow(1 - saturate(dot(n, view)), 3);
            float edge = saturate(IN.color.r);
            float foam = saturate(edge * 1.2 + pow(saturate(wave * .5 + .5), 8) * .22 * clarity * (.45 + .55 * fine));
            float3 albedo = lerp(_Deep.rgb, _Shallow.rgb, saturate(.42 + ripple * .14 + edge * .25 + (medium - .5) * .08));
            albedo = lerp(albedo, _Foam.rgb, foam * .85);
            albedo *= lerp(.95, 1.04, fine);
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
