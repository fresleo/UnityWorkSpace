#ifndef TOONPBR_EYE
#define TOONPBR_EYE

/*
 *  由于眼球的渲染比较特殊，所以单独拆出来，直截了当的算完diffuse+specular
 */

half3 EyeRender(BRDFDataToon brdfData, SurfaceDataToon surfaceData, InputDataToon inputData)
{
    float2 uv = inputData.texcoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
    
    float parallaxDepth = smoothstep(1, 0.5, distance(uv, float2(0.5, 0.5)) / _PupilSize);
    float3x3 TBN = float3x3(inputData.tangentWS.xyz, inputData.bitangentWS, inputData.normalWS);
    // float3 viewTS = SafeNormalize(mul(TBN, inputData.viewDirectionWS));
    // 视差，模拟折射
    float2 parrallaxUV = lerp(uv, uv - inputData.viewDirTS.xy * _PupilSunken, parallaxDepth);

    half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, parrallaxUV).rgb * _BaseColor.rgb;
    half3 diffuseColor = saturate(saturate(dot(inputData.normalWS, _MainLightPosition.xyz)) * 0.5 + 0.5) * baseColor;

    // specular,用matcap提供
    // TODO 这里有一个防切变的逻辑可以加，目前没必要，因为眼球模型不会scale

    float2 matcapUV = TransformWorldToViewDir(inputData.normalWS).xy * 0.5 + 0.5;
    // 眼球Shader的alpha通道用于标记是否存在specular
    half specularMask = surfaceData.alpha;

    half3 specularColor = SAMPLE_TEXTURE2D(_PupilMatcap, sampler_PupilMatcap, matcapUV).rgb * specularMask * _SpecColor.rgb * _PupilMatcapIntensity;

    return diffuseColor + specularColor;
}

#endif // TOONPBR_EYE
