Shader "Hidden/ScreenSpaceScatter"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
        }
        ZWrite Off
        ZTest Always
        Cull Off

        // ------------------------------------------------------------------ //
        Pass
        {
            Name "SSSScatterFallback"
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragScatter
            #pragma target 4.5
            #pragma multi_compile_local _ SSS_REMULTIPLY_ALBEDO

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define SSS_GOLDEN_ANGLE 2.39996323

            TEXTURE2D(_SSSDiffuse);
            TEXTURE2D(_SSSAlbedo);
            TEXTURE2D(_DiscKernel);
            TEXTURE2D_FLOAT(_SSSDepthTexture);

            float4 _SSSScreenSize; // xy = (width, height)
            int _DiscKernelCount;
            float4 _SSSZBufferParams;
            float _SSSPixelScale;
            float _SSSMaxRadiusPx;
            float _SSSDepthFalloff;
            float4 _SSSRtHandleScale; // x = 1/rtWidth, y = 1/rtHeight, zw unused
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float LinDepth(float d) { return 1.0 / (_SSSZBufferParams.z * d + _SSSZBufferParams.w); }

            float4 FragScatter(Varyings i) : SV_Target
            {
                int2 size = (int2)_SSSScreenSize.xy;
                int2 px = clamp((int2)((i.uv / _SSSRtHandleScale.xy) * _SSSScreenSize.xy), int2(0, 0), size - 1);

                float4 centerIrr = LOAD_TEXTURE2D(_SSSDiffuse, px);
                if (centerIrr.a < 1e-4 || _DiscKernelCount <= 0)
                    return float4(0.0, 0.0, 0.0, 0.0);

                float centerDepth = LinDepth(LOAD_TEXTURE2D(_SSSDepthTexture, px).r);
                float pxScale = _SSSPixelScale / max(centerDepth, 1e-4);

                float3 result = 0.0;
                float3 wsum = 0.0;

                for (int s = 0; s < _DiscKernelCount; s++)
                {
                    float4 k = LOAD_TEXTURE2D(_DiscKernel, int2(s, 0));
                    float rMM = k.a;

                    float theta = (float)s * SSS_GOLDEN_ANGLE;
                    float2 dir;
                    sincos(theta, dir.y, dir.x);

                    float2 offPx = clamp(dir * rMM * pxScale, -_SSSMaxRadiusPx, _SSSMaxRadiusPx);
                    int2 spx = clamp(px + (int2)round(offPx), int2(0, 0), size - 1);

                    float4 sIrr = LOAD_TEXTURE2D(_SSSDiffuse, spx);
                    float sDepth = LinDepth(LOAD_TEXTURE2D(_SSSDepthTexture, spx).r);

                    float depthDiffMM = abs(sDepth - centerDepth) * 1000.0;
                    float falloff = saturate(1.0 - depthDiffMM * _SSSDepthFalloff / max(rMM, 1.0));

                    float3 w = k.rgb * sIrr.a * falloff;
                    result += sIrr.rgb * w;
                    wsum += w;
                }

                float3 scattered = float3(
                    wsum.x > 1e-5 ? result.x / wsum.x : centerIrr.r,
                    wsum.y > 1e-5 ? result.y / wsum.y : centerIrr.g,
                    wsum.z > 1e-5 ? result.z / wsum.z : centerIrr.b);

                #if defined(SSS_REMULTIPLY_ALBEDO)
                scattered *= LOAD_TEXTURE2D(_SSSAlbedo, px).rgb;
                #endif

                return float4(scattered, centerIrr.a);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ //
        Pass
        {
            Name "SSSComposite"

            Blend One One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D_X(_SSSAlbedo);
            TEXTURE2D_X(_SSSScatterResult);

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float4 FragComposite(Varyings i) : SV_Target
            {
                int2 size = (int2)_ScreenSize.xy;
                int2 px = clamp((int2)((i.uv / _RTHandleScale.xy) * _ScreenSize.xy), int2(0, 0), size - 1);
                float3 c = LOAD_TEXTURE2D_X(_SSSScatterResult, px).rgb;
                float a = LOAD_TEXTURE2D_X(_SSSAlbedo, px).a;
                return float4(c, a);
            }
            ENDHLSL
        }


    }
    Fallback Off
}