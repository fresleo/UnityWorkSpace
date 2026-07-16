#ifndef __TERRAIN_PBR_PASSES__
#define __TERRAIN_PBR_PASSES__

#include ".././ShaderLibrary/Lighting.hlsl"
#include "./TerrainPbrBakeSlat.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 clipPos                  : SV_POSITION;
    
    float4 uvMainAndLM              : TEXCOORD0; // xy: control, zw: lightmap

    #ifndef TERRAIN_SPLAT_BASEPASS
    float4 uvSplat01                : TEXCOORD1; // xy: splat0, zw: splat1
    float4 uvSplat23                : TEXCOORD2; // xy: splat2, zw: splat3
    #endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half4 normal                    : TEXCOORD3; // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4; // xyz: tangent, w: viewDir.y
    half4 bitangent                 : TEXCOORD5; // xyz: bitangent, w: viewDir.z
    #else
    half3 normal                    : TEXCOORD3;
    half3 vertexSH                  : TEXCOORD4; // SH
    #endif

    float3 positionWS               : TEXCOORD6;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
    #endif
    
    UBPA_FOG_COORDS(8)
    
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings SplatmapVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

    VertexPositionInputs Attributes = GetVertexPositionInputs(input.positionOS.xyz);

    output.uvMainAndLM.xy = input.texcoord;
    output.uvMainAndLM.zw = input.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;

    #ifndef TERRAIN_SPLAT_BASEPASS
    output.uvSplat01.xy = TRANSFORM_TEX(input.texcoord, _Splat0);
    output.uvSplat01.zw = TRANSFORM_TEX(input.texcoord, _Splat1);
    output.uvSplat23.xy = TRANSFORM_TEX(input.texcoord, _Splat2);
    output.uvSplat23.zw = TRANSFORM_TEX(input.texcoord, _Splat3);
    #endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(Attributes.positionWS);
    float4 vertexTangent = float4(cross(float3(0, 0, 1), input.normalOS), 1.0);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, vertexTangent);

    output.normal = half4(normalInput.normalWS, viewDirWS.x);
    output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
    output.normal = TransformObjectToWorldNormal(input.normalOS);
    output.vertexSH = SampleSH(output.normal);
    #endif

    output.positionWS = Attributes.positionWS;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(Attributes);
    #endif
    
    UBPA_TRANSFER_FOG(output, output.positionWS);

    output.clipPos = Attributes.positionCS;

    return output;
}

void InitializeInputData(Varyings IN, half3 normalTS, half GIIndirectDiffuseBoost, half BakedGITintIntensity, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = IN.positionWS;
    inputData.positionCS = IN.clipPos;

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = half3(IN.normal.w, IN.tangent.w, IN.bitangent.w);
    inputData.tangentToWorld = half3x3(-IN.tangent.xyz, IN.bitangent.xyz, IN.normal.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
    half3 SH = 0;
    
    #elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
    float2 sampleCoords = (IN.uvMainAndLM.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    half3 normalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
    half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(-tangentWS, cross(normalWS, tangentWS), normalWS));
    half3 SH = IN.vertexSH;
    
    #else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
    inputData.normalWS = IN.normal;
    half3 SH = IN.vertexSH;
    
    #endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = IN.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = 0;

    half3 bakedGITint = lerp(1, _BakedGITint.rgb, BakedGITintIntensity);
    // GI = lightmap * GI贡献比例 * 全局调色参数
    inputData.bakedGI = SAMPLE_GI(IN.uvMainAndLM.zw, SH, inputData.normalWS) * GIIndirectDiffuseBoost * bakedGITint;

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.clipPos);
    inputData.shadowMask = SAMPLE_SHADOWMASK(IN.uvMainAndLM.zw)
}

#ifdef _TERRAIN_BLEND_HEIGHT
void HeightBasedSplatModify(inout half4 splatControl, in half4 masks[4])
{
    // heights are in mask blue channel, we multiply by the splat Control weights to get combined height
    half4 splatHeight = half4(masks[0].b, masks[1].b, masks[2].b, masks[3].b) * splatControl.rgba;
    half maxHeight = max(splatHeight.r, max(splatHeight.g, max(splatHeight.b, splatHeight.a)));

    // Ensure that the transition height is not zero.
    half transition = max(_HeightTransition, 1e-5);

    // This sets the highest splat to "transition", and everything else to a lower value relative to that, clamping to zero
    // Then we clamp this to zero and normalize everything
    half4 weightedHeights = splatHeight + transition - maxHeight.xxxx;
    weightedHeights = max(0, weightedHeights);

    // We need to add an epsilon here for active layers (hence the blendMask again)
    // so that at least a layer shows up if everything's too low.
    weightedHeights = (weightedHeights + 1e-6) * splatControl;

    // Normalize (and clamp to epsilon to keep from dividing by zero)
    half sumHeight = max(dot(weightedHeights, half4(1, 1, 1, 1)), 1e-6);
    splatControl = weightedHeights / sumHeight.xxxx;
}
#endif

