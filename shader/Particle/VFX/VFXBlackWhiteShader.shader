Shader "XKnight/Particle/VFXBlackWhiteShader"
{
    Properties
    {
        [HDR] _MainColor("Main Color", Color) = (0,0,0,1)
        [HDR] _SecondColor("Second Color", Color) = (1,1,1,1)
        _ColorRange("Color Range", Range(0, 1)) = 0.1
        _ColorOffset("Color Offset", Range(0,0.1)) = 0.0
        _PolarCenter("Polar Center", Vector) = (0.5, 0.5, 0, 0)
        
        _PolarTexture("Noise Texture", 2D) = "white" {}
        _PolarTexture2("Noise2 Texture", 2D) = "white" {}
        
        _WireIntensity("Wire Intensity", Range(0, 1)) = 0.1
        _WireScale("Radial Scale&Length Scale", Vector) = (1,1,1,1)
        _WireSpeed("Wire Speed", Range(0, 1)) = 1
        _WireSpeed2("Wire Speed 2", Range(0, 1)) = 1
        
        [Toggle] _RevertColor("Revert Color", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent" "Queue"="Transparent"
        }

        Pass
        {
            ZTest Off
            Cull Off
            Blend One Zero
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 vertex    : SV_POSITION;
            };

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                half4 _SecondColor;
                half  _ColorRange;
                half  _ColorOffset;
                half4 _PolarCenter;
                half  _WireIntensity;
                half4 _WireScale;
                half  _WireSpeed;
                half  _WireSpeed2;
                half  _RevertColor;
            CBUFFER_END

            TEXTURE2D(_PolarTexture);  SAMPLER(sampler_PolarTexture);
            TEXTURE2D(_PolarTexture2); SAMPLER(sampler_PolarTexture2);

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"
            
            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = float4(v.uv * 2.0f - 1.0f, .0f, 1.0f);
                o.uv = v.uv;

                float4 screenPos = ComputeScreenPos(o.vertex);

                o.screenPos = screenPos;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half2 screenUV = i.screenPos.xy / i.screenPos.w;
                half3 screenColor = SampleSceneColor(screenUV);

                float desaturateDot = dot( screenColor, float3( 0.299, 0.587, 0.114 ));

                float2 centeredUV = screenUV - _PolarCenter;

                float2 uv = float2(length(centeredUV) * _WireScale.x * 2.0f, atan2(centeredUV.x, centeredUV.y) * (1.0 / TWO_PI) * _WireScale.y);
                float2 pannerUV = GET_GLOBAL_TIME.y * _WireSpeed + uv;

                float2 uv2 = float2(length(centeredUV) * _WireScale.z * 2.0f, atan2(centeredUV.x, centeredUV.y) * (1.0 / TWO_PI) * _WireScale.w);
                float2 pannerUV2 = GET_GLOBAL_TIME.y * _WireSpeed2 + uv2;  
                
                half wireR = SAMPLE_TEXTURE2D(_PolarTexture, sampler_PolarTexture, pannerUV).r;
                wireR += SAMPLE_TEXTURE2D(_PolarTexture2, sampler_PolarTexture2, pannerUV2).r;

                wireR = lerp(desaturateDot, wireR, _WireIntensity);

                wireR = smoothstep(_ColorRange, _ColorRange + _ColorOffset, wireR);

                UNITY_BRANCH
                if (_RevertColor)
                {
                    wireR = 1.0f - wireR;
                }
                
                half3 result = lerp(_MainColor, _SecondColor, wireR);

                return half4(result, 1);
            }
            
            ENDHLSL
        }
    }
}
