#ifndef __TREE_1_FORWARD_PASS__
#define __TREE_1_FORWARD_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define ao saturate(input.color.a + _AOOffset)
#define _TYPE_PLANT_ON
// #define _RECEIVE_SHADOWS_OFF 1

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "./Wind.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float4 uv0 : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 uv2 : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;

    half4 uv0 : TEXCOORD0;
    half4 uv1 : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half3 faceNormalWS : TEXCOORD3;
    half4 color : TEXCOORD4;
    float3 positionWS : TEXCOORD5;
    float3 positionOS : TEXCOORD6;
	
    UBPA_FOG_COORDS(7)
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord : TEXCOORD8;
    #endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 9);

	float objectId : TEXCOORD10;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionOS = input.positionOS;
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.faceNormalWS = TransformObjectToWorldNormal(input.color.xyz * 2.0 - 1.0);

    // #ifdef _WIND_ON
    // Wind(input.uv2, output.normalWS, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    // #endif

    output.color = input.color;
    output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
    #endif

    output.positionCS = TransformWorldToHClip(output.positionWS);

    UBPA_TRANSFER_FOG(output, output.positionWS);

	// 基于中心点计算对象id
	VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
	float objectId = dot(vertexInput0.positionWS, 1);
	output.objectId = objectId;

    return output;
}

void MRTBufferPass(Varyings input, float objectId, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
{
	half4 color0 = 0;
	color0.r = _BloomFactor;
	color0.g = _WaterColorOn;
	color0.b = objectId * _SceneSpaceOutlineOn;
	outForwardBuffer0 = color0;

	half4 color1 = 0;
	color1.rgb = NormalizeNormalPerPixel(input.normalWS);
	outForwardBuffer1 = color1;

	half4 color2 = 0;
	color2.r = input.positionCS.z;
	outForwardBuffer2 = color2;
}

void LitPassFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC
	, out half4 outColor : SV_Target0
	#ifdef _MRT_BUFFER
	, out half4 outForwardBuffer0 : SV_Target1
	, out half4 outForwardBuffer1 : SV_Target2
	, out half4 outForwardBuffer2 : SV_Target3
	#endif
	)
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0.xy);
    // clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

    half3 cameraMinusPositionWS = GetCameraPositionWS() - input.positionWS;
    half3 distanceToCamera = length(cameraMinusPositionWS);
    half3 viewDir = normalize(cameraMinusPositionWS);

    half3 faceNormalWS = normalize(input.faceNormalWS);
    half faceNormalWSDotViewDir = clamp(abs(dot(faceNormalWS, viewDir)) - 0.1, 0.0, 1.0);
    half faceCutoff = faceNormalWSDotViewDir * (1.0 - _CutOffset) + _CutOffset;
    half distanceCutoff = clamp(distanceToCamera / _ClipEnhanceDistance, 0.0, 1.0) * _ClipEnhance;

    half cutoff = distanceCutoff + albedoAlpha.a - _Cutoff * faceCutoff;
    clip(cutoff);

    #if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
    #endif

    half3 albedo = lerp(_BaseColor, _TopColor, smoothstep(0.0, 1.0, input.normalWS.y * 0.5 + 0.5)).rgb * albedoAlpha.rgb;

    float4 shadowCoord = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif

    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
	half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #elif !defined (LIGHTMAP_ON)
    half4 shadowMask = unity_ProbesOcclusion;
    #else
	half4 shadowMask = half4(1, 1, 1, 1);
    #endif

    Light light = GetMainLight(shadowCoord, input.positionWS, shadowMask);
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);

    half NdotL = saturate(dot(input.normalWS, light.direction));
    // half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
    half3 directDiffuse = attenuatedLightColor * albedo * NdotL * ao;

    half3 irradiance = lerp(half3(1, 1, 1), SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalize(input.normalWS)), _GIFalloff) * _GIExposure * ao;

    half3 indirectDiffuse = irradiance * albedo;

    half3 color = directDiffuse + indirectDiffuse;

    half3 sss = .0f;
    #ifdef _TRANSLUCENCY_ON
    half3 lightDir = light.direction + input.normalWS * _TranslucencyDistortion;
    half angle = dot(viewDir, -lightDir);
    half transVdotL = saturate(pow(angle, _TranslucencyScattering ) * _TranslucencyStrength);
    sss = transVdotL * _SSSColor;

	// 补光
	// half fakeLightNdotL = pow(saturate(dot(input.normalWS, half3(0, -1, 0))), _TranslucencyFakeLightFalloff) * _TranslucencyFakeLightIntensity;
	// sss += fakeLightNdotL * albedo * _TranslucencyFakeColor;
    #endif

    color += sss;

    // point light
    half3 additionalColor = half3(0, 0, 0);
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light aLight = GetAdditionalLight(lightIndex, input.positionWS);
        half aNdotL = dot(input.normalWS, aLight.direction) * 0.5 + 0.5;
        half3 aDirectDiffuse = aLight.color * (aLight.distanceAttenuation * aLight.shadowAttenuation) * aNdotL * albedo;

        additionalColor += aDirectDiffuse;
    }
    color += additionalColor;

    UBPA_APPLY_FOG(input, color);

	outColor = half4(color, _Alpha);
	
	#ifdef _MRT_BUFFER
	MRTBufferPass(input, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
	#endif // _MRT_BUFFER
}

#endif // __TREE_1_FORWARD_PASS__
