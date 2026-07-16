#ifndef XKNIGHT_SSR
#define XKNIGHT_SSR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

#define MAX_MARCHING 12

// float _ReflectionMaxDistance;
// float _ReflectionThinkness;

#define _ReflectionMaxDistance 130
#define _ReflectionThickness 3

uniform float _FrameIndexMod8;

float2 ViewPosToCS(float3 vpos)
{
    float4 proj_pos = mul(unity_CameraProjection, float4(vpos, 1));
    float3 screenPos = proj_pos.xyz / proj_pos.w;
    return float2(screenPos.x, screenPos.y) * 0.5 + 0.5;
}

float CompareWithDepth(float3 vpos)
{
    float2 uv = ViewPosToCS(vpos);
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    depth = LinearEyeDepth(depth, _ZBufferParams);
    int isInside = uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1;
    return lerp(0, vpos.z + depth, isInside);
}

// View Space Ray Marching
bool RayMarching(float3 o, float3 r, out float2 hitUV)
{
    float3 end = o;
    float stepSize = _ReflectionMaxDistance / MAX_MARCHING;

    UNITY_LOOP
    for (int i = 1; i <= MAX_MARCHING; ++i)
    {
        end += r * stepSize;

        float collied = CompareWithDepth(end);
        if (collied < 0)
        {
            if (abs(collied) < _ReflectionThickness)
            {
                hitUV = ViewPosToCS(end);
                return true;
            }

            //回到当前起点
            end -= r * stepSize;
            //步进减半
            stepSize *= 0.5;
        }
    }

	// 远平面，用于采集天空盒的颜色
	float3 viewFarPos = o + r * 1.0f * rcp(r.z) * (_ProjectionParams.z + o.z);
	hitUV = ViewPosToCS(viewFarPos);
	
	if(hitUV.x < 0 || hitUV.x > 1 || hitUV.y < 0 || hitUV.y > 1)
		return false;
    
	return true;
}

//  ------   UE4 Screen Space Ray Marching  -----

// Number of sample batched at same time.
#define SSRT_SAMPLE_BATCH_SIZE 4

float min3( float a, float b, float c )
{
	return min( a, min( b, c ) );
}

/** Structure that represent a ray to be shot in screen space. */
struct FSSRTRay
{
    float3 RayStartScreen;
    float3 RayStepScreen;

    float CompareTolerance;
};

float ComputeHitVignetteFromScreenPos(float2 ScreenPos)
{
    float2 Vignette = saturate(abs(ScreenPos) * 5 - 4);
	
    //PrevScreen sometimes has NaNs or Infs.  DX11 is protected because saturate turns NaNs -> 0.
    //Do a SafeSaturate so other platforms get the same protection.
    return SafeNormalize(1.0 - dot(Vignette, Vignette));
}

/** Return float multiplier to scale RayStepScreen such that it clip it right at the edge of the screen. */
float GetStepScreenFactorToClipAtScreenEdge(float2 RayStartScreen, float2 RayStepScreen)
{
    // Computes the scale down factor for RayStepScreen required to fit on the X and Y axis in order to clip it in the viewport
    const float RayStepScreenInvFactor = 0.5 * length(RayStepScreen);
    const float2 S = 1 - max(abs(RayStepScreen + RayStartScreen * RayStepScreenInvFactor) - RayStepScreenInvFactor, 0.0f) / abs(RayStepScreen);

    // Rescales RayStepScreen accordingly
    const float RayStepFactor = min(S.x, S.y) / RayStepScreenInvFactor;

    return RayStepFactor;
}

