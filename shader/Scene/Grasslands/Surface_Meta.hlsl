#ifndef __SURFACE_META__
#define __SURFACE_META__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    float4 texcoord0 : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 positionWS : TEXCOORD0;
    #endif

    float4 ase_texcoord4 : TEXCOORD4;
    float4 ase_texcoord5 : TEXCOORD5;
    float4 ase_texcoord6 : TEXCOORD6;
    float4 ase_texcoord7 : TEXCOORD7;
    float4 ase_texcoord8 : TEXCOORD8;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.ase_texcoord4.xy = input.texcoord0.xy;

    float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
    output.ase_texcoord5.xyz = ase_worldNormal;

    float3 ase_worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
    output.ase_texcoord6.xyz = ase_worldTangent;

    float ase_vertexTangentSign = input.tangentOS.w * (unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0);
    float3 ase_worldBitangent = cross(ase_worldNormal, ase_worldTangent) * ase_vertexTangentSign;
    output.ase_texcoord7.xyz = ase_worldBitangent;

    output.ase_texcoord8 = input.positionOS;

    output.ase_texcoord4.zw = 0;
    output.ase_texcoord5.w = 0;
    output.ase_texcoord6.w = 0;
    output.ase_texcoord7.w = 0;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    output.positionWS = positionWS;
    #endif

    output.positionCS = MetaVertexPosition(input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST);

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
    float3 WorldPosition = input.positionWS;
    #endif

    float2 temp_cast_0 = _Tiling.xx;
    float2 texCoord472 = input.ase_texcoord4.xy * temp_cast_0;
    float2 Tiling462 = texCoord472;
    half4 tex2DNode2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, Tiling462);
    
    float4 temp_output_3_0 = _Color * tex2DNode2;
    float CoverageTiling463 = _CovTiling;
    float2 temp_cast_1 = CoverageTiling463.xx;
    float3 ase_worldNormal = input.ase_texcoord5.xyz;
    float4 triplanar421 = TriplanarSampling421(TEXTURE2D_ARGS(_CovMainTex, sampler_CovMainTex), WorldPosition, ase_worldNormal, 10, temp_cast_1, 1, 0);

    half4 bumpMapTex = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, Tiling462);
    float3 unpack6 = UnpackNormalScale(bumpMapTex, _BumpScale);
    unpack6.z = lerp(1, unpack6.z, saturate(_BumpScale));
    
    float3 tex2DNode6 = unpack6;
    float3 MainNormalMap454 = tex2DNode6;
    float3 ase_worldTangent = input.ase_texcoord6.xyz;
    float3 ase_worldBitangent = input.ase_texcoord7.xyz;
    float3 tanToWorld0 = float3(ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x);
    float3 tanToWorld1 = float3(ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y);
    float3 tanToWorld2 = float3(ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z);
    
    float3 tanNormal283 = MainNormalMap454;
    float3 worldNormal283 = float3(dot(tanToWorld0, tanNormal283), dot(tanToWorld1, tanNormal283), dot(tanToWorld2, tanNormal283));
    float lerpResult457 = lerp(worldNormal283.y, input.ase_texcoord8.xyz.y, _CovOverlayMethod);
    float temp_output_289_0 = (lerpResult457 + (1.0 - _CovOffset));
    float lerpResult541 = lerp(temp_output_289_0, (1.0 - temp_output_289_0), _CovBalance);
    float2 appendResult550 = (float2(_MaskTilingX, _MaskTilingY));
    
    float2 texCoord545 = input.ase_texcoord4.xy * appendResult550;
    float2 MaskTiling551 = texCoord545;
    half4 tex2DNode507 = SAMPLE_TEXTURE2D(_CovMask, sampler_CovMask, MaskTiling551);
    
    float lerpResult524 = lerp((1.0 - tex2DNode507.g), tex2DNode507.g, _MaskContrast);
    float CoverageMask297 = saturate((lerpResult541 * saturate(lerpResult524)));
    float4 lerpResult302 = lerp(temp_output_3_0, (_CovColor * triplanar421), CoverageMask297);
    #ifdef _COVERAGEON_ON
    float4 staticSwitch304 = lerpResult302;
    #else
    float4 staticSwitch304 = temp_output_3_0;
    #endif
    float4 Albedo19 = staticSwitch304;

    float3 BaseColor = Albedo19.rgb;
    float3 Emission = 0;

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = BaseColor;
    metaInput.Emission = Emission;

    return UnityMetaFragment(metaInput);
}

#endif
