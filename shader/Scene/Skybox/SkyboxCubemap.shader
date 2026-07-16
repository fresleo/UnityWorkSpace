Shader "XKnight/Scene/Skybox/Cubemap"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 4)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex ("Cubemap  (HDR)", Cube) = "grey" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        
        Cull Off ZWrite Off

        Pass 
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include_with_pragmas "./SkyboxCubemap.hlsl"
            ENDHLSL
        }
    }
}
