Shader "Unlit/VolumetricLightingFog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VerticalBlur"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag


            float4 Frag(Varyings input) : SV_Target
            {
                return DepthAwareGaussianBlur(input.texcoord, float2(0.0, 1.0), _BlitTexture, sampler_PointClamp, _BlitTexture_TexelSize.xy);
            }

            ENDHLSL
        }

        Pass
        {
            Name "HorizontalBlur"


            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                return DepthAwareGaussianBlur(input.texcoord, float2(1.0, 0.0), _BlitTexture, sampler_LinearClamp, _BlitTexture_TexelSize.xy);
            }

            ENDHLSL
        }

        Pass
        {
            Name "VolumetricRender"
            ZTest Always
            ZWrite Off
            Cull Off
            //Blend Off
            Blend One SrcAlpha, Zero SrcAlpha
            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _OPEN_VOLUMETRIC_FOG
            #pragma multi_compile _ _OPEN_VOLUMETRIC_LIGHTING

            #pragma target 3.0
            #define VOLUMETRIC_FOG_SHADOW
            #define VOLUMETRIC_FOG_MULTI_LIGHT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"
            #include "./MathCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            int _FrameCount;
            float _StepSize;
            float4 _VolumetricLightingParam;//x: minH y:maxH z:density w:extinctionScale
            float4 _VolumetricLightingParam2;//x: Anisotropy  y:contrast 
            float4 _VolumetricFogParam;//x:minH  y:maxH  z:density
            float4 _VolumetricFogColor;
            float4 _AmbientColor;
            //
            // 光源数据
            int _LightCount;
            float4 _LightPositions[4];   // xyz = 世界位置
            float4 _LightColors[4];      // rgb = 颜色 * 强度
            float4 _LightDirections[4];  // xyz = 方向（聚光灯用）
            float4 _LightParams[4];      // x=range, y=spotAngle, z=lightType, w=innerSpotAngle

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

            float3 GetPointSpotLight(const float3 positionWS, const float4 phaseAddLight)
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

                    // 累加光照贡献
                    totalLighting += lightColor * totalAttenuation * phaseAddLight[i];
                }
                return totalLighting;
            }


            float GetShadow(const float3 position) 
            {
                float4 shadowCoord = TransformWorldToShadowCoord(position);
                float shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                return shadowAttenuation;
            }
            
            float4 MarchVolumetricLighting(const float3 rayOrigin, const float3 rayDirection, const float2 rayNearFar, const float cosTheta, float jitter)
            {
                float3 radianceIntegral = float3(0, 0, 0);
                float3 transmittanceIntegral = 1;

                float maxRayDistance = rayNearFar.y;
                float stepSize = _StepSize;

                float heightDensity = _VolumetricLightingParam.z * 0.1;
                float minHeight = _VolumetricLightingParam.x;
                float maxHeight = _VolumetricLightingParam.y;
                float lightScale = _VolumetricLightingParam.w;
                float scatteringCoefficient = 1;
                float absorptionCoefficient = 0;
                float anisotropy = _VolumetricLightingParam2.x;
                float phaseMainLight = PhaseFunction(cosTheta, anisotropy);//0.67
                //
                Light mainLight = GetMainLight();
                float rayDistance = rayNearFar.x + stepSize * jitter;
                float3 sunIrradiance = mainLight.color;
                float3 skyIrradiance = _AmbientColor.rgb;
                //
                UNITY_UNROLL
                for (int stepCounter = 0; stepCounter < 32; ++stepCounter)
                {
                    float3 position = rayDistance * rayDirection + rayOrigin;
                    float shadow = GetShadow(position);
                    float density = GetHeightFogDensity(position.y, minHeight, maxHeight) * heightDensity;

                    float3 scattering = density * scatteringCoefficient;//density * _ScatteringCoefficient
                    float3 extinction = density * absorptionCoefficient + scattering;//(density * _AbsorptionCoefficient + scattering);

                    {
                        //假设opticalDepth=0，那么这里的多重散射约等于phaseMainLight * 2
                        float3 radiance = sunIrradiance * phaseMainLight * 2 * shadow * lightScale;
                        //float3 radiance = sunIrradiance * ApproximateMultipleScatteringFit(0, cosTheta);

                        radiance += skyIrradiance * RECIPROCAL_PI4;
                        radiance *= scattering;

                        //
                        float3 transmittance = exp(-extinction * stepSize * 0.5); // extinctionScale 是一个非物理的消光缩放系数  
                        //float3 transmittance = 1;

                        //散射光的解析积分计算
                        //5.6.3 in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
                        /*float clampedExtinction = max(extinction, 1e-5);
                        float3 scatteringIntegral = (radiance - radiance * transmittance) / clampedExtinction;
                        radianceIntegral += transmittanceIntegral * scatteringIntegral;*/

                        radianceIntegral += transmittanceIntegral * radiance * stepSize;

                        transmittanceIntegral *= transmittance;
                    }

                    rayDistance += stepSize;

                    UNITY_BRANCH
                    if (all(transmittanceIntegral <= 0.001) || rayDistance > maxRayDistance)
                    {
                        break;
                    }
                }
                float contrast = 1 + _VolumetricLightingParam2.y;
                radianceIntegral.rgb = LuminanceContrast(radianceIntegral.rgb, contrast);

                //alpha设置为1，则混合方式变成additive，画面会更通透
                return float4(radianceIntegral, 1);
                //return float4(radianceIntegral, dot(transmittanceIntegral, float3(0.3333,0.3333,0.3333)));
            }

            float4 MarchVolumetricFog(const float3 rayOrigin, const float3 rayDirection, const float2 rayNearFar, const float cosTheta, float jitter)
            {
                float3 radianceIntegral = float3(0, 0, 0);
                float3 transmittanceIntegral = 1;

                float maxRayDistance = rayNearFar.y;
                float stepSize = _StepSize * 1.2; //

                float fogDensity = _VolumetricFogParam.z * 0.05;
                float minHeight = _VolumetricFogParam.x;
                float maxHeight = _VolumetricFogParam.y;
                float fadeoutDist = _VolumetricFogParam.w;

                Light mainLight = GetMainLight();
                float3 sunDirection = mainLight.direction;
                float3 sunIrradiance = mainLight.color * _VolumetricFogColor.rgb;
                float3 skyIrradiance = _AmbientColor.rgb;

                //
                float rayDistance = rayNearFar.x + stepSize * jitter;
                float phaseMainLight = PhaseFunction(cosTheta, 0);
                float4 phaseAddLight = RECIPROCAL_PI4 * 2;
                //
                
                UNITY_UNROLL
                for(int stepCounter = 0; stepCounter < 32; ++stepCounter)
                {
                    float3 position = rayDistance * rayDirection + rayOrigin;
                    float height = position.y;
                    float heightFraction = GetHeightFogDensity(position.y, minHeight, maxHeight);// 1 - remapClamped(height, minHeight, maxHeight);
                    float distanceFade = (length(position - _WorldSpaceCameraPos) * fadeoutDist);

                    float density = fogDensity * heightFraction * heightFraction * distanceFade;//1 - exp(-heightFraction)

                    {
                        float shadow = 1;
#if defined(VOLUMETRIC_FOG_SHADOW)
                        shadow = GetShadow(position);
#endif
                        float3 pointLight = 0;

                        pointLight = GetPointSpotLight(position, phaseAddLight);

                        float3 radiance = sunIrradiance * phaseMainLight * 2 * shadow + pointLight;// *ApproximateMultipleScatteringFit(opticalDepth, cosTheta);
                        radiance += skyIrradiance * RECIPROCAL_PI4;
                        radiance *= density;

                        float3 transmittance = exp(-density * stepSize);// * 0.5 削弱消光系数
                        //散射光的解析积分计算
                        //5.6.3 in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
                        /*float clampedExtinction = max(extinction, 1e-5);
                        float3 scatteringIntegral = (radiance - radiance * transmittance) / clampedExtinction;
                        radianceIntegral += transmittanceIntegral * scatteringIntegral;*/
                        
                        radianceIntegral += transmittanceIntegral * radiance * stepSize;

                        transmittanceIntegral *= transmittance;
                    }

                    rayDistance += stepSize; 

                    UNITY_BRANCH
                    if (all(transmittanceIntegral <= 0.0001) || rayDistance > maxRayDistance)
                    {
                        break;
                    }
                }
                //把raymarching没有走完的后半段雾效补上
                if (rayDistance < maxRayDistance)
                {
                    float d = maxRayDistance - rayDistance;
                    float h0 = rayOrigin.y;
                    float h_enter = h0 + rayDirection.y * rayDistance;
                    float h_exit = h0 + rayDirection.y * maxRayDistance;

                    float density_enter = GetHeightFogDensity(h_enter, minHeight, maxHeight);
                    float density_exit = GetHeightFogDensity(h_exit, minHeight, maxHeight);
                    float distFade_enter = (length(rayOrigin + rayDirection * rayDistance - _WorldSpaceCameraPos) * fadeoutDist);
                    float distFade_exit = (length(rayOrigin + rayDirection * maxRayDistance - _WorldSpaceCameraPos) * fadeoutDist);
                    float avgDensity = (density_enter * density_enter * distFade_enter + density_exit * density_exit * distFade_exit) * 0.5 * fogDensity;
 
                    float3 transmittanceLeft = exp(-avgDensity * d); // * 0.5 削弱消光系数
                    //
                    float3 radianceLeft = sunIrradiance * phaseMainLight * 2;
                    radianceLeft += skyIrradiance * RECIPROCAL_PI4;
                    radianceLeft *= avgDensity;

                    float clampedExtinction = max(avgDensity, 1e-5);
                    float3 L = (radianceLeft - radianceLeft * transmittanceLeft) / clampedExtinction;
                    radianceIntegral += transmittanceIntegral * L;

                    transmittanceIntegral *= transmittanceLeft;
                }

                return float4(radianceIntegral, dot(transmittanceIntegral, float3(0.33333, 0.33333, 0.33333)));
            }
            /*float GetBlueNoise(float2 uv, float frame)
            {
                float3 scale = float3(1.0 / 128.0, 1.0 / 64.0, 1.0 / 128.0);

                return SAMPLE_TEXTURE3D_LOD(_BlueNoise, sampler_BlueNoise, float3 (uv.x, float(frame % 64), uv.y) * scale, 0).r;
            }*/

            float4 Frag(Varyings i) : SV_Target
            {
                //
                float depth = SAMPLE_TEXTURE2D(_DepthTexture, sampler_PointClamp, i.texcoord).r; //_CameraDepthTexture  sampler_PointClamp
            #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
            #endif
                Light mainLight = GetMainLight();
                float3 sunDirection = mainLight.direction;

                float3 cameraPosition = _WorldSpaceCameraPos;
                float3 posWS = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP);
                float3 rayDirection = posWS - cameraPosition;

                float viewLen = length(rayDirection);
                rayDirection = rayDirection / viewLen;

                float3 rayOrigin = cameraPosition;

                float4 color = float4(0,0,0,1);
                float near = 0;
                float far = 0;

                float minVolumetricLightingHeight = _VolumetricLightingParam.x;
                float maxVolumetricLightingHeight = _VolumetricLightingParam.y;
                float minVolumetricFogHeight = _VolumetricFogParam.x;
                float maxVolumetricFogHeight = _VolumetricFogParam.y;
                float frameCount = _FrameCount;
                float cosTheta = dot(sunDirection, rayDirection);

                //TODO: instead of blue noise
                //https://github.com/NVIDIAGameWorks/SpatiotemporalBlueNoiseSDK/raw/refs/heads/main/STBN.zip 
                //float jitter = GetBlueNoise(i.positionCS.xy, frameCount); //blur noise需要3D texture
                float jitter = InterleavedGradientNoise(i.positionCS.xy, frameCount); //InterleavedGradientNoise 定义在Random.hlsl
                
