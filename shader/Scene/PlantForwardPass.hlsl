#ifndef __PLANT_FORWARD_PASS__
#define __PLANT_FORWARD_PASS__

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

    #ifdef _INTERSECTION_ON
    output.positionWS += VegetationInteractiveWS(output.positionWS, clamp(input.uv1.x * 1.5, 0.0, 1.0), _IntersectionIntensity);
    #endif

    #ifdef _WIND_ON
    Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    #endif
    
    #ifdef _RIM_ON
    output.positionVS = TransformWorldToView(output.positionWS);
    output.positionNDC = ComputeScreenPos(TransformWorldToHClip(output.positionWS));
    #endif

    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.positionSS = ComputeScreenPos(output.positionCS);
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
    #endif
    
    UBPA_TRANSFER_FOG(output, output.positionWS);

    // 基于中心点计算对象id
    VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
    float objectId = dot(vertexInput0.positionWS, 1);
    output.objectId = objectId;

    return output;
}

void MRTBufferPass(SurfaceInput input, float objectId, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
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

void frag(SurfaceInput input
    , out half4 outColor : SV_Target0
    #ifdef _MRT_BUFFER
    , out half4 outForwardBuffer0 : SV_Target1
    , out half4 outForwardBuffer1 : SV_Target2
    , out half4 outForwardBuffer2 : SV_Target3
    #endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.uv0.xy) * _MainColor;
    clip(albedo.a - _AlphaTestThreshold);
    #ifdef  _THECUT_ON
    half3 absWorldPos = GetAbsolutePositionWS(input.positionWS).xyz;
    half3 viewDirWS = normalize( absWorldPos - GetCameraPositionWS());
    // 计算原始面法线
    half3 orig = cross(ddx(absWorldPos),ddy(absWorldPos));
    orig = normalize(orig);
    float viewAngle = dot(orig,viewDirWS);
    float angleFactor = 1.0 - abs(viewAngle);
    // 计算裁剪
    float clipThreshold = cos(radians(_ThecutAngle));
    float clipFactor = smoothstep(
        clipThreshold - _ThecutSmoothness, 
        clipThreshold + _ThecutSmoothness, 
        angleFactor
    );
    clip(albedo.a * _MainColor.a *(1 - clipFactor * _ThecutStrength)-_AlphaTestThreshold);
    #endif

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

    half sndl = NdotL;
    #ifdef _COLORFUL_ON
    // GraaientCol
    half3 gradientCol = 1;
    half3 shadowColor = half3(0,0,0);
    half wpDot = dot(input.normalWS,half3(0,1,0));
    wpDot = smoothstep(0,1,wpDot);
    gradientCol = lerp(_BottomColor.rgb, _TopColor.rgb, wpDot);
    sndl = smoothstep(_ShadowRange - 0.5 * _ShadowSmooth, _ShadowRange + 0.5 * _ShadowSmooth, NdotL);
    shadowColor = _ShadowColor.rgb;
    directDiffuse = light.color * albedo*gradientCol* ao;
    directDiffuse = lerp(shadowColor * directDiffuse, directDiffuse, sndl * (light.distanceAttenuation * light.shadowAttenuation));
    #endif
    
    #ifdef _RIM_ON
    half Rim = 0;
    // 等宽边缘光计算
    // 采样当前场景深度图
    half2 screenUV = input.positionNDC.xy/input.positionNDC.w;
    half depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture,sampler_CameraDepthTexture, screenUV).r;
    half linearEyeDepth = LinearEyeDepth(depth,_ZBufferParams);
    
    // 顶点外扩后采样深度图
    half3 normalVS = normalize(TransformWorldToViewNormal(input.normalWS));
    half3 offsetPositionVS = half3(input.positionVS.xy + normalVS.xy * _RimOffset * 0.01h, input.positionVS.z);
    half4 offsetPositionCS = TransformWViewToHClip(offsetPositionVS);
    half4 offsetPostionVP = TransformHClipToViewPortPos(offsetPositionCS);
    half offsetDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture,offsetPostionVP).r;
    half offsetLinearEyeDepth = LinearEyeDepth(offsetDepth, _ZBufferParams);
    
    half todIndex_1, todIndex_2, todIndex_3, todIndex_4;
    GetTODIndex(todIndex_1, todIndex_2, todIndex_3, todIndex_4);
    half rimIntensity = GetRimIntensity(todIndex_1, todIndex_2, todIndex_3, todIndex_4);
    half3 rimColor = GetRimColor(todIndex_1, todIndex_2, todIndex_3, todIndex_4);
    
    Rim = smoothstep(0,_RimThreshold, offsetLinearEyeDepth - linearEyeDepth) * rimIntensity;
    Rim = lerp(0, Rim, sndl * light.distanceAttenuation * light.shadowAttenuation);
    directDiffuse += Rim * albedo * rimColor;
    #endif

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
    
    #ifdef _MRT_BUFFER
    MRTBufferPass(input, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // __PLANT_FORWARD_PASS__
