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
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Material properties (as plain uniforms to avoid any D3D11 CBUFFER quirks)
            float4 _ShallowColor;
            float4 _DeepColor;
            float   _FresnelStrength;
            float   _FresnelPower;

            // Water sim params (set via WaterRenderer MPB)
            float   _SeaLevel;
            float   _Gravity;
            float   _SimTime;
            float   _WaveCount; // 0..8

            // Per-wave packed data (amp, wavelength, dirX, dirZ) and (speed, 0,0,0)
            float4 _WaveData1_0;
            float4 _WaveData1_1;
            float4 _WaveData1_2;
            float4 _WaveData1_3;
            float4 _WaveData1_4;
            float4 _WaveData1_5;
            float4 _WaveData1_6;
            float4 _WaveData1_7;

            float4 _WaveData2_0;
            float4 _WaveData2_1;
            float4 _WaveData2_2;
            float4 _WaveData2_3;
            float4 _WaveData2_4;
            float4 _WaveData2_5;
            float4 _WaveData2_6;
            float4 _WaveData2_7;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            void AccumulateWave(float amp, float wl, float2 dir, float spd, float3 worldPos, inout float y, inout float2 grad)
{
    wl = max(wl, 0.1);
    spd = max(spd, 0.001);
    dir = normalize(dir);

    float k = 6.2831853 / wl;
    float omega = sqrt(_Gravity * k) * spd;

    float phase = k * dot(dir, worldPos.xz) - omega * _SimTime;

    float s = sin(phase);
    float c = cos(phase);

    y += amp * s;
    grad += (amp * k) * dir * c;
}

void SampleWaves(float3 worldPos, out float height, out float3 normalWS)
{
    float y = 0.0;
    float2 grad = float2(0.0, 0.0);

    // Fallback if no data yet: three default waves so you always see motion
    if (_WaveCount <= 0.5)
    {
        AccumulateWave(0.6, 12.0, float2(1.0, 0.3), 1.0, worldPos, y, grad);
        AccumulateWave(0.4,  8.0, float2(-0.7, 0.2), 1.1, worldPos, y, grad);
        AccumulateWave(0.3, 20.0, float2(0.2, -1.0), 0.9, worldPos, y, grad);
    }
    else
    {
        if (_WaveCount > 0.0) AccumulateWave(_WaveData1_0.x, _WaveData1_0.y, _WaveData1_0.zw, _WaveData2_0.x, worldPos, y, grad);
        if (_WaveCount > 1.0) AccumulateWave(_WaveData1_1.x, _WaveData1_1.y, _WaveData1_1.zw, _WaveData2_1.x, worldPos, y, grad);
        if (_WaveCount > 2.0) AccumulateWave(_WaveData1_2.x, _WaveData1_2.y, _WaveData1_2.zw, _WaveData2_2.x, worldPos, y, grad);
        if (_WaveCount > 3.0) AccumulateWave(_WaveData1_3.x, _WaveData1_3.y, _WaveData1_3.zw, _WaveData2_3.x, worldPos, y, grad);
        if (_WaveCount > 4.0) AccumulateWave(_WaveData1_4.x, _WaveData1_4.y, _WaveData1_4.zw, _WaveData2_4.x, worldPos, y, grad);
        if (_WaveCount > 5.0) AccumulateWave(_WaveData1_5.x, _WaveData1_5.y, _WaveData1_5.zw, _WaveData2_5.x, worldPos, y, grad);
        if (_WaveCount > 6.0) AccumulateWave(_WaveData1_6.x, _WaveData1_6.y, _WaveData1_6.zw, _WaveData2_6.x, worldPos, y, grad);
        if (_WaveCount > 7.0) AccumulateWave(_WaveData1_7.x, _WaveData1_7.y, _WaveData1_7.zw, _WaveData2_7.x, worldPos, y, grad);
    }

    height = _SeaLevel + y;
    normalWS = normalize(float3(-grad.x, 1.0, -grad.y));
}

            Varyings Vert(Attributes v)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                float h; float3 nWS;
                SampleWaves(worldPos, h, nWS);
                worldPos.y = h;

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