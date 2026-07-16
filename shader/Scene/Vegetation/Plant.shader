Shader "XKnight/Scene/PlantV2" 
{
	Properties
	{
		[Main(Base, __, on, off)]
		_Main("基础设置", Float) = 1
		[Sub(Base)] [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
		[Sub(Base)] [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[Sub(Base)] _BumpMixMap("BumpMix Map", 2D) = "white" {}
		[Sub(Base)] _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

		[Main(Wind, __, on, off)]
		_Wind("风场设置", Float) = 1
		[Sub(Wind)] _MotionFacingValue("Motion Direction Mask", Range(0.0, 1.0)) = 0
		
		[Sub(Wind)] _MotionAmplitude_10("弯曲强度(Motion Bending)", Range(0.0, 3.0)) = 0.5
		[Sub(Wind)] _MotionPosition_10("硬度(Motion Rigidity)", Range(0.0, 1.0)) = 0.0
		[Sub(Wind)] _MotionSpeed_10("速度(Motion Speed)", Range(0, 40)) = 5
		[Sub(Wind)] _MotionScale_10("缩放(Motion Scale)", Range(0, 20)) = 6
		[Sub(Wind)] _MotionVariation_10("变化程度(Motion Variation)", Range(0, 20)) = 1
		[Sub(Wind)] _MotionAmplitude_20("挤压强度(Motion Squash)", Range(0, 2)) = 1
		[Sub(Wind)] _MotionAmplitude_22("摆动强度(Motion Rolling)", Range(0, 2)) = 1
		[Sub(Wind)] _MotionSpeed_20("速度(Motion Speed)", Range(0, 40)) = 1
		[Sub(Wind)] _MotionScale_20("缩放(Motion Scale)", Range(0, 20)) = 6
		[Sub(Wind)] _MotionVariation_20("变化程度(Motion Variation)", Range(1, 20)) = 1
		[Sub(Wind)] _MotionAmplitude_32("震动(Motion Flutter)", Range(0, 2)) = 1
		[Sub(Wind)] _MotionSpeed_32("速度(Motion Speed)", Range(0, 40)) = 1
		[Sub(Wind)] _MotionScale_32("缩放(Motion Scale)", Range(0, 20)) = 6
		[Sub(Wind)] _MotionVariation_32("变化程度(Motion Variation)", Range(1, 20)) = 1
		
		[HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
		
		// 用于做半透
		[HideInInspector] _Alpha("Alpha", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

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
			#pragma shader_feature_local_vertex _ _WIND_ON

			//--------------------------------------
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif
			
			#include "../../ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			#include "PlantInput.hlsl"
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
				float2	uv						: TEXCOORD0;
				float3	positionWS				: TEXCOORD1;
				float3	normalWS				: TEXCOORD2;
				float4	tangentWS				: TEXCOORD3;	
				UBPA_FOG_COORDS(4)
				float4	shadowCoord				: TEXCOORD5;

				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
				
				float4	positionCS				: SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				// wind v2
				float3 positionWS = TransformObjectToWorld(input.positionOS);
				ComputeWindFinalVertexPosition(input.positionOS, positionWS, input.uv0, 0, input.color);
				
				output.positionWS = TransformObjectToWorld(input.positionOS);
				output.uv.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

				OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				
				// already normalized from normal transform to WS.
				output.normalWS = normalInput.normalWS;

				real sign = input.tangentOS.w * GetOddNegativeScale();
				output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
				
				output.positionCS = TransformWorldToHClip(output.positionWS);

				UBPA_TRANSFER_FOG(output, positionWS);
				
				return output;
			}

			half3 SampleNormalMapMix(float2 uv, out half metallic, out half ao)
			{
				float4 n = SAMPLE_TEXTURE2D(_BumpMixMap, sampler_BumpMixMap, uv);
				
				ao = LerpWhiteTo(n.b, _OcclusionStrength);
				metallic = n.a;

				float3 normal;
				normal.xy = n.xy * 2.0f - 1.0f;
				normal.z = max(1.0e-16, sqrt(1.0f - saturate(dot(normal.xy, normal.xy))));

				return normal;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif

				SurfaceData outSurfaceData = (SurfaceData)0;
				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
				
				outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

				half metallic, ao;
				half3 normalMix = SampleNormalMapMix(input.uv, metallic, ao);
				outSurfaceData.smoothness = 1.0f - albedoAlpha.a;
				outSurfaceData.occlusion = ao;
				outSurfaceData.normalTS = normalMix;
				outSurfaceData.metallic = metallic;

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;

				float sgn = input.tangentWS.w;      // should be either +1 or -1
				float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

				inputData.normalWS = TransformTangentToWorld(outSurfaceData.normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				inputData.shadowCoord = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
				inputData.shadowCoord = float4(0,0,0,0);
				#endif
				
				inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
				inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

				ExtendData extendData = (ExtendData)0;
				extendData.specularScaleBRDF = 1;
				
				half4 color = FragmentPBR(inputData, outSurfaceData, extendData);
				UBPA_APPLY_FOG(input, color);
				return color;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ _ALPHATEST_ON

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#include "PlantInput.hlsl"
			#include "../../Common/SimpleLitShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
