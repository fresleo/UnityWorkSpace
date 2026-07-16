Shader "XKnight/Particle/Distortion"
{
    Properties
    {
        // 设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("剔除模式", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("深度测试", Float) = 4
        
        
        
        // 扭曲/扰动 >>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Toggle] _DistortionRadialUVOn ("极坐标", Int) = 0
        [LiteToggle] _DistortionAffectU ("U", Int) = 1
        [LiteToggle] _DistortionAffectV ("V", Int) = 1
        [LiteToggle] _EdgeSoftening ("边缘软化", Int) = 0
        _EdgeSoftIntensity ("边缘软化强度", Range(0, 1)) = 0
        
        [NoScaleOffset] _DistortionNoiseTex ("噪声纹理 (xy: 第1层, zw: 第2层)", 2D) = "white" {}
        _DistortTile ("Tiling(xy:层1, zw:层2)", Vector) = (1,1,1,1)
        _DistortDir ("Offset(xy:层1, zw:层2)", Vector) = (0,1,0,-1)

        _DistortionMaskTex ("遮罩贴图", 2D) = "white"{}
        [Enum(R,0,G,1,B,2,A,3)] _DistortionMaskChannel ("遮罩通道", Int) = 0
        _DistortionIntensity ("强度", Range(-10, 10)) = 0.5
        
        // todo : 该功能暂未完成
        [NoScaleOffset] _DistortionNormalMap ("扭曲法线", 2D) = "gray" {}
        _DistortionNormalStrength ("扭曲法线强度", Range(0, 1)) = 0.1
        _DistortionNormalScrollSpeed ("扭曲法线滚动速度", float) = 10.0
        _DistortionNormalDirectionX ("扭曲法线滚动方向X", float) = 0
        _DistortionNormalDirectionY ("扭曲法线滚动方向Y", float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" "Queue" = "Transparent+1501"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "DistortionOffset"
            }

            Cull [_CullMode]
            ZWrite Off
            ZTest [_ZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha,Zero One

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _DOWN_SAMPLING_DEPTH_ON

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DISTORTIONRADIALUVON_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./libs/NodeLib.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                half _DistortionAffectU;
                half _DistortionAffectV;
                half _EdgeSoftening;
                half _EdgeSoftIntensity;
                
                float4 _DistortionMaskTex_ST;
                int _DistortionMaskChannel;
                half _DistortionIntensity;
                half4 _DistortTile;
                half4 _DistortDir;

                half _DistortionNormalStrength;
                half _DistortionNormalScrollSpeed;
                half _DistortionNormalDirectionX, _DistortionNormalDirectionY;
            CBUFFER_END
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

            sampler2D _DistortionNoiseTex; // (xy: 第1层, zw: 第2层)
            sampler2D _DistortionMaskTex; // (r, a)
            sampler2D _CameraOpaqueTexture;
            sampler2D _DistortionNormalMap;

            #if defined( _DOWN_SAMPLING_DEPTH_ON )
            TEXTURE2D_X_FLOAT(_CameraDepthScaledAttachment);
            SAMPLER(sampler_CameraDepthScaledAttachment);
            #else
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif
            
            float SampleDistortionDepth(float2 uv)
            {
                #if defined( _DOWN_SAMPLING_DEPTH_ON )
                return SAMPLE_TEXTURE2D_X(_CameraDepthScaledAttachment, sampler_CameraDepthScaledAttachment, UnityStereoTransformScreenSpaceTex(uv)).r;
                #else
                return SampleSceneDepth(uv);
                #endif
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0; // xy:main uv, zw : particle's customData(mainTex scroll)
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                //#if defined( SHADER_API_D3D11 )
                float4 grabPos : TEXCOORD2;
                //#endif
            };

            float2 GetDistortionOffset(float2 mainUV, float2 noise, half4 color)
            {
                float2 maskUV = mainUV * _DistortionMaskTex_ST.xy + _DistortionMaskTex_ST.zw;
                half4 maskTex = tex2D(_DistortionMaskTex, maskUV);

                // 开空气扭曲，color.a做为扭曲强度，不作为alpha
                float2 offset = noise * /*0.2f * */_DistortionIntensity * maskTex[_DistortionMaskChannel] * color.a;
                
                // 扭曲拆出来uv
                return offset * float2(_DistortionAffectU, _DistortionAffectV);
            }

            void ApplySoftParticle(inout float2 offset, float4 projPos)
            {
                float sampleDepth = SampleDistortionDepth(projPos.xy / projPos.w);
                float sceneZ = LinearEyeDepth(sampleDepth, _ZBufferParams);
                float delta = sceneZ - projPos.z;
                float fade = saturate(delta + 0.12 * delta);
                offset *= fade;
            }
            
            // // 获取法线
            // float3 GetNormal(TEXTURE2D_PARAM(_normal, sampler_normal), float2 uv, float4 _normal_ST, float3x3 tbnWS)
            // {
            //     float3 normal = 0;
            //
            //     float4 normalTex = SAMPLE_TEXTURE2D(_normal, sampler_normal, uv * _normal_ST.xy + _normal_ST.zw);
            //     float3 normalTS = normalize(UnpackNormal(normalTex));
            //     normal = normalize(TransformTangentToWorld(normalTS, tbnWS));
            //     
            //     return normal;
            // }
            
            // // 高度转法线
            // float3 HeightToNormal(TEXTURE2D_PARAM(_Height, sampler_Height), float4 texelSize, float2 uv)
            // {
            //     // 采样邻域高度（假设高度在 R 通道）
            //     float hL = SAMPLE_TEXTURE2D(_Height, sampler_Height, uv + float2(-texelSize.x, 0)).r;
            //     float hR = SAMPLE_TEXTURE2D(_Height, sampler_Height, uv + float2( texelSize.x, 0)).r;
            //     float hD = SAMPLE_TEXTURE2D(_Height, sampler_Height, uv + float2(0, -texelSize.y)).r;
            //     float hU = SAMPLE_TEXTURE2D(_Height, sampler_Height, uv + float2(0,  texelSize.y)).r;
            //
            //     float dx = hR - hL;
            //     float dy = hU - hD;
            //
            //     float3 n = normalize(float3(-dx, -dy, 1.0));
            //     return n;                  // [-1,1] 切线空间法线
            // }
            
            // // 法线转uv偏移
            // float2 NormalToUVOffset(float3 normalTS, float3x3 tbnWS)
            // {
            //     // 世界空间法线
            //     float3 normalWS = TransformTangentToWorld(normalTS, tbnWS, true);
            //     // 观察空间法线
            //     float3 normalVS = TransformWorldToViewDir(normalWS, true);
            //     return normalVS.xy;
            // }

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                o.color = v.color;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv; // uv.xy : main uv, zw : custom data.xy
                #if defined( SHADER_API_D3D11 )
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.grabPos.z = -TransformWorldToView(worldPos).z;
                #endif

                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                float2 distortUV = i.uv;
                #if defined( _DISTORTIONRADIALUVON_ON )
                distortUV = PolarUV(i.uv);
                #endif
                distortUV = distortUV * _DistortTile.xy + frac(_DistortDir.xy * GET_GLOBAL_TIME.x);
                half2 noise = tex2D(_DistortionNoiseTex, distortUV).xy;
                float2 offset = GetDistortionOffset(i.uv, noise, i.color);
                #if defined( SHADER_API_D3D11 )
                ApplySoftParticle(offset, i.grabPos); // 激活软粒子

                // todo: 暂时没空搞完整，先注释它
                // 增加法线扭曲，来模拟模糊的效果
                // i.uv.x += (_DistortionNormalScrollSpeed * _Time) * _DistortionNormalDirectionX;
                // i.uv.y += (_DistortionNormalScrollSpeed * _Time) * _DistortionNormalDirectionY;
                // normalOffset = i.grabPos.xy + ((tex2D(_DistortionNormalMap, i.uv) - 0.5) * _DistortionNormalStrength).xy;
                #endif
                
                offset = saturate(offset + 0.5);
                

               // 1. 基础计算
                float2 uvUsed = (offset - 0.5) * 0.2;
                float2 resUV  = offset + uvUsed;

                // 2. 判定当前处于哪种状态 (通过步进函数 step 替代 if)
                // 当 _DistortionIntensity > 0 时为 1.0，否则为 0.0
                float isPositive = step(0.00001, _DistortionIntensity); 
                float isNegative = 1.0 - isPositive;

                // 3. 计算各个边界条件的激活权重 (1.0 表示触发，0.0 表示未触发)
                float condX_GT1 = isPositive * step(1.0, resUV.x); // resUV.x > 1 且强度 > 0
                float condY_GT1 = isPositive * step(1.0, resUV.y); // resUV.y > 1 且强度 > 0
                float condX_LT0 = isNegative * step(resUV.x, 0.0); // resUV.x < 0 且强度 <= 0
                float condY_LT0 = isNegative * step(resUV.y, 0.0); // resUV.y < 0 且强度 <= 0

                // 4. 计算每种情况下的 Remap 目标值
                // 注意：因为 Remap 具体实现未知，这里用标准的重构方式。如果是自定义函数，请确保它支持内联计算。
                float u_GT1, u_LT0, v_GT1, v_LT0;
                Remap(uvUsed.x, float2(0, 0.1), float2(-0.1, 0.0), u_GT1); // 对应原 maxUV 到 minUV
                Remap(uvUsed.x, float2(-0.1, 0.0), float2(0, 0.1), u_LT0); // 对应原 minUV 到 maxUV
                Remap(uvUsed.y, float2(0, 0.2), float2(-0.1, 0.1), v_GT1);
                Remap(uvUsed.y, float2(-0.079, 0), float2(0, 0.05), v_LT0);
                // 5. 使用 lerp (线性插值) 混合结果，彻底消除 if
                float uNew = lerp(uvUsed.x, u_GT1, condX_GT1);
                uNew       = lerp(uNew,     u_LT0, condX_LT0);

                float vNew = lerp(uvUsed.y, v_GT1, condY_GT1);
                vNew       = lerp(vNew,     v_LT0, condY_LT0);
                // 6. 输出最终 offset
                offset = float2(uNew, vNew) * 5.0 + 0.5;
                //-------------------------
                
                
                // 因为 offset = saturate(offset + 0.5) 这个用了很久的奇怪方法
                // 边缘软化做了适配的混合插值
                // 后续，如果有将offset重构的情况下，务必将此处也修改一下
                float blend = saturate((noise.x + noise.y) * (2 - _EdgeSoftIntensity * 1.5));
                      blend = lerp(1, blend, _EdgeSoftening);
                
                return half4(offset.x, offset.y, 0, blend);
            }
            ENDHLSL
        }
    }

    CustomEditor "VFX.DistortionInspector"
}