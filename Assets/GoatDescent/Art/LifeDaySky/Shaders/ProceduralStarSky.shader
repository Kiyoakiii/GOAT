Shader "LifeDay/ProceduralStarSky"
{
    Properties
    {
        [HDR] _HorizonColor ("Horizon Color", Color) = (0.95, 0.28, 0.10, 1)
        [HDR] _ZenithColor ("Zenith Color", Color) = (0.015, 0.045, 0.16, 1)
        _GradientPower ("Gradient Power", Range(0.2, 4.0)) = 1.35
        [HDR] _SunColor ("Sun Color", Color) = (1.0, 0.42, 0.10, 1)
        _SunDirection ("Sun Direction", Vector) = (0.55, 0.12, 0.82, 0)
        _SunDiskSize ("Sun Disk Size", Range(0.001, 0.08)) = 0.018
        _SunGlowPower ("Sun Glow Power", Range(4, 128)) = 28
        _SunIntensity ("Sun Intensity", Range(0, 8)) = 2.5
        _RayIntensity ("Ray Intensity", Range(0, 2)) = 0.18
        _RayLength ("Ray Length", Range(0.04, 0.9)) = 0.42
        _RayCount ("Ray Count", Range(4, 360)) = 12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            float4 _HorizonColor;
            float4 _ZenithColor;
            float _GradientPower;

            float4 _SunColor;
            float4 _SunDirection;
            float _SunDiskSize;
            float _SunGlowPower;
            float _SunIntensity;
            float _RayIntensity;
            float _RayLength;
            float _RayCount;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                float3 direction = normalize(input.direction);

                // 0 у горизонта, 1 прямо над головой.
                // A mountain needs the warm horizon lower in the frame than
                // LifeDay's compact arena; preserve its colours and sun model.
                float height = saturate((direction.y + 0.02) * 2.6);
                height = pow(height, _GradientPower);

                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, height);
                float3 sunDirection = normalize(_SunDirection.xyz);
                float sunDot = saturate(dot(direction, sunDirection));

                float sunDisk = smoothstep(1.0 - _SunDiskSize, 1.0, sunDot);
                float sunGlow = pow(sunDot, _SunGlowPower);

                // Make soft radial atmospheric rays around the sun without a texture.
                float3 referenceUp = abs(sunDirection.y) > 0.98 ? float3(1, 0, 0) : float3(0, 1, 0);
                float3 sunRight = normalize(cross(referenceUp, sunDirection));
                float3 sunUp = cross(sunDirection, sunRight);
                float2 sunPlane = float2(dot(direction, sunRight), dot(direction, sunUp));
                float rayDistance = length(sunPlane);
                float rayEnvelope = pow(saturate(1.0 - rayDistance / _RayLength), 2.2);
                float rayAngle = atan2(sunPlane.y, sunPlane.x);
                float rayBands = 0.5 + 0.5 * sin(rayAngle * _RayCount + sin(rayAngle * 2.0) * 0.65);
                float rays = rayEnvelope * smoothstep(0.70, 0.98, rayBands);

                sky += _SunColor.rgb * (sunDisk * _SunIntensity + sunGlow * 0.22 + rays * _RayIntensity);
                // LifeDay uses URP HDR tonemapping. Goat Descent uses built-in RP,
                // so map the same HDR sky colours into display range here.
                sky = max(sky, 0.0);
                sky = sky / (1.0 + sky);
                return float4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
