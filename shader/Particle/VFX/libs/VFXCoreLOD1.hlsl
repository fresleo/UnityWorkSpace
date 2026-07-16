#if !defined(VFX_CORE_LOD1)
#define VFX_CORE_LOD1

#include "VFXInputLOD1.hlsl"
#include "UtilLib.hlsl"
#include "VFXDataLOD1.hlsl"
#include "NodeLib.hlsl"

/**
    return : float4
    xy:offset and scale vertex uv,
    zw:vertex uv
*/
float4 MainTexOffset(float4 uv)
{
    float2 offsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop);
    float2 mainTexOffset = _MainTex_ST.zw * offsetScale;
    mainTexOffset = lerp(mainTexOffset, uv.zw, _MainTexOffsetUseCustomData_XY); // vertex uv0.z : particle customData1.xy

    // According to world scale to auto set texture scale.
    float scalex = lerp(1.0f, length(unity_ObjectToWorld._11_12_13), _MainTexAutoScale);
    float scaley = lerp(1.0f, length(unity_ObjectToWorld._21_22_23), _MainTexAutoScale);
    
    float4 scrollUV = (float4)0;
    scrollUV.xy = uv.xy * _MainTex_ST.xy * float2(scalex, scaley) + mainTexOffset;
    scrollUV.zw = uv.xy;
    return scrollUV;
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

float2 MainTexRotation(float2 uv, float2 center, float angle)
{
    float rotaiton = angle;
    float s, c;
    sincos(rotaiton, s, c);
    float2x2 rtMatrix = float2x2(c, -s, s, c);
    uv = mul(uv - center, rtMatrix) + center;
    
    return uv;
}

inline float2 MainTexOffset(float2 uv, float2 customData1XY)
{
    float2 offsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop);
    float2 mainTexOffset = _MainTex_ST.zw * offsetScale;
    mainTexOffset = lerp(mainTexOffset, customData1XY, _MainTexOffsetUseCustomData_XY); // vertex uv0.z : particle customData1.xy
    
    return uv.xy * _MainTex_ST.xy + mainTexOffset;
}

half4 MainTexGray(half4 mainColor)
{
    mainColor.rgb = mainColor.r * 0.264 + mainColor.g * 0.617 + mainColor.b * 0.149;
    return mainColor;
}

// 踩坑：half的uv遇上clamp就像素化了
half4 SampleMainTex_LOD1(float2 uv, v2f input, half faceId, half colorIntensity)
{
    half4 color = _BackFaceOn ? lerp(_BackFaceColor, _Color, faceId) : _Color;
    color.rgb *= _MainTexColorIntensityUseCustomData_W ? colorIntensity : 1.0;

    half4 mainTex = (half4)0;
    
    // 约定：开启clamp时不做自动offset的支持
    uv.x = lerp(uv.x, saturate(uv.x), _MainTexUClamp);
    uv.y = lerp(uv.y, saturate(uv.y), _MainTexVClamp);
    
    mainTex = tex2D(_MainTex, uv);
    mainTex.rgb = lerp(mainTex.rgb * _MainTexColorCorrection.r, pow(max(mainTex.rgb, 1e-5), _MainTexColorCorrection.z) * _MainTexColorCorrection.y, _MainTexColorCorrection.w);
    
    mainTex = lerp(mainTex, MainTexGray(mainTex), _MainTexGray);

    if(_MainTexSingleChannelOn)
    {
        mainTex = mainTex[_MainTexChannel];
    }

    mainTex.rgb *= lerp(1, mainTex.a * input.color.a * color.a, _MainTexMultiAlpha);
    mainTex *= color * input.color;
    
    return mainTex;
}

