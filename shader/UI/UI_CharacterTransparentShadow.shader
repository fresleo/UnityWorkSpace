/*
为 UI 产生角色投影的着色器
以透明平面来接收阴影
*/
Shader "XKnight/UI/UI_CharacterTransparentShadow"
{
    Properties
    {
        _ShadowColor ("阴影颜色", Color) = (0, 0, 0, 0.5)
    }

    SubShader
    {
        Tags 
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
        }
        
        ZWrite Off
        ZTest LEqual
        Cull Back // Quad 的背面没有用处
        
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "ReceiveShadow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_MEDIUM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // 计算阴影坐标
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // 采样主光源阴影，0=阴影中，1=光照中
                half shadowAttenuation = MainLightRealtimeShadow(input.shadowCoord);
                half shadowFactor = 1.0 - shadowAttenuation;
                
                half3 finalColor = _ShadowColor.rgb;
                half finalAlpha = shadowFactor * _ShadowColor.a;
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}