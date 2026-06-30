Shader "Custom/AnubisWater"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.4, 0.8, 0.8, 0.5)
        _DeepColor("Deep Color", Color) = (0.0, 0.5, 0.6, 0.9)
        _DepthMaxDistance("Depth Max Distance", Float) = 2.0
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveStrength("Wave Strength", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthMaxDistance;
                float _WaveSpeed;
                float _WaveStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Simple vertex wave
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS.y += sin(posWS.x * 2.0 + _Time.y * _WaveSpeed) * _WaveStrength;
                
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Get Depth
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                float thisZ = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                
                float depthDiff = max(0.0, sceneZ - thisZ);
                float depthFade = saturate(depthDiff / _DepthMaxDistance);
                
                // Blend Color
                half4 finalColor = lerp(_ShallowColor, _DeepColor, depthFade);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
