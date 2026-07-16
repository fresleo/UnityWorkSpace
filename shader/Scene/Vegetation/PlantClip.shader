Shader "XKnight/Scene/PlantClipV2" 
{
	Properties
	{
        [Main(Base, __, on, off)]
        _Main("基础设置", Float) = 1
		
		[MainTexture] _BaseMap("Main Tex", 2D) = "white" {}
		[Sub(Base)] [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
		[Sub(Base)] _SSSColor("SSS Color", Color) = (1,1,1,1)
		[Sub(Base)] _TopColor("Top Color", Color) = (1,1,1,1)

		[Sub(Base)] _GIExposure("GI Exposure", Range(0.1, 4.0)) = 1.0
		[Sub(Base)] _GIFalloff("GI Falloff", Range(0, 1)) = 1
		[Sub(Base)] _AOOffset("AO Offset", Range(-0.5, 1.0)) = 0.0
		
		[Sub(Base)] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Sub(Base)] _CutOffset("Cut Offset", Range(0.0, 10.0)) = 2.0
		[Sub(Base)] _ClipEnhanceDistance("Clip EnhanceDistance", Range(1.0, 200.0)) = 10.0
		[Sub(Base)] _ClipEnhance("Clip Enhance", Range(0.0, 1.0)) = 0.5
        
        [Main(Translucency, __, on)]
        _Translucency("透射设置", Float) = 1
        [Sub(Translucency)] _TranslucencyStrength ("Translucency Strength", Range(0, 2)) = 1
        [Sub(Translucency)] _TranslucencyDistortion ("Translucency Distortion", Range(0, 1)) = 0.5
        [Sub(Translucency)] _TranslucencyScattering ("Translucency Scattering", Range(0.01, 3)) = 2
		
    	[Main(Wind, __, off, off)]
    	_Wind("风场", Float) = 1
		[Sub(Wind)] _MotionFacingValue("Motion Direction Mask", Range(0.0, 1.0)) = 0
		[Sub(Wind)] _MotionAmplitude_10("Motion Bending", Range(0.0, 3.0)) = 0.5
        [Sub(Wind)] _MotionPosition_10("Motion Rigidity", Range(0.0, 1.0)) = 0.0
        [Sub(Wind)] _MotionSpeed_10("Motion Speed", Range(0, 40)) = 5
        [Sub(Wind)] _MotionScale_10("Motion Scale", Range(0, 20)) = 6
        [Sub(Wind)] _MotionVariation_10("Motion Variation", Range(0, 20)) = 1
		[Sub(Wind)] _MotionAmplitude_20("Motion Squash", Range(0, 2)) = 1
		[Sub(Wind)] _MotionAmplitude_22("Motion Rolling", Range(0, 2)) = 1
		[Sub(Wind)] _MotionSpeed_20("Motion Speed", Range(0, 40)) = 1
		[Sub(Wind)] _MotionScale_20("Motion Scale", Range(0, 20)) = 6
		[Sub(Wind)] _MotionVariation_20("Motion Variation", Range(1, 20)) = 1
		[Sub(Wind)] _MotionAmplitude_32("Motion Flutter", Range(0, 2)) = 1
		[Sub(Wind)] _MotionSpeed_32("Motion Speed", Range(0, 40)) = 1
		[Sub(Wind)] _MotionScale_32("Motion Scale", Range(0, 20)) = 6
		[Sub(Wind)] _MotionVariation_32("Motion Variation", Range(0, 20)) = 1
		
		// 用于做半透
		[HideInInspector] _Alpha("Alpha", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "Queue" = "AlphaTest+3" }
		
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off

			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			// -------------------------------------
            // Material Keywords
			#pragma shader_feature_local_fragment _ _TRANSLUCENCY_ON
			#pragma shader_feature_local_vertex _ _WIND_ON
			
			// #define _RECEIVE_SHADOWS_OFF 1
			
			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#define ao saturate(input.color.a + _AOOffset)
			#define _TYPE_PLANT_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif
			
			#include "PlantClipInput.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			#include "WindV2.hlsl"

			struct Attributes
			{
			    float3 positionOS			: POSITION;
			    float3 normalOS				: NORMAL;
			    float4 tangentOS			: TANGENT;
			    float4 color				: COLOR;
			    float4 uv0					: TEXCOORD0;
			    float2 staticLightmapUV		: TEXCOORD1;
				float2 uv2					: TEXCOORD2;
				
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			            
			struct Varyings
			{
			    float4 positionCS : SV_POSITION;

			    half4 uv0			: TEXCOORD0;
			    half4 uv1			: TEXCOORD1;
			    half3 normalWS		: TEXCOORD2;
				half3 faceNormalWS  : TEXCOORD3;
			    half4 color			: TEXCOORD4;
			    float3 positionWS	: TEXCOORD5;
			    float3 positionOS	: TEXCOORD6;

				UBPA_FOG_COORDS(7)
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD8;
				#endif
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 9);
				
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				// wind
				float3 positionWS = TransformObjectToWorld(input.positionOS);
				ComputeWindFinalVertexPosition(input.positionOS, positionWS, input.uv0, 0, input.color);

				output.positionOS = input.positionOS;
				output.positionWS = TransformObjectToWorld(input.positionOS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.faceNormalWS = TransformObjectToWorldNormal(input.color.xyz * 2.0 - 1.0);

				output.color = input.color;
				output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

				OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
				#endif

				output.positionCS = TransformWorldToHClip(output.positionWS);

				UBPA_TRANSFER_FOG(output, output.positionWS);
				
				return output;
			}

			half4 LitPassFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				
				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0.xy);
				clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif

				half3 cameraMinusPositionWS = GetCameraPositionWS() - input.positionWS;
				half3 distanceToCamera = length(cameraMinusPositionWS);
				half3 viewDir = normalize(cameraMinusPositionWS);

				half3 faceNormalWS = normalize(input.faceNormalWS);
				half faceNormalWSDotViewDir = clamp(abs(dot(faceNormalWS, viewDir)) - 0.1, 0.0, 1.0);
				half faceCutoff = faceNormalWSDotViewDir * (1.0 - _CutOffset) + _CutOffset;

				half distanceCutoff = clamp(distanceToCamera / _ClipEnhanceDistance, 0.0, 1.0) * _ClipEnhance;

				half cutoff = distanceCutoff + albedoAlpha.a - _Cutoff * faceCutoff;

				// clip(cutoff);
				// return input.color.aaaa;

				half3 albedo = lerp(_BaseColor, _TopColor, smoothstep(0.0, 1.0, input.normalWS.y * 0.5 + 0.5)).rgb * albedoAlpha.rgb;

				float4 shadowCoord = float4(0, 0, 0, 0);
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				shadowCoord = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#endif

				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
				#elif !defined (LIGHTMAP_ON)
				half4 shadowMask = unity_ProbesOcclusion;
				#else
				half4 shadowMask = half4(1, 1, 1, 1);
				#endif

                Light light = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
				
                half NdotL = saturate(dot(input.normalWS, light.direction));
				// half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
                half3 directDiffuse = attenuatedLightColor * albedo * NdotL * ao;
				
                half3 irradiance = lerp(half3(1,1,1), SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalize(input.normalWS)), _GIFalloff) * _GIExposure * ao;
				
                half3 indirectDiffuse = irradiance * albedo;

				half3 color = directDiffuse + indirectDiffuse;
				
                half3 sss = .0f;
				#ifdef _TRANSLUCENCY_ON
			    half3 lightDir = light.direction + input.normalWS * _TranslucencyDistortion;
			    half angle = dot(viewDir, -lightDir);
			    half transVdotL = saturate(pow(angle, _TranslucencyScattering ) * _TranslucencyStrength);
			    sss = transVdotL * _SSSColor;

				// 补光
				// half fakeLightNdotL = pow(saturate(dot(input.normalWS, half3(0, -1, 0))), _TranslucencyFakeLightFalloff) * _TranslucencyFakeLightIntensity;
				// sss += fakeLightNdotL * albedo * _TranslucencyFakeColor;
				#endif

				color += sss;

				// point light
                half3 additionalColor = half3(0, 0, 0);
    
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
                    half3 directDiffuse = light.color * (light.distanceAttenuation * light.shadowAttenuation) * NdotL * albedo;
                    
                    additionalColor += directDiffuse;
                }
    
				color += additionalColor;
				
				UBPA_APPLY_FOG(input, color);
				
				return half4(color, _Alpha);
			}

			ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
