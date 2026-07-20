Shader "Hidden/XKnightSSR"
{
    Properties
    {
        _Color("", Color) = (1,1,1)
        _NoiseTex("", any) = "" {}
        _SSRSettings("", Vector) = (1,1,1,1)
        _SSRSettings2("", Vector) = (1,1,1,1)
        _StencilValue("Stencil Value", Int) = 0
        _StencilCompareFunction("Stencil Compare Function", Int) = 8
    }

    Subshader
    {

        Tags
        {
            "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "DisableBatching"="True" "ForceNoShadowCasting"="True"
        }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #pragma target 3.0
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "SSRCommon.hlsl"
        ENDHLSL

        Pass
        {
            // 0
            Name "Copy Depth"
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragCopyDepth
            #pragma multi_compile _ SSR_BACK_FACES
            #include "SSRCopy.hlsl"
            ENDHLSL
        }
        Pass
        {
            // 1  Point Copy限制最小值为0，阻止NaN扩散，非必要
            Name "Copy exact"
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragCopyExact
            #include "SSRCopy.hlsl"
            ENDHLSL
        }

        Pass
        {
            // 2 计算反射信息在屏幕UV hit点，blurRadius,reflectionIntensity
            Name "Surface reflection"
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM
            #pragma vertex VertSSRSurf
            #pragma fragment FragSSRSurf
            #pragma multi_compile_local _ SSR_JITTER
            #pragma multi_compile _ SSR_BACK_FACES
            #pragma multi_compile _ SSR_SKYBOX
            #pragma multi_compile_local _ SSR_THICKNESS_FINE
            #include "SSRSurfacePass.hlsl"
            ENDHLSL
        }
        Pass
        {
            // 3 还原反射颜色
            Name "Resolve"
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragResolve
            #pragma multi_compile _ SSR_SKYBOX
            #include "SSRSolveColor.hlsl"
            ENDHLSL
        }
        Pass
        {
            // 4
            Name "Blur horizontally"
            HLSLPROGRAM
            #pragma vertex VertBlur
            #pragma fragment FragBlur
            #pragma multi_compile_local _ SSR_DENOISE
            #define SSR_BLUR_HORIZ
            #include "SSRBlur.hlsl"
            ENDHLSL
        }

        Pass
        {
            // 5
            Name "Blur vertically"
            HLSLPROGRAM
            #pragma vertex VertBlur
            #pragma fragment FragBlur
            #pragma multi_compile_local _ SSR_DENOISE
            #include "SSRBlur.hlsl"
            ENDHLSL
        }

        Pass
        {
            // 6 反射Combine

            Name "Combine"
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilCompareFunction]
            }
            Blend One OneMinusSrcAlpha // precomputed source alpha in Resolve pass
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragCombine

            #include "SSRBlends.hlsl"
            ENDHLSL
        }
        Pass
        {
            // 7
            Name "Combine with compare"
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilCompareFunction]
            }
            Blend One OneMinusSrcAlpha // One One // precomputed source alpha in Resolve pass
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragCombineWithCompare

            #include "SSRBlends.hlsl"
            ENDHLSL
        }


        Pass
        {
            // 8
            Name "Debug"
            Blend One Zero
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragCopyExact
            #include "SSRCopy.hlsl"
            ENDHLSL
        }

        Pass
        {
            // 9
            Name "DebugRayCast"
            Blend One Zero
            HLSLPROGRAM
            #pragma vertex VertSSR
            #pragma fragment FragRayCast
            #include "SSRCopy.hlsl"
            ENDHLSL
        }
        //    
        //      
        //    

        //    

        //    
        //      Pass { // 9
        //          Name "Copy with bilinear filter"
        //          HLSLPROGRAM
        //          #pragma vertex VertSSR
        //          #pragma fragment FragCopy
        //          #include "SSR_Blends.hlsl"
        //          ENDHLSL
        //      }
        //    
        //      Pass { // 10
        //          Name "Temporal Accumulation"
        //          HLSLPROGRAM
        //          #pragma vertex VertSSR
        //          #pragma fragment FragAcum
        //          #include "SSR_TAcum.hlsl"
        //          ENDHLSL
        //      }
        //    
        //      Pass { // 11
        //          Name "Debug Depth"
        //          HLSLPROGRAM
        //          #pragma vertex VertSSR
        //          #pragma fragment FragDebugDepth
        //          #include "SSR_Blends.hlsl"
        //          ENDHLSL
        //      }
        //    
        //      Pass { // 12
        //          Name "Debug Normals"
        //          HLSLPROGRAM
        //          #pragma vertex VertSSR
        //          #pragma fragment FragDebugNormals
        //          #pragma multi_compile _ _GBUFFER_NORMALS_OCT
        //          #include "SSR_Blends.hlsl"
        //          ENDHLSL
        //      }
        //      
        //      Pass { // 13
        //          Name "Copy Depth"
        //          HLSLPROGRAM
        //          #pragma vertex VertSSR
        //          #pragma fragment FragCopyDepth
        //          #pragma multi_compile _ SSR_BACK_FACES
        //          #include "SSR_Blends.hlsl"
        //          ENDHLSL
        //      }

    }
    FallBack Off
}