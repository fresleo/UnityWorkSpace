#ifndef TOONPBR_COMBAT_GLOW_SURFACE
#define TOONPBR_COMBAT_GLOW_SURFACE

inline half SoftRamp(half x, half start, half end, half power)
{
    half range = max(end - start, 1e-3h);
    half t = saturate((x - start) / range);
    return pow(max(t, 1e-4h), power);
}

half Hash12(float2 p)
{
    float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return (half)frac((p3.x + p3.y) * p3.z);
}

half ValueNoise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    half a = Hash12(i);
    half b = Hash12(i + float2(1.0, 0.0));
    half c = Hash12(i + float2(0.0, 1.0));
    half d = Hash12(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, (half)u.x), lerp(c, d, (half)u.x), (half)u.y);
}

half Fbm2Octaves(float2 p)
{
    return ValueNoise2(p) * 0.62h + ValueNoise2(p * 2.17h + float2(11.3, 7.7)) * 0.38h;
}

inline half ComputeCombatViewBand(InputDataToon inputData)
{
    half fresnel = 1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half bandStart = saturate(_CombatSurfaceGlowBand.x);
    half bandEnd = max(_CombatSurfaceGlowBand.y, bandStart + 1e-3h);
    half bandPower = max(_CombatSurfaceGlowBand.z, 1e-3h);
    return SoftRamp(fresnel, bandStart, bandEnd, bandPower);
}

inline half ComputeCombatSurfaceFill(InputDataToon inputData)
{
    half fresnel = 1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half softStart = saturate(_CombatSurfaceGlowFill.y);
    half softEnd   = max(_CombatSurfaceGlowFill.z, softStart + 1e-3h);
    half softPower = max(_CombatSurfaceGlowFill.w, 1e-3h);
    return SoftRamp(fresnel, softStart, softEnd, softPower);
}

inline half ComputeCombatPreserveShading(half3 baseColor)
{
    half preserve = saturate(_CombatSurfaceGlowBand.w);
    half luminance = saturate(dot(baseColor, half3(0.299h, 0.587h, 0.114h)) * 2.0h);
    half dimmer = lerp(1.0h, 0.5h + 0.5h * luminance, preserve);
    return dimmer;
}

inline half3 EvaluateCombatSurfaceGlowColor(half band)
{
    half midMask = smoothstep(0.15h, 0.65h, band);
    half outerMask = smoothstep(0.55h, 1.0h, band);

    half3 color = lerp(_CombatSurfaceGlowColorInner.rgb, _CombatSurfaceGlowColorMid.rgb, midMask);
    color = lerp(color, _CombatSurfaceGlowColorOuter.rgb, outerMask);
    return color;
}

inline half ComputeCombatBreakup(float3 positionWS, float2 uv)
{
    half breakupAmount = saturate(_CombatSurfaceGlowBreakupParams.y);
    if (breakupAmount <= 0.0h)
    {
        return 1.0h;
    }

    float scale = max((float)_CombatSurfaceGlowBreakupParams.x, 1e-3);

    float2 coordXZ = positionWS.xz * scale + float2(3.14, 1.59);
    float2 coordXY = positionWS.xy * scale * 1.37 + float2(8.21, 2.73);
    half fbmA = Fbm2Octaves(coordXZ);
    half fbmB = Fbm2Octaves(coordXY);

    half peaks = max(fbmA, fbmB);
    half valleys = min(fbmA, fbmB);
    half proc = saturate(peaks * 0.55h + valleys * 0.45h);

    proc = saturate((proc - 0.5h) * 2.4h + 0.5h);

    half noise = proc;
    UNITY_BRANCH
    if (_CombatSurfaceGlowUseBreakupTex > 0.5h)
    {
        float2 texUV = uv * _CombatSurfaceGlowBreakupTex_ST.xy + _CombatSurfaceGlowBreakupTex_ST.zw;
        half texSample = SAMPLE_TEXTURE2D_X(_CombatSurfaceGlowBreakupTex, sampler_CombatSurfaceGlowBreakupTex, texUV).r;
        noise = saturate(proc + (texSample - 0.5h) * 0.7h);
    }

    half threshold = saturate(_CombatSurfaceGlowBreakupParams.z);
    half feather = max(_CombatSurfaceGlowBreakupParams.w, 1e-3h);
    half shaped = smoothstep(threshold - feather, threshold + feather, noise);

    shaped = pow(shaped, 1.8h);

    half low = lerp(1.0h, -0.8h, breakupAmount);
    half high = lerp(1.0h, 1.8h, breakupAmount);
    return saturate(lerp(low, high, shaped));
}

inline half3 ApplyCombatSurfaceGlow(half3 baseColor, InputDataToon inputData, SurfaceDataToon surfaceData, float2 uv)
{
#if defined(_COMBAT_SURFACE_GLOW_ON)
    half viewBand = ComputeCombatViewBand(inputData);
    half surfaceFill = ComputeCombatSurfaceFill(inputData);

    half fillWeight = saturate(_CombatSurfaceGlowFill.x);
    half shapeSignal = saturate(viewBand + surfaceFill * fillWeight * (1.0h - viewBand));

    half breakup = ComputeCombatBreakup(inputData.positionWS, uv);
    half preserve = ComputeCombatPreserveShading(baseColor);

    half3 glowColor = EvaluateCombatSurfaceGlowColor(saturate(shapeSignal));
    half glowStrength = shapeSignal * breakup * preserve * _CombatSurfaceGlowIntensity;

    return baseColor + glowColor * glowStrength;
#else
    return baseColor;
#endif
}

#endif // TOONPBR_COMBAT_GLOW_SURFACE
