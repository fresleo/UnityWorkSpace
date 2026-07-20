Shader "Unlit/LocalVolumetricFog"
{
    Properties
    {
    }
    SubShader
    {
        //Tags { "RenderType"="Transparent" "Queue"="Transparent+999" "RenderPipeline" = "UniversalPipeline"}
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        ZTest Always
        ZWrite Off
        Blend One SrcAlpha, Zero SrcAlpha
        Cull front
        Pass
        {
            //Tags { "LightMode" = "UniversalForward" }
            Tags {"LightMode" = "LocalVolumetric"}
            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ _BEAM_SHADOWMAP
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                float4 localCameraPos : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            float4 _VolumetricParam; //x:frameCount y:stepCount z:fadeToBorder w:density

            float3 _ScatteringCoefficient;
            float3 _AbsorptionCoefficient;
            float _ExtinctionScale;
            float _Coverage;
            float _FadeToCamera;

            TEXTURE2D(_MainShape2DTexture);
            SAMPLER(sampler_MainShape2DTexture);
            float4 _MainShapeOffset;

            TEXTURE3D(_DetailShape3DTexture);
            SAMPLER(sampler_DetailShape3DTexture);
            float4 _DetailShapeOffset;

            //BSM
            TEXTURE2D(_BeamShadowmap);
            SAMPLER(sampler_BeamShadowmap);
            float4 _ShadowmapTextureSize;
            float4x4 _ShadowVP;
            float4x4 _ShadowIVP;

            float3 _SunIrradiance;
            float3 _SkyIrradiance;
            float3 _PlayerPos;
            // 光源数据
            int _LightCount;
            float4 _LightPositions[4];   // xyz = 世界位置
            float4 _LightColors[4];      // rgb = 颜色 * 强度
            float4 _LightDirections[4];  // xyz = 方向（聚光灯用）
            float4 _LightParams[4];      // x=range, y=spotAngle, z=lightType, w=innerSpotAngle
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.localPos = v.vertex.xyz;
                o.localCameraPos = mul(UNITY_MATRIX_I_M, float4(_WorldSpaceCameraPos, 1));
                o.localCameraPos.xyz /= o.localCameraPos.w;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            // 计算点光源的衰减
            float CalculatePointLightAttenuation(float distance, float range)
            {
                // 使用Unity标准的平方反比衰减
                float attenuation = 1.0 / (1.0 + distance * distance / (range * range));

                // 应用范围截止
                float fadeStart = range * 0.8;
                float fadeFactor = saturate((range - distance) / (range - fadeStart));

                return attenuation * fadeFactor;
            }

            // 计算聚光灯的角度衰减
            float CalculateSpotLightAttenuation(float3 lightDir, float3 spotDir, float spotAngle, float innerSpotAngle)
            {
                float cosAngle = dot(-lightDir, spotDir);
                float cosSpotAngle = cos(spotAngle * 0.5);
                float cosInnerAngle = cos(innerSpotAngle * 0.5);

                // 在内角和外角之间平滑过渡
                float attenuation = saturate((cosAngle - cosSpotAngle) / (cosInnerAngle - cosSpotAngle));
                return attenuation * attenuation; // 平方以获得更平滑的过渡
            }

            float3 GetPointSpotLight(const float3 positionWS, const float phaseAddLight)
            {
                float3 totalLighting = float3(0, 0, 0);

                UNITY_UNROLL
                    for (int i = 0; i < 4; i++)
                    {
                        UNITY_BRANCH
                            if (i >= _LightCount)
                                break;
                        float3 lightPos = _LightPositions[i].xyz;
                        float3 lightColor = _LightColors[i].rgb;
                        float3 lightDir = _LightDirections[i].xyz;

                        float range = _LightParams[i].x;
                        float spotAngle = _LightParams[i].y;
                        float lightType = _LightParams[i].z; // 0 = point, 1 = spot
                        float innerSpotAngle = _LightParams[i].w;

                        // 计算到光源的向量
                        float3 toLightVector = lightPos - positionWS;
                        float distance = length(toLightVector);
                        float3 toLightDir = toLightVector / distance;

                        // 跳过超出范围的片元
                        UNITY_BRANCH
                            if (distance > range)
                                continue;

                        // 计算距离衰减
                        float distanceAttenuation = CalculatePointLightAttenuation(distance, range);

                        // 计算角度衰减（仅聚光灯）
                        float angleAttenuation = 1.0;
                        if (lightType > 0.5) // 聚光灯
                        {
                            angleAttenuation = CalculateSpotLightAttenuation(toLightDir, lightDir, spotAngle, innerSpotAngle);
                        }

                        // 应用衰减幂次
                        float totalAttenuation = pow(distanceAttenuation * angleAttenuation, 2);
                        //随着距离衰减
                        //float fogAttenuation = saturate(1.0 -  distance / range) * _ExtinctionScale;
           
                        // 累加光照贡献
                        totalLighting += lightColor * totalAttenuation * phaseAddLight * _ExtinctionScale;
                    }
                return totalLighting;
            }

#if defined(_BEAM_SHADOWMAP)
            float SampleBeamShadowmapPCF(float3 position, float3 sunDirection)
            {
                /*
                float dist = length(position - _WorldSpaceCameraPos);
                float shadowDist = _GlobalParam.y;
                if (dist > shadowDist)
                    return 1;*/

                const int sampleCount = 4; //4 9 16
                real weights[sampleCount];
                real2 positions[sampleCount];
                float4 size = _ShadowmapTextureSize.yyxx;

                float4 shadowClipPos = mul(_ShadowVP, float4(position, 1));
                float3 shadowNDC = shadowClipPos.xyz / shadowClipPos.w;
                float2 uv = shadowNDC.xy * 0.5 + 0.5;
                //

                //计算当前点沿着sun方向到top的距离
                float distToTop = 0;
                
                float3 rayDistances = IntersectOBBRay(position, sunDirection, UNITY_MATRIX_M, UNITY_MATRIX_I_M).xyz;
                distToTop = rayDistances.z - rayDistances.y;

                SampleShadow_ComputeSamples_Tent_3x3(size, uv.xy, weights, positions);

                float opticalDepth = 0;
                float offset = _ShadowmapTextureSize.z;

                UNITY_UNROLL
                for (int i = 0; i < sampleCount; i++)
                {
                    float4 shadowMap = SAMPLE_TEXTURE2D_LOD(_BeamShadowmap, sampler_BeamShadowmap, positions[i], 0).rgba;
                    float depth = max(0, distToTop - shadowMap.r - offset) * shadowMap.g;

                    //depth = min(depth, shadowMap.b + shadowMap.a);
                    opticalDepth += weights[i] * depth;//
                }

                return opticalDepth;
            }
#endif

            //已知视角射线和Cube必定有交
            //已知出射点:就是当前像素的坐标点
            //基于以上两点进行的射线和Cube求交优化版
            float2 IntersectCube(float3 localCameraPos, float3 localFragmentPos,
                float3 worldCameraPos, float3 worldFragmentPos)
            {
                // 世界射线信息
                float worldRayLength = distance(worldCameraPos, worldFragmentPos);

                // 检查相机是否在立方体内部
                float3 absPos = abs(localCameraPos);
                bool cameraInside = max(absPos.x, max(absPos.y, absPos.z)) <= 0.5;

                if (cameraInside)
                {
                    return float2(0.0, worldRayLength);
                }

                // 计算本地射线参数
                float3 localRayDir = normalize(localFragmentPos - localCameraPos);
                float localRayLength = distance(localCameraPos, localFragmentPos);

                // 射线-立方体相交
                float3 invDir = 1.0 / (localRayDir + 1e-7 * sign(localRayDir));
                float3 t1 = (-0.5 - localCameraPos) * invDir;
                float3 t2 = (0.5 - localCameraPos) * invDir;

                float3 tNear = min(t1, t2);
                float entryT = max(tNear.x, max(tNear.y, tNear.z));
                entryT = max(entryT, 0.0);

                // 使用比例关系计算世界距离
                float entryDistance = (entryT / localRayLength) * worldRayLength;
                /*float3 localEntry = localCameraPos + entryT * localRayDir;
                float3 worldEntry = mul(UNITY_MATRIX_M, float4(localEntry, 1)).xyz;
                float entryDistance = length(worldEntry - worldCameraPos);*/

                return float2(entryDistance, worldRayLength);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float depth = SAMPLE_TEXTURE2D(_DepthTexture, sampler_PointClamp, screenUV).r; //SampleSceneDepth(screenUV);
#if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
#endif
                //float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float3 sceneWorldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float sceneDistance = distance(_WorldSpaceCameraPos, sceneWorldPos);
                
                float2 rayDistances = IntersectCube(i.localCameraPos.xyz, i.localPos.xyz, _WorldSpaceCameraPos.xyz, i.worldPos);

                float entryDistance = rayDistances.x;
                float exitDistance = min(sceneDistance, rayDistances.y);
                float rayLength = max(0, exitDistance - entryDistance);

                if (rayLength <= 0)
                    return float4(0, 0, 0, 1);
                
                float4 col = float4(0, 0, 0, 1);
                //float fade = saturate(rayLength * _FadeOut);
                
                float frameCount = _VolumetricParam.x;
                float stepCount =  _VolumetricParam.y;
                float fadeToBorder = _VolumetricParam.z;
                float maxRayDistance = rayLength;
                float densityScale = _VolumetricParam.w;
                float stepSize = maxRayDistance / stepCount;
    

                float jitter = InterleavedGradientNoise(screenUV * _ScreenParams.xy, frameCount); //InterleavedGradientNoise 定义在Random.hlsl
                
                float3 rayDirection = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos.xyz + entryDistance * rayDirection;
                float rayDistance = jitter * stepSize * 1;

                float3 radianceIntegral = float3(0, 0, 0);
                float3 transmittanceIntegral = float3(1, 1, 1);
                float4x4 worldToLocal = UNITY_MATRIX_I_M;
                float3 sunDirection = GetMainLight().direction;
                float cosTheta = dot(sunDirection, rayDirection);

                for (int count = 0; count < stepCount; ++count)
                {
                    float3 position = rayOrigin + rayDistance * rayDirection;
                    float3 localPos = mul(worldToLocal, float4(position, 1)).xyz;
                    float2 distToBorder2 = 0.5 - abs(localPos.xz);
                    float distToBorder = min(distToBorder2.x, distToBorder2.y);
                    float fade = min(1, distToBorder * fadeToBorder);
                    //dist to camera
                    float distToCamera = length(position - _PlayerPos.xyz);
                    distToCamera = saturate(distToCamera * _FadeToCamera - _FadeToCamera);
                    fade *= distToCamera;
                    //
                    float3 mainShapeUV = float3(position.x, localPos.y + 0.5, position.z) * float3(_MainShapeOffset.w, 1, _MainShapeOffset.w) + _MainShapeOffset.xyz;

                    mainShapeUV.y = saturate(mainShapeUV.y);
                    float mainShape = SAMPLE_TEXTURE2D_LOD(_MainShape2DTexture, sampler_MainShape2DTexture, mainShapeUV.xz, 0).r;
                    mainShape = SampleWeather(mainShapeUV.y, mainShape, _Coverage);
                    mainShape = mainShape * fade;

                    if (mainShape <= 0.001) 
                    {
                        rayDistance += stepSize;
                        continue;
                    }

                    float shape = SAMPLE_TEXTURE3D_LOD(_DetailShape3DTexture, sampler_DetailShape3DTexture, position * float3(_DetailShapeOffset.w, 2 * _DetailShapeOffset.w, _DetailShapeOffset.w) + _DetailShapeOffset.xyz, 0).r;
                    float density = saturate((mainShape - 1) / shape + 1) * densityScale;
       
                    if (density > 0.001)
                    {
                        float3 scattering = density * _ScatteringCoefficient;
                        float3 absorption = density * _AbsorptionCoefficient;
                        float3 extinction = scattering + absorption;
                        //optical depth to the sun 
                        float opticalDepth = 0;
                        float shadowAttenuation = 1;
#if defined(_BEAM_SHADOWMAP)
                        opticalDepth = SampleBeamShadowmapPCF(position, sunDirection);
                        float4 shadowCoord = TransformWorldToShadowCoord(position);
                        shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
#endif
                        float3 pointLight = 0;

                        pointLight = GetPointSpotLight(position, RECIPROCAL_PI4);

                        float3 radiance = _SunIrradiance * shadowAttenuation * ApproximateMultipleScatteringFit(opticalDepth, cosTheta);
                        radiance += _SkyIrradiance * RECIPROCAL_PI4 + pointLight;
                        radiance *= scattering;
                        float3 transmittance = exp(-extinction * stepSize * _ExtinctionScale);

#if defined(_BEAM_SHADOWMAP)
                        //散射光的解析积分计算
                        //5.6.3 in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
                        float3 clampedExtinction = max(extinction * _ExtinctionScale, 1e-5);
                        float3 scatteringIntegral = (radiance - radiance * transmittance) / clampedExtinction;
                        radianceIntegral += transmittanceIntegral * scatteringIntegral;
#else
                        radianceIntegral += transmittanceIntegral * radiance * stepSize; //这是离散的累加，上面是物理正确能量守恒的解析计算方式
#endif
                        transmittanceIntegral *= transmittance;
                    }

                    rayDistance += stepSize;
                    if (all(transmittanceIntegral <= 0.001))
                    {
                        break;
                    }
                }

                col.rgb = radianceIntegral.rgb;
                col.a = dot(transmittanceIntegral, float3(0.3333, 0.3333, 0.3333));
                
                return col;
            }
            ENDHLSL
        }
    }
}
