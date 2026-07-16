#if !defined(VFX_CORE_HLSL)
#define VFX_CORE_HLSL

#include "VFXInput.hlsl"
#include "VFXData.hlsl"
#include "NodeLib.hlsl"

float4 SampleAttenMap(float2 mainUV,float attenMaskCDATA)
{
    float2 offsetScale = 0;
    // auto offset
    if(!_VertexWaveAtten_MaskMapOffsetStopOn)
    {
        offsetScale = GET_GLOBAL_TIME.y  * _VertexWaveAtten_MaskMap_ST.zw;
    }
    
    // offset by custom data
    if(_VertexWaveAttenMaskOffsetScale_UseCustomeData2_X)
    {
        offsetScale = attenMaskCDATA;
    }
    
    float4 attenMapUV = float4(mainUV * _VertexWaveAtten_MaskMap_ST.xy + _VertexWaveAtten_MaskMap_ST.zw + offsetScale, 0, 0);
    return tex2Dlod(_VertexWaveAtten_MaskMap,attenMapUV);
}

void ApplyVertexWaveWorldSpace(inout float3 worldPos, float3 normal, float3 vertexColor, float2 mainUV, float attenMaskCDATA, float attenLen)
{
    float2 worldUV = worldPos.xz + _VertexWaveSpeed * lerp(GET_GLOBAL_TIME.xx, 1, _VertexWaveSpeedManual);
    float noise = 0;
    float4 attenMap = 0;

    if(_NoiseUseAttenMaskMap)
    {
        attenMap = SampleAttenMap(mainUV, attenMaskCDATA);
        noise = attenMap.x;
    }
    else
    {
        noise = Unity_GradientNoise(worldUV, _VertexWaveIntensity);
    }

    //判断是否使用外部传入的衰减长度
    float len = _VertexWaveDirAtten_CustomDataWOn > 0 ? attenLen : _VertexWaveDirAtten.w;
    
    //1 vertex color atten
    //2 uniform dir atten
    float3 dir = SafeNormalize(_VertexWaveDirAtten.xyz) * len;
    if(_VertexWaveDirAlongNormalOn)
        dir *= normal;
    
    if(_VertexWaveDirAtten_LocalSpaceOn)
        dir = mul(unity_ObjectToWorld, half4(dir, 1.0f)).xyz;

    float3 vcAtten = _VertexWaveAtten_VertexColor ? vertexColor : 1;
    float3 atten = dir * vcAtten;
    
    //3 normal direction atten
    if(_VertexWaveAtten_NormalAttenOn)
    {
        atten *= saturate(dot(dir, normal));
    }
    
    //4 atten map
    if(_VertexWaveAtten_MaskMapOn)
    {
        if(! _NoiseUseAttenMaskMap)
            attenMap = SampleAttenMap(mainUV, attenMaskCDATA);
        
        atten *= attenMap[_VertexWaveAtten_MaskMapChannel];
    }
    
    worldPos.xyz +=  noise * atten;
}

/**
    return : float4
    xy:offset and scale vertex uv,
    zw:vertex uv
*/
float4 MainTexOffset(float4 uv)
{
    float2 offsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop);
    float2 mainTexOffset = (_MainTex_ST.zw * offsetScale);
    mainTexOffset = lerp(mainTexOffset, uv.zw, _MainTexOffsetUseCustomData_XY); // vertex uv0.zw : particle customData1.xy

    float4 scrollUV = (float4)0;
    scrollUV.xy = uv.xy * _MainTex_ST.xy + mainTexOffset;
    scrollUV.zw = uv.xy;
    return scrollUV;
}

float2 MainTexOffset(float2 uv, float2 customData1XY)
{
    float2 offsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop);
    float2 mainTexOffset = (_MainTex_ST.zw * offsetScale);
    mainTexOffset = lerp(mainTexOffset, customData1XY, _MainTexOffsetUseCustomData_XY); // vertex uv0.zw : particle customData1.xy

    return uv.xy * _MainTex_ST.xy + mainTexOffset;
}

float2 MainTexRotation(float2 uv, float2 center, float angle)
{
    float rotaiton = angle;
    float s, c;
    sincos(rotaiton, s, c);
    float2x2 rtMatrix = float2x2(c, -s, s, c);
    uv = mul(uv - center, rtMatrix) + center;
    
    return uv;
}

