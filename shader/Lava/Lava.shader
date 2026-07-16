Shader "XKnight/Lava/Lava"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A("全局", Float) = 1
        [Sub(A)]_GlobalTiling("全局 Tiling", Range(0.001, 100)) = 1
        [Sub(A)][ToggleUI]_UVVDirection1UDirection0("UV 方向 V(T) U(F)", Float) = 1
        [Sub(A)][NoScaleOffset]_ColdLavaAlbedo_SM("固有色/光滑度贴图", 2D) = "white" {}
        [Sub(A)][Normal][NoScaleOffset]_ColdLavaNormal("法线贴图", 2D) = "bump" {}
        [Sub(A)][NoScaleOffset]_ColdLavaMT_AO_H_EM("金属/AO/高度/自发光遮罩贴图", 2D) = "black" {}

        [Main(B, __, on, off)]
        _B("冷岩浆层", Float) = 1
        [Sub(B)]_ColdLavaMainSpeed("流动速度", Vector) = (1, 1, 0, 0)
        [Sub(B)]_ColdLavaFlowUVRefresSpeed("流动 UV 刷新速度", Range(0, 1)) = 0.05
        [Sub(B)]_ColdLavaAlbedoColor("固有色染色", Color) = (1, 1, 1, 0)
        [Sub(B)]_ColdLavaAlbedoColorMultiply("固有色倍增", Float) = 1
        [Sub(B)]_ColdLavaTiling("Tiling", Vector) = (1, 1, 0, 0)
        [Sub(B)]_ColdLavaSmoothness("光滑度", Range(0, 1)) = 1
        [Sub(B)]_ColdLavaNormalScale("法线强度", Float) = 1
        [Sub(B)]_ColdLavaMetalic("金属度", Range(0, 1)) = 1
        [Sub(B)]_ColdLavaAO("环境光遮蔽", Range(0, 1)) = 1

        [Main(C, __, on, off)]
        _C("热岩浆层", Float) = 1
        [Sub(C)]_HotLavaAngle("显露角度", Range(0.001, 90)) = 9.8
        [Sub(C)]_HotLavaAngleFalloff("角度衰减", Range(0, 80)) = 1.5
        [Sub(C)]_HotLavaHeightBlendTreshold("高度混合阈值", Range(0, 10)) = 3.09
        [Sub(C)]_HotLavaHeightBlendStrenght("高度混合强度", Range(0, 20)) = 2.37
        [Sub(C)]_HotLavaBlendMax("热层最大混合上限", Range(0, 1)) = 0.85
        [Sub(C)]_HotLavaMainSpeed("流动速度", Vector) = (1, 1, 0, 0)
        [Sub(C)]_HotLavaFlowUVRefreshSpeed("流动 UV 刷新速度", Range(0, 1)) = 0.05
        [Sub(C)]_HotLavaAlbedoColor("固有色染色", Color) = (1, 1, 1, 0)
        [Sub(C)]_HotLavaAlbedoColorMultiply("固有色倍增", Float) = 1
        [Sub(C)]_HotLavaTiling("Tiling", Vector) = (1, 1, 0, 0)
        [Sub(C)]_HotLavaSmoothness("光滑度", Range(0, 1)) = 1
        [Sub(C)]_HotLavaNormalScale("法线强度", Float) = 1
        [Sub(C)]_HotLavaMetallic("金属度", Range(0, 1)) = 1
        [Sub(C)]_HotLavaAO("环境光遮蔽", Range(0, 1)) = 1

        [Main(D, __, on, off)]
        _D("自发光", Float) = 1
        [Sub(D)][HDR]_LavaEmissionColor("自发光颜色", Color) = (1, 0.1862055, 0, 0)
        [Sub(D)]_ColdLavaEmissionMaskIntensivity("冷层 遮罩强度", Range(0, 100)) = 1.9
        [Sub(D)]_ColdLavaEmissionMaskTreshold("冷层 遮罩阈值", Float) = 2.55
        [Sub(D)]_HotLavaEmissionMaskIntensivity("热层 遮罩强度", Range(0, 100)) = 2
        [Sub(D)]_HotLavaEmissionMaskTreshold("热层 遮罩阈值", Float) = 9.52
        [Sub(D)]_EmissionWhiteoutThreshold("亮白阈值(亮度超过此值开始趋白)", Range(0, 20)) = 3
        [Sub(D)]_EmissionWhiteoutStrength("亮白强度", Range(0, 1)) = 0.6

        [Main(E, __, on, off)]
        _E("边缘光", Float) = 1
        [Sub(E)][HDR]_RimColor("边缘光颜色", Color) = (1, 0, 0, 0)
        [Sub(E)]_RimLightPower("边缘光强度", Float) = 4

        [Main(F, __, on, off)]
        _F("噪声", Float) = 1
        [Sub(F)][NoScaleOffset]_Noise("噪声贴图", 2D) = "white" {}
        [Sub(F)]_NoiseTiling("噪声 Tiling", Vector) = (1, 1, 0, 0)
        [Sub(F)]_NoiseSpeed("噪声速度", Vector) = (0.5, 0.5, 0, 0)
        [Sub(F)]_NoiseFlowUVRefreshSpeed("噪声 UV 刷新速度", Range(0, 1)) = 0.05
        [Sub(F)]_ColdLavaNoisePower("冷层 噪声幂", Range(0, 10)) = 6.45
        [Sub(F)]_HotLavaNoisePower("热层 噪声幂", Range(0, 10)) = 5.48

        [Main(G, __, on, off)]
        _G("视差(冷却壳高度)", Float) = 1
        [Sub(G)]_ParallaxScale("视差缩放", Range(0, 0.1)) = 0.02
        [Sub(G)]_ParallaxStrength("视差强度", Range(0, 2)) = 1

        [Main(H, __, on, off)]
        _H("温度与脉动", Float) = 1
        [Sub(H)][HDR]_TemperatureColorCold("温度 冷色(暗红)", Color) = (0.2, 0.02, 0, 1)
        [Sub(H)][HDR]_TemperatureColorHot("温度 热色(亮黄白)", Color) = (2, 1.2, 0.3, 1)
        [Sub(H)]_TemperatureNoiseInfluence("温度 噪声影响", Range(0, 1)) = 0.3
        [Sub(H)]_PulseSpeed("脉动 速度", Range(0.1, 4)) = 0.8
        [Sub(H)]_PulseStrength("脉动 强度", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float  _GlobalTiling;
            float  _UVVDirection1UDirection0;

            float2 _ColdLavaMainSpeed;
            float  _ColdLavaFlowUVRefresSpeed;
            float4 _ColdLavaAlbedoColor;
            float  _ColdLavaAlbedoColorMultiply;
            float2 _ColdLavaTiling;
            float  _ColdLavaSmoothness;
            float  _ColdLavaNormalScale;
            float  _ColdLavaMetalic;
            float  _ColdLavaAO;

            float  _HotLavaAngle;
            float  _HotLavaAngleFalloff;
            float  _HotLavaHeightBlendTreshold;
            float  _HotLavaHeightBlendStrenght;
            float2 _HotLavaMainSpeed;
            float  _HotLavaFlowUVRefreshSpeed;
            float4 _HotLavaAlbedoColor;
            float  _HotLavaAlbedoColorMultiply;
            float2 _HotLavaTiling;
            float  _HotLavaSmoothness;
            float  _HotLavaNormalScale;
            float  _HotLavaMetallic;
            float  _HotLavaAO;
            float  _HotLavaBlendMax;

            float4 _LavaEmissionColor;
            float  _ColdLavaEmissionMaskIntensivity;
            float  _ColdLavaEmissionMaskTreshold;
            float  _HotLavaEmissionMaskIntensivity;
            float  _HotLavaEmissionMaskTreshold;
            float  _EmissionWhiteoutThreshold;
            float  _EmissionWhiteoutStrength;

            float4 _RimColor;
            float  _RimLightPower;

            float2 _NoiseTiling;
            float2 _NoiseSpeed;
            float  _NoiseFlowUVRefreshSpeed;
            float  _ColdLavaNoisePower;
            float  _HotLavaNoisePower;

            float  _ParallaxScale;
            float  _ParallaxStrength;

            float4 _TemperatureColorCold;
            float4 _TemperatureColorHot;
            float  _TemperatureNoiseInfluence;
            float  _PulseSpeed;
            float  _PulseStrength;
        CBUFFER_END

        TEXTURE2D(_ColdLavaAlbedo_SM);
        TEXTURE2D(_ColdLavaNormal);
        TEXTURE2D(_ColdLavaMT_AO_H_EM);
        TEXTURE2D(_Noise);
        SAMPLER(sampler_linear_repeat);

        float2 GetFlowDirection(float2 speed, float2 tiling)
        {
            float2 flowVec = speed * tiling;
            return _UVVDirection1UDirection0 > 0.5 ? flowVec : flowVec.yx;
        }

        float2 ComputeScrollUV(float2 tiling, float2 speed, float scrollSpeed, float2 uv0)
        {
            float2 flowDir = GetFlowDirection(speed, tiling);
            return tiling * uv0 / _GlobalTiling + flowDir * _Time.y * scrollSpeed;
        }

        half ComputeAngleMask(float3 worldNormalWS, float angle, float falloff)
        {
            float normalizedAngle = angle / 45.0;
            float absNY = saturate(abs(worldNormalWS.y));
            float rawMask = saturate((absNY - (1.0 - normalizedAngle)) / normalizedAngle);
            return (half)saturate(pow(abs(1.0 - rawMask), falloff));
        }

        half HeightBlendSplat(half height, half angleMask, half strength)
        {
            half v = height * angleMask * 4.0 + angleMask * 2.0;
            return saturate(pow(abs(v), strength));
        }

        half ComputeEmissionMask(half emA, half intensity, half threshold)
        {
            return pow(abs(emA * intensity), threshold);
        }

        half3 ApplyNormalStrength(half3 n, half scale)
        {
            return half3(n.xy * scale, lerp(1.0, n.z, saturate(scale)));
        }

        half3 ApplyEmissionWhiteout(half3 emission, half threshold, half strength)
        {
            half luma = max(emission.r, max(emission.g, emission.b));
            half t = saturate((luma - threshold) / max(threshold, 0.001));
            return lerp(emission, half3(luma, luma, luma), t * strength);
        }

        struct LayerData
        {
            half3 albedo;
            half  smoothness;
            half3 normalTS;
            half  metallic;
            half  ao;
            half  emissionMask;
            half  heightInv;
        };

        LayerData SampleLayer(
            float2 uv,
            half4 albedoColor, half colorMul, half smoothnessScale,
            half normalScale, half metallicScale, half aoScale,
            half emIntensity, half emThreshold)
        {
            LayerData d = (LayerData)0;
            half4 mask = SAMPLE_TEXTURE2D(_ColdLavaMT_AO_H_EM, sampler_linear_repeat, uv);
            half4 alb  = SAMPLE_TEXTURE2D(_ColdLavaAlbedo_SM, sampler_linear_repeat, uv);

            d.albedo     = alb.rgb * albedoColor.rgb * colorMul;
            d.smoothness = alb.a * smoothnessScale;
            d.normalTS   = ApplyNormalStrength(UnpackNormal(SAMPLE_TEXTURE2D(_ColdLavaNormal, sampler_linear_repeat, uv)), normalScale);
            d.metallic   = metallicScale * mask.r;
            d.ao         = clamp(mask.g, 1.0 - aoScale, 1.0);
            d.heightInv  = 1.0 - mask.b;
            d.emissionMask = ComputeEmissionMask(mask.a, emIntensity, emThreshold);

            return d;
        }

        ENDHLSL

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #include "../ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 uv0        : TEXCOORD0;
                float4 uv1        : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS              : SV_POSITION;
                float3 positionWS              : TEXCOORD0;
                float3 normalWS                : TEXCOORD1;
                float4 tangentWS               : TEXCOORD2;
                float4 uv0                     : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
                float4 fogFactorAndVertexLight  : TEXCOORD5;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord             : TEXCOORD6;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vpi.positionCS;
                output.positionWS = vpi.positionWS;
                output.normalWS   = vni.normalWS;
                output.tangentWS  = float4(vni.tangentWS, input.tangentOS.w);
                output.uv0        = input.uv0;

                #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #else
                    output.vertexSH = SampleSHVertex(output.normalWS);
                #endif

                half fogFactor = ComputeFogFactor(vpi.positionCS.z);
                half3 vertexLight = VertexLighting(vpi.positionWS, vni.normalWS);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vpi);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float2 uv0 = input.uv0.xy;

                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * (input.tangentWS.w * GetOddNegativeScale());
                float3x3 tbn = float3x3(input.tangentWS.xyz, bitangentWS, normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 viewDirTS = normalize(mul(tbn, viewDirWS));
                half heightForParallax = SAMPLE_TEXTURE2D(_ColdLavaMT_AO_H_EM, sampler_linear_repeat, uv0).b;
                half2 parallaxOffset = half2(viewDirTS.xy) / (viewDirTS.z + 0.00001) * heightForParallax * _ParallaxScale * _ParallaxStrength;
                uv0 = uv0 - parallaxOffset;

                float2 coldUV = ComputeScrollUV(_ColdLavaTiling, _ColdLavaMainSpeed, _ColdLavaFlowUVRefresSpeed, uv0);
                float2 hotUV  = ComputeScrollUV(_HotLavaTiling, _HotLavaMainSpeed, _HotLavaFlowUVRefreshSpeed, uv0);

                LayerData cold = SampleLayer(coldUV,
                    _ColdLavaAlbedoColor, _ColdLavaAlbedoColorMultiply, _ColdLavaSmoothness,
                    _ColdLavaNormalScale, _ColdLavaMetalic, _ColdLavaAO,
                    _ColdLavaEmissionMaskIntensivity, _ColdLavaEmissionMaskTreshold);

                LayerData hot = SampleLayer(hotUV,
                    _HotLavaAlbedoColor, _HotLavaAlbedoColorMultiply, _HotLavaSmoothness,
                    _HotLavaNormalScale, _HotLavaMetallic, _HotLavaAO,
                    _HotLavaEmissionMaskIntensivity, _HotLavaEmissionMaskTreshold);

                half hotAngleMask = ComputeAngleMask(normalWS, _HotLavaAngle, _HotLavaAngleFalloff);
                half hotBlend = min(HeightBlendSplat(
                    pow(abs(hot.heightInv), _HotLavaHeightBlendTreshold),
                    hotAngleMask, _HotLavaHeightBlendStrenght), _HotLavaBlendMax);

                half4 blended = lerp(half4(cold.albedo, cold.smoothness), half4(hot.albedo, hot.smoothness), hotBlend);
                half3 blendedNrm = lerp(cold.normalTS, hot.normalTS, hotBlend);
                half3 blendedMAOE = lerp(half3(cold.metallic, cold.ao, cold.emissionMask), half3(hot.metallic, hot.ao, hot.emissionMask), hotBlend);
                half emissionMask = blendedMAOE.z;

                half3 emColor = _LavaEmissionColor.rgb * emissionMask;

                float2 noiseUV = ComputeScrollUV(_NoiseTiling, _NoiseSpeed, _NoiseFlowUVRefreshSpeed, uv0);
                half noiseSample = SAMPLE_TEXTURE2D(_Noise, sampler_linear_repeat, noiseUV).r;

                // 温度/颜色渐变：冷(暗红)->热(亮黄白)，可由噪声带来局部变化
                half temperature = saturate(hotBlend + (noiseSample - 0.5) * _TemperatureNoiseInfluence);
                half3 temperatureTint = lerp(_TemperatureColorCold.rgb, _TemperatureColorHot.rgb, temperature);
                emColor *= temperatureTint;

                half blendedNoisePow = lerp(_ColdLavaNoisePower, _HotLavaNoisePower, hotAngleMask);
                half noiseModulation = clamp(pow(noiseSample, blendedNoisePow) * 20.0, 0.05, 1.2);

                half3 emission = emColor * noiseModulation;

                half rim = pow(abs(1.0 - saturate(dot(blendedNrm, viewDirTS))), 10.0);
                half3 rimEmission = _RimColor.rgb * rim * _RimLightPower * emissionMask;
                emission += rimEmission;

                // 脉动/呼吸：整体发光强度随时间缓慢变化
                half pulse = 1.0 + _PulseStrength * sin(_Time.y * _PulseSpeed);
                emission *= pulse;

                emission = ApplyEmissionWhiteout(emission, _EmissionWhiteoutThreshold, _EmissionWhiteoutStrength);

                SurfaceData surfData = (SurfaceData)0;
                surfData.albedo     = blended.rgb;
                surfData.metallic   = blendedMAOE.x;
                surfData.smoothness = blended.a;
                surfData.normalTS   = blendedNrm;
                surfData.occlusion  = blendedMAOE.y;
                surfData.emission   = emission;
                surfData.alpha      = 1.0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(blendedNrm, half3x3(input.tangentWS.xyz, bitangentWS, normalWS)));
                inputData.viewDirectionWS = viewDirWS;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

                #if defined(LIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                #else
                inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
                #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                ExtendData extendData = (ExtendData)0;
                extendData.specularScaleBRDF = 1;

                half4 color = FragmentPBR(inputData, surfData, extendData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0;

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 _LightDirection;
            float3 _LightPosition;

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - posWS);
                #else
                float3 lightDir = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, lightDir));
                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma vertex DNVert
            #pragma fragment DNFrag

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float4 uv0        : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DNVaryings DNVert(DNAttributes input)
            {
                DNVaryings output = (DNVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS  = vni.normalWS;
                output.tangentWS = float4(vni.tangentWS, input.tangentOS.w);
                output.uv0 = input.uv0;
                return output;
            }

            half4 DNFrag(DNVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = normalize(input.normalWS);
                float2 uv0 = input.uv0.xy / _GlobalTiling * _ColdLavaTiling;

                half3 normalTS = ApplyNormalStrength(UnpackNormal(SAMPLE_TEXTURE2D(_ColdLavaNormal, sampler_linear_repeat, uv0)), _ColdLavaNormalScale);
                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * (input.tangentWS.w * GetOddNegativeScale());
                float3 nWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, normalWS)));
                return half4(nWS, 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex MetaVert
            #pragma fragment MetaFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MetaAttributes
            {
                float4 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;
                float4 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2;
                float3 normalOS   : NORMAL;
            };

            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0        : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            MetaVaryings MetaVert(MetaAttributes input)
            {
                MetaVaryings output = (MetaVaryings)0;
                output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1.xy, input.uv2.xy);
                output.uv0  = input.uv0;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 MetaFrag(MetaVaryings input) : SV_Target
            {
                float2 uv0 = input.uv0.xy / _GlobalTiling * _ColdLavaTiling;

                LayerData cold = SampleLayer(uv0,
                    _ColdLavaAlbedoColor, _ColdLavaAlbedoColorMultiply, _ColdLavaSmoothness,
                    _ColdLavaNormalScale, _ColdLavaMetalic, _ColdLavaAO,
                    _ColdLavaEmissionMaskIntensivity, _ColdLavaEmissionMaskTreshold);

                half3 temperatureTint = lerp(_TemperatureColorCold.rgb, _TemperatureColorHot.rgb, 0.5);
                half3 emission = _LavaEmissionColor.rgb * cold.emissionMask * temperatureTint;
                emission = ApplyEmissionWhiteout(emission, _EmissionWhiteoutThreshold, _EmissionWhiteoutStrength);

                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = cold.albedo;
                metaInput.Emission = emission;

                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "LWGUI.LWGUI"
}
