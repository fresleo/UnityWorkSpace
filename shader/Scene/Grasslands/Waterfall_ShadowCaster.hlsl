#ifndef __WATERFALL_SHADOW_CASTER__
#define __WATERFALL_SHADOW_CASTER__

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
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    float Time98 = _TimeParameters.x;
    float Flow_Speed156 = ( _FlowSpeed * 0.1 );
    
    float2 texCoord109 = input.uv.xy * float2( 1,1 ) + ( float2( 0,1 ) * ( Time98 * ( Flow_Speed156 * 7 ) ) );
    float simplePerlin2D100 = snoise( texCoord109*_VOScale );
    simplePerlin2D100 = simplePerlin2D100*0.5 + 0.5;
    
    float2 texCoord66 = input.uv.xy * float2( 1,1 ) + float2( 0,0 );
    float Gradient74 = saturate( ( ( texCoord66.y + _GradientLevel ) * _GradientFade ) );
    float3 lerpResult105 = lerp( float3( 0,0,0 ) , ( input.normalOS * ( simplePerlin2D100 * _VOIntensity ) ) , Gradient74);
    float3 Vertex_Offset144 = lerpResult105;
    
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
    
    return 0;
}

#endif
