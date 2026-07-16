Shader "XKnight/Particle/VFXChromaticAberration"
{
    Properties
    {
        _MaskTex ("_MaskTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" "Queue" = "Transparent+1501"
        }
        LOD 400
        
        Pass
        {
            Name "ChromaticAberration"
            Tags
            {
                "LightMode" = "DistortionOffset"
            }
            ZWrite Off
            Blend Zero One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _USE_DITHER
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            TEXTURE2D (_MaskTex);
            SamplerState sampler_MaskTex;
                        
            CBUFFER_START(UnityPerMaterial)
            float4 _MaskTex_ST;
            float _RadiusAmount;
            CBUFFER_END         
            
             // 4x4 标准拜耳矩阵，值域 0 ~ 15/16
            static const float4x4 bayerMatrix = float4x4(
                0.0/16.0,  8.0/16.0,  2.0/16.0,  10.0/16.0,
                12.0/16.0, 4.0/16.0,  14.0/16.0, 6.0/16.0,
                3.0/16.0,  11.0/16.0, 1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0,  13.0/16.0, 5.0/16.0
            );
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv.xy = TRANSFORM_TEX(v.uv, _MaskTex);
                return o;
            }
            //  后处理中 传的参数 最后要乘以 0.05，  结果非常小：存入图中误差太大
            half4 frag (v2f i) : SV_Target
            {
                
                //----------------------------------
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex,i.uv.xy).r;                
                // 1. 计算当前 UV 到 UV 中心点 (0.5, 0.5) 的方向向量
                float2 uvOffset = i.uv.xy - float2(0.5, 0.5);
                // 2. 引入屏幕宽高比进行修正，确保过渡区域在屏幕上呈现为“正圆形”
                // _ScreenParams.x 是屏幕宽，_ScreenParams.y 是屏幕高
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uvOffset.x *= aspect; 
                // 3. 计算修正后的绝对距离
                float distToUVCenter = length(uvOffset);
                float radiusWeight = saturate(distToUVCenter / 0.5);
                // float radiusWeight = smoothstep(0,0.2,distToUVCenter);
                radiusWeight = smoothstep(0,_RadiusAmount,distToUVCenter);
                float dataRound = round(mask * 15.0);
                uint2 screenPos = uint2(i.vertex.xy) % 4;
                float noise = bayerMatrix[screenPos.x][screenPos.y];
                float maskScaled = mask * 15.0;
                float dataBayer = floor(maskScaled + noise);
                float dataA = lerp(dataRound, dataBayer, radiusWeight);
                dataA = clamp(dataA, 0.0, 15.0); 
                float raw8Bit = dataA * 16.0;    
                return half4(0,0,0,raw8Bit / 255.0);
            }
            ENDHLSL
        }
    }
}
