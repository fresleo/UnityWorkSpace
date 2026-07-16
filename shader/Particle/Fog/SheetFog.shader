Shader "XKnight/Particle/Fog/Sheet Fog"
{
    Properties
    {
        [Main(SimpleNoise, __, off, off)]
        _SimpleNoise ("简单噪声", float) = 1
        [Sub(SimpleNoise)] _SimpleNoiseScale ("UV缩放", Float) = 20
        [Sub(SimpleNoise)] _SimpleNoiseAnimation ("UV滚动动画", Vector) = (0,0,0,0)
        [Sub(SimpleNoise)] _SimpleNoiseAmount ("强度", Range( 0 , 1)) = 0.25
        [Sub(SimpleNoise)] _SimpleNoiseRemap ("重映射的范围", Range( 0 , 1)) = 0

        [Main(SimplexNoise, __, off, off)]
        _SimplexNoise ("Simplex 噪声", float) = 1
        [Sub(SimplexNoise)] _SimplexNoiseScale ("UV缩放", Float) = 4
        [Sub(SimplexNoise)] _SimplexNoiseAnimation ("UV滚动动画", Vector) = (0,0,0.02,0)
        [Sub(SimplexNoise)] _SimplexNoiseAmount ("强度", Range( 0 , 1)) = 0.25
        [Sub(SimplexNoise)] _SimplexNoiseRemap ("重映射的范围", Range( 0 , 1)) = 0

        [Main(VoronoiNoise, __, off, off)]
        _VoronoiNoise ("Voronoi 噪声", float) = 1
        [Sub(VoronoiNoise)] _VoronoiNoiseScale ("UV缩放", Float) = 5
        [Sub(VoronoiNoise)] _VoronoiNoiseAnimation ("UV滚动动画", Vector) = (0,0,0,0)
        [Sub(VoronoiNoise)] _VoronoiNoiseAmount ("强度", Range( 0 , 1)) = 0.5
        [Sub(VoronoiNoise)] _VoronoiNoiseRemap ("重映射的范围", Range( 0 , 1)) = 0

        [Main(Combined, __, on, off)]
        _Combined ("组合", float) = 1
        [Sub(Combined)] _CombinedNoiseRemap ("组合噪声的重映射范围%uv0.z 用来传递粒子系统产生的稳定随机数", Range( 0 , 1)) = 0
        [Sub(Combined)] [HDR] _Albedo ("反照率颜色", Color) = (1,1,1,1)

        [Main(SurfaceFade, __, on, off)]
        _SurfaceFade ("根据深度，控制表面之间的淡入淡出", float) = 1
        [Sub(SurfaceFade)] _SurfaceDepthFade ("表面深度淡入淡出", Float) = 0
        [Sub(SurfaceFade)] _CameraDepthFadeRange ("相机深度淡入淡出范围", Float) = 0
        [Sub(SurfaceFade)] _CameraDepthFadeOffse ("相机深度淡入淡出偏移", Float) = 0
        
        [Main(WholeFade, _WHOLEFADE_ON, on, on)]
        _WholeFade ("根据深度，控制整个的淡入淡出", float) = 1
        [Sub(WholeFade)] _ViewAngleFading ("观察角度过渡值", Range(0,5)) = 1
        [Sub(WholeFade)] _CameraDistanceFading ("摄像机距离过渡值", Range(0,255)) = 75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent" "Queue"="Transparent"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        Offset 0, 0
        AlphaToMask Off

        HLSLINCLUDE
        #pragma prefer_hlslcc gles

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

        #pragma multi_compile_instancing
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        ENDHLSL

        // Forward
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForward"
            }
            
            ZWrite Off //On // 写入深度的话，叠加会有问题
            ZTest LEqual

            ColorMask RGBA
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha // rgb, a 通道分别使用不同的混合模式

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature _ _ADDITIONAL_LIGHTS

            // #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            
            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // #pragma multi_compile _ _LIGHT_LAYERS
            #pragma shader_feature_fragment _ _LIGHT_COOKIES
            //#pragma multi_compile _ _FORWARD_PLUS

            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma shader_feature _ SHADOWS_SHADOWMASK
            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma shader_feature _ LIGHTMAP_ON
            //#pragma multi_compile _ DYNAMICLIGHTMAP_ON

            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _WHOLEFADE_ON

            // -------------------------------------
            // 宏开关
            //#define _RECEIVE_SHADOWS_OFF 1 // 是否接收阴影 [ToggleOff] 不好使，如果需要做开关，则需要用 GUI 来实现相应的逻辑
            #define _SURFACE_TYPE_TRANSPARENT 1 // Shadows.hlsl 中的宏开关
            #define REQUIRE_DEPTH_TEXTURE 1 // ShaderGraphFunctions.hlsl 中采样场景深度的宏开关
            #define _EMISSION

            //--------------------------------------
            #include "./SheetFogInput.hlsl"
            #include "./SheetFogForwardPass.hlsl"
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            #include "./SheetFogInput.hlsl"
            #include "./SheetFogShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode"="Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "./SheetFogInput.hlsl"
            #include "./SheetFogMetaPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "LWGUI.LWGUI"
}