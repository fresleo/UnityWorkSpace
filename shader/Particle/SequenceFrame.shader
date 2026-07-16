/*
用来绘制序列帧的着色器
*/
Shader "XKnight/Particle/SequenceFrame"
{
    Properties
    {
        // Blend
        _BlendMode ("混合模式", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode ("Src Mode", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstMode ("Dst Mode", Float) = 10
        
        // Base
        _MainTex ("主纹理", 2D) = "white" {}
        [HDR] _Color ("主颜色", Color) = (1, 1, 1, 1)
        [MaterialToggle] _MainTexGray ("主纹理去色", Range(0, 1)) = 0
        
        _MainTexMask ("主纹理遮罩", 2D) = "white" {}
        
        // Sequence
        _SequenceGrid ("序列的网格结构 (X=列, Y=行)", Vector) = (4, 4, 0, 0)
        _CustomFrameIndex ("当前的帧索引 (从0开始)", Float) = 0
        
        // Advanced
        [MaterialToggle] _FullScreenOn ("开启全屏效果", Range(0, 1)) = 0
        
        _Cutoff ("透明裁切的阈值", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend [_SrcMode] [_DstMode]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _MainTexMask_ST;
                half4 _Color;
                
                half _MainTexGray;
                
                float4 _SequenceGrid;
                half _CustomFrameIndex;
                
                half _FullScreenOn;
                
                half _Cutoff;
            CBUFFER_END
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MainTexMask); SAMPLER(sampler_MainTexMask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 vertexColor : COLOR;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetFullScreenPos(float2 uv)
            {
                // 用 UV 构造 NDC 全屏四边形
                float2 remap = uv.xy * 2 - 1;
                float4 fullScreenPos = float4(remap.x, remap.y, 0, 1);
                
                #if UNITY_UV_STARTS_AT_TOP
                fullScreenPos.y = -fullScreenPos.y;
                #endif
                
                #if UNITY_REVERSED_Z
                fullScreenPos.z = 1;
                #endif
                
                return fullScreenPos;
            }
            
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float4 fullScreenPos = GetFullScreenPos(input.uv.xy);
                output.positionCS = lerp(positionCS, fullScreenPos, _FullScreenOn);
                
                output.uv = input.uv;
                output.vertexColor = input.color;
                
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float cols = _SequenceGrid.x;
                float rows = _SequenceGrid.y;
                float totalTiles = cols * rows;

                float currentFrameIndex = floor(_CustomFrameIndex);
                currentFrameIndex = currentFrameIndex - totalTiles * floor(currentFrameIndex / totalTiles);
                
                // 每格的 uv 宽度
                float2 uvTileSize = float2(1 / cols, 1 / rows);

                // 先填满1行，再填下1行
                float frameRow = floor(currentFrameIndex / cols);
                float frameCol = currentFrameIndex - cols * frameRow;
                
                float2 uvFlippedV = float2(input.uv.x, 1.0 - input.uv.y);
                float2 sequenceUV = (uvFlippedV + float2(frameCol, frameRow)) * float2(uvTileSize.x, -uvTileSize.y);
                sequenceUV = sequenceUV * _MainTex_ST.xy + _MainTex_ST.zw;

                half4 mainTexSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sequenceUV);
                
                float2 mainTexMaskUV = input.uv.xy * _MainTexMask_ST.xy + _MainTexMask_ST.zw;
                half4 mainTexMaskSample = SAMPLE_TEXTURE2D(_MainTexMask, sampler_MainTexMask, mainTexMaskUV);
                
                // 去色变灰
                half gray = dot(mainTexSample.rgb, half3(0.299, 0.587, 0.114));
                half3 mainTexColor = lerp(mainTexSample.rgb, half3(gray, gray, gray), _MainTexGray);

                half3 finalColor = input.vertexColor.rgb * _Color.rgb * mainTexColor;
                
                half finalAlpha = input.vertexColor.a * _Color.a * mainTexSample.a * mainTexMaskSample.r;
                clip(finalAlpha - _Cutoff);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "XKnight.ShaderGUI.VFX.SequenceFrameShaderInspector"
}