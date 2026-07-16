#ifndef XKNIGHT_CRYSTAL_SHADOW_CASTER_PASS_INCLUDED
#define XKNIGHT_CRYSTAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"

#include "./CrystalDissolve.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
	
	#if defined( _ALPHATEST_ON )
	float2 texcoord		: TEXCOORD0;
	#endif
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	#if defined( _ALPHATEST_ON )
	float2 uv			: TEXCOORD0;
	#endif
	
	DISSOLVE_FACTOR(1)
	
	float4 positionCS	: SV_POSITION;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetShadowPositionHClip(Attributes input)
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

Varyings ShadowPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	
	#if defined( _ALPHATEST_ON )
	output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	#endif
	output.positionCS = GetShadowPositionHClip(input);

	DISSOLVE_TRANSFER_FACTOR(output, input.positionOS, _DissolveDir)
	
	return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	
	#if defined( _ALPHATEST_ON )
	//half baseAlpha = _BaseColor.a + _ShadowCasterBaseColorAlphaOffset;
	half baseAlpha = _BaseColor.a;
	half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
	clip(albedoAlpha.a * baseAlpha - _Cutoff);
	#endif

	#if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
	#endif
	
	half4 fakeColor = (half4)0;
	DISSOLVE_APPLY(fakeColor, input.uv, input.directionFactor)
	
	return 0;
}

#endif // XKNIGHT_CRYSTAL_SHADOW_CASTER_PASS_INCLUDED
