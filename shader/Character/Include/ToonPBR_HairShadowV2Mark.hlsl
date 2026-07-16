#ifndef __TOON_PBR_HAIR_SHADOW_V2_MARK__
#define __TOON_PBR_HAIR_SHADOW_V2_MARK__

struct Attributes
{
    float4 positionOS: POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS: SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings HairShadowVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    output.positionCS = positionInputs.positionCS;

    float2 lightOffset = normalize(_LightDirSS.xy);

    // 乘以 _ProjectionParams.x 是考虑裁剪空间 y 轴是否因为 DX 与 OpenGL 的差异而被翻转
    // 参照 https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
    // "Similar to Texture coordinates, the clip space coordinates differ between Direct3D-like and OpenGL-like platforms"
    lightOffset.y *= _ProjectionParams.x;

    float scaleFactor = 0.01;
    
    float offsetX = lightOffset.x * _HairShadowOffsetX * scaleFactor;
    float sx = sign(offsetX);
    if (sx < 0)
    {
        offsetX = abs(offsetX);
    }
    
    output.positionCS.x += offsetX;
    output.positionCS.y += lightOffset.y * _HairShadowOffsetY * scaleFactor;
    output.positionCS.z += _HairShadowOffsetZ * scaleFactor;

    return output;
}

half4 HairShadowFragment(Varyings input): SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return 0;
}

#endif // __TOON_PBR_HAIR_SHADOW_V2_MARK__
