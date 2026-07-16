Shader "Hidden/XKNight/SubsurfaceDiffuse"
{
    SubShader
    {
        Pass
        {

            Name "SubsurfaceDiffuse"
            Tags
            {
                "LightMode" = "SubsurfaceDiffuse"
            }
            Blend One Zero
            ZWrite On
            ZTest LEqual
            Offset 0 , 0
            ColorMask RGBA

            HLSLPROGRAM
            // #pragma target 4.5
            // #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET

            #include "./ToonPBR_SubsurfaceLighting.hlsl"

            #pragma vertex VertexFunction
            #pragma fragment Frag


            #pragma multi_compile_fog


            // doesn't have defined param: _Thickness _BaseColor


            struct AttributesMesh
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
            };

            struct FragInputs
            {
                float4 positionSS;
                float3 positionRWS;
                float3 positionPredisplacementRWS;
                float2 positionPixel;
                float4 texCoord0;
                float4 texCoord1;
                float4 texCoord2;
                float4 texCoord3;
                float4 color; // vertex color
                float3x3 tangentToWorld;
            };

            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_Position;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float4 uv0 : TEXCOORD3;
                float4 uv1 : TEXCOORD4;
                float4 uv2 : TEXCOORD5;
            };

            float3x3 BuildTangentToWorld(float4 tangentWS, float3 normalWS)
            {
                float3 unnormalizedNormalWS = normalWS;
                float renormFactor = rcp(length(unnormalizedNormalWS));
                float3x3 tangentToWorld = CreateTangentToWorld(
                    unnormalizedNormalWS, tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0);
                tangentToWorld[0] *= renormFactor;
                tangentToWorld[1] *= renormFactor;
                tangentToWorld[2] *= renormFactor;
                return tangentToWorld;
            }

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
                output.uv0 = inputMesh.uv0;
                output.uv1 = inputMesh.uv1;
                output.uv2 = inputMesh.uv2;

                return output;
            }


            void FragFunction(PackedVaryingsMeshToPS packedInput, out float4 ouputColor : SV_Target0,
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
                float3 NormalWS = packedInput.normalWS;
                float4 TangentWS = packedInput.tangentWS;
                float3 BitangentWS = input.tangentToWorld[1];

                float3 Albedo = SampleAlbedoAlpha(packedInput.uv0.xy, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                // Albedo.rgb = Albedo.rgb * _BaseColor.rgb;

                // ------------------ indirectDiffuse ------------------
                float3 IndirectDiffuse = float3(0, 0, 0);
                irradianceSSS(NormalWS, IndirectDiffuse);


                // -------------- directDiffuse ------------------
                DirectSufsurfaceLighting subsurfaceLight;
                float3 DirectDiffuse = float3(0, 0, 0);
                subsurfaceLight = (DirectSufsurfaceLighting)0;
                float4 thicknessAndDifuseMask = tex2D(_ScattingThicknessMap, packedInput.uv0.xy);
                subsurfaceLight.Albedo = Albedo.rgb;
                subsurfaceLight.NormalWS = NormalWS;
                subsurfaceLight.uv = packedInput.uv0.xy;
                subsurfaceLight.PositionWS = PositionWS;
                subsurfaceLight.PositionRWS = input.positionRWS;
                subsurfaceLight.viewDir = V;
                subsurfaceLight.TangentWS = TangentWS;
                subsurfaceLight.positionCS = input.positionSS;
                subsurfaceLight.Thickness = thicknessAndDifuseMask.r;
                subsurfaceLight.SSSMask = _IsSingleChanelThicknessMap == 0 ? 1 : thicknessAndDifuseMask.g;
                subsurfaceLight.TransmissionMask = _IsSingleChanelThicknessMap == 0 ? 1 : thicknessAndDifuseMask.b;
                float ShadowMask = 0;
                uint materialIndex = FindDiffusionParametersIndex(asuint(_DiffusionParameter));
                SSSData sssData = DecodeFromSSSBuffer(materialIndex);
                DirectLightSSS(subsurfaceLight, sssData, DirectDiffuse, ShadowMask);

                float3 finalColor = DirectDiffuse * 0.8f + (IndirectDiffuse * 0.2f);
                // float3 finalColor = DirectDiffuse;
                finalColor.b = max(finalColor.b, HALF_MIN);
                // ShadowMask = max(ShadowMask, HALF_MIN);
                finalColor = float3(1, 1, 1);
                Albedo = float3(1, 1, 1);
                ouputColor = float4(finalColor, ShadowMask);
                ouputAlbedo = float4(Albedo, 1.0);


                // debug
                // if (materialIndex == 0xffffffff)
                //     ouputAlbedo = float4(0, 0, 1, 1); // 没找到
                // else if (materialIndex == 0)
                //     ouputAlbedo = float4(1, 0, 0, 1); // index 0
                // else if (materialIndex == 1)
                //     ouputAlbedo = float4(0, 1, 0, 1); // index 1

                EncodeIntoSSSBuffer(subsurfaceLight, ouputAlbedo, materialIndex, subsurfaceLight.SSSMask);
            }

            void Frag(PackedVaryingsMeshToPS packedInput, out float4 ouputColor : SV_Target0,
                  out float4 ouputAlbedo : SV_Target1)
            {
                if (_KnightToonScattingSurface > 0)
                {
                    FragFunction(packedInput, ouputColor, ouputAlbedo);
                }
                else
                {
                    ouputColor = float4(0, 0, 0, 0);
                    ouputAlbedo = float4(0, 0, 0, 0);
                }
            }
            ENDHLSL
        }
    }
}