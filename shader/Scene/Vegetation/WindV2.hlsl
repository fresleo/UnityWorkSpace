/*
 *  风场算法来源于Vegetation Engine插件，初步评估性能消耗较高，后期优化思路是拆分叶子一二级枝干
 */

#ifndef WIND_NEW_VERSION_INCLUDED
#define WIND_NEW_VERSION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

uniform TEXTURE2D(TVE_NoiseTex);   SAMPLER(sampler_TVE_NoiseTex);
uniform half4 TVE_MotionParams;
uniform float TVE_NoiseTexTilling;
uniform float TVE_MotionValue_10;
uniform float TVE_MotionValue_20;
uniform float TVE_MotionValue_30;

float2 DecodeFloatToVector2(float enc)
{
    float2 result;
    result.y = enc % 2048;
    result.x = floor(enc / 2048);
    return result / (2048 - 1);
}

half3 GetPivotsOS()
{
    return 0;
}

// half3 GetMeshPivots(half4 uv3)
// {
//     return uv3.xzy;
// }

// DOT Instance的内存分布有变化,但是接口封装之后，结果是统一的
half3 GetObjectPosition()
{
    half3 position = UNITY_MATRIX_M._m03_m13_m23;

    return position;
}

// global mesh variation
half ComputeGlobalVariation(half3 objectPosition, half meshVariation)
{
    float3 positionRandom = sin(dot(objectPosition.xz,  float2(12.99, 78.23)));
    half output = clamp(frac(meshVariation + positionRandom), 0.01, 0.99);

    return output;
}

// motion facing mask
half ComputeMotionFacing(half3 vertexPosition, half3 directionOS, half motionDirectonMask)
{
    half facing = dot(normalize(vertexPosition), half3(-directionOS.x, 0, -directionOS.y)) * 0.5 + 0.5;
    half output = max(lerp(1, facing, motionDirectonMask), 0.001);

    return output;
}

// mesh height
half3 ComputeMeshHeight(half3 vertexPosition, half objectHeight = 1.0h)
{
    return saturate(vertexPosition / objectHeight);
}

// mesh spherical
half ComputeMeshSpherical(half3 vertexPosition, half objectRadius = 1.0h)
{
    half output = saturate(distance(vertexPosition.xyz, half3(0, vertexPosition.y, 0)) / objectRadius);
    
    return output;
}

struct GlobalMotionInfo
{
    float2 globalWindDirectionWS;
    half  globalInteractionMask;
    half2 globalWindDirectionOS;
    half  globalWindPower;
    half2 globalReactDirectionOS;
    half2 globalReactDirectionWS;
};

GlobalMotionInfo ComputeGlobalMotion(half4 globalMotionParams)
{
    GlobalMotionInfo info = (GlobalMotionInfo)0;

    half2 windDirectionWS = globalMotionParams.xy * 2.0h - 1.0h;
    half2 windDirectionOS = TransformWorldToObjectDir(half3(windDirectionWS.x, 0, windDirectionWS.y), false).xz;
    
    half2 reactDirectionWS = windDirectionWS;
    half2 reactDirectionOS = windDirectionOS;

    half windPower = 1.0h - (1.0h - globalMotionParams.z) * (1.0h - globalMotionParams.z);
    half reactMask = globalMotionParams.w * globalMotionParams.w * globalMotionParams.w * globalMotionParams.w;

    info.globalWindDirectionWS = windDirectionWS;
    info.globalInteractionMask = reactMask;
    info.globalWindDirectionOS = windDirectionOS;
    info.globalWindPower = windPower;
    info.globalReactDirectionOS = reactDirectionOS;
    info.globalReactDirectionWS = reactDirectionWS;

    return info;
}

struct MeshDataInfo
{
    half  meshVariation;
    half  meshOcclusion;
    half  meshDetailMask;
    half2 meshDetailCoord;
    half  meshHeight;
    half  meshMotion10;
    half  meshMotion20;
    half  meshMotion30;
    half  boundsHeight;
    half  boundsRadius;
};

