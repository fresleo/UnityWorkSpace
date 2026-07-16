#ifndef __SURFACE_FORWARD__
#define __SURFACE_FORWARD__

#include "../../ShaderLibrary/Lighting.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

struct VertexInput
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;

    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS               : SV_POSITION;

    float4 clipPosV                 : TEXCOORD0;
    float4 lightmapUVOrVertexSH     : TEXCOORD1;
    half4 fogFactorAndVertexLight   : TEXCOORD2;

    float4 tSpace0                  : TEXCOORD3;
    float4 tSpace1                  : TEXCOORD4;
    float4 tSpace2                  : TEXCOORD5;

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    float4 shadowCoord              : TEXCOORD6;
    #endif

    float4 uv                       : TEXCOORD7;
    float4 positionOS               : TEXCOORD8;

    float3 positionWS			    : TEXCOORD9;
    UBPA_FOG_COORDS(10)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv.xy = input.texcoord.xy;
    output.uv.zw = 0;

    output.positionOS = input.positionOS;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.tSpace0 = float4(normalInput.normalWS, vertexInput.positionWS.x);
    output.tSpace1 = float4(normalInput.tangentWS, vertexInput.positionWS.y);
    output.tSpace2 = float4(normalInput.bitangentWS, vertexInput.positionWS.z);

    #if defined( LIGHTMAP_ON )
    OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
    #endif

    #if !defined( LIGHTMAP_ON )
    OUTPUT_SH(normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz);
    #endif

    half fogFactor = 0;
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;

    output.positionWS = vertexInput.positionWS;
    UBPA_TRANSFER_FOG(output, output.positionWS);

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(input.positionCS.xyz, unity_LODFade.x);
    #endif

    float3 WorldNormal = normalize(input.tSpace0.xyz);
    float3 WorldTangent = input.tSpace1.xyz;
    float3 WorldBiTangent = input.tSpace2.xyz;

    float3 WorldPosition = float3(input.tSpace0.w, input.tSpace1.w, input.tSpace2.w);
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz - WorldPosition;
    float4 ShadowCoords = float4(0, 0, 0, 0);

    float4 ScreenPos = ComputeScreenPos(input.clipPosV);

    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    ShadowCoords = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
    #endif

    WorldViewDirection = SafeNormalize(WorldViewDirection);

    float2 temp_cast_0 = _Tiling.xx;
    float2 texCoord472 = input.uv.xy * temp_cast_0;
    float2 Tiling462 = texCoord472;
    half4 tex2DNode2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, Tiling462);
    float4 temp_output_3_0 = _Color * tex2DNode2;
    
    float CoverageTiling463 = _CovTiling;
    float2 temp_cast_1 = CoverageTiling463.xx;
    float4 triplanar421 = TriplanarSampling421(TEXTURE2D_ARGS(_CovMainTex, sampler_CovMainTex), WorldPosition, WorldNormal, 10, temp_cast_1, 1, 0);

    half4 bumpMapTex = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, Tiling462);
    float3 unpack6 = UnpackNormalScale(bumpMapTex, _BumpScale);
    unpack6.z = lerp(1, unpack6.z, saturate(_BumpScale));
    float3 tex2DNode6 = unpack6;
    float3 MainNormalMap454 = tex2DNode6;
    
    float3 tanToWorld0 = float3(WorldTangent.x, WorldBiTangent.x, WorldNormal.x);
    float3 tanToWorld1 = float3(WorldTangent.y, WorldBiTangent.y, WorldNormal.y);
    float3 tanToWorld2 = float3(WorldTangent.z, WorldBiTangent.z, WorldNormal.z);
    float3 tanNormal283 = MainNormalMap454;
    float3 worldNormal283 = float3(dot(tanToWorld0, tanNormal283), dot(tanToWorld1, tanNormal283), dot(tanToWorld2, tanNormal283));

    float lerpResult457 = lerp(worldNormal283.y, input.positionOS.y, _CovOverlayMethod);
    float temp_output_289_0 = (lerpResult457 + (1.0 - _CovOffset));
    float lerpResult541 = lerp(temp_output_289_0, (1.0 - temp_output_289_0), _CovBalance);
    float2 appendResult550 = (float2(_MaskTilingX, _MaskTilingY));
    float2 texCoord545 = input.uv.xy * appendResult550;
    float2 MaskTiling551 = texCoord545;
    half4 tex2DNode507 = SAMPLE_TEXTURE2D(_CovMask, sampler_CovMask, MaskTiling551);
    
    float lerpResult524 = lerp((1.0 - tex2DNode507.g), tex2DNode507.g, _MaskContrast);
    float CoverageMask297 = saturate((lerpResult541 * saturate(lerpResult524)));
    float4 lerpResult302 = lerp(temp_output_3_0, (_CovColor * triplanar421), CoverageMask297);
    #ifdef _COVERAGEON_ON
    float4 staticSwitch304 = lerpResult302;
    #else
    float4 staticSwitch304 = temp_output_3_0;
    #endif
    float4 Albedo19 = staticSwitch304;

    float2 temp_cast_4 = CoverageTiling463.xx;
    float3x3 ase_worldToTangent = float3x3(WorldTangent, WorldBiTangent, WorldNormal);
    float3 triplanar432 = TriplanarSampling432(TEXTURE2D_ARGS(_CovBumpMap, sampler_CovBumpMap), WorldPosition, WorldNormal, 10, temp_cast_4, _CovBumpScale, 0);
    
    float3 tanTriplanarNormal432 = mul(ase_worldToTangent, triplanar432);
    float3 lerpResult515 = lerp(BlendNormal(tex2DNode6, tanTriplanarNormal432), tanTriplanarNormal432, (1.0 - _NormalBlending));
    float3 lerpResult309 = lerp(tex2DNode6, lerpResult515, CoverageMask297);
    #ifdef _COVERAGEON_ON
    float3 staticSwitch308 = lerpResult309;
    #else
    float3 staticSwitch308 = tex2DNode6;
    #endif
    float3 Normal75 = staticSwitch308;

    half4 tex2DNode239 = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, Tiling462);
    float temp_output_241_0 = _Metallic * tex2DNode239.r;
    float2 temp_cast_5 = CoverageTiling463.xx;
    float4 triplanar430 = TriplanarSampling430(TEXTURE2D_ARGS(_CoverageMetallicSmoothness, sampler_CoverageMetallicSmoothness), WorldPosition, WorldNormal, 10, temp_cast_5, 1, 0);
    float lerpResult335 = lerp(temp_output_241_0, (_CovMetallic * triplanar430.x), CoverageMask297);
    #ifdef _COVERAGEON_ON
    float staticSwitch340 = lerpResult335;
    #else
    float staticSwitch340 = temp_output_241_0;
    #endif
    float Metallic262 = staticSwitch340;

    float AlbedoAlpha434 = tex2DNode2.a;
    float lerpResult441 = lerp(tex2DNode239.a, AlbedoAlpha434, _SmoothnessTextureChannel);
    float temp_output_240_0 = (lerpResult441 * _Glossiness);
    float CoverageAlbedoAlpha369 = triplanar421.a;
    float lerpResult451 = lerp(triplanar430.a, CoverageAlbedoAlpha369, _CovSmoothnessTextureChannel);
    float lerpResult333 = lerp(temp_output_240_0, (lerpResult451 * _CovGlossiness), CoverageMask297);
    #ifdef _COVERAGEON_ON
    float staticSwitch339 = lerpResult333;
    #else
    float staticSwitch339 = temp_output_240_0;
    #endif
    float Smoothness263 = staticSwitch339;

    half4 occlusionMapTex = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, Tiling462);
    float lerpResult410 = lerp(1.0, occlusionMapTex.g, _OcclusionStrength);
    float Ambient_Occlusion415 = lerpResult410;

    float3 BaseColor = Albedo19.rgb;
    float3 Normal = Normal75;
    float3 Emission = 0;
    float3 Specular = 0.5;
    float Metallic = Metallic262;
    float Smoothness = Smoothness263;
    float Occlusion = Ambient_Occlusion415;
    float Alpha = 1;
    float AlphaClipThreshold = 0.5;
    float AlphaClipThresholdShadow = 0.5;
    float3 BakedGI = 0;
    float3 RefractionColor = 1;
    float RefractionIndex = 1;
    float3 Transmission = 1;
    float3 Translucency = 1;

    InputData inputData = (InputData)0;
    inputData.positionWS = WorldPosition;
    inputData.viewDirectionWS = WorldViewDirection;

    #ifdef _NORMALMAP
        #if _NORMAL_DROPOFF_TS
    inputData.normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent, WorldBiTangent, WorldNormal));
        #elif _NORMAL_DROPOFF_OS
    inputData.normalWS = TransformObjectToWorldNormal(Normal);
        #elif _NORMAL_DROPOFF_WS
    inputData.normalWS = Normal;
        #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    #else
    inputData.normalWS = WorldNormal;
    #endif

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    inputData.shadowCoord = ShadowCoords;
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

    float3 SH = input.lightmapUVOrVertexSH.xyz;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);

    inputData.normalizedScreenSpaceUV = NormalizedScreenSpaceUV;
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);

    SurfaceData surfaceData;
    surfaceData.albedo              = BaseColor;
    surfaceData.metallic            = saturate(Metallic);
    surfaceData.specular            = Specular;
    surfaceData.smoothness          = saturate(Smoothness),
    surfaceData.occlusion           = Occlusion,
    surfaceData.emission            = Emission,
    surfaceData.alpha               = saturate(Alpha);
    surfaceData.normalTS            = Normal;
    surfaceData.clearCoatMask       = 0;
    surfaceData.clearCoatSmoothness = 1;

    //half4 color = UniversalFragmentPBR( inputData, surfaceData);

    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = 1;
    
    half4 color = FragmentPBR(inputData, surfaceData, extendData);

    UBPA_APPLY_FOG(input, color);
    
    return color;
}

#endif
