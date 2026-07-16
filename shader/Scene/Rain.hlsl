// todo: 老版本的雨，已经不在使用了，只是留下作参考用的

#ifndef XKNIGHT_RAIN_INCLUDED
#define XKNIGHT_RAIN_INCLUDED

// reference https://seblagarde.wordpress.com/2013/01/03/water-drop-2b-dynamic-rain-and-its-effects/

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

uniform TEXTURECUBE(_RainReflectionCube);	SAMPLER(sampler_RainReflectionCube);
uniform TEXTURE2D(_RainNormalMap);			SAMPLER(sampler_RainNormalMap);
uniform TEXTURE2D(_RippleTexture);			SAMPLER(sampler_RippleTexture);
uniform TEXTURE2D(_TerrainRainAreaMap);		SAMPLER(sampler_TerrainRainAreaMap);

uniform float4 _TerrainRainAreaUV;
uniform float4 _RippleTimes;
// xy:wave zw:ripple
uniform float4 _TerrainRainTilings;
// xy:wave zw:ripple
uniform float4 _LitObjectRainTilings;

uniform float _RainIndirectSpecularIntensity;
uniform float _RainBlurReflection;
uniform float _RainNormalIntensity;
uniform float _RainAlbedoBrightnessIntensity;
uniform float _RainWaveSpeed;

//  for tools
uniform float  _TimeForWave;
uniform float4 _TimeForOfflineRender;

uniform TEXTURE2D(_RainTextureAtlas);   SAMPLER(sampler_RainTextureAtlas);

float2 GetTerrainRainMaskUV(float3 wPos)
{
	return _TerrainRainAreaUV.z * (wPos.xz - _TerrainRainAreaUV.xy);
}

float4 RainUV(float2 uv)
{
	float4 coords;
	coords.xy = uv * _TerrainRainTilings.xy;
	coords.zw = uv;

	return coords;
}

float2 Flipbook(float2 UV, float Width, float Height, float time)
{
	float2 tile = float2(Width, Height);
	float total = Width * Height;
	float2 append = float2(total, Width);
	float cl = clamp(0, 0.0001, total - 1.0);
	float tempOut = frac((time + cl) / total);
	float2 uvTemp = float2(tempOut, 1.0 - tempOut);
	float2 result = UV / tile + floor(append * uvTemp) / tile;

	return result;
}

half3 UnpackNormal(half2 packNormal, half scale = 1.0)
{
	half3 normal;
	normal.xy = packNormal * 2.0 - 1.0;
	normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));

	normal.xy *= scale;
	return normalize(normal);
}

float3 ComputeRipple(float2 UV, float CurrentTime, float Weight)
{
	float4 Ripple = SAMPLE_TEXTURE2D(_RippleTexture, sampler_RippleTexture, UV);
	Ripple.yz = Ripple.yz * 2.0 - 1.0;
            
	float DropFrac = frac(Ripple.w + CurrentTime);
	float TimeFrac = DropFrac - 1.0 + Ripple.x;
	float DropFactor = saturate(0.2 + Weight * 0.8 - DropFrac);
	float FinalFactor = DropFactor * Ripple.x * sin( clamp(TimeFrac * 9.0, 0.0f, 3.0) * PI);
   
	return float3(Ripple.yz * FinalFactor * 0.35, 1.0);
}

// TODO 可优化为2或者3层涟漪，节省采样数
// TODO 亦可尝试烘焙到texture array中
half3 ComputeRippleNormal(float2 rippleUV, float4 weight)
{
	float3 ripple1 = ComputeRipple(rippleUV + float2( 0.25f,0.0f), _RippleTimes.x, weight.x);
	float3 ripple2 = ComputeRipple(rippleUV + float2(-0.55f,0.3f), _RippleTimes.y, weight.y);
	float3 ripple3 = ComputeRipple(rippleUV + float2(0.6f, 0.85f), _RippleTimes.z, weight.z);
	float3 ripple4 = ComputeRipple(rippleUV + float2(0.5f,-0.75f), _RippleTimes.w, weight.w);
	
	// Merge the 4 layers
	float4 z = lerp(1, float4(ripple1.z, ripple2.z, ripple3.z, ripple4.z), weight);
	float3 rippleNormal = float3( weight.x * ripple1.xy +
							weight.y * ripple2.xy + 
							weight.z * ripple3.xy + 
							weight.w * ripple4.xy, 
							z.x * z.y * z.z * z.w);
	
	// 法线强度和wave一致
	rippleNormal = normalize(float3(rippleNormal.xy * _RainNormalIntensity * 2.0f, rippleNormal.z));
	
	return normalize(rippleNormal);

	return half3(0,0,1);
}

