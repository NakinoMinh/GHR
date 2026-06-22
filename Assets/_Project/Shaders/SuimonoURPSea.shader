Shader "GHR/Suimono URP Sea"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.38, 0.52, 0.82)
        _depthColor ("Depth Color", Color) = (0.02, 0.16, 0.26, 1)
        _shallowColor ("Shallow Color", Color) = (0.22, 0.72, 0.78, 1)
        _BlendColor ("Blend Color", Color) = (0.06, 0.40, 0.52, 1)
        _OverlayColor ("Overlay Color", Color) = (0.35, 0.80, 0.88, 0.25)
        _FoamColor ("Foam Color", Color) = (0.88, 0.98, 1, 1)
        _SpecularColor ("Specular Color", Color) = (0.70, 0.92, 1, 1)
        _ReflectionColor ("Reflection Color", Color) = (0.40, 0.70, 0.86, 1)
        _SSSColor ("SSS Color", Color) = (0.12, 0.55, 0.62, 1)
        _UnderwaterColor ("Underwater Color", Color) = (0.02, 0.22, 0.28, 1)
        _reflectFallbackColor ("Reflection Fallback", Color) = (0.12, 0.38, 0.50, 1)
        _CausticsColor ("Caustics Color", Color) = (0.6, 0.9, 1, 1)
        _SkyTint ("Sky Tint", Color) = (1, 1, 1, 1)

        _overallBrightness ("Brightness", Float) = 0.9
        _overallTransparency ("Transparency", Float) = 0.25
        _heightScale ("Wave Height", Float) = 0.25
        _heightScaleFac ("Height Factor", Float) = 1
        _heightProjection ("Height Projection", Float) = 0
        _waveScale ("Wave Scale", Float) = 0.42
        _lgWaveScale ("Large Wave Scale", Float) = 1
        _lgWaveHeight ("Large Wave Height", Float) = 0.35
        _turbulenceFactor ("Turbulence", Float) = 0.25
        _foamScale ("Foam Scale", Float) = 10
        _foamSpeed ("Foam Speed", Float) = 0.5
        _enableFoam ("Enable Foam", Float) = 1
        _EdgeFoamFade ("Edge Foam Fade", Float) = 1
        _HeightFoamAmt ("Height Foam Amount", Float) = 1
        _HeightFoamHeight ("Height Foam Height", Float) = 1
        _HeightFoamSpread ("Height Foam Spread", Float) = 1
        _ShallowFoamAmt ("Shallow Foam", Float) = 0.5
        _shorelineHeight ("Shore Height", Float) = 0
        _shorelineFrequency ("Shore Frequency", Float) = 1
        _shorelineScale ("Shore Scale", Float) = 1
        _shorelineSpeed ("Shore Speed", Float) = 1
        _shorelineNorm ("Shore Norm", Float) = 1
        _DepthFade ("Depth Fade", Float) = 60
        _ShallowFade ("Shallow Fade", Float) = 120
        _EdgeFade ("Edge Fade", Float) = 80
        _RefractStrength ("Refract", Float) = 0
        _ReflectStrength ("Reflect", Float) = 1
        _reflectFlag ("Reflect Flag", Float) = 1
        _reflectDynamicFlag ("Dynamic Reflect Flag", Float) = 0
        _reflectFallback ("Reflect Fallback", Float) = 1
        _cameraDistance ("Camera Distance", Float) = 0
        _beaufortFlag ("Beaufort Flag", Float) = 0
        _beaufortScale ("Beaufort Scale", Float) = 0
        _specularPower ("Specular Power", Float) = 1
        _roughness ("Roughness", Float) = 0.25
        _roughness2 ("Roughness 2", Float) = 0.25
        _reflecTerm ("Reflection Term", Float) = 0
        _reflecSharp ("Reflection Sharp", Float) = 0
        _aberrationScale ("Aberration", Float) = 0
        _CausticsFade ("Caustics Fade", Float) = 0
        _isPlaying ("Is Playing", Float) = 0
        _suimono_uvx ("UV X", Float) = 0
        _suimono_uvy ("UV Y", Float) = 0
        _suimono_DebugDepthMask ("Depth Debug", Float) = 0
        _suimono_DebugWorldNormalMask ("Normal Debug", Float) = 0

        _WaveTex ("Wave Texture", 2D) = "white" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _NormalTexS ("Normal S", 2D) = "bump" {}
        _NormalTexD ("Normal D", 2D) = "bump" {}
        _NormalTexR ("Normal R", 2D) = "bump" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _CubeTex ("Cube", Cube) = "" {}
        _SkyCubemap ("Sky Cube", Cube) = "" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WaveTex);
            SAMPLER(sampler_WaveTex);
            TEXTURE2D(_FoamTex);
            SAMPLER(sampler_FoamTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _depthColor;
                half4 _shallowColor;
                half4 _BlendColor;
                half4 _OverlayColor;
                half4 _FoamColor;
                half4 _SpecularColor;
                half4 _ReflectionColor;
                float _overallBrightness;
                float _overallTransparency;
                float _heightScale;
                float _waveScale;
                float _lgWaveScale;
                float _lgWaveHeight;
                float _foamScale;
                float _foamSpeed;
                float _enableFoam;
                float _suimono_uvx;
                float _suimono_uvy;
            CBUFFER_END

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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y;
                float smallWave = sin((worldPos.x * 0.22 + worldPos.z * 0.13) + t * 1.05);
                float crossWave = sin((worldPos.x * -0.16 + worldPos.z * 0.19) + t * 0.72);
                float broadWave = sin((worldPos.x * 0.040 - worldPos.z * 0.060) + t * 0.30);
                worldPos.y += (smallWave * 0.020 + crossWave * 0.014 + broadWave * 0.070) * max(_heightScale, 0.05);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float shore = 1.0 - smoothstep(2.8, 12.0, input.positionWS.z);
                float deep = smoothstep(8.0, 78.0, input.positionWS.z);
                float far = smoothstep(55.0, 190.0, input.positionWS.z);

                float longPhase = input.positionWS.z * 0.34
                    + sin(input.positionWS.x * 0.045 + t * 0.18) * 0.75
                    + sin(input.positionWS.x * 0.090 - input.positionWS.z * 0.020) * 0.35
                    - t * 0.42;
                float crestLine = 1.0 - abs(sin(longPhase));
                float brokenMask = saturate(sin(input.positionWS.x * 0.19 + input.positionWS.z * 0.055 + t * 0.55) * 0.5 + 0.5);
                float fineRipple = saturate(sin(input.positionWS.x * 0.72 + input.positionWS.z * 0.38 + t * 1.8) * 0.5 + 0.5);
                float crest = smoothstep(0.965, 0.999, crestLine) * smoothstep(0.60, 0.92, brokenMask);
                float microHighlight = smoothstep(0.88, 0.998, fineRipple) * 0.070;
                float foam = (crest * lerp(0.16, 0.30, far) + shore * 0.16) * _enableFoam;

                float broadShade = sin((input.positionWS.x * 0.038 - input.positionWS.z * 0.052) + t * 0.26) * 0.5 + 0.5;
                half3 baseCol = lerp(_shallowColor.rgb, _depthColor.rgb, deep);
                baseCol = lerp(baseCol, _BlendColor.rgb, 0.18 + far * 0.16);
                baseCol += _OverlayColor.rgb * (microHighlight + crest * 0.075);
                baseCol *= lerp(1.08, 0.82, far);
                baseCol *= lerp(0.92, 1.08, broadShade);
                baseCol = lerp(baseCol, _FoamColor.rgb, saturate(foam) * 0.46);
                baseCol *= max(_overallBrightness, 0.2);

                float alpha = saturate(_BaseColor.a * (1.0 - _overallTransparency * 0.32));
                return half4(baseCol, alpha);
            }
            ENDHLSL
        }
    }
}
