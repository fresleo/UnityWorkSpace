#ifndef __BILLBOARD_DEPTH_NORMALS__
#define __BILLBOARD_DEPTH_NORMALS__

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
    
    float4 uv : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    
    float4 clipPosV : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float4 worldTangent : TEXCOORD2;
    
    float4 uv : TEXCOORD5;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

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
    
    output.uv.xy = input.uv.xy;
    output.uv.zw = 0;

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

    half4 normalTex = SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, input.uv.xy);
    float3 unpack2 = UnpackNormalScale( normalTex, _Normal );
    unpack2.z = lerp( 1, unpack2.z, saturate(_Normal) );
    
    float4 tex2DNode1 = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, input.uv.xy);

    float3 Normal = unpack2;
    float Alpha = tex2DNode1.a;
    float AlphaClipThreshold = _OpacityCutoff;

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