void ApplyMainTexGray(inout half4 mainColor)
{
    mainColor.xyz = (mainColor.r * 0.264 + mainColor.g * 0.617 + mainColor.b * 0.149);
}

half4 MainTexGray(half4 mainColor)
{
    mainColor.rgb = (mainColor.r * 0.264 + mainColor.g * 0.617 + mainColor.b * 0.149);
    return mainColor;
}

float4 SampleMainTex(float2 uv, v2f input, float faceId, float2 uvDistorted)
{
    float4 color = _BackFaceOn ? lerp(_BackFaceColor, _Color, faceId) : _Color;

    // 约定：开启clamp时不做自动offset的支持
    uv.x = lerp(uv.x, saturate(uv.x), _MainTexUClamp);
    uv.y = lerp(uv.y, saturate(uv.y), _MainTexVClamp);

    half4 mainTex = tex2D(_MainTex, uv);
    
    #ifdef _MAINTEX_DISPERSION_ON
    {
        uvDistorted = lerp(0.2, uvDistorted, _DistortionMainTextureDispersion);
        float2 uvDispersion = float2(_MainTexHorizontalDispersion, _MainTexVerticalDispersion) * uvDistorted;
        float2 mainTexRA = tex2D(_MainTex, uv + uvDispersion).ra;
        float2 mainTexBA = tex2D(_MainTex, uv - uvDispersion).ba;
        mainTex.r = mainTexRA.x;
        mainTex.b = mainTexBA.x;
        mainTex.a = max(mainTex.a, max(mainTexRA.y, mainTexBA.y));
    }
    #endif

    mainTex.rgb = lerp(mainTex.rgb * _MainTexColorCorrection.r, pow(max(mainTex.rgb, 1e-5), _MainTexColorCorrection.z) * _MainTexColorCorrection.y, _MainTexColorCorrection.w);

    if (_MainTexGray)
    {
        ApplyMainTexGray(mainTex);
    }
    
    if(_MainTexSingleChannelOn)
    {
        mainTex = mainTex[_MainTexChannel];
    }

    // mainTex.xyz = lerp(mainTex.rgb, mainTex[_MainTexChannel], _MainTexSingleChannelOn);
    mainTex.xyz *= lerp(1, mainTex.a * input.color.a * color.a, _MainTexMultiAlpha);
    mainTex *= color * input.color;
    
    return mainTex;
}

//应用颜色混合
void ApplyMainTexBlend(inout float4 mainColor, float2 uv)
{
    // 混合纹理
    #ifdef _MAINTEXBLEND_ON
    {
        half2 uvMainBlend = lerp(uv, float2(mainColor[_MainTexBlendRampChannal], _MainTexBlendRampY), _MainTexBlendSample);
        half3 mainBlendColor = tex2D(_MainTexBlend, uvMainBlend * _MainTexBlend_ST.xy + _MainTexBlend_ST.zw).rgb;
        mainBlendColor = lerp(mainColor.rgb * mainBlendColor, mainBlendColor, _MainTexBlendSample);
        mainColor.rgb = lerp(mainColor.rgb, mainBlendColor, _MainTexBlendIntensity);
    }
    #endif
}

void ApplyMainTexMask(inout float4 mainColor, float2 uv, float2 uvDistorted)
{
    float2 maskTexOffset = _MainTexMaskOffsetStop ? _MainTexMask_ST.zw : _MainTexMask_ST.zw * GET_GLOBAL_TIME.xx;
    maskTexOffset += _DistortionAffectMainMaskTexture ? uvDistorted : float2(0, 0);
    float2 finalMaskUV = uv * _MainTexMask_ST.xy + maskTexOffset;
    finalMaskUV.x = lerp(finalMaskUV.x, saturate(finalMaskUV.x), _MainTexMaskUClamp);
    finalMaskUV.y = lerp(finalMaskUV.y, saturate(finalMaskUV.y), _MainTexMaskVClamp);
    float4 maskTex = tex2D(_MainTexMask, finalMaskUV);
    mainColor.a *= maskTex[_MainTexMaskChannel] * _MainTexMultiFactor;
}