float3 ComputeWaveNormal(float2 rainUV)
{
	float2 offsetUV = float2(0, frac(_Time.x * _RainWaveSpeed));
	float3 waveNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV - offsetUV), _RainNormalIntensity).xyz;
	float3 waveNormal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV + offsetUV + float2(0.5f, 0.0f)), _RainNormalIntensity).xyz;
	float3 waveNormal3 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV.yx - offsetUV), _RainNormalIntensity).xyz;
	float3 waveNormal4 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV.yx + offsetUV + float2(0.5f, 0.0f)), _RainNormalIntensity).xyz;
	
	float3 waveNormal = (waveNormal1 + waveNormal2 + waveNormal3 + waveNormal4) * 0.25f;

	return normalize(waveNormal);
}

// for terrain
// float3 ComputeRainWorldNormal(float4 rainUV, float3 worldPos, float3 sourceNormal, float3x3 tangentToWorld)
// {
// 	float noise = SAMPLE_TEXTURE2D(_TerrainRainAreaMap, sampler_TerrainRainAreaMap, GetTerrainRainMaskUV(worldPos)).x;
//
// 	float3 waveNormal = ComputeWaveNormal(rainUV.xy);
//
// 	float4 weights = 1.0f - float4(0.0f, 0.25f, 0.5f, 0.75f);
// 	weights = saturate(weights * 4);
// 	float2 rippleUV = rainUV.zw * _TerrainRainTilings.zw;
// 	float3 rippleNormal = ComputeRippleNormal(rippleUV, weights);
// 	
// 	float3 rainLocalNormal = BlendNormal(rippleNormal, waveNormal);
//
// 	float3 worldNormal = normalize(lerp(sourceNormal, rainLocalNormal, noise));
//
// 	worldNormal = TransformTangentToWorld(worldNormal, tangentToWorld);
//
// 	return worldNormal;
// }

// for terrain
float3 ComputeRainWorldNormal(float4 rainUV, float3 worldPos, float3 sourceNormal, float3x3 tangentToWorld)
{
	float noise = SAMPLE_TEXTURE2D(_TerrainRainAreaMap, sampler_TerrainRainAreaMap, GetTerrainRainMaskUV(worldPos)).x;

	// float3 waveNormal = ComputeWaveNormal(rainUV.xy);
	//
	// float4 weights = 1.0f - float4(0.0f, 0.25f, 0.5f, 0.75f);
	// weights = saturate(weights * 4);
	// float2 rippleUV = rainUV.zw * _TerrainRainTilings.zw;
	// float3 rippleNormal = ComputeRippleNormal(rippleUV, weights);
	//
	// float3 rainLocalNormal = BlendNormal(rippleNormal, waveNormal);
	//
	// float3 worldNormal = normalize(lerp(sourceNormal, rainLocalNormal, noise));
	//
	// worldNormal = TransformTangentToWorld(worldNormal, tangentToWorld);
	//
	// return worldNormal;

	float2 uv = Flipbook(rainUV.zw * _TerrainRainTilings.xy, 8, 8, _Time.y * 10.0);
	float4 rainInfo = SAMPLE_TEXTURE2D(_RainTextureAtlas, sampler_RainTextureAtlas, uv);

	half3 waveNormal = UnpackNormal(rainInfo.xy);
	half3 rippleNormal = UnpackNormal(rainInfo.zw, 2.0);
	half3 rainNormal = BlendNormal(waveNormal, rippleNormal);
	rainNormal = normalize(lerp(sourceNormal, rainNormal, noise));
	float3 worldNormal = TransformTangentToWorld(rainNormal, tangentToWorld);

	return worldNormal;
}

// for lit
// half3 ComputeRainWorldNormal(float3 worldPos, float3 sourceNormal, float3x3 tangentToWorld)
// {
// 	float2 rainUV = worldPos.xz * _LitObjectRainTilings.xy;
// 	
//
// 	float3 waveNormal = ComputeWaveNormal(rainUV);
// 	
// 	float RainIntensity = 1.0f;
// 	float4 weights = RainIntensity - float4(0, 0.25, 0.5, 0.75);
// 	weights = saturate(weights * 4);   
// 	float2 rippleUV = worldPos.xz * _LitObjectRainTilings.zw;
// 	half3 rippleNormal = ComputeRippleNormal(rippleUV, weights);
//
// 	return waveNormal;
// 	
// 	float3 rainNoraml = BlendNormal(waveNormal, rippleNormal);
//
// 	rainNoraml = BlendNormal(rainNoraml, sourceNormal);
// 	
// 	rainNoraml = normalize(lerp(sourceNormal, rainNoraml, pow(tangentToWorld[2][1], 3)));
// 	
// 	float3 worldNormal = TransformTangentToWorld(rainNoraml, tangentToWorld);
//
// 	return worldNormal;
// }

