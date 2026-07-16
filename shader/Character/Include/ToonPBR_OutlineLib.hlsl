#ifndef TOONPBR_OUTLINE_LIB_INCLUDED
#define TOONPBR_OUTLINE_LIB_INCLUDED
#include "./Include/ToonPBR_Core.hlsl"
#define OUTLINE_ATTRIBUTES \
    float3 normalOS         : NORMAL; \
    float4 tangentOS        : TANGENT; \
    float4 color            : COLOR; \
    float2 outlineNormalOct : TEXCOORD2;

// mesh.uv3.xy：八面体编码的平滑法线
float3 DecodeOctOutlineNormal(float2 octUv)
{
    float2 f = octUv * 2.0 - 1.0;
    float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));
    if (n.z < 0.0)
    {
        n.xy = (1.0 - abs(n.yx)) * sign(n.xy);
    }
    return normalize(n);
}

float4 TransformOutlineToHClipScreenSpaceFromBaseClip(float4 positionCS, float3 normalOS_, float outlineWidth_, half outlinePower_)
{
    float3 normalWS = TransformObjectToWorldNormal(normalOS_);

    // TODO 如果先normalize在取xy会导致dx11和gles平台表现不一致，暂未想出是为什么
    float2 clipNormal = normalize(TransformWorldToHClipDir(normalWS).xy);
    float2 projectedNormal = clipNormal.xy * pow(abs(positionCS.w), outlinePower_);
    float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));
    float aspect = abs(nearUpperRight.y / nearUpperRight.x);

    // 适配 viewport
    projectedNormal.x *= aspect;
    positionCS.xy += 0.002 * outlineWidth_ * projectedNormal.xy;

    return positionCS;
}

float4 TransformOutlineToHClipScreenSpace(float4 position, float3 normal, float outlineWidth_, half outlinePower_)
{
    float4 positionCS = TransformObjectToHClip(position.xyz);
    return TransformOutlineToHClipScreenSpaceFromBaseClip(positionCS, normal, outlineWidth_, outlinePower_);
}

float OutlineLerp(float start, float end, float Z_start, float Z_end, float Z)
{
    float t = (Z - Z_start) / max(Z_end - Z_start, 0.001); // linear 
    t = clamp(t, 0.0f, 1.0f);
    return lerp(start, end, t);
}


float4 GetOutlineColor(float4 outlineColor,float3 positionWS)
{
    
    float localFactor0 = ComputeLocalFactor(_Local_RGWorldToLocal_0, positionWS);
    float localFactor1 = ComputeLocalFactor(_Local_RGWorldToLocal_0, positionWS);
    float localFactor = max(localFactor0, localFactor1);
    return lerp(outlineColor,_OutlineLocalColor,localFactor);
}

float4 OutlineVertexPhase(
    float4 positionOS_, half outlineWidth_, half outlinePower_,
    out VertexPositionInputs vertexInput_,
    float3 normalOS_, float4 tangentOS_, float4 inputArgs_, half miHoYo)
{
    float4 positionCS = 0;
    float3 inputNormalOS = inputArgs_.xyz;

    // 蒙皮网格：uv3 平滑法线为切线空间，使用前转回模型空间
    #if !defined( _MESH_PREVIEW_MODE )
    float sign = tangentOS_.w * GetOddNegativeScale();
    
    float3 tangentOS = normalize(tangentOS_.xyz);
    float3 bitangentOS = cross(normalOS_, tangentOS_.xyz) * sign;
    float3 normalOS = normalize(normalOS_);
    float3x3 TBN = float3x3(tangentOS, bitangentOS, normalOS);
    
    inputNormalOS = mul(inputNormalOS, TBN);
    #endif // _MESH_PREVIEW_MODE

    float outlineWidth = inputArgs_.a * outlineWidth_;

    vertexInput_ = GetVertexPositionInputs(positionOS_.xyz);
    ApplyCharacterFOVFixInPlace(vertexInput_);
    
    positionCS = TransformOutlineToHClipScreenSpaceFromBaseClip(vertexInput_.positionCS, inputNormalOS, outlineWidth, outlinePower_);
    
    float4 widthScales = float4(0.105, 0.245, 0.6, 0);
    float4 widthAdj = float4(0.01, 2.0, 6.0, 0);

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

    float depth = -vertexInput_.positionVS.z * fov;
    bool outline_depth = depth < widthAdj.y;

    float4 widthZs = 1;
    widthZs.xy = outline_depth ? widthAdj.xy : widthAdj.yz;
    widthZs.zw = outline_depth ? widthScales.xy : widthScales.yz;

    float offset = OutlineLerp(widthZs.z, widthZs.w, widthZs.x, widthZs.y, depth);
    float outlineOffset = offset * outlineWidth_ * 1.82 * 0.03 * 0.41425 * inputArgs_.a / 5.0;

    float3 normalWS = TransformObjectToWorldNormal(inputNormalOS, false);
    float3 normalVS = TransformWorldToViewDir(normalWS, false);
    vertexInput_.positionVS.xy += normalize(float3(normalVS.x, normalVS.y, 0.01f)).xy * outlineOffset;

    if (miHoYo)
    {
        positionCS = TransformWViewToHClip(vertexInput_.positionVS);
    }

    return positionCS;
}

#endif // TOONPBR_OUTLINE_LIB_INCLUDED
