#ifndef __VEGETATION_DEPTH_NORMALS__
#define __VEGETATION_DEPTH_NORMALS__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    float4 color : COLOR;
    float4 uv : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float4 clipPosV : TEXCOORD0;
    
    float3 worldNormal : TEXCOORD1;
    float4 worldTangent : TEXCOORD2;
    
    float4 uv : TEXCOORD3;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    float3 ase_worldPos = TransformObjectToWorld( input.positionOS.xyz );
    
    float mulTime34 = _TimeParameters.x * ( RAYGlobalWindForce * ( _WindForce * 5 ) );
    float simplePerlin3D35 = snoise( ( ase_worldPos + mulTime34 )*( ( 1.0 - _WindWavesScale ) * RAYGlobalWavesScale ) );
    simplePerlin3D35 = simplePerlin3D35 * 0.5 + 0.5;
    float temp_output_231_0 = ( pow( abs(simplePerlin3D35) , ( _WindFlowDensity * RAYGlobalFlowDensity ) ) * 0.01 );
    
    float2 texCoord357 = input.uv.xy;
    float lerpResult1020 = lerp( temp_output_231_0 , ( temp_output_231_0 * pow( abs(texCoord357.y) , 1.5 ) ) , _UVBaseLock);
    float4 transform916 = mul(GetWorldToObjectMatrix(),float4( ( RAYGlobalDirection * ( lerpResult1020 * input.color.r * ( ( _WindForce * 100 ) * RAYGlobalWindForce ) ) ) , 0.0 ));
    #ifdef _ENABLEWIND_ON
    float4 staticSwitch341 = transform916;
    #else
    float4 staticSwitch341 = float4( 0,0,0,0 );
    #endif
    float4 Wind191 = staticSwitch341;
    
    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz));
    float eyeDepth = -objectToViewPos.z;
    float cameraDepthFade958 = (( eyeDepth -_ProjectionParams.y - _GrassFadeDistance ) / 5.0);
    float lerpResult1039 = lerp( ( 1.0 - cameraDepthFade958 ) , cameraDepthFade958 , ( _GrassFalloff * 0.5 ));
    float lerpResult1023 = lerp( 1.0 , saturate( lerpResult1039 ) , _GrassDistanceFadeEnable);
    float GrassDistanceFadeMask982 = lerpResult1023;
    float4 lerpResult1065 = lerp( float4( ( input.positionOS.xyz * -1 ) , 0.0 ) , Wind191 , GrassDistanceFadeMask982);
    
    float3 lerpResult1096 = lerp( input.normalOS , float3(0,1,0) , _LightingFlatness);
    float3 LightingFlatness1101 = lerpResult1096;
    
    output.uv.xy = input.uv.xy;
    output.uv.zw = 0;

    float3 vertexValue = lerpResult1065.xyz;
    input.positionOS.xyz += vertexValue;

    input.normalOS = LightingFlatness1101;

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
    float3 normalWS = TransformObjectToWorldNormal( input.normalOS );
    float4 tangentWS = float4( TransformObjectToWorldDir( input.tangentOS.xyz ), input.tangentOS.w );

    output.worldNormal = normalWS;
    output.worldTangent = tangentWS;

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;
    
    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

    #ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition( input.positionCS.xyz, unity_LODFade.x );
    #endif
    
    float3 WorldNormal = input.worldNormal;
    float4 WorldTangent = input.worldTangent;

    float2 temp_cast_0 = _Tiling.xx;
    float2 Tiling1032 = input.uv.xy * temp_cast_0;
    half4 normalTex = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, Tiling1032);
    float3 unpack886 = UnpackNormalScale( normalTex, _NormalScale );
    unpack886.z = lerp( 1, unpack886.z, saturate(_NormalScale) );
    float3 Normal888 = unpack886;

    half4 albedoTex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, Tiling1032);
    float AlbedoAlpha263 = albedoTex.a;

    float3 Normal = Normal888;
    float Alpha = AlbedoAlpha263;
    float AlphaClipThreshold = _AlphaCutoff;

    #ifdef _ALPHATEST_ON
    clip(Alpha - AlphaClipThreshold);
    #endif

    #if defined( _NORMALMAP )
        #if _NORMAL_DROPOFF_TS
    float crossSign = (WorldTangent.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(WorldNormal.xyz, WorldTangent.xyz);
    float3 normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent.xyz, bitangent, WorldNormal.xyz));
        #elif _NORMAL_DROPOFF_OS
    float3 normalWS = TransformObjectToWorldNormal(Normal);
        #elif _NORMAL_DROPOFF_WS
    float3 normalWS = Normal;
        #endif
    #else
    float3 normalWS = WorldNormal;
    #endif
    
    return half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