MeshDataInfo GetMeshData(half4 vertexColor, half4 uv0, half4 uv1)
{
    MeshDataInfo info = (MeshDataInfo)0;
    
    info.meshVariation = vertexColor.r;
    info.meshOcclusion = vertexColor.g;
    info.meshDetailMask = vertexColor.b;
    info.meshMotion10 = vertexColor.a;
    info.meshHeight = vertexColor.a;

    // 没用
    info.meshDetailCoord = uv1.zw;

    // 这里踩坑：打包之后移动端通道为16位，所以不能大于65535
    float2 resultMotion = DecodeFloatToVector2(uv0.z) * 100;
    
    info.meshMotion20 = resultMotion.x;
    info.meshMotion30 = resultMotion.y;

    float2 resultBounds = DecodeFloatToVector2(uv0.w) * 100;

    info.boundsHeight = resultBounds.x;
    info.boundsRadius = resultBounds.y;

    return info;
}

half3 ComputeMotionBending(half2 globalWindDirectionOS, half2 globalNoiseDirectionOS, half2 globalReactDirectionOS, half globalWindPower,
                         half boundsHeight, half meshMotion10, half motionAmplitude, half intersectionAmplitude, half intersectionUseMask,
                         half globalIntersectionMask,
    out half motion10Intersection)
{
    // TODO 交互相关
    motion10Intersection = 0;
    
    half inputMeshHeight = meshMotion10;
    half inputBoundsHeight = boundsHeight;
    half inputBendingAmplitude = motionAmplitude;
    half inputInteractionAmplitude = intersectionAmplitude;
    half inputInteractionUseMask = intersectionUseMask;
    half inputInteractionGlobalMask = globalIntersectionMask;
    half2 inputWindDirectionOS = globalWindDirectionOS;
    half2 inputReactDirectionOS = globalReactDirectionOS;
    half2 inputNoiseDirectionOS = globalNoiseDirectionOS;
    half inputWindPower = globalWindPower;

    float globalMotionValue = TVE_MotionValue_10;
    
    half finalMotion10Mask = inputMeshHeight * 2.0h;

    half2 finalBending = lerp(inputNoiseDirectionOS, inputWindDirectionOS, inputWindPower * 0.6) * finalMotion10Mask *
                         inputBendingAmplitude * inputWindPower * inputWindPower * globalMotionValue;

    return half3(finalBending.x, 0.0, finalBending.y);
}

half2 ComputeMotionNoise(half3 motionPosition, float2 globalWindDirectionWS, half globalMeshVariation, half motionSpeed, float
    motionScale, half motionVariation,
                        /*out half noiseR, out half noiseG,*/ out half noiseB  , out half noiseA)
{
    float time = (_Time.y * motionSpeed + motionVariation * globalMeshVariation) * 0.03;
    float fracTime = frac(time);

    float lerpVar = abs(fracTime - 0.5) / 0.5; 

    motionScale += 0.2;
    
    float2 uv = motionPosition.xz * motionScale * TVE_NoiseTexTilling * 0.007;
    float2 uv0 = uv + -globalWindDirectionWS * fracTime;
    float2 uv1 = uv + -globalWindDirectionWS * frac(time + 0.5);

    half4 noise0 = SAMPLE_TEXTURE2D_LOD(TVE_NoiseTex, sampler_TVE_NoiseTex, uv0, 0);
    half4 noise1 = SAMPLE_TEXTURE2D_LOD(TVE_NoiseTex, sampler_TVE_NoiseTex, uv1, 0);

    // 世界空间
    half4 noiseComplex = lerp(noise0, noise1, lerpVar);
    // noiseR = noiseComplex.x;
    // noiseG = noiseComplex.y;
    noiseB = noiseComplex.z;
    noiseA = noiseComplex.w;

    // 本地空间
    half2 noisePart = noiseComplex.xy * 2.0h - 1.0h;
    half2 noiseOS = TransformWorldToObjectDir(half3(noisePart.x, 0.0h, noisePart.y), false).xz;

    return noiseOS;
}

