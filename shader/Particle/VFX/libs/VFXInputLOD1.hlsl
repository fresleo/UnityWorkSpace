#if !defined(VFX_INPUT_LOD1)
#define VFX_INPUT_LOD1

sampler2D _MainTex;
sampler2D _MainTexBlend;
sampler2D _MainTexMask;// (r,a)
sampler2D _DistortionNoiseTex;//(xy : layer1,zw : layer2)
sampler2D _DistortionMaskTex;//(r,a)
sampler2D _DissolveTex;
sampler2D _DissolveDirectionTex;

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    
    float4 _MainTexBlend_ST;

    half _MainTexBlendIntensity;
    half _MainTexBlendSample;
    half _MainTexBlendRampChannal;
    half _MainTexBlendRampY;

    half4 _MainTexColorCorrection;

    int _MainTexColorIntensityUseCustomData_W;
    int _MainTexGray;
    half _MainTexUClamp;
    half _MainTexVClamp;

    float4 _MainTexRotationCenter;
    float _MainTexRotationAngle;

    int _MainTexSingleChannelOn;
    int _MainTexChannel;
    int _MainTexMultiAlpha;
    int _BackFaceOn;

    float4 _MainTex_ST;
    half4 _BackFaceColor;

    int _MainTexAutoScale;
    // int _MainTexUseScreenUV;
    int _OnlyMainTexUseScreenUV;
    half _MainTexMultiFactor;
    half _MainTexOffsetStop;

    half _MainTexOffsetUseCustomData_XY;
    //int _MainTextureRadialUVOn;
    int _MainTexMaskUseCustomData2_XY;
    half _MainTexMaskOffsetStop;

    float4 _MainTexMask_ST;

    int _MainTexMaskChannel;
    //float _DistortionOn;
    int _DistortionMaskChannel;
    half _DistortionAutoScale;

    //int _DistortionRadialUVOn;
    int _DistortionAffectU;
    float _DistortionIntensity;

    float4 _DistortionMaskTex_ST;
    float4 _DistortTile, _DistortDir;

    int _DistortionAffectV;
    int _DistortionApplyToOffset;

    //float _DissolveOn;
    half _DistortionAffectDissolve;
    half _DistortionAffectMainMaskTexture;
    half _DistortionAffectMainTexture;
    half _DissolveByVertexColor;

    int _DissolveByCustomData_Z;
    int _DissolveOffsetByCustomData2xy;
    half _DissolveTexChannel;
    half _DissolveTexOffsetStop, _DissolveDirectionTexOffsetStop;

    float4 _DissolveTex_ST;
    float4 _DissolveDirectionTex_ST;

    half _DissolveClipOn;
    half _Cutoff;
    half _PixelDissolveOn;
    half _PixelWidth;

    half _DissolveEdgeOn;
    half _EdgeWidth;
    half _DissolveFadingMin;
    half _DissolveFadingMax;
    half _EdgeColorMultiVertexColor;
    half _EdgeFadeRange;
    half4 _EdgeColor;
    half4 _EdgeColor2;
    half _BlackEdgeAlphaFactor;

    //float _FresnelOn;
    half _FresnelScale;
    half _FresnelPower,_VertexColorCondition;
    half _OffsetVertexByNormal;
    half _FresnelUseCustomData2W;
    half4 _FresnelColor,_OffsetFresnel;

    //float _FresnelRevertOn;
    half _RevertFresnelAlpha;
    half _RimTransparencyIntensity;

    half _Alpha;

    float4 _ClipRect;
CBUFFER_END

half _IsGammaUI;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

#endif // VFX_INPUT_LOD1
