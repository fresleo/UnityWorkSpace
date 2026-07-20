Shader "Hidden/ToonPostProcessing/OutlineDistortMask"
{
    Properties
    {
        _MeshPreview ("未蒙皮的 Mesh 预览", Range(0, 1)) = 0
        
        _OutlineWidth ("轮廓宽度", Float) = 5
        _OutlinePower ("调整轮廓透视", Range(0.1, 1.5)) = 0.6
        
        _OutlineFadeStart ("描边渐隐 - 开始距离", Float) = 0
        _OutlineFadeEnd ("描边渐隐 - 结束距离", Float) = 50
        
        _YAxisOffset ("Y 轴偏移（模型的世界空间）", Float) = 0
        
        _InvertFadeDirection ("反转与视空间法线产生的 dot 值", Range(0, 1)) = 1
        
        _GradientScale ("调整渐变曲线-缩放", Float) = 1
        _GradientLeft ("调整渐变曲线-左值", Float) = 0
        _GradientRight ("调整渐变曲线-右值", Float) = 1
        _GradientPower ("调整渐变曲线-power", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Cull Back
        ZWrite On
        ZTest Always
        ColorMask RG

        Pass
        {
            Name "OutlineDistortMask"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            // -------------------------------------
            // Material Keywords
            #pragma multi_compile_local_vertex _ _MESH_PREVIEW_MODE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"
            #include "./OutlineDistortLib.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _OutlineWidth;
                half _OutlinePower;
                half _OutlineFadeStart, _OutlineFadeEnd;
                
                half _YAxisOffset;
                
                half _InvertFadeDirection;
                
                half _GradientScale, _GradientLeft, _GradientRight, _GradientPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                OUTLINE_ATTRIBUTES

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = input.uv;
                
                float3 normalV3 = input.color.rgb * 2.0 - 1.0;
                float4 normalV4 = float4(normalV3, input.color.a);
                
                VertexPositionInputs vertexInput;
                OutlineVertexPhase(
                    input.positionOS, input.normalOS, input.tangentOS, normalV4, 
                    _OutlineWidth, _OutlinePower, 
                    _YAxisOffset, 
                    vertexInput, output.normalWS);
                
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                
                return output;
            }
            
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // 计算摄像机与像素之间的距离
                float3 cameraWS = _WorldSpaceCameraPos;
                //float3 pixelWS = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                float3 pixelWS = GetAbsolutePositionWS(input.positionWS);
                float dis = distance(cameraWS, pixelWS);

                // 根据距离，计算该何时丢弃像素
                float clipFactor = 0;
                Remap(dis, float2(_OutlineFadeStart, _OutlineFadeEnd), float2 (0, 1), clipFactor);
                clip(1 - clipFactor);
                
                // 在视空间中，摄像机在原点，所以从摄像机到片元的方向就是归一化的 positionVS
                float3 positionVS = TransformWorldToView(input.positionWS);
                float3 viewDirVS = normalize(positionVS);
                
                half3 normalVS = mul(input.normalWS, (float3x3) UNITY_MATRIX_I_V); // 视空间法线
                
                half dotProduct = dot(viewDirVS, normalVS); // [-1, 1] 值越小表现越是侧面
                half normalizedValue = saturate((dotProduct + 1.0) * 0.5); // [-1, 1] -> [0, 1]
                normalizedValue = lerp(normalizedValue, 1.0 - normalizedValue, _InvertFadeDirection); // 反转
                
                // 调整渐变曲线
                normalizedValue *= _GradientScale;
                normalizedValue = CheapSmoothStep(_GradientLeft, _GradientRight, normalizedValue);
                normalizedValue = pow(normalizedValue, _GradientPower);
                normalizedValue = saturate(normalizedValue);
                
                half4 result;
                result.r = input.positionCS.z;
                result.g = normalizedValue;
                result.b = 0;
                result.a = 0;
                
                return result;
            }
            ENDHLSL
        }
    }
}