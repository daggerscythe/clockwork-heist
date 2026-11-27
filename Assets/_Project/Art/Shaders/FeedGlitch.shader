Shader "Clockwork Heist/FeedGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Feed", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FeedGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 0 means a clean feed and 1 means full static and the hack script sets it
            float _FeedGlitch;

            static const float2 FeedSize = float2(480.0, 270.0); // feed pixels so the static matches them
            static const float NoiseRate = 30.0; // new static patterns per s
            static const float TearBands = 24.0; // strips across the picture that can tear sideways
            static const float TearChance = 0.6; // part of the strips that tear at full glitch
            static const float TearSize = 0.12; // part of the width a strip can slide
            static const float RollSize = 0.08; // part of the height the picture can jump
            static const float SplitSize = 0.012; // part of the width the colors split apart
            static const float SnowAmount = 0.85; // how much static covers the picture at full glitch
            static const float ScanlineDark = 0.3; // how dark every other line gets
            static const float FlickerSize = 0.5; // how much the brightness jumps
            static const float NightGain = 4.0; // how much the night vision lifts dark parts while white stays white
            static const half3 NightGreen = half3(0.55, 1.0, 0.6); // phosphor green of the night vision

            // a repeatable random number from 0 to 1 for any point
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // the ui quad in place
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // tears splits and snows the feed in steps so it looks like a broken signal
            half4 Frag(Varyings input) : SV_Target
            {
                float glitch = _FeedGlitch;
                float frame = floor(_Time.y * NoiseRate);
                float2 uv = input.uv;

                // some strips slide sideways by a random amount
                float band = floor(uv.y * TearBands);
                float tears = step(1.0 - TearChance * glitch, Hash(float2(band, frame)));
                uv.x += (Hash(float2(band, frame + 17.0)) - 0.5) * TearSize * glitch * tears;

                // the whole picture jumps up or down
                uv.y = frac(uv.y + (Hash(float2(frame, 3.0)) - 0.5) * RollSize * glitch);

                // red and blue pull apart from green
                float split = SplitSize * glitch;
                half red = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(split, 0.0)).r;
                half green = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                half blue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(split, 0.0)).b;
                half3 color = half3(red, green, blue);

                // night vision lifts dark parts the most so shadowed props show and lit ones never wash out then paints it green
                half light = dot(color, half3(0.2126, 0.7152, 0.0722)) * NightGain;
                color = saturate(light / (1.0 + light) * (1.0 + NightGain) / NightGain) * NightGreen;

                // snow on the feed pixel grid
                float snow = Hash(floor(input.uv * FeedSize) + frame);
                color = lerp(color, snow.xxx, saturate(glitch * SnowAmount));

                // dark lines on every other feed row
                float darkRow = step(0.5, frac(input.uv.y * FeedSize.y / 2.0));
                color *= 1.0 - ScanlineDark * glitch * darkRow;

                // the brightness jumps from step to step
                color *= 1.0 + (Hash(float2(frame, 9.0)) - 0.5) * FlickerSize * glitch;

                return half4(color, 1.0) * input.color;
            }
            ENDHLSL
        }
    }
}