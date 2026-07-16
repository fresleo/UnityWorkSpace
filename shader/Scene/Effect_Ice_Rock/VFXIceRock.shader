Shader "XKnight/Particle/VFXIceRock"
{
    Properties
    {
        // [Header(Manager)] [Space(6)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("剔除模式", Float) = 2
        [Toggle] _ZWriteMode ("深度写入", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("深度测试", Float) = 4



        // [Header(Main)] [Space(6)]
        [HDR] _Color ("主颜色", Color) = (1,1,1,1)
        _MainTex ("主纹理", 2D) = "white" {}
        _ParallaxDepth ("主纹理视差深度", Range(0, 2)) = 0

        // [Header(Normal)] [Space(6)]
        [Toggle(_NORMAL_ON)] _Normal ("法线", int) = 0
        _NormalTex ("法线贴图", 2D) = "bump" {}
        _NormalIntensity ("法线强度", Range(0, 2)) = 1

        // [Header(Render)] [Space(6)]
        [KeywordEnum(PBR, Matcap)] _RenderMode ("渲染模式", int) = 0
        _DiffuseOffset ("漫反射偏移", Range(0, 1)) = 0.5
        _DiffuseFeather ("漫反射羽化", Range(0, 1)) = 0.5
        _DiffuseIntensity ("漫反射强度", Range(0, 1)) = 0.5

        // [Header(PBR)] [Space(6)]
        _MetallicRoughnessMap ("金属度R/粗糙度G 贴图", 2D) = "red"{}
        _Metallic ("金属度", Range(0, 1)) = 0
        _Smoothness ("平滑度", Range(0, 1)) = 1

        // [Header(Matcap)] [Space(6)]
        [Enum(Add, 0, Blend, 1)] _MatcapBlend ("Matcap混合模式", int) = 0
        [HDR] _MatcapColor ("MatcapColor", Color) = (1,1,1,1)
        _MatcapTex ("Matcap贴图", 2D) = "black" {}


        // [Header(Emission)] [Space(6)]
        [Toggle(_EMISSION_ON)] _Emission ("自发光", int) = 0
        [HDR] _EmissionColor ("自发光颜色", Color) = (0,0,0,1)
        _EmissionTex ("自发光贴图", 2D) = "white" {}

        
        // [Header(OpaqueBlend)] [Space(6)]
        [Toggle(_OPAQUEALPHABLEND_ON)] _OpaqueAlphaBlend ("与不透明物体混合", int) = 0
        _OpaqueBlendDistance ("不透明物体混合距离", Range(0, 2)) = 0.1


        // [Header(Reflection)] [Space(6)]
        [Toggle(_CUBEMAPREFLECTION_ON)] _CubeMapReflection ("Cube贴图反射", int) = 0
        [HDR] _CubeColor ("CubeColor", Color) = (1,1,1,1)
        _CubeTex ("Cube贴图", Cube) = "black" {}
        _CubeTexMipLevel ("Cube贴图Mipmap", Range(0, 1)) = 0


        // [Header(Fresnel)] [Space(6)]
        [Toggle(_FRESNEL_ON)] _Fresnel ("菲涅尔", int) = 0
        [HDR] _FresnelColor ("菲涅尔颜色", Color) = (1,1,1,1)
        _FresnelPower ("菲涅尔尺寸", float) = 2
        _FresnelScale ("菲涅尔强度", float) = 1
        [Toggle] _FresnelReverse ("菲涅尔反转", int) = 0
        _FresnelDiffuseScale ("菲涅尔混合漫反射", Range(0, 1)) = 0


        // [Header(Streamer)] [Space(6)]
        [Toggle(_STREAMER_ON)] _Streamer("流光", int) = 0
        _StreamerColor ("流光颜色", Color) = (1,1,1,1)
        _StreamerTex ("流光贴图", 2D) = "black" {}
        _StreamerMask ("流光遮罩", 2D) = "white" {}
        [Enum(R, 0, G, 1, B, 2, A, 3)] _StreamerMaskChannal ("流光遮罩通道", int) = 0
        // _StreamerSpeedX ("流光速度-水平", float) = 0
        // _StreamerSpeedY ("流光速度-垂直", float) = 0




        
        
        // [Header(Stencil)] [Space(6)]
        // [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 0
        // [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 0
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
        // [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
        // [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0
        // _ColorMask ("Color Mask", Float) = 15
    }
    
    HLSLINCLUDE
    
    

    ENDHLSL

    SubShader
    {
        LOD 300
        
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        // ForwardLit - UniversalForward
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            // Stencil
            // {
            //     Ref [_Stencil]
            //     Comp [_StencilComp]
            //     Pass [_StencilOp]
            //     ReadMask [_StencilReadMask]
            //     WriteMask [_StencilWriteMask]
            //     Fail [_StencilFail]
            //     ZFail [_StencilZFail]
            // }

            // ColorMask [_ColorMask]
            
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            
            Cull [_CullMode]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            // #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _RENDERMODE_PBR _RENDERMODE_MATCAP
            #pragma shader_feature _ _NORMAL_ON
            #pragma shader_feature _ _EMISSION_ON
            #pragma shader_feature _ _OPAQUEALPHABLEND_ON
            #pragma shader_feature _ _CUBEMAPREFLECTION_ON
            #pragma shader_feature _ _FRESNEL_ON
            #pragma shader_feature _ _STREAMER_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            //--------------------------------------
            // Include To HLSL
            #include "./VFXInputIce.hlsl"
            #include "./VFXCoreIce.hlsl"
            


            // 简化2D贴图的采样方式
            #define SamplerMap2D(map, uv) (SAMPLE_TEXTURE2D(map, sampler ## map, uv * map ## _ST.xy + map ## _ST.zw))

            
            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // UV
                float2 uv = i.uv.xy;
                float2 uvParallax = ParallaxUV(uv, i.viewDirTS, _ParallaxDepth);

                // 灯光
                Light light = GetMainLight();
                float3 lightColor = light.color;
                float3 lightDirWS = light.direction;
                
                // 颜色与法线
                float4 color = SamplerMap2D(_MainTex, uvParallax) * _Color;
                float3 normal = GetNormal(i);
                
                // 向量
                float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - i.vertexWS);
                float3 vrDirWS = normalize(reflect(-viewDirWS, normal));
                
                // 点乘
                half nDotL = dot(normal, lightDirWS);
                half nDotV = dot(normal, viewDirWS);

                // 漫反射
                float diffuseMask = CalculateDiffuse(nDotL, _DiffuseOffset, _DiffuseFeather);
                float diffuse = lerp(1, diffuseMask, _DiffuseIntensity);
                
                // 输出颜色
                float4 returnColor = color;

                // 如果处于PBR渲染模式
                #ifdef _RENDERMODE_PBR
                {
                    // //以下部分解除注释可用
                    // float4 mAndR = SamplerMap2D(_MetallicRoughnessMap, uv);
                    //
                    // float metallic = _Metallic * mAndR.r;
                    // float smoothness = _Smoothness * RoughnessToSmoothness(mAndR.g);;
                    // float roughness = SmoothnessToRoughness(_Smoothness);
                    //
                    // float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                    //
                    // half nDotH = dot(normal, halfDirWS);
                    // half lDotH = dot(lightDirWS, halfDirWS);
                    //
                    // float specular = BRDF_Specular(roughness, nDotH, lDotH);
                    //
                    // float3 ambient = SampleSH(normal);
                    // float3 diffuseColor = lerp(0.96 * returnColor.rgb, 0, metallic);
                    // float3 specularColor = lerp(0.04, returnColor.rgb, metallic);
                    // float3 enviromentColor = Box0ReflectColor(vrDirWS, smoothness);
                    //
                    // float3 IBL = EnvironmentBRDF(diffuseColor, specularColor, ambient, enviromentColor, 
                    //     metallic, smoothness, roughness, nDotV);
                    //
                    // float3 pbr = diffuseColor * diffuse + specularColor * specular * diffuse + IBL;
                    // returnColor.rgb = pbr;
                    
                    // 以下部分接入ShaderLibrary的PBR
                    float4 mAndR = SamplerMap2D(_MetallicRoughnessMap, uv);
                    float metallic = _Metallic * mAndR.r;
                    float smoothness = _Smoothness * RoughnessToSmoothness(mAndR.g);;

                    BRDFData brdf = InitializeBRDFData(color, metallic, smoothness);
                    float3 pbr = GlobalIllumination_UE(brdf, i.vertexWS, normal, viewDirWS, 0);
                    pbr += LightingPhysicallyBased_XK(brdf, light, diffuse);
                    returnColor.rgb = pbr;
                    
                }
                // 如果处于Matcap渲染模式
                #elif _RENDERMODE_MATCAP
                {
                    float2 uvMatcap = GetMatcapUV(normal);
                    float3 matcapTex = SamplerMap2D(_MatcapTex, uvMatcap).xyz;
                    returnColor.rgb = lerp(returnColor.rgb + matcapTex, returnColor.rgb * matcapTex, _MatcapBlend);
                    returnColor.rgb *= diffuse;
                }
                #endif

                // 盒状贴图叠加
                #ifdef _CUBEMAPREFLECTION_ON
                {
                    float3 cube = Sample_ReflectProbeCubeMap(_CubeTex, sampler_CubeTex, _CubeTex_HDR, vrDirWS, _CubeTexMipLevel * 7).rgb;
                    returnColor.rgb += cube * _CubeColor.rgb;
                }
                #endif

                // 菲涅尔
                #ifdef _FRESNEL_ON
                {
                    float fresnel = lerp(1 - nDotV, nDotV, _FresnelReverse);
                    fresnel *= lerp(1, diffuseMask, _FresnelDiffuseScale);
                    ApplyFresnel(returnColor, fresnel, _FresnelColor, _FresnelScale, _FresnelPower);
                }
                #endif

                // 深度距离混合透明度
                #ifdef _OPAQUEALPHABLEND_ON
                {
                    float2 uvDepth = clamp(i.vertex.xy, 0, _ScreenParams.xy - 1);
                    float depthEyeCurr = i.vertex.w;
                    float depthEyeOpaque = LinearEyeDepth(LOAD_TEXTURE2D_X(_CameraDepthTexture, uvDepth).r, _ZBufferParams);

                    float alphaDepth = smoothstep(0, _OpaqueBlendDistance, depthEyeOpaque - depthEyeCurr);
                    returnColor.a *= alphaDepth;
                }
                #endif

                // 自发光
                #ifdef _EMISSION_ON
                {
                    float4 emission = SamplerMap2D(_EmissionTex, uv) * _EmissionColor;
                    returnColor.rgb += emission.rgb * emission.a;
                }
                #endif

                // 流光
                #ifdef _STREAMER_ON
                {
                    float2 uvStreamer = float2(frac(_Time.y * 0.1 * _StreamerTex_ST.z), frac(_Time.y * 0.1 * _StreamerTex_ST.w));
                    float4 streamerTex = SamplerMap2D(_StreamerTex, uv + uvStreamer) * _StreamerColor;
                    float streamerMask = SamplerMap2D(_StreamerMask, uv)[_StreamerMaskChannal];
                    returnColor.rgb += streamerTex.rgb * streamerMask;
                }
                #endif



                
                return saturate(returnColor);
            }
            
            ENDHLSL
        }

    }

    CustomEditor "VFX.VFXIceInspector"
}



// 以下是Cursor的AI生成GUI的形容词
// 请帮我为当前Shader修改GUI脚本，要求：
// 1、“// [Header(Main)] [Space(6)]”类似格式的部分为一组信息的开头，请将其分配到一个折叠组，并将"[Header(Main)]"内的英文标题翻译为中文，
// 作为折叠组标题
// 2、在参数未被应用时，不可以调整参数
// 3、_RenderMode用于控制渲染模式，请将PBR与Matcap作为其子分类，子分类不需要折叠，这两个子分类的标题仍为PBR和Matcap
// 4、每个组需要使用Box框住，组间隔4单位
// 5、折叠组的组标题高26单位 
// 6、贴图不以迷你状态展示
// 7、使用EditorGUILayout.GetControlRect方法获取折叠组的Rect
// 8、法线作为单独的折叠组，不放到Render下
// 9、将RenderQueue放在Manager折叠组下