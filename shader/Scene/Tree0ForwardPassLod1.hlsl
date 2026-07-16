#ifndef __TREE_0_FORWARD_PASS_LOD_1__
#define __TREE_0_FORWARD_PASS_LOD_1__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define ao input.color.a
#define _TYPE_PLANT_ON

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Translucency.hlsl"
#include "./Wind.hlsl"

SurfaceInput LitPassVertex(VertexAttributes input)
{
    SurfaceInput output = (SurfaceInput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

	output.color = input.color;
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	output.positionWS = vertexInput.positionWS;
	
	output.normalWS = normalInput.normalWS;
	half3 ambientOrLightmapUV = 0;
    OUTPUT_SH(output.normalWS, ambientOrLightmapUV);

	output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);
	
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.positionSS = ComputeScreenPos(output.positionCS);
	
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
    #endif
	
    UBPA_TRANSFER_FOG(output, output.positionWS);

    return output;
}

half ApplyAO(half normalAO, half withoutSSSAO, half3 positionWS)
{
    half mainLightShadowFade = GetMainLightShadowFade(positionWS);
    return lerp(normalAO, withoutSSSAO, mainLightShadowFade);
}

void LitPassFragment(SurfaceInput input
	, out half4 outColor : SV_Target0
)
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0.xy);
    clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
    #endif

	// 抖动
	#ifdef _DITHER_ON
	DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
		TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
	#endif

    half3 albedo;
    #if _VERTEXCOLOR_ON
	albedo = albedoAlpha.rgb * _BaseColor.rgb * (input.color.rgb + 1 - input.color.a) * _AlbedoExposure;
    #else
    albedo = albedoAlpha.rgb * _BaseColor.rgb;
    #endif

    float4 shadowCoord = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif

    half3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);

    Light light = GetMainLight(shadowCoord, input.positionWS, unity_ProbesOcclusion);
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);

    // return half4(attenuatedLightColor, 1.0);

    half3 sourceAlbedo = albedo;

    half finalAOStrength = ApplyAO(_AOStrength, _AOWitoutSSSStrength, input.positionWS);

    // AO
    albedo = lerp(albedo, albedo * ao, finalAOStrength);

    // half lambert
    half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
    half3 directDiffuse = attenuatedLightColor * NdotL * albedo;

    half3 irradiance = SampleSH(input.normalWS);
    half3 indirectDiffuse = irradiance * albedo;

    // return half4(indirectDiffuse, 1.0);

    half3 color = directDiffuse + indirectDiffuse;

    half3 sss = .0f;
    #ifdef _TRANSLUCENCY_ON
	TranslucencyProperty transProp = (TranslucencyProperty)0;
	transProp.translucencyStrength = _TranslucencyStrength;
	transProp.translucencyDistortion = _TranslucencyDistortion;
	transProp.translucencyScattering = _TranslucencyScattering;
	transProp.translucencyColor = half4(1.0, 1.0, 1.0, 1.0);
	transProp.translucencyAmbient = _TranslucencyAmbient;
	transProp.translucencyShadow = _TranslucencyShadow;

	// 超出光源的视椎体，则关闭SSS
	// if(shadowCoord.z < 1.0 && shadowCoord.z > 0.0)
        sss = Translucency(irradiance, sourceAlbedo, input.normalWS, viewDir, light, 0.4, transProp);

	// 补光
	half fakeLightNdotL = pow(saturate(dot(input.normalWS, half3(0, -1, 0))), _TranslucencyFakeLightFalloff) * _TranslucencyFakeLightIntensity;
	sss += fakeLightNdotL * sourceAlbedo * _TranslucencyFakeColor;
    #endif

    // return half4(sss, 1.0);

    color += sss;

    /*
    // point light
    half3 additionalColor = half3(0, 0, 0);

    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, input.positionWS);
        half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
        half3 directDiffuse = light.color * (light.distanceAttenuation * light.shadowAttenuation) * NdotL * albedo;
        
        additionalColor += directDiffuse;
    }

    color += additionalColor;
    */

    UBPA_APPLY_FOG(input, color);

	half outAlpha = _DitherAlpha;
    outColor = half4(color, outAlpha);
}

#endif // __TREE_0_FORWARD_PASS_LOD_1__
