#ifndef __TERRAIN_VT_LIT_INPUT__
#define __TERRAIN_VT_LIT_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _Diffuse_ST, _Normal_ST;
CBUFFER_END

TEXTURE2D(_Diffuse); SAMPLER(sampler_Diffuse);
TEXTURE2D(_Normal); SAMPLER(sampler_Normal);

#endif // __TERRAIN_VT_LIT_INPUT__
