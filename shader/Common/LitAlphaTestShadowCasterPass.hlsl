#ifndef XKNIGHT_LIT_ALPHA_TEST_SHADOW_CASTER_PASS_INCLUDED
#define XKNIGHT_LIT_ALPHA_TEST_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"
#include "../Scene/Wind.hlsl"

float3 _LightDirection;
float3 _LightPosition;

float4 GetShadowPositionHClip(VertexAttributes input)
{
	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

	#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - positionWS);
	#else
	float3 lightDirectionWS = _LightDirection;
	#endif

	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

	#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	#endif

	return positionCS;
}

SurfaceInput ShadowPassVertex(VertexAttributes input)
{
	SurfaceInput output = (SurfaceInput)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	
	output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	#ifdef _WIND_ON
	Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
	#endif
	
	output.positionCS = GetShadowPositionHClip(input);
	
	return output;
}

half4 ShadowPassFragment(SurfaceInput input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	
	Alpha(SampleAlbedoAlpha(input.uv0, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a, _BaseColor, _Cutoff);

	#if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
	#endif
	
	return 0;
}

#endif // XKNIGHT_LIT_ALPHA_TEST_SHADOW_CASTER_PASS_INCLUDED
