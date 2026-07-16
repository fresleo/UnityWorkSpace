Shader "XKnight/Scene/FakeVolumetricLight"
{
    Properties
    {
        _LightColor ("灯光颜色", Color) = (1, 0.8, 0.6, 1)
        _Density ("灯光强度", Range(0.1, 1)) = 1
        _EdgeFalloff ("灯光边缘衰减", Range(0.01, 2)) = 0.2
        _EdgeSoftness ("灯光边缘软化", Range(0.01, 2)) = 0.2
        
    }
    
    SubShader
    {
        Tags 
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        // ForwardLit
        Pass
        {
            Name "VolumetricLightPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha // 混合的参数
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _HEIGHT_FOG
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float3 normal :TEXCOORD4;
                float3 normalWS : TEXCOORD5;
                UBPA_FOG_COORDS(6)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            CBUFFER_START(UnityPerMaterial)
            half3 _LightColor;
            half _Density;
            half _EdgeSoftness;
            half _EdgeFalloff;
            CBUFFER_END
            
            
            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // 计算世界空间位置
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                
                // 计算裁剪空间位置
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                
                // 视图方向
                o.viewDir = GetWorldSpaceNormalizeViewDir(positionWS);
                
                // 屏幕空间位置
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.uv = v.uv;
                o.normal = v.normal;
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                UBPA_TRANSFER_FOG(o, o.positionWS);
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // 深度衰减
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                half screenDepthFP = sceneDepth;
                screenDepthFP -=  i.screenPos.w;
                screenDepthFP = clamp(0 , 1, screenDepthFP);
                
                // 相机距离衰减
                float cameraDist = distance(GetCameraPositionWS(), i.positionWS);
                float cameraAtten = saturate(cameraDist - 0.5h);
                
                // 边缘软化 (使用菲涅尔近似)
                float3 viewDir = i.viewDir;
                float edge =pow(saturate(dot(i.normalWS, viewDir)), _EdgeSoftness);
                edge = smoothstep(0, _EdgeFalloff, edge);
                
                // 最终颜色
                half4 finalColor;
                finalColor.rgb = _LightColor.rgb;
                finalColor.a = screenDepthFP * cameraAtten * i.uv.y * (1-i.uv.y) *edge*_Density;
                UBPA_APPLY_FOG(i, finalColor);
                return finalColor;
            }
            ENDHLSL
        }

        // MotionVectors
        /*
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectorsTransparent"
            }
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex MotionVectorsVertex
            #pragma fragment MotionVectorsFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "../../Common/LitMotionVectors.hlsl"
            ENDHLSL
        }
        */
    }
    
    CustomEditor "LWGUI.LWGUI"
}