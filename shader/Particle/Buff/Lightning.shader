Shader "XKnight/Particle/Buff/Lightning"
{
    Properties
    {
        // 主要设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Main, __, on, off)]
        _Main ("主要设置", float) = 1
        
        [Sub(Main)] [HDR] _TintColor ("色调颜色", Color) = (0.5, 0.5, 0.5, 1)
        
        [Sub(Main)] _MainTex ("遮罩纹理", 2D) = "white" {}
        [Sub(Main)] _Speed ("遮罩纹理 UV滚动速度", Range(-2, 2)) = 1.0
        
        [Sub(Main)] _GradientTex ("渐变纹理", 2D) = "white" {}
        [Sub(Main)] _Stretch ("渐变纹理 U拉伸", Range(-2, 2)) = 1.0
        [Sub(Main)] _Offset ("渐变纹理 U偏移", Range(-2, 2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"="True" "PreviewType"="Plane"
            "RenderType"="Transparent" "Queue"="Transparent"
        }

        ColorMask RGB
        Blend One OneMinusSrcAlpha
        Lighting Off Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex PassVertex
            #pragma fragment PassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _GradientTex_ST;

                half4 _TintColor;
                float _Speed;
                float _Stretch, _Offset;
            CBUFFER_END
            
            TEXTURE2D_X(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D_X(_GradientTex); SAMPLER(sampler_GradientTex);
            
            #include "../../ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                
                float4 vertexColor : TEXCOORD2;
                
                float3 positionWS : TEXCOORD3;
                UBPA_FOG_COORDS(4)
                
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings PassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 vertex = input.positionOS.xyz;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(vertex);

                output.positionCS = vertexInput.positionCS;

                output.vertexColor = input.color;
                
                output.texcoord.xy = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.texcoord2 = TRANSFORM_TEX(input.texcoord, _GradientTex);
                // 粒子系统的自定义数据
                output.texcoord.z = input.texcoord.z;
                output.texcoord.w = input.texcoord.w;
                
                output.positionWS = vertexInput.positionWS;
                UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

                return output;
            }

            void PassFragment(Varyings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 通过粒子系统的自定义数据来获取值
                float lifetime = input.texcoord.z;
                float randomOffset = input.texcoord.w;

                // 梯度下降
                float gradientfalloff = smoothstep(0.99, 0.95, input.texcoord2.x) * smoothstep(0.99, 0.95, 1 - input.texcoord2.x);
                // 滚动 UV
                float2 movingUV = float2(input.texcoord.x + randomOffset + (_Time.x * _Speed), input.texcoord.y);
                float mask = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, movingUV).r * gradientfalloff;

                // 用来模拟消融
                float cutoff = step(lifetime, mask);

                // 拉伸渐变图的 UV
                float2 gradientTexUV = float2(mask * _Stretch - lifetime + _Offset, 1);
                float4 colorMap = SAMPLE_TEXTURE2D_X(_GradientTex, sampler_GradientTex, gradientTexUV);

                half4 col;
                col.rgb = colorMap.rgb * _TintColor * input.vertexColor.rgb;
                col.a = cutoff;
                col *= col.a;

                UBPA_APPLY_FOG(input, col);

                outColor = col;
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}