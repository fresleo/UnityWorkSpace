#ifndef __WATERFALL_FORWARD__
#define __WATERFALL_FORWARD__

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
    
    float4 unAndEyeDepth            : TEXCOORD7;

    float3 positionWS			    : TEXCOORD8;
    UBPA_FOG_COORDS(9)
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    float Time98 = _TimeParameters.x;
    float Flow_Speed156 = ( _FlowSpeed * 0.1 );
    float2 texCoord109 = input.texcoord.xy + ( float2( 0,1 ) * ( Time98 * ( Flow_Speed156 * 7 ) ) );
    float simplePerlin2D100 = snoise( texCoord109 * _VOScale );
    simplePerlin2D100 = simplePerlin2D100*0.5 + 0.5;
    
    float2 texCoord66 = input.texcoord.xy;
    float Gradient74 = saturate( ( ( texCoord66.y + _GradientLevel ) * _GradientFade ) );
    float3 lerpResult105 = lerp( float3( 0,0,0 ) , ( input.normalOS * ( simplePerlin2D100 * _VOIntensity ) ) , Gradient74);
    float3 Vertex_Offset144 = lerpResult105;

    output.unAndEyeDepth.xy = input.texcoord.xy;
    
    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz));
    float eyeDepth = -objectToViewPos.z;
    output.unAndEyeDepth.z = eyeDepth;
    
    output.unAndEyeDepth.w = 0;

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
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

    float3 WorldNormal = normalize( input.tSpace0.xyz );
    float3 WorldTangent = input.tSpace1.xyz;
    float3 WorldBiTangent = input.tSpace2.xyz;

    float3 WorldPosition = float3( input.tSpace0.w, input.tSpace1.w, input.tSpace2.w );
    float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
    float4 ShadowCoords = float4( 0, 0, 0, 0 );

    float4 ClipPos = input.clipPosV;
    float4 ScreenPos = ComputeScreenPos( input.clipPosV );

    float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( input.positionCS );
    
    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    ShadowCoords = input.shadowCoord;
    #elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
    ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
    #endif
    
    WorldViewDirection = SafeNormalize( WorldViewDirection );

    float3 temp_cast_0 = (0.0).xxx;
    
    float2 appendResult165 = float2(_NormalTilingX, _NormalTilingY);
    float Flow_Speed156 = ( _FlowSpeed * 0.1 );
    float Time98 = _TimeParameters.x;
    float2 texCoord158 = input.unAndEyeDepth.xy * appendResult165 + ( float2( 0,1 ) * Flow_Speed156 * ( Time98 * 1.3 ) );
    half4 normalMapTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoord158);
    float3 unpack5 = UnpackNormalScale( normalMapTex, _NormalScale );
    unpack5.z = lerp( 1, unpack5.z, saturate(_NormalScale) );
    float3 Normal148 = unpack5;

    float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
    float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
    float4 fetchOpaqueVal126 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ase_grabScreenPosNorm.xy ), 1.0 );
    float4 temp_output_121_0 = ( ase_grabScreenPosNorm + float4( ( Normal148 * ( _RefractionFactor * 0.1 ) ) , 0.0 ) );
    float4 fetchOpaqueVal131 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( temp_output_121_0.xy ), 1.0 );
    float eyeDepth123 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( temp_output_121_0.xy ),_ZBufferParams);
    
    float eyeDepth = input.unAndEyeDepth.z;
    float ifLocalVar125 = 0;
    if( eyeDepth123 > eyeDepth )
        ifLocalVar125 = 1.0;
    else if( eyeDepth123 < eyeDepth )
        ifLocalVar125 = 0.0;
    
    float4 lerpResult127 = lerp( fetchOpaqueVal126 , fetchOpaqueVal131 , ifLocalVar125);
    float4 Refractions132 = saturate( lerpResult127 );
    float4 lerpResult137 = lerp( _MainColor , Refractions132 , ( 1.0 - _MainColor.a ));
    float2 appendResult59 = (float2(_FoamTilingX , _FoamTilingY));
    float2 texCoord21 = input.unAndEyeDepth.xy * appendResult59 + ( ( float2( 0,1 ) * Flow_Speed156 ) * Time98 );
    float2 uv17 = 0;
    float3 unityVoronoy17 = UnityVoronoi(texCoord21,( Time98 * _FoamVoronoiSpeed ),15.0,uv17);
    float saferPower45 = abs( unityVoronoy17.x );
    
    float2 texCoord2 = input.unAndEyeDepth.xy * float2( 1,1 ) + float2( 0,0 );
    float lerpResult53 = lerp( 0.0 , saturate( (pow( saferPower45 , 3.0 )*_FoamScale + ( 1.0 - _FoamOffset )) ) , ( ( texCoord2.y + _FoamLevel ) * _FoamFade ));
    float Foam54 = saturate( lerpResult53 );
    
    float2 texCoord66 = input.unAndEyeDepth.xy * float2( 1,1 ) + float2( 0,0 );
    float Gradient74 = saturate( ( ( texCoord66.y + _GradientLevel ) * _GradientFade ) );
    float4 lerpResult77 = lerp( ( _FoamColor * Foam54 ) , ( _GradientColor * Gradient74 ) , Gradient74);
    float4 Color8 = ( lerpResult137 + lerpResult77 );

    float Smoothness146 = ( saturate( ( 1.0 - ( Foam54 + Gradient74 ) ) ) * _Smoothness );

    float2 texCoord170 = input.unAndEyeDepth.xy * float2( 1,1 ) + float2( 0,0 );
    #ifdef _OPACITYENABLE_ON
    float staticSwitch180 = saturate( ( ( texCoord170.y + _OpacityLevel ) * _OpacityFade ) );
    #else
    float staticSwitch180 = 1.0;
    #endif
    float Opacity177 = staticSwitch180;

    float3 BaseColor = temp_cast_0;
    float3 Normal = Normal148;
    float3 Emission = Color8.rgb;
    float3 Specular = 0.5;
    float Metallic = 0;
    float Smoothness = Smoothness146;
    float Occlusion = 1;
    float Alpha = Opacity177;
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
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
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
