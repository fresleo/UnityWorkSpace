#ifndef __VEGETATION_SHADOW_CASTER__
#define __VEGETATION_SHADOW_CASTER__

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
    float4 color : COLOR;
    
    float4 uv : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float4 clipPosV : TEXCOORD0;
    
    float4 uv : TEXCOORD1;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 _LightDirection;
float3 _LightPosition;

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    float3 ase_worldPos = TransformObjectToWorld( input.positionOS.xyz );
    
    float mulTime34 = _TimeParameters.x * ( RAYGlobalWindForce * ( _WindForce * 5 ) );
    float simplePerlin3D35 = snoise( ( ase_worldPos + mulTime34 ) * ( ( 1.0 - _WindWavesScale ) * RAYGlobalWavesScale ) );
    simplePerlin3D35 = simplePerlin3D35 * 0.5 + 0.5;
    float temp_output_231_0 = ( pow( abs(simplePerlin3D35) , ( _WindFlowDensity * RAYGlobalFlowDensity ) ) * 0.01 );
    
    float2 texCoord357 = input.uv.xy;
    float lerpResult1020 = lerp( temp_output_231_0 , ( temp_output_231_0 * pow( abs(texCoord357.y) , 1.5 ) ) , _UVBaseLock);
    float4 transform916 = mul(GetWorldToObjectMatrix(), float4( ( RAYGlobalDirection * ( lerpResult1020 * input.color.r * ( ( _WindForce * 100 ) * RAYGlobalWindForce ) ) ) , 0.0 ));
    #ifdef _ENABLEWIND_ON
    float4 staticSwitch341 = transform916;
    #else
    float4 staticSwitch341 = float4( 0,0,0,0 );
    #endif
    float4 Wind191 = staticSwitch341;
    
    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld( input.positionOS.xyz ));
    float eyeDepth = -objectToViewPos.z;
    float cameraDepthFade958 = (( eyeDepth -_ProjectionParams.y - _GrassFadeDistance ) / 5.0);
    float lerpResult1039 = lerp( ( 1.0 - cameraDepthFade958 ) , cameraDepthFade958 , ( _GrassFalloff * 0.5 ));
    float lerpResult1023 = lerp( 1.0 , saturate( lerpResult1039 ) , _GrassDistanceFadeEnable);
    float GrassDistanceFadeMask982 = lerpResult1023;
    float4 lerpResult1065 = lerp( float4( ( input.positionOS.xyz * -1 ) , 0.0 ) , Wind191 , GrassDistanceFadeMask982);
    
    float3 lerpResult1096 = lerp( input.normalOS , float3( 0,1,0 ) , _LightingFlatness);
    float3 LightingFlatness1101 = lerpResult1096;
    
    output.uv.xy = input.uv.xy;
    output.uv.zw = 0;

    float3 vertexValue = lerpResult1065.xyz;
    input.positionOS.xyz += vertexValue;
    input.normalOS = LightingFlatness1101;

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

    #ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition( input.positionCS.xyz, unity_LODFade.x );
    #endif
    
    float2 temp_cast_0 = _Tiling.xx;
    float2 texCoord1028 = input.uv.xy * temp_cast_0;
    float2 Tiling1032 = texCoord1028;
    half4 albedoTex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, Tiling1032);
    float AlbedoAlpha263 = albedoTex.a;

    float Alpha = AlbedoAlpha263;
    float AlphaClipThreshold = _AlphaCutoff;
    float AlphaClipThresholdShadow = 0.5;

    #ifdef _ALPHATEST_ON
    clip(Alpha - AlphaClipThreshold);
    #endif
    
    return 0;
}

#endif
