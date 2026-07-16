/*
为 UI 产生角色投影的着色器
平面阴影 - 把 mesh 的顶点投影成1个平面
*/
Shader "XKnight/UI/UI_CharacterPlaneShadow"
{
    Properties
    {
        _ShadowColor ("阴影颜色", Color) = (0, 0, 0, 0.5)
        
        _LightDirection ("光源方向", Vector) = (0, 0, -1, 0)
        _PlaneNormal ("投影平面法线", Vector) = (0, 0, 1, 0)
        
        _ShadowFalloff ("阴影衰减系数", Float) = 0
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
        Cull Off // 拍平后，不双面的话，某些面会被裁剪掉
        
        Blend SrcAlpha OneMinusSrcAlpha
        
        // 解决 double blending, 保证一个点只被渲染一次
        Stencil
        {
            Ref 0 // 设定参考值0，stencilbuffer 里面的值会跟它进行比较, stencilBuffer 值默认为 0  
            Comp Equal // 比较方式为"相等"
            Pass IncrWrap // 当模版测试和深度测试都通过的时候，当前模板缓冲中的是值 +1
            
            WriteMask 255
            ReadMask 255
            Fail Keep
            ZFail Keep
        }
        
        Pass
        {
            Name "BackShadow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 planeCenterWS : TEXCOORD2;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                
                float4 _LightDirection, _PlaneNormal;
                
                half _ShadowFalloff;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // 世界空间模型原点
                float3 modelOriginPosWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 planePosWS = modelOriginPosWS;
                
                float3 lightDir = normalize(_LightDirection.xyz);
                float3 planeNormal = normalize(_PlaneNormal.xyz);
                
                // 计算投影坐标
                float distance = dot(planePosWS.xyz - positionWS.xyz, planeNormal.xyz) / dot(lightDir.xyz, planeNormal.xyz);
                float3 projectedPosWS = positionWS + lightDir * distance;
                
                output.positionCS = TransformWorldToHClip(projectedPosWS);
                output.positionWS = projectedPosWS;
                output.positionOS = TransformWorldToObject(projectedPosWS);
                output.planeCenterWS = planePosWS;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // 使用距离衰减（基于投影后的位置到平面中心的距离）
                half attenuate = 1.0 - saturate(distance(input.planeCenterWS, input.positionWS) * _ShadowFalloff);
                half shadowAlpha = _ShadowColor.a * attenuate;
                
                half4 color = half4(_ShadowColor.rgb, shadowAlpha);
                
                return color;
            }
            ENDHLSL
        }
    }
}