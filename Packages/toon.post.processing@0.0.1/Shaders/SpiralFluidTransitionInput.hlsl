#if !defined(SPIRAL_FLUID_TRANSITION_INPUT_H)
#define SPIRAL_FLUID_TRANSITION_INPUT_H

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
float2 _Center;
float4 _TransitionParams;
float4 _VisualParams;
float4 _ToFinishParams;
float4 _DistortionTilingFlow;
float4 _TextureDistortionParams;
float4 _RadiusParams;
float4 _SwirlParams;
float4 _NoiseParams;
float4 _EdgeParams;
float4 _FoldParams;
float4 _ExposureParams;
float4 _LayerParams;
float4 _WarmBrightLutParams;
CBUFFER_END

TEXTURE2D_X(_FromTex); SAMPLER(sampler_FromTex);
TEXTURE2D_X(_ToTex); SAMPLER(sampler_ToTex);
TEXTURE2D(_DistortionTex); SAMPLER(sampler_DistortionTex);
TEXTURE2D(_WarmBrightLut); SAMPLER(sampler_WarmBrightLut);

#endif
