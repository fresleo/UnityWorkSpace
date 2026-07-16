#ifndef WATER_REFLECTION
#define WATER_REFLECTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Water_Variables.hlsl"
#include "Water_SSR.hlsl"

// 平面反射
TEXTURE2D(_PlanarReflectionTexture);            SAMPLER(sampler_PlanarReflectionTexture);

void ComputeReflections(inout GlobalData data, Varyings IN)
{
    float4 reflColor = (0,0,0,1);
    
    // Reflection Distortion
    float3 reflectionNormal = data.worldNormal;
    float3 n = IN.worldNormal;

    // 根据距离判断扭曲强度，越远的越弱 写死的50米
    float dis = length(GetCameraPositionWS().xz - data.worldPosition.xz);
    float distortion = lerp(_ReflectionDistortion, _ReflectionFarDistortion, dis / 50);
    reflectionNormal = lerp(n, reflectionNormal, distortion);

    float fresnel = 1 - dot(data.worldViewDir, reflectionNormal);
    fresnel = pow(saturate(fresnel), _ReflectionFresnel);

#ifndef _PLANARREFLECTION_ON

    //  View Space Ray Marching
    float3 reflectVS = normalize(reflect(IN.positionVS, TransformWorldToViewDir(reflectionNormal)));
    float2 hitUV;

    UNITY_BRANCH
    if (RayMarching(IN.positionVS, reflectVS, hitUV))
    {
        reflColor = SampleScreenColor(hitUV);
        const float fadeRateEdge = 8.0f;

        // Fade the reflections out near the edges.
        float2 edgeFactor = max(fadeRateEdge * abs(hitUV.xy - 0.5f) - (fadeRateEdge * 0.5f - 1.0f), 0.0f);
        float fade = saturate(1.0 - dot(edgeFactor, edgeFactor));

        // 指向物体背面的向量都干掉
        fade *= (1.0 - max(0.0, reflectVS.z));
        
        reflColor *= fade;

        float3 reflectVector = reflect(-data.worldViewDir, reflectionNormal);
        float4 cubeColor = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector);
        cubeColor.rgb = DecodeHDREnvironment(cubeColor, _CubemapTexture_HDR);

        reflColor = lerp(cubeColor, reflColor, fade);
    }
    else
    {
        float3 reflectVector = reflect(-data.worldViewDir, reflectionNormal);
        reflColor.rgb = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector).rgb;
        reflColor.rgb = DecodeHDREnvironment(reflColor, _CubemapTexture_HDR);
    }

#else
    float2 reflectionUV = data.screenUV + reflectionNormal.zx * float2(0.02, 0.15) * _PlanarReflectionDistortionIntensity;
    reflColor = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, reflectionUV);
#endif
    
    // TODO 虚幻的屏幕空间的摩尔纹很严重，还不知道为啥
    // float3 ueHitUV;
    //
    // UNITY_BRANCH
    // if(RayCast(IN.pos.xy, data.worldPosition, -data.pixelDepth, normalize(reflect(-data.worldViewDir, reflectionNormal)), 12, ueHitUV))
    // {
    //     reflColor = SampleScreenColor(ueHitUV);
    // }
    // else
    // {
    //     // cubemap TODO 矫正反射射线
    //     float3 reflectVector = reflect(-data.worldViewDir, reflectionNormal);
    //     reflColor.rgb = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector).rgb;
    // }

    // data.debugInfo.rgb = reflColor;

     data.finalColor = lerp(data.finalColor, reflColor, fresnel * saturate(_ReflectionIntensity));
     data.finalColor += data.addLight;
}


#endif