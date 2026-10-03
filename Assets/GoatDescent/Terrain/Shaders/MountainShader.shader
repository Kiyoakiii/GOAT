Shader "GoatDescent/Mountain"
{
    Properties
    {
        _DirtTex ("Dirt", 2D) = "white" {}
        _GrassTex ("Grass", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        _SnowTex ("Snow", 2D) = "white" {}
        _RockNormal ("Rock Normal", 2D) = "bump" {}
        _RockDetail ("Rock Detail Normal", 2D) = "bump" {}
        _GrassNormal ("Grass Normal", 2D) = "bump" {}
        _DirtNormal ("Dirt Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0
        _DetailScale ("Detail Strength", Range(0, 2)) = 0.5
        _Tiling ("Texture Tiling", Float) = 0.02
        _DetailTiling ("Detail Tiling", Float) = 0.06
        _GrassEnd ("Grass Fades Below Height", Range(0, 1)) = 0.42
        _SnowStart ("Snow Starts Above Height", Range(0, 1)) = 0.86
        _Fade ("Blend Softness", Range(0.01, 0.4)) = 0.12
        _Steep ("Steepness Cutoff", Range(0, 1)) = 0.35
        _HeightMin ("Height Min (world)", Float) = -10
        _HeightMax ("Height Max (world)", Float) = 260
        _GrassBlend ("Grass Cover", Range(0, 1)) = 0.75
        _GrassDetail ("Grass Patch Scale", Float) = 0.06
        _HighlightTrack ("Highlight Track", Range(0, 1)) = 0
        _RouteProgress ("Route Progress", Range(0, 1)) = 0
        _RouteSpacing ("Route Step", Float) = 0.04
        _AtmoColor ("Atmosphere Color", Color) = (0.75, 0.88, 0.95, 1)
        _AtmoDensity ("Atmosphere Density", Float) = 0.006
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _DirtTex;
        sampler2D _GrassTex;
        sampler2D _RockTex;
        sampler2D _SnowTex;
        sampler2D _RockNormal;
        sampler2D _RockDetail;
        sampler2D _GrassNormal;
        sampler2D _DirtNormal;
        float _BumpScale;
        float _DetailScale;
        float _Tiling;
        float _DetailTiling;
        float _GrassEnd;
        float _SnowStart;
        float _Fade;
        float _Steep;
        float _HeightMin;
        float _HeightMax;
        float _GrassBlend;
        float _GrassDetail;
        float _HighlightTrack;
        float _RouteProgress;
        float _RouteSpacing;
        float4 _AtmoColor;
        float _AtmoDensity;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float4 color : COLOR;
        };

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float ValueNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 local = frac(p);
            local = local * local * (3.0 - 2.0 * local);
            float a = Hash21(cell);
            float b = Hash21(cell + float2(1, 0));
            float c = Hash21(cell + float2(0, 1));
            float d = Hash21(cell + float2(1, 1));
            return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
        }

        float2 Rotate2D(float2 p, float angle)
        {
            float sine = sin(angle);
            float cosine = cos(angle);
            return float2(cosine * p.x - sine * p.y, sine * p.x + cosine * p.y);
        }

        float3 StochasticSample(sampler2D tex, float2 uv)
        {
            float2 cell = floor(uv);
            float2 local = frac(uv);
            float angle = floor(Hash21(cell) * 4.0) * 1.5707963;
            float2 rotated = cell + 0.5 + Rotate2D(local - 0.5, angle);
            float edgeDistance = max(abs(local.x - 0.5), abs(local.y - 0.5));
            float interior = 1.0 - smoothstep(0.22, 0.49, edgeDistance);
            float3 regularColor = tex2D(tex, uv).rgb;
            float3 rotatedColor = tex2D(tex, rotated).rgb;
            return lerp(regularColor, rotatedColor, interior * 0.85);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float h = saturate((IN.worldPos.y - _HeightMin) / max(0.001, _HeightMax - _HeightMin));
            float platform = saturate(IN.color.r);

            float3 n = normalize(IN.worldNormal);
            float slope = 1.0 - saturate(n.y);
            float3 triWeights = pow(abs(n), 4.0);
            triWeights /= max(0.001, triWeights.x + triWeights.y + triWeights.z);

            float2 uv = IN.worldPos.xz * _Tiling;
            float3 dirtTop = StochasticSample(_DirtTex, uv);
            float3 dirtTri = StochasticSample(_DirtTex, IN.worldPos.zy * _Tiling) * triWeights.x
                + dirtTop * triWeights.y
                + StochasticSample(_DirtTex, IN.worldPos.xy * _Tiling) * triWeights.z;
            float3 dirtCol = lerp(dirtTop, dirtTri, smoothstep(_Steep * 0.55, _Steep + 0.15, slope));
            float3 grassCol = StochasticSample(_GrassTex, uv);
            float3 rockTop = StochasticSample(_RockTex, uv);
            float3 rockTri = StochasticSample(_RockTex, IN.worldPos.zy * _Tiling) * triWeights.x
                + rockTop * triWeights.y
                + StochasticSample(_RockTex, IN.worldPos.xy * _Tiling) * triWeights.z;
            float3 rockCol = lerp(rockTop, rockTri, smoothstep(_Steep * 0.65, _Steep + 0.2, slope));
            float3 snowCol = StochasticSample(_SnowTex, uv);

            float snowNoise = tex2D(_GrassTex, IN.worldPos.xz * (_Tiling * 0.37)).r;
            float snow = smoothstep(_SnowStart, _SnowStart + _Fade, h) * (0.78 + snowNoise * 0.22) * (1.0 - platform);
            float steep = smoothstep(_Steep, _Steep + 0.2, slope);
            float grassZone = 1.0 - smoothstep(_GrassEnd, _GrassEnd + _Fade, h);
            float grass = grassZone * (1.0 - steep) * (1.0 - platform);
            float dirtSlope = grassZone * steep * (1.0 - platform) * 0.5;
            float rock = saturate(1.0 - snow - grass - platform - dirtSlope);

            float2 patchUv = IN.worldPos.xz * _GrassDetail;
            float patch = tex2D(_GrassTex, patchUv * 2.0).b;
            float edge = saturate(IN.color.g);
            float grassCover = saturate(_GrassBlend * (0.6 + 0.6 * patch) + edge * 0.25 - (1.0 - platform) * 0.2);
            float3 platformCol = lerp(dirtCol, grassCol, grassCover);
            float platformType = floor(IN.color.b * 5.0 + 0.5);
            float3 typeTint = float3(1.0, 1.0, 1.0);
            if (platformType > 0.5 && platformType < 1.5) typeTint = float3(.78, 1.0, .68);
            else if (platformType > 1.5 && platformType < 2.5) typeTint = float3(1.0, .87, .56);
            else if (platformType > 2.5 && platformType < 3.5) typeTint = float3(.43, .82, 1.0);
            else if (platformType > 3.5 && platformType < 4.5) typeTint = float3(1.0, .56, .28);
            else if (platformType > 4.5) typeTint = float3(.74, .79, .86);
            platformCol *= typeTint;

            float3 albedo = grassCol * grass + dirtCol * dirtSlope + rockCol * rock + snowCol * snow + platformCol * platform;
            float macro = ValueNoise(IN.worldPos.xz * 0.035 + 17.2);
            albedo *= 0.84 + macro * 0.32;

            float concavity = pow(1.0 - saturate(n.y), 2.0) * 0.2;
            albedo *= 1.0 - concavity * rock;

            float cameraDistance = distance(IN.worldPos, _WorldSpaceCameraPos);
            float atmospheric = 1.0 - exp(-cameraDistance * _AtmoDensity);
            atmospheric *= lerp(1.1, 0.55, h);
            albedo = lerp(albedo, _AtmoColor.rgb, saturate(atmospheric * 0.68));

            float routeStep = max(0.001, _RouteSpacing);
            float stepsFromProgress = (IN.color.a - _RouteProgress) / routeStep;
            float currentHint = exp(-abs(stepsFromProgress) * 2.4);
            float nextHint = exp(-abs(stepsFromProgress - 1.0) * 2.2);
            float secondHint = exp(-abs(stepsFromProgress - 2.0) * 1.7);
            float pulse = 0.5 + 0.5 * sin(_Time.y * 4.2);
            float routeHint = saturate(.12 + currentHint * .22 + nextHint * (.45 + pulse * .4) + secondHint * .18);
            o.Albedo = lerp(albedo, float3(1.0, 0.85, 0.1), platform * _HighlightTrack * routeHint);
            o.Emission = _AtmoColor.rgb * saturate(atmospheric * 0.12);
            o.Metallic = 0.0;
            o.Smoothness = rock * 0.25 + grass * 0.08 + snow * 0.6 + platform * 0.12;

            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
