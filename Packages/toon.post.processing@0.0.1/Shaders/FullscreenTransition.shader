/*
全屏过渡后处理效果
*/
Shader "XKnight/ToonPostProcessing/FullscreenTransition"
{
    Properties
    {
        _MaxFarDepth ("最远深度", Float) = 20
        _TransitionCenterPosition ("过渡的世界空间中心位置", Vector) = (0, 0, 0, 0)
        _MaxRadius ("过渡的最大半径", Float) = 20
        _AlphaMultiFactor ("Alpha 相乘系数", Float) = 1        
        _NoiseTex ("噪声纹理", 2D) = "white" {}
        _NoiseDir ("噪声滚动 (xy 方向/速度)", Vector) = (0, 1, 0, 0)
        _NoiseIntensity ("边缘扭曲强度", Float) = 1
        
        
        [HDR] _TransitionColor ("过渡颜色", Color) = (0, 0, 0, 1)
        [HDR] _FillFogBrightColor ("填充雾效-亮部颜色", Color) = (0.2, 0.2, 0.2, 1)
        _FillFogNoiseScale ("填充雾效-噪声缩放", Float) = 15
        _FillFogNoiseSpeed ("填充雾效-噪声速度 (xy)", Vector) = (-0.5, -0.3, 0, 0)
        _FillFogDark ("填充雾效-暗部比例", Range(0, 1)) = 0
        _FillFogIntensity ("雾强度", Range(0, 1)) = 0
        
        // 整个圆
        [HDR] _EdgeColor ("边缘颜色", Color) = (1, 1, 0, 1)
        _EdgeWidth ("边缘宽度", Float) = 0.01
        
        _BlendTex ("混合纹理", 2D) = "white" {}
        _BlendAmount ("混合强度", Range(0, 1)) = 1
        
        [Toggle] _FBMMode ("使用FBM模式", Int) = 1
        //------------- 基础-------------
        _MainTex ("主纹理", 2D) = "white" {}
        [HDR] _Color ("主纹理颜色", Color) = (1, 1, 1, 1)
        [Toggle] _MainTextureRadialUVOn ("主纹理极坐标", Int) = 0
        [Toggle] _MainTexMultiAlpha ("主纹理预乘Alpha(主纹理,主颜色)", Int) = 0
        [Toggle] _MainTexSingleChannelOn ("主纹理使用单通道", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MainTexChannel ("主纹理通道", Int) = 0
        _MainTexMultiFactor ("主纹理预乘系数", Float) = 1
        _MainTexMask ("主纹理遮罩", 2D) = "white" {}
        [Enum(R,0,G,1,B,2,A,3)] _MainTexMaskChannel ("遮罩通道", Int) = 0
        //------------- 基础-------------
        
        //--------------------扭曲------------------------------ 
        [Toggle] _DistortionOn ("开启扭曲", Int) = 0
        [Toggle] _DistortionRadialUVOn ("扭曲纹理极坐标", Int) = 0
        [Toggle] _DistortionAffectDissolve ("扭曲溶解图", Int) = 0
        [Toggle] _DistortionAffectMainTexture ("扭曲主纹理图", Int) = 1
        [Toggle] _DistortionAffectMainMaskTexture ("扭曲主纹理 Mask 图", Int) = 0
        _DistortionNoiseTex ("扭曲贴图", 2D) = "white" {}
        _DistortTile ("Tiling(xy:层1, zw:层2)", Vector) = (1, 1, 1, 1)
        _DistortDir ("Offset(xy:层1, zw:层2)", Vector) = (0, 1, 0, -1)
        _DistortionIntensity ("强度", Range(-10, 10)) = 0.5        
        //--------------------溶解：------------------------------
        [Toggle] _DissolveOn ("开启溶解", Int) = 0
        _DissolveTex ("溶解图", 2D) = "white" {}
        _DissolveDirectionTex ("溶解方向图", 2D) = "white" {}
        [Toggle] _DissolveTexOffsetStop ("禁止溶解自动滚动", Int) = 0
        [Toggle] _DissolveDirectionTexOffsetStop ("禁止溶解方向图自动滚动", Int) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DissolveTexChannel ("溶解图通道", Int) = 0
//        [Header(DissolveFading)]
        _DissolveFadingMin ("透明 Min", Range(0, 0.2)) = 0
        _DissolveFadingMax ("透明 Max", Range(0, 1.0)) = 0.2
//        [Header(Dissolve Clip)]
        [Toggle] _DissolveClipOn ("像素剔除", Int) = 1
        _Cutoff ("镂空值", Range(0, 1)) = 0.5
//        [Header(PixelDissolve)]
        [Toggle] _PixelDissolveOn ("像素化溶解", Float) = 0
        _PixelWidth ("像素化宽", Float) = 10
//        [Header(DissolveEdge)]
//        [Toggle] _DissolveEdgeOn ("开启溶解边", Int) = 0
//        _EdgeWidth1 ("Dissolve 噪声边宽度", Range(0, 0.3)) = 0.1
//        [Toggle] _DissolveEdgeWidthByCustomData_W ("溶解边受 CustomData.w 控制", Int) = 0
//        [Toggle] _DissolveEdgeWidthTexture("溶解边受贴图边控制", Int) = 0
//        _EdgeFadeRange ("边缘淡出范围", Range(0.001, 1)) = 0.6
//        [HDR] _EdgeColor1 ("Dissolve 噪声边1颜色", Color) = (1, 0, 0, 1)
//        [HDR] _EdgeColor2 ("Dissolve 噪声边2颜色", Color) = (0, 1, 0, 1)
//        _BlackEdgeAlphaFactor ("黑边的透明系数", Range(0, 1)) = 0.5        
    }
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "FullscreenTransition"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Off Cull Off
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex fullscreen_vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _FBMMODE_ON
            #pragma shader_feature_local_fragment _ _DISTORTIONON_ON
            #pragma shader_feature_local_fragment _ _DISSOLVEON_ON
            
            #pragma shader_feature_fragment _ _BLEND_TEX_ON
            #pragma shader_feature_fragment _ _NEED_LINEAR_TO_SRGB
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/Common.hlsl"
            #include "./FBMFog.hlsl"
            #include "Assets/OutputRes/shader/Particle/VFX/libs/NodeLib.hlsl"
            #include "FullScreenTransitionInput.hlsl"
            #include "FullScreenTransitionCommonFunction.hlsl"
            
            #if SHADER_API_GLES
            struct AttributesFT
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            #else
            struct AttributesFT
            {
                uint vertexID : SV_VertexID;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            #endif

            struct VaryingsFT
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                float3 viewRayWS : TEXCOORD1;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsFT fullscreen_vert(AttributesFT input)
            {
                VaryingsFT output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
                #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
                #endif
                
                output.positionCS = pos;
                output.uv = uv;
                
                // 在 ndc 空间中远平面上的点
                float4 positionNDC = float4(output.uv * 2 - 1, 1, 1);
                positionNDC.y *= _ProjectionParams.x;
                
                // 从 ndc 空间转换到世界空间
                float4 rayEnd = mul(UNITY_MATRIX_I_VP, positionNDC);
                output.viewRayWS = rayEnd.xyz / rayEnd.w - _WorldSpaceCameraPos;
                
                return output;
            }

            #define FAR_DEPTH_THRESHOLD     0.999h

            float SampleSceneDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraSceneDepthTexture, sampler_CameraSceneDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
            }
            
            half4 frag(VaryingsFT input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 先确保把角色不会被挡住
                half4 cameraCharacterDepthColor = SAMPLE_TEXTURE2D_X(_CameraCharacterDepthTexture, sampler_CameraCharacterDepthTexture, input.uv);
                half cameraCharacter01Depth = Linear01Depth(cameraCharacterDepthColor.r, _ZBufferParams);
                clip(cameraCharacter01Depth - FAR_DEPTH_THRESHOLD);
                
                float depth = SampleSceneDepth(input.uv);
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float fragDepth = min(linearDepth, _MaxFarDepth);
                
                // 因为 fragDepth 里记录的深度都是垂直的，所以需要用这个余弦值来做矫正
                float3 rayDir = normalize(input.viewRayWS);
                float viewZ = dot(rayDir, -UNITY_MATRIX_V[2].xyz);
                float3 positionWS = _WorldSpaceCameraPos + rayDir * (fragDepth / abs(viewZ));
                
                float3 centerPosWS = _TransitionCenterPosition.xyz;
                float dist = distance(positionWS, centerPosWS);
                
                // 采样噪声
                float3 dirToCenter = normalize(positionWS - centerPosWS);
                float2 distortUV = dirToCenter.xz * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                distortUV += frac(_NoiseDir.xy * _Time.y);
                
                float2 noise2 = SAMPLE_TEXTURE2D_X(_NoiseTex, sampler_NoiseTex, distortUV).xy;
                noise2 = noise2 * 2.0 - 1.0; // [0, 1] -> [-1, 1]
                
                float radiusOffset = noise2.x * _NoiseIntensity;
                float noisyRadius = _MaxRadius + radiusOffset;
                // 与边之间的距离
                float distFromEdge = dist - noisyRadius;
                
                // 过渡遮罩
                float transitionMask = 1.0 - CheapSmoothStep(0, _EdgeWidth, max(0, distFromEdge));
                // 纹理混合
                #ifdef _BLEND_TEX_ON
                half4 blendTexColor = SAMPLE_TEXTURE2D_X(_BlendTex, sampler_BlendTex, input.uv);
                half3 baseColor = lerp(_TransitionColor.rgb, blendTexColor.rgb, _BlendAmount);
                // 填充纯色，或与雾做混合
                #else
                half3 baseColor = _TransitionColor.rgb;
                #if defined(_FBMMODE_ON)  //if (_FBMMode)
                {
                    // FBM 雾效
                    float2 st = input.uv;
                    st.x *= _ScreenParams.x / _ScreenParams.y;
                
                    float2 pos = st * _FillFogNoiseScale;
                    float2 flow = _Time.y * _FillFogNoiseSpeed.xy;
                    float warp = FillFBM(pos);
                    float2 warpVec = float2(warp, warp);
                    float fbmVal = saturate(FillFBM(pos + flow + warpVec)); // 雾图案 0~1
                
                    half3 fogBrightColor = _FillFogBrightColor.rgb;
                    half3 fogDarkColor = fogBrightColor * _FillFogDark;
                    half3 foggedColor = lerp(fogDarkColor, fogBrightColor, fbmVal);
                
                    baseColor = lerp(baseColor, foggedColor, _FillFogIntensity);
                }
                #else //else
                {
                    //-----------------基础-------------------------------------
                    // 
                    float2 inputUV = input.uv;
                    float2 finalMainUV = inputUV; 
                    float2 distortionUV = float2(0,0);
                    float2 polarUV = PolarUV(input.uv);
                    // 扭曲：
                    #if defined(_DISTORTIONON_ON)  //if (_DistortionOn)
                    {
                        float2 distortUV = float2(0, 0);
        
                        if (_DistortionRadialUVOn)
                        {
                            distortUV = polarUV * _DistortTile.xy + frac(_DistortDir.xy * GET_GLOBAL_TIME.xx);
                        }
                        else
                        {
                            distortUV.xy = inputUV * _DistortTile.xy + frac(_DistortDir.xy * GET_GLOBAL_TIME.xx);
                        }
                        float2 mainUV = inputUV;
                        if(_MainTextureRadialUVOn)
                        {
                            mainUV = polarUV; 
                        }
                        mainUV = mainUV * _MainTex_ST.xy +  _MainTex_ST.zw * GET_GLOBAL_TIME.xx;
                        float2 distortionUV = ApplyDistortionOffset(distortUV);
                        if(_DistortionAffectMainTexture)
                            finalMainUV = mainUV.xy + distortionUV;
                    }
                    #else //else
                    {
                        float2 curUV = inputUV;
                        if(_MainTextureRadialUVOn)
                        {
                            curUV = polarUV;
                        }
                        finalMainUV = curUV * _MainTex_ST.xy +  _MainTex_ST.zw * GET_GLOBAL_TIME.xx;                    
                    }
                    #endif
                    half4 mainColor = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, finalMainUV);
                    
                    if(_MainTexSingleChannelOn)
                    {
                        mainColor = mainColor[_MainTexChannel];
                    }
                    mainColor.rgb *= lerp(1,mainColor.a * _Color.a,_MainTexMultiAlpha);
                    mainColor *= _Color;
                    ApplyMainTexMask(mainColor, inputUV, distortionUV);
                    //  上面已准备完成  mainColor的数据  
                    //-----------------基础end-------------------------------------
                    //-------------------溶解相关--------------------------
                    #if defined(_DISSOLVEON_ON)    //if(_DissolveOn)
                    {
                        float2 uvTextures = input.uv;
                        float2 distortionUV = input.uv;
                        // 溶解图 UV
                        float2 dissolveOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveTexOffsetStop);
                        float2 dissolveOffset = _DissolveTex_ST.zw * dissolveOffsetScale;
                        float2 dissolveUV = uvTextures + (_DistortionAffectDissolve ? distortionUV : 0);
                        dissolveUV = dissolveUV * _DissolveTex_ST.xy + dissolveOffset;
                        //                    // 溶解方向图 UV
                        //                    float2 dissolveDirectionOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveDirectionTexOffsetStop);
                        //                    float2 dissolveDirectionOffset = _DissolveDirectionTex_ST.zw * dissolveDirectionOffsetScale;
                        //                    float2 dissolveDirectionUV = uvTextures * _DissolveDirectionTex_ST.xy + dissolveDirectionOffset;
                        ApplyDissolve(mainColor, dissolveUV);
                    }
                    #endif
                    baseColor = mainColor.rgb;
                }
                #endif
                #endif
                half3 finalColor = lerp(_EdgeColor.rgb, baseColor.rgb, transitionMask);
                float alpha = saturate(transitionMask * _AlphaMultiFactor) * step(1e-5, _MaxRadius);
                
                #ifdef _NEED_LINEAR_TO_SRGB
                finalColor = GetLinearToSRGB(finalColor);
                #endif
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "XKnight.ShaderGUI.FullScreenTransition"
}