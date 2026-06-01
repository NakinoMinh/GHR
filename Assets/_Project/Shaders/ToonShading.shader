Shader "GanhHangRong/ToonShading"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.2, 0.2, 0.4, 1)
        
        [Header(Toon Lighting)]
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0, 1)) = 0.05
        
        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        
        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float4 _RimColor;
                float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalize normal
                float3 normalWS = normalize(input.normalWS);
                
                // Get main light
                Light mainLight = GetMainLight();
                
                // Calculate dot product for diffuse
                float NdotL = dot(normalWS, mainLight.direction);
                
                // Step for toon banding
                float lightIntensity = smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, NdotL);
                
                // Shadows
                // Basic shadow sampling (simplistic for this prototype)
                // In full URP, we'd use MainLightRealtimeShadow(TransformWorldToShadowCoord(input.positionWS))
                float shadowAtten = mainLight.shadowAttenuation;
                lightIntensity *= shadowAtten;

                // Base color sampling
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Color blending (Lit vs Shadow)
                half3 finalColor = lerp(_ShadowColor.rgb * albedo.rgb, albedo.rgb * mainLight.color, lightIntensity);

                // Additive Lights (Point lights)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    float addNdotL = saturate(dot(normalWS, light.direction));
                    float addLightIntensity = smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, addNdotL);
                    float attenuation = light.distanceAttenuation * light.shadowAttenuation;
                    finalColor += albedo.rgb * light.color * (addLightIntensity * attenuation);
                }

                // Rim Light
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float rim = 1.0 - saturate(dot(viewDir, normalWS));
                float rimIntensity = smoothstep(0.7, 1.0, pow(rim, _RimPower));
                finalColor += _RimColor.rgb * rimIntensity * lightIntensity; // Only rim light where hit by light

                // Emission
                half4 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv) * _EmissionColor;
                finalColor += emission.rgb;

                // Apply Fog
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
