#ifndef __MOUNTAINS_META__
#define __MOUNTAINS_META__

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
    
    float4 texcoord0 : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    
    float4 ase_texcoord4 : TEXCOORD4;
    float4 ase_texcoord5 : TEXCOORD5;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput output = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_TRANSFER_INSTANCE_ID( input, output );
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

    output.ase_texcoord4.xy = input.texcoord0.xy;
    output.ase_texcoord4.zw = 0;
    
    output.ase_texcoord5 = input.positionOS;
    
    output.positionCS = MetaVertexPosition( input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );

    return output;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID( input );
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );
    
    #ifdef _ENABLEFOG_ON
    float4 staticSwitch35 = ( _FogColor * saturate( ( ( ( ( 1.0 - input.ase_texcoord5.xyz.y ) + _Height ) * 0.1 ) * ( _Density * 0.1 ) ) ) );
    #else
    float4 staticSwitch35 = float4( 0,0,0,0 );
    #endif

    half4 albedoTex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.ase_texcoord4.xy);
    float3 BaseColor = ( albedoTex * _Color ).rgb;
    float3 Emission = staticSwitch35.rgb;

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = BaseColor;
    metaInput.Emission = Emission;

    return UnityMetaFragment(metaInput);
}

#endif
