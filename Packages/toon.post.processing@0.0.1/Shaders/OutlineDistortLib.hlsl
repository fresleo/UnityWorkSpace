/*
 * 因为这个功能主要就是为了角色写的，所以这个文件其实就是 ToonPBR_OutlineLib.hlsl 的复制品，因为包引用的限制，才有了它。
 * 我知道写 2 遍这点很 Low，但是现阶段这就是最好的选择了。
 */

#ifndef __OUTLINE_DISTORT_LIB__
#define __OUTLINE_DISTORT_LIB__

#define OUTLINE_ATTRIBUTES \
    float3 normalOS     : NORMAL; \
    float4 tangentOS    : TANGENT; \
    float4 color        : COLOR; \
    
float4 TransformOutlineToHClipScreenSpace(float4 position, float3 normal, float outlineWidth_, half outlinePower_)
{
    float4 positionCS = TransformObjectToHClip(position.xyz);
    float3 normalWS = TransformObjectToWorldNormal(normal);
    
    // TODO 如果先normalize在取xy会导致dx11和gles平台表现不一致，暂未想出是为什么
    float2 clipNormal = normalize(TransformWorldToHClipDir(normalWS).xy);
    float2 projectedNormal = clipNormal.xy * pow(abs(positionCS.w), outlinePower_);
    float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));
    float aspect = abs(nearUpperRight.y / nearUpperRight.x);

    // 适配viewport
    projectedNormal.x *= aspect;
    positionCS.xy += 0.002 * outlineWidth_ * projectedNormal.xy;
    
    return positionCS;
}

float OutlineLerp(float start, float end, float Z_start, float Z_end, float Z)
{
    float t = (Z - Z_start) / max(Z_end - Z_start, 0.001); // linear 
    t = clamp(t, 0.0f, 1.0f);
    return lerp(start, end, t);
}

float4 PositionCS_2_NDC(float4 positionCS)
{
    float4 positionNDC = 0;
    
    // 参考自 GetVertexPositionInputs 里的方法
    float4 ndc = positionCS * 0.5f;
    positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    positionNDC.zw = positionCS.zw;
    
    return positionNDC;
}

void OutlineVertexPhase(
    // in    
    float4 positionOS_, float3 normalOS_, float4 tangentOS_, float4 color_, 
    half outlineWidth_, half outlinePower_,
    half yAxisOffset_, 
    // out
    out VertexPositionInputs _vertexInput, out float3 _normalWS)
{
    _vertexInput = (VertexPositionInputs)0;
    _normalWS = 0;
    
    float3 colorNormalOS = color_.xyz;

    // 蒙皮网格存在顶点色的法线，是切线空间的，使用前需要转换回模型空间
    #if !defined( _MESH_PREVIEW_MODE )
    float sign = tangentOS_.w * GetOddNegativeScale();
    
    float3 tangentOS = normalize(tangentOS_.xyz);
    float3 bitangentOS = cross(normalOS_, tangentOS_.xyz) * sign;
    float3 normalOS = normalize(normalOS_);
    float3x3 TBN = float3x3(tangentOS, bitangentOS, normalOS);
    
    colorNormalOS = mul(colorNormalOS, TBN);
    #endif

    // 扩张顶点位置
    float outlineWidth = color_.a * outlineWidth_;
    _vertexInput.positionCS = TransformOutlineToHClipScreenSpace(positionOS_, colorNormalOS, outlineWidth, outlinePower_);
    
    // 反推其它项
    _vertexInput.positionWS = ComputeWorldSpacePosition(_vertexInput.positionCS, UNITY_MATRIX_I_VP);
    _vertexInput.positionVS = TransformWorldToView(_vertexInput.positionWS);
    _vertexInput.positionNDC = PositionCS_2_NDC(_vertexInput.positionCS);
    
    // 计算视空间偏移
    float4 widthScales = float4(0.105, 0.245, 0.6, 0);
    float4 widthAdj = float4(0.01, 2, 6, 0);

    float fov = 1;
    if (GetViewToHClipMatrix()[3].w) // perspective check
    {
        fov = 0.5; // perspective off
    }
    else
    {
        fov = -2.414 / GetViewToHClipMatrix()[1].y;
    }

    // 平台差异性
    #if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
    fov = -fov;
    #endif

    float depth = -_vertexInput.positionVS.z * fov;
    bool outline_depth = depth < widthAdj.y;

    float4 widthZs = 1;
    widthZs.xy = outline_depth ? widthAdj.xy : widthAdj.yz;
    widthZs.zw = outline_depth ? widthScales.xy : widthScales.yz;

    float offset = OutlineLerp(widthZs.z, widthZs.w, widthZs.x, widthZs.y, depth);
    float outlineOffset = offset * outlineWidth_ * 1.82 * 0.03 * 0.41425 * color_.a / 5;

    // 计算世界空间法线
    _normalWS = TransformObjectToWorldNormal(colorNormalOS, false);
    float3 normalVS = TransformWorldToViewDir(_normalWS, false);
    
    // 应用额外的视图空间偏移
    _vertexInput.positionVS.xy += normalize(float2(normalVS.x, normalVS.y)) * outlineOffset;
    
    // 反推其它项
    _vertexInput.positionCS = TransformWViewToHClip(_vertexInput.positionVS);
    _vertexInput.positionWS = ComputeWorldSpacePosition(_vertexInput.positionCS, UNITY_MATRIX_I_VP);
    _vertexInput.positionNDC = PositionCS_2_NDC(_vertexInput.positionCS);
    
    // 偏移Y轴
    _vertexInput.positionWS = _vertexInput.positionWS + float3(0, yAxisOffset_, 0);
    
    // 反推其它项
    _vertexInput.positionVS = TransformWorldToView(_vertexInput.positionWS);
    _vertexInput.positionCS = TransformWorldToHClip(_vertexInput.positionWS);
    _vertexInput.positionNDC = PositionCS_2_NDC(_vertexInput.positionCS);
}

#endif
