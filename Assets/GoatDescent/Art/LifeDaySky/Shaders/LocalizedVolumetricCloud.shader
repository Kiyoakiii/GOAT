Shader "LifeDay/Localized Volumetric Cloud"
{
    Properties
    {
        [NoScaleOffset] _DensityTex ("3D cloud density", 3D) = "white" {}
        [HDR] _BottomColor ("Deep cloud shadow", Color) = (0.06, 0.16, 0.29, 1)
        [HDR] _MiddleColor ("Cloud body", Color) = (0.52, 0.64, 0.72, 1)
        [HDR] _TopColor ("Cloud crown", Color) = (1.0, 0.76, 0.55, 1)
        [HDR] _SunColor ("Sunset scattering", Color) = (1.35, 0.38, 0.15, 1)
        _Density ("Density", Range(0.1, 6)) = 2.1
        _Absorption ("View absorption", Range(0.1, 8)) = 2.25
        _LightAbsorption ("Sun shadow strength", Range(0.1, 8)) = 2.6
        _StepCount ("Raymarch steps", Range(24, 96)) = 72
        _LightStep ("Sun sample distance", Range(0.01, 0.3)) = 0.085
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "VolumetricCloudForward"
            Tags { "LightMode"="ForwardBase" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            // Goat Descent uses the built-in renderer. Keep LifeDay's density field,
            // palette and ray marcher, but feed it the sun explicitly instead of URP.
            #include "UnityCG.cginc"

            #define MAX_STEPS 96
            #define MAX_CLOUD_SHADOW_VOLUMES 32

            sampler3D _DensityTex;
            float4 _LifeDaySunDirection;
            float4 _LifeDaySunColor;

            // Updated by CloudBankFollower once per frame. These are cheap analytic
            // blockers used for light travelling between separate cloud volumes. The
            // detailed 3D texture below still handles each cloud's own self-shadow.
            float4x4 _CloudShadowWorldToLocal[MAX_CLOUD_SHADOW_VOLUMES];
            float _CloudShadowCount;
            float _CloudShadowStrength;
            float _CloudShadowSelfIndex;

            half4 _BottomColor;
            half4 _MiddleColor;
            half4 _TopColor;
            half4 _SunColor;
            half _Density;
            half _Absorption;
            half _LightAbsorption;
            half _StepCount;
            half _LightStep;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float2 IntersectUnitBox(float3 rayOrigin, float3 rayDirection)
            {
                float3 safeDirection = max(abs(rayDirection), 0.0001) * sign(rayDirection);
                float3 inverseDirection = 1.0 / safeDirection;
                float3 nearTimes = (-0.5 - rayOrigin) * inverseDirection;
                float3 farTimes = (0.5 - rayOrigin) * inverseDirection;
                float3 minimumTimes = min(nearTimes, farTimes);
                float3 maximumTimes = max(nearTimes, farTimes);
                return float2(max(max(minimumTimes.x, minimumTimes.y), minimumTimes.z), min(min(maximumTimes.x, maximumTimes.y), maximumTimes.z));
            }

            float SampleDensity(float3 localPosition)
            {
                float3 uvw = localPosition + 0.5;
                if (any(uvw < 0.0) || any(uvw > 1.0)) return 0.0;
                // Explicit LOD keeps sampling stable inside the variable ray loop.
                return tex3Dlod(_DensityTex, float4(uvw, 0.0)).r;
            }

            float SampleInterCloudShadow(float3 worldPosition, float3 sunDirectionWS)
            {
                float opticalDepth = 0.0;
                [loop]
                for (int cloudIndex = 0; cloudIndex < MAX_CLOUD_SHADOW_VOLUMES; cloudIndex++)
                {
                    if (cloudIndex >= (int)_CloudShadowCount || abs((float)cloudIndex - _CloudShadowSelfIndex) < 0.5)
                    {
                        continue;
                    }

                    float3 blockerOrigin = mul(_CloudShadowWorldToLocal[cloudIndex], float4(worldPosition, 1.0)).xyz;
                    float3 blockerDirection = normalize(mul((float3x3)_CloudShadowWorldToLocal[cloudIndex], sunDirectionWS));
                    float2 blockerHit = IntersectUnitBox(blockerOrigin, blockerDirection);
                    float blockerEntry = max(blockerHit.x, 0.0);
                    float blockerExit = blockerHit.y;
                    if (blockerExit <= blockerEntry)
                    {
                        continue;
                    }

                    // A box intersection alone would make every volume an opaque
                    // billboard. Sampling the middle of the intersection gives it a
                    // soft ellipsoidal optical core, matching the generated cloud field.
                    float blockerLength = blockerExit - blockerEntry;
                    float3 blockerMiddle = blockerOrigin + blockerDirection * (blockerEntry + blockerLength * 0.5);
                    float coreCoverage = saturate(1.0 - length(blockerMiddle * 2.15));
                    opticalDepth += coreCoverage * saturate(blockerLength * 1.8) * 1.35;
                }

                return exp(-opticalDepth * max(0.0, _CloudShadowStrength));
            }

            half3 CloudPalette(float localHeight, float sunReach)
            {
                half vertical = saturate(localHeight + 0.5);
                half3 color = lerp(_BottomColor.rgb, _MiddleColor.rgb, smoothstep(0.08, 0.58, vertical));
                color = lerp(color, _TopColor.rgb, smoothstep(0.45, 0.96, vertical) * 0.62h);
                half sunsetAmount = sunReach * (0.20h + vertical * 0.52h);
                return lerp(color, _SunColor.rgb, sunsetAmount);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rayDirection = normalize(input.positionOS - rayOrigin);
                float2 hit = IntersectUnitBox(rayOrigin, rayDirection);
                if (hit.y <= max(hit.x, 0.0)) discard;

                // Preserve the LifeDay cloud shapes, but leave the sun disc and its
                // immediate glow unobstructed from every camera position.
                float3 viewDirectionWS = normalize(mul((float3x3)unity_ObjectToWorld, rayDirection));
                half sunClearance = smoothstep(0.965, 0.993,
                    dot(viewDirectionWS, normalize(_LifeDaySunDirection.xyz)));
                if (sunClearance >= 0.999h) return half4(0.0h, 0.0h, 0.0h, 0.0h);

                float entry = max(hit.x, 0.0);
                float travel = hit.y - entry;
                int stepCount = clamp((int)_StepCount, 24, MAX_STEPS);
                float stepSize = travel / stepCount;
                float pixelJitter = frac(sin(dot(input.positionHCS.xy, float2(12.9898, 78.233))) * 43758.5453);
                float3 rayPosition = rayOrigin + rayDirection * (entry + stepSize * pixelJitter);

                // Both the sky disc and clouds use the same world-space direction.
                float3 sunDirectionWS = normalize(_LifeDaySunDirection.xyz);
                float3 sunDirection = normalize(mul((float3x3)unity_WorldToObject, sunDirectionWS));
                half3 accumulated = 0.0h;
                half transmittance = 1.0h;
                half interCloudReach = 1.0h;
                int shadowCountdown = 0;

                [loop]
                for (int step = 0; step < MAX_STEPS; step++)
                {
                    if (step >= stepCount || transmittance < 0.012h) break;
                    float density = SampleDensity(rayPosition);
                    if (density > 0.003)
                    {
                        // A short march towards the sun gives the dense core a cool shadow
                        // while exposed, ragged edges glow warm. It is the key difference
                        // between a volume and a painted sphere.
                        float sunDensity = 0.0;
                        sunDensity += SampleDensity(rayPosition + sunDirection * _LightStep);
                        sunDensity += SampleDensity(rayPosition + sunDirection * _LightStep * 2.0);
                        sunDensity += SampleDensity(rayPosition + sunDirection * _LightStep * 3.0);
                        sunDensity += SampleDensity(rayPosition + sunDirection * _LightStep * 4.0);
                        // Keep the tonal strength close to the former three-sample
                        // march, while gaining one more layer of directional depth.
                        half sunReach = exp(-sunDensity * _LightAbsorption * 0.78h);
                        // Cross-volume occlusion is intentionally evaluated every few
                        // view steps: the blocker is broad, while this keeps the 17+
                        // volume cloudscape comfortably real-time.
                        if (_CloudShadowCount > 1.0 && shadowCountdown <= 0)
                        {
                            interCloudReach = SampleInterCloudShadow(mul(unity_ObjectToWorld, float4(rayPosition, 1.0)).xyz, sunDirectionWS);
                            shadowCountdown = 6;
                        }
                        sunReach *= interCloudReach;
                        // A restrained forward-scattering lift makes the sun-facing rim
                        // breathe as the player changes viewing angle, without making
                        // the whole cloud flash or lose its cool shaded core.
                        half forwardScatter = saturate(dot(-rayDirection, sunDirection) * 0.5h + 0.5h);
                        sunReach = saturate(sunReach + forwardScatter * (1.0h - sunReach) * 0.16h);
                        // Density texture values describe air density, not final alpha.
                        // The scale below gives the cloud a visible core while retaining
                        // the smooth, transparent erosion at its procedural edge.
                        half alphaStep = 1.0h - exp(-density * _Density * _Absorption * stepSize * 2.4h);
                        half3 cloudColor = CloudPalette(rayPosition.y, sunReach);
                        cloudColor *= lerp(half3(0.78h, 0.78h, 0.78h), _LifeDaySunColor.rgb, 0.18h);
                        accumulated += transmittance * alphaStep * cloudColor;
                        transmittance *= (1.0h - alphaStep);
                    }
                    rayPosition += rayDirection * stepSize;
                    shadowCountdown--;
                }

                half visibleCloud = 1.0h - sunClearance;
                return half4(accumulated * visibleCloud, (1.0h - transmittance) * visibleCloud);
            }
            ENDHLSL
        }
    }
}
