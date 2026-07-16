#ifndef __MOUNTAINS_DEPTH_NORMALS__
#define __MOUNTAINS_DEPTH_NORMALS__

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
    
    float4 ase_texcoord : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float4 clipPosV : TEXCOORD0;
    
    float3 worldNormal : TEXCOORD1;
    float4 worldTangent : TEXCOORD2;
    
    float4 ase_texcoord5 : TEXCOORD5;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    output.ase_texcoord5.xy = input.ase_texcoord.xy;
    output.ase_texcoord5.zw = 0;

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

    float3 WorldNormal = input.worldNormal;
    float4 WorldTangent = input.worldTangent;
    
    half4 normalTex = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.ase_texcoord5.xy);
    float3 unpack3 = UnpackNormalScale( normalTex, _NormalScale );
    unpack3.z = lerp( 1, unpack3.z, saturate(_NormalScale) );
    
    float3 Normal = unpack3;

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
    
    return half4(NormalizeNormalPerPixel(normalWS), 0);
}

#endif
