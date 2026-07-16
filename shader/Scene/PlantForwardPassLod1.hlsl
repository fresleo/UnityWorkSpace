#ifndef __PLANT_FORWARD_PASS_LOD_1__
#define __PLANT_FORWARD_PASS_LOD_1__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_PLANT_ON
#define ao input.color.a

#include "../ShaderLibrary/Lighting.hlsl"
#include "./Wind.hlsl"
#include "../ShaderLibrary/Translucency.hlsl"
#include "../ShaderLibrary/InteractiveParams.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

SurfaceInput vert(VertexAttributes input)
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

    output.uv0.xy = TRANSFORM_TEX(input.uv0, _Albedo);
    output.uv1 = input.uv1;

    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.positionSS = ComputeScreenPos(output.positionCS);
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
    #endif
    
    UBPA_TRANSFER_FOG(output, output.positionWS);

    return output;
}

void frag(SurfaceInput input
    , out half4 outColor : SV_Target
)
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.uv0.xy) * _MainColor;
    clip(albedo.a - _AlphaTestThreshold);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    half3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);

    float4 shadowCoord = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif

    Light light = GetMainLight(shadowCoord, input.positionWS, unity_ProbesOcclusion);
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);

    // half lambert
    half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
    half3 directDiffuse = attenuatedLightColor * NdotL * albedo * ao;

    half3 irradiance = SampleSH(input.normalWS);
    half3 indirectDiffuse = irradiance * albedo * ao;

    half3 sss = .0f;
    #ifdef _TRANSLUCENCY_ON
    TranslucencyProperty transProp = (TranslucencyProperty)0;
    transProp.translucencyStrength = _TranslucencyStrength;
    transProp.translucencyDistortion = _TranslucencyDistortion;
    transProp.translucencyScattering = _TranslucencyScattering;
    transProp.translucencyColor = _TranslucencyColor;
    transProp.translucencyAmbient = _TranslucencyAmbient;
    transProp.translucencyShadow = _TranslucencyShadow;
    sss = Translucency(irradiance, albedo, input.normalWS, viewDir, light, 0.1, transProp);
    #endif

    half3 shadingColor = directDiffuse + indirectDiffuse + sss;

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
    
    shadingColor += additionalColor;
    */

    UBPA_APPLY_FOG(input, shadingColor);

    half outAlpha = _DitherAlpha;
    outColor = half4(shadingColor, outAlpha);
}

#endif // __PLANT_FORWARD_PASS_LOD_1__
