#ifndef SSR_SURFACE
#define SSR_SURFACE

TEXTURE2D(_NoiseTex);

float4 _NoiseTex_TexelSize;

float4 _MaterialData;
#define SMOOTHNESS _MaterialData.x
#define FRESNEL _MaterialData.y
#define FUZZYNESS _MaterialData.z
#define DECAY _MaterialData.w

float4 _SSRSettings;
#define THICKNESS _SSRSettings.x
#define SAMPLES _SSRSettings.y
#define BINARY_SEARCH_ITERATIONS _SSRSettings.z
#define MAX_RAY_LENGTH _SSRSettings.w

float4 _SSRSettings2;
#define JITTER _SSRSettings2.x
#define CONTACT_HARDENING _SSRSettings2.y
#define REFLECTIVITY _SSRSettings2.w

float4 _SSRSettings3;
#define INPUT_SIZE _SSRSettings3.xy
#define GOLDEN_RATIO_ACUM _SSRSettings3.z
#define DEPTH_BIAS _SSRSettings3.w

float3 _SSRSettings5;
#define REFLECTIONS_THRESHOLD _SSRSettings5.y
#define SKYBOX_INTENSITY _SSRSettings5.z

#if SSR_THICKNESS_FINE
#define THICKNESS_FINE _SSRSettings5.x
#else
#define THICKNESS_FINE THICKNESS
#endif
float collision;

struct AttributesSurf
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSSRSurf
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 scrPos : TEXCOORD1;
    float3 positionVS : TEXCOORD2;
    float3 normal : TEXCOORD3;
    #if SSR_SKYBOX
    float3 viewDirWS : TEXCOORD6;
    float3 normalWS : TEXCOORD7;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
