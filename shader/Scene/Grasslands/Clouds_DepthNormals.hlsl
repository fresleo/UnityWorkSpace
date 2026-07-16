#ifndef __CLOUDS_DEPTH_NORMALS__
#define __CLOUDS_DEPTH_NORMALS__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 color : COLOR;
    
    float4 texcoord : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float4 color : COLOR;
    
    float4 clipPosV : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 uv : TEXCOORD2;
    
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
    
    output.uv.xy = input.texcoord.xy;
    output.uv.zw = 0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;
    output.normalWS = TransformObjectToWorldNormal( input.normalOS );

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    float3 normalWS = input.normalWS;

    return half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
