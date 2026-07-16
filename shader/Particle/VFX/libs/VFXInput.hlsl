#if !defined(VFX_INPUT_CGINC)
#define VFX_INPUT_CGINC

sampler2D _MainTex;

sampler2D _MainTexBlend;

sampler2D _BumpMap;
sampler2D _MainTexMask;// (r,a)

sampler2D _DistortionNoiseTex;//(xy : layer1,zw : layer2)
sampler2D _DistortionMaskTex;//(r,a)

sampler2D _DissolveTex;
sampler2D _DissolveDirectionTex;

sampler2D _OffsetTex;
sampler2D _OffsetMaskTex;//(r,a)
sampler2D _VertexWaveAtten_MaskMap;//r

sampler2D _ParallaxTex;

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float4 _MainTexBlend_ST;
    half _MainTexBlendIntensity;
    half _MainTexBlendSample;
    half _MainTexBlendRampChannal;
    half _MainTexBlendRampY;
    half _MainTexUClamp;
    half _MainTexVClamp;
    half _BumpScale;
    half4 _MainTexColorCorrection;
    float4 _BumpMap_ST;
    int _MainTexGray;
    int _MainTexSingleChannelOn;
    int _MainTexChannel;
    int _MainTexMultiAlpha;
    half _MainTexMultiFactor;
    int _BackFaceOn;
    half4 _BackFaceColor;
    float4 _MainTex_ST;
    half _MainTexOffsetStop, _MainTexOffsetUseCustomData_XY;
    //int _MainTextureRadialUVOn;
    half _BumpMapOffsetStop;

    //int _DecalEffectOn;
    int _FullScreenOn;
    
    int _CameraDistanceAngleFadeOn;
    half _CameraDistanceFading;
    half _ViewAngleFading;

    float4 _MainTexMask_ST;
    half _MainTexMaskUClamp;
    half _MainTexMaskVClamp;
    int _MainTexUseScreenUV;
    int _OnlyMainTexUseScreenUV;
    half _MainTexMaskOffsetStop;
    int _MainTexMaskChannel;

    float4 _MainTexRotationCenter;
    float _MainTexRotationAngle;

    // 色散相关参数
    float _MainTexHorizontalDispersion;
    float _MainTexVerticalDispersion;

    //float _VertexWaveOn;
    half _NoiseUseAttenMaskMap;
    float _VertexWaveSpeed;
    int _VertexWaveSpeedManual;
    float _VertexWaveIntensity;

    // vertex wave attenuations
    half _VertexWaveAtten_VertexColor;
    float4 _VertexWaveDirAtten;
    int _VertexWaveDirAlongNormalOn;
    int _VertexWaveDirAtten_LocalSpaceOn;
    int _VertexWaveAtten_NormalAttenOn;
    int _VertexWaveDirAtten_CustomDataWOn;

    int _VertexWaveAtten_MaskMapOn;
    float4 _VertexWaveAtten_MaskMap_ST;
    int _VertexWaveAtten_MaskMapOffsetStopOn;
    int _VertexWaveAtten_MaskMapChannel;
    int _VertexWaveAttenMaskOffsetScale_UseCustomeData2_X;

    //float _DistortionOn;
    int _DistortionMaskChannel;
    float4 _DistortionMaskTex_ST;
    float _DistortionIntensity;
    float4 _DistortTile, _DistortDir;
    //int _DistortionRadialUVOn;
    int _DistortionAffectU;
    int _DistortionAffectV;
    int _DistortionApplyToOffset;
    int _DistortionApplyToDissolve;
    int _DistortionAffectMainMaskTexture;
    int _DistortionAffectMainTexture;
    int _DistortionMainTextureDispersion;

    //float _DissolveOn;
    half _DissolveByVertexColor;
    int _DissolveByCustomData_Z;
    half _DissolveTexChannel;
    float4 _DissolveTex_ST;
    float4 _DissolveDirectionTex_ST;
    half _DissolveTexOffsetStop, _DissolveDirectionTexOffsetStop;
    half _DissolveClipOn;
    half _Cutoff;

    half _PixelDissolveOn;
    half _PixelWidth;

    half _DissolveEdgeOn;
    half _DissolveEdgeWidthByCustomData_W;
    half _DissolveEdgeWidthTexture;
    half _EdgeWidth;
    half _EdgeFadeRange;
    half4 _EdgeColor;
    half4 _EdgeColor2;
    half _BlackEdgeAlphaFactor;

    half _DissolveFadingMin;
    half _DissolveFadingMax;

    //float _OffsetOn;
    float4 _OffsetMaskTex_ST;
    half _OffsetMaskChannel;
    half4 _OffsetTexColorTint, _OffsetTexColorTint2;
    float4 _OffsetTile, _OffsetDir;
    int _OffsetDirTimeInvariant;
    half _OffsetBlendIntensity;
    // radial uv 
    int _OffsetRadialUVOn;

    half _FresnelOn;
    half _FresnelUseCustomData2W;
    half4 _FresnelColor,_OffsetFresnel;
    half _FresnelScale,_VertexColorCondition;
    half _FresnelPower;
    half _OffsetVertexByNormal;

    half _FresnelRevertOn;
    half _FresnelRevertAlpha;
    half _RimTransparencyIntensity;

    //int _DepthFadingOn;
    half _DepthFadingWidth;
    //float _LightOn;

    // 自定义主光源颜色
    half _CustomMainLightColorOn;
    half4 _CustomMainLightColor;
    half _CustomMainLightDirectionOn;
    half4 _CustomMainLightDirection;

    float _DecalKnifeEdgeEffectOn;
    float3 _DecalScale;
    float4 _DecalRotation;

    half _ParallaxOn;
    half _Parallax;
    float4 _ParallaxTex_ST;
    half _ParallaxTexOffsetStop;
    
    half _Alpha;
CBUFFER_END

half _IsGammaUI;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

#endif // VFX_INPUT_CGINC