/** Compile a ray for screen space ray casting. */
FSSRTRay InitScreenSpaceRayFromWorldSpace(float3 RayOrigindWorld, float3 WorldRayDirection, float SceneDepth)
{
    float4 RayStartClip	= mul(GetWorldToHClipMatrix(), float4(RayOrigindWorld, 1));
	// 这里的减是因为摄像机空间是右手坐标系
    float4 RayEndClip = mul(GetWorldToHClipMatrix(), float4(RayOrigindWorld - WorldRayDirection * SceneDepth, 1));

    float3 RayStartScreen = RayStartClip.xyz * rcp(RayStartClip.w);
    float3 RayEndScreen = RayEndClip.xyz * rcp(RayEndClip.w);

	// TODO 
    float4 RayDepthClip = RayStartClip + mul(GetViewToHClipMatrix(), float4(0, 0, SceneDepth, 0));
    float3 RayDepthScreen = RayDepthClip.xyz * rcp(RayDepthClip.w);

    FSSRTRay Ray;
    Ray.RayStartScreen = RayStartScreen;
    Ray.RayStepScreen = RayEndScreen - RayStartScreen;
	
    Ray.RayStepScreen *= GetStepScreenFactorToClipAtScreenEdge(RayStartScreen.xy, Ray.RayStepScreen.xy);

    // TODO 这么用科学吗？
    // Ray.CompareTolerance = max(abs(Ray.RayStepScreen.z), (RayStartScreen.z - RayDepthScreen.z) * 4);
	Ray.CompareTolerance = max(abs(Ray.RayStepScreen.y), (RayStartScreen.y - RayDepthScreen.y) * 4);

    return Ray;
} // InitScreenSpaceRayFromWorldSpace()

/** Cast a screen space ray. */
bool CastScreenSpaceRay(FSSRTRay Ray, uint NumSteps, float StepOffset, out float3 OutHitUVz)
{
	// 这个是ndc空间，范围为[-1, 1]
    const float3 RayStartScreen = Ray.RayStartScreen;
    float3 RayStepScreen = Ray.RayStepScreen;

    float3 RayStartUVz = float3((RayStartScreen.xy * float2( 0.5, -0.5 ) + 0.5), RayStartScreen.z );
    float3 RayStepUVz  = float3(  RayStepScreen.xy  * float2( 0.5, -0.5 )      , RayStepScreen.z  );

    const float Step = 1.0 / NumSteps;
    float CompareTolerance = Ray.CompareTolerance * Step;

	float LastDiff = 0;

    RayStepUVz *= Step;
	float3 RayUVz = RayStartUVz + RayStepUVz * StepOffset;

	float4 MultipleSampleDepthDiff;
	bool4 bMultipleSampleHit; 
    bool bFoundAnyHit = false;

	uint i;
	
	UNITY_LOOP
	for (i = 0; i < NumSteps; i += SSRT_SAMPLE_BATCH_SIZE)
	{
		float2 SamplesUV[SSRT_SAMPLE_BATCH_SIZE];
		float4 SamplesZ;

		// Compute the sample coordinates.
		{
			UNITY_UNROLLX(SSRT_SAMPLE_BATCH_SIZE)
			for (uint j = 0; j < SSRT_SAMPLE_BATCH_SIZE; j++)
			{
				SamplesUV[j] = RayUVz.xy + (float(i) + float(j + 1)) * RayStepUVz.xy;
				SamplesZ[j] = RayUVz.z + (float(i) + float(j + 1)) * RayStepUVz.z;
			}
		}

		// Sample the scene depth.
		float4 SampleDepth;
		{
			UNITY_UNROLLX(SSRT_SAMPLE_BATCH_SIZE)
			for (uint j = 0; j < SSRT_SAMPLE_BATCH_SIZE; j++)
			{
				SampleDepth[j] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, SamplesUV[j]).r;
			}
		}

		// Evaluates the intersections.
		MultipleSampleDepthDiff = SamplesZ - SampleDepth;
		bMultipleSampleHit = abs(MultipleSampleDepthDiff + CompareTolerance) < CompareTolerance;
		bFoundAnyHit = any(bMultipleSampleHit);

		UNITY_BRANCH
		if (bFoundAnyHit)
		{
			break;
		}

		LastDiff = MultipleSampleDepthDiff.w;
	} // for( uint i = 0; i < NumSteps; i += 4 )
	
	// Compute the output coordinates.
	UNITY_BRANCH
	if (bFoundAnyHit)
    {
#if 0
		// If hit set to intersect time. If missed set to beyond end of ray
		float4 HitTime = bMultipleSampleHit ? float4(1, 2, 3, 4) : 5;

		// Take closest hit
		float Time1 = float(i) + min(min3(HitTime.x, HitTime.y, HitTime.z), HitTime.w);
		float Time0 = Time1 - 1;

		const uint NumBinarySteps = 4;

		// Binary search
		for (uint j = 0; j < NumBinarySteps; j++)
		{
			CompareTolerance *= 0.5;

			float MidTime = 0.5 * (Time0 + Time1);
			float3 MidUVz = RayUVz + RayStepUVz * MidTime;
			float MidDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, MidUVz.xy).r;
			float MidDepthDiff = MidUVz.z - MidDepth;

			if (abs(MidDepthDiff + CompareTolerance) < CompareTolerance)
			{
				Time1 = MidTime;
			}
			else
			{
				Time0 = MidTime;
			}
		}
			
		OutHitUVz = RayUVz + RayStepUVz * Time1;
