Shader "XKnight/Character/ToonPBR_CharacterGhost"
{
    Properties
    {
        // 表面设置
        [HideInInspector] _WorkflowMode ("工作流模式", Float) = 0.0
        [HideInInspector] _ShadingModel ("渲染模型", Float) = 0.0
        
        // 基本
        _BaseColor ("基础调色", Color) = (1, 1, 1, 1)
        
        [HideInInspector] _ShadowType ("阴影类型", Float) = 0.0
        
        // 2阶阴影
        _Shadow1Color ("Shadow1Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Shadow1Step ("Shadow1 Step", Range(0.0, 1.0)) = 0.5
        _Shadow1Feather ("Shadow1 Feather", Range(0.0, 1.0)) = 0.0
        _Shadow2Color ("Shadow2Color", Color) = (0.0, 0.0, 0.0, 0.0)
        _Shadow2Step ("Shadow1 Step", Range(0.0, 1.0)) = 0.3
        _Shadow2Feather ("Shadow1 Feather", Range(0.0, 1.0)) = 0.0

        // 阴影遮罩控制
        _Source175Shadow ("175阴影的源", Float) = 0
        _TintBrightness ("暗部明度强度", Range(0, 1)) = 0.5
        _TintShadowFactor ("顶点色阴影衰减", Range(0, 1)) = 1
        
        // 高光着色模式
        _SpecularShadingMode ("SpecularShadingMode", Float) = 0
        
        _Metallic ("金属性", Range(0.0, 5.0)) = 0.5
        [HDR] _SpecColor ("高光颜色", Color) = (0.2, 0.2, 0.2)
        
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _SpecularStep ("SpecularStep", Range(0, 1)) = 0.5
        _SpecularFeather ("SpecularFeather", Range(0, 1)) = 0
        
        // 抖动
        _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        _DitherSize ("抖动尺寸", Float) = 1
        _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        _DitherWithMatrix ("抖动矩阵", Int) = 0
        _DitherTexture ("抖动图", 2D) = "black" {}
        
        _TimelineMainLightIntensity ("用于大招时压暗角色亮度", Range(0, 5)) = 1.0
        
        // Advanced Options
        [ToggleOff] _EnvironmentReflections ("Environment Reflections （这个暂时应该是废的）", Range(0, 1)) = 0.0
        _EnvReflectStrength ("烘焙 GI 比例（漫反射 + 镜面高光）", Range(0, 1)) = 1.0
        
        // 模板缓冲
        [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
        [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
        [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            
            Cull Back
            ZWrite Off // 不关的话，会挡住火球的光，遮挡关系用模版缓冲来还原
            
            ColorMask RGBA
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON

            // -------------------------------------
            // Material Keywords
            #pragma multi_compile_local_fragment _ _DITHER_ON // 抖动透明开关
            
            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "../ShaderLibrary/Lighting.hlsl"
            #include "./Ghost/ToonPBR_Ghost_Input.hlsl"

            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"
            
            #include "./Ghost/ToonPBR_Ghost_Forward.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "XKnight.ShaderGUI.ToonPBR_Ghost_ShaderGUI"
}