half ComputeMotionBranch(half3 worldPositionShifted, half2 globalDirectionOS, half globalNoise, half globalWind, half globalVariation,
                         half meshMotion20, half boundsRadius, half motionAmplitude, half motionSquash, half motionRolling, half motionSpeed,
                         half motionScale, half motionVariation, out half motion20Rolling)
{
    half3 inputPosition = worldPositionShifted;
    half2 inputDirectionOS = globalDirectionOS;
    half  inputMeshMotion20 = meshMotion20;
    half inputBoundsRadius = boundsRadius;
    half inputSquash = motionSquash;
    half inputrolling = motionRolling;
    half inputMotionAmplitude = motionAmplitude;
    half inputMotionSpeed = motionSpeed;
    half inputMotionScale = motionScale * 0.1h;
    half inputMotionVariation = motionVariation;
    half inputGlobalVariation = globalVariation;
    half inputGlobalWind = globalWind;
    half inputGlobalNoise = globalNoise;

    half globalMotionValue = TVE_MotionValue_20;

    half sumPosition = inputPosition.x + inputPosition.y + inputPosition.z;

    half motionSineA = sin(sumPosition * inputMotionScale + inputMotionVariation * inputGlobalVariation + _Time.y * inputMotionSpeed);
    half motionSineB = sin(_Time.y * inputMotionSpeed * 0.6842 + sumPosition * inputMotionScale);
    half motionAmplitudeVar = inputMotionAmplitude * inputGlobalWind * pow(inputGlobalNoise, lerp(1.8, 0.4, inputGlobalWind));

    half squash = (max(motionSineA, motionSineB) * 0.5 + 0.5) * motionAmplitudeVar * inputSquash * inputMeshMotion20 * inputBoundsRadius * globalMotionValue *
                   half3(inputDirectionOS.x, motionSineA * 0.3, inputDirectionOS.y);

    half rolling = motionSineA * motionAmplitudeVar * inputrolling * inputMeshMotion20 * globalMotionValue;

    motion20Rolling = rolling;

    return squash;
}

half3 ComputeMotionFlutter(half3 worldPositionShifted, half globalNoise, half globalWind, half globalVariation, half meshMotion30,
                          half motionAmplitude, half motionSpeed, half motionScale, half motionVariation)
{
    half3 inputPosition = worldPositionShifted;
    half inputGlobalWind = globalWind;
    half inputGlobalNoise = globalNoise;
    half inputGlobalVariaton = globalVariation;
    half inputMeshMotion30 = meshMotion30;
    half inputMotionAmplitude = motionAmplitude;
    half inputMotionSpeed = motionSpeed;
    half inputMotionScale = motionScale;
    half inputMotionVariation = motionVariation;

    half globalMotionValue = TVE_MotionValue_30;

    half flutterAmplitude = inputMotionAmplitude * inputMeshMotion30 * inputGlobalWind * globalMotionValue * pow(inputGlobalNoise, lerp(2.4, 0.6, inputGlobalWind));

    float2 uv = inputPosition.xz * inputMotionScale * 0.03 + inputMotionVariation * inputGlobalVariaton + _Time.y * inputMotionSpeed * 0.02;
    half3 noise = SAMPLE_TEXTURE2D_LOD(TVE_NoiseTex, sampler_TVE_NoiseTex, uv, 0).rgb * 2.0 - 1.0;

    half3 motion30Flutter = noise * flutterAmplitude;

    return motion30Flutter;
}

// TODO 这里可以做基于rt方式的压草，考虑到需要新的rt，所以暂不做支持
float4 GetGlobalMotionParams()
{
    return TVE_MotionParams;
}

// TODO FacingMask, 目前没理解这个功能的作用
half GetMotion30Amplitude(half motionFacingMask)
{
    return _MotionAmplitude_32 * motionFacingMask;
}

half3 ComputeRotationX(half3 positionOS, float angle)
{
    half3 VertexPosRotationAxis = half3(positionOS.x, 0, 0);
    half3 VertexPosOtherAxis = half3(0, positionOS.y, positionOS.z);

    half3 result = VertexPosOtherAxis * cos(angle) + VertexPosRotationAxis + cross(half3(1, 0, 0), VertexPosOtherAxis) * sin(angle);

    return result;
}

half3 ComputeRotationZ(half3 positionOS, float angle)
{
    angle = -angle;

    half3 VertexPosRotationAxis = half3(0, 0, positionOS.z);
    half3 VertexPosOtherAxis = half3(positionOS.x, positionOS.y, 0);

    half3 result = VertexPosRotationAxis + VertexPosOtherAxis * cos(angle) + cross(half3(0, 0, 1), VertexPosOtherAxis) * sin(angle);

    return result;
}


half3 ComputeRotationXZ(half3 positionOS, float3 angle)
{
    half3 computeX = ComputeRotationX(positionOS, angle.z);
    half3 computexz = ComputeRotationZ(computeX, angle.x);

    return computexz;
}