// for lit
half3 ComputeRainWorldNormal(float3 worldPos, float3 sourceNormal, float3x3 tangentToWorld)
{
	float2 rainUV = worldPos.xz * _LitObjectRainTilings.xy;
	rainUV = frac(rainUV);

	float2 uv = Flipbook(rainUV, 8, 8, _Time.y * 10.0);
	float4 rainInfo = SAMPLE_TEXTURE2D(_RainTextureAtlas, sampler_RainTextureAtlas, uv);

	half3 waveNormal = UnpackNormal(rainInfo.xy);
	half3 rippleNormal = UnpackNormal(rainInfo.zw, 2.0);
	half3 rainNormal = BlendNormal(waveNormal, rippleNormal);
	rainNormal = BlendNormal(rainNormal, sourceNormal);
	rainNormal = normalize(lerp(sourceNormal, rainNormal, pow(tangentToWorld[2][1], 3)));
	float3 worldNormal = TransformTangentToWorld(rainNormal, tangentToWorld);

	return worldNormal;
}

// for lit
float3 ComputeRainIndirectSpecular(half3 normalWS, half3 viewDirWS, half worldNormalY)
{
	half3 reflectVector = reflect(-viewDirWS, normalWS);

	half3 reflection = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, _RainBlurReflection);

	return reflection * _RainIndirectSpecularIntensity * pow(worldNormalY, 3);
}

half SampleRainAreaMap(float3 wPos)
{
	float noise = SAMPLE_TEXTURE2D(_TerrainRainAreaMap, sampler_TerrainRainAreaMap, GetTerrainRainMaskUV(wPos)).x;
	return noise;
}

// for terrain
half3 ComputeRainIndirectSpecular(half3 wPos, half3 normalWS, half3 viewDirWS)
{
	half3 reflectVector = reflect(-viewDirWS, normalWS);

	half3 reflection = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, _RainBlurReflection);
	
	return reflection * _RainIndirectSpecularIntensity;
}

// ----------------------------   工具函数，用于生成wave和ripple的方法  -------------------------------

float3 ComputeRippleForTool(float2 UV, float CurrentTime, float Weight)
{
	float4 Ripple = SAMPLE_TEXTURE2D(_RippleTexture, sampler_RippleTexture, UV);
	Ripple.yz = Ripple.yz * 2.0 - 1.0;
            
	float DropFrac = frac(Ripple.w + CurrentTime);
	float TimeFrac = DropFrac - 1.0 + Ripple.x;
	float DropFactor = saturate(0.2 + Weight * 0.8 - DropFrac);
	float FinalFactor = DropFactor * Ripple.x * sin( clamp(TimeFrac * 9.0, 0.0f, 3.0) * PI);
   
	return float3(Ripple.yz * FinalFactor * 0.35, 1.0);
}

half3 ComputeRippleNormalForTool(float2 rippleUV, float4 weight)
{
	float4 tfor = frac(_TimeForOfflineRender);
	float3 ripple1 = ComputeRippleForTool(rippleUV + float2( 0.25f,0.0f), tfor.x, weight.x);
	float3 ripple2 = ComputeRippleForTool(rippleUV + float2(-0.55f,0.3f), tfor.y, weight.y);
	float3 ripple3 = ComputeRippleForTool(rippleUV + float2(0.6f, 0.85f), tfor.z, weight.z);
	float3 ripple4 = ComputeRippleForTool(rippleUV + float2(0.5f,-0.75f), tfor.w, weight.w);
	
	// Merge the 4 layers
	float4 z = lerp(1, float4(ripple1.z, ripple2.z, ripple3.z, ripple4.z), weight);
	float3 rippleNormal = float3( weight.x * ripple1.xy +
							weight.y * ripple2.xy + 
							weight.z * ripple3.xy + 
							weight.w * ripple4.xy, 
							z.x * z.y * z.z * z.w);

	return normalize(rippleNormal);
}

float3 CalcuateOfflineRipple(float2 uv)
{
	float4 weights = 1.0f - float4(0.0f, 0.25f, 0.5f, 0.75f);
	weights = saturate(weights * 4);
	weights.w = 0;
	// float4 weights = float4(1,1,1,1);
	float3 rippleNormal = ComputeRippleNormalForTool(uv, weights);

	return rippleNormal;
}

float3 CalcuateOfflineWave(float2 uv)
{
	float2 offsetUV = float2(0.0f, frac(_TimeForWave.x));
	float3 waveNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, uv - offsetUV), 1).xyz;
	float3 waveNormal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, uv + offsetUV + float2(0.5f, 0.0f)), 1).xyz;
	float3 waveNormal3 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, uv.yx - offsetUV), 1).xyz;
	float3 waveNormal4 = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, uv.yx + offsetUV + float2(0.5f, 0.0f)), 1).xyz;
	
	float3 waveNormal = (waveNormal1 + waveNormal2 + waveNormal3 + waveNormal4) * 0.25f;

	return normalize(waveNormal);
}

// ----------------------------   工具函数，用于生成wave和ripple的方法  -------------------------------

#endif // XKNIGHT_RAIN_INCLUDED
