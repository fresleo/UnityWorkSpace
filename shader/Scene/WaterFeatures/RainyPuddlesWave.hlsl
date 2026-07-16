// 雨坑的波纹
#ifndef __RAINY_PUDDLES_PUDDLE__
#define __RAINY_PUDDLES_PUDDLE__

float2 WaveUV(float2 uv, float tiling, float speed, float rotationAngle)
{
    float2 temp_cast = (speed).xx;
    float temp_cos = cos(radians(rotationAngle));
    float temp_sin = sin(radians(rotationAngle));
    float2 rotator = mul(uv * tiling - float2(0.5, 0.5), float2x2(temp_cos, -temp_sin, temp_sin, temp_cos)) + float2(0.5, 0.5);
    float2 panner = (GET_GLOBAL_TIME_PARAMETERS.x * 0.05 * temp_cast + rotator);

    float2 waveUV = panner;
    return waveUV;
}

#endif // __RAINY_PUDDLES_PUDDLE__
