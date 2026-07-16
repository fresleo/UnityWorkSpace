#ifndef XKNIGHT_LIT_PRE_ALPHA_TEST_PASS_INCLUDED
#define XKNIGHT_LIT_PRE_ALPHA_TEST_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
	float2 texcoord		: TEXCOORD0;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float2 uv			: TEXCOORD0;
	float4 positionCS	: SV_POSITION;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings PreAlphaTestPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

	output.positionCS = vertexInput.positionCS;
	output.uv = input.texcoord;

	return output;
}

half4 PreAlphaTestPassFragment(Varyings input) : SV_TARGET
{
	half baseAlpha = _BaseColor.a * SAMPLE_TEXTURE2D(_AlphaTestMap, sampler_AlphaTestMap, input.uv).a;
	clip(baseAlpha - _Cutoff);

	#if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
	#endif

	return 0;
}

#endif // XKNIGHT_LIT_PRE_ALPHA_TEST_PASS_INCLUDED
