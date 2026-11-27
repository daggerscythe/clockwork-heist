Shader "Clockwork Heist/Haze"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Haze"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // 0 means no haze and 1 means full haze and the camouflage script sets it
            float _HazeStrength;

            // the brightness setting where 1 is normal and above 1 lifts the dark parts and game options sets it
            float _Brightness;

            static const float WobbleSize = 0.008; // part of the screen the edges can bend
            static const float BigWaves = 23.0; // large ripples across the screen
            static const float SmallWaves = 61.0; // small ripples laid over the large ones
            static const float BigSpeed = 3.0; // how fast the large ripples rise
            static const float SmallSpeed = 7.0; // how fast the small ripples rise
            static const float DoubleGap = 0.5; // how far apart the two blended copies sit in the ripple

            // how far a point is pushed by the rising ripples at a moment
            float2 Ripple(float2 uv, float time)
            {
                float x = sin(uv.y * BigWaves - time * BigSpeed) + 0.5 * sin(uv.y * SmallWaves - time * SmallSpeed);
                float y = cos(uv.x * BigWaves + time * BigSpeed) + 0.5 * cos(uv.x * SmallWaves - time * SmallSpeed);
                return float2(x, y);
            }

            // bends the picture near the screen edges and blends two bent copies like air over a hot road
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 0 in the middle of the screen and 1 out at the edges
                float edge = smoothstep(0.25, 0.65, length(uv - 0.5)) * _HazeStrength;
                float size = WobbleSize * edge;

                half3 first = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + Ripple(uv, _Time.y) * size).rgb;
                half3 second = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + Ripple(uv, _Time.y + DoubleGap) * size).rgb;
                half3 color = (first + second) * 0.5;

                // the brightness setting bends the curve so dark parts lift and white stays white
                float brightness = _Brightness > 0.0 ? _Brightness : 1.0;
                color = pow(max(color, 0.0), 1.0 / brightness);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}