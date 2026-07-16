Shader "XKnight/Scene/GemPro" 
{
	Properties
	{
		[HideInInspector] _SurfaceType("__surface", Float) = 0.0
		
		[MainColor] [HDR] _BaseColor("Color", Color) = (1,1,1,1)
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		_Saturation("Saturation", Range(-1, 100)) = 1
		_Smoothness("Smooth", Range(0, 1)) = 1
		_Metallic("Metalic", Range(0, 1)) = 0
		_Contrast("Contrast", Range(0, 4)) = 1
		_Alpha("Alpha", Range(0, 1)) = 0.5
		
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Range(0.0, 2.0)) = 1
		
		_CubeMap("Cubemap", Cube) = "black" {}
		_CubemapColor("CubemapColor", Color) = (1,1,1,1)
		_ReflectionIntensity("Reflcetion Intensity", Range(0, 20)) = 1
		_Blur("Blur", Range(0, 8)) = 0
		
		[Toggle] _RefractionEnabled("Refraction Enabled", Float) = 1
		_IOR("IOR", Range(-0.25 , 0.3)) = -0.15
		
        //Emission
        [ToggleOff] _EmissionOn("EmissionOn",Float) = 0.0
        [HDR] _EmissionColor("EmissionColor", Color) = (0.0,0.0,0.0)
        _EmissionStrength("EmissionStrength", Range(0, 2.0)) = 1
		
		_FresnelColor("Fresnel Color", Color) =(1,1,1,1)
		_FresnelIntensity("Fresnel Intensity", Range(0, 30)) = 1
		_FresnelPower("Fresnel Power", Range(0.1, 10)) = 1
		
		[Toggle] _OutlineEnabled("Outline Enabled", Float) = 0
		_MeshPreview("Mesh Preview", Float) = 0
		_OutlineWidth("Outline Width", Range(0.1, 40)) = 1
		_OutlineColor("Outline Color", Color) = (1,1,1,1)
		
		// Dissolve
		_DissolveType("Dissolve Type", Float) = 0
		_Dissolve("Dissolve", Float) = 0
		_Random_Dissolve("Random Dissolve", Int) = 0
        _DissolveTex("Dissolve Texture", 2D) = "white" {}
        _DissolveTexChannel("Dissolve Texture Channel", Vector) = (1,0,0,0)
		_DissolveFadingMin("Dissolve Fading Min", Range(0, 1.0)) = 0
		_DissolveFadingMax("Dissolve Fading Max", Range(0, 1.0)) = 0.2
		_EdgeWidth("Edge Width", Range(0,0.3)) = 0.1
		[HDR] _EdgeColor1("Edge Color1", Color) = (1,0,0,1)
		[HDR] _EdgeColor2("Edge Color2", Color) = (0,1,0,1)
        _DissolveCutoff("Dissolve Cutoff", Range(-1, 2)) = 0.5
		_DissolveDir("DirectionDir", Vector) = (0, 1, 0)
		
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
		
		_RenderQueue("Render Queue", Float) = 2000
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		// ForwardLit
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
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
            // Pipeline keywords
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			
			#pragma multi_compile _ _REFRACTION_ON

			// -------------------------------------
            // Material Keywords
			#pragma shader_feature_local_fragment _ _EMISSION

			#pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif
			
			#include "GemInput.hlsl"
			#include "GemDissolve.hlsl"
			#include "../ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float3 normalOS				: NORMAL;
				float4 tangentOS			: TANGENT;
				float2 texcoord				: TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float2	uv						: TEXCOORD0;
				float3	positionWS				: TEXCOORD1;
				float3	normalWS				: TEXCOORD2;
				float4	tangentWS				: TEXCOORD3;
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
				float4	positionSS				: TEXCOORD5;
				DISSOLVE_FACTOR(6)
				float4	positionCS				: SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				
				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

				// already normalized from normal transform to WS.
				output.normalWS = normalInput.normalWS;

				real sign = input.tangentOS.w * GetOddNegativeScale();
				output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

				OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

				output.positionSS = ComputeScreenPos(vertexInput.positionCS);

				output.positionWS = vertexInput.positionWS;

				output.positionCS = vertexInput.positionCS;

				DISSOLVE_TRANSFER_FACTOR(output, input.positionOS, _DissolveDir)

				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				
				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif
				
				// refraction + cubemap + fresnel + pbr
				half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);

				InputData inputData = (InputData)0;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);

				float sgn = input.tangentWS.w;      // should be either +1 or -1
				float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
				half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
				inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));

				// refraction
				float2 offsetUV = TransformWorldToTangent(refract(-inputData.viewDirectionWS, input.normalWS, _IOR), tangentToWorld).xy;
				half3 refractionColor = SampleSceneColor(input.positionSS.xy / input.positionSS.w + offsetUV);

				// cubemap
				half4 cubeMapColor = SAMPLE_TEXTURECUBE_LOD(_CubeMap, sampler_CubeMap, reflect(-inputData.viewDirectionWS, inputData.normalWS), _Blur);
				cubeMapColor.rgb = DecodeHDREnvironment(cubeMapColor, _CubeMap_HDR);
				// 因移动端使用rgbm编码，最大值为34.49，而pc端是真hdr，无上限，所以特此处理
				cubeMapColor.rgb = min(cubeMapColor.rgb, 34.49);
				cubeMapColor.rgb *= _CubemapColor * _ReflectionIntensity * 0.1;

				// Fresnel
				half3 fresnelColor = Fresnel(_FresnelPower, 0.05, _FresnelIntensity, input.normalWS, inputData.viewDirectionWS) * _FresnelColor;

				half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);// * _BaseColor;
				half3 albedowithColor = albedo * _BaseColor;

				//Desaturate
				float  desaturate = dot(albedowithColor, float3( 0.299, 0.587, 0.114));
				float3 desaturateVar = lerp(desaturate.xxx, albedowithColor, _Saturation);
				float3 desaturateAlbedo = CalculateContrast(_Contrast, float4(desaturateVar, 1.0)).rgb;

				half3 emission = SampleEmission(input.uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
				emission += cubeMapColor + fresnelColor;
				#ifdef _REFRACTION_ON
				emission += refractionColor * _Alpha;
				#endif

				ExtendData extendData = (ExtendData)0;
				extendData.specularScaleBRDF = 1;
				
				half occlusion = 1.0;
				half4 color = FragmentPBR(inputData, extendData,
					desaturateAlbedo, half3(0,0,0), _Metallic, _Smoothness, occlusion, emission, 1.0 - _Alpha);
				
				#if _ADDITIONAL_LIGHTS
                half3 additionalColor = half3(0, 0, 0);
                
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
                	// 因为该材质的basemap为明度图，所以直接使用明度即可
                    half3 directDiffuse = light.color * (light.distanceAttenuation * light.shadowAttenuation) * NdotL * albedo;
                    
                    additionalColor += directDiffuse;
                }

				color.rgb += additionalColor;
				#endif // _ADDITIONAL_LIGHTS
				
				// 此处因上面对albedo的操作可能产生负数，而美术认为功能好用，所以对结果保证非负
				color.rgb = max(float3(0.0, 0.0, 0.0), color.rgb);

				DISSOLVE_APPLY(color, input.uv, input.directionFactor)
				
				return color;
			}

			ENDHLSL
		}

		// ShadowCaster
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
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
            // Pipeline keywords

			// -------------------------------------
            // Material Keywords
			#pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "GemInput.hlsl"
			#include "GemDissolve.hlsl"

			#include "GemShadowCasterPass.hlsl"
			ENDHLSL
		}

        // Outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Outline" }
            
            ZWrite On
            Cull Front

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Pipeline keywords
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _MESH_PREVIEW_MODE

            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "GemInput.hlsl"
            // #include "GemDissolve.hlsl"
            #include "GemOutlinePass.hlsl"
            ENDHLSL
        }
	}

	CustomEditor "XKnight.ShaderGUI.GemShaderGUI"
}
