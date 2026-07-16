#ifndef TOONPBR_RIM
#define TOONPBR_RIM

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// 因为只能使用预深度来进行绘制，所以这里无法用 _CameraDepthTexture 来进行替代
#ifdef _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE
TEXTURE2D_X_FLOAT(_CameraCharacterDepthTexture); SAMPLER(sampler_CameraCharacterDepthTexture);
#else
TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
#endif

float SampleSceneDepth(float2 uv)
{
    float depth = 0;

    #ifdef _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE
    depth = SAMPLE_TEXTURE2D_X(_CameraCharacterDepthTexture, sampler_CameraCharacterDepthTexture, uv).r;
    #else
    depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
    #endif

    return depth;
}

// float3 RimLight(half3 rimColor, half3 normalWS, half3 viewDirectionWS, half3 lightDirectionWS, half4 vertecColor, half rimStep, half rimFeather, half rimBlendShadow, half rimBlendLdotV, half rimFlip, half radiance)
// {
//     half LdotV = dot(-lightDirectionWS, viewDirectionWS) * 0.5 + 0.5;
//     half fresnel = 1.0 - saturate(dot(normalWS, viewDirectionWS));
//     fresnel = StepFeatherToon(fresnel, rimStep, rimFeather);
//     fresnel = lerp(fresnel, fresnel * LdotV, rimBlendLdotV);
//     half3 color = rimColor * fresnel;
//     radiance = lerp(radiance, 1 - radiance, rimFlip);
//     color = lerp(color, color * radiance, rimBlendShadow);
//     return float3(0,0,0);
//     return color;
// }

// depth rim
half3 RimLight(
    half3 shadingColor, half3 frontRimColor, half3 backRimColor,
    half3 normalWS, half3 lightDirectionWS, float4 screenPos,
    half rimWidth, half rimDepthCutOff,
    half rimControlMask, half rimWidth2, half rimDepthCutOff2)
{
    #if !defined( _RIM_ON )
    return 0;
    #endif

    float3 normalVS = TransformWorldToViewDir(normalWS, true);

    half NdotL = dot(normalWS, lightDirectionWS);
    half revertNdotL = saturate(-NdotL);
    NdotL = saturate(NdotL);

    half3 rimColor = (frontRimColor * NdotL + backRimColor * revertNdotL) * shadingColor;

    float2 depthUV = (screenPos.xy + normalVS.xy * rimWidth * 0.05) / screenPos.w;
    float depth = LinearEyeDepth(SampleSceneDepth(depthUV), _ZBufferParams);
    float deltaDepth = depth - screenPos.z;

    float2 depthUV2 = (screenPos.xy + normalVS.xy * rimWidth2 * 0.05) / screenPos.w;
    float depth2 = LinearEyeDepth(SampleSceneDepth(depthUV2), _ZBufferParams);
    float deltaDepth2 = depth2 - screenPos.z;
    
    float dco = rimControlMask > 0 ? rimDepthCutOff2 : rimDepthCutOff;
    float dd = rimControlMask > 0 ? deltaDepth2 : deltaDepth;
    float rimIntensity = step(dco, dd);
    
    return rimIntensity * rimColor;
}

#endif // TOONPBR_RIM