half3 ComputeRotationY(half3 positionOS, float angle)
{
    half3 VertexPosRotationAxis = half3(0, positionOS.y, 0);
    half3 VertexPosOtherAxis = half3(positionOS.x, 0, positionOS.z);

    half3 result = VertexPosRotationAxis + VertexPosOtherAxis * cos(angle) + cross(half3(0, 1, 0), VertexPosOtherAxis) * sin(angle);

    return result;
}

half GetMotionFacingMask(half3 positionOS, half2 reactDirectionalOS, half motionDirectionMask)
{
    half right = dot(normalize(positionOS), half3(reactDirectionalOS.x, 0, reactDirectionalOS.y)) * 0.5h + 0.5;
    half result = max(0.001h, lerp(1.0h, right, motionDirectionMask));

    return result;
}

half ComputeWindFinalVertexPosition(inout float3 positionOS, half3 positionWS, float4 uv0, half4 uv1, half4 vertexColor)
{
    half3 worldPositionShifted = positionWS;

    // 等同于ObjectPosition，因为Shifted是减去世界原点，而世界原点在当前使用场景为0
    half3 objectPositionShifted = GetObjectPosition();
    MeshDataInfo meshDataInfo = GetMeshData(vertexColor, uv0, uv1);

    half3 motion10Position = lerp(worldPositionShifted, objectPositionShifted, _MotionPosition_10);
    half globalMeshVariation = ComputeGlobalVariation(objectPositionShifted, meshDataInfo.meshVariation);

    float4 globalMotionParams = GetGlobalMotionParams();
    GlobalMotionInfo motionInfo = ComputeGlobalMotion(globalMotionParams);

    half noiseA, noiseB;
    half2 globalNoiseOS = ComputeMotionNoise(motion10Position, motionInfo.globalWindDirectionWS, globalMeshVariation,
                                             _MotionSpeed_10, _MotionScale_10, _MotionVariation_10, noiseB, noiseA);

    half motion10Intersection;
    half3 motion10Bending = ComputeMotionBending(motionInfo.globalWindDirectionOS, globalNoiseOS, motionInfo.globalReactDirectionOS, motionInfo.globalWindPower,
                         meshDataInfo.boundsHeight, meshDataInfo.meshMotion10, _MotionAmplitude_10, _InteractionAmplitude,
                         _InteractionMaskValue, motionInfo.globalInteractionMask, motion10Intersection);


    half motionFacingMask = GetMotionFacingMask(positionOS, motionInfo.globalReactDirectionOS, _MotionFacingValue);
    half motion2DAmplitude = motionFacingMask;

    half motion20Rolling = 0;
    half motion20Squash = 0;

#ifndef  _WIND_V2_GRASS_MODE_ON
    motion20Squash = ComputeMotionBranch(worldPositionShifted, motionInfo.globalReactDirectionOS, noiseB, motionInfo.globalWindPower, globalMeshVariation,
                        meshDataInfo.meshMotion20, meshDataInfo.boundsRadius, motion2DAmplitude, _MotionAmplitude_20, _MotionAmplitude_22,
                        _MotionSpeed_20, _MotionScale_20, _MotionVariation_20, motion20Rolling);
#endif
    
    half motion30Amplitude = GetMotion30Amplitude(motionFacingMask);
    half3 motion30Flutter = ComputeMotionFlutter(worldPositionShifted, noiseB, motionInfo.globalWindPower, globalMeshVariation, meshDataInfo.meshMotion30, motion30Amplitude,
                         _MotionSpeed_32, _MotionScale_32, _MotionVariation_32);

    half3 meshPivotsOS = 0;

    // TODO 关闭了交互逻辑
    half3 bendModeResult = ComputeRotationXZ(positionOS - meshPivotsOS, motion10Bending) + motion20Squash;

#ifndef  _WIND_V2_GRASS_MODE_ON
    half3 vertexMotionObject = ComputeRotationY(bendModeResult, motion20Rolling) + motion30Flutter;
#else
    half3 vertexMotionObject = bendModeResult + motion30Flutter;
#endif
    
    half3 grassPersective = 0;

    // TODO 视角偏移
    vertexMotionObject += grassPersective;

    half vertexSize = 1;
    half vertexSizeFade = 1;

    // TODO 顶点大小和fade相关逻辑
    vertexMotionObject *= vertexSize * vertexSizeFade;

    positionOS = vertexMotionObject;

    // highlight
    half hightLight = noiseA * motionInfo.globalWindPower * meshDataInfo.meshHeight;
    return hightLight;
}

#endif // WIND_NEW_VERSION_INCLUDED