float2 ApplyDistortion(float4 mainUV, float2 distortUV)
{
    half2 noise = tex2D(_DistortionNoiseTex, distortUV).xy;
    float2 maskUV = Unity_TilingAndOffset(mainUV.xy, _DistortionMaskTex_ST);
    float maskTex = tex2D(_DistortionMaskTex, maskUV)[_DistortionMaskChannel];

    float2 offset = noise * 0.2f * _DistortionIntensity * maskTex;
    offset = float2(_DistortionAffectU ? offset.x : 0.0f, _DistortionAffectV ? offset.y : 0.0f);

    return offset;
}

void ApplyDissolve(inout float4 mainColor, float2 dissolveUV, float2 directionalUV, float4 color, float dissolveCDATA, float edgeWidthCDATA)
{
    if(_PixelDissolveOn)
    {
        dissolveUV = abs(dissolveUV - 0.5);
        dissolveUV = round(dissolveUV * _PixelWidth) / max(0.0001, _PixelWidth);
    }

    half4 dissolveTex = tex2D(_DissolveTex, dissolveUV.xy);
    half refDissolve = dissolveTex[_DissolveTexChannel];

    half dissolveDirectionTex = tex2D(_DissolveDirectionTex, directionalUV.xy).x;

    // remap cutoff
    half cutoff = _Cutoff;
    if(_DissolveByVertexColor)
        cutoff =  1 - color.a; // slider or vertex color

    if(_DissolveByCustomData_Z)
        cutoff = 1 - dissolveCDATA; // slider or particle's custom data
    
    cutoff = lerp(-0.15, 1.01, cutoff);

    half dissolve = refDissolve * dissolveDirectionTex - cutoff;
    dissolve = saturate(CheapSmoothStep(_DissolveFadingMin, _DissolveFadingMax, dissolve));

    if(_DissolveClipOn)
    {
        clip(dissolve - 0.01);
    }
    
    half originalAlpha = mainColor.a; // 备份原始的alpha值
    mainColor.a *= dissolve;

    if(_DissolveEdgeOn)
    {
        half edgeWidth = _DissolveEdgeWidthByCustomData_W > 0 ? edgeWidthCDATA : _EdgeWidth;
        // 确定范围 : 0~1 : 外~内
        half edge = saturate(CheapSmoothStep(edgeWidth - 0.1, edgeWidth + 0.1, dissolve));
        
        half3 edgeColor = lerp(_EdgeColor.rgb, _EdgeColor2.rgb, edge);
        half edgeAlpha = lerp(_EdgeColor.a, _EdgeColor2.a, edge);
        
        // 计算边缘区域的强度 : 0~1 : 画面本身~边缘区域
        half edgeStrength = saturate(CheapSmoothStep(0, _EdgeFadeRange, 1 - dissolve));
        mainColor.a *= lerp(1.0, edgeAlpha, edgeStrength);
        edge = edgeStrength;
        
        // 识别黑色的区域
        half edgeColorLum = dot(edgeColor, half3(0.299, 0.587, 0.114));
        bool isBlackEdge = edgeColorLum < 0.01 && originalAlpha > 0.01;
        
        half3 finalEdgeColor;
        if(isBlackEdge)
        {
            // edge 越接近0，颜色越接近图像的本来颜色，反之我们可以假设都是黑色的
            finalEdgeColor = mainColor.xyz * (1.0 - edge);
            // 黑色的地方得提高透明度才能看得见
            half targetAlpha = mainColor.a + originalAlpha * edgeStrength * _BlackEdgeAlphaFactor; // 用 edgeStrength 来保持原始的渐变关系
            mainColor.a = clamp(targetAlpha, 0, 1);
        }
        else
        {
            // 彩色边缘：使用加法混合，让边缘更亮
            finalEdgeColor = (mainColor.xyz * 0.5 + edgeColor) * 1.5;
        }
        
        // 添加贴图边颜色
        half mainColorR = pow(mainColor.x, 5);
        half3 edgeEffectWithTexture = lerp(half3(0,0,0), finalEdgeColor, max(edge, mainColorR));
        mainColor.xyz = _DissolveEdgeWidthTexture > 0 ? edgeEffectWithTexture : lerp(mainColor.xyz, finalEdgeColor, edge);
    }
}

