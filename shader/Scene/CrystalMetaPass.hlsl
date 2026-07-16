#ifndef CRYSTAL_METAPASS_INCLUDED
#define CRYSTAL_METAPASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 FragmentMeta(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    float emissionRange;
    InitializeStandardLitSurfaceData(input.uv, surfaceData, emissionRange);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = (brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5);
    metaInput.Emission = surfaceData.emission;
    return UniversalFragmentMeta(input, metaInput);
}

#endif // CRYSTAL_METAPASS_INCLUDED
