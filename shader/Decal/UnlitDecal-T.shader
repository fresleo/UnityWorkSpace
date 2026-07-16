Shader "XKnight/Decal/Unlit Decal (Transparent)"
{
    Properties
    {
        // 渲染模式 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(RenderMode, __, off, off)]
        _RenderMode ("渲染模式", float) = 1
        
        [SubEnum(RenderMode, UnityEngine.Rendering.BlendMode)] _SrcBlend ("混合模式 - Src", float) = 5 // 5 = SrcAlpha
        [SubEnum(RenderMode, UnityEngine.Rendering.BlendMode)] _DstBlend ("混合模式 - Dst", float) = 10 // 10 = OneMinusSrcAlpha
        
        [SubEnum(RenderMode, UnityEngine.Rendering.CullMode)] _Cull ("剔除方式", float) = 1 // 1 = Front
        [SubEnum(RenderMode, UnityEngine.Rendering.CompareFunction)] _ZTest ("Z测试", float) = 5 // 5 = Greater
        
        // 主要设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Main, __, on, off)]
        _Main ("主要设置", float) = 1
        
        [Sub(Main)] _BaseMap ("主纹理", 2D) = "white" {}
        [Sub(Main)] [HDR] _BaseColor ("主颜色", color) = (1,1,1,1)
        
        [Sub(Main)] _AlphaRemap ("透明度重映射 - 透明度将先 *x，然后 +y，zw 没用", vector) = (1,0,0,0)
        [Sub(Main)] _MulAlphaToRGB ("0 = 透明度=1，1 = 最终混合的透明度", float) = 0
        
        [SubToggle(Main, _FRAC_UV_ENABLE)] _FracUVEnable ("uv 只取小数，切换它可以用来消除 Tiling 带来的边缘接缝问题", float) = 0
        
        // 防止侧面拉伸（将投影方向与场景法线进行比较，并根据需要丢弃）
        [SubToggle(Main, _PROJECTION_ANGLE_DISCARD_ENABLE)] _ProjectionAngleDiscardEnable ("启用投影角度丢弃", float) = 0
        [Sub(Main)] _ProjectionAngleDiscardThreshold ("投影角度丢弃阈值", range(-1,1)) = 0
        
        // 不常修改的附加设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Additive, __, off, off)]
        _Additive ("附加设置", float) = 1
        
        [Sub(Additive)] _StencilRef ("模版引用值", float) = 0
        [SubEnum(Additive, UnityEngine.Rendering.CompareFunction)] _StencilComp ("模版比较方式 - 如果要按特定值屏蔽，请设置为 NotEqual，否则设置为 Disable", float) = 0
        
        [SubToggle(Additive, _SUPPORT_ORTHOGRAPHIC_CAMERA)] _SupportOrthographicCamera ("支持正交摄像机", float) = 0
    }
    
    SubShader
    {
        // 为了避免渲染顺序问题, Queue必须 >= 2501, 它位于透明队列中
        // 在透明队列中，Unity总是从后到前渲染
        // 从最远的开始渲染，到最近的结束
        Tags
        {
            "RenderType" = "Overlay"
            "Queue" = "Transparent-499"
            "DisableBatching" = "True"
        }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite off // 为了支持透明度混合，关闭深度写入
            
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
            }
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #pragma target 3.0 // 为了使用 ddx() & ddy()
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local_fragment _FRAC_UV_ENABLE
            #pragma shader_feature_local_fragment _PROJECTION_ANGLE_DISCARD_ENABLE
            #pragma shader_feature_local_fragment _SUPPORT_ORTHOGRAPHIC_CAMERA

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            struct appdata
            {
                float3 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                
                // 屏幕坐标
                float4 screenPos : TEXCOORD0;
                
                // xyz 分量: 表示 viewRayOS, 即 模型空间 下的摄像机到顶点的射线
                // w 分量: 拷贝 positionVS.z 的值，即 观察空间 下的顶点坐标的z分量
                float4 viewRayOS : TEXCOORD1;
                
                // rgb 分量：表示 模型空间 下的摄像机坐标
                float4 cameraPosOSAndFogFactor : TEXCOORD2;

                float3 positionWS: TEXCOORD3;
                UBPA_FOG_COORDS(4)
            };

            sampler2D _BaseMap;
            sampler2D _CameraDepthTexture;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half2 _AlphaRemap;
                half _MulAlphaToRGB;
                float _ProjectionAngleDiscardThreshold;
            CBUFFER_END
            
            v2f vert(appdata Input)
            {
                v2f Out = (v2f)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(Input.positionOS);
                Out.positionCS = vertexInput.positionCS;
                
                Out.screenPos = ComputeScreenPos(Out.positionCS);

                // 观察空间坐标，即在观察空间中摄像机到顶点的射线向量
                float3 viewRay = vertexInput.positionVS;

                // [注意，这一步很关键]
                //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                // viewRay除以z分量必须在片元着色器中执行，不能在顶点着色器中执行! (由于光栅化变化插值的透视校正)
                // 我们先把 viewRay.z 存到 Out.viewRayOS.w 中，等到片元着色器阶段在进行处理
                Out.viewRayOS.w = viewRay.z;
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                // 观察空间 到 模型空间 的变换矩阵
                float4x4 ViewToObjectMatrix = mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V);
                // unity 的相机空间是右手坐标系(z轴负方向指向屏幕)，我们希望片段着色器中z射线是正的，所以取反
                viewRay *= -1;

                // 观察空间 转 模型空间
                Out.viewRayOS.xyz = mul((float3x3)ViewToObjectMatrix, viewRay);
                // 模型空间 下摄像机的坐标
                Out.cameraPosOSAndFogFactor.xyz = mul(ViewToObjectMatrix, float4(0, 0, 0, 1)).xyz;

                Out.positionWS = vertexInput.positionWS;
                UBPA_TRANSFER_FOG(Out, vertexInput.positionWS);
                
                return Out;
            }

            half4 frag(v2f Input) : SV_Target
            {
                // [注意，这一步很关键]
                //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                // 去齐次
                Input.viewRayOS.xyz /= Input.viewRayOS.w;
                //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                // 深度纹理的UV
                float2 screenSpaceUV = Input.screenPos.xy / Input.screenPos.w;
                // 对深度纹理进行采样，得到深度信息
                float sceneRawDepth = tex2D(_CameraDepthTexture, screenSpaceUV).r;

                float3 decalSpaceScenePos;

                // 正交相机
                #if _SUPPORT_ORTHOGRAPHIC_CAMERA

                // 我们必须支持正交和透视两种投影
                // unity_OrthoParams：是内置着色器遍历，存储的信息如下：
                //      x 是正交摄像机的宽度，
                //      y 是正交摄像机的高度，
                //      z 未使用，
                //      w 在摄像机为正交模式时是 1.0，而在摄像机为透视模式时是 0.0。
                if(unity_OrthoParams.w)
                {
                    // 如果是正交摄像机, _CameraDepthTexture 在 [0,1] 内线性存储场景深度
                    #if defined(UNITY_REVERSED_Z)
                    // 如果 platform 使用反向深度，要使用 1 - depth
                    // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                    sceneRawDepth = 1 - sceneRawDepth;
                    #endif

                    // 使用简单的 lerp 插值： lerp(near, far, [0,1] linear depth)， 得到 观察空间 的深度信息               
                    float sceneDepthVS = lerp(_ProjectionParams.y, _ProjectionParams.z, sceneRawDepth);

                    // 投影
                    float2 viewRayEndPosVS_xy = float2(unity_OrthoParams.xy * (Input.screenPos.xy - 0.5) * 2);  
                    // 构建观察空间坐标
                    float4 vposOrtho = float4(viewRayEndPosVS_xy, -sceneDepthVS, 1);
                    // 观察空间 转 世界空间
                    float3 wposOrtho = mul(UNITY_MATRIX_I_V, vposOrtho).xyz;
                    // 世界空间 转 模型空间(贴花空间)
                    decalSpaceScenePos = mul(GetWorldToObjectMatrix(), float4(wposOrtho, 1)).xyz;
                }
                else
                    
                #endif // _SUPPORT_ORTHOGRAPHIC_CAMERA
                    
                {
                    // 如果是透视相机，LinearEyeDepth 将处理一切
                    // 记住，我们不能使用 LinearEyeDepth 处理正交相机!
                    // _ZBufferParams: 用于线性化 Z 缓冲区值。
                    //      x 是 (1-远/近)，
                    //      y 是 (远/近)，
                    //      z 是 (x/远)，
                    //      w 是 (y/远)。
                    float sceneDepthVS = LinearEyeDepth(sceneRawDepth, _ZBufferParams);

                    // 在任何空间中，场景深度 = rayStartPos + rayDir * rayLength
                    // 注意，viewRayOS 不是一个单位向量，所以不要规一化它，它是一个方向向量，视图空间z的长度是1
                    decalSpaceScenePos = Input.cameraPosOSAndFogFactor.xyz + Input.viewRayOS.xyz * sceneDepthVS;
                }

                // unity 的 cube 的顶点坐标范围是 [-0.5, 0.5,]，我们把它转到 [0,1] 的范围，用于映射UV
                // 只有你使用 cube 作为 mesh filter 时才能这么干
                float2 decalSpaceUV = decalSpaceScenePos.xy + 0.5;

                // 剔除逻辑
                //===================================================
                // 剔除在 cube 以外的像素信息
                float shouldClip = 0;

                #if _PROJECTION_ANGLE_DISCARD_ENABLE
                // 也丢弃 “场景法向不面对贴花投射器方向” 的像素
                // 使用 ddx 和 ddy 重建场景法线信息
                // ddx 就是右边的像素块的值减去左边像素块的值，而 ddy 就是下面像素块的值减去上面像素块的值。
                // ddx 和 ddy 的结果就是副切线和切线方向，利用右手定理，叉乘后就是法线，最后执行归一化得到法线单位向量
                float3 decalSpaceHardNormal = normalize(cross(ddx(decalSpaceScenePos), ddy(decalSpaceScenePos)));

                // 判断是否进行剔除
                shouldClip = decalSpaceHardNormal.z > _ProjectionAngleDiscardThreshold ? 0 : 1;
                #endif // _PROJECTION_ANGLE_DISCARD_ENABLE

                // 执行剔除
                // 如果 ZWrite 关闭，在移动设备上 clip() 函数是足够效率的，因为它不会写入深度缓冲，所以GPU渲染管线不会卡住（经过ARM官方人员确认过）
                clip(0.5 - abs(decalSpaceScenePos) - shouldClip);
                //===================================================

                // 贴花UV计算
                // _xxx_ST.xy: 表示 uv 的 tilling
                // _xxx_ST.zw: 表示 uv 的 offset     
                float2 uv = decalSpaceUV.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

                #if _FRAC_UV_ENABLE
                uv = frac(uv); // uv只取小数部分
                #endif

                // 贴花纹理采样
                half4 col = tex2D(_BaseMap, uv);
                col *= _BaseColor;
                col.a = saturate(col.a * _AlphaRemap.x + _AlphaRemap.y); // 透明通道重新映射
                col.rgb *= lerp(1, col.a, _MulAlphaToRGB); // 插值

                UBPA_APPLY_FOG(Input, col);
                
                return col;
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}