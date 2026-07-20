#ifndef XKNIGHT_FROSTED_GLASS_INCLUDED
#define XKNIGHT_FROSTED_GLASS_INCLUDED

TEXTURE2D_X(_BluredTexture0);     SAMPLER(sampler_BluredTexture0);
TEXTURE2D_X(_BluredTexture1);     SAMPLER(sampler_BluredTexture1);


float2 ProjectWorldToScreenUV(float3 positionWS)
{
	float4 clip = mul(UNITY_MATRIX_VP, float4(positionWS, 1));
	float2 ndc = clip.xy / max(1e-4, clip.w);
	return ndc * 0.5 + 0.5;
}

float2 ComputeRefractionUV_Thin(float2 screenUV, float3 normalWS, float3 viewDirWS, float iOR, float refractStrengthPX)
{
	float3 N = normalize(normalWS);
	float3 V = normalize(viewDirWS);
	N *= sign(dot(N, V));
	float eta = 1.0 / max(iOR, 1e-3);
	float3 refractDirWS = refract(-V, N, eta);
	float3 refractDirVS = mul(UNITY_MATRIX_V, float4(refractDirWS, 0)).xyz;

	float2 step = (abs(refractDirVS.z) > 1e-4) ? (refractDirVS.xy / refractDirVS.z) : refractDirVS.xy;
	float2 pixelSize = rcp(_ScreenParams.xy);
	float2 offset = step * (refractStrengthPX * pixelSize);
	return saturate(screenUV + offset);
}

float2 ComputeRefractionUV_Planar(float2 screenUV, float3 positionWS, float3 normalWS, float3 viewDirWS, float iOR, float thicknessWorld)
{
	float3 N = normalize(normalWS);
	float3 V = normalize(viewDirWS);
	float eta = 1.0 / max(iOR, 1e-3);
	float3 refractDirWS = refract(-V, N, eta);

	float NoV = max(1e-3, abs(dot(N, V)));
	float L = thicknessWorld / NoV;
	float3 exitWS = positionWS + refractDirWS * L;
	return saturate(ProjectWorldToScreenUV(exitWS));
}

float2 ComputeRefractionUV_Sphere(float2 screenUV, float3 positionWS, float3 normalWS, float3 viewDirWS, float iOR, float sphereRadius)
{
	float3 centerWS = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
	float3 N = normalize(normalWS);
	float3 V = normalize(viewDirWS);
	float eta = 1.0 / max(iOR, 1e-3);
	float3 dir = refract(-V, N, eta);

	float3 oc = positionWS - centerWS;
	float b = dot(oc, dir);
	float c = dot(oc, oc) - sphereRadius * sphereRadius;
	float disc = b*b - c;
	if (disc <= 0)
	{
		return screenUV;
	}
	float tExit = -b + sqrt(disc); // forward exit
	float3 exitWS = positionWS + dir * tExit;
	return saturate(ProjectWorldToScreenUV(exitWS));
}

half4 GetFrostedColorFromScreenUV(float2 uv, float2 screenUV, Texture2D frostedTexture, SamplerState samplerFrostedTex, float frostIntensity)
{
	float frost = saturate(SAMPLE_TEXTURE2D(frostedTexture, samplerFrostedTex, uv).x * frostIntensity);

	float2 ssUV = UnityStereoTransformScreenSpaceTex(screenUV);
	half4 ref0 = SAMPLE_TEXTURE2D_X(_BluredTexture0, sampler_BluredTexture0, ssUV);
	half4 ref1 = SAMPLE_TEXTURE2D_X(_BluredTexture1, sampler_BluredTexture1, ssUV);

	return lerp(ref0, ref1, frost);
}

half4 GetColorFromScreenUV(float2 screenUV)
{
	float2 ssUV = UnityStereoTransformScreenSpaceTex(screenUV);
    return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, ssUV);
}

#endif