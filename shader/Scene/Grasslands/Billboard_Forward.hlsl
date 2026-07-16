#ifndef __BILLBOARD_FORWARD__
#define __BILLBOARD_FORWARD__

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

    float3 positionWS			    : TEXCOORD7;
    UBPA_FOG_COORDS(8)
    
    float4 uv                       : TEXCOORD9;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    // 计算新的广告牌顶点位置和法线
    float3 upCamVec = float3( 0, 1, 0 );
    float3 forwardCamVec = -normalize ( UNITY_MATRIX_V._m20_m21_m22 );
    float3 rightCamVec = normalize( UNITY_MATRIX_V._m00_m01_m02 );
    float4x4 rotationCamMatrix = float4x4( rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1 );
    
    input.normalOS = normalize( mul( float4( input.normalOS , 0 ), rotationCamMatrix )).xyz;
    input.tangentOS.xyz = normalize( mul( float4( input.tangentOS.xyz , 0 ), rotationCamMatrix )).xyz;
    
    input.positionOS.x *= length( GetObjectToWorldMatrix()._m00_m10_m20 );
    input.positionOS.y *= length( GetObjectToWorldMatrix()._m01_m11_m21 );
    input.positionOS.z *= length( GetObjectToWorldMatrix()._m02_m12_m22 );
    input.positionOS = mul( input.positionOS, rotationCamMatrix );
    input.positionOS = mul( GetWorldToObjectMatrix(), float4( input.positionOS.xyz, 0 ) );
    
    output.uv.xy = input.texcoord.xy;
    output.uv.zw = 0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
    VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

    output.tSpace0 = float4( normalInput.normalWS, vertexInput.positionWS.x );
    output.tSpace1 = float4( normalInput.tangentWS, vertexInput.positionWS.y );
    output.tSpace2 = float4( normalInput.bitangentWS, vertexInput.positionWS.z );

    #if defined( LIGHTMAP_ON )
    OUTPUT_LIGHTMAP_UV( input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy );
    #endif

    #if !defined( LIGHTMAP_ON )
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
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 WorldNormal = normalize( input.tSpace0.xyz );
    float3 WorldTangent = input.tSpace1.xyz;
    float3 WorldBiTangent = input.tSpace2.xyz;

    float3 WorldPosition = float3(input.tSpace0.w, input.tSpace1.w, input.tSpace2.w);
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
    float4 ShadowCoords = float4( 0, 0, 0, 0 );

    float4 ClipPos = input.clipPosV;
    float4 ScreenPos = ComputeScreenPos( input.clipPosV );
    
    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    ShadowCoords = input.shadowCoord;
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
    #endif

    WorldViewDirection = SafeNormalize( WorldViewDirection );
    
    half4 tex2DNode1 = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, input.uv.xy);
    
    half4 normalTex = SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, input.uv.xy);
    float3 unpack2 = UnpackNormalScale( normalTex, _Normal );
    unpack2.z = lerp( 1, unpack2.z, saturate(_Normal) );

    float3 BaseColor = ( tex2DNode1 * _Color ).rgb;
    float3 Normal = unpack2;
    float3 Emission = 0;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = _Smoothness;
    float Occlusion = 1;
    float Alpha = tex2DNode1.a;
    float AlphaClipThreshold = _OpacityCutoff;
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

    //half4 color = UniversalFragmentPBR( inputData, surfaceData);

    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = 1;
    
    half4 color = FragmentPBR(inputData, surfaceData, extendData);

    UBPA_APPLY_FOG(input, color);
    
    return color;
}

#endif
