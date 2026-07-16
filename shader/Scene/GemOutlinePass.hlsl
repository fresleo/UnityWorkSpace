#ifndef GEM_OUTLINEPASS_INCLUDED
#define GEM_OUTLINEPASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float4 color      : TEXCOORD1;
    // DISSOLVE_FACTOR(2)

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float outline_lerp(float start, float end, float Z_start, float Z_end, float Z)
{
    float t = (Z - Z_start) / max(Z_end - Z_start, 0.001); // linear 
    t = clamp(t, 0.0f, 1.0f);
    return lerp(start, end, t);
}

Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.uv = input.uv;
    
    float3 normalOS = input.color.xyz * 2.0f - 1.0f;
    
    float sign = input.tangentOS.w * GetOddNegativeScale();
    float3 binormalOS = cross(input.normalOS, input.tangentOS.xyz) * sign;

    #ifndef _MESH_PREVIEW_MODE
    normalOS = mul(normalOS, float3x3(input.tangentOS.xyz, binormalOS, input.normalOS));
    #endif
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    float4 widthScales = float4(0.105, 0.245, 0.6, 0);
    float4 widthAdj = float4(0.01, 2.0, 6.0, 0);

    float fov = 1;
    if(GetViewToHClipMatrix()[3].w) // perspective check
    {
        fov = 0.5; // perspective off
    } 
    else
    {
        fov = -2.414 / GetViewToHClipMatrix()[1].y;
    }

    // 平台差异性
    #if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
    fov = -fov;
    #endif

    float depth = -vertexInput.positionVS.z * fov;
    bool outline_depth = depth < widthAdj.y;
                
    float4 widthZs = 1;// initialize
    widthZs.xy = outline_depth ?  widthAdj.xy : widthAdj.yz;
    widthZs.zw = outline_depth ?  widthScales.xy : widthScales.yz;

    float offset = outline_lerp(widthZs.z, widthZs.w, widthZs.x, widthZs.y, depth);

    float outline_offset = offset * _OutlineWidth * 1.82 * 0.03 * 0.41425 * input.color.a / 5.0;

    float3 normalWS = TransformObjectToWorldNormal(normalOS, false);
    float3 normalVS = TransformWorldToViewDir(normalWS, false);
    vertexInput.positionVS.xy += normalize(float3(normalVS.x, normalVS.y, 0.01f)).xy * outline_offset;
    output.positionCS = TransformWViewToHClip(vertexInput.positionVS);

    // DISSOLVE_TRANSFER_FACTOR(output, input.positionOS, _DissolveDir)
    
    return output;
}

// 处理完后无需先隐藏描边在溶解了，后续有需要了可以配合相关人员打开
half4 Fragment(Varyings input /*, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC */) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif
    
//     half4 fakeColor = (half4)0;
//     DISSOLVE_APPLY(fakeColor, input.uv, input.directionFactor)
//
// #if _RANDOM_DISSOLVE_ON
//     clip(face);
// #endif    
    
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    return _OutlineColor * albedo;
}

#endif // GEM_OUTLINEPASS_INCLUDED
