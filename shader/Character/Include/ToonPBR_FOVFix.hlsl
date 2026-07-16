#ifndef TOONPBR_FOVFIX_INCLUDED
#define TOONPBR_FOVFIX_INCLUDED

float3 GetFlattenCameraPositionWS()
{
    return GetCameraPositionWS();
}

float3 GetFlattenCameraForwardWS()
{
    return normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(0.0, 0.0, -1.0)));
}

void ApplyCharacterFOVFixInPlace(inout VertexPositionInputs vertexInput)
{
    float active = step(0.5, _FOV_Parameters.x);
    if (active <= 0)
    {
        return;
    }
    
    float flattenTarget = max(_FOV_Parameters.y, 1);
    float shapeCompensation = saturate(_FOV_Parameters.z);

    float3 worldPosition = vertexInput.positionWS;
    float3 actorPosition = _FOV_PivotWS.xyz;
    float3 cameraPosition = GetFlattenCameraPositionWS();
    float3 cameraForward = GetFlattenCameraForwardWS();

    float3 pixelVector = worldPosition - cameraPosition;
    float3 actorVector = actorPosition - cameraPosition;
    float flattenFactor = saturate(1.0 - rcp(flattenTarget));
    float actorDepth = dot(cameraForward, actorVector);
    float pixelDepth = dot(cameraForward, pixelVector);
    float3 pixelParallel = cameraForward * pixelDepth;
    float3 pixelPerpendicular = pixelVector - pixelParallel;
    
    // 仅压缩相对深度变化。保持相机平面的占据面积不变
    float flattenedDepth = actorDepth + (pixelDepth - actorDepth) * rcp(flattenTarget);
    float compensatedDepth = lerp(flattenedDepth, pixelDepth, shapeCompensation);
    float3 compensatedPosition = cameraPosition + pixelPerpendicular + cameraForward * compensatedDepth;

    worldPosition = lerp(worldPosition, compensatedPosition, flattenFactor * active);

    float3 positionVS = TransformWorldToView(worldPosition);
    
    vertexInput.positionWS = worldPosition;
    vertexInput.positionVS = positionVS;
    vertexInput.positionCS = TransformWViewToHClip(positionVS);
    vertexInput.positionNDC = vertexInput.positionCS * rcp(vertexInput.positionCS.w);
}

VertexPositionInputs GetVertexPositionInputsWithFOVFix(float3 positionOS)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
    ApplyCharacterFOVFixInPlace(vertexInput);
    return vertexInput;
}

float4 TransformObjectToHClipWithFOVFix(float3 positionOS)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
    ApplyCharacterFOVFixInPlace(vertexInput);
    return vertexInput.positionCS;
}

#endif