void SplatmapFinalColor(Varyings input, inout half4 color)
{
    color.rgb *= color.a;

    #ifdef TERRAIN_SPLAT_ADDPASS
    UBPA_APPLY_FOG_COLOR(input, color, 0);
    #else
    UBPA_APPLY_FOG(input, color);
    #endif
}

void ComputeMasks(out half4 masks[4], half4 hasMask, Varyings IN)
{
    masks[0] = 0.5h;
    masks[1] = 0.5h;
    masks[2] = 0.5h;
    masks[3] = 0.5h;

    #ifdef _MASKMAP
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, IN.uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, IN.uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, IN.uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, IN.uvSplat23.zw), hasMask.w);
    #endif

    masks[0] *= _MaskMapRemapScale0.rgba;
    masks[0] += _MaskMapRemapOffset0.rgba;
    masks[1] *= _MaskMapRemapScale1.rgba;
    masks[1] += _MaskMapRemapOffset1.rgba;
    masks[2] *= _MaskMapRemapScale2.rgba;
    masks[2] += _MaskMapRemapOffset2.rgba;
    masks[3] *= _MaskMapRemapScale3.rgba;
    masks[3] += _MaskMapRemapOffset3.rgba;
}

void SplatmapFragment(Varyings input
    , out half4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float2 splatUV = (input.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);

    #ifdef TERRAIN_SPLAT_BASEPASS
    half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMainAndLM.xy).rgb;
    half smoothness = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMainAndLM.xy).a;
    half metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, input.uvMainAndLM.xy).r;
    half alpha = 1;
    half occlusion = 1;
    
    #else
    half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    half4 masks[4];
    ComputeMasks(masks, hasMask, input);
    
    half alpha = dot(splatControl, 1.0h);
    #ifdef _TERRAIN_BLEND_HEIGHT
    // disable Height Based blend when there are more than 4 layers (multi-pass breaks the normalization)
    if (_NumLayersCount <= 4)
    {
        HeightBasedSplatModify(splatControl, masks);
    }
    #endif

    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness;
    half sampleAO = 1, sampleMetallic = 0;
    SplatmapMix(input.uvSplat01, input.uvSplat23, splatControl
        , weight, mixedDiffuse, defaultSmoothness
        , normalTS, sampleAO, sampleMetallic);
    half3 albedo = mixedDiffuse.rgb;

    half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    half4 defaultOcclusion = half4(_MaskMapRemapScale0.g, _MaskMapRemapScale1.g, _MaskMapRemapScale2.g, _MaskMapRemapScale3.g)
        + half4(_MaskMapRemapOffset0.g, _MaskMapRemapOffset1.g, _MaskMapRemapOffset2.g, _MaskMapRemapOffset3.g);

    half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    half smoothness = dot(splatControl, defaultSmoothness);

    half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
    defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
    half metallic = dot(splatControl, defaultMetallic);

    half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
    defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
    half occlusion = dot(splatControl, defaultOcclusion);

    #endif

    half mixSpecularScaleBRDF = ControlMixValue(splatControl, _SpecularScaleBRDF0, _SpecularScaleBRDF1, _SpecularScaleBRDF2, _SpecularScaleBRDF3);
    half mixGIIndirectDiffuseBoost = ControlMixValue(splatControl, _GIIndirectDiffuseBoost0, _GIIndirectDiffuseBoost1, _GIIndirectDiffuseBoost2, _GIIndirectDiffuseBoost3);
    half mixBakedGITintIntensity = ControlMixValue(splatControl, _BakedGITintIntensity0, _BakedGITintIntensity1, _BakedGITintIntensity2, _BakedGITintIntensity3);
    
    InputData inputData;
    InitializeInputData(input, normalTS, mixGIIndirectDiffuseBoost, mixBakedGITintIntensity, inputData);
    
    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = mixSpecularScaleBRDF;
    
    half4 color = FragmentPBR(inputData, extendData, albedo, /* specular */ half3(0, 0, 0), metallic, smoothness, occlusion, /* emission */ half3(0, 0, 0), alpha);
    SplatmapFinalColor(input, color);
    outColor = half4(color.rgb, 1);
}

#endif //__TERRAIN_PBR_PASSES__
