Shader "XKnight/ToonPostProcessing/SpiralFluidTransition"
{
    Properties
    {
        _FromTex ("源画面 RT", 2D) = "black" {}
        _ToTex ("目标画面 RT", 2D) = "black" {}
        [NoScaleOffset] _DistortionTex ("扭曲贴图", 2D) = "gray" {}
        _DistortionTilingFlow ("扭曲贴图 平铺/流动", Vector) = (2, 1, 0, -1)
        _TextureDistortionParams ("贴图扭曲 开场/扩散/预留/预留", Vector) = (0.12, 0.025, 0, 0)
        [NoScaleOffset] _WarmBrightLut ("暖亮 LUT", 2D) = "white" {}
        _Center ("旋涡中心", Vector) = (0.5, 0.5, 0, 0)
        _TransitionParams ("转场进度 宽高比 时间 变亮阶段", Vector) = (0, 1.7777, 0, 0.2)
        _VisualParams ("视觉进度 变亮进度 Alpha淡出起点 原图结束Alpha", Vector) = (0, 0, 0.85, 0)
        _ToFinishParams ("目标图清晰起点 正常亮度起点 提亮强度 最大模糊半径", Vector) = (0.85, 0.92, 0.45, 0.008)
        _RadiusParams ("半径 起始/结束/边缘起始/边缘结束", Vector) = (0.02, 1.35, 0.15, 0.035)
        _SwirlParams ("旋涡 旋转/扭转/扭曲/边缘扭曲", Vector) = (72, 10, 0.14, 0.22)
        _NoiseParams ("噪声 不规则/缩放/流速/边界起伏", Vector) = (0.2, 5.2, 0.28, 0.34)
        _EdgeParams ("边缘 预留/旧画面残影/旧画面宽度/旧画面缩放", Vector) = (0, 0.08, 0.95, 0.035)
        _FoldParams ("翻卷 径向/切向/浪形", Vector) = (0.18, 0.16, 0.22, 0)
        _ExposureParams ("曝光强度 清晰终点 正常亮度终点 预留", Vector) = (0.42, 0.94, 0.94, 0)
        _LayerParams ("层级 扩张/外侧宽度/外侧扭曲/预留", Vector) = (0.12, 0.72, 0.18, 0)
        [HideInInspector] _WarmBrightLutParams ("暖亮 LUT 参数 尺寸/宽/高/预留", Vector) = (32, 1024, 32, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SpiralFluidTransition"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex FullscreenVert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _SPIRAL_FLUID_LOW_QUALITY
            #pragma shader_feature_local_fragment _NEED_LINEAR_TO_SRGB
            #pragma shader_feature_local_fragment _FROM_TEX_DISPLAY_SRGB
            #pragma shader_feature_local_fragment _TO_TEX_DISPLAY_SRGB

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "SpiralFluidTransitionInput.hlsl"
            #include "SpiralFluidTransitionNoise.hlsl"
            #include "SpiralFluidTransitionSampling.hlsl"
            #include "SpiralFluidTransitionBrighten.hlsl"
            #include "SpiralFluidTransitionCore.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
                half totalProgress = half(saturate(_TransitionParams.x));
                half visualProgress = half(saturate(_VisualParams.x));
                half brightenOnlyProgress = half(saturate(_VisualParams.y));
                half brightenPhase = half(clamp(_TransitionParams.w, 0.05, 0.95));

                if (totalProgress < brightenPhase)
                {
                    half4 fromColor = SpiralFluidSampleFromTex(uv);
                    fromColor.rgb = SpiralFluidApplyPreRevealBrighten(fromColor.rgb, brightenOnlyProgress);
#if defined(_NEED_LINEAR_TO_SRGB)
                    fromColor.rgb = SpiralFluidLinearToSRGB(fromColor.rgb);
#endif
                    return fromColor;
                }

                half fromCurrentAlpha = lerp(1.0h, half(saturate(_VisualParams.w)),
                    smoothstep(half(saturate(_VisualParams.z)), 1.0h, visualProgress));
                half toReachClarityRatio = half(saturate(_ToFinishParams.x));
                half toReachNormalBrightRatio = half(saturate(_ToFinishParams.y));
                half toBrightenIntensity = half(saturate(_ToFinishParams.z));
                half preRevealWarpProgress = smoothstep(brightenPhase * 0.62h, brightenPhase + 0.12h, totalProgress);
                preRevealWarpProgress *= 1.0h - smoothstep(0.24h, 0.56h, visualProgress);
                half aspect = half(max(_TransitionParams.y, 1.0e-4));
                float time = _TransitionParams.z;
                half2 center = half2(_Center.xy);
                float2 polarUV = SpiralFluidPolarUV(uv, center);
                half2 textureDistortion = SpiralFluidSampleTextureDistortion(polarUV, aspect);

                half revealProgress = smoothstep(0.08h, 1.0h, visualProgress);
                half revealEase = SpiralFluidEaseOutCubic(visualProgress);
                half radius = lerp(half(_RadiusParams.x), half(_RadiusParams.y), revealProgress);
                half edgeWidth = max(lerp(half(_RadiusParams.z), half(_RadiusParams.w), visualProgress), 1.0e-4h);

                half2 p = half2(uv - center);
                p.x *= aspect;

                half r = sqrt(max(dot(p, p), 1.0e-4h));
                half2 dir = p / max(r, 1.0e-3h);
                half2 tangent = half2(-dir.y, dir.x);

                float2 noiseUV = uv * _NoiseParams.y + _TransitionParams.z * _NoiseParams.z;
                half3 noise = SpiralFluidSampleNoise(noiseUV);
                float2 edgeNoiseUV = float2(center.x + dir.x / max(aspect, 1.0e-4h), center.y + dir.y);
                edgeNoiseUV = edgeNoiseUV * (_NoiseParams.y * 0.62h);
                edgeNoiseUV += float2(_TransitionParams.z * _NoiseParams.z, 0.0);
                half3 edgeNoise = SpiralFluidSampleNoise(edgeNoiseUV);

                float2 pFloat = float2(p.x, p.y);
                float2 waveOrbit = 0.12 * float2(sin(time * 1.37), cos(time * 1.71));
                half radialPulse = half(sin(r * (18.0 + visualProgress * 8.0) - time * (2.6 + _NoiseParams.z * 3.2)
                        + edgeNoise.g * 2.4));
                half logSpiralWaveA = SpiralFluidLogSpiralWave(pFloat, time, 0.618, 3.0, 5.0, visualProgress * 2.4h);
                half logSpiralWaveB = SpiralFluidLogSpiralWave(pFloat + waveOrbit, time, 0.618, 6.53, 6.0,
                        edgeNoise.r * 2.1h);
                half vortexWave = logSpiralWaveA * 0.54h + logSpiralWaveB * 0.34h + radialPulse * 0.12h;
                half vortexWaveMask = smoothstep(0.015h, 0.58h, r) * (1.0h - smoothstep(1.05h, 1.55h, r))
                    * smoothstep(0.04h, 0.22h, visualProgress) * (1.0h - smoothstep(0.92h, 1.0h, visualProgress));
                half secondaryVortexWave = SpiralFluidLogSpiralWave(pFloat * 1.42 + waveOrbit.yx * 0.55, time, 0.43,
                        -5.7, 8.3, radialPulse * 1.7h + visualProgress * 3.4h);
                half secondaryWaveMask = vortexWaveMask * smoothstep(0.06h, 0.26h, visualProgress);
                half preRevealCenterMask = (1.0h - smoothstep(0.03h, 0.44h, r)) * preRevealWarpProgress;
                half preRevealRingMask = smoothstep(0.06h, 0.2h, r) * (1.0h - smoothstep(0.24h, 0.58h, r))
                    * preRevealWarpProgress;
                half preRevealWave = logSpiralWaveA * 0.58h + logSpiralWaveB * 0.28h + radialPulse * 0.14h;
#if defined(_SPIRAL_FLUID_LOW_QUALITY)
                vortexWave *= 0.55h;
                secondaryVortexWave *= 0.5h;
                preRevealWave *= 0.65h;
#endif

                half earlyTwistMask = 1.0h - smoothstep(0.12h, 0.46h, visualProgress);
                earlyTwistMask = earlyTwistMask * earlyTwistMask * (3.0h - 2.0h * earlyTwistMask);
                half rippleWave = (edgeNoise.r - 0.5h) * 0.22h + (noise.b - 0.5h) * 0.14h;
                rippleWave += vortexWave * vortexWaveMask * earlyTwistMask * 0.38h;
                rippleWave += secondaryVortexWave * secondaryWaveMask * earlyTwistMask * 0.18h;
                rippleWave *= smoothstep(0.02h, 0.62h, r);

                half shapeBreakupGate = smoothstep(0.22h, 0.52h, visualProgress);
                half shapeWaveStrength = smoothstep(0.18h, 0.46h, visualProgress)
                    * (1.0h - smoothstep(0.92h, 1.0h, visualProgress));
                half shapeWaveLimit = max(edgeWidth * lerp(0.28h, 0.78h + visualProgress * 0.52h, shapeBreakupGate), 0.012h);
                half rawIrregular = ((noise.r - 0.5h) * 0.72h + (edgeNoise.b - 0.5h) * 0.28h);
                half rawBoundaryWave = ((edgeNoise.g - 0.5h) * 0.78h + (edgeNoise.r - 0.5h) * 0.34h + rippleWave * 0.22h
                    + vortexWave * vortexWaveMask * earlyTwistMask * 0.54h
                    + secondaryVortexWave * secondaryWaveMask * earlyTwistMask * 0.2h);
                half2 boundaryVector = edgeNoise.rg * 2.0h - 1.0h;
                half rawAngularWave = (dot(dir, boundaryVector) * 0.72h + (edgeNoise.b - 0.5h) * 0.32h
                    + rippleWave * 0.18h + vortexWave * vortexWaveMask * earlyTwistMask * 0.42h
                    + secondaryVortexWave * secondaryWaveMask * earlyTwistMask * 0.24h);
                half theta = half(atan2(p.y, p.x));
                half angularLobeWave = half(sin(theta * 3.0h + time * 0.75h + edgeNoise.r * 2.1h)) * 0.55h
                    + half(sin(theta * 5.0h - time * 0.52h + noise.g * 3.1h)) * 0.32h
                    + half(sin(theta * 7.0h + radialPulse * 1.7h)) * 0.18h;
                half maskBreakupStrength = shapeWaveStrength * (0.36h + smoothstep(0.32h, 0.68h, visualProgress) * 0.48h);
                half rawShapeWave = rawIrregular * half(_NoiseParams.x) * 0.62h
                    + rawBoundaryWave * half(_NoiseParams.w) * 0.46h + rawAngularWave * half(_NoiseParams.w) * 0.34h
                    + angularLobeWave * half(_NoiseParams.w) * 0.58h;
                half shapeWave = clamp(rawShapeWave * maskBreakupStrength, -shapeWaveLimit, shapeWaveLimit);
                half fluidRadius = r + shapeWave;

                half edgeDistance = fluidRadius - radius;
                half effectGate = smoothstep(0.0h, 0.08h, visualProgress);
                half revealMask = 1.0h - smoothstep(-edgeWidth, edgeWidth, edgeDistance);
                revealMask *= effectGate;
                half finalFillMask = smoothstep(0.94h, 1.0h, visualProgress);
                revealMask = lerp(revealMask, 1.0h, finalFillMask);
                half edgeRollMask = 1.0h - saturate(abs(edgeDistance) / edgeWidth);
                edgeRollMask *= smoothstep(0.02h, 0.2h, visualProgress);
                edgeRollMask *= 1.0h - smoothstep(0.88h, 1.0h, visualProgress);
                edgeRollMask *= 1.0h - finalFillMask;
                half toRevealMask = revealMask;
                half membraneAlpha = saturate((1.0h - saturate(abs(edgeDistance) / (edgeWidth * 1.85h)))
                        * (0.35h + abs(vortexWave) * earlyTwistMask * 0.24h + abs(radialPulse) * 0.2h)
                        * shapeWaveStrength * (1.0h - finalFillMask));
                half edgeHeatMask = saturate(edgeRollMask * 1.3h);
                half outerInfluenceWidth = max(half(_LayerParams.y) * 0.52h, 1.0e-4h);
                half outerDistortionMask = saturate((1.0h - toRevealMask)
                    * (1.0h - smoothstep(edgeWidth * 0.5h, edgeWidth + outerInfluenceWidth, edgeDistance))
                    * effectGate);
                half outerReadabilityMask = outerDistortionMask * (1.0h - finalFillMask);
                half outerInfluenceMask = outerReadabilityMask * 0.52h;
                half openingTwistMask = earlyTwistMask;
                half peelMembraneMask = smoothstep(0.22h, 0.48h, visualProgress) * (1.0h - smoothstep(0.82h, 1.0h, visualProgress))
                    * (1.0h - finalFillMask);
                half nearPeelEdgeMask = saturate(edgeRollMask * 1.18h + membraneAlpha * 0.48h) * peelMembraneMask;
                half outerAirBandMask = saturate((1.0h - toRevealMask) * (1.0h - smoothstep(edgeWidth * 0.25h,
                            edgeWidth + outerInfluenceWidth * 1.45h, edgeDistance)) * effectGate
                        * (1.0h - finalFillMask));
                half outerAirWaveMask = saturate(outerAirBandMask
                        * (0.7h + abs(rippleWave) * 0.34h + abs(radialPulse) * 0.14h)
                        * (1.0h - nearPeelEdgeMask * 0.55h)) * smoothstep(0.16h, 0.36h, visualProgress)
                    * (1.0h - smoothstep(0.9h, 1.0h, visualProgress));

                half2 noiseVector = noise.rg * 2.0h - 1.0h;
                half centerCollapseRange = max(radius * 0.72h, 0.18h);
                half centerCollapseMask = (1.0h - smoothstep(centerCollapseRange * 0.18h, centerCollapseRange, r))
                    * effectGate * openingTwistMask;
                half swirlRadius = max(radius + edgeWidth * 2.0h, 0.12h);
                half centerWeight = (1.0h - smoothstep(swirlRadius * 0.08h, swirlRadius, r)) * effectGate
                    * openingTwistMask;
                half edgeSpinWeight = saturate(edgeHeatMask * 1.15h + outerInfluenceMask * 0.35h) * openingTwistMask;
                half rimCurlMask = saturate(edgeRollMask * 1.25h + outerInfluenceMask * 0.45h);
                half coreSuctionMask = (1.0h - smoothstep(swirlRadius * 0.06h, swirlRadius * 0.88h, r)) * effectGate
                    * openingTwistMask;
                half suctionWave = (0.55h + abs(vortexWave) * 0.45h)
                    * saturate(coreSuctionMask + rimCurlMask * openingTwistMask * 0.22h);
                float spiralTurns = (time * _SwirlParams.x * 0.038 + (1.0h - saturate(r / max(swirlRadius, 1.0e-3h)))
                    * half(_SwirlParams.y) * 0.26 + visualProgress * 0.42) + edgeNoise.r * 0.12;
                float spiralAngle = spiralTurns * 6.2831853 + float(vortexWave * vortexWaveMask)
                    * (1.8 + _FoldParams.z * 3.1) * earlyTwistMask;
                half verticalStretchMask = saturate((edgeHeatMask * openingTwistMask * 0.32h + nearPeelEdgeMask * 0.24h
                        + outerAirWaveMask * 0.52h) * smoothstep(0.1h, 0.42h, visualProgress)
                        * (1.0h - smoothstep(0.86h, 1.0h, visualProgress)));
                half verticalStretchWave = half(sin((uv.y - center.y) * (28.0 + visualProgress * 18.0) + spiralAngle * 0.38
                        + float(secondaryVortexWave) * 2.2 * openingTwistMask
                        + float(edgeNoise.g - 0.5h) * 2.4 * outerAirWaveMask));
                half2 screenStretchDir = normalize(half2(tangent.x * 0.22h, 1.0h + tangent.y * 0.18h));
                half2 verticalStretchOffset = screenStretchDir * verticalStretchWave * verticalStretchMask
                    * half(_FoldParams.z) * 0.42h;
                half airWave = half(sin(float(edgeDistance / max(edgeWidth, 1.0e-4h)) * 5.6 + time * 1.65
                        + float(edgeNoise.r) * 4.1));
                half2 airWaveOffset = (dir * airWave * 0.46h + tangent * (edgeNoise.g - 0.5h) * 0.58h
                    + screenStretchDir * verticalStretchWave * 0.26h) * outerAirWaveMask * half(_LayerParams.z)
                    * (0.78h + visualProgress * 0.32h);
                half2 rotatedP = SpiralFluidRotate(p, spiralAngle);
                half2 polarSpiralOffset = rotatedP - p;
                half spinWave = half(sin(spiralAngle + 0.17 + float(vortexWave) * 1.4));
                half spiralSpinMask = saturate(edgeHeatMask * 1.25h + outerInfluenceMask * 0.35h + centerWeight * 0.25h
                    + centerCollapseMask * 0.25h) * openingTwistMask;
#if defined(_SPIRAL_FLUID_LOW_QUALITY)
                spiralSpinMask *= 0.62h;
#endif

                half waveAreaMask = saturate(edgeHeatMask * 0.72h + outerAirWaveMask * 0.55h
                    + outerInfluenceMask * openingTwistMask * 0.18h);
                half2 rippleOffset = (dir * rippleWave * 0.62h + tangent * (edgeNoise.b - 0.5h)
                    * (0.24h + half(_FoldParams.y) * 1.4h)
                    + tangent * vortexWave * vortexWaveMask * openingTwistMask * 0.68h
                    + tangent * secondaryVortexWave * secondaryWaveMask * openingTwistMask * 0.32h
                    + dir * radialPulse * vortexWaveMask * openingTwistMask * 0.22h) * waveAreaMask
                    * half(_FoldParams.z);

                half expandCurve = saturate(visualProgress * (1.35h - visualProgress * 0.35h));
                half expandAmount = half(_LayerParams.x) * expandCurve * 0.55h;
                half fromZoomEnvelope = smoothstep(0.04h, 0.28h, visualProgress) * (1.0h - smoothstep(0.78h, 1.0h, visualProgress));
                half2 globalExpandOffset = half2(uv - center) * (expandAmount + half(_EdgeParams.w) * fromZoomEnvelope);
                half toEdgeWarpMask = saturate(edgeHeatMask * openingTwistMask * 0.78h + nearPeelEdgeMask * 0.58h
                        + outerAirWaveMask * 0.28h);
                half2 heatVector = noiseVector * 0.38h + tangent * ((edgeNoise.b * 2.0h - 1.0h) * 0.36h)
                    + tangent * spinWave * spiralSpinMask * 0.46h
                    + tangent * vortexWave * vortexWaveMask * openingTwistMask * 0.62h
                    + tangent * secondaryVortexWave * secondaryWaveMask * openingTwistMask * 0.28h
                    + dir * ((edgeNoise.g - 0.5h) * 0.22h)
                    + dir * vortexWave * vortexWaveMask * openingTwistMask * 0.28h + verticalStretchOffset * 0.22h
                    + rippleOffset * 0.2h;
                half spiralInfluence = openingTwistMask * (2.1h + centerWeight * 0.8h + edgeSpinWeight * 2.1h);
                half2 spiralOffset = polarSpiralOffset * half(_SwirlParams.z) * spiralInfluence;
                spiralOffset += (tangent * spinWave * half(_SwirlParams.z) * 1.2h
                    + tangent * vortexWave * vortexWaveMask * half(_FoldParams.y) * 2.1h
                    + tangent * secondaryVortexWave * secondaryWaveMask * half(_FoldParams.y) * 0.95h
                    - dir * (centerCollapseMask + coreSuctionMask * 0.65h) * half(_FoldParams.x) * 1.05h)
                    * spiralSpinMask * 0.55h;
                half2 spiralUVOffset = half2(spiralOffset.x / max(aspect, 1.0e-4h), spiralOffset.y);
                half2 suctionOffset = dir * suctionWave * half(_FoldParams.x) * (0.35h + visualProgress * 0.65h);
                half2 edgeHeatOffset = (heatVector * half(_SwirlParams.z) * 2.4h
                    + tangent * rippleWave * half(_SwirlParams.w) * 5.0h
                    + tangent * vortexWave * rimCurlMask * openingTwistMask * half(_SwirlParams.w) * 1.7h + tangent
                    * secondaryVortexWave * secondaryWaveMask * openingTwistMask * half(_SwirlParams.w) * 0.95h
                    + dir * half(_FoldParams.x) * edgeRollMask * 0.85h) * edgeHeatMask
                    * saturate(openingTwistMask + nearPeelEdgeMask * 0.58h);
                half2 outerOffset = (heatVector * 1.2h + tangent * spinWave * openingTwistMask * 0.2h
                    + tangent * vortexWave * vortexWaveMask * openingTwistMask * 0.2h
                    + tangent * secondaryVortexWave * secondaryWaveMask * openingTwistMask * 0.1h
                    + dir * (0.2h + radialPulse * 0.1h) + airWaveOffset * 2.2h)
                    * saturate(outerInfluenceMask * openingTwistMask + outerAirWaveMask * 1.25h) * half(_LayerParams.z)
                    * (0.58h + visualProgress * 0.46h);
                half2 edgeExpandOffset = dir
                    * (edgeHeatMask * openingTwistMask + outerAirWaveMask * 0.48h + nearPeelEdgeMask * 0.36h)
                    * half(_LayerParams.x) * (0.35h + visualProgress * 0.65h);
                half preRevealDistortMask = saturate(preRevealCenterMask + preRevealRingMask * 0.75h);
                half2 preRevealCenterOffset = (tangent * preRevealWave * half(_SwirlParams.z) * 0.85h
                    - dir * (0.18h + abs(preRevealWave) * 0.28h) * half(_FoldParams.x)
                    + (noiseVector * 0.18h + screenStretchDir * verticalStretchWave * 0.22h) * half(_SwirlParams.w))
                    * preRevealDistortMask;
                half openingTextureDistortionMask = saturate(preRevealDistortMask + centerWeight * 0.45h
                    + spiralSpinMask * 0.35h) * openingTwistMask;
                half expandingTextureDistortionMask = saturate(edgeHeatMask * 0.55h + outerAirWaveMask * 0.65h
                    + outerInfluenceMask * 0.35h + nearPeelEdgeMask * 0.35h);
                half2 openingTextureDistortionOffset = textureDistortion * half(_TextureDistortionParams.x)
                    * openingTextureDistortionMask;
                half2 expandingTextureDistortionOffset = textureDistortion * half(_TextureDistortionParams.y)
                    * expandingTextureDistortionMask;
                half2 textureDistortionOffset = openingTextureDistortionOffset + expandingTextureDistortionOffset;
                float2 fromUV = saturate(uv - float2(globalExpandOffset + edgeExpandOffset + textureDistortionOffset));
                float2 toUV = saturate(uv + float2(expandingTextureDistortionOffset * 0.45h * toEdgeWarpMask
                    - openingTextureDistortionOffset * 0.15h * toEdgeWarpMask));
                half foldBackMask = saturate((nearPeelEdgeMask * (0.32h + abs(rippleWave) * 0.32h)
                        + edgeHeatMask * openingTwistMask * (0.18h + abs(vortexWave) * 0.2h))
                        * (1.0h - smoothstep(0.9h, 1.0h, visualProgress)));
                half2 foldBackOffset = (tangent * (0.012h + half(_SwirlParams.w) * 0.16h)
                    + verticalStretchOffset * 0.38h - dir * half(_FoldParams.x) * 0.16h) * foldBackMask;
                float2 foldBackUV = saturate(fromUV + float2(foldBackOffset));

                half4 fromColor = SpiralFluidSampleFromTex(fromUV);
                half4 foldedFromColor = SpiralFluidSampleFromTex(foldBackUV);
                fromColor = lerp(fromColor, foldedFromColor, foldBackMask * (0.18h + edgeRollMask * 0.22h));
                half clarityEnd = clamp(half(_ExposureParams.y), 0.001h, 1.0h);
                half normalBrightEnd = clamp(half(_ExposureParams.z), 0.001h, 1.0h);
                half clarityStart = min(toReachClarityRatio, clarityEnd - 0.001h);
                half normalBrightStart = min(toReachNormalBrightRatio, normalBrightEnd - 0.001h);
                half warmBrightHold = 1.0h - smoothstep(normalBrightStart, normalBrightEnd, visualProgress);
                half preRevealBright = brightenOnlyProgress * warmBrightHold;
                half preRevealWarmth = preRevealBright * (1.0h - smoothstep(0.78h, 0.96h, visualProgress));
                half4 preRevealBlurColor = SpiralFluidSampleFromRTBlur(fromUV, preRevealWarmth * 0.003h);
                fromColor = lerp(fromColor, preRevealBlurColor, saturate(preRevealWarmth * 0.18h));
                fromColor.rgb = SpiralFluidApplyPreRevealBrighten(fromColor.rgb, preRevealBright);
                half toClarity = smoothstep(clarityStart, clarityEnd, visualProgress);
                half toRevealRampUp = smoothstep(0.0h, toReachNormalBrightRatio * 0.75h, visualProgress);
                half toBrightFade = (1.0h - smoothstep(normalBrightStart, normalBrightEnd, visualProgress))
                    * toRevealRampUp;
                half toBlurRadius = (1.0h - toClarity) * half(_ToFinishParams.w);
                half4 sharpToColor = SpiralFluidSampleToTex(toUV);
                half4 blurredToColor = SpiralFluidSampleToTexBlur(toUV, toBlurRadius);
                half toSharpBlend = smoothstep(0.88h, 1.0h, toClarity);
                half4 toColor = lerp(blurredToColor, sharpToColor, toSharpBlend);
                half toExposure = (1.0h - toClarity) * saturate(toRevealMask * 0.85h + edgeHeatMask * 0.15h);
                toColor.rgb *= 1.0h + toExposure * half(_ExposureParams.x) * 0.4h;
                toColor.rgb = SpiralFluidApplyPreRevealBrighten(toColor.rgb, toBrightenIntensity * toBrightFade);
                half revealOpacity = smoothstep(0.02h, 0.32h, revealEase);
                half toCoreMask = smoothstep(0.68h, 0.96h, toRevealMask);
                half toEdgeFeatherMask = saturate(toRevealMask - toCoreMask);
                half toBlendMask = saturate(toCoreMask + toEdgeFeatherMask * revealOpacity + membraneAlpha * 0.16h);
                toBlendMask = lerp(toBlendMask, toRevealMask, visualProgress * 0.35h);
                toBlendMask = lerp(toBlendMask, 1.0h, finalFillMask);
                half fromWeight = (1.0h - toBlendMask) * fromCurrentAlpha;
                half4 color;
                color.rgb = fromColor.rgb * fromWeight + toColor.rgb * (1.0h - fromWeight);

                half fromRimOverlayStrength = max(half(_EdgeParams.y), 0.0h);
                half fromRimOverlayWidth = max(half(_EdgeParams.z), 0.1h);
                half fromRimOverlayMask = saturate(nearPeelEdgeMask * 0.78h
                        + membraneAlpha * peelMembraneMask * 0.18h);
                half2 fromRimPullOffset = dir * edgeWidth * fromRimOverlayWidth
                    * (0.48h + abs(edgeNoise.g - 0.5h) * 0.44h) + tangent * (edgeNoise.r - 0.5h) * half(_SwirlParams.w)
                    * 0.07h;
                float2 fromRimUV = saturate(fromUV - float2(fromRimPullOffset.x / max(aspect, 1.0e-4h),
                        fromRimPullOffset.y) + float2(spiralUVOffset * 0.08h * rimCurlMask * openingTwistMask));
                half3 fromRimColor = SpiralFluidSampleFromTex(fromRimUV).rgb;
                half fromRimOverlay = saturate(fromRimOverlayMask * (0.08h + fromRimOverlayStrength * 1.65h))
                    * fromCurrentAlpha;
                color.rgb = lerp(color.rgb, fromRimColor, fromRimOverlay);

                half peelLiftMask = saturate(nearPeelEdgeMask * 1.08h + outerAirWaveMask * 0.24h)
                    * smoothstep(0.24h, 0.5h, visualProgress) * (1.0h - smoothstep(0.86h, 1.0h, visualProgress));
                half2 fromPeelPullOffset = dir * edgeWidth * fromRimOverlayWidth
                    * (1.05h + abs(edgeNoise.g - 0.5h) * 0.9h) + tangent * (edgeNoise.r - 0.5h) * half(_SwirlParams.w)
                    * 0.18h + verticalStretchOffset * 0.62h + airWaveOffset * 0.42h;
                float2 fromPeelInnerUV = saturate(fromUV - float2(fromPeelPullOffset.x / max(aspect, 1.0e-4h),
                        fromPeelPullOffset.y));
                half2 fromPeelFoldOffset = tangent * (0.018h + half(_SwirlParams.w) * 0.2h)
                    * (0.55h + edgeNoise.r * 0.65h) - dir * edgeWidth * 0.38h + verticalStretchOffset * 0.45h
                    + airWaveOffset * 0.7h;
                float2 fromPeelFoldUV = saturate(fromUV + float2(fromPeelFoldOffset.x / max(aspect, 1.0e-4h),
                        fromPeelFoldOffset.y));
                half3 fromPeelColor = lerp(SpiralFluidSampleFromTex(fromPeelInnerUV).rgb,
                    SpiralFluidSampleFromTex(fromPeelFoldUV).rgb, 0.38h + edgeNoise.r * 0.22h);
                half peelLayer = saturate(peelLiftMask * (0.12h + fromRimOverlayStrength * 2.25h))
                    * fromCurrentAlpha;
                color.rgb = lerp(color.rgb, fromPeelColor, peelLayer);

                half membraneFeather = 1.0h - saturate(abs(edgeDistance)
                    / max(edgeWidth * (2.8h + fromRimOverlayWidth * 0.35h), 1.0e-4h));
                half membraneRefractMask = saturate((membraneAlpha * 1.4h + nearPeelEdgeMask * 0.35h
                        + outerAirWaveMask * 0.22h) * peelMembraneMask * membraneFeather
                    * (0.48h + fromRimOverlayStrength * 1.4h) * (1.0h - finalFillMask));
                half2 membranePullOffset = fromPeelPullOffset * 0.55h + airWaveOffset * 0.4h
                    + verticalStretchOffset * 0.28h;
                float2 membraneFromUV = saturate(fromUV - float2(membranePullOffset.x / max(aspect, 1.0e-4h),
                    membranePullOffset.y));
                half3 membraneColor = lerp(
                    SpiralFluidSampleFromRTBlur(membraneFromUV, min(edgeWidth * 0.22h, 0.018h)).rgb,
                    SpiralFluidSampleToTex(toUV).rgb,
                    saturate(toRevealMask * 0.35h + membraneAlpha * 0.18h));
                color.rgb = lerp(color.rgb, membraneColor, membraneRefractMask * 0.28h * fromCurrentAlpha);

                color.a = 1.0h;
#if defined(_NEED_LINEAR_TO_SRGB)
                color.rgb = SpiralFluidLinearToSRGB(color.rgb);
#endif
                return color;
            }
            ENDHLSL
        }

    }

    FallBack Off
}