#if defined(_OPEN_VOLUMETRIC_LIGHTING)
                int hit = IntersectHeightRange(rayOrigin, rayDirection, minVolumetricLightingHeight, maxVolumetricLightingHeight, near, far);
                if (hit != 0)
                {
                    float2 rayNearFar = float2(near, min(viewLen, far));
                    
                    color = MarchVolumetricLighting(
                        rayOrigin,
                        rayDirection,
                        rayNearFar,
                        cosTheta,
                        jitter
                    );
                }
#endif

#if defined(_OPEN_VOLUMETRIC_FOG)
                int hitFog = IntersectHeightRange(rayOrigin, rayDirection, minVolumetricFogHeight, maxVolumetricFogHeight, near, far);
                if (hitFog != 0) 
                {
                    float2 rayNearFar = float2(near, min(viewLen, far));
                    float4 fogColor = MarchVolumetricFog(
                        rayOrigin,
                        rayDirection,
                        rayNearFar,
                        cosTheta,
                        jitter
                    );

                    color.rgb = color.rgb + fogColor.rgb * color.a;
                    color.a = color.a * fogColor.a;
                    //color = saturate(color + fogColor * color.a);
                }
#endif
                //color.a = 1 - color.a;
                return color; 
            }
            ENDHLSL
        }
    }
}
