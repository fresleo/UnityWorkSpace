#ifndef __SHEET_FOG_FORWARD_PASS__
#define __SHEET_FOG_FORWARD_PASS__

/*
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
*/

#include "../../ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "../../ShaderLibrary/ASENoise.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 vertexColor : COLOR;

    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float4 vertexColor : COLOR;

    float4 clipPosV : TEXCOORD0;
    float4 lightmapUVOrVertexSH : TEXCOORD1;
    half4 fogFactorAndVertexLight : TEXCOORD2;

    float4 tSpace0 : TEXCOORD3;
    float4 tSpace1 : TEXCOORD4;
    float4 tSpace2 : TEXCOORD5;

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
		float4 shadowCoord : TEXCOORD6;
    #endif
    #if defined( DYNAMICLIGHTMAP_ON )
		float2 dynamicLightmapUV : TEXCOORD7;
    #endif

    float4 uv : TEXCOORD8;
    float4 eyeDepth : TEXCOORD9;

    float3 positionWS : TEXCOORD10;
    UBPA_FOG_COORDS(11)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.positionOS.xyz));
    float eyeDepth = -objectToViewPos.z;
    o.eyeDepth = float4(eyeDepth, 0, 0, 0);

    o.vertexColor = v.vertexColor;
    o.uv = v.texcoord;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);

    o.tSpace0 = float4(normalInput.normalWS, vertexInput.positionWS.x);
    o.tSpace1 = float4(normalInput.tangentWS, vertexInput.positionWS.y);
    o.tSpace2 = float4(normalInput.bitangentWS, vertexInput.positionWS.z);

    #if defined( LIGHTMAP_ON )
		OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
    #endif

    #if !defined( LIGHTMAP_ON )
		OUTPUT_SH(normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz);
    #endif

    #if defined( DYNAMICLIGHTMAP_ON )
		o.dynamicLightmapUV.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    half fogFactor = 0;
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
		o.shadowCoord = GetShadowCoord( vertexInput );
    #endif

    o.positionCS = vertexInput.positionCS;
    o.clipPosV = vertexInput.positionCS;

    o.positionWS = vertexInput.positionWS;
    UBPA_TRANSFER_FOG(o, vertexInput.positionWS);

    return o;
}

TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

float SampleSceneDepth(float2 uv)
{
	float depth = 0;
	
	depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
    
	return depth;
}

void ApplyViewAngleFade(inout float angleFadeFactor, float fresnel, float viewAngleFade)
{
	float fadeFactor = max(fresnel, 0.001);
	angleFadeFactor = lerp(1.0, 1.0f - fadeFactor, viewAngleFade);
}

