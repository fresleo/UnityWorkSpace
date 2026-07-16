Shader "XKnight/Scene/Billboard"
{
	Properties
	{
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		[MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor ("Color", Color) = (1,1,1,1)

		_GIIndirectDiffuseBoost ("Indirect Diffuse Boost", Range(-3,5)) = 1
		
		_BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
		_WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
		_SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
	}
	
	// LOD 500 - 支持 MRT
	/*
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest+1" }
		LOD 500
		
		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			//ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY
			
			// #pragma multi_compile_fragment _ _MRT_BUFFER

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			//--------------------------------------
			#include "./BillboardInput.hlsl"
			#include "./BillboardForwardPass.hlsl"
			ENDHLSL
		}
		
		// DepthOnly
		Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            #include "./BillboardInput.hlsl"
            #include "../Common/LitDepthOnlyPass.hlsl"
            ENDHLSL
        }
	}
	*/
	
	// LOD 400~300 - 不支持 MRT
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest+1" }
		LOD 300

		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			//ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			//--------------------------------------
			#include "./BillboardInput.hlsl"
			#include "./BillboardForwardPass.hlsl"
			ENDHLSL
		}

		// PreAlphaTest
		/*
		Pass
		{
			Name "PreAlphaTest"
			Tags{ "LightMode" = "PreAlphaTest" }

			ColorMask 0
			Cull Off

			HLSLPROGRAM

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float4	_BaseMap_ST;
			half4	_BaseColor;
			half	_Cutoff;
			half	_GIIndirectDiffuseBoost;
			CBUFFER_END

			TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float2 texcoord				: TEXCOORD0;
			};

			struct Varyings
			{
				float2	uv						: TEXCOORD0;

				float4	positionCS				: SV_POSITION;
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				//Calculate new billboard vertex position and normal;
				float3 upCamVec = float3(0, 1, 0);
				float3 forwardCamVec = -normalize(UNITY_MATRIX_V._m20_m21_m22);
				float3 rightCamVec = normalize(UNITY_MATRIX_V._m00_m01_m02);
				float4x4 rotationCamMatrix = float4x4(rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1);
				input.positionOS.x *= length(GetObjectToWorldMatrix()._m00_m10_m20);
				input.positionOS.y *= length(GetObjectToWorldMatrix()._m01_m11_m21);
				input.positionOS.z *= length(GetObjectToWorldMatrix()._m02_m12_m22);
				input.positionOS = mul(input.positionOS, rotationCamMatrix);
				input.positionOS.xyz += GetObjectToWorldMatrix()._m03_m13_m23;
				//Need to nullify rotation inserted by generated surface shader;
				input.positionOS = mul(GetWorldToObjectMatrix(), input.positionOS);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
				clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

				return 0;
			}

			ENDHLSL
		}
		*/
		
		// DepthOnly
		Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            #include "./BillboardInput.hlsl"
            #include "../Common/LitDepthOnlyPass.hlsl"
            ENDHLSL
        }
		
		// DepthNormals
		// This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            #include "./BillboardInput.hlsl"
            #include "../Common/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

		// DepthMask
		Pass
		{
			Name "DepthMask"
			Tags { "LightMode" = "DepthMask" }
			
			Cull Off
			
			HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // #pragma multi_compile_fragment _BLOOMFACTORMASK _WATERCOLORMASK _SCENESPACEOUTLINEMASK

            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            #include "./BillboardInput.hlsl"
            #include "../Common/LitDepthMask.hlsl"
            ENDHLSL
		}

		// ViewSpaceNormals
        Pass
        {
            Name "ViewSpaceNormals"
            Tags
            {
                "LightMode" = "ViewSpaceNormals"
            }
            
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON
            
            // -------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            #include "./BillboardInput.hlsl"
            #include "../Common/LitViewSpaceNormals.hlsl"
            ENDHLSL
        }
	}
}
