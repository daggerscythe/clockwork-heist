Shader "Clockwork Heist/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Bands("Light Bands", Range(1, 4)) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // every pass shares these so the srp batcher can group draws
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Bands; // number of light steps between dark and fully lit
        CBUFFER_END
        ENDHLSL

        // the main pass that draws color and light
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // light and shadow versions unity picks from at runtime
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // the shadow tint script sets both so shadows read at a glance
            half4 _ShadowTint; // dark purple that unlit parts lean toward
            float _ShadowTintAmount; // how far unlit parts lean toward it from 0 to 1

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
            };

            // moves each vertex into world space and onto the screen
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // ignores tiny amounts of light so faces at a grazing angle stay dark
            static const half BandEdge = 0.05;

            // the most a light's strength can scale its reach so tiny distances never overflow
            static const float MaxReach = 1000.0;

            // how much one light hits this spot snapped into flat bands then colored by the light
            // strength scales the falloff so a strong lamp lights everything inside its range and cone with a sharp edge
            half3 BandedLight(Light light, float3 normalWS, float strength)
            {
                float facing = saturate(dot(normalWS, light.direction)) * min(light.distanceAttenuation * strength, MaxReach) * light.shadowAttenuation;

                // any spot turned toward the light gets at least the first band
                half banded = ceil(saturate(facing) * _Bands - BandEdge) / _Bands;
                return light.color * banded;
            }

            // adds up the banded light from every light and caps it at fully lit
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

                // the sun or moon with its shadows
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lighting = BandedLight(mainLight, normalWS, 1.0);

                // lamps and other small lights
                #if defined(_ADDITIONAL_LIGHTS)
                // forward plus finds nearby lights from the screen spot and world spot
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    float strength = max(light.color.r, max(light.color.g, light.color.b));
                    lighting += BandedLight(light, normalWS, strength);
                LIGHT_LOOP_END
                #endif

                // cap at fully lit but keep the light color so a strong lamp makes one flat pool
                half brightness = max(lighting.r, max(lighting.g, lighting.b));
                lighting /= max(brightness, 1);

                // ambient keeps dark areas from going black
                half3 color = albedo * (SampleSH(normalWS) + lighting);

                // unlit parts stay dark but lean toward the purple and lit parts keep their colors
                color = lerp(color, _ShadowTint.rgb, _ShadowTintAmount * (1 - saturate(brightness)));
                return half4(color, 1);
            }
            ENDHLSL
        }

        // draws the object into shadow maps
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // draws the object into the depth texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // draws the object into the depth and normals texture
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}