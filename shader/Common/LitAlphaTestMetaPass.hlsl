#ifndef XKNIGHT_LIT_ALPHA_TEST_META_PASS_INCLUDED
#define XKNIGHT_LIT_ALPHA_TEST_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 FragmentMeta(Varyings input) : SV_Target
{
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);
	
	BRDFData brdfData;
	InitializeBRDFData(surfaceData, brdfData);

	MetaInput metaInput;
    metaInput.Albedo = (brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5);
	metaInput.Emission = surfaceData.emission;
	return UniversalFragmentMeta(input, metaInput);
}

#endif // XKNIGHT_LIT_ALPHA_TEST_META_PASS_INCLUDED
