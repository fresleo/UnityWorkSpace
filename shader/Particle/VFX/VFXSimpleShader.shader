Shader "XKnight/Particle/SimpleVFX"
{
    Properties
    {
        _MainTex ("主纹理", 2D) = "white" {}
        [LiteToggle] _MainTexColorIntensityUseCustomData_W ("亮度使用 CustomData.w", Int) = 0
        [LiteToggle] _MainTexUClamp ("U Clamp", Float) = 0
        [LiteToggle] _MainTexVClamp ("V Clamp", Float) = 0
        _MainTexColorCorrection ("主纹理校色", Vector) = (1, 1, 1, 0)
        
        [VFXBlendMap(_MAINTEXBLEND_ON)]
        _MainTexBlendOn ("开启混合纹理", int) = 0
        [HideInInspector] _MainTexBlendIntensity ("混合强度", Range(0, 1)) = 1
        [HideInInspector] _MainTexBlend ("混合纹理", 2D) = "white" {}
        [HideInInspector] _MainTexBlendSample ("混合模式", int) = 0
        [HideInInspector] _MainTexBlendRampChannal ("Ramp模式: 主纹理通道", int) = 0
        [HideInInspector] _MainTexBlendRampY ("Ramp模式: 垂直坐标", Range(0, 1)) = 0.5
        
        [LiteToggle] _MainTexGray ("主纹理去色", Int) = 0
        [LiteToggle] _MainTexOffsetStop ("禁用主纹理自动滚动", Int) = 0
        [LiteToggle] _MainTexOffsetUseCustomData_XY ("纹理滚动使用 CustomData.xy", Int) = 0

        [Space]
        [Header(Main Color)]
        [HDR] _Color ("主纹理颜色", Color) = (1, 1, 1, 1)
        [LiteToggle] _MainTexMultiAlpha ("主纹理预乘Alpha(主纹理,主颜色,顶点色)", Int) = 0
        [Toggle] _MainTexUseScreenUV ("主纹理使用屏幕坐标 UV", Int) = 0
        [Toggle] _OnlyMainTexUseScreenUV ("只让主纹理使用屏幕坐标 UV", Int) = 0

        [LiteToggle] _MainTexSingleChannelOn ("主纹理使用单通道", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MainTexChannel ("主纹理通道", Int) = 0

        _MainTexMultiFactor ("主纹理预乘系数", Float) = 1
        [LiteToggle] _MainTexAutoScale ("根据缩放自动平铺", Float) = 0

        [Header(Back Face)]
        [LiteToggle] _BackFaceOn ("背面(需要手动Cull Off)", Int) = 0
        [HDR] _BackFaceColor ("背面色", Color) = (0.5, 0.5, 0.5, 1)

        [Header(Mask Tex)]
        _MainTexMask ("主纹理遮罩", 2D) = "white" {}
        [LiteToggle] _MainTexMaskUseCustomData2_XY ("主纹理遮罩滚动使用 CustomData2.xy", Float) = 0
        [LiteToggle] _MainTexMaskOffsetStop ("禁用主纹理遮罩自动滚动", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MainTexMaskChannel ("遮罩通道", Int) = 0

        [Space]
        [Toggle] _MainTextureRadialUVOn ("极坐标", Int) = 0
        
        [VFXRotationToggle(_MainTexRotationAngle, _MainTexRotationCenter)]
        _MainTextureRotationUVOn ("主纹理旋转", Int) = 0
        _MainTexRotationAngle ("主纹理旋转角度", Range(0, 360)) = 0
        _MainTexRotationCenter ("主纹理旋转中心", Vector) = (0.5, 0.5, 0, 0)

        [Header(BlendMode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode ("Src Mode", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstMode ("Dst Mode", Int) = 10

        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("剔除模式", Float) = 0
        [LiteToggle] _ZWriteMode ("写入深度", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("深度测试", Float) = 4

        // ==================================================
        [Toggle] _DistortionOn ("开启扭曲", Int) = 0
        [noscaleoffset] _DistortionNoiseTex ("扭曲贴图", 2D) = "white" {}
        [Toggle] _DistortionRadialUVOn ("极坐标", Int) = 0
        [LiteToggle] _DistortionAffectU ("影响U", Int) = 1
        [LiteToggle] _DistortionAffectV ("影响V", Int) = 1
        [LiteToggle] _DistortionAutoScale ("根据缩放自动平铺", Float) = 0
        [LiteToggle] _DistortionAffectDissolve ("扭曲溶解图", Int) = 0
        [LiteToggle] _DistortionAffectMainMaskTexture ("扭曲主纹理 Mask 图", Int) = 0
        [LiteToggle] _DistortionAffectMainTexture ("扭曲主纹理图", Int) = 1
        [LiteToggle]_DistortionApplyToOffset ("扭曲流光", Int) = 0
        _DistortionMaskTex ("遮罩贴图", 2D) = "white"{}
        [Enum(R,0,G,1,B,2,A,3)] _DistortionMaskChannel ("遮罩通道", Int) = 0
        _DistortionIntensity ("强度", Range(-10, 10)) = 0.5
        _DistortTile ("Tiling(xy:层1, zw:层2)", Vector) = (1, 1, 1, 1)
        _DistortDir ("Offset(xy:层1, zw:层2)", Vector) = (0, 1, 0, -1)

        // ==================================================
        [Toggle] _DissolveOn ("开启溶解", Int) = 0
        _DissolveTex ("溶解图", 2D)= "black" {}
        _DissolveDirectionTex ("溶解方向图", 2D) = "white" {}
        [LiteToggle] _DissolveTexOffsetStop ("禁止溶解自动滚动", Int) = 0
        [LiteToggle] _DissolveDirectionTexOffsetStop ("禁止溶解方向图自动滚动", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DissolveTexChannel ("溶解图通道", Int) = 0

        [Header(DissolveType)]
        [LiteToggle] _DissolveByVertexColor ("使用顶点色(Alpha)控制", Int) = 0
        [LiteToggle] _DissolveByCustomData_Z ("使用 CustomData.z 控制", Int) = 0
        [LiteToggle] _DissolveOffsetByCustomData2xy ("Offset 使用 CustomData2.xy", Int) = 0

        [Header(DissolveFading)]
        _DissolveFadingMin ("透明 Min", Range(0, 0.2)) = 0
        _DissolveFadingMax ("透明 Max", Range(0, 1.0)) = .2

        [Header(Dissolve Clip)]
        [LiteToggle] _DissolveClipOn ("像素剔除", Float) = 1
        _Cutoff ("镂空值", Range(0, 1)) = 0.5

        [Header(PixelDissolve)]
        [LiteToggle] _PixelDissolveOn ("像素化溶解", Float) = 0
        _PixelWidth ("像素化宽", Float) = 10

        [Header(DissolveEdge)]
        [LiteToggle] _DissolveEdgeOn ("开启溶解边", Int) = 0
        _EdgeWidth ("边宽度", Range(0, 0.3)) = 0.1
        [LiteToggle] _EdgeColorMultiVertexColor ("颜色乘顶点色", Float) = 0
        _EdgeFadeRange ("边缘淡出范围", Range(0.001, 1)) = 0.6
        [HDR] _EdgeColor ("边1颜色", Color) = (1, 0, 0, 1)
        [HDR] _EdgeColor2 ("边2颜色", Color) = (0, 1, 0, 1)
        _BlackEdgeAlphaFactor ("黑边的透明系数", Range(0, 1)) = 0.5

        [Toggle] _FresnelOn ("开启轮廓光", Int) = 0
        [HDR] _FresnelColor ("边缘颜色", Color) = (1, 1, 1, 1)
        [LiteToggle] _FresnelUseCustomData2W ("透明度使用 CustomData2.w", Int) = 0
        _FresnelScale ("菲尼尔缩放", Float) = 1
        _FresnelPower ("菲尼尔强度", Float) = 1

        [Toggle] _FresnelRevertOn ("开启边缘变透", Int) = 0
        _RimTransparencyIntensity ("边缘半透强度", Range(0.1, 50)) = 1

        [HideInInspector] _Alpha ("_Alpha", Range(0, 1)) = 1

        // 该Shader逻辑不会使用模板测试，仅UI Mask时使用
        _Stencil ("Stencil ID", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" "Queue" = "Transparent"
            "PreviewType" = "Plane"
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
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            
            ColorMask [_ColorMask]
            
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            
            //Blend [_SrcMode][_DstMode], Zero [_DstMode]
            // 旧方式导致 alpha 恒为 0，暂时不知道当初这么做的目的。
            // 在 HDR 下无法在 UI 上绘制出来，需要改为 One OneMinusSrcAlpha 累积真实覆盖率的方式，理论上应该不影响粒子特效的叠加，出现问题的话考虑再改回去。
            Blend [_SrcMode][_DstMode], One OneMinusSrcAlpha
            
            Cull [_CullMode]
            
            HLSLPROGRAM
            // -------------------------------------
            // Shader Stages
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert_simple
            #pragma fragment frag_simple

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma shader_feature _ _MAINTEXUSESCREENUV_ON
            
            #pragma multi_compile _ _BLEND_VOLUME_COLOR
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _MAINTEXBLEND_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTURERADIALUVON_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTUREROTATIONUVON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONON_ON
            #pragma shader_feature_local_fragment _ _DISSOLVEON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONRADIALUVON_ON
            #pragma shader_feature_local_fragment _ _FRESNELON_ON
            #pragma shader_feature_local_fragment _ _FRESNELREVERTON_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "libs/VFXPassLOD1.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "VFX.VFXSimpleInspector"
}