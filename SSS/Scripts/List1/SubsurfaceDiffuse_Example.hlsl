// =============================================================================
//  SubsurfaceDiffuse_Example.hlsl  (模板 / 示例)
//  desc : SSS 材质 Shader 中需要新增的一个 Pass，作为 Custom Pass 三段式流程的
//         「Stage 1 Split Lighting」数据源。
//
//  输出（MRT，由 SubsurfaceScatteringCustomPass 绑定）：
//     SV_Target0 = 漫反射「辐照度」(不含反照率), a = 1 (coverage)
//     SV_Target1 = 漫反射「反照率」(albedo)
//
//  随后 Custom Pass 会：对 Target0 做 disc 散射 → 乘回 Target1 → 加法叠加回相机颜色。
//  因此你的「材质主 Pass」应只输出 高光/透射/环境 等非漫反射分量，避免漫反射被计两次。
//
//  ⚠ 这是可编译的最小模板，光照只用了 1 个主平行光(由 Custom Pass 写入的全局量)。
//     生产环境请把这里替换成 HDRP light loop / 你引擎框架内的多光源 + 阴影评估。
//     把下面整段 Pass 粘进你的 .shader 的 SubShader 内即可。
// =============================================================================

/*
Pass
{
    Name "SubsurfaceDiffuse"
    Tags { "LightMode" = "SubsurfaceDiffuse" }

    // 只读深度做 ZTest；HDRP 有 depth prepass，用 Equal 精确命中可见片元
    ZWrite Off
    ZTest  Equal
    Cull   Back
    Blend  Off

    HLSLPROGRAM
    #pragma vertex   Vert
    #pragma fragment Frag
    #pragma target   4.5

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

    // 由 SubsurfaceScatteringCustomPass.PushMainLightGlobals 写入
    float4 _SSSMainLightDir;    // xyz = 指向光源的方向(已归一化建议)
    float4 _SSSMainLightColor;  // rgb = color * intensity

    // 材质属性（按需替换成你的）
    TEXTURE2D(_BaseColorMap); SAMPLER(sampler_BaseColorMap);
    float4 _BaseColor;

    struct Attributes
    {
        float3 positionOS : POSITION;
        float3 normalOS   : NORMAL;
        float2 uv         : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 normalWS   : TEXCOORD0;
        float2 uv         : TEXCOORD1;
    };

    struct FragOutput
    {
        float4 diffuse : SV_Target0;   // 漫反射辐照度, a=1
        float4 albedo  : SV_Target1;   // 反照率
    };

    Varyings Vert(Attributes v)
    {
        Varyings o;
        float3 positionWS = TransformObjectToWorld(v.positionOS);
        o.positionCS = TransformWorldToHClip(positionWS);
        o.normalWS   = normalize(TransformObjectToWorldNormal(v.normalOS));
        o.uv         = v.uv;
        return o;
    }

    FragOutput Frag(Varyings i)
    {
        float3 N = normalize(i.normalWS);
        float3 L = normalize(_SSSMainLightDir.xyz);

        // —— 这里替换成你的完整光照（多光源 + 阴影 + GI）——
        float  NdotL = saturate(dot(N, L));
        float3 irradiance = _SSSMainLightColor.rgb * NdotL;   // 注意：不乘 albedo

        float3 albedo = _BaseColor.rgb
                      * SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, i.uv).rgb;

        FragOutput o;
        o.diffuse = float4(irradiance, 1.0);   // a=1 标记这是 SSS 像素(coverage)
        o.albedo  = float4(albedo, 1.0);
        return o;
    }
    ENDHLSL
}
*/
