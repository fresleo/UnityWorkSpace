Shader "Hidden/XKnight/SSSDebug"
{
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D_X(_SSSDebugOutput);
            float _DebugChannel;   // 0=raw depth, 1=linear01(归一化), 2=stencil mask
            float _DepthScale;     // linear 深度归一化系数, 比如 1/远平面米数

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(i.vertexID);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                uint2 pc = uint2(i.positionCS.xy);
                float4 d = LOAD_TEXTURE2D_X(_SSSDebugOutput, pc);

                // float v;
                // if      (_DebugChannel == 0) v = d.x;               
                // else if (_DebugChannel == 1) v = saturate(d.y * _DepthScale); 
                // else                         v = d.z;
                return d;
            }
            ENDHLSL
        }
    }
}