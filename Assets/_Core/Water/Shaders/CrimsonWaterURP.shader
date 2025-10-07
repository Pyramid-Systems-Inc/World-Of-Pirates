Shader "Crimson/WaterURP"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.15, 0.40, 0.60, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.08, 0.16, 1)
        _FresnelStrength("Fresnel Strength", Range(0,2)) = 0.6
        _FresnelPower("Fresnel Power", Range(0.5,8)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Back
        ZWrite On
        Blend Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Material properties
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _FresnelStrength;
                float _FresnelPower;

                float _SeaLevel;
                float _Gravity;
                float _SimTime;

                int _WaveCount;
                float _Amp[8];
                float _WL[8];
                float _DirX[8];
                float _DirZ[8];
                float _Speed[8];
            CBUFFER_END

            static const float TWO_PI = 6.28318530718;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            void SampleWaves(float3 worldPos, out float height, out float3 normalWS)
            {
                float y = 0.0;
                float2 grad = float2(0.0, 0.0);

                [unroll]
                for (int i = 0; i < _WaveCount; i++)
                {
                    float wl = max(_WL[i], 0.1);
                    float k = TWO_PI / wl;
                    float2 d = normalize(float2(_DirX[i], _DirZ[i]));
                    float omega = sqrt(_Gravity * k) * max(_Speed[i], 0.001);

                    float phase = k * dot(d, worldPos.xz) - omega * _SimTime;

                    float s = sin(phase);
                    float c = cos(phase);

                    y += _Amp[i] * s;

                    float ak = _Amp[i] * k;
                    grad += ak * d * c; // ∂y/∂x, ∂y/∂z
                }

                height = _SeaLevel + y;
                normalWS = normalize(float3(-grad.x, 1.0, -grad.y));
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                float height;
                float3 nWS;
                SampleWaves(worldPos, height, nWS);
                worldPos.y = height;

                o.worldPos = worldPos;
                o.normalWS = nWS;

                o.positionCS = TransformWorldToHClip(worldPos);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 N = normalize(i.normalWS);

                // Simple color + fresnel
                float3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(N.y));
                float fresnel = pow(saturate(1.0 - dot(N, V)), _FresnelPower) * _FresnelStrength;

                float3 col = baseCol + fresnel;

                col = MixFog(col, i.fogFactor);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}