Shader "XKnight/Scene/TerrainVTBump"
{
    Properties
    {
        _Control("Control (RGBA)", 2D) = "white" {}
        
        _Normal0("Layer 1 Normal", 2D) = "bump" {}
        _Normal1("Layer 2 Normal", 2D) = "bump" {}
        _Normal2("Layer 3 Normal", 2D) = "bump" {}
        _Normal3("Layer 4 Normal", 2D) = "bump" {}
        
        _NormalScale0("Layer 1 Normal Scale", Range(0.0, 10.0)) = 1.0
        _NormalScale1("Layer 2 Normal Scale", Range(0.0, 10.0)) = 1.0
        _NormalScale2("Layer 3 Normal Scale", Range(0.0, 10.0)) = 1.0
        _NormalScale3("Layer 4 Normal Scale", Range(0.0, 10.0)) = 1.0
    }
    
    HLSLINCLUDE
    #pragma multi_compile __ _ALPHATEST_ON
    ENDHLSL
    
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1500" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        
        Lighting Off ZTest Off ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #include "./TerrainVTBump.hlsl"
            ENDHLSL
        }
    }
}