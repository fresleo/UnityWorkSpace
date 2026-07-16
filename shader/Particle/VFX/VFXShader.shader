Shader "XKnight/Particle/VFX"
{
    Properties
    {
        _MainTex ("主纹理", 2D) = "white" {}
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

        // [Space(20)]
        [LiteToggle] _MainTexGray ("主纹理去色", Int) = 0
        [LiteToggle] _MainTexOffsetStop ("禁用主纹理自动滚动", Int) = 0
        [LiteToggle] _MainTexOffsetUseCustomData_XY ("纹理滚动使用 CustomData.xy", Int) = 0

        _BumpMap ("法线贴图", 2D) = "bump" {}
        _BumpScale ("法线强度", Range(0, 2)) = 1
        [LiteToggle] _BumpMapOffsetStop ("禁用法线纹理自动滚动", Int) = 0

        [Space]
        [Header(Main Color)]
        [HDR] _Color ("主纹理颜色", Color) = (1, 1, 1, 1)
        [LiteToggle] _MainTexMultiAlpha ("主纹理预乘Alpha(主纹理,主颜色,顶点色)", Int) = 0
        [LiteToggle] _MainTexUseScreenUV ("主纹理使用屏幕坐标 UV", Int) = 0
        [LiteToggle] _OnlyMainTexUseScreenUV ("只让主纹理使用屏幕坐标 UV", Int) = 0

        [LiteToggle] _MainTexSingleChannelOn ("主纹理使用单通道", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MainTexChannel ("主纹理通道", Int) = 0

        _MainTexMultiFactor ("主纹理预乘系数", Float) = 1

        [Header(Back Face)]
        [LiteToggle] _BackFaceOn ("背面(需要手动Cull Off)", Int) = 0
        [HDR] _BackFaceColor ("背面色", Color) = (0.5, 0.5, 0.5, 1)

        [Header(Mask Tex)]
        _MainTexMask ("主纹理遮罩", 2D) = "white" {}
        [LiteToggle] _MainTexMaskUClamp ("U Mask Clamp", Float) = 0
        [LiteToggle] _MainTexMaskVClamp ("V Mask Clamp", Float) = 0
        [LiteToggle] _MainTexMaskOffsetStop ("禁用主纹理遮罩自动滚动", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MainTexMaskChannel ("遮罩通道", Int) = 0

        [Space]
        [Toggle] _MainTextureRadialUVOn ("极坐标", Int) = 0
        
        [VFXRotationToggle(_MainTexRotationAngle, _MainTexRotationCenter)]
        _MainTextureRotationUVOn ("主纹理旋转", Int) = 0
        _MainTexRotationAngle ("主纹理旋转角度", Range(0, 360)) = 0
        _MainTexRotationCenter ("主纹理旋转中心", Vector) = (0.5, 0.5, 0, 0)
        
        [Space(22)] [Toggle(_MAINTEX_DISPERSION_ON)]
        _MainTexDispersion ("主纹理色散", Float) = 0
        _MainTexHorizontalDispersion ("色散强度-水平", Range(-1, 1)) = 0
        _MainTexVerticalDispersion ("色散强度-垂直", Range(-1, 1)) = 0
        
        

        [Header(BlendMode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode ("Src Mode", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstMode ("Dst Mode", Int) = 10

        [LiteToggle] _FullScreenOn ("开启全屏效果", Int) = 0

        [Toggle] _CameraDistanceAngleFadeOn ("开启摄像机距离|角度过渡", Int) = 0
        [IntRange] _CameraDistanceFading ("摄像机距离过渡值", Range(0, 255)) = 75
        [FloatRange] _ViewAngleFading ("观察角度过渡值", Range(0, 5)) = 1

        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("剔除模式", Float) = 0
        [LiteToggle] _DoubleFaceDoublePassOn ("双面双通道", Float) = 0
        [LiteToggle] _ZWriteMode ("写入深度", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("深度测试", Float) = 4
        //-------------VERTEX WAVE----------------
        [Toggle] _VertexWaveOn ("开启顶点摆动", Int) = 0

        [Enum(NoiseFunction,0,Texture,1)] _NoiseUseAttenMaskMap ("顶点摆动衰减方式", Int) = 0

        //控制贴图流动速度属性
        _VertexWaveSpeed ("速度", Float) = 1
        [LiteToggle] _VertexWaveSpeedManual ("关闭速度", Int) = 0
        _VertexWaveIntensity ("密度", Float) = 1

        [Enum(No,0,Yes,1)] _VertexWaveAtten_VertexColor ("是否顶点色RGB控制方向?", Float) = 0

        _VertexWaveDirAtten ("方向衰减(xyz:dir, w:len)", Vector) = (1, 1, 1, 1)
        [Enum(No,0,Yes,1)] _VertexWaveDirAlongNormalOn ("方向沿法线运动", Int) = 0
        [Enum(Local,0,World,1)] _VertexWaveDirAtten_LocalSpaceOn ("本地空间方向", Int) = 0
        [Enum(No,0,Yes,1)] _VertexWaveAtten_NormalAttenOn ("法线参与方向改变", Int) = 0
        [LiteToggle] _VertexWaveDirAtten_CustomDataWOn ("方向强度使用 CustomData2.w", Int) = 0

        [Enum(No,0,Yes,1)] _VertexWaveAtten_MaskMapOn ("衰减图来控制扰动?选择一个通道", Int) = 0
        _VertexWaveAtten_MaskMap ("贴图", 2D) = "white"{}
        [Enum(R,0,G,1,B,2,A,3)] _VertexWaveAtten_MaskMapChannel ("衰减图通道", Int) = 0
        [Enum(Play,0,Pause,1)] _VertexWaveAtten_MaskMapOffsetStopOn ("停止衰减 uv 自动滚动", Int) = 0
        [LiteToggle] _VertexWaveAttenMaskOffsetScale_UseCustomeData2_X ("衰减图滚动,customData2.x控制", Int) = 0
        //-------------VETEX WAVE END----------------
        [Toggle] _DistortionOn ("开启扭曲", Int) = 0
        [noscaleoffset] _DistortionNoiseTex ("扭曲贴图", 2D) = "white" {}
        [Toggle] _DistortionRadialUVOn ("极坐标", Int) = 0
        [LiteToggle] _DistortionAffectU ("影响U", Int) = 1
        [LiteToggle] _DistortionAffectV ("影响V", Int) = 1

        _DistortionMaskTex ("遮罩贴图", 2D) = "white"{}
        [Enum(R,0,G,1,B,2,A,3)] _DistortionMaskChannel ("遮罩通道", Int) = 0
        _DistortionIntensity ("强度", Range(-10, 10)) = 0.5
        _DistortTile ("Tiling(xy:层1, zw:层2)", Vector) = (1, 1, 1, 1)
        _DistortDir ("Offset(xy:层1, zw:层2)", Vector) = (0, 1, 0, -1)

        [Space]
        [LiteToggle] _DistortionApplyToOffset ("扭曲流光", Int) = 0
        [LiteToggle] _DistortionApplyToDissolve ("扭曲溶解", Int) = 0
        [LiteToggle] _DistortionAffectMainMaskTexture ("扭曲主纹理 Mask 图", Int) = 0
        [LiteToggle] _DistortionAffectMainTexture ("扭曲主纹理图", Int) = 1
        [LiteToggle] _DistortionMainTextureDispersion ("扭曲主纹理图色散", Int) = 0

        [Toggle] _DissolveOn ("开启溶解", Int) = 0
        _DissolveTex ("溶解图", 2D) = "white" {}
        _DissolveDirectionTex ("溶解方向图", 2D) = "white" {}
        [LiteToggle] _DissolveTexOffsetStop ("禁止溶解图自动滚动", Int) = 0
        [LiteToggle] _DissolveDirectionTexOffsetStop ("禁止溶解方向图自动滚动", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DissolveTexChannel ("溶解图通道", Int) = 0

        [Header(DissolveType)]
        [LiteToggle] _DissolveByVertexColor ("使用顶点色(Alpha)控制", Int) = 0
        [LiteToggle] _DissolveByCustomData_Z ("使用 CustomData.z 控制", Int) = 0

        [Header(DissolveFading)]
        _DissolveFadingMin ("透明 Min", Range(0, 0.2)) = 0
        _DissolveFadingMax ("透明 Max", Range(0, 1.0)) = 0.2

        [Header(Dissolve Clip)]
        [LiteToggle] _DissolveClipOn ("像素剔除", Int) = 1
        _Cutoff ("镂空值", Range(0, 1)) = 0.5

        [Header(PixelDissolve)]
        [LiteToggle] _PixelDissolveOn ("像素化溶解", Float) = 0
        _PixelWidth ("像素化宽", Float) = 10

        [Header(DissolveEdge)]
        [LiteToggle] _DissolveEdgeOn ("开启溶解边", Int) = 0
        _EdgeWidth ("边宽度", Range(0, 0.3)) = 0.1
        [LiteToggle] _DissolveEdgeWidthByCustomData_W ("溶解边受 CustomData.w 控制", Int) = 0
        [LiteToggle] _DissolveEdgeWidthTexture("溶解边受贴图边控制", Int) = 0
        _EdgeFadeRange ("边缘淡出范围", Range(0.001, 1)) = 0.6
        [HDR] _EdgeColor ("边1颜色", Color) = (1, 0, 0, 1)
        [HDR] _EdgeColor2 ("边2颜色", Color) = (0, 1, 0, 1)
        _BlackEdgeAlphaFactor ("黑边的透明系数", Range(0, 1)) = 0.5

        [Toggle] _OffsetOn ("开启流光", Int) = 0
        [Space] [NoScaleOffset] _OffsetTex ("流光纹理", 2D) = "black" {}
        [Space] _OffsetMaskTex ("遮罩纹理", 2D) = "white" {}
        [Enum(R,0,G,1,B,2,A,3)] _OffsetMaskChannel ("遮罩通道", Int) = 0
        [HDR] _OffsetTexColorTint ("层1颜色", Color) = (1, 1, 1, 1)
        [HDR] _OffsetTexColorTint2 ("层2颜色", Color) = (1, 1, 1, 1)
        _OffsetTile ("Tliing(xy:层1, zw:层2)", Vector) = (1, 1, 1, 1)
        _OffsetDir ("Offset(xy:层1, zw:层2)", Vector) = (1, 1, 0, 0)
        [LiteToggle] _OffsetDirTimeInvariant ("禁用流光纹理自动滚动", Int) = 0 // 本质是不让 Time 起效果
        _OffsetBlendIntensity ("混合强度", Range(0, 10)) = 0.5
        
        [Space]
        [LiteToggle] _OffsetRadialUVOn ("极坐标", Int) = 0
        
        [Toggle] _FresnelOn ("开启轮廓光", Int) = 0
        [HDR] _FresnelColor ("边缘颜色", Color) = (1, 1, 1, 1)
        [LiteToggle] _FresnelUseCustomData2W ("透明度使用 CustomData2.w", Int) = 0
        _FresnelScale ("菲尼尔缩放", Float) = 1
        _FresnelPower ("菲尼尔强度", Float) = 1
        _OffsetFresnel ("轮廓光偏移", Vector) = (0, 0, 0, 0)
        _OffsetVertexByNormal ("顶点大一些防止zfighting", Range(0,0.05)) = 0
        [Toggle]_VertexColorCondition ("顶点色控制(a越接近1越倾向顶点色)", Int) = 0
        [Toggle] _FresnelRevertOn ("开启边缘变透", Int) = 0
        [Toggle] _FresnelRevertAlpha ("开启中间变透", Int) = 0
        _RimTransparencyIntensity ("半透强度", Range(0.1, 50)) = 1

        [Toggle] _DepthFadingOn ("深度渐隐(软粒子)", Int) = 0
        _DepthFadingWidth ("渐隐宽", Range(0.01, 3)) = 1
        [Toggle] _LightOn ("开启光照效果", Int) = 0
        [LiteToggle] _CustomMainLightColorOn ("开启自定义主光源颜色", Int) = 0
        [HDR] _CustomMainLightColor ("自定义主光源颜色", Color) = (1, 1, 1, 1)
        [LiteToggle] _CustomMainLightDirectionOn ("开启自定义主光源方向", Int) = 0
        _CustomMainLightDirection ("自定义主光源方向", Vector) = (0, 0, 0, 0)

        [Toggle] _DecalEffectOn ("开启贴花效果", Int) = 0
        [Toggle] _DecalKnifeEdgeEffectOn ("刀痕贴花(侧面)", Int) = 0
        _DecalScale ("贴花缩放(下投影：xz值常用)", Vector) = (1, 1, 1, 0)
        _DecalRotation ("贴花旋转(下投影：y值常用)", Vector) = (0, 0, 0, 0)

        [Toggle] _ParallaxOn ("开启视差（前置条件是开启光照）", Int) = 0
        _ParallaxTex ("视差高度图", 2D) = "white" {}
        [LiteToggle] _ParallaxTexOffsetStop ("禁用视差纹理自动滚动", Int) = 0
        _Parallax ("视差强弱", Range(-0.1, 0.1)) = 0
        
        [Toggle(_HEIGHT_FOG)] _FogOn ("是否受到雾的影响", Int) = 0

        [HideInInspector] _Alpha ("_Alpha", Range(0, 1)) = 1
        
        [Header(Stencil)]
        [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
        [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
        [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0
        
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        // ForwardLit - UniversalForward
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
                Fail [_StencilFail]
                ZFail [_StencilZFail]
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
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature _RECORDING_QUALITY
            #pragma multi_compile _ _BLEND_VOLUME_COLOR

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _MAINTEXBLEND_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTURERADIALUVON_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTUREROTATIONUVON_ON
            #pragma shader_feature_local_fragment _ _MAINTEX_DISPERSION_ON
            #pragma shader_feature_local _ _DECALEFFECTON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONRADIALUVON_ON
            #pragma shader_feature_local_fragment _ _OFFSETON_ON
            #pragma shader_feature_local_fragment _ _DISSOLVEON_ON
            #pragma shader_feature_local _ _LIGHTON_ON
            #pragma shader_feature_local_fragment _ _DEPTHFADINGON_ON
            #pragma shader_feature_local_vertex _ _VERTEXWAVEON_ON
            
            //#pragma shader_feature_local _ _FRESNELON_ON
            //#pragma shader_feature_local _ _FRESNELREVERTON_ON

            #pragma shader_feature_local _ _HEIGHT_FOG
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "libs/VFXPass.hlsl"
            ENDHLSL
        }

        // ForwardLit2 - UniversalForward2
        Pass
        {
            Name "ForwardLit2"
            Tags
            {
                "LightMode" = "UniversalForward2"
            }
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            
            ColorMask [_ColorMask]
            
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            
            //Blend [_SrcMode][_DstMode], Zero [_DstMode]
            Blend [_SrcMode][_DstMode], One OneMinusSrcAlpha
            
            Cull Back // pass1 强制画 Front，这里强制画 Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature _RECORDING_QUALITY
            #pragma multi_compile _ _BLEND_VOLUME_COLOR

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _MAINTEXBLEND_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTURERADIALUVON_ON
            #pragma shader_feature_local_fragment _ _MAINTEXTUREROTATIONUVON_ON
            #pragma shader_feature_local _ _DECALEFFECTON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONON_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONRADIALUVON_ON
            #pragma shader_feature_local_fragment _ _OFFSETON_ON
            #pragma shader_feature_local_fragment _ _DISSOLVEON_ON
            #pragma shader_feature_local _ _LIGHTON_ON
            #pragma shader_feature_local_fragment _ _DEPTHFADINGON_ON
            #pragma shader_feature_local_vertex _ _VERTEXWAVEON_ON
            
            //#pragma shader_feature_local _ _FRESNELON_ON
            //#pragma shader_feature_local _ _FRESNELREVERTON_ON

            #pragma shader_feature_local _ _HEIGHT_FOG

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "libs/VFXPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "VFX.VFXInspector"
}