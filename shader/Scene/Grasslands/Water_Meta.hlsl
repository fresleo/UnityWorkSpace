#ifndef __WATER_META__
#define __WATER_META__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

struct VertexInput
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;

    float4 texcoord1    : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS   : SV_POSITION;

    float3 positionWS   : TEXCOORD0;

    float4 positionSS   : TEXCOORD1;
    float4 eyeDepth     : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);

    float4 screenPos = ComputeScreenPos(positionCS);
    output.positionSS = screenPos;

    float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz));
    float eyeDepth = -objectToViewPos.z;
    output.eyeDepth.x = eyeDepth;
    output.eyeDepth.yzw = 0;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionWS = positionWS;

    output.positionCS = MetaVertexPosition(input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST);

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 worldPosition = input.positionWS;

    float4 screenPos = input.positionSS;
    float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos(screenPos);
    float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
    float4 fetchOpaqueVal341 = float4(SHADERGRAPH_SAMPLE_SCENE_COLOR(ase_grabScreenPosNorm.xy), 1.0);
    
    float waveTime = _TimeParameters.x * _WavesSpeed;
    float wt_1 = (waveTime * 0.1);
    
    float2 appendResult = float2(worldPosition.x, worldPosition.z);
    float2 WorldSpaceTile68 = appendResult * _Tiling;
    float2 panner112 = (wt_1 * float2(1, 1) + WorldSpaceTile68);
    
    half4 wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, panner112);
    float3 unpack38 = UnpackNormalScale(wavesNormalTex, _NormalIntensity);
    unpack38.z = lerp(1, unpack38.z, saturate(_NormalIntensity));
    
    float2 panner114 = ((1.0 - wt_1) * float2(1, 1) + WorldSpaceTile68);
    wavesNormalTex = SAMPLE_TEXTURE2D(_WavesNormal, sampler_WavesNormal, (1.0 - panner114));
    float3 unpack46 = UnpackNormalScale(wavesNormalTex, _NormalIntensity);
    unpack46.z = lerp(1, unpack46.z, saturate(_NormalIntensity));
    
    float3 Normal89 = BlendNormal(unpack38, unpack46);
    float4 temp_output_277_0 = (ase_grabScreenPosNorm + float4((Normal89 * (_RefractionFactor * 0.1)), 0.0));
    float4 fetchOpaqueVal274 = float4(SHADERGRAPH_SAMPLE_SCENE_COLOR(temp_output_277_0.xy), 1.0);
    
    float eyeDepth337 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(temp_output_277_0.xy), _ZBufferParams);
    float eyeDepth = input.eyeDepth.x;
    
    float ifLocalVar336 = 0;
    if (eyeDepth337 > eyeDepth)
        ifLocalVar336 = 1.0;
    else if (eyeDepth337 < eyeDepth)
        ifLocalVar336 = 0.0;

    float4 lerpResult342 = lerp(fetchOpaqueVal341, fetchOpaqueVal274, ifLocalVar336);
    float4 Refractions282 = saturate(lerpResult342);
    float4 ase_screenPosNorm = screenPos / screenPos.w;
    ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
    float screenDepth384 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(ase_screenPosNorm.xy), _ZBufferParams);
    float distanceDepth384 = saturate(abs((screenDepth384 - LinearEyeDepth(ase_screenPosNorm.z, _ZBufferParams)) / (_Transparency)));
    float saferPower405 = abs(distanceDepth384);
    float4 lerpResult389 = lerp(Refractions282, _WaterColor, pow(saferPower405, _TransparencyFade));
    
    float wt_2 = waveTime * 5;
    float2 voronoiSmoothId61 = 0;
    float2 coords61 = WorldSpaceTile68 * _FoamTiling;
    float2 id61 = 0;
    float2 uv61 = 0;
    float fade61 = 0.5;
    float voroi61 = 0;
    float rest61 = 0;
    for (int it61 = 0; it61 < 3; it61++)
    {
        voroi61 += fade61 * voronoi61(coords61, wt_2, id61, uv61, 0, voronoiSmoothId61);
        rest61 += fade61;
        coords61 *= 2;
        fade61 *= 0.5;
    } // Voronoi61
    voroi61 /= rest61;
    
    float saferPower557 = abs((1.0 - voroi61));
    float screenDepth17 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(ase_screenPosNorm.xy), _ZBufferParams);
    float distanceDepth17 = abs((screenDepth17 - LinearEyeDepth(ase_screenPosNorm.z, _ZBufferParams)) / ((_FoamDistance * 0.3)));
    
    #ifdef _ENABLEFOAM_ON
    float staticSwitch538 = (saturate((pow(saferPower557, -1.5) + (1.0 - distanceDepth17))) * _FoamOpacity);
    #else
    float staticSwitch538 = 0.0;
    #endif
    float Foam183 = staticSwitch538;
    float4 Color502 = (lerpResult389 + Foam183);

    float screenDepth390 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(ase_screenPosNorm.xy), _ZBufferParams);
    float distanceDepth390 = saturate(abs((screenDepth390 - LinearEyeDepth(ase_screenPosNorm.z, _ZBufferParams)) / ((_CoastalBlending * 0.5))));
    
    float3 BaseColor = Color502.rgb;
    float3 Emission = 0;

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = BaseColor;
    metaInput.Emission = Emission;

    return UnityMetaFragment(metaInput);
}

#endif
