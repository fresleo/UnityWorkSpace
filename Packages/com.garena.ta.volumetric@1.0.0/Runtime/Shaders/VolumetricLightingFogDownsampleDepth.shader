Shader "Unlit/VolumetricLightingFogDownsampleDepth"
{

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        LOD 300
        Pass
        {
            Name "DownsampleDepth"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
            ColorMask R

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            float Frag(Varyings input) : SV_Target
            {
                //float4 depths = GATHER_RED_TEXTURE2D(_BlitTexture, sampler_PointClamp, input.texcoord);
                float4 depths = GATHER_RED_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
                
                float minDepth = Min3(depths.x, depths.y, min(depths.z, depths.w));
                float maxDepth = Max3(depths.x, depths.y, max(depths.z, depths.w));

                return (uint(input.positionCS.x + input.positionCS.y) & 1) > 0 ? minDepth : maxDepth;
            }

            ENDHLSL
        }

        
        Pass
        {
            Name "VolumetricUpsample"
            
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One SrcAlpha
            //Blend One One

            HLSLPROGRAM
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float3 _Tint;

            float4 Frag(Varyings input) : SV_Target
            {
                float4 volumetricCol = DepthAwareUpsample(input.texcoord, _BlitTexture, _DepthTexture);
                volumetricCol = saturate(volumetricCol);
                return volumetricCol;
            }

            ENDHLSL
        }
    }

    //
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200
        Pass
        {
            Name "DownsampleDepth"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
            ColorMask R

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            float Frag(Varyings input) : SV_Target
            {
                float4 depths;

                uint2 fullResTopLeftCorner = uint2(input.positionCS.xy * 2.0);

                depths.x = LoadSceneDepth(fullResTopLeftCorner + uint2(0, 1));
                depths.y = LoadSceneDepth(fullResTopLeftCorner + uint2(1, 1));
                depths.z = LoadSceneDepth(fullResTopLeftCorner + uint2(1, 0));
                depths.w = LoadSceneDepth(fullResTopLeftCorner);

                float minDepth = Min3(depths.x, depths.y, min(depths.z, depths.w));
                float maxDepth = Max3(depths.x, depths.y, max(depths.z, depths.w));

                return (uint(input.positionCS.x + input.positionCS.y) & 1) > 0 ? minDepth : maxDepth;
            }

            ENDHLSL
        }

        
        Pass
        {
            Name "VolumetricUpsample"
            
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One SrcAlpha

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./VolumetricLightingFogCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            float3 _Tint;
            float4 Frag(Varyings input) : SV_Target
            {
                float4 volumetricCol = DepthAwareUpsample(input.texcoord, _BlitTexture, _DepthTexture);
                volumetricCol = saturate(volumetricCol);
                return volumetricCol;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
