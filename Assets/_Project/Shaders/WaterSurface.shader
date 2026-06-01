Shader "GanhHangRong/WaterSurface"
{
    Properties
    {
        _WaterColor("Water Color", Color) = (0, 0.4, 0.6, 0.8)
        _DeepWaterColor("Deep Water Color", Color) = (0, 0.1, 0.3, 0.9)
        _WaveSpeed("Wave Speed", Vector) = (0.1, 0.1, 0, 0)
        _WaveScale("Wave Scale", float) = 10.0
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThickness("Foam Thickness", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _DeepWaterColor;
                float4 _WaveSpeed;
                float _WaveScale;
                float4 _FoamColor;
                float _FoamThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                // Simple vertex wave
                float wave = sin(vertexInput.positionWS.x * _WaveScale + _Time.y * _WaveSpeed.x) * 
                             cos(vertexInput.positionWS.z * _WaveScale + _Time.y * _WaveSpeed.y);
                vertexInput.positionWS.y += wave * 0.1;
                
                // Recalculate CS after modifying WS
                output.positionCS = TransformWorldToHClip(vertexInput.positionWS);
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Animated UV for surface noise/waves
                float2 scrollUV = input.uv + _Time.y * _WaveSpeed.xy;
                float waveNoise = (sin(scrollUV.x * _WaveScale) * cos(scrollUV.y * _WaveScale)) * 0.5 + 0.5;
                
                // Base Color interpolation based on fake depth (using noise for now)
                half4 baseColor = lerp(_WaterColor, _DeepWaterColor, waveNoise * 0.5);
                
                // Fake foam at wave peaks
                float foam = smoothstep(1.0 - _FoamThickness, 1.0, waveNoise);
                half4 finalColor = lerp(baseColor, _FoamColor, foam);
                
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
