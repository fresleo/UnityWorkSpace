Shader "Hidden/ToonPostProcessing/ViewSpaceNormals"
{
    Properties
    {
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
        }
        
        //Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // UniversalForward
        Pass
        {
            Name "Universal Forward"
            
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;

                return output;
            }
            
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // 法线
                half3 normalVS = mul(input.normalWS, (float3x3) UNITY_MATRIX_I_V);
                
                half3 remapNormal = 0;
                Remap(normalVS, float2(-1, 1), float2(0, 1), remapNormal);

                half4 col = half4(remapNormal, 1); // a是是否有写入的标记，所以这里给1
                return col;
            }
            ENDHLSL
        }
    }
}