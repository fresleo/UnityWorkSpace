#ifndef __TESSELLATED_PARALLAX_TESSELLATION_FUNCS__
#define __TESSELLATED_PARALLAX_TESSELLATION_FUNCS__

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors TessellationFunction(InputPatch<VertexControl, 3> v)
{
    TessellationFactors o;
    float4 tf = 1;
    float tessValue = _Tess;
    float tessMin = _MinDistance;
    float tessMax = _MaxDistance;
    float edgeLength = _TessEdgeLength;
    float tessMaxDisp = _TessMaxDisp;

    #if defined(ASE_FIXED_TESSELLATION)
    tf = FixedTess( tessValue );
    #elif defined(ASE_DISTANCE_TESSELLATION)
    tf = DistanceBasedTess(v[0].positionOS, v[1].positionOS, v[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
    #elif defined(ASE_LENGTH_TESSELLATION)
    tf = EdgeLengthBasedTess(v[0].positionOS, v[1].positionOS, v[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
    #elif defined(ASE_LENGTH_CULL_TESSELLATION)
    tf = EdgeLengthBasedTessCull(v[0].positionOS, v[1].positionOS, v[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
    #endif

    o.edge[0] = tf.x;
    o.edge[1] = tf.y;
    o.edge[2] = tf.z;
    o.inside = tf.w;
    return o;
}

// 曲面细分控制函数
[domain("tri")] // 处理的是三角形
[partitioning("fractional_odd")] // 分割模式
[outputtopology("triangle_cw")] // 顺时针的三角形
[patchconstantfunc("TessellationFunction")] // 评估用的常量函数
[outputcontrolpoints(3)] // 3个控制点
VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

// 曲面细分评估函数
[domain("tri")]
Varyings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
{
    Attributes o = (Attributes)0;
    o = PatchToOutput(patch, bary);

    #if defined(ASE_PHONG_TESSELLATION)
    float3 pp[3];
    for (int i = 0; i < 3; ++i)
    {
        pp[i] = o.positionOS.xyz - patch[i].normalOS * (dot(o.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
    }
    
    float phongStrength = _TessPhongStrength;
    o.positionOS.xyz = phongStrength * (pp[0] * barycentricCoordinates.x + pp[1] * barycentricCoordinates.y + pp[2] * barycentricCoordinates.z) + (1.0f - phongStrength) * o.positionOS.xyz;
    #endif

    UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
    return VertexFunction(o);
}

#endif // __TESSELLATED_PARALLAX_TESSELLATION_FUNCS__
