#ifndef __TESSELLATED_PARALLAX_FORWARD_PASS__
#define __TESSELLATED_PARALLAX_FORWARD_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TessellationFuncs.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct Attributes
{
    float4 positionOS           : POSITION;

    half4 color                 : COLOR;
    float3 normalOS             : NORMAL;
    float4 tangentOS            : TANGENT;
    
    float2 texcoord             : TEXCOORD0;

    float2 staticLightmapUV     : TEXCOORD1;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS               : SV_POSITION;

    half4 color                     : COLOR;
    float3 normalWS                 : TEXCOORD0;
    half4 tangentWS                 : TEXCOORD1; // xyz: tangent, w: sign
    
    half3 viewDirTS                 : TEXCOORD2;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD3;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);

    float3 positionWS               : TEXCOORD5;
    float2 uv                       : TEXCOORD6;

    UBPA_FOG_COORDS(7)
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;
    
    inputData.positionWS = input.positionWS;
    
    float sgn = input.tangentWS.w; // 应该为 +1 或 -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.viewDirectionWS = viewDirWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    /*
    #if defined(DEBUG_DISPLAY)
        #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
        #endif
    
        #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
        #else
    inputData.vertexSH = input.vertexSH;
        #endif
    #endif
    */
}


///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

Varyings VertexFunction(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // 根据视察贴图，扩张顶点
    half4 parallaxMap = SAMPLE_TEXTURE2D_X_LOD(_ParallaxMap, sampler_ParallaxMap, output.uv, 0);
    float disp = saturate(parallaxMap.r - 0.515) * 2 * _Displacement;
    input.positionOS.xyz += input.normalOS * disp;
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    output.color = input.color;
    
    output.normalWS = normalInput.normalWS;
    
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tangentWS = tangentWS;
    
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif
    
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    
    output.positionWS = vertexInput.positionWS;

    UBPA_TRANSFER_FOG(output, vertexInput.positionWS);
    
    return output;
}

// 启用镶嵌
#if defined( ASE_TESSELLATION )
struct VertexControl
{
    float4 positionOS : INTERNALTESSPOS;

    half4 color : COLOR;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    
    float2 texcoord : TEXCOORD0;
    
    float2 staticLightmapUV : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexControl LitPassVertex(Attributes input)
{
    VertexControl output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionOS = input.positionOS;
    output.color = input.color;
    output.normalOS = input.normalOS;
    output.tangentOS = input.tangentOS;
    output.texcoord = input.texcoord;
    output.staticLightmapUV = input.staticLightmapUV;

    return output;
}

Attributes PatchToOutput(OutputPatch<VertexControl, 3> patch, float3 barycentricCoordinates)
{
    Attributes output = (Attributes)0;

    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, positionOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, color);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, normalOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, tangentOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, texcoord);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, staticLightmapUV);

    return output;
}

#include "./TessellatedParallax_TessellationFuncs.hlsl"

#else // 没启用镶嵌

Varyings LitPassVertex(Attributes input)
{
    return VertexFunction(input);
}
#endif // ASE_TESSELLATION


void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 视差
    half3 viewDirTS = input.viewDirTS;
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, input.color, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    // SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    //color.rgb = MixFog(color.rgb, inputData.fogCoord);

    UBPA_APPLY_FOG(input, color);
    
    outColor = color;
}

#endif // __TESSELLATED_PARALLAX_FORWARD_PASS__
