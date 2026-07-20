/*******************************************************************************
* File: FullScreenTransitionCommonFunction.hlsl
 * Author: os.yongzi.xie
 * Data: 2026/04/23 10:00
 * Description: 后处理中 传送领域的shader 中的公共函数
 * Notice:无
 ******************************************************************************/
#if !defined(FULL_SCREEN_TRANSITION_COMMON_FUNCTION_H)
#define FULL_SCREEN_TRANSITION_COMMON_FUNCTION_H


float2 ApplyDistortionOffset(float2 distortUV)
{
    float2 noise = tex2D(_DistortionNoiseTex, distortUV.xy).xy;
    float2 offset = noise * 0.2f * _DistortionIntensity;
    return offset;
}

void ApplyMainTexMask(inout half4 mainColor, float2 uv, float2 distortionUV)
{
    float2 maskTexOffset = _MainTexMask_ST.zw * GET_GLOBAL_TIME.xx;
    maskTexOffset += _DistortionAffectMainMaskTexture ? distortionUV : float2(0, 0);
    half4 maskTex = tex2D(_MainTexMask, uv * _MainTexMask_ST.xy + maskTexOffset);// fp opearate mask uv.
    mainColor.a *= maskTex[_MainTexMaskChannel] * _MainTexMultiFactor;
}


void ApplyDissolve(inout float4 mainColor, float2 dissolveUV)
{
    half4 dissolveTex = tex2D(_DissolveTex, dissolveUV.xy);
    half refDissolve = dissolveTex[_DissolveTexChannel];
   // half dissolveDirectionTex = tex2D(_DissolveDirectionTex, directionalUV.xy).x;
    // remap cutoff
    half cutoff = _Cutoff;
    cutoff = lerp(-0.15, 1.01, cutoff);
    half dissolve = refDissolve - cutoff;
    
    
 //   
    dissolve = saturate(CheapSmoothStep(_DissolveFadingMin, _DissolveFadingMax, dissolve));

    if(_DissolveClipOn)
    {
        clip(dissolve - 0.01);
    }
    
    half originalAlpha = mainColor.a; // 备份原始的alpha值
    //mainColor.a *= dissolve;
    mainColor.rgb *= dissolve;
    /*
    if(_DissolveEdgeOn)
    {
        half edgeWidth = _EdgeWidth1;
        // 确定范围 : 0~1 : 外~内
        half edge = saturate(CheapSmoothStep(edgeWidth - 0.1, edgeWidth + 0.1, dissolve));
        half3 edgeColor = lerp(_EdgeColor1.rgb, _EdgeColor2.rgb, edge);
        half edgeAlpha = lerp(_EdgeColor1.a, _EdgeColor2.a, edge);
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
        mainColor.xyz = lerp(mainColor.xyz, finalEdgeColor, edge);   
    }*/
//    
}



#endif