//TODO urp中没有对应的命名，暂时先放到这里，后期整体整理
#define unity_ColorSpaceDouble half4(4.59479380, 4.59479380, 4.59479380, 2.0)

void ApplyOffset(inout float4 mainColor, float2 offsetUV, float2 maskUV)
{
    half3 offsetColor = tex2D(_OffsetTex, offsetUV.xy).rgb * _OffsetTexColorTint.rgb;
    half mask = tex2D(_OffsetMaskTex, maskUV)[_OffsetMaskChannel];
    offsetColor = mainColor.rgb * offsetColor * unity_ColorSpaceDouble.rgb * _OffsetBlendIntensity;
    mainColor.rgb += lerp(0, offsetColor, mask);
}

void ApplyFresnel(inout float4 mainColor, float fresnel, float fresnelAlpha)
{
    fresnel = _FresnelScale * pow(max(fresnel, 0.001), _FresnelPower);
    half4 fresnelNode = fresnel * _FresnelColor;
    mainColor.rgb += fresnelNode.rgb;
    mainColor.a *= fresnelAlpha;
}

void ApplyFresnelRevert(inout float4 mainColor, float fresnel)
{
    fresnel = _FresnelScale * pow(max(fresnel, 0.001) , _FresnelPower);
    fresnel = abs(1.0f - fresnel);
    fresnel = pow(fresnel, _RimTransparencyIntensity);
    mainColor.a *= fresnel;
}

void ApplyFresnelConditionWithVertexColor(inout float4 mainColor, float4 vertexColor)
{
    mainColor.xyz =lerp(mainColor.xyz,vertexColor.xyz,vertexColor.w);
}

void ApplyFresnelAlpha(inout float4 mainColor, float fresnel)
{
    fresnel = _FresnelScale * pow(max(fresnel, 0.001) , _FresnelPower);
    fresnel = pow(fresnel, _RimTransparencyIntensity);
    mainColor.a *= fresnel;
}

void ApplyViewAngleFade(inout float angleFadeFactor, float fresnel, float viewAngleFade)
{
    float fadeFactor = max(fresnel, 0.001);
    angleFadeFactor = lerp(1.0, 1.0f - fadeFactor, viewAngleFade);
}

void ApplySoftParticle(inout float4 mainColor, float4 projPos)
{
    float sceneZ = LinearEyeDepth(SampleSceneDepth(projPos.xy / projPos.w), _ZBufferParams);
    float delta = abs(sceneZ - projPos.z);
    float fade = saturate(_DepthFadingWidth * delta + 0.12 * delta);
    mainColor.a *= fade;
}

