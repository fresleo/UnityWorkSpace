#ifndef __SIMPLE_LIT_META__
#define __SIMPLE_LIT_META__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 FragmentMeta(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    MetaInput metaInput;
    metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
    metaInput.Emission = half3(0.0, 0.0, 0.0);

    return UniversalFragmentMeta(input, metaInput);
}

#endif // __SIMPLE_LIT_META__
