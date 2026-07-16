#ifndef __GRASS_FORWARD_PASS__
#define __GRASS_FORWARD_PASS__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_GRASS_ON
#define ao input.color.a

#include "./VegetationUtils.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/InteractiveParams.hlsl"
#include "../ShaderLibrary/GrassBlendWithTerrainAlbedo.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "./Wind.hlsl"
#include "Packages/toon.post.processing/Shaders/ExcludeCharacterLib.hlsl"

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
    
    // 没有在使用
    //half3 ambientOrLightmapUV = 0;
    //OUTPUT_SH(output.normalWS, ambientOrLightmapUV);
    
    output.uv0 = input.uv0;
    output.uv1 = input.uv1;

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && (defined(_MESH_INSTANCE_CULL_ON) || defined(_MESH_INSTANCE_TEX_FETCH_ON))
    int grassShaderLod = (int)UNITY_MATRIX_M[3].x;
#else
    int grassShaderLod = GetGrassShaderLod(output.positionWS);
#endif
    output.shaderLod = (half)grassShaderLod;

    // LOD 0 生效
    bool allowWind = grassShaderLod == 0; 
    bool allowInteraction = grassShaderLod == 0; 

    #ifdef _INTERSECTION_ON
    UNITY_BRANCH
    if (allowInteraction)
    {
        output.positionWS += VegetationInteractiveWS(output.positionWS, input.uv0.y);
    }
    #endif

    #ifdef _WIND_ON
    UNITY_BRANCH
    if (allowWind)
    {
        Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    }
    #endif

    UNITY_BRANCH
    if (_PersectiveCorrection > 0 && grassShaderLod == 0)
    {
        half3 viewDirectionWS = normalize(_WorldSpaceCameraPos - output.positionWS);
        output.positionWS += CorrectPerspective(output.positionWS, viewDirectionWS, input.uv1.x * _PersectiveCorrection);
    }

    output.positionCS = TransformWorldToHClip(output.positionWS);
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
    #endif

    output.positionSS = ComputeScreenPos(output.positionCS);
    
    // LOD 2 跳过雾效计算
    UNITY_BRANCH
    if (grassShaderLod <= 1)
    {
    	UBPA_TRANSFER_FOG(output, output.positionWS);
    }

    // 基于中心点计算对象id
    VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
    //float objectId = dot(vertexInput0.positionWS, float3(1, 1, 1));
    //output.objectId = objectId;
    // 简化写法
    output.objectId = dot(GetObjectPivot(), float3(1, 1, 1));

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
    int grassShaderLod = (int)input.shaderLod;
    // Lod 0/1 生效
    bool allowTerrainBlend = grassShaderLod <= 1;
    bool allowVariationSample = grassShaderLod <= 1;
    bool allowCharacterExclude = grassShaderLod <= 1;
    
    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif
    
    ApplyGrassDitherClip(input.positionSS, input.positionWS);
    
    #if defined( _EXCLUDE_CHARACTER_ON )
    UNITY_BRANCH
    if (allowCharacterExclude)
    {
        ExcludeCharacter(input.positionSS);
    }
    #endif

    half3 albedo = half3(0, 0, 0);
    half3 blendTerrainColor = half3(0, 0, 0);
    half3 blendTerrainColor2 = half3(0, 0, 0);

    half variationMask = 0.5h;
    if (allowVariationSample)
    {
        half variationMaskScale = max(_VariationMaskScale, 1e-4h);
        half2 variationMaskUV = input.positionWS.xz * rcp(variationMaskScale);
        variationMask = SAMPLE_TEXTURE2D(_VariationMask, sampler_VariationMask, variationMaskUV).r;
    }

    if (allowTerrainBlend)
    {
        half sqrtV = sqrt(input.uv0.y);
        half mask  = smoothstep(_BlendWithTerrainHeight,  1.0 + _BlendWithTerrainHeight,  sqrtV);
        half mask2 = smoothstep(_BBlendWithTerrainHeight, 1.0 + _BBlendWithTerrainHeight, sqrtV);

        half3 colorMapSample = SampleColorMapTexture(input.positionWS.xyz).rgb;
        blendTerrainColor.rgb  = lerp(ApplyColorMapPresampled(colorMapSample, _VariationColorA.rgb, _BlendWithTerrainStrength),  _VariationColorA.rgb, mask);
        blendTerrainColor2.rgb = lerp(ApplyColorMapPresampled(colorMapSample, _VariationColorB.rgb, _BBlendWithTerrainStrength), _VariationColorB.rgb, mask2);
        //blendTerrainColor = ApplyColorMap(input.positionWS.xyz, _VariationColorA.rgb, _BlendWithTerrainStrength);
        //blendTerrainColor2 = ApplyColorMap(input.positionWS.xyz, _VariationColorB.rgb, _BBlendWithTerrainStrength);
    }
    else
    {
        blendTerrainColor.rgb = _VariationColorA.rgb;
        blendTerrainColor2.rgb = _VariationColorB.rgb;
    }

    half3 baseColor = lerp(blendTerrainColor, blendTerrainColor2, variationMask);
    albedo = lerp(baseColor, baseColor * ao, _AOStrength);

    float4 shadowCoord = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif

    Light light = GetMainLight(shadowCoord, input.positionWS, unity_ProbesOcclusion);

    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
    half3 directDiffuse = attenuatedLightColor * NdotL * albedo;

    half3 irradiance = SampleSH(input.normalWS) * lerp(_GIIntensity, 1, light.shadowAttenuation);
    half3 indirectDiffuse = irradiance * albedo;

    half3 specularColor = lerp(_SpecularColor.rgb, _SpecularColor2.rgb, variationMask);
    half3 directSpecular = saturate(pow(input.uv0.y, 8) * 0.8) * attenuatedLightColor * specularColor;

    half3 shadingColor = indirectDiffuse + directDiffuse + directSpecular;
    
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

    // LOD 2 跳过雾效
    if (grassShaderLod <= 1)
    {
        UBPA_APPLY_FOG(input, shadingColor);
    }

    half outAlpha = ApplyBakedGrassLocalAlpha(input.positionWS, _DitherAlpha);
    outColor = half4(shadingColor, outAlpha);

    #ifdef _MRT_BUFFER
    MRTBufferPass(input, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // __GRASS_FORWARD_PASS__
