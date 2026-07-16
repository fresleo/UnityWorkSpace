#ifndef __SHEET_FOG_META_PASS__
#define __SHEET_FOG_META_PASS__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
	float4 vertexColor : COLOR;
	
    float4 texcoord0 : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
	float4 vertexColor : COLOR;
	
    #ifdef EDITOR_VISUALIZATION
		float4 VizUV : TEXCOORD0;
		float4 LightCoord : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.vertexColor = v.vertexColor;
	
    o.positionCS = MetaVertexPosition(v.positionOS, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST);

    #ifdef EDITOR_VISUALIZATION
		float2 VizUV = 0;
		float4 LightCoord = 0;
		UnityEditorVizData(v.positionOS.xyz, v.texcoord0.xy, v.texcoord1.xy, v.texcoord2.xy, VizUV, LightCoord);
		o.VizUV = float4(VizUV, 0, 0);
		o.LightCoord = LightCoord;
    #endif

    return o;
}

half4 frag(VertexOutput IN) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float4 AlbedoCol = _Albedo * IN.vertexColor;

    float3 BaseColor = float3(0.5, 0.5, 0.5);
    float3 Emission = AlbedoCol.rgb;

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = BaseColor;
    metaInput.Emission = Emission;

    #ifdef EDITOR_VISUALIZATION
		metaInput.VizUV = IN.VizUV.xy;
		metaInput.LightCoord = IN.LightCoord;
    #endif

    return UnityMetaFragment(metaInput);
}

#endif // __SHEET_FOG_META_PASS__
