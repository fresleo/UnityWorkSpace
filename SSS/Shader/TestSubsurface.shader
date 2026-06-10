// =============================================================================
//  TestSubsurface.shader
//  desc : 配合 SubsurfaceScatteringCustomPass 使用的「自包含」HDRP 测试 Shader。
//         挂到球 / 头模上即可端到端验证整条 SSS 链路。
//
//  与 CustomPass 的契约（务必一致）：
//    · ShaderTag      : "SubsurfaceDiffuse"（对应 CustomPass.subsurfaceDiffuseTag 默认值）
//    · Split MRT 布局 : SV_Target0 = 漫反射「辐照度」(rgb) + coverage(a=1)
//                       SV_Target1 = 漫反射「反照率」albedo (rgb)
//    · 全局量(由 PushMainLightGlobals 写入)：
//         _SSSMainLightDir.xyz   = 指向光源的方向(已归一化)
//         _SSSMainLightColor.rgb = color * intensity
//
//  三个 Pass：
//    1) DepthForwardOnly : 写深度，使 CustomPass 注入点(AfterOpaqueAndSky)的相机深度
//                          里有本物体 —— SubsurfaceDiffuse 的 ZTest Equal 才能命中。
//    2) ForwardOnly      : 物体的「可见」前向 Pass，按「避免重复计」约定，只输出
//                          高光 + 环境填充，【不含 Lambert 漫反射】。
//                          → 漫反射全部由 CustomPass 的散射结果加法叠回。
//    3) SubsurfaceDiffuse : SSS 数据源，向 MRT 写「辐照度 / 反照率」。
//
//  ★ 这正好是一个干净的 A/B 测试：关掉 CustomPass，物体只有高光+微弱环境(几乎无漫反射)；
//    打开 CustomPass，柔和的次表面漫反射就会叠加上来。把 _AmbientStrength 调 0 对比最明显。
//
//  ⚠ 说明：这是手写的最小 HDRP 前向 Shader(Unlit 风格：ForwardOnly + DepthForwardOnly)，
//     光照只评估了 1 个主平行光，没有走 HDRP 完整 light loop / 阴影 / GI。
//     生产请换成你引擎框架内的完整光照；这里的重点是把 CustomPass 跑通。
//  ⚠ 曝光：建议场景 Exposure 用 Fixed(=0)，让 pre-exposure 系数为 1，
//     这样本 Shader 的输出与 CustomPass 的加法合成在同一线性尺度上、不串味。
//  ⚠ 包含路径/SHADERPASS 宏按 HDRP 17.3.0 写；若你的包版本报错，按下方注释微调。
// =============================================================================
Shader "Illusion/SSS/TestSubsurface"
{
    Properties
    {
        [MainColor]   _BaseColor      ("Base Color", Color)        = (0.85, 0.6, 0.55, 1)
        [MainTexture] _BaseColorMap   ("Base Color Map", 2D)       = "white" {}
        _Smoothness                   ("Smoothness", Range(0,1))   = 0.5
        _SpecularTint                 ("Specular Tint", Color)     = (1,1,1,1)
        _AmbientColor                 ("Ambient (test fill)", Color)        = (0.10, 0.12, 0.16, 1)
        _AmbientStrength              ("Ambient Strength", Range(0,2))      = 0.25
    }

    HLSLINCLUDE
    // ---- HDRP 公共头（最小集）---------------------------------------------
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    // 用于拿 SHADERPASS 枚举值；我们不依赖 SHADERPASS 分支，只是满足 ShaderVariables 头
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
    #define SHADERPASS SHADERPASS_FORWARD_UNLIT
    // 提供 TransformObjectToWorld / TransformWorldToHClip / GetWorldSpaceNormalizeViewDir
    // 以及相机相对渲染(camera-relative)所需的矩阵与变量
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    // ---- 材质属性(SRP Batcher 兼容)--------------------------------------
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _BaseColorMap_ST;
        float4 _SpecularTint;
        float4 _AmbientColor;
        float  _Smoothness;
        float  _AmbientStrength;
    CBUFFER_END

    TEXTURE2D(_BaseColorMap); SAMPLER(sampler_BaseColorMap);

    // ---- 由 SubsurfaceScatteringCustomPass.PushMainLightGlobals 写入(全局)---
    float4 _SSSMainLightDir;     // xyz = 指向光源
    float4 _SSSMainLightColor;   // rgb = color * intensity
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        // =====================================================================
        //  Pass 1 —— 深度预通道：让本物体进入相机深度缓冲
        //  (ColorMask 0：只写深度，避免与 HDRP 深度预通道的 MRT 布局冲突)
        // =====================================================================
        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode" = "DepthForwardOnly" }

            ZWrite On
            ZTest  LEqual
            Cull   Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   VertDepth
            #pragma fragment FragDepth
            #pragma target   4.5

            struct AttributesD { float3 positionOS : POSITION; };
            struct VaryingsD   { float4 positionCS : SV_POSITION; };

            VaryingsD VertDepth(AttributesD v)
            {
                VaryingsD o;
                float3 positionRWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(positionRWS);
                return o;
            }

            float4 FragDepth(VaryingsD i) : SV_Target { return 0; }
            ENDHLSL
        }

        // =====================================================================
        //  Pass 2 —— 可见前向：只输出「高光 + 环境填充」，不含 Lambert 漫反射
        //  (漫反射交给 CustomPass 的散射结果加法叠回，避免重复计)
        // =====================================================================
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZWrite On
            ZTest  LEqual
            Cull   Back
            Blend  Off

            HLSLPROGRAM
            #pragma vertex   VertFwd
            #pragma fragment FragFwd
            #pragma target   4.5

            struct AttributesF
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VaryingsF
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionRWS : TEXCOORD1;   // 相机相对世界坐标，用于算视线
                float2 uv          : TEXCOORD2;
            };

            VaryingsF VertFwd(AttributesF v)
            {
                VaryingsF o;
                o.positionRWS = TransformObjectToWorld(v.positionOS);
                o.positionCS  = TransformWorldToHClip(o.positionRWS);
                o.normalWS    = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.uv          = TRANSFORM_TEX(v.uv, _BaseColorMap);
                return o;
            }

            float4 FragFwd(VaryingsF i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 L = normalize(_SSSMainLightDir.xyz);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionRWS);
                float3 H = normalize(L + V);

                float  NdotL = saturate(dot(N, L));
                float  NdotH = saturate(dot(N, H));

                // 简易 Blinn-Phong 高光(测试级，非 PBR)
                float  shininess = exp2(10.0 * _Smoothness + 1.0);
                float  specTerm  = pow(NdotH, shininess) * NdotL;
                float3 specular  = _SpecularTint.rgb * _SSSMainLightColor.rgb * specTerm;

                // 环境填充(纯测试用，仅为让暗面也能看见轮廓；与 albedo 无关)
                float3 ambient   = _AmbientColor.rgb * _AmbientStrength;

                // 注意：这里【不输出】Lambert 漫反射 —— 由 SSS 散射结果叠回
                return float4(specular + ambient, 1.0);
            }
            ENDHLSL
        }

        // =====================================================================
        //  Pass 3 —— SubsurfaceDiffuse：SSS 数据源，写入 Split MRT
        //  ZTest Equal：只命中相机深度里最前的可见片元(已由前两 Pass 写好深度)
        //  若你的工程没跑深度预通道、导致什么都没出现，可临时改成 ZTest LEqual。
        // =====================================================================
        Pass
        {
            Name "SubsurfaceDiffuse"
            Tags { "LightMode" = "SubsurfaceDiffuse" }

            ZWrite Off
            ZTest  Equal
            Cull   Back
            Blend  Off

            HLSLPROGRAM
            #pragma vertex   VertSSS
            #pragma fragment FragSSS
            #pragma target   4.5

            struct AttributesS
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            struct FragOutput
            {
                float4 diffuse : SV_Target0;   // rgb = 漫反射辐照度, a = coverage(=1)
                float4 albedo  : SV_Target1;   // rgb = 反照率
            };

            VaryingsS VertSSS(AttributesS v)
            {
                VaryingsS o;
                float3 positionRWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(positionRWS);
                o.normalWS   = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.uv         = TRANSFORM_TEX(v.uv, _BaseColorMap);
                return o;
            }

            FragOutput FragSSS(VaryingsS i)
            {
                float3 N = normalize(i.normalWS);
                float3 L = normalize(_SSSMainLightDir.xyz);

                // —— 只算主光直接漫反射「辐照度」（不乘 albedo！）——
                //    生产环境在此替换为多光源 + 阴影 + GI
                float  NdotL      = saturate(dot(N, L));
                float3 irradiance = _SSSMainLightColor.rgb * NdotL;

                float3 albedo = _BaseColor.rgb
                              * SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, i.uv).rgb;

                FragOutput o;
                o.diffuse = float4(irradiance, 1.0);   // a=1：标记此处为 SSS 像素(coverage)
                o.albedo  = float4(albedo, 1.0);
                return o;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
