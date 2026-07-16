Shader "XKnight/Scene/TerrainVTDiffuse"
{
    Properties
    {
        _Control("Control (RGBA)", 2D) = "white" {}
        
        _Splat0("Layer 1", 2D) = "white" {}
        _Splat1("Layer 2", 2D) = "white" {}
        _Splat2("Layer 3", 2D) = "white" {}
        _Splat3("Layer 4", 2D) = "white" {}
    }
    
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
            //为了能支持低端的 gles 2.0 ，用 HLSLcc 来编译 gles 2.0
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #include "./TerrainVTDiffuse.hlsl"
            ENDHLSL
        }
    }
}