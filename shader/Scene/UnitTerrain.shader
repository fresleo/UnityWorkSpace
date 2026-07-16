// 这个是与 GrassColorMapRenderer 组件搭配拍照用的
Shader "XKnight/Scene/UnlitTerrain"
{
    Properties
    {
        [Main(BL1, __, on, off)]
        _BL1 ("笔刷层 1", Float) = 1

        [Sub(BL1)] _Splat0 ("笔刷层 1 - 油彩", 2D) = "white" {}

        [Main(BL2, __, on, off)]
        _BL2 ("笔刷层 2", Float) = 1

        [Sub(BL2)] _Splat1 ("笔刷层 2 - 油彩", 2D) = "white" {}

        [Main(BL3, __, on, off)]
        _BL3 ("笔刷层 3", Float) = 1

        [Sub(BL3)] _Splat2 ("笔刷层 3 - 油彩", 2D) = "white" {}

        [Main(BL4, __, on, off)]
        _BL4 ("笔刷层 4", Float) = 1

        [Sub(BL4)] _Splat3 ("笔刷层 4 - 油彩", 2D) = "white" {}

        [Main(C, __, on, off)]
        _C ("控制", Float) = 1

        [Sub(C)] _Control ("控制图 (RGBA)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque" "Queue" = "Geometry-1500"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            CBUFFER_END
            
            TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);

            TEXTURE2D(_Control); SAMPLER(sampler_Control);

            struct Attributes
            {
                float4 positionOS           : POSITION;
                float3 normalOS             : NORMAL;
                float4 tangentOS            : TANGENT;
                
                float2 texcoord             : TEXCOORD0;
                float2 staticLightmapUV     : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pack0        : TEXCOORD0; // xy: _Control uv  zw: _Splat0 uv
                float4 pack1        : TEXCOORD1; // xy: _Splat1 uv   zw: _Splat2 uv
                float2 pack2        : TEXCOORD2; // xy: _Splat3 uv
                
                float3 positionWS   : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
                float4 tangentWS    : TEXCOORD5;
                float3 viewDirWS    : TEXCOORD6;
                
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord  : TEXCOORD7;
                #endif

                float4 positionCS   : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                output.pack0.xy = input.texcoord;
                output.pack0.zw = TRANSFORM_TEX(input.texcoord, _Splat0);
                output.pack1.xy = TRANSFORM_TEX(input.texcoord, _Splat1);
                output.pack1.zw = TRANSFORM_TEX(input.texcoord, _Splat2);
                output.pack2.xy = TRANSFORM_TEX(input.texcoord, _Splat3);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }

            half3 SplatmapMix(half4 splatControl, half4 uvPack0, half4 uvPack1, half2 uvPack2)
            {
                half3 lay0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvPack0.zw).rgb;
                half3 lay1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvPack1.xy).rgb;
                half3 lay2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvPack1.zw).rgb;;
                half3 lay3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvPack2.xy).rgb;
                return lay0 * splatControl.r + lay1 * splatControl.g + lay2 * splatControl.b + lay3 * splatControl.a;
            }

            half4 LitPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.pack0.xy);
                half3 albedo = SplatmapMix(splatControl, input.pack0, input.pack1, input.pack2);
                return half4(albedo, 1.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}