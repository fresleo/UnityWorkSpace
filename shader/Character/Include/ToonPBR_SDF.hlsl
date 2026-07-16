#ifndef TOONPBR_SDF
#define TOONPBR_SDF

#define RAD2DEG 57.29578
#define LIGHTING_THRESHOLD_MIN 0.0001f
#define LIGHTING_THRESHOLD_MAX 0.9999f

/*
 *  此处踩坑之处是Unity中编辑器可视化的本地空间坐标系和真正运行时的本地坐标系有可能不一致
 *  
 *  具体体现在：MeshRender是一致的，而SkinnedMeshRender是不一致的
 *
 *  所以需将两种方式分开，具体坐标系可以使用Renderdoc查看
 */

float3 CalculateFaceShadowFactor(float curDeltaAngle, float2 texcoord, half sdfFullLight)
{
    UNITY_BRANCH
    if(curDeltaAngle < 0)
    {
        texcoord.x = 1.0f - texcoord.x;
    }
    
    // 0 is full light, 1 is full dark.
    float lightingThreshold = abs(curDeltaAngle / PI);
    lightingThreshold = clamp(lightingThreshold, LIGHTING_THRESHOLD_MIN, LIGHTING_THRESHOLD_MAX);
    lightingThreshold *= (1.0 - sdfFullLight);

    return float3(texcoord, lightingThreshold);
}

// skinned mesh render
float3 CalculateFaceShadowFactorSkinnedMeshRender(float3 lightDirectionWS, float2 texcoord, half sdfFullLight)
{
    float3 lightDWS = lightDirectionWS;
    if(length(lightDWS) < 0.9)
    {
        lightDWS = float3(0, 1, 0);
    }
    lightDWS = normalize(lightDWS);

    // 使用球面光，解决部分模型会低头的问题
    // lightDWS = normalize(float3(lightDWS.x, 0, lightDWS.z));
    
    float3 lightLocalDir = mul((float3x3)unity_WorldToObject, lightDWS);
    float curDeltaAngle = atan2(lightLocalDir.z, lightLocalDir.y);

    return CalculateFaceShadowFactor(curDeltaAngle, texcoord, sdfFullLight);
}

// mesh render
float3 CalculateFaceShadowFactorMeshRender(float3 lightDirectionWS, float2 texcoord, half sdfFullLight)
{
    float3 lightDWS = lightDirectionWS;
    if(length(lightDWS) < 0.9)
    {
        lightDWS = float3(0, 1, 0);
    }
    lightDWS = normalize(lightDWS);
    
    float3 lightLocalDir = mul((float3x3)unity_WorldToObject, lightDWS);
    float curDeltaAngle = atan2(-lightLocalDir.x, -lightLocalDir.y);

    return CalculateFaceShadowFactor(curDeltaAngle, texcoord, sdfFullLight);
}

// SDF 的 R
half GetToonFaceDiffuseMaskFromR(half sdfR, half shadow1Step, half shadowFeather, half lightingThreshold)
{
    half diffuseThreshold = sdfR - (shadow1Step * 2 - 1);
    half diffuseShadowMask = smoothstep(
        max(lightingThreshold - shadowFeather, LIGHTING_THRESHOLD_MIN),
        min(lightingThreshold + shadowFeather, LIGHTING_THRESHOLD_MAX),
        diffuseThreshold);
    return diffuseShadowMask;
}

// SDF 的 G
half GetNoseSdfSpecular(float3 lightDirectionWS, half sdfFaceU, half noseSdfG)
{
    float3 lightDWS = lightDirectionWS;
    if (length(lightDWS) < 0.0001)
    {
        lightDWS = float3(0, 1, 0);
    }
    lightDWS = normalize(lightDWS);

    float3 lightDOS = mul((float3x3)unity_WorldToObject, lightDWS);
    
    float3 lightDirH = float3(lightDOS.x, 0, lightDOS.z);
    float3 forward = float3(0, 0, 1);
    float3 left = float3(-1, 0, 0);
    
    if (length(lightDirH) < 0.0001)
    {
        lightDirH = forward;
    }
    else
    {
        lightDirH = normalize(lightDirH);
    }
    
    half filpU = saturate(sign(dot(lightDirH, left)));
    half cutU = step(0.5, sdfFaceU);
    half uvMask = lerp(cutU, 1.h - cutU, filpU);

    half lightAtten = abs((dot(lightDirH, forward) * 0.5h + 0.5h) - 0.5h) * 2.h;
    half noseSpecular = step(lightAtten, uvMask * noseSdfG);
    return noseSpecular;
}

#endif // TOONPBR_SDF
