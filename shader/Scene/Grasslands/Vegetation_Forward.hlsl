#ifndef __VEGETATION_FORWARD__
#define __VEGETATION_FORWARD__

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

    float4 color        : COLOR;

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

    float3 ase_worldPos = TransformObjectToWorld(input.positionOS.xyz);

    float mulTime34 = _TimeParameters.x * (RAYGlobalWindForce * (_WindForce * 5));
    float simplePerlin3D35 = snoise((ase_worldPos + mulTime34) * ((1.0 - _WindWavesScale) * RAYGlobalWavesScale));
    simplePerlin3D35 = simplePerlin3D35 * 0.5 + 0.5;
    float temp_output_231_0 = pow( abs(simplePerlin3D35), (_WindFlowDensity * RAYGlobalFlowDensity) ) * 0.01;

    float2 texCoord357 = input.texcoord.xy;
    float lerpResult1020 = lerp(temp_output_231_0, (temp_output_231_0 * pow( abs(texCoord357.y), 1.5 )), _UVBaseLock);
    float4 transform916 = mul(GetWorldToObjectMatrix(), float4((RAYGlobalDirection * (lerpResult1020 * input.color.r * ((_WindForce * 100) * RAYGlobalWindForce))), 0.0));
    #ifdef _ENABLEWIND_ON
    float4 staticSwitch341 = transform916;
    #else
    float4 staticSwitch341 = float4(0, 0, 0, 0);
    #endif
    float4 Wind191 = staticSwitch341;

    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz));
    float eyeDepth = -objectToViewPos.z;
    float cameraDepthFade958 = ((eyeDepth - _ProjectionParams.y - _GrassFadeDistance) / 5.0);
    float lerpResult1039 = lerp((1.0 - cameraDepthFade958), cameraDepthFade958, (_GrassFalloff * 0.5));
    float lerpResult1023 = lerp(1.0, saturate(lerpResult1039), _GrassDistanceFadeEnable);
    float GrassDistanceFadeMask982 = lerpResult1023;
    float4 lerpResult1065 = lerp(float4((input.positionOS.xyz * -1), 0.0), Wind191, GrassDistanceFadeMask982);

    float3 lerpResult1096 = lerp(input.normalOS, float3(0, 1, 0), _LightingFlatness);
    float3 LightingFlatness1101 = lerpResult1096;

    output.uv.xy = input.texcoord.xy;
    output.uv.zw = 0;

    output.positionOS = input.positionOS;

    float3 vertexValue = lerpResult1065.xyz;
    input.positionOS.xyz += vertexValue;

    input.normalOS = LightingFlatness1101;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.tSpace0 = float4(normalInput.normalWS, vertexInput.positionWS.x);
    output.tSpace1 = float4(normalInput.tangentWS, vertexInput.positionWS.y);
    output.tSpace2 = float4(normalInput.bitangentWS, vertexInput.positionWS.z);

    #if defined(LIGHTMAP_ON)
    OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
    #endif

    #if !defined(LIGHTMAP_ON)
    OUTPUT_SH(normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz);
    #endif

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
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
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
    #endif

    WorldViewDirection = SafeNormalize(WorldViewDirection);

    float2 temp_cast_0 = _Tiling.xx;
    float2 texCoord1028 = input.uv.xy * temp_cast_0;
    float2 Tiling1032 = texCoord1028;
    half4 tex2DNode1 = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, Tiling1032);
    
    float4 temp_output_10_0 = _MainColor * tex2DNode1;
    float simplePerlin2D742 = snoise(WorldPosition.xz * _WorldNoiseScale);
    simplePerlin2D742 = simplePerlin2D742 * 0.5 + 0.5;
    
    float simplePerlin3D1015 = snoise(WorldPosition * _WorldNoiseScale);
    simplePerlin3D1015 = simplePerlin3D1015 * 0.5 + 0.5;
    
    float2 texCoord361 = input.uv.xy;
    #if defined( _SECONDCOLOROVERLAYTYPE_WORLD_NOISE_2D )
    float staticSwitch360 = simplePerlin2D742;
    #elif defined( _SECONDCOLOROVERLAYTYPE_WORLD_NOISE_3D )
    float staticSwitch360 = simplePerlin3D1015;
    #elif defined( _SECONDCOLOROVERLAYTYPE_VERTEX_GRADIENT )
    float staticSwitch360 = input.positionOS.y;
    #elif defined( _SECONDCOLOROVERLAYTYPE_UV_GRADIENT )
    float staticSwitch360 = texCoord361.y;
    #else
    float staticSwitch360 = simplePerlin2D742;
    #endif
    
    float temp_output_875_0 = (staticSwitch360 + (1.0 - _SecondColorOffset));
    float lerpResult1025 = lerp(temp_output_875_0, (1.0 - temp_output_875_0), (_SecondColorFade - -0.5));
    float SecondColorMask335 = saturate(lerpResult1025);
    
    float4 lerpResult332 = lerp(temp_output_10_0, (_SecondColor * tex2DNode1), SecondColorMask335);
    #ifdef _COLOR2ENABLE_ON
    float4 staticSwitch1022 = lerpResult332;
    #else
    float4 staticSwitch1022 = temp_output_10_0;
    #endif
    
    float4 Albedo259 = staticSwitch1022;
    float dotResult1117 = dot(WorldViewDirection, SafeNormalize(_MainLightPosition.xyz));
    float ase_lightAtten = 0;
    Light ase_mainLight = GetMainLight(ShadowCoords);
    ase_lightAtten = ase_mainLight.distanceAttenuation * ase_mainLight.shadowAttenuation;
    float ase_lightIntensity = max(max(_MainLightColor.r, _MainLightColor.g), _MainLightColor.b);
    float4 ase_lightColor = float4(_MainLightColor.rgb / ase_lightIntensity, ase_lightIntensity);
    float4 Translucency1142 = saturate((-dotResult1117 - 0.3) * ase_lightAtten * ase_lightColor * _TranslucencyColor * _TranslucencyInt);

    half4 normalTex = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, Tiling1032);
    float3 unpack886 = UnpackNormalScale( normalTex, _NormalScale );
    unpack886.z = lerp( 1, unpack886.z, saturate(_NormalScale) );
    float3 Normal888 = unpack886;

    half4 smoothnessTex = SAMPLE_TEXTURE2D(_SmoothnessTexture, sampler_SmoothnessTexture, Tiling1032);
    float Smoothness734 = saturate( smoothnessTex.g * _Smoothness );
    
    float AlbedoAlpha263 = tex2DNode1.a;

    float3 BaseColor = ( Albedo259 + Translucency1142 ).rgb;
    float3 Normal = Normal888;
    float3 Emission = 0;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = Smoothness734;
    float Occlusion = 1;
    float Alpha = AlbedoAlpha263;
    float AlphaClipThreshold = _AlphaCutoff;
    float AlphaClipThresholdShadow = 0.5;
    float3 BakedGI = 0;
    float3 RefractionColor = 1;
    float RefractionIndex = 1;
    float3 Transmission = 1;
    float3 Translucency = 1;

    #ifdef _ALPHATEST_ON
    clip(Alpha - AlphaClipThreshold);
    #endif

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

    //half4 color = UniversalFragmentPBR( inputData, surfaceData );

    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = 1;
    
    half4 color = FragmentPBR(inputData, surfaceData, extendData);

    UBPA_APPLY_FOG(input, color);
    
    return color;
}

#endif