float4 SSR_Pass(float2 uv, float3 normalVS, float3 rayStart, float roughness)
{
    //构建反射方向，视图空间下像素位置天然就是viewDir
    float3 viewDirVS = normalize(rayStart);
    float3 rayDir = reflect(viewDirVS, normalVS);
  
    // if ray is toward the camera, early exit (optional)
    //if (rayDir.z < 0) return 0.0.xxxx;
    
    //最大步进长度
    float rayLength = MAX_RAY_LENGTH;
    
    //步进终点
    float3 rayEnd = rayStart + rayDir * rayLength;
    
    //射线裁剪到近平面（防止穿透近平面）
    if (rayEnd.z < _ProjectionParams.y)
    {
        rayLength = (_ProjectionParams.y - rayStart.z) / rayDir.z;
        rayEnd = rayStart + rayDir * rayLength;
    }
    //射线起点从vs->cs
    float4 sposStart = mul(unity_CameraProjection, float4(rayStart, 1.0));
   
    //射线终点从vs->cs
    float4 sposEnd = mul(unity_CameraProjection, float4(rayEnd, 1.0));
  
    //透视缩放
    float k0 = rcp(sposStart.w);
    //透视正确深度
    float q0 = rayStart.z * k0;
    float k1 = rcp(sposEnd.w);
    float q1 = rayEnd.z * k1;
    float4 p = float4(uv, q0, k0);

    // depth clip check
    float sceneDepth = GetLinearDepth(p.xy);
    // float logd = log2(sceneDepth + 1.0);
    // return float4(logd.xxx * 0.2, 1);
    float pz = rayStart.z;

    //TODO:earlyTest关闭，否则与ATAA冲突造成闪烁
    // if (sceneDepth < pz - DEPTH_BIAS)
    // {
    //     #if SSR_SKYBOX
    //     collision = 0;
    //     #endif
    //     return 0;
    // }
    // length in pixels
    float2 uv1 = (sposEnd.xy * rcp(rayEnd.z) + 1.0) * 0.5;
    float2 duv = uv1 - uv;

    float2 duvPixel = abs(duv * INPUT_SIZE);
    float pixelDistance = max(duvPixel.x, duvPixel.y);
    int sampleCount = (int)clamp(pixelDistance, 1, SAMPLES);
    float4 pincr = float4(duv, q1 - q0, k1 - k0) * rcp(sampleCount);

    #if SSR_JITTER
    float jitter = SAMPLE_TEXTURE2D(_NoiseTex, sampler_PointRepeat,
                                    uv * INPUT_SIZE * _NoiseTex_TexelSize.xy + GOLDEN_RATIO_ACUM).a;
    pincr *= 1.0 + jitter * JITTER; // modifying pincr and p gives best results
    p += pincr * (jitter * JITTER);
    #endif
 
    float3 hitp = 0;
    float thicknessReDefine = THICKNESS;
    UNITY_LOOP
    for (int k = 0; k < sampleCount; k++)
    {
        p += pincr;
        if (any(floor(p.xy) != 0)) return 0.0; // exit if out of screen space
        pz = p.z / p.w;
        //float t = (float)k / sampleCount;
        //thicknessReDefine = lerp(0.05, THICKNESS, t);
        float sceneBackDepth, depthDiff;
        #if SSR_BACK_FACES
        GetLinearDepths(p.xy, sceneDepth, sceneBackDepth);
        if (pz >= sceneDepth && pz <= sceneBackDepth) { 
        #else
        sceneDepth = GetLinearDepth(p.xy);
        depthDiff = pz - sceneDepth;
        if (depthDiff > 0 && depthDiff < thicknessReDefine)
        {
            #endif
            float4 origPincr = pincr;
            p -= pincr;
            float reduction = 1.0;
            UNITY_LOOP
            for (int j = 0; j < BINARY_SEARCH_ITERATIONS; j++)
            {
                reduction *= 0.5;
                p += pincr * reduction;
                pz = p.z / p.w;
                sceneDepth = GetLinearDepth(p.xy);
                depthDiff = sceneDepth - pz;
                pincr = sign(depthDiff) * origPincr;
            }

            float hitAccuracy = 1.0 - abs(depthDiff) / thicknessReDefine;
            float candidateCollision = hitAccuracy;
           
            #if SSR_THICKNESS_FINE
            if (candidateCollision > collision) {
                hitp = float3(p.xy, pz);
                collision = candidateCollision;
            }
            if (abs(depthDiff) < THICKNESS_FINE)
                break;

            pincr = origPincr;
            p += pincr;
            #else
            hitp = float3(p.xy, pz);
            collision = candidateCollision;
            break;
            #endif
        }
    }

    if (collision > 0)
    {
        // intersection found

        // reduce collision intensity with distance to reflection point
        float zdist = (hitp.z - rayStart.z) / (0.0001 + rayEnd.z - rayStart.z);
        float rayFade = 1.0 - saturate(zdist);
        collision *= rayFade;

        float reflectionIntensity = (1.0 - roughness);
        reflectionIntensity *= pow(collision, DECAY);
        // compute fresnel
        float fresnel = 1.0 - FRESNEL * abs(dot(normalVS, viewDirVS));
        float reflectionAmount = reflectionIntensity * fresnel;

        // compute blur amount
        float wdist = rayLength * zdist;
        float blurAmount = max(0, wdist - CONTACT_HARDENING) * FUZZYNESS * roughness;

        // return hit pixel
        return float4(hitp.xy, blurAmount + 0.001, reflectionAmount);
    }
    return float4(0, 0, 0, 0);
}


VaryingsSSRSurf VertSSRSurf(AttributesSurf input)
{
    VaryingsSSRSurf output = (VaryingsSSRSurf)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positions.positionCS;
    output.positionVS = positions.positionVS * float3(1, 1, -1);
    output.scrPos = ComputeScreenPos(positions.positionCS);
    output.uv = input.texcoord;

    half3 viewDirWS = GetCameraPositionWS() - positions.positionWS;

    output.normal = TransformWorldToViewDir(normalInput.normalWS) * float3(1, 1, -1);

    #if SSR_SKYBOX
    output.viewDirWS = viewDirWS;
    output.normalWS = normalInput.normalWS;
    #endif

    #if UNITY_REVERSED_Z
    output.positionCS.z += 0.001;
    #else
    output.positionCS.z -= 0.001;
    #endif

    return output;
}

float4 FragSSRSurf(VaryingsSSRSurf input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    input.scrPos.xy /= input.scrPos.w;
    //input.scrPos = SSRStereoTransformScreenSpaceTex(input.scrPos);

    float3 normalWS;
    #if SSR_SKYBOX
    normalWS = input.normalWS;
    #endif

    float3 normalVS = input.normal;


    collision = -1;

    float smoothness = SMOOTHNESS;
    float roughness = 1.0 - max(0, smoothness - REFLECTIONS_THRESHOLD);
    float4 reflection = SSR_Pass(input.scrPos.xy, normalVS, input.positionVS, roughness);
    #if SSR_SKYBOX
    if (collision < 0)
    {
        float3 viewDirWS = normalize(input.viewDirWS);
        float3 reflDir = -reflect(viewDirWS, normalWS);
        reflection.xyz = reflDir;
        float sm = 1.0 - roughness;
        reflection.w = -sm;
    }
    #endif

    return reflection;
}

#endif
