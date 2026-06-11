Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (0.85, 0.6, 0.55, 1)
        [MainTexture] _BaseColorMap ("Base Color Map", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _SpecularTint ("Specular Tint", Color) = (1,1,1,1)
        _AmbientColor ("Ambient (test fill)", Color) = (0.10, 0.12, 0.16, 1)
        _AmbientStrength ("Ambient Strength", Range(0,2)) = 0.25
    }
    SubShader
    {
        Name "SubsurfaceDiffuse"
        Tags
        {
            "LightMode" = "SubsurfaceDiffuse"
        }

        Pass
        {
            ZWrite On
            ZTest LEqual
            Offset 0 , 0
            ColorMask RGBA


            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"


            #pragma vertex VertexFunction
            #pragma fragment Frag


            #pragma multi_compile_fog

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorTest_ST;
                float _Float0;
                float _Phase;
            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float4 texcoord : TEXCOORD0;
            };

            struct PackedVaryings
            {
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BaseColorTest;
            sampler2D _PreIntegrated;//PerMaterial
            sampler2D _Curve;
            sampler2D _Thickness;
            sampler2D _PreIntegratedTex;

            float4 _SSSMainLightDir;
            float4 _SSSMainLightColor;


            struct AttributesMesh
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
            };

            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_Position;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
                float4 uv1 : TEXCOORD3;
                float4 uv2 : TEXCOORD4;

                float4 ase_texcoord7 : TEXCOORD7;
            };

            PackedVaryingsMeshToPS VertexFunction(AttributesMesh inputMesh)
            {
                PackedVaryingsMeshToPS output = (PackedVaryingsMeshToPS)0;

                float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
                float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);


                output.positionCS = TransformWorldToHClip(positionRWS);
                output.positionWS = positionRWS;
                output.normalWS = normalWS;
                output.tangentWS = tangentWS;
                output.uv1 = inputMesh.uv1;
                output.uv2 = inputMesh.uv2;

                return output;
            }

            void Frag(PackedVaryingsMeshToPS packedInput, out float4 ouputColor : SV_Target0,
      out float4 ouputAlbedo : SV_Target1)
            {
                FragInputs input;

                input.positionSS = packedInput.positionCS;
                input.positionRWS = packedInput.positionWS;
                input.tangentToWorld = BuildTangentToWorld(packedInput.tangentWS, packedInput.normalWS);

                uint2 tileIndex = uint2(input.positionSS.xy) / 1;
                //标准化结构方便后续采样，重建
                PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z,
                           input.positionSS.w, input.positionRWS.xyz,
                           tileIndex);
                float3 PositionWS = GetAbsolutePositionWS(posInput.positionWS);
                float3 V = GetWorldSpaceNormalizeViewDir(packedInput.positionWS);
                float4 ScreenPosNorm = float4(posInput.positionNDC, packedInput.positionCS.zw);
                float4 ClipPos = ComputeClipSpacePosition(ScreenPosNorm.xy, packedInput.positionCS.z) * packedInput.
        positionCS.w;
                float4 ScreenPos = ComputeScreenPos(ClipPos, _ProjectionParams.x);
                float3 NormalWS = packedInput.normalWS;
                float3 TangentWS = packedInput.tangentWS.xyz;
                float3 BitangentWS = input.tangentToWorld[1];

                //计算公式
                float NDL = saturate( dot(NormalWS, normalize(_SSSMainLightDir.xyz)));
                ouputColor = float4(NDL, NDL, NDL, 1.0);
                ouputAlbedo = float4(1, 1, 1, 1.0);
            }
            ENDHLSL
        }
    }
}