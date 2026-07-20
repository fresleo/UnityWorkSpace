Shader "GarenaTA/GPerObjectShadow/Decal"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0)
        [HideInInspector]_DrawOrder ("Draw Order", Range(-50, 50)) = 0
        [HideInInspector][Enum(Depth Bias, 0, View Bias, 1)]_DecalMeshBiasType ("DecalMesh BiasType", Float) = 0
        [HideInInspector]_DecalMeshDepthBias ("DecalMesh DepthBias", Float) = 0
        [HideInInspector]_DecalMeshViewBias ("DecalMesh ViewBias", Float) = 0
        [HideInInspector][NoScaleOffset]unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" { }
        [HideInInspector][NoScaleOffset]unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" { }
        [HideInInspector][NoScaleOffset]unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" { }

        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 10 // ScrAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 5 // OneMinusSrcAlpha
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "DecalScreenSpaceProjector"
            Tags { "LightMode" = "DecalScreenSpaceProjector" }
            
            // Render State
            Cull Front
            // Blend SrcAlpha OneMinusSrcAlpha
            Blend[_SrcBlend][_DstBlend]
            ZTest Greater
            ZWrite Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma editor_sync_compilation
            
            // Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
            #pragma multi_compile _ _DECAL_LAYERS
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SH
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV
            #define VARYINGS_NEED_SHADOW_COORD
            
            #define SHADERPASS SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR
            #define _MATERIAL_AFFECTS_ALBEDO 1
            #define _MATERIAL_AFFECTS_NORMAL 1
            #define _MATERIAL_AFFECTS_NORMAL_BLEND 1
            
            // -- Properties used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
                float4 _SelectionID;
            #endif

            #if _RENDER_PASS_ENABLED
                #define GBUFFER3 0
                #define GBUFFER4 1
                FRAMEBUFFER_INPUT_HALF(GBUFFER3);
                FRAMEBUFFER_INPUT_HALF(GBUFFER4);
            #endif

            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            #include "./GPerObjectShadow.hlsl"

            // --------------------------------------------------
            // Structs and Packing
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _DrawOrder;
                float _DecalMeshBiasType;
                float _DecalMeshDepthBias;
                float _DecalMeshViewBias;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                return output;
            }

            void Frag(Varyings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    // Adjust Z to match NDC for OpenGL ([-1, 1])
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                half perObjectShadow = PerObjectRealtimeShadow(worldPos);

                outColor = half4(perObjectShadow.xxx, 1 - perObjectShadow);
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Shader Graph/FallbackError"
}