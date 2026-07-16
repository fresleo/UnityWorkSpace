Shader "XKnight/Character/ToonPBR_Character_Transparent"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _Base ("基础设置", Float) = 1
        
        [Sub(Base)] _Color ("Color", Color) = (1, 1, 1, 1)
        
        
        [Main(Dither, __, on)]
        _Dither ("抖动透明", Float) = 0
        
        [Sub(Dither)] _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        [Sub(Dither)] _DitherSize ("抖动尺寸", Float) = 1
        [Sub(Dither)] _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        [Sub(Dither)] [DitherMatrixSelector] _DitherWithMatrix ("抖动矩阵", Int) = 0
        [Sub(Dither)] [DitherTextureReadOnly] _DitherTexture ("抖动图", 2D) = "black" {}
        
        
        [Main(Vertex, __, on, off)]
        _Vertex ("顶点控制", Float) = 1

        [Sub(Vertex)] _VertexColorAlphaWeight ("顶点色 Alpha 权重%0 忽略顶点色 A，1 完全使用顶点色 A", Range(0, 1)) = 0
        
        
        // FOV
        [HideInInspector] _FOV_PivotWS ("角色枢轴世界坐标", Vector) = (0,0,0,0)
        [HideInInspector] _FOV_Parameters ("透视压扁参数 (x=脚本开关, y=压扁目标, z=形体补偿, w=1)", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Transparent-401" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DITHER_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                
                // 抖动
                half    _DitherIntensity, _DitherSize, _DitherAlpha;
                half    _DitherWithMatrix;
                float4  _DitherTexture_TexelSize;
                
                half _VertexColorAlphaWeight;
                
                // FOV
                float4  _FOV_PivotWS;
                float4  _FOV_Parameters; // 复合参数
            CBUFFER_END
            
            TEXTURE2D_X(_DitherTexture);
            
            #include "./Include/ToonPBR_FOVFix.hlsl"

            struct a2v
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                
                float4 positionSS : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(a2v input)
            {
                v2f output = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.color = input.color;
                
                #ifdef _DITHER_ON
                output.positionSS = ComputeScreenPos(output.positionCS);
                #endif
                
                return output;
            }

            half4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half alpha = lerp(1.0h, input.color.a, saturate(_VertexColorAlphaWeight));
                
                // 抖动
                #ifdef _DITHER_ON
                DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
                    TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
                
                alpha *= _DitherAlpha;
                #endif
                
                half4 color = _Color;
                color.a *= alpha;
                
                return color;
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}
