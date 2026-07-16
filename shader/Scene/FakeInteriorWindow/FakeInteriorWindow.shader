Shader "XKnight/Scene/FakeInteriorWindow"
{
    Properties
    {
        [HideInInspector] _AutoControlByScript ("被脚本自动控制了 (仅编辑器用)", Float) = 0
        
        // 房间
        [NoScaleOffset] _Cubemap ("房间的 Cubemap", CUBE) = "" {}
        _Room_Tiling ("房间的 Tiling (xy)", Vector) = (0, 0, 0, 0)
        _Room_Offset ("房间的内部偏移 (xy)", Vector) = (1, 1, 0, 0)
        _Room_Depth ("房间内部深度", Range(0, 1)) = 0.18
        
        _Window_Number ("窗户数 (xy)", Vector) = (4, 4, 0, 0)
        _Frame_Thickness ("窗户边框的厚度 (xy)", Vector) = (0.1, 0.05, 0, 0)
        
        _Glass_Color ("玻璃颜色", Color) = (0.6941177, 0.7450981, 0.7411765, 0)
        _Smoothness ("光滑度", Range(0, 1)) = 0.98
        _Metalic ("金属性", Range(0, 1)) = 0.12
        _Emission ("内部自发光", Range(0, 5)) = 0
        
        // 灰尘
        _Dust_Texture ("灰尘纹理", 2D) = "black" {}
        _Dust_Intensity ("灰尘强度", Range(0, 1)) = 1
        _Dust_Color ("灰尘颜色", Color) = (1, 1, 1, 0)
        _Dust_Smoothness ("灰尘光滑度", Range(0, 1)) = 1
        
        // 裂缝
        _Crack_Mask ("裂缝遮罩纹理 (xy:法线, b:细节噪声, a:高度)", 2D) = "black" {}
        
        _Crack_Color ("裂缝颜色", Color) = (0.6627451, 0.7254902, 0.7176471, 0)
        
        [HDR] _Break_Noise_Color ("细节噪声颜色", Color) = (0.8603088, 1.304119, 1.208529, 0)
        _Noise_Intensity ("细节噪声强度", Range(0, 1)) = 1
        
        // 玻璃破洞
        _Break_Mask ("玻璃破洞遮罩", 2D) = "white" {}
        _Glass_Break ("玻璃破洞尺寸", Range(0, 1)) = 0.12
        _Glass_Thickness ("裂纹厚度", Range(0, 1)) = 1
        _Thickness_Color ("裂纹厚度颜色", Color) = (0.4235294, 0.5294118, 0.4980392, 0)
        _Shadow_Offset ("破洞的影子偏移", Vector) = (1.7, 0.58, 0, 0)
        
        // 窗帘
        [NoScaleOffset] _Curtain_Texture ("窗帘纹理 (uv4)", 2D) = "black" {}
        _Curtain_Tiling ("窗帘的 Tiling", Vector) = (0, 0, 0, 0)
        _Curtain_Color ("窗帘颜色", Color) = (0.7843137, 0.7529412, 0.6901961, 0)
        _Curtain_Depth ("窗帘深度", Range(0, 1)) = 0
        
        // 烘焙 AO
        _BakerAOMap ("烘焙 AO 纹理 (uv3)", 2D) = "white" {}
        
        // 高级选项
        [HideInInspector] _QueueOffset ("队列偏移", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // Forward
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES

            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DUST_ON
            #pragma shader_feature_local_fragment _ _CRACK_MASK_ON
            #pragma shader_feature_local_fragment _ _BREAK_MASK_ON
            #pragma shader_feature_local_fragment _ _BAKERAO
            
            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            // --------------------------------------
            #include "./FakeInteriorWindowInput.hlsl"
            #include "./FakeInteriorWindowForwardPass.hlsl"
            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
            
            Cull Off
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DUST_ON
            #pragma shader_feature_local_fragment _ _CRACK_MASK_ON
            #pragma shader_feature_local_fragment _ _BREAK_MASK_ON
            #pragma shader_feature_local_fragment _ _BAKERAO

            //--------------------------------------
            #include "./FakeInteriorWindowInput.hlsl"
            #include "./FakeInteriorWindowMetaPass.hlsl"
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            Cull Back
            ZTest LEqual
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            //#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW // 点光源投影用的
            
            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            //--------------------------------------
            #include "./FakeInteriorWindowInput.hlsl"
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            
            Cull Back
            ZTest LEqual
            ZWrite On
            ColorMask R
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            //--------------------------------------
            #include "./FakeInteriorWindowInput.hlsl"
            #include "./FakeInteriorWindowDepthOnlyPass.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            
            Cull Back
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _CRACK_MASK_ON
            #pragma shader_feature_local_fragment _ _BREAK_MASK_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            //--------------------------------------
            #include "./FakeInteriorWindowInput.hlsl"
            #include "./FakeInteriorWindowDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "XKnight.ShaderGUI.FakeInteriorWindowShaderGUI"
}