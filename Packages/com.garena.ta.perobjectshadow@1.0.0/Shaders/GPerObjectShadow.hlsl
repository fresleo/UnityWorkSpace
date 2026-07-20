#ifndef G_PER_OBJECT_SHADOW
#define G_PER_OBJECT_SHADOW

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"

// 正常投影效果
TEXTURE2D_SHADOW(_GPerObjectShadowMap);
float4 _GPerObjectShadowMapSize; // (xy: 1/width and 1/height, zw: width and height)

uint _GPerObjectShadowCount;
float4x4 _GPerObjectWorldToShadow[16];
float4 _GPerObjectShadowUVRect[16];
float _GPerObjectShadowIntensity[16];

float _GPerObjectShadowEnable;

// 角色自投影效果
float _GPerObjectCharacterShadowEnable;
TEXTURE2D_SHADOW(_GPerObjectCharacterShadowMap);
float4 _GPerObjectCharacterShadowMapSize; // (xy: 1/width and 1/height, zw: width and height)
uint _GPerObjectCharacterShadowCount;
float4x4 _GPerObjectCharacterWorldToShadow[16];
float4 _GPerObjectCharacterShadowUVRect[16];


//非OpenGL下（Vulkan）会渲染到包围盒外的东西，临时处理一下
float4 ShadowCoordZ(float4 shadowCoord)
{
    shadowCoord.z = max(0.0001, shadowCoord.z);
    return shadowCoord;
}


real SamplePerObjectShadowmap(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData, half4 shadowParams, bool isPerspectiveProjection = true)
{
    // Compiler will optimize this branch away as long as isPerspectiveProjection is known at compile time
    if (isPerspectiveProjection)
        shadowCoord.xyz /= shadowCoord.w;

    real attenuation;
    real shadowStrength = shadowParams.x;

    // #define _SHADOWS_SOFT_HIGH 1

    // 因为有逐对象阴影的软阴影质量固定，不受全局控制的需求，所以这里直接把阴影质量写死了
    /*
    if (shadowParams.y > SOFT_SHADOW_QUALITY_OFF)
    {
        attenuation = SampleShadowmapFiltered_(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    }
    else
    {
        // 1-tap 硬件比较
        attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
    }
    */
    attenuation = SampleShadowmapFilteredMediumQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);

    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    // TODO: We could use branch here to save some perf on some platforms.
    return attenuation;
}

half PerObjectRealtimeShadowUnroll(float3 positionWS)
{
    if(_GPerObjectShadowEnable < 0.5)
        return 1;

    half r = 1;
    UNITY_UNROLL
    for(uint i = 0; i < 16; i++)
    {
        if(i >= _GPerObjectShadowCount)
            continue;

        float4 shadowCoord = mul(_GPerObjectWorldToShadow[i], float4(positionWS, 1.0));
        shadowCoord.w = 0;
        shadowCoord = ShadowCoordZ(shadowCoord);
        // shadowCoord.z = min(1, shadowCoord.z);

        float4 rect = _GPerObjectShadowUVRect[i];
        half s = 1;

        if (shadowCoord.x < rect.x || shadowCoord.x > rect.z || shadowCoord.y < rect.y || shadowCoord.y > rect.w) 
        {
            continue;
        }

        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        shadowSamplingData.shadowmapSize = _GPerObjectShadowMapSize;
        shadowSamplingData.shadowOffset0 = half4(_MainLightShadowOffset0);
        shadowSamplingData.shadowOffset1 = half4(_MainLightShadowOffset1);

        half4 shadowParams = GetMainLightShadowParams();
        shadowParams.x *= saturate(_GPerObjectShadowIntensity[i]);
        // return real(SAMPLE_TEXTURE2D_SHADOW(_GPerObjectShadowmapTexture, sampler_LinearClampCompare, shadowCoord.xyz));
        
        s = SamplePerObjectShadowmap(TEXTURE2D_ARGS(_GPerObjectShadowMap, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        // s = SampleShadowmap(TEXTURE2D_ARGS(_GPerObjectShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        r = min(r, s);
    }
    return r;
}

half PerObjectRealtimeShadow(float3 positionWS)
{
    if(_GPerObjectShadowEnable < 0.5)
        return 1;

    half r = 1;
    for(uint i = 0; i < _GPerObjectShadowCount; i++)
    {
        float4 shadowCoord = mul(_GPerObjectWorldToShadow[i], float4(positionWS, 1.0));
        shadowCoord.w = 0;
        shadowCoord = ShadowCoordZ(shadowCoord);
        // shadowCoord.z = min(1, shadowCoord.z);

        float4 rect = _GPerObjectShadowUVRect[i];
        half s = 1;

        if (shadowCoord.x < rect.x || shadowCoord.x > rect.z || shadowCoord.y < rect.y || shadowCoord.y > rect.w) 
        {
            continue;
        }

        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        shadowSamplingData.shadowmapSize = _GPerObjectShadowMapSize;
        shadowSamplingData.shadowOffset0 = half4(_MainLightShadowOffset0);
        shadowSamplingData.shadowOffset1 = half4(_MainLightShadowOffset1);

        half4 shadowParams = GetMainLightShadowParams();
        shadowParams.x *= saturate(_GPerObjectShadowIntensity[i]);
        // return real(SAMPLE_TEXTURE2D_SHADOW(_GPerObjectShadowmapTexture, sampler_LinearClampCompare, shadowCoord.xyz));
        
        s = SamplePerObjectShadowmap(TEXTURE2D_ARGS(_GPerObjectShadowMap, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        // s = SampleShadowmap(TEXTURE2D_ARGS(_GPerObjectShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        r = min(r, s);
    }
    return r;
}

half PerObjectCharacterRealtimeShadow(float3 positionWS)
{
    if(_GPerObjectCharacterShadowEnable < 0.5)
        return 1;

    half r = 1;
    for(uint i = 0; i < _GPerObjectCharacterShadowCount; i++)
    {
        float4 shadowCoord = mul(_GPerObjectCharacterWorldToShadow[i], float4(positionWS, 1.0));
        shadowCoord.w = 0;
        shadowCoord = ShadowCoordZ(shadowCoord);
        // shadowCoord.z = min(1, shadowCoord.z);

        float4 rect = _GPerObjectCharacterShadowUVRect[i];
        half s = 1;

        if (shadowCoord.x < rect.x || shadowCoord.x > rect.z || shadowCoord.y < rect.y || shadowCoord.y > rect.w) 
        {
            continue;
        }

        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        shadowSamplingData.shadowmapSize = _GPerObjectCharacterShadowMapSize;
        shadowSamplingData.shadowOffset0 = half4(_MainLightShadowOffset0);
        shadowSamplingData.shadowOffset1 = half4(_MainLightShadowOffset1);

        half4 shadowParams = GetMainLightShadowParams();
        // return real(SAMPLE_TEXTURE2D_SHADOW(_GPerObjectShadowmapTexture, sampler_LinearClampCompare, shadowCoord.xyz));
        
        s = SamplePerObjectShadowmap(TEXTURE2D_ARGS(_GPerObjectCharacterShadowMap, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        // s = SampleShadowmap(TEXTURE2D_ARGS(_GPerObjectShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);

        r = min(r, s);
    }
    return r;
}

#endif
