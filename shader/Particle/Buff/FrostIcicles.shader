// 霜冻冰柱
Shader "XKnight/Buff/FrostIcicles"
{
    Properties
    {
        // 卡通照明 ------------------------------
        [Main(ToonLighting, __, on, off)]
        _ToonLighting ("卡通照明", Float) = 1
        
        [Sub(ToonLighting)] _PBRMaskMap ("角色的PBR遮罩", 2D) = "white"{}
        
        [Sub(ToonLighting)] _Shadow1Color ("阴影1 - 颜色", Color) = (0.5, 0.5, 0.5, 0.5)
        [Sub(ToonLighting)] _Shadow1Step ("阴影1 - Step", Range(0.0, 1.0)) = 0.5
        [Sub(ToonLighting)] _Shadow1Feather ("阴影1 - 羽化", Range(0.0, 1.0)) = 0.0
        [Sub(ToonLighting)] _Shadow2Color ("阴影2 - 颜色", Color) = (0.0, 0.0, 0.0, 0.0)
        [Sub(ToonLighting)] _Shadow2Step ("阴影2 - Step", Range(0.0, 1.0)) = 0.3
        [Sub(ToonLighting)] _Shadow2Feather ("阴影2 - 羽化", Range(0.0, 1.0)) = 0.0
        
        [Sub(ToonLighting)] [HDR] _SpecColor ("高光颜色", Color) = (0.2, 0.2, 0.2)
        [Sub(ToonLighting)] _Smoothness ("光滑度", Range( 0, 1 )) = 0.5
        [Sub(ToonLighting)] _SpecularStep ("高光Step", Range(0, 1)) = 0.5
        [Sub(ToonLighting)] _SpecularFeather ("高光羽化", Range(0, 1)) = 0
        
        [Sub(ToonLighting)] _EnvReflectStrength ("环境反射强度", Range( 0, 1 )) = 0.5
        
        
        // 霜冻 ------------------------------
        [Main(Frost, __, on, off)]
        _Frost("霜冻", Float) = 1
        
        [Sub(Frost)] _FrostTint ("冰霜调色", Color) = (1, 1, 1, 0)
        [Sub(Frost)] _FrostTexture ("冰霜纹理", 2D) = "white" {}
        
        [Sub(Frost)] _FrostBumpMap ("霜冻法线", 2D) = "bump" {}
        [Sub(Frost)] _FrostBumpScale ("霜冻法线强度", Range( 0, 10 )) = 1
        
        [Sub(Frost)] _IcicleMask ("冰柱遮罩", 2D) = "white" {}
        [Sub(Frost)] _IcicleMaskTile ("冰柱遮罩的 Tiling 值", Range( 0 , 1)) = 0.5
        
        [SubToggle(Frost, _ICE_OVERLAY_MASK_ON)] _IceOverlayMaskOn ("启用冰的覆盖遮罩", float) = 0
        [Sub(Frost)] _IceOverlayMask ("冰的覆盖遮罩", 2D) = "white" {}
        
        [Sub(Frost)] _IceSlider ("冰的总强度", Range( 0 , 1)) = 1
        [Sub(Frost)] _IceAmount ("冰量", Range( 0, 1 )) = 0
        [Sub(Frost)] _YMaskTop ("y轴遮罩 - 上半部分系数", Range( 0, 0.5 )) = 0.03
        [Sub(Frost)] _YMaskDown ("y轴遮罩 - 下半部分系数", Range( -0.5, 0 )) = -0.3
        [Sub(Frost)] _IcicleLength ("冰柱长度", Range( 0 , 1)) = 0
        [Sub(Frost)] _yIceMultiplier ("y轴冰柱倍增器", float) = 8
        
        [Sub(Frost)] _FrostEmissionFresnelIntensity ("霜冻自发光菲涅尔效应的强度", float) = 3
        [Sub(Frost)] _FrostEmissionFresnelPow ("霜冻自发光菲涅尔效果的幂值", float) = 2.5
        
        
        // 光传输 ------------------------------
        [Main(Transmission, _TRANSMISSION_LIGHT_ON, off, on)]
        _TransmissionLightOn ("启用光传输 - 参数基本不用调", float) = 0
        
        [Sub(Transmission)] _TransmissionShadow ("传输阴影", Range( 0, 1 ) ) = 0.5
        
        // SSS半透明
        [Sub(Transmission)] _TransStrength ("SSS - 强度", Range( 0, 50 ) ) = 1
        [Sub(Transmission)] _TransNormal ("SSS - 法线失真", Range( 0, 1 ) ) = 0.5
        [Sub(Transmission)] _TransScattering ("SSS - 散射", Range( 1, 50 ) ) = 2
        [Sub(Transmission)] _TransDirect ("SSS - 直接的", Range( 0, 1 ) ) = 0.9
        [Sub(Transmission)] _TransAmbient ("SSS - 环境", Range( 0, 1 ) ) = 0.1
        [Sub(Transmission)] _TransShadow ("SSS - 阴影", Range( 0, 1 ) ) = 0.5
        
        
        // 镶嵌 ------------------------------
        [Main(Tessellation, __, off, off)]
        _Tessellation ("镶嵌（曲面细分） - 参数基本不用调", Float) = 1
        
        [Sub(Tessellation)] _TessValue ("最大镶嵌", Range( 1, 32 ) ) = 4
        [Sub(Tessellation)] _TessMin ("镶嵌的最小距离", Float ) = 1
        [Sub(Tessellation)] _TessMax ("镶嵌的最大距离", Float ) = 10
        
        
        // 特殊的 ------------------------------
        _TimelineMainLightIntensity ("用于大招时压暗角色亮度", Range(0, 5)) = 1.0
    }

    HLSLINCLUDE
    #pragma target 3.0
    #pragma prefer_hlslcc gles

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    ENDHLSL
    
    /*
    // 410 - 支持镶嵌
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 410
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            offset -1, -1
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE //_MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //_ADDITIONAL_LIGHTS_VERTEX
            
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            //#pragma multi_compile _ _FORWARD_PLUS

            // #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            #pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            // -------------------------------------
            #define _SPECULAR_SETUP 1
            
            // 镶嵌
            #pragma require tessellation tessHW
            #pragma hull HullFunction
            #pragma domain DomainFunction
            // 距离镶嵌
            #define ASE_TESSELLATION 1
            #define ASE_DISTANCE_TESSELLATION
            
            #include "./FrostIcicles_Input.hlsl"
            #include "./FrostIcicles_ForwardPass.hlsl"
            ENDHLSL
        }
    }
    */

    // 400~300 - 不支持镶嵌
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 300

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            offset -1, -1
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE //_MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //_ADDITIONAL_LIGHTS_VERTEX
            
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            // #pragma multi_compile _ _FORWARD_PLUS

            // #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            #pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            // -------------------------------------
            #define _SPECULAR_SETUP 1
            
            #include "./FrostIcicles_Input.hlsl"
            #include "./FrostIcicles_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
    CustomEditor "LWGUI.LWGUI"
}