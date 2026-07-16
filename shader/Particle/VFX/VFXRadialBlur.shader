Shader "XKnight/Particle/VFXRadialBlur"
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
            Name "Radial Blur"
            Tags
            {
                "LightMode" = "DistortionOffset"
            }
            ZWrite Off
            Blend Zero One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };
            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            TEXTURE2D (_MaskTex);
            SamplerState sampler_MaskTex;
            CBUFFER_START(UnityPerMaterial)
            float4 _MaskTex_ST;
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
                // o.uv.z = v.uv.z;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half maskB = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex,i.uv.xy).r;
                // half amount = saturate(i.uv.z);
                // half amount = (i.uv.z);
                // maskB *= amount;
                // maskB = saturate(maskB);
                //2. 获取屏幕像素坐标 (4x4 循环)
                uint2 screenPos = uint2(i.vertex.xy) % 4;
                float noise = bayerMatrix[screenPos.x][screenPos.y];
                float maskScaled = maskB * 15.0;
                float dataB = floor(maskScaled + noise);
                dataB = clamp(dataB, 0.0, 15.0);
                return float4(0, 0, 0, dataB / 255.0);
            }
            ENDHLSL
        }
    }
}
