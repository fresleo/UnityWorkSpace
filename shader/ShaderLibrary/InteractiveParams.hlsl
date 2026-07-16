#ifndef VEGETATION_INTERACTIVE_PARAMS
#define VEGETATION_INTERACTIVE_PARAMS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ---------------- Interactive Params -----------------
// xyz: object position w: bend strength
float4 _GlobalBendingObjectA;

// xy  中心位置的世界空间x和z坐标
// z   距离
// w   高度
half4 _GlobalVegetationWaveInfo;

half3 VegetationBendWithObject(float4 objInfo, half3 posWS, half mask)
{
    half distanceValue = distance(posWS, objInfo.xyz);
    half pushDown = saturate(1.5 - distanceValue) * objInfo.w * mask;
    half3 directionWS = normalize(posWS - objInfo.xyz);
    directionWS.y = 0; // Y轴不受影响
    return directionWS * pushDown;
}

half3 VegetationInteractiveWS(half3 posWS, half mask)
{
    half3 motion = VegetationBendWithObject(_GlobalBendingObjectA, posWS, mask);
    return motion;
}

half3 VegetationInteractiveWS(half3 posWS, half mask, half localIntensity)
{
    half distanceValue = distance(posWS, _GlobalBendingObjectA.xyz);
    half pushDown = saturate(2.5 - distanceValue) * _GlobalBendingObjectA.w * mask * localIntensity;
    half3 directionWS = normalize(posWS - _GlobalBendingObjectA.xyz);
    directionWS.y = 0; // Y轴不受影响
    return directionWS * pushDown;
}

//腐蚀变正常时的草浪
half3 VegetationWave(half3 positionWS)
{
    float dist = distance(float3(_GlobalVegetationWaveInfo.x, 0, _GlobalVegetationWaveInfo.y), float3(positionWS.x, 0, positionWS.z));
    half a = clamp(_GlobalVegetationWaveInfo.z - dist, 0, PI);
    return sin(a) * _GlobalVegetationWaveInfo.w;
}

// persective correction，
half3 CorrectPerspective(float3 positionWS, float3 viewDirectionWS, float heightMask )
{
    float upDotView = dot(float3(0, 1, 0), viewDirectionWS);
    return  mul( UNITY_MATRIX_I_V, float4(0,1,0,0) ).xyz * (upDotView * upDotView) * heightMask * saturate( distance(positionWS, _WorldSpaceCameraPos.xyz) );
}

#endif