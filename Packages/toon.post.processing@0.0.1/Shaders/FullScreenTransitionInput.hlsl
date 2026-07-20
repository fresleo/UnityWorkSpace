/*******************************************************************************
* File: FullScreenTransitionInput.hlsl
 * Author: os.yongzi.xie
 * Data: 2026/04/23 10:00
 * Description: 后处理中 传送领域的shader Input 参数
 * Notice:无
 ******************************************************************************/
#if !defined(FULL_SCREEN_TRANSITION_INPUT_H)
#define FULL_SCREEN_TRANSITION_INPUT_H
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
half _MaxFarDepth;
float4 _TransitionCenterPosition;
half _MaxRadius;
float4 _NoiseTex_ST;
float4 _NoiseDir;
half _NoiseIntensity;
half4 _TransitionColor, _FillFogBrightColor;
half _FillFogNoiseScale;
float4 _FillFogNoiseSpeed;
half _FillFogDark, _FillFogIntensity;
half _BlendAmount;
half _AlphaMultiFactor; 
half4 _EdgeColor;//
half _EdgeWidth;//
half _FBMMode;
// --------------基础--------------------------
float4 _MainTex_ST;
//half _MainTexAutoScale;
float4 _Color;
half _MainTextureRadialUVOn;
half _MainTexMultiAlpha;
half _MainTexSingleChannelOn;
half _MainTexChannel;
half _MainTexMultiFactor;
float4 _MainTexMask_ST;
half _MainTexMaskChannel;
// ---------------扭曲-------------------------
half _DistortionOn;
half _DistortionRadialUVOn;
half _DistortionAffectDissolve;
half _DistortionAffectMainTexture;
half _DistortionAffectMainMaskTexture;
float4 _DistortionNoiseTex_ST;
half4 _DistortTile;
half4 _DistortDir;
half _DistortionIntensity;
// --------------溶解--------------------------
half _DissolveOn;
float4 _DissolveTex_ST;
float4 _DissolveDirectionTex_ST;
half _DissolveTexOffsetStop;
half _DissolveDirectionTexOffsetStop;
half _DissolveTexChannel;
half _DissolveFadingMin;
half _DissolveFadingMax;
half _DissolveClipOn;
half _Cutoff;
half _PixelDissolveOn;
half _PixelWidth;
half _DissolveEdgeOn;
half _DissolveEdgeWidthByCustomData_W;
half _DissolveEdgeWidthTexture;
half _EdgeFadeRange;
//  和原来的重合了
half _EdgeWidth1;
//  和原来的重合了
half4 _EdgeColor1;
half4 _EdgeColor2;
half _BlackEdgeAlphaFactor;

CBUFFER_END
            
TEXTURE2D_X_FLOAT(_CameraCharacterDepthTexture); SAMPLER(sampler_CameraCharacterDepthTexture);
TEXTURE2D_X_FLOAT(_CameraSceneDepthTexture); SAMPLER(sampler_CameraSceneDepthTexture);
TEXTURE2D_X(_NoiseTex); SAMPLER(sampler_NoiseTex);
TEXTURE2D_X(_BlendTex); SAMPLER(sampler_BlendTex);

//TEXTURE2D_X(_MainTexMask); SAMPLER(sampler_MainTexMask);
TEXTURE2D_X(_MainTex); SAMPLER(sampler_MainTex);
//TEXTURE2D_X(_DistortionNoiseTex); SAMPLER(sampler_DistortionNoiseTex);
//TEXTURE2D_X(_DissolveTex); SAMPLER(sampler_DissolveTex);
//TEXTURE2D_X(_DissolveDirectionTex); SAMPLER(sampler_DissolveDirectionTex);
sampler2D _MainTexMask;
sampler2D _DistortionNoiseTex;
sampler2D _DissolveTex;
sampler2D _DissolveDirectionTex;
#endif