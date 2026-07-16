#ifndef __BFX_VATBLOOD_INPUT__
#define __BFX_VATBLOOD_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _PosMap_ST, _BumpMap_ST;

    half4 _Color;
	float _Metallic, _Smoothness, _Occlusion;

    float _BoundingMax, _BoundingMin;
    float4 _HeightOffset;

    float _TimeInFrames;
CBUFFER_END

TEXTURE2D(_PosMap); SAMPLER(sampler_PosMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

inline void InitializeStandardLitSurfaceData(float3 positionVS, float3 normalWS, out SurfaceData outSurfaceData)
{
	half3 albedo = 0;
	albedo = _Color.rgb * 0.6;
	
	half fresnel = saturate(1 - dot(normalWS, positionVS));
	fresnel = fresnel * fresnel;
	
	albedo = lerp(albedo * 0.15, albedo, fresnel);
	albedo = min(albedo, _Color.rgb * 0.55);

	outSurfaceData.albedo = albedo;
	outSurfaceData.alpha = 1;

	outSurfaceData.metallic = _Metallic;
	outSurfaceData.specular = half3(0.0, 0.0, 0.0);
	outSurfaceData.smoothness = _Smoothness;
	outSurfaceData.occlusion = _Occlusion;
	
	outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
	outSurfaceData.emission = 0;

	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // __BFX_VATBLOOD_INPUT__