// 视差贴图偏移计算函数 (性能消耗Max，移动端慎用)
#ifdef _LIGHTON_ON
float2 CalculateParallaxOffset(sampler2D heightMap, float2 uvs, float3 viewDirTS, float parallax, float cosViewAngle, float refPlane = 0, int minSamples = 32, int maxSamples = 32)
{
    //原始计算，需要最大（maxSamples）最小值（minSamples），通过cosViewAngle插值，计算如下 注释部分：
    //但如果从外部shader参数赋值min=8、16、32 max=8、16、32 则shader会报错，暂时强制最大最小为同值，减少一部分无效计算优先
    int numSteps = (int)lerp(maxSamples, minSamples, cosViewAngle);
    float layerHeight = 1.0 / numSteps;
    float2 plane = parallax * (viewDirTS.xy / viewDirTS.z);
    float2 deltaTex = -plane * layerHeight;
    float2 prevTexOffset = 0;
    float prevRayZ = 1.0;
    float prevHeight = 0.0;
    float2 currTexOffset = deltaTex;
    float currRayZ = 1.0 - layerHeight;
    float currHeight = 0.0;
    float intersection = 0;
    float2 finalTexOffset = 0;

    int stepIndex = 0;
    while (stepIndex < numSteps + 1)
    {
        currHeight = tex2Dgrad(heightMap, uvs + currTexOffset, ddx(uvs), ddy(uvs)).r; // 注意这里直接使用ddx(uvs)和ddy(uvs)
        if (currHeight > currRayZ)
        {
            stepIndex = numSteps + 1;
        }
        else
        {
            stepIndex++;
            prevTexOffset = currTexOffset;
            prevRayZ = currRayZ;
            prevHeight = currHeight;
            currTexOffset += deltaTex;
            currRayZ -= layerHeight;
        }
    }

    int sectionSteps = 8;
    int sectionIndex = 0;
    float newZ = 0;
    float newHeight = 0;
    while (sectionIndex < sectionSteps)
    {
        intersection = (prevHeight - prevRayZ) / (prevHeight - currHeight + currRayZ - prevRayZ);
        finalTexOffset = prevTexOffset + intersection * deltaTex;
        newZ = prevRayZ - intersection * layerHeight;
        newHeight = tex2Dgrad(heightMap, uvs + finalTexOffset, ddx(uvs), ddy(uvs)).r;// 注意这里直接使用ddx(uvs)和ddy(uvs)
        if (newHeight > newZ)
        {
            currTexOffset = finalTexOffset;
            currHeight = newHeight;
            currRayZ = newZ;
            deltaTex = intersection * deltaTex;
            layerHeight = intersection * layerHeight;
        }
        else
        {
            prevTexOffset = finalTexOffset;
            prevHeight = newHeight;
            prevRayZ = newZ;
            deltaTex = (1 - intersection) * deltaTex;
            layerHeight = (1 - intersection) * layerHeight;
        }
        sectionIndex++;
    }

    uvs += finalTexOffset; // 应用最终偏移
    return uvs;  // 返回最终uv
}
#endif

// Lambert
#ifdef _LIGHTON_ON
void ApplyLightWithNormalMap(inout float4 mainColor, v2f i)
{
    float2 bumpMapOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _BumpMapOffsetStop);
    float2 bumpMapOffset = (_BumpMap_ST.zw * bumpMapOffsetScale);
    
    half2 normalUV = i.uv.xy * _BumpMap_ST.xy + bumpMapOffset;
    
    half3 tangentNormal = UnpackNormalScale(tex2D(_BumpMap, normalUV), _BumpScale);
    half3 worldNormal = TangentToWorld(i.tSpace0, i.tSpace1, i.tSpace2, tangentNormal);
    
    float3 lightDir = lerp(_MainLightPosition.xyz, _CustomMainLightDirection.xyz, _CustomMainLightDirectionOn);
    lightDir = normalize(lightDir);
    
    float nl = saturate(dot(worldNormal, lightDir));

    half3 mainLightColor = _CustomMainLightColorOn ? _CustomMainLightColor.rgb : _MainLightColor.rgb;
    mainColor.xyz *= (mainLightColor * nl);
}
#endif

#ifdef _DECALEFFECTON_ON
void ApplyParticleDecal(inout float mainColoralpha, inout float4 mainUV, v2f i)
{
    // Decal 计算
    float4 screenPosNorm = i.grabPos / i.grabPos.w;
    screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
    float2 screenUV = screenPosNorm.xy;
    float rawDepth = SampleSceneDepth(screenUV);

    // 重建世界坐标和计算 UV
    float3 reconstructedWorldPos = ReconstructWorldPos(screenUV, rawDepth);
    float3 localPos = (reconstructedWorldPos - i.center.xyz) / (i.size.xyz * _DecalScale);
    float3 angles = radians(_DecalRotation.xyz);
    float3 rotatedPos = RotateAroundXYZ(localPos, angles);
    
    // RotateWithMatrix PC友好，安卓消耗大
    // float3 rotatedPos = RotateWithMatrix(localPos, angles);
    
    float2 decalUV = _DecalKnifeEdgeEffectOn > 0 ? rotatedPos.yz + 0.5 : rotatedPos.xz + 0.5;
    mainUV.xy = decalUV;
    mainUV.zw = decalUV;

    // Decal边界框
    float3 boundingBox = step(-0.5, rotatedPos) * (1.0 - step(0.5, rotatedPos));
    float validArea = boundingBox.x * boundingBox.y * boundingBox.z;
    mainColoralpha = saturate(mainColoralpha * validArea);
}
#endif

#endif