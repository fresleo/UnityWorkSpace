#ifndef URPWATER_LIGHTING_INCLUDED
#define URPWATER_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Water_Variables.hlsl"
#include "Water_Helpers.hlsl"

void ComputeCaustics(out float3 causticColor, inout GlobalData data, Varyings IN, float3 Ambient)
{
#if UNITY_REVERSED_Z
	real depth = data.rawDepthDst;
#else
	// Adjust Z to match NDC for OpenGL ([-1, 1])
	real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, data.rawDepthDst);
#endif

	float3 worldUV = ComputeWorldSpacePosition(data.refractionUV, depth, UNITY_MATRIX_I_VP);

	float causticFade = DistanceFade(data.refractionData.a, data.pixelDepth, _CausticsStart, _CausticsEnd);

	float4 offsets = frac(_CausticsSpeed * _Time.x);
	float3 CausticsA = SAMPLE_TEXTURE2D(_CausticsTex, URPWater_linear_repeat_sampler, worldUV.xz * _CausticsTiling.xy + offsets.xy).rgb;
	float3 CausticsB = SAMPLE_TEXTURE2D(_CausticsTex, URPWater_linear_repeat_sampler, worldUV.xz * _CausticsTiling.zw + offsets.zw).rgb;
	float3 CausticMix = min(CausticsA, CausticsB);

	Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldUV));
	float shadow = mainLight.shadowAttenuation;
	
	causticColor = CausticMix * max(0, _CausticsIntensity) * causticFade * (shadow + Ambient * 0.5);
}

float3 ComputeMainLightDiffuse(float3 direction, float3 worldNormal)
{
	return saturate(dot(worldNormal, direction));
}

float3 ComputeMainLightSpecular(
	Light mainLight,
	float3 worldNormal,
	float3 worldViewDir,
	float3 specular,
	float smoothness)
{
	smoothness = exp2(10 * smoothness + 1);

	// Unity spec
	return LightingSpecular(mainLight.color, mainLight.direction, worldNormal, worldViewDir, float4(specular, 0), smoothness);
}

Light ComputeMainLight(float3 worldPos)
{
	return GetMainLight(TransformWorldToShadowCoord(worldPos));
}

void ComputeUnderWaterShading(inout GlobalData data, Varyings IN, float3 ambient) 
{
	float3 shallowColor = _Color.rgb * data.refractionData.rgb;
	float3 depthColor = _DepthColor.rgb * data.shadowColor.rgb;
	data.refractionData.rgb = lerp(depthColor, shallowColor, data.depth);
	
	float invDepth = 1 - data.depth;
	data.finalColor = lerp(data.refractionData.rgb, data.refractionData.rgb * saturate(data.mainLight.color) , invDepth);
}

void ComputeLighting(inout GlobalData data, Varyings IN)
{
	Light mainLight = data.mainLight;
	float3 lightColor = mainLight.color;
#if _PLANARREFLECTION_ON
	mainLight.direction.xyz = normalize(_PlanarReflectionLightDirection.xyz);
#endif
	
	float3 mainSpecular = float3(0,0,0);
#if _SPECULAR_ON
	mainSpecular = ComputeMainLightSpecular(mainLight, data.worldNormal, data.worldViewDir, _SpecColor.rgb, _Smoothness);
#endif
	float shadow = mainLight.shadowAttenuation;
	float shadowMask = shadow;
	float3 ambient = SampleSH(data.worldNormal);

	// Shadow
	data.shadowColor = lerp(saturate(ambient * 2), float3(1, 1, 1), shadowMask);

	// Underwater color
	ComputeUnderWaterShading(data, IN, ambient);

#if _CAUSTICS_ON
	float3 caustics = float3(1,1,1);
	ComputeCaustics(caustics, data, IN, ambient);

	data.finalColor += data.finalColor * caustics * saturate(length(lightColor));
#endif
	
	data.addLight = mainSpecular * data.shadowColor; 
	// Shadows
	data.finalColor = data.finalColor * data.shadowColor;
}

void ComputeLighting_LOD1(inout GlobalData data, Varyings IN)
{
	Light mainLight = data.mainLight;
	float3 lightColor = mainLight.color;

	float3 mainSpecular = float3(0,0,0);
#if _SPECULAR_ON	
	mainSpecular = ComputeMainLightSpecular(mainLight, data.worldNormal, data.worldViewDir, _SpecColor.rgb, _Smoothness);
#endif	
	float shadow = mainLight.shadowAttenuation;
	float shadowMask = shadow;
	float3 ambient = SampleSH(data.worldNormal);

	// Shadow
	data.shadowColor = lerp(saturate(ambient * 2), float3(1, 1, 1), shadowMask);

	// Underwater color
	ComputeUnderWaterShading(data, IN, ambient);

// #if _CAUSTICS_ON
// 	float3 caustics = float3(1,1,1);
// 	ComputeCaustics(caustics, data, IN, ambient);
//
// 	data.finalColor += data.finalColor * caustics * saturate(length(lightColor));
// #endif
	
	data.addLight = mainSpecular * data.shadowColor; 
	// Shadows
	data.finalColor = data.finalColor * data.shadowColor;
}


#endif