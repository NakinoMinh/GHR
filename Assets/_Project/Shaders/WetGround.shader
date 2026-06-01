Shader "GanhHangRong/WetGround"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Wetness("Wetness", Range(0, 1)) = 0.5
        _PuddleColor("Puddle Color", Color) = (0.2, 0.2, 0.3, 1)
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.8
        _NormalMap("Normal Map", 2D) = "bump" {}
        
        [Header(Rain Ripples)]
        _RippleSpeed("Ripple Speed", float) = 1.0
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _PuddleColor;
                float _Wetness;
                float _ReflectionStrength;
                float _RippleSpeed;
                float _RippleStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Simple ripple effect using sin time
                float2 rippleUV = input.uv * 10.0;
                float ripple = sin(rippleUV.x + _Time.y * _RippleSpeed) * sin(rippleUV.y + _Time.y * _RippleSpeed);
                ripple *= _RippleStrength * _Wetness; // Only ripples where wet
                
                // Normal
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                // Add ripple to normal
                normalTS.xy += ripple;
                normalTS = normalize(normalTS);

                float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));

                // Albedo
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo.rgb * mainLight.color * NdotL;

                // Fake Reflection / Wetness
                // In a real project, we'd use reflection probes or planar reflections. 
                // Here we fake it by mixing in a dark/reflective puddle color based on wetness.
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                
                // Simple specular
                float3 halfVector = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specular = pow(NdotH, 64.0) * _Wetness; // sharper highlight when wet

                half3 finalColor = lerp(diffuse, diffuse * 0.7 + _PuddleColor.rgb * _ReflectionStrength, _Wetness);
                finalColor += mainLight.color * specular;

                // Fog
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