void ApplyMainTexMask(inout half4 mainColor, float2 uv, float2 mainTexMaskScroll, float2 distortionUV)
{
    float2 maskTexOffset = _MainTexMaskOffsetStop ? _MainTexMask_ST.zw : _MainTexMask_ST.zw * GET_GLOBAL_TIME.xx;
    maskTexOffset += _DistortionAffectMainMaskTexture ? distortionUV : float2(0, 0);
    float2 scroll = _MainTexMaskUseCustomData2_XY ? mainTexMaskScroll : maskTexOffset;
    half4 maskTex = tex2D(_MainTexMask, uv * _MainTexMask_ST.xy + scroll);// fp opearate mask uv.
    mainColor.a *= maskTex[_MainTexMaskChannel] * _MainTexMultiFactor;
}

#ifdef _DISTORTIONON_ON
float2 ApplyDistortion_LOD1(float4 mainUV, float2 distortUV)
{
    float2 noise = tex2D(_DistortionNoiseTex, distortUV.xy).xy;
    float2 maskUV = mainUV.xy * _DistortionMaskTex_ST.xy + _DistortionMaskTex_ST.zw;
    float maskTex = tex2D(_DistortionMaskTex, maskUV)[_DistortionMaskChannel];

    float2 offset = noise * (0.2f * _DistortionIntensity * maskTex);
    offset = float2(_DistortionAffectU ? offset.x : 0.0f, _DistortionAffectV ? offset.y : 0.0f);
    return offset;
}
#endif

#ifdef _DISSOLVEON_ON
void ApplyDissolve(inout half4 mainColor, float2 dissolveUV, float2 dissolveDirUV, half4 color, half dissolveCDATA)
{
    if(_PixelDissolveOn)
    {
        dissolveUV = abs(dissolveUV - 0.5);
        dissolveUV = round(dissolveUV * _PixelWidth) / max(0.0001, _PixelWidth);
    }
    
    half refDissolve = tex2D(_DissolveTex, dissolveUV.xy)[_DissolveTexChannel];

    half dissolveDirectionTex = tex2D(_DissolveDirectionTex, dissolveDirUV.xy).x;

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
        // 确定范围 : 0~1 : 外~内
        half edge = saturate(CheapSmoothStep(_EdgeWidth - 0.1, _EdgeWidth + 0.1, dissolve));
        
        half3 edgeColor = lerp(_EdgeColor.rgb, _EdgeColor2.rgb, edge);
        half edgeAlpha = lerp(_EdgeColor.a, _EdgeColor2.a, edge);
        
        if(_EdgeColorMultiVertexColor)
        {
            edgeColor *= color.rgb;
        }
        
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
            // 黑色和彩色的逻辑不同，得增强透明度，才能被看到
            half targetAlpha = mainColor.a + originalAlpha * edgeStrength * _BlackEdgeAlphaFactor; // 用 edgeStrength 来保持原始的渐变关系
            mainColor.a = clamp(targetAlpha, 0, 1);
        }
        else
        {
            // 彩色边缘：使用加法混合，让边缘更亮
            finalEdgeColor = (mainColor.xyz * 0.5 + edgeColor) * 1.5;
        }
        
        mainColor.xyz = lerp(mainColor.xyz, finalEdgeColor, edge);
    }
}
#endif

#ifdef _FRESNELON_ON
void ApplyFresnel(inout float4 mainColor, half fresnel, half fresnelAlpha)
{
    fresnel = _FresnelScale * pow(max(fresnel, 0.001), _FresnelPower);
    half4 fresnelNode = fresnel * _FresnelColor;
    mainColor.rgb += fresnelNode.rgb;
    mainColor.a *= fresnelAlpha;
}
#endif

#ifdef _FRESNELREVERTON_ON
void ApplyFresnelRevert(inout float4 mainColor, half fresnel)
{
    fresnel = _FresnelScale * pow(max(fresnel, 0.001), _FresnelPower);
    fresnel = abs(1.0 - fresnel);
    fresnel = pow(fresnel, _RimTransparencyIntensity);
    mainColor.a *= fresnel;
}
#endif

#endif