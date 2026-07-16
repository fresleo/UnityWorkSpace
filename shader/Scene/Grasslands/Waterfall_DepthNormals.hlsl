#ifndef __WATERFALL_DEPTH_NORMALS__
#define __WATERFALL_DEPTH_NORMALS__

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

    float Time98 = _TimeParameters.x;
    float Flow_Speed156 = ( _FlowSpeed * 0.1 );
    float2 texCoord109 = input.uv.xy + ( float2( 0,1 ) * ( Time98 * ( Flow_Speed156 * 7 ) ) );
    float simplePerlin2D100 = snoise( texCoord109 * _VOScale );
    simplePerlin2D100 = simplePerlin2D100 * 0.5 + 0.5;
    
    float2 texCoord66 = input.uv.xy;
    float Gradient74 = saturate( ( texCoord66.y + _GradientLevel ) * _GradientFade );
    float3 lerpResult105 = lerp( float3( 0,0,0 ) , ( input.normalOS * ( simplePerlin2D100 * _VOIntensity ) ) , Gradient74);
    float3 Vertex_Offset144 = lerpResult105;
    
    output.uv.xy = input.uv.xy;
    output.uv.zw = 0;

    float3 vertexValue = Vertex_Offset144;
    input.positionOS.xyz += vertexValue;

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

    float2 appendResult165 = float2(_NormalTilingX , _NormalTilingY);
    float Flow_Speed156 = ( _FlowSpeed * 0.1 );
    float Time98 = _TimeParameters.x;
    float2 texCoord158 = input.uv.xy * appendResult165 + ( float2( 0,1 ) * Flow_Speed156 * ( Time98 * 1.3 ) );

    half4 normalMapTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoord158);
    float3 unpack5 = UnpackNormalScale( normalMapTex, _NormalScale );
    unpack5.z = lerp( 1, unpack5.z, saturate(_NormalScale) );
    
    float3 Normal = unpack5;

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