half4 frag(VertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    #if defined( LOD_FADE_CROSSFADE )
		LODFadeCrossFade( IN.positionCS );
    #endif

    float3 WorldNormal = normalize(IN.tSpace0.xyz);
    float3 WorldTangent = IN.tSpace1.xyz;
    float3 WorldBiTangent = IN.tSpace2.xyz;

    float3 WorldPosition = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz - WorldPosition;

    float4 ScreenPos = ComputeScreenPos(IN.clipPosV);

    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

    float4 ShadowCoords = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		ShadowCoords = IN.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
    #endif

    WorldViewDirection = SafeNormalize(WorldViewDirection);

	// Simple 噪声
    float ParticleStableRandom = IN.uv.z; // 粒子系统产生的稳定随机数
    float2 texCoord3 = IN.uv.xy + ((_SimpleNoiseAnimation * _TimeParameters.x) + (ParticleStableRandom * 10.0));
    float simpleNoise1 = SimpleNoise(texCoord3 * _SimpleNoiseScale);
    float SimpleNoise18 = saturate((simpleNoise1 - _SimpleNoiseRemap) / (1.0 - _SimpleNoiseRemap));
    float lerpResult34 = lerp(1.0, SimpleNoise18, _SimpleNoiseAmount);

	// Simplex 噪声
    float2 texCoord20 = IN.uv.xy;
    float simplePerlin3D19 = snoise((float3(texCoord20, 0.0) + (_SimplexNoiseAnimation * _TimeParameters.x) + (ParticleStableRandom * 20.0)) * _SimplexNoiseScale);
    simplePerlin3D19 = simplePerlin3D19 * 0.5 + 0.5;
    float SimplexNoise25 = saturate((simplePerlin3D19 - _SimplexNoiseRemap) / (1.0 - _SimplexNoiseRemap));
    float lerpResult33 = lerp(1.0, SimplexNoise25, _SimplexNoiseAmount);

	// Voronoi 噪声
    float mulTime6 = _TimeParameters.x * _VoronoiNoiseAnimation.z;
    float time2 = mulTime6;
    float2 voronoiSmoothId2 = 0;
    float2 texCoord7 = IN.uv.xy + (_VoronoiNoiseAnimation.xy * _TimeParameters.x);
    float2 coords2 = texCoord7 * _VoronoiNoiseScale;
    float2 id2 = 0;
    float2 uv2 = 0;
    float voroi2 = voronoi2(coords2, time2, id2, uv2, 0, voronoiSmoothId2);
    float VoronoiNoise12 = saturate((voroi2 - _VoronoiNoiseRemap) / (1.0 - _VoronoiNoiseRemap));
    float lerpResult5 = lerp(1.0, VoronoiNoise12, _VoronoiNoiseAmount);

	// 组合噪声
    float Noise36 = lerpResult34 * lerpResult33 * lerpResult5;
    float RemappedNoise60 = saturate((Noise36 - _CombinedNoiseRemap) / (1.0 - _CombinedNoiseRemap));

	// 表面深度的衰减
    float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
    ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
    float screenDepth50 = LinearEyeDepth(SampleSceneDepth(ase_screenPosNorm.xy), _ZBufferParams);
    float distanceDepth50 = abs((screenDepth50 - LinearEyeDepth(ase_screenPosNorm.z, _ZBufferParams)) / (_SurfaceDepthFade));
    float SurfaceDepthFade55 = saturate(distanceDepth50);

	// 计算相机深度衰减值
    float eyeDepth = IN.eyeDepth.x;
    float cameraDepthFade87 = ((eyeDepth - _ProjectionParams.y - _CameraDepthFadeOffset) / _CameraDepthFadeRange);
    float CameraDepthFade88 = saturate(cameraDepthFade87);

	// 计算径向遮罩值
    float2 texCoord76 = IN.uv.xy * float2(2, 2) + float2(-1, -1);
    float RadialMask = saturate(1.0 - length(texCoord76));

	// 反照率颜色
	float4 albedoCol = _Albedo * IN.vertexColor;
	
    float3 BaseColor = float3(0.5, 0.5, 0.5);
    float3 Normal = float3(0, 0, 1);
    float3 Emission = albedoCol.rgb;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = 0.5;
    float Occlusion = 1;
    float Alpha = RemappedNoise60 * SurfaceDepthFade55 * CameraDepthFade88 * RadialMask * albedoCol.a;
	Alpha = saturate(Alpha);

	#ifdef _WHOLEFADE_ON
	// 视角角度衰减
	float angleFadeFactor;
	ApplyViewAngleFade(angleFadeFactor, 0, _ViewAngleFading);
	Alpha *= angleFadeFactor;

	// 摄像机距离衰减
	float distanceToCamera = distance(IN.positionWS.xyz, _WorldSpaceCameraPos);
	float fadeDistance = saturate(distanceToCamera / _CameraDistanceFading);
	Alpha = saturate(Alpha) * fadeDistance;
	#endif // _WHOLEFADE_ON

	// ----------------------------------------------------------------
	// InputData
    InputData inputData = (InputData)0;
    inputData.positionWS = WorldPosition;
    inputData.viewDirectionWS = WorldViewDirection;
    inputData.normalWS = WorldNormal;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		inputData.shadowCoord = ShadowCoords;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
		inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;

    float3 SH = IN.lightmapUVOrVertexSH.xyz;

    #if defined( DYNAMICLIGHTMAP_ON )
		inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, IN.dynamicLightmapUV.xy, SH, inputData.normalWS);
    #else
		inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
    #endif

    inputData.normalizedScreenSpaceUV = NormalizedScreenSpaceUV;
    inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUVOrVertexSH.xy);

	// ----------------------------------------------------------------
	// SurfaceData
    SurfaceData surfaceData;
    surfaceData.albedo = BaseColor;
    surfaceData.metallic = saturate(Metallic);
    surfaceData.specular = Specular;
    surfaceData.smoothness = saturate(Smoothness);
    surfaceData.occlusion = Occlusion;
    surfaceData.emission = Emission;
    surfaceData.alpha = saturate(Alpha);
    surfaceData.normalTS = Normal;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

	ExtendData extendData = (ExtendData)0;
	extendData.specularScaleBRDF = 1;

    //half4 color = UniversalFragmentPBR(inputData, surfaceData);
	half4 color = FragmentPBR(inputData, surfaceData, extendData);

    UBPA_APPLY_FOG(IN, color);

    return color;
}

#endif // __SHEET_FOG_FORWARD_PASS__
