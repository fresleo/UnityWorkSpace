Shader "Unlit/LocalVolumetricFogShadow"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="UniversalPipeline" }
        ZTest Always
        ZWrite Off
        //Blend One SrcAlpha
        Cull front

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            float4 _VolumetricParam; //x:frameCount y:stepCount z:fadeToBorder w:density
            float3 _ScatteringCoefficient;
            float3 _AbsorptionCoefficient;
            float _Coverage;
            float _FadeToCamera;
            TEXTURE2D(_MainShape2DTexture);
            SAMPLER(sampler_MainShape2DTexture);
            float4 _MainShapeOffset;

            TEXTURE3D(_DetailShape3DTexture);
            SAMPLER(sampler_DetailShape3DTexture);
            float4 _DetailShapeOffset;

            float3 _LightDirection;
            float4x4 _ShadowVP;
            float4x4 _ShadowIVP;
            float4 _ShadowmapTextureSize;
            float3 _PlayerPos;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.localPos = v.vertex.xyz;

                return o;
            }



            float4 frag(v2f i) : SV_Target
            {
                //这里我们把当前像素的世界坐标作为rayOrigin，灯光方向作为rayDirection
                float4x4 localToWorld = UNITY_MATRIX_M;
                float4x4 worldToLocal = UNITY_MATRIX_I_M;
                float3 rayDistances = IntersectOBBRay(i.worldPos, _LightDirection, localToWorld, worldToLocal).xyz;

                float exitDistance = rayDistances.z; 
                float entryDistance = rayDistances.y;
                float rayLength = exitDistance - entryDistance;

                float3 rayDirection = -_LightDirection;
                float3 rayOrigin = i.worldPos + rayLength * _LightDirection;
                float fadeToBorder = _VolumetricParam.z;
                float frame = _VolumetricParam.x;
                float stepCount = _VolumetricParam.y;
                float densityScale = _VolumetricParam.w * _ShadowmapTextureSize.w;

                float scatteringCoefficient = dot(_ScatteringCoefficient, float3(0.333, 0.333, 0.333));// _DensityScale.z;
                float absorptionCoefficient = dot(_AbsorptionCoefficient, float3(0.333, 0.333, 0.333));// _DensityScale.w;

                float4 color = 0;

                float rayMaxDist = rayLength;
                //
                float stepSize = rayMaxDist * 0.03125;// 0.03125 = 1/32;
                float rayDistance = stepSize;
                //
                float extinctionSum = 0.0;
                float maxOpticalDepth = 0.0;
                float maxOpticalDepthTail = 0.0;
                float transmittanceIntegral = 1.0;
                float weightedDistanceSum = 0.0;
                float transmittanceSum = 0.0;
                float opticalDepthTailScale = 10;

                int sampleCount = 0;

                UNITY_UNROLL
                for (int i = 0; i < 32; i++)
                {
                    float3 position = rayOrigin + rayDirection * rayDistance;
                    float3 localPos = mul(worldToLocal, float4(position, 1)).xyz;
                    float2 distToBorder2 = 0.5 - abs(localPos.xz);
                    float distToBorder = min(distToBorder2.x, distToBorder2.y);
                    float fade = min(1, distToBorder * fadeToBorder);
                    //
                    //dist to player
                    float distToCamera = length(position - _PlayerPos.xyz);
                    distToCamera = saturate(distToCamera * _FadeToCamera - _FadeToCamera);
                    fade *= distToCamera;
                    //mainShape
                    float3 mainShapeUV = float3(position.x, localPos.y + 0.5, position.z) * float3(_MainShapeOffset.w, 1, _MainShapeOffset.w) + _MainShapeOffset.xyz;
                    
                    mainShapeUV.y = saturate(mainShapeUV.y);
                    float mainShape = SAMPLE_TEXTURE2D_LOD(_MainShape2DTexture, sampler_MainShape2DTexture, mainShapeUV.xz, 0).r;
                    mainShape = SampleWeather(mainShapeUV.y, mainShape, _Coverage);
                    mainShape = mainShape * fade;

                    UNITY_BRANCH
                    if (mainShape > 0.001)
                    {
                        float shape = SAMPLE_TEXTURE3D_LOD(_DetailShape3DTexture, sampler_DetailShape3DTexture, position * float3(_DetailShapeOffset.w, 2 * _DetailShapeOffset.w, _DetailShapeOffset.w) + _DetailShapeOffset.xyz, 0).r;
                        shape = max(shape, 1e-5);
                        float density = saturate((mainShape - 1) / shape + 1) * densityScale;

                        float extinction = density * scatteringCoefficient + density * absorptionCoefficient;

                        extinctionSum += extinction;
                        maxOpticalDepth += extinction * stepSize;
                        transmittanceIntegral *= exp(-extinction * stepSize);
                        weightedDistanceSum += rayDistance * transmittanceIntegral;
                        transmittanceSum += transmittanceIntegral;
                        sampleCount++;
                    }

                    UNITY_BRANCH
                    if (transmittanceIntegral <= 0.001)
                    {
                        maxOpticalDepthTail = min(
                            opticalDepthTailScale * stepSize * exp(float(1 - sampleCount)),
                            stepSize * 0.5 // Excessive optical depth only introduces aliasing.
                        );
                        break;
                    }
                    rayDistance += stepSize;
                }

                if (sampleCount == 0) {
                    return float4(rayMaxDist, 0.0, 0.0, 0.0);
                }

                float frontDepth = weightedDistanceSum / transmittanceSum;
                float meanExtinction = extinctionSum / float(sampleCount);
                return float4(frontDepth, meanExtinction, maxOpticalDepth, maxOpticalDepthTail);
            }
            ENDHLSL
        }
    }
}
