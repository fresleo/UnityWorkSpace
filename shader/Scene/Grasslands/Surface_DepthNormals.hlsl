#ifndef __SURFACE_DEPTH_NORMALS__
#define __SURFACE_DEPTH_NORMALS__

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
    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 positionWS : TEXCOORD3;
    #endif

    float4 ase_texcoord5 : TEXCOORD5;
    float4 ase_texcoord6 : TEXCOORD6;
    float4 ase_texcoord7 : TEXCOORD7;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
    float3 ase_worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
    float ase_vertexTangentSign = input.tangentOS.w * (unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0);
    float3 ase_worldBitangent = cross(ase_worldNormal, ase_worldTangent) * ase_vertexTangentSign;
    output.ase_texcoord6.xyz = ase_worldBitangent;

    output.ase_texcoord5.xy = input.ase_texcoord.xy;
    output.ase_texcoord7 = input.positionOS;

    output.ase_texcoord5.zw = 0;
    output.ase_texcoord6.w = 0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    output.positionWS = vertexInput.positionWS;
    #endif

    output.worldNormal = normalWS;
    output.worldTangent = tangentWS;

    output.positionCS = vertexInput.positionCS;
    output.clipPosV = vertexInput.positionCS;

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(input.positionCS.xyz, unity_LODFade.x);
    #endif

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 WorldPosition = input.positionWS;
    #endif

    float3 WorldNormal = input.worldNormal;
    float4 WorldTangent = input.worldTangent;

    float2 temp_cast_0 = _Tiling.xx;
    float2 texCoord472 = input.ase_texcoord5.xy * temp_cast_0 + float2(0, 0);
    float2 Tiling462 = texCoord472;
    half4 bumpMapTex = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, Tiling462);
    float3 unpack6 = UnpackNormalScale(bumpMapTex, _BumpScale);
    unpack6.z = lerp(1, unpack6.z, saturate(_BumpScale));
    
    float3 tex2DNode6 = unpack6;
    float CoverageTiling463 = _CovTiling;
    float2 temp_cast_1 = CoverageTiling463.xx;
    float3 ase_worldBitangent = input.ase_texcoord6.xyz;
    float3x3 ase_worldToTangent = float3x3(WorldTangent.xyz, ase_worldBitangent, WorldNormal);
    float3 triplanar432 = TriplanarSampling432(TEXTURE2D_ARGS(_CovBumpMap, sampler_CovBumpMap), WorldPosition, WorldNormal, 10, temp_cast_1, _CovBumpScale, 0);
    
    float3 tanTriplanarNormal432 = mul(ase_worldToTangent, triplanar432);
    float3 lerpResult515 = lerp(BlendNormal(tex2DNode6, tanTriplanarNormal432), tanTriplanarNormal432, (1.0 - _NormalBlending));
    float3 MainNormalMap454 = tex2DNode6;
    float3 tanToWorld0 = float3(WorldTangent.xyz.x, ase_worldBitangent.x, WorldNormal.x);
    float3 tanToWorld1 = float3(WorldTangent.xyz.y, ase_worldBitangent.y, WorldNormal.y);
    float3 tanToWorld2 = float3(WorldTangent.xyz.z, ase_worldBitangent.z, WorldNormal.z);
    float3 tanNormal283 = MainNormalMap454;
    float3 worldNormal283 = float3(dot(tanToWorld0, tanNormal283), dot(tanToWorld1, tanNormal283), dot(tanToWorld2, tanNormal283));
    float lerpResult457 = lerp(worldNormal283.y, input.ase_texcoord7.xyz.y, _CovOverlayMethod);
    float temp_output_289_0 = (lerpResult457 + (1.0 - _CovOffset));
    float lerpResult541 = lerp(temp_output_289_0, (1.0 - temp_output_289_0), _CovBalance);
    float2 appendResult550 = (float2(_MaskTilingX, _MaskTilingY));
    float2 texCoord545 = input.ase_texcoord5.xy * appendResult550 + float2(0, 0);
    float2 MaskTiling551 = texCoord545;
    half4 tex2DNode507 = SAMPLE_TEXTURE2D(_CovMask, sampler_CovMask, MaskTiling551);
    
    float lerpResult524 = lerp((1.0 - tex2DNode507.g), tex2DNode507.g, _MaskContrast);
    float CoverageMask297 = saturate((lerpResult541 * saturate(lerpResult524)));
    float3 lerpResult309 = lerp(tex2DNode6, lerpResult515, CoverageMask297);
    #ifdef _COVERAGEON_ON
    float3 staticSwitch308 = lerpResult309;
    #else
    float3 staticSwitch308 = tex2DNode6;
    #endif
    float3 Normal = staticSwitch308;

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
