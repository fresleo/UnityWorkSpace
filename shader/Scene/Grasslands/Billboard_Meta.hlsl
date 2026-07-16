#ifndef __BILLBOARD_META__
#define __BILLBOARD_META__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    
    float4 texcoord0 : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;

    float4 uv : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // 计算广告牌的顶点和法线
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

    output.positionCS = MetaVertexPosition( input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );

    output.uv.xy = input.texcoord0.xy;
    output.uv.zw = 0;

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    half4 mainTex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, input.uv.xy);

    half3 BaseColor = ( mainTex * _Color ).rgb;
    half3 Emission = 0;
    
    float Alpha = mainTex.a;
    float AlphaClipThreshold = _OpacityCutoff;
    #ifdef _ALPHATEST_ON
    clip(Alpha - AlphaClipThreshold);
    #endif

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = BaseColor;
    metaInput.Emission = Emission;
    
    return UnityMetaFragment(metaInput);
}

#endif
