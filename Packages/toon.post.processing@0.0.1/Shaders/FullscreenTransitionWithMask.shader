/*
全屏过渡后处理效果 - 用遮罩来控制范围
*/
Shader "XKnight/ToonPostProcessing/FullscreenTransitionWithMask"
{
    Properties
    {
        [Main(Mask, __, on, off)]
        _Mask ("遮罩设置", Float) = 1
        
        [Sub(Mask)] _FullSceneTransitionMaskRT ("过渡范围遮罩", 2D) = "black" {}
        
        
        [Main(Fill, __, on, off)]
        _Fill ("填充设置", Float) = 1
        
        [Sub(Fill)] [HDR] _TransitionColor ("过渡颜色", Color) = (0, 0, 0, 1)
        [Sub(Fill)] [HDR] _FillFogBrightColor ("填充雾效-亮部颜色", Color) = (0.2, 0.2, 0.2, 1)
        
        [Sub(Fill)] _FillFogNoiseScale ("填充雾效-噪声缩放", Float) = 15
        [Sub(Fill)] _FillFogNoiseSpeed ("填充雾效-噪声速度", Float) = (-0.5, -0.3, 0, 0)
        
        [Sub(Fill)] _FillFogDark ("填充雾效-暗部比例", Range(0, 1)) = 0
        [Sub(Fill)] _FillFogIntensity ("雾强度", Range(0, 1)) = 0
        
        [Sub(Fill)] [HideInInspector] _FillFogFlowSpeed ("填充雾效-流动速度", Float) = 1
        [Sub(Fill)] [HideInInspector] _FillFogFlowDirection ("填充雾效-流动方向", Vector) = (0, 0, 1, 0)
        
        
        [Main(Edge, __, on, off)]
        _Edge ("边设置", Float) = 1
        
        [Sub(Edge)] [HDR] _EdgeColor ("边缘颜色", Color) = (1, 1, 0, 1)
        [Sub(Edge)] _EdgeAmount ("边缘强度", Range(0, 1)) = 0.5
        
        
        [Main(Blend, __, on, off)]
        _Blend ("混合设置", Float) = 1
        
        [Sub(Blend)] _BlendTex ("混合纹理", 2D) = "white" {}
        [Sub(Blend)] _BlendAmount ("混合强度", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "FullscreenTransitionWithMask"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Off Cull Off
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex fullscreen_vert
            #pragma fragment frag
            
            #pragma shader_feature_fragment _ _BLEND_TEX_ON
            #pragma shader_feature_fragment _ _NEED_LINEAR_TO_SRGB
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/Common.hlsl"
            #include "./FBMFog.hlsl"
            //#include "./FBMFogVolume.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                half4 _TransitionColor;
                half4 _FillFogBrightColor;
                half _FillFogNoiseScale;
                float4 _FillFogNoiseSpeed;
                half _FillFogDark, _FillFogIntensity;
                half _FillFogFlowSpeed;
                float4 _FillFogFlowDirection;
                
                half4 _EdgeColor;
                half _EdgeAmount;
                
                half _BlendAmount;
            CBUFFER_END
            
            TEXTURE2D_X_FLOAT(_FullSceneTransitionMaskRT); SAMPLER(sampler_FullSceneTransitionMaskRT);
            TEXTURE2D_X(_BlendTex); SAMPLER(sampler_BlendTex);
            
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
                
                return output;
            }
            
            half4 frag(VaryingsFT input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 ftmColor = SAMPLE_TEXTURE2D_X(_FullSceneTransitionMaskRT, sampler_FullSceneTransitionMaskRT, input.uv);
                float ftMask = ftmColor.r;
                
                half3 baseColor = 0;
                #ifdef _BLEND_TEX_ON
                float2 blendUV = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                blendUV.y = 1 - blendUV.y;
                #endif
                
                half4 blendTexColor = SAMPLE_TEXTURE2D_X(_BlendTex, sampler_BlendTex, blendUV);
                blendTexColor = GetLinearToSRGB(blendTexColor);
                
                baseColor = lerp(_TransitionColor.rgb, blendTexColor.rgb, _BlendAmount);
                
                #else 
                baseColor = _TransitionColor.rgb;
                
                
                // FBM 雾效 - 复刻 FullscreenTransition 黑色区域填充
                float2 st = input.uv;
                st.x *= _ScreenParams.x / _ScreenParams.y;
                
                float2 pos = st * _FillFogNoiseScale;
                float2 flow = _Time.y * _FillFogNoiseSpeed.xy;
                float warp = FillFBM(pos);
                float2 warpVec = float2(warp, warp);
                float fbmVal = saturate(FillFBM(pos + flow + warpVec));
                
                half3 fogBrightColor = _FillFogBrightColor.rgb;
                half3 fogDarkColor = fogBrightColor * _FillFogDark;
                half3 foggedColor = lerp(fogDarkColor, fogBrightColor, fbmVal);
                
                baseColor = lerp(baseColor, foggedColor, _FillFogIntensity);
                
                
                /*
                // 屏幕坐标 -> 射线方向（与 Shadertoy 一致）
                float2 coord = input.uv * 2.0 - 1.0;
                coord.x *= _ScreenParams.x / _ScreenParams.y;
                float3 rayDir = normalize(float3(coord, 1.0));

                // 射线起点：只让 z 随时间变化 → 喷涌/收缩
                float fogTime = _Time.y * _FillFogFlowSpeed;
                fogTime /= FOG_SAMPLER_SCALE;
                
                float3 rayOrigin = _FillFogFlowDirection.xyz * fogTime;

                float3 fogColor;
                float fogAlpha;
                TraceFogVolume(rayOrigin, rayDir, FOG_TMIN, FOG_TMAX, FOG_SAMPLER_SCALE, FOG_STEP_COUNT, fogColor, fogAlpha);

                // 和原来一样：雾色、暗部、强度
                half3 fogBrightColor = _FillFogBrightColor.rgb;
                half3 fogDarkColor = fogBrightColor * _FillFogDark;
                half3 foggedColor = lerp(fogDarkColor, fogBrightColor, saturate(sqrt(fogColor)));  // 或直接用 fogColor 的 luminance
                baseColor = lerp(baseColor, foggedColor, _FillFogIntensity * fogAlpha);
                */
                #endif
                
                // 计算梯度变化，做边缘强度
                float gx = ddx(ftMask);
                float gy = ddy(ftMask);
                float gradMag = length(float2(gx, gy));
                
                half inTransition = step(0.0, ftMask) * (1 - step(0.95, ftMask));
                half edgeFactor = step(0.0, gradMag) * inTransition;
                
                half3 finalColor = lerp(baseColor, _EdgeColor.rgb, edgeFactor * _EdgeAmount);
                half finalAlpha = ftMask;
                
                #ifdef _NEED_LINEAR_TO_SRGB
                finalColor = GetLinearToSRGB(finalColor);
                #endif
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}