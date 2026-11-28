Shader "Hidden/Krab/ImageLayer/Image"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "Image"
            // Textures are premultiplied when they are loaded.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Unit quad (0..1, Y down) to output pixels (top-left origin, Y down).
            float4x4 _PixelMatrix;
            float4 _UvScale;
            float4 _Tint;
            float4 _Dark;
            float4 _Light;
            float _Duotone;
            // x = level, y = softness, z = 1 when enabled.
            float4 _Threshold;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float2 pixel = mul(_PixelMatrix, float4(input.uv, 0.0, 1.0)).xy;

                // Matches where p5.Unite's screen drawing lands in the output texture (checked by reading the
                // texture back): pixel Y down maps to clip Y without a flip.
                float2 clip = pixel / _ScreenParams.xy * 2.0 - 1.0;
                output.positionCS = float4(clip, 0.5, 1.0);

                // Content sits at the bottom-left of the padded texture; quad Y runs down from the content top.
                output.uv = float2(input.uv.x, 1.0 - input.uv.y) * _UvScale.xy;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float alpha = texel.a;
                float3 color = alpha > 0.0 ? texel.rgb / alpha : 0.0;

                if (_Duotone > 0.0)
                {
                    // Perceptual lightness, so the threshold level reads like a brightness slider.
                    float lightness = LinearToSRGB(dot(color, float3(0.2126, 0.7152, 0.0722)));
                    if (_Threshold.z > 0.0)
                    {
                        float softness = max(_Threshold.y, 1e-4);
                        lightness = smoothstep(_Threshold.x - softness, _Threshold.x + softness, lightness);
                    }

                    color = lerp(color, lerp(_Dark.rgb, _Light.rgb, lightness), _Duotone);
                }

                color *= _Tint.rgb;
                alpha *= _Tint.a;
                return float4(color * alpha, alpha);
            }

            ENDHLSL
        }
    }
}
