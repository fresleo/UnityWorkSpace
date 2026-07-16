#ifndef __FAKE_INTERIOR_WINDOW_META_PASS__
#define __FAKE_INTERIOR_WINDOW_META_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    
    float2 uv : TEXCOORD0;
    float2 uv3 : TEXCOORD2;
    float2 uv4 : TEXCOORD3;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;

    float3 normalWS : TEXCOORD0;
    float4 tangentWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;

    float4 texCoord : TEXCOORD3;
    float4 texCoord3 : TEXCOORD4;
    float4 texCoord4 : TEXCOORD5;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;

    output.normalWS = normalInput.normalWS;

    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    output.positionWS = vertexInput.positionWS;

    output.texCoord.xy = input.uv;
    output.texCoord3.xy = input.uv3;
    output.texCoord4.xy = input.uv4;

    return output;
}

struct SurfaceDescriptionInputs
{
    float3 WorldSpaceNormal;
    float3 WorldSpaceTangent;
    float3 WorldSpaceBiTangent;
    float3 WorldSpaceViewDirection;
    float3 TangentSpaceViewDirection;

    float4 uv;
    float4 uv3;
    float4 uv4;
};

struct SurfaceDescription
{
    float3 BaseColor;
    float3 Emission;
};

SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output = (SurfaceDescriptionInputs)0;

    // 用来做归1化的系数
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
    
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;

    // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    output.WorldSpaceBiTangent = renormFactor * bitang;

    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
    
    float3x3 tangentSpaceTransform = float3x3(output.WorldSpaceTangent, output.WorldSpaceBiTangent, output.WorldSpaceNormal);
    output.TangentSpaceViewDirection = mul(tangentSpaceTransform, output.WorldSpaceViewDirection);

    output.uv = input.texCoord;
    output.uv3 = input.texCoord3;
    output.uv4 = input.texCoord4;
    
    return output;
}

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
    SurfaceDescription surface = (SurfaceDescription)0;
    
    float3 viewDirInv = float3(1, 1, 1) / IN.TangentSpaceViewDirection;
    float3 viewDirAbs = abs(viewDirInv);
    
    float2 roomUV = IN.uv.xy * _Room_Tiling + float2(0, 0);
    float2 roomUVScaled = roomUV * float2(2, 2) - float2(1, 1);

    float roomDepthScaled = _Room_Depth * 5 + 1;
    float roomDepthOffset = roomDepthScaled - 0.2;
    
    half breakMaskR = 1;
    #ifdef _BREAK_MASK_ON
    half4 breakMaskSample = SAMPLE_TEXTURE2D(_Break_Mask, sampler_Break_Mask, IN.uv.xy * _Break_Mask_ST.xy + _Break_Mask_ST.zw);
    breakMaskR = breakMaskSample.r;
    #endif // _BREAK_MASK_ON

    float glassBreakScaled = floor(_Glass_Break * 50) * 0.02;
    float breakStep = step(breakMaskR, glassBreakScaled);
    
    float roomDepthLerp = lerp(roomDepthOffset, roomDepthScaled, breakStep);
    float3 combinedUV = float3(roomUVScaled[0], roomUVScaled[1], roomDepthLerp);
    float3 viewDirCombined = viewDirInv * combinedUV;
    
    float3 viewDirDiff = viewDirAbs - viewDirCombined;
    float minViewDirDiff = min(min(viewDirDiff[0], viewDirDiff[1]), viewDirDiff[2]);
    
    float3 viewDirScaled = IN.TangentSpaceViewDirection * minViewDirDiff.xxx;
    float3 viewDirFinal = viewDirScaled * roomDepthScaled.xxx;
    
    float glassThicknessScaled = _Glass_Thickness * -0.0035;
    
    Bindings_Parallax bpInput;
    bpInput.TangentSpaceViewDirection = IN.TangentSpaceViewDirection;
    bpInput.uv = IN.uv;
    
    float crackMapSample1 = 1;
    float crackMapSample2 = 1;
    float crackMapSample3 = 1;

    float2 parallaxMapUV = 0;
    #ifdef _CRACK_MASK_ON
    parallaxMapUV = SampleParallaxMapUV(glassThicknessScaled, bpInput);
    crackMapSample1 = SAMPLE_TEXTURE2D(_Crack_Mask, sampler_Crack_Mask, parallaxMapUV).a;

    parallaxMapUV = SampleParallaxMapUV(glassThicknessScaled * 2, bpInput);
    crackMapSample2 = SAMPLE_TEXTURE2D(_Crack_Mask, sampler_Crack_Mask, parallaxMapUV).a;

    parallaxMapUV = SampleParallaxMapUV(glassThicknessScaled * 3, bpInput);
    crackMapSample3 = SAMPLE_TEXTURE2D(_Crack_Mask, sampler_Crack_Mask, parallaxMapUV).a;
    #endif // _CRACK_MASK_ON
    
    float crackMap1Inv = 1 - crackMapSample1;
    float crackMap2Inv = 1 - crackMapSample2;
    float crackMap3Inv = 1 - crackMapSample3;
    float crackMapTotal = crackMap1Inv + crackMap2Inv + crackMap3Inv;
    float crackMapTotalScaled = crackMapTotal * 2.5;
    
    float crackMapPower = pow(crackMapTotalScaled, 1.5);
    float crackMapClamped = clamp(crackMapPower, 0, 1);
    
    half breakMaskSample2 = 1;
    #ifdef _BREAK_MASK_ON
    parallaxMapUV = SampleParallaxMapUV(glassThicknessScaled * 4, bpInput);
    breakMaskSample2 = SAMPLE_TEXTURE2D(_Break_Mask, sampler_Break_Mask, parallaxMapUV).r;
    #endif // _BREAK_MASK_ON
    
    float breakStep2 = step(breakMaskSample2, glassBreakScaled);
    float breakStep2Inv = 1 - breakStep2;
    float breakStep2Scaled = crackMapClamped * breakStep2Inv;
    float3 viewDirLerp = lerp(viewDirScaled, viewDirFinal, breakStep2Scaled.xxx);
    
    float roomDepthScaled2 = roomDepthScaled * 2;
    float3 viewDirFinalScaled = viewDirScaled * roomDepthScaled2.xxx;
    
    // 裂缝细节噪声
    float crackNoise = 0;
    #ifdef _CRACK_MASK_ON
    crackNoise = SAMPLE_TEXTURE2D(_Crack_Mask, sampler_Crack_Mask, IN.uv.xy * _Crack_Mask_ST.xy + _Crack_Mask_ST.zw).b;
    #endif // _CRACK_MASK_ON
    
    float noiseIntensityScaled = _Noise_Intensity * 2;
    float noiseMapScaled = crackNoise * noiseIntensityScaled;
    
    float breakStepInv = 1 - breakStep;
    float noiseMapFinal = noiseMapScaled * breakStepInv;
    float3 viewDirLerpFinal = lerp(viewDirLerp, viewDirFinalScaled, noiseMapFinal.xxx);
    
    float3 roomOffset = float3(_Room_Offset[0], _Room_Offset[1], 0);
    float3 viewDirOffset = combinedUV + roomOffset;
    float3 viewDirOffsetScaled = viewDirLerpFinal + viewDirOffset;
    float3 viewDirOffsetFinal = viewDirOffsetScaled * float3(-1, 1, 1);
    
    // _Cubemap
    float4 cubemapSample = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, viewDirOffsetFinal, 0);
    
    float curtainDepthScaled = _Curtain_Depth * -1;
    float curtainDepthOffset = curtainDepthScaled + 0.05;
    float curtainDepthInv = 1 - curtainDepthOffset;
    
    float curtainDepthFinal = curtainDepthInv;
    float2 viewDirXY = float2(IN.TangentSpaceViewDirection[0], IN.TangentSpaceViewDirection[1]);
    float2 viewDirXYDiv = viewDirXY / (IN.TangentSpaceViewDirection[2]).xx;
    
    float glassThicknessOffset = -0.1;
    float glassThicknessOffsetFinal = glassThicknessOffset + 0.015;
    float glassThicknessLerp = lerp(glassThicknessOffsetFinal, glassThicknessOffset, breakStep);
    
    float glassThicknessOffset2 = -0.2;
    float glassThicknessOffsetFinal2 = glassThicknessOffset2 + 0.015;
    float glassThicknessLerp2 = lerp(glassThicknessOffsetFinal2, glassThicknessOffset2, breakStep);
    
    float noiseMapFinalScaled = breakStepInv * breakStep2Scaled;
    float glassThicknessLerpFinal = lerp(glassThicknessLerp, glassThicknessLerp2, noiseMapFinalScaled);
    
    float glassThicknessOffset3 = -0.5;
    float glassThicknessOffsetFinal3 = glassThicknessOffset3 + 0.015;
    float glassThicknessLerp3 = lerp(glassThicknessOffsetFinal3, glassThicknessOffset3, breakStep);
    
    float glassThicknessLerpFinal2 = lerp(glassThicknessLerpFinal, glassThicknessLerp3, noiseMapFinal);
    
    float curtainDepthFinal2 = curtainDepthOffset + glassThicknessLerpFinal2;
    float2 viewDirXYScaled = viewDirXYDiv * curtainDepthFinal2.xx;
    float2 viewDirUV = IN.uv4.xy * curtainDepthFinal.xx + viewDirXYScaled;
    float2 viewDirUVReverse = viewDirUV * (-1).xx;
    
    float curtainDepthInv2 = 1 - curtainDepthFinal;
    float curtainDepthScaled2 = curtainDepthInv2 * 0.5;
    float curtainDepthInv3 = 1 - curtainDepthScaled2;
    
    float2 viewDirUVFinal = viewDirUVReverse + curtainDepthInv3.xx;
    float viewDirUVFinalX = viewDirUVFinal[0];
    float viewDirUVFinalY = viewDirUVFinal[1];
    
    float curtainTilingX = _Curtain_Tiling[0];
    float curtainTilingY = _Curtain_Tiling[1];
    
    float curtainDepthScaled3 = viewDirUVFinalX * curtainTilingX;
    float curtainDepthScaled4 = curtainTilingY * -1;
    float curtainDepthOffset2 = curtainDepthScaled3 + curtainDepthScaled4;
    
    float curtainDepthComp = viewDirUVFinalX > 0.5 ? 1 : 0;
    float curtainDepthBranch = curtainDepthComp ? -1 : 1;
    float curtainDepthScaled5 = curtainTilingY * curtainDepthBranch;
    float curtainDepthOffset3 = curtainDepthOffset2 + curtainDepthScaled5;
    float2 viewDirUVBranch = float2(curtainDepthOffset3, viewDirUVFinalY);
    viewDirUVBranch = float2(viewDirUVBranch.x, 1.0 - viewDirUVBranch.y); // 窗帘的 y 需要翻转一下

    // 窗帘纹理
    float2 curtainUV = viewDirUVBranch * _Curtain_Texture_ST.xy + _Curtain_Texture_ST.zw;
    float4 curtainTextureSample = SAMPLE_TEXTURE2D(_Curtain_Texture, sampler_Curtain_Texture, curtainUV);
    float curtainTextureA = curtainTextureSample.a;
    
    float4 curtainBlend;
    Blend_Overlay(curtainTextureSample, _Curtain_Color, curtainBlend, 1);
    
    float4 curtainLerp = lerp(cubemapSample, curtainBlend, (curtainTextureA).xxxx);
    
    float4 thicknessColorScaled = breakStep2Scaled.xxxx * _Thickness_Color;
    float4 thicknessColorFinal = thicknessColorScaled * (0.3).xxxx;
    float crackMap1Scaled = crackMap1Inv * 0.3;
    float crackMap2Scaled = crackMap2Inv * 0.7;
    float crackMapSumFinal = crackMap1Scaled + crackMap2Scaled;
    
    float crackMapSumFinal2 = crackMapSumFinal + crackMap3Inv;
    float4 thicknessLerp = lerp(thicknessColorFinal, _Thickness_Color, crackMapSumFinal2.xxxx);
    float crackMapSumFinal3 = crackMapSumFinal2 * breakStep2Inv;
    float crackMapClamped2 = clamp(crackMapSumFinal3, 0, 1);
    
    float crackMapFinal = crackMapClamped2 * _Thickness_Color.w;
    float4 finalLerp = lerp(curtainLerp, thicknessLerp, crackMapFinal.xxxx);
    
    float breakStepFinal = step(breakMaskR, glassBreakScaled);
    float breakStepInvFinal = 1 - breakStepFinal;
    float breakStepScaledFinal = breakStepInvFinal * 0.25;
    float4 glassLerp = lerp(finalLerp, _Glass_Color, breakStepScaledFinal.xxxx);

    // 裂缝高度
    half crackHeight = 1;
    #ifdef _CRACK_MASK_ON
    crackHeight = SAMPLE_TEXTURE2D(_Crack_Mask, sampler_Crack_Mask, IN.uv.xy * _Crack_Mask_ST.xy + _Crack_Mask_ST.zw).a;
    #endif // _CRACK_MASK_ON
    
    float crackHeightRInv = 1 - crackHeight;
    float crackHeightScaledFinal = breakStepInvFinal * crackHeightRInv;
    float crackHeightFinalScaled = crackHeightScaledFinal * _Crack_Color.w;
    float4 crackLerp = lerp(glassLerp, _Crack_Color, crackHeightFinalScaled.xxxx);
    
    float curtainDepthScaledFinal = _Curtain_Depth * 8;
    float curtainDepthClamped = clamp(curtainDepthScaledFinal, 0, 1);
    float curtainDepthLerp = lerp(0.5, 1, curtainDepthClamped);
    float4 curtainDepthFinalScaled = crackLerp * curtainDepthLerp.xxxx;
    
    half breakMaskRFinal = 1;
    #ifdef _BREAK_MASK_ON
    float2 shadowOffsetScaled = _Shadow_Offset * (0.01).xx;
    float2 shadowOffsetFinal = viewDirXYScaled + shadowOffsetScaled;
    float2 shadowUV = IN.uv.xy * float2(0.95, 0.95) + shadowOffsetFinal;
    
    half4 breakMaskSampleFinal = SAMPLE_TEXTURE2D(_Break_Mask, sampler_Break_Mask, shadowUV * _Break_Mask_ST.xy + _Break_Mask_ST.zw);
    breakMaskRFinal = breakMaskSampleFinal.r;
    #endif // _BREAK_MASK_ON
    
    float breakStepFinal2 = step(breakMaskRFinal, glassBreakScaled);
    float breakStepInvFinal2 = 1 - breakStepFinal2;
    float breakStepInvFinal3 = 1 - breakStep;
    float breakStepDiff = breakStepInvFinal2 - breakStepInvFinal3;
    float breakStepScaledFinal2 = breakStepDiff * curtainTextureA;
    float breakStepClamped = clamp(breakStepScaledFinal2, 0, 1);
    float4 finalLerp2 = lerp(crackLerp, curtainDepthFinalScaled, breakStepClamped.xxxx);
    
    float4 breakNoiseColor = _Break_Noise_Color;
    #ifdef UNITY_COLORSPACE_GAMMA
    breakNoiseColor = LinearToSRGB(_Break_Noise_Color);
    #endif
    
    float noiseMapFinalScaled2 = noiseMapScaled * breakStepInv;
    float noiseMapFinalScaled3 = noiseMapFinalScaled2 * breakNoiseColor.a;
    float4 finalLerp3 = lerp(finalLerp2, breakNoiseColor, noiseMapFinalScaled3.xxxx);

    // 灰尘遮罩
    float dustTextureR = 0;
    #ifdef _DUST_ON
    dustTextureR = SAMPLE_TEXTURE2D(_Dust_Texture, sampler_Dust_Texture, IN.uv.xy * _Dust_Texture_ST.xy + _Dust_Texture_ST.zw).r;
    #endif // _DUST_ON
    
    float dustIntensityScaled = dustTextureR * _Dust_Intensity;
    float dustIntensityFinal = dustIntensityScaled * breakStepInvFinal;
    float dustIntensityClamped = clamp(dustIntensityFinal, 0, 1);
    float4 finalLerp4 = lerp(finalLerp3, _Dust_Color, dustIntensityClamped.xxxx);
    
    float emissionScaled = _Emission * 0.5;
    float4 emissionFinal = finalLerp4 * emissionScaled.xxxx;
    
    surface.BaseColor = finalLerp4.xyz;
    surface.Emission = emissionFinal.xyz;
    
    return surface;
}

half4 frag(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(input);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = surfaceDescription.BaseColor;
    metaInput.Emission = surfaceDescription.Emission;

    half4 color = UnityMetaFragment(metaInput);
    
    float3 bakerAO = SampleBakerAO(input.texCoord3);
    color.rgb *= bakerAO.rgb;
    
    return color;
}

#endif // __FAKE_INTERIOR_WINDOW_META_PASS__
