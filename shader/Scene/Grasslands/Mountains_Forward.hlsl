#ifndef __MOUNTAINS_FORWARD__
#define __MOUNTAINS_FORWARD__

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
    
    float4 ase_texcoord8            : TEXCOORD7;
    float4 ase_texcoord9            : TEXCOORD8;

    float3 positionWS			    : TEXCOORD9;
    UBPA_FOG_COORDS(10)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    output.ase_texcoord8.xy = input.texcoord.xy;
    output.ase_texcoord8.zw = 0;
    
    output.ase_texcoord9 = input.positionOS;

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
    VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

    output.tSpace0 = float4( normalInput.normalWS, vertexInput.positionWS.x );
    output.tSpace1 = float4( normalInput.tangentWS, vertexInput.positionWS.y );
    output.tSpace2 = float4( normalInput.bitangentWS, vertexInput.positionWS.z );

    #if defined(LIGHTMAP_ON)
    OUTPUT_LIGHTMAP_UV( input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy );
    #endif

    #if !defined(LIGHTMAP_ON)
    OUTPUT_SH( normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz );
    #endif

    half fogFactor = 0;
    half3 vertexLight = VertexLighting( vertexInput.positionWS, normalInput.normalWS );
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    output.shadowCoord = GetShadowCoord( vertexInput );
    #endif

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;

    output.positionWS = vertexInput.positionWS;
    UBPA_TRANSFER_FOG(output, output.positionWS);
    
    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

    float3 WorldNormal = normalize( input.tSpace0.xyz );
    float3 WorldTangent = input.tSpace1.xyz;
    float3 WorldBiTangent = input.tSpace2.xyz;

    float3 WorldPosition = float3(input.tSpace0.w, input.tSpace1.w, input.tSpace2.w);
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
    float4 ShadowCoords = float4( 0, 0, 0, 0 );

    float4 ScreenPos = ComputeScreenPos( input.clipPosV );

    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( input.positionCS );

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    ShadowCoords = input.shadowCoord;
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
    #endif

    WorldViewDirection = SafeNormalize( WorldViewDirection );
    
    half4 albedoTex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.ase_texcoord8.xy);
    half4 normalTex = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.ase_texcoord8.xy);
    
    float3 unpack3 = UnpackNormalScale( normalTex, _NormalScale );
    unpack3.z = lerp( 1, unpack3.z, saturate(_NormalScale) );

    #ifdef _ENABLEFOG_ON
    float4 staticSwitch35 = ( _FogColor * saturate( ( ( ( ( 1.0 - input.ase_texcoord9.xyz.y ) + _Height ) * 0.1 ) * ( _Density * 0.1 ) ) ) );
    #else
    float4 staticSwitch35 = float4( 0,0,0,0 );
    #endif

    float3 BaseColor = ( albedoTex * _Color ).rgb;
    float3 Normal = unpack3;
    float3 Emission = staticSwitch35.rgb;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = _Smoothness;
    float Occlusion = 1;
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
