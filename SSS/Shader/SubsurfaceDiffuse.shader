Shader "Hidden/SubsurfaceDiffuse"
{
    SubShader
    {
        Name "SubsurfaceDiffuse"
        Tags
        {
            "LightMode" = "SubsurfaceDiffuse"
        }
        Pass
        {

            Blend One Zero
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
            #include "./Libs/SubsurfaceLighting.hlsl"

            #pragma vertex VertexFunction
            #pragma fragment Frag


            #pragma multi_compile_fog

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _BaseColor;
                float4 _KnightThicknessMap_ST;
                float _Float0;
                float _Phase;
            CBUFFER_END

            // haven't defined param: _Thickness _BaseColor

            sampler2D _BaseColorMap;
            sampler2D _KnightThicknessMap;


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

                float3 Albedo = tex2D(_BaseColorMap, packedInput.uv1.xy).rgb * _BaseColor;
                // ------------------ indirectDiffuse ------------------
                float3 IndirectDiffuse = float3(0, 0, 0);
                irradianceSSS(NormalWS, IndirectDiffuse);


                // -------------- directDiffuse ------------------
                DirectSufsurfaceLighting subsurfaceLight;
                float3 DirectDiffuse = float3(0, 0, 0);
                subsurfaceLight = (DirectSufsurfaceLighting)0;
                subsurfaceLight.Albedo = Albedo.rgb;
                subsurfaceLight.NormalWS = NormalWS;
                subsurfaceLight.uv = packedInput.uv1.xy;
                subsurfaceLight.PositionWS = PositionWS;
                subsurfaceLight.PositionRWS = input.positionRWS;
                subsurfaceLight.viewDir = V;
                subsurfaceLight.positionCS = input.positionSS;
                subsurfaceLight.Thickness = tex2D(_KnightThicknessMap, packedInput.uv1.xy).r;
                float ShadowMask = 0;
                DirectLightSSS(subsurfaceLight, DirectDiffuse, ShadowMask);

                float3 finalColor = DirectDiffuse * 0.8f + (IndirectDiffuse * 0.2f);
                // float3 finalColor = DirectDiffuse;
                finalColor.b = max(finalColor.b, HALF_MIN);
                // ShadowMask = max(ShadowMask, HALF_MIN);
                ouputColor = float4(finalColor, ShadowMask);
                ouputAlbedo = float4(Albedo, 1.0);
                // outputShadow = float4(Shadow, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}