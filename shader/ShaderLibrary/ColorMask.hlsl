#ifndef XKNIGHT_COLOR_MASK
#define XKNIGHT_COLOR_MASK

int _MaskEnable;
float3 _MaskCenter;
float _MaskSize;
float _MaskLerpSize;
float3 _MaskColor;

void BlendColorMask(inout half3 col, float3 positionWS)
{
    float dis = distance(positionWS, _MaskCenter);
    float lerpVal = smoothstep(-0.5 * _MaskLerpSize, 0.5 * _MaskLerpSize, dis - _MaskSize);
    col = _MaskEnable ? lerp(col, col * _MaskColor, lerpVal) : col;
}

#endif