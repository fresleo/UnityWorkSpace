Shader "MT/OcclusionTest"
{
    Properties
    {
        _InstanceOffset ( "实例的偏移量", Int ) = 0
        _DebugBoxes ( "绘制调试用的 Box", Int ) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        
        Cull Off
        ZClip Off
        ZWrite Off
        ZTest On
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            //#pragma enable_d3d11_debug_symbols

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                int _InstanceOffset;
                int _DebugBoxes;
            CBUFFER_END

            struct GpuData
            {
                int visible;
            };
            
            uniform RWStructuredBuffer<GpuData> TestBuffer : register(u1);

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint inst : SV_InstanceID;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                uint inst : INSTANCE;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings vert(Attributes In)
            {
                Varyings Out;
                UNITY_SETUP_INSTANCE_ID(In);
                UNITY_TRANSFER_INSTANCE_ID(In, Out);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(In.positionOS.xyz);
                Out.positionCS = vertexInput.positionCS;

                Out.inst = In.inst;

                return Out;
            }

            [earlydepthstencil]
            float4 frag(Varyings In) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(In);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(In);
                
                int offset = _InstanceOffset + In.inst;
                TestBuffer[offset].visible = 1;
                
                // 不调试时，直接丢弃
                if (!_DebugBoxes)
                    discard;

                // 颜色就是用来调试的
                return float4(0, 1, 0, 0.3f);
            }
            ENDHLSL
        }
    }
}
