Shader "XKnight/Scene/GrassV2"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _Main("基础设置", Float) = 1
        
		[HideInInspector] _MainTex ("MainTex", 2D) = "white" {}
        [Sub(Base)] _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        [Sub(Base)] _SpecularColor2("Specular2 Color", Color) = (1, 1, 1, 1)
        [Sub(Base)] _AOStrength("AO Strength", Range(0, 1)) = 0
        [Sub(Base)] _PersectiveCorrection("Persective Correction Strength", Range(0, 1)) = 0
        [SubToggle(Base, __)] _Intersection("Intersection", Float) = 1
        [Sub(Base)] _GIIntensity("GI Intensity", Range(1, 3)) = 1
        
        [Main(VariationColor, _, off, off)]
        _VariationColor("杂色", Float) = 0
        [Sub(VariationColor)] _VariationMask("Variation Mask", 2D) = "white" {}
        [Sub(VariationColor)] _VariationMaskScale("Variation Scale", Float) = 50
        [Sub(VariationColor)] _VariationColorA("Variation Color A", Color) = (1,1,1,1)
        [Sub(VariationColor)] _VariationColorB("Variation Color B", Color) = (1,1,1,1)
        
        [Main(BlendWithTerrain, __, off)]
        _BlendWithTerrain("地形混合", Float) = 1
        [Sub(BlendWithTerrain)] _BlendWithTerrainStrength("Blend Strength", Range(0.0, 1.0)) = 0.0
        [Sub(BlendWithTerrain)] _BlendWithTerrainHeight("Blend Height", Range(0.0, 1.0)) = 0.0
        [Sub(BlendWithTerrain)] _BBlendWithTerrainStrength("B Blend Strength", Range(0.0, 1.0)) = 1.0
        [Sub(BlendWithTerrain)] _BBlendWithTerrainHeight("B Blend Height", Range(0.0, 1.0)) = 1.0
    	
    	[Main(Wind, __, off, off)]
    	_Wind("风场", Float) = 1
    	[Sub(Wind)] [HDR] _MotionHighLightColor("Motion HighLight Color", Color) = (1,1,1,1)
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
		[Sub(Wind)] _MotionVariation_32("Motion Variation", Range(1, 20)) = 1
        
        _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
        _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque"  "RenderPipeline"="UniversalPipeline" "Queue" = "Geometry+20" }
        
        Pass
        {
            Name "Universal Forward"
            
            Tags{ "LightMode" = "UniversalForward" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

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
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON
            #pragma shader_feature_local_fragment _ _BLENDWITHTERRAIN_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define ao input.color.g
            #define _WIND_V2_GRASS_MODE_ON 1
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #include "../../ShaderLibrary/Lighting.hlsl"
            // #include "../ShaderLibrary/InteractiveParams.hlsl"
            
            // Properties
	        CBUFFER_START(UnityPerMaterial)
	            half4 _SpecularColor;
	            half4 _SpecularColor2;
	            half _AOStrength;
	            half _PersectiveCorrection;
	            half _GIIntensity;

	            half _WindVariation;
	            half _WindStrength;
	            half _TurbulenceStrength;
	            
	            half _VariationMaskScale;
	            half3 _VariationColorA;
	            half3 _VariationColorB;

	            half _BlendWithTerrainStrength;
	            half _BlendWithTerrainHeight;
	            half _BBlendWithTerrainStrength;
	            half _BBlendWithTerrainHeight;

	            half4 _MotionHighLightColor;
			    half _MotionFacingValue;

			    half _MotionAmplitude_10;     // Motion Bending
			    half _MotionPosition_10; // Motion Rigidity,主干刚度
			    half _MotionSpeed_10;
			    half _MotionScale_10;
			    half _MotionVariation_10;
			    
			    half _InteractionAmplitude;
			    half _InteractionMaskValue;

			    half _MotionAmplitude_20;
			    half _MotionAmplitude_22;
			    half _MotionSpeed_20;
			    half _MotionScale_20;
			    half _MotionVariation_20;

			    half _MotionAmplitude_32;
			    half _MotionSpeed_32;
			    half _MotionScale_32;
			    half _MotionVariation_32;

	            // 遮罩
	            half _BloomFactor, _WaterColorOn;
	        CBUFFER_END

            TEXTURE2D(_VariationMask);   SAMPLER(sampler_VariationMask);

            #include "../../ShaderLibrary/GrassBlendWithTerrainAlbedo.hlsl"
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
				float2	uv						: TEXCOORD0;
				float3	positionWS				: TEXCOORD1;
				float3	normalWS				: TEXCOORD2;
				UBPA_FOG_COORDS(3)
				float4	shadowCoord				: TEXCOORD4;

				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);

				float4  color					: TEXCOORD6;
				float4  hightLight              : TEXCOORD7;
				float4	positionCS				: SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				// wind v2
				float3 positionWS = TransformObjectToWorld(input.positionOS);
            	half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
				half highLight = ComputeWindFinalVertexPosition(input.positionOS, positionWS, input.uv0, 0, input.color);

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = normalWS;
            	output.uv = input.uv0.xy;
            	output.color = input.color;
            	output.hightLight = highLight;

                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

            	// #ifdef _INTERSECTION_ON
            	// positionWS += VegetationInteractiveWS(positionWS, input.uv0.y);
            	// #endif
            	
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            	output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
				#endif

                output.positionCS = TransformWorldToHClip(output.positionWS);

                UBPA_TRANSFER_FOG(output, output.positionWS);

                return output;
            }
            
            void frag(Varyings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);

            	#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif

                half3 albedo = half3(0,0,0);
                half3 blendTerrainColor = half3(0,0,0);
                half3 blendTerrainColor2 = half3(0,0,0);

                half2 variationMaskUV = input.positionWS.xz * rcp(_VariationMaskScale.x);
                half variationMask = SAMPLE_TEXTURE2D(_VariationMask, sampler_VariationMask, variationMaskUV).r;

                half mask = smoothstep(_BlendWithTerrainHeight, 1.0 + _BlendWithTerrainHeight, sqrt(input.uv.y));
		        blendTerrainColor.rgb = lerp(ApplyColorMap(input.positionWS.xyz, _VariationColorA.rgb, _BlendWithTerrainStrength), _VariationColorA.rgb, mask);
                half mask2 = smoothstep(_BBlendWithTerrainHeight, 1.0 + _BBlendWithTerrainHeight, sqrt(input.uv.y));
                blendTerrainColor2.rgb = lerp(ApplyColorMap(input.positionWS.xyz, _VariationColorB.rgb, _BBlendWithTerrainStrength), _VariationColorB.rgb, mask2);
                
                half3 baseColor = lerp(blendTerrainColor, blendTerrainColor2, variationMask);
                albedo = lerp(baseColor, baseColor * ao, _AOStrength) * (input.hightLight * _MotionHighLightColor + half3(1,1,1));

				float4 shadowCoord = float4(0, 0, 0, 0);
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            	shadowCoord = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
            	shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#endif

                Light light = GetMainLight(shadowCoord, input.positionWS, unity_ProbesOcclusion);
                
                half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                half NdotL = dot(input.normalWS, light.direction) * 0.5 + 0.5;
                half3 directDiffuse = attenuatedLightColor * NdotL * albedo;

                half3 irradiance = SampleSH(input.normalWS) * lerp(_GIIntensity, 1, light.shadowAttenuation);
                half3 indirectDiffuse = irradiance * albedo;

                half3 specularColor = lerp(_SpecularColor.rgb, _SpecularColor2.rgb, variationMask);
                half3 directSpecular = saturate(pow(input.uv.y, 8) * 0.8) * attenuatedLightColor * specularColor; 

                half3 shadingColor = indirectDiffuse + directDiffuse;// + directSpecular;

                UBPA_APPLY_FOG(input, shadingColor);

                outColor = half4(shadingColor, 1.0f);
            }
            
            ENDHLSL
        }
    }
	
	Fallback Off
	
    CustomEditor "LWGUI.LWGUI"
}
