#ifndef __BILLBOARD_SHADOW_CASTER__
#define __BILLBOARD_SHADOW_CASTER__

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
    float4 uv : TEXCOORD3;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 _LightDirection;
float3 _LightPosition;

VertexOutput vert(VertexInput input)
{
    VertexOutput output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

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

    float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
    float3 normalWS = TransformObjectToWorldDir( input.normalOS );

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    output.positionCS = positionCS;
    output.clipPosV = positionCS;

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

    half4 tex2DNode1 = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, input.uv.xy);
    
    float Alpha = tex2DNode1.a;
    float AlphaClipThreshold = _OpacityCutoff;
    float AlphaClipThresholdShadow = 0.5;

    #ifdef _ALPHATEST_ON
        #ifdef _ALPHATEST_SHADOW_ON
    clip(Alpha - AlphaClipThresholdShadow);
        #else
    clip(Alpha - AlphaClipThreshold);
        #endif
    #endif
    
    return 0;
}

#endif
