#ifndef __WATER_DEPTH_NORMALS__
#define __WATER_DEPTH_NORMALS__

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
    float4 positionOS   : POSITION;
    
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    
    float4 clipPosV : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float4 worldTangent : TEXCOORD2;
    
    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 positionWS : TEXCOORD3;
    #endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

    float3 normalWS = TransformObjectToWorldNormal( input.normalOS );
    float4 tangentWS = float4( TransformObjectToWorldDir( input.tangentOS.xyz ), input.tangentOS.w );

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    output.positionWS = vertexInput.positionWS;
    #endif

    output.worldNormal = normalWS;
    output.worldTangent = tangentWS;

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 WorldPosition = input.positionWS;
    #endif
    
    float3 WorldNormal = input.worldNormal;
    float4 WorldTangent = input.worldTangent;

    float4 ClipPos = input.clipPosV;
    float4 ScreenPos = ComputeScreenPos( input.clipPosV );
    
    float Time525 = _TimeParameters.x * _WavesSpeed;
    float temp_output_533_0 = ( Time525 * 0.1 );
    
    float2 appendResult106 = (float2(WorldPosition.x , WorldPosition.z));
    float2 WorldSpaceTile68 = ( appendResult106 * _Tiling );
    
    float2 panner112 = ( temp_output_533_0 * float2( 1,1 ) + WorldSpaceTile68);
    half4 wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, panner112);
    float3 unpack38 = UnpackNormalScale( wavesNormalTex, _NormalIntensity );
    unpack38.z = lerp( 1, unpack38.z, saturate(_NormalIntensity) );

    float2 panner114 = ( ( 1.0 - temp_output_533_0 ) * float2( 1,1 ) + WorldSpaceTile68);
    wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, (1.0 - panner114));
    float3 unpack46 = UnpackNormalScale( wavesNormalTex, _NormalIntensity );
    unpack46.z = lerp( 1, unpack46.z, saturate(_NormalIntensity) );

    float3 Normal89 = BlendNormal( unpack38 , unpack46 );
    
    float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
    ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
    float screenDepth390 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
    float distanceDepth390 = saturate( abs( ( screenDepth390 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( ( _CoastalBlending * 0.5 ) ) ) );

    float3 Normal = Normal89;

    #if defined(_NORMALMAP)
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
