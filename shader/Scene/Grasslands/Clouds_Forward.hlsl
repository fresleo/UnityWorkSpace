#ifndef __CLOUDS_FORWARD__
#define __CLOUDS_FORWARD__

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

struct VertexInput
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 color        : COLOR;
    
    float4 uv           : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS   : SV_POSITION;
    float4 color        : COLOR;
    
    float4 clipPosV     : TEXCOORD0;
    float4 uv           : TEXCOORD1;

    float3 positionWS   : TEXCOORD2;
    UBPA_FOG_COORDS(3)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    output.color = input.color;
    
    output.uv.xy = input.uv.xy;
    output.uv.zw = 0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

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

    float4 ScreenPos = ComputeScreenPos( input.clipPosV );
    float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
    ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;

    half4 tex2DNode2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
    
    float screenDepth6 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
    float distanceDepth6 = saturate( abs( ( screenDepth6 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _ParticleSoftness ) ) );
    float lerpResult8 = lerp( 0.0 , ( tex2DNode2.a * _Color.a * input.color.a ) , distanceDepth6);

    half4 color = 1;
    color.rgb = input.color.rgb * tex2DNode2.rgb * _Color.rgb;
    color.a = lerpResult8;
    
    UBPA_APPLY_FOG(input, color);
    
    return color;
}

#endif
