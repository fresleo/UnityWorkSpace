Shader "GarenaTA/GPerObjectShadow/Resolve"
{
    Properties
    {

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 0 // ScrAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        // _CharacterMaskTexture ("_CharacterMaskTexture", 2D) = "black" { }
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Blend[_SrcBlend][_DstBlend]
        ZTest Greater
        ZWrite Off
        Cull[_Cull]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "./GPerObjectShadow.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            float4 _Color;

            TEXTURE2D(_CharacterMaskTexture);
            SAMPLER(sampler_CharacterMaskTexture);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionHCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.positionHCS.xy / _ScaledScreenParams.xy;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                half perObjectShadow = 1 - PerObjectRealtimeShadow(worldPos);

                float mask = SAMPLE_TEXTURE2D(_CharacterMaskTexture, sampler_CharacterMaskTexture, uv);
                perObjectShadow *= 1 - mask;
                
                half4 col = half4(_Color.rgb, _Color.a * perObjectShadow);

                return col;
            }
            ENDHLSL
        }
    }
}
