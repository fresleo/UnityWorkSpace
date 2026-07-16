#ifndef TOONPBR_MOTION_VECTORS_OUTLINE_INCLUDED
#define TOONPBR_MOTION_VECTORS_OUTLINE_INCLUDED

// -------------------------------------
// 包含
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#include "./ToonPBR_Dissolve.hlsl"
#include "./ToonPBR_OutlineLib.hlsl"

#ifndef HAVE_VFX_MODIFICATION
#pragma multi_compile _ DOTS_INSTANCING_ON
    #if UNITY_PLATFORM_ANDROID || UNITY_PLATFORM_WEBGL || UNITY_PLATFORM_UWP
#pragma target 3.5 DOTS_INSTANCING_ON
    #else
#pragma target 4.5 DOTS_INSTANCING_ON
    #endif
#endif // HAVE_VFX_MODIFICATION

// -------------------------------------
// 结构
struct Attributes
{
    float4 positionOS                   : POSITION;
    float3 positionOld                  : TEXCOORD4;

    OUTLINE_ATTRIBUTES

    float2 uv                           : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS                   : SV_POSITION;
    float4 positionCSNoJitter           : TEXCOORD0;
    float4 previousPositionCSNoJitter   : TEXCOORD1;

    TOONPBR_DISSOLVE_FACTOR(2)
    float4 positionSS                   : TEXCOORD3;

    float2 uv                           : TEXCOORD4;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// -------------------------------------
// 顶点阶段
Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    float3 normalV3 = DecodeOctOutlineNormal(input.outlineNormalOct);
    float4 normalV4 = float4(normalV3, input.color.a);
    
    VertexPositionInputs vertexInput;
    output.positionCS = OutlineVertexPhase(
        input.positionOS, _OutlineWidth, _OutlinePower,
        vertexInput,
        input.normalOS, input.tangentOS, normalV4, _MiOutline);

    output.uv = input.uv;
    
    // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
    #ifdef UNITY_REVERSED_Z
    output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
    #else
    output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
    #endif

    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));

    const float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.positionOS;
    output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, vertexInput.positionWS)

    return output;
}

#ifdef SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER
// Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
// since uv remap functions use floats
#define POS_NDC_TYPE float2
#else
#define POS_NDC_TYPE half2
#endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

// -------------------------------------
// 片元阶段
half4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        return half4(0.0, 0.0, 0.0, 1.0);
    }

    // Calculate positions
    float4 posCS = input.positionCSNoJitter;
    float4 prevPosCS = input.previousPositionCSNoJitter;

    POS_NDC_TYPE posNDC = posCS.xy * rcp(posCS.w);
    POS_NDC_TYPE prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);
    
    half2 velocity;
    #ifdef SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        // Convert velocity from NDC space (-1..1) to screen UV 0..1 space since FoveatedRendering remap needs that range.
        half2 posUV = RemapFoveatedRenderingResolve(posNDC * 0.5 + 0.5);
        half2 prevPosUV = RemapFoveatedRenderingPrevFrameResolve(prevPosNDC * 0.5 + 0.5);

        // Calculate forward velocity
        velocity = (posUV - prevPosUV);
        
        #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
        #endif
    }
    else
    #endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER
    {
        // Calculate forward velocity
        velocity = (posNDC.xy - prevPosNDC.xy);
        #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
        #endif

        // Convert velocity from NDC space (-1..1) to UV 0..1 space
        // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
        // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
        velocity.xy *= 0.5;
    }
    
    return half4(velocity, 0.0, 1.0);
}

#endif // TOONPBR_MOTION_VECTORS_OUTLINE_INCLUDED