#endif

#if 1		
        {
            float DepthDiff0 = MultipleSampleDepthDiff[2];
            float DepthDiff1 = MultipleSampleDepthDiff[3];
            float Time0 = 3;

            UNITY_FLATTEN
            if (bMultipleSampleHit[2])
            {
                DepthDiff0 = MultipleSampleDepthDiff[1];
                DepthDiff1 = MultipleSampleDepthDiff[2];
                Time0 = 2;
            }
        	
        	UNITY_FLATTEN
            if (bMultipleSampleHit[1])
            {
                DepthDiff0 = MultipleSampleDepthDiff[0];
                DepthDiff1 = MultipleSampleDepthDiff[1];
                Time0 = 1;
            }
        	
        	UNITY_FLATTEN
            if (bMultipleSampleHit[0])
            {
                DepthDiff0 = LastDiff;
                DepthDiff1 = MultipleSampleDepthDiff[0];
                Time0 = 0;
            }

			Time0 += float(i);
			float Time1 = Time0 + 1;

        	// TODO
			#if 1
			{
				// Binary search
				for( uint j = 0; j < 4; j++ )
				{
					CompareTolerance *= 0.5;

					float  MidTime = 0.5 * ( Time0 + Time1 );
					float3 MidUVz = RayUVz + RayStepUVz * MidTime;
					float  MidDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, MidUVz.xy).r;
					float  MidDepthDiff = MidUVz.z - MidDepth;

					if( abs( MidDepthDiff + CompareTolerance ) < CompareTolerance )
					{
						DepthDiff1	= MidDepthDiff;
						Time1		= MidTime;
					}
					else
					{
						DepthDiff0	= MidDepthDiff;
						Time0		= MidTime;
					}
				}
			}
			#endif

			// Find more accurate hit using line segment intersection
            float TimeLerp = saturate(DepthDiff0 / (DepthDiff0 - DepthDiff1));
            float IntersectTime = Time0 + TimeLerp;
				
            OutHitUVz = RayUVz + RayStepUVz * IntersectTime;
        }
		
		// OutHitUVz.xy = OutHitUVz.xy * float2( 2, -2 ) + float2( -1, 1 );
		// OutHitUVz.xy = OutHitUVz.xy * float2( 0.5, -0.5 ) + float2( 0.5, 0.5 );

#endif
    }
	else
    {
		OutHitUVz = float3(0, 0, 0);
    }

	return bFoundAnyHit;
}

// high frequency dither pattern appearing almost random without banding steps
//note: from "NEXT GENERATION POST PROCESSING IN CALL OF DUTY: ADVANCED WARFARE"
//      http://advances.realtimerendering.com/s2014/index.html
// Epic extended by FrameId
// ~7 ALU operations (2 frac, 3 mad, 2 *)
// @return 0..1
float InterleavedGradientNoise( float2 uv, float FrameId )
{
	// magic values are found by experimentation
	uv += FrameId * (float2(47, 17) * 0.695f);

	const float3 magic = float3( 0.06711056f, 0.00583715f, 52.9829189f );
	return frac(magic.z * frac(dot(uv, magic.xy)));
}

bool RayCast(float2 SvPosition, float3 RayOrigindWorld, float SceneDepth, float3 RayDirection, uint NumSteps, out float3 OutHitUVz)
{
    float StepOffset = InterleavedGradientNoise(SvPosition, _FrameIndexMod8);
	StepOffset -= 0.5;
    
    FSSRTRay Ray = InitScreenSpaceRayFromWorldSpace(RayOrigindWorld, RayDirection, SceneDepth);

    return CastScreenSpaceRay(Ray, NumSteps, StepOffset, OutHitUVz);
}

float4 SampleScreenColor(float2 uv)
{
	float4 OutColor;
	OutColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
	
	return OutColor;
}

#endif
