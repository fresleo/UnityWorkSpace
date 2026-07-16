#ifndef __WATER_FORWARD__
#define __WATER_FORWARD__

#include "../../ShaderLibrary/Lighting.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
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

    float4 eyeDepth                 : TEXCOORD7;

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

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 objectToViewPos = TransformWorldToView(positionWS);
    float eyeDepth = -objectToViewPos.z;
    output.eyeDepth.x = eyeDepth;
    output.eyeDepth.yzw = 0;

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

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(0, vertexLight);

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
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float3 WorldNormal = normalize(input.tSpace0.xyz);
    float3 WorldTangent = input.tSpace1.xyz;
    float3 WorldBiTangent = input.tSpace2.xyz;

    float3 WorldPosition = float3(input.tSpace0.w, input.tSpace1.w, input.tSpace2.w);
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz - WorldPosition;
    float4 ShadowCoords = float4(0, 0, 0, 0);

    float4 ClipPos = input.clipPosV;
    float4 ScreenPos = ComputeScreenPos(input.clipPosV);

    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    ShadowCoords = input.shadowCoord;
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
    #endif

    WorldViewDirection = SafeNormalize(WorldViewDirection);

    float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos(ScreenPos);
    float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
    float4 fetchOpaqueVal341 = float4(SHADERGRAPH_SAMPLE_SCENE_COLOR(ase_grabScreenPosNorm.xy), 1.0);
    
    float Time525 = _TimeParameters.x * _WavesSpeed;
    float temp_output_533_0 = (Time525 * 0.1);
    
    float2 appendResult106 = (float2(WorldPosition.x, WorldPosition.z));
    float2 WorldSpaceTile68 = (appendResult106 * _Tiling);
    
    float2 panner112 = (temp_output_533_0 * float2(1, 1) + WorldSpaceTile68);
    half4 wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, panner112);
    float3 unpack38 = UnpackNormalScale(wavesNormalTex, _NormalIntensity);
    unpack38.z = lerp(1, unpack38.z, saturate(_NormalIntensity));
    
    float2 panner114 = ((1.0 - temp_output_533_0) * float2(1, 1) + WorldSpaceTile68);
    wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, (1.0 - panner114));
    float3 unpack46 = UnpackNormalScale(wavesNormalTex, _NormalIntensity);
    unpack46.z = lerp(1, unpack46.z, saturate(_NormalIntensity));

    float3 Normal89 = BlendNormal(unpack38, unpack46);
    float4 temp_output_277_0 = (ase_grabScreenPosNorm + float4((Normal89 * (_RefractionFactor * 0.1)), 0.0));
    float4 fetchOpaqueVal274 = float4(SHADERGRAPH_SAMPLE_SCENE_COLOR(temp_output_277_0.xy), 1.0);
    float eyeDepth337 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(temp_output_277_0.xy), _ZBufferParams);
    
    float eyeDepth = input.eyeDepth.x;
    float ifLocalVar336 = 0;
    if (eyeDepth337 > eyeDepth)
        ifLocalVar336 = 1.0;
    else if (eyeDepth337 < eyeDepth)
        ifLocalVar336 = 0.0;
    float4 lerpResult342 = lerp(fetchOpaqueVal341, fetchOpaqueVal274, ifLocalVar336);
    float4 Refractions282 = saturate(lerpResult342);
    
    float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
    ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
    float screenDepth384 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(ase_screenPosNorm.xy), _ZBufferParams);
    float distanceDepth384 = saturate(abs((screenDepth384 - LinearEyeDepth(ase_screenPosNorm.z, _ZBufferParams)) / (_Transparency)));
    float saferPower405 = abs(distanceDepth384);
    float4 lerpResult389 = lerp(Refractions282, _WaterColor, pow(saferPower405, _TransparencyFade));
    
    float time61 = (Time525 * 5);
    float2 voronoiSmoothId61 = 0;
    float2 coords61 = WorldSpaceTile68 * _FoamTiling;
    float2 id61 = 0;
    float2 uv61 = 0;
    float fade61 = 0.5;
    float voroi61 = 0;
    float rest61 = 0;
    for (int it61 = 0; it61 < 3; it61++)
    {
        voroi61 += fade61 * voronoi61(coords61, time61, id61, uv61, 0, voronoiSmoothId61);
        rest61 += fade61;
        coords61 *= 2;
        fade61 *= 0.5;
    } //Voronoi61
    voroi61 /= rest61;

    float saferPower557 = abs( 1.0 - voroi61 );
    float screenDepth17 = LinearEyeDepth( SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ), _ZBufferParams );
    float distanceDepth17 = abs( ( screenDepth17 - LinearEyeDepth( ase_screenPosNorm.z, _ZBufferParams ) ) / ( _FoamDistance * 0.3 ) );
    #ifdef _ENABLEFOAM_ON
    float staticSwitch538 = ( saturate( ( pow( saferPower557 , -1.5 ) + ( 1.0 - distanceDepth17 ) ) ) * _FoamOpacity );
    #else
    float staticSwitch538 = 0.0;
    #endif
    float Foam183 = staticSwitch538;
    float4 Color502 = ( lerpResult389 + Foam183 );

    float screenDepth390 = LinearEyeDepth( SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ), _ZBufferParams );
    float distanceDepth390 = saturate( abs( ( screenDepth390 - LinearEyeDepth( ase_screenPosNorm.z, _ZBufferParams ) ) / ( _CoastalBlending * 0.5 ) ) );

    float3 BaseColor = Color502.rgb;
    float3 Normal = Normal89;
    float3 Emission = 0;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = ( _Smoothness - Foam183 );
    float Occlusion = 1;
    float Alpha = distanceDepth390;
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

    inputData.fogCoord = 0;
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
