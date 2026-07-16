/// <summary>
/// 假点光源
/// 作者：Ling mei an
/// 日期：2025-09-11
/// 功能：假点光源效果
/// </summary>
Shader "XKnight/Scene/FakeParticlePointLight"
{
    Properties
    {
        [Toggle] _IfParticleControl ("是否使用粒子控制(默认是)", int) = 1
        [HDR] _LightColor ("灯光颜色(粒子中使用颜色)", Color) = (1, 0.8, 0.6, 1)
        _Density ("灯光强度(粒子中使用 CustomData1.x)", Range(0.1, 1)) = 1
        _EdgeFalloff ("灯光边缘衰减(粒子中使用 CustomData1.y)", Range(0.5, 1)) = 0.2
        _EdgeSoftness ("灯光边缘软化(粒子中使用 CustomData1.z)", Range(0.01, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha // 混合的参数
        Cull Back
        ZWrite Off
        ZTest LEqual
        
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
            }
            Name "VolumetricLightPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
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
                float4 vectorColor : COLOR;
                
                float4 uv : TEXCOORD0;
                float uv2 : TEXCOORD2;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                
                float4 vectorColor : COLOR;
                
                float3 positionWS : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float4 uv : TEXCOORD3;
                float3 normal :TEXCOORD4;
                float3 normalWS : TEXCOORD5;
                float uv2 : TEXCOORD6;
                UBPA_FOG_COORDS(7)
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                int _IfParticleControl;
                half3 _LightColor;
                half _Density;
                half _EdgeSoftness;
                half _EdgeFalloff;
            CBUFFER_END
            
            half remap(half x, half t1, half t2, half s1, half s2)
            {
                return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
            }
            
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
                o.uv2 = v.uv2;
                o.normal = v.normal;
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.vectorColor = v.vectorColor;
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
                half edgeSoftness = lerp(_EdgeSoftness, i.uv2, _IfParticleControl);
                float edge = pow(saturate(dot(i.normalWS, viewDir)), edgeSoftness * 3);
                half remapUVW = remap(i.uv.w, 0, 1.0, 0.8, 1.0);
                half edgeFalloff = lerp(_EdgeFalloff, remapUVW, _IfParticleControl); // i.uv.w 的粒子覆盖值
                edge = smoothstep(0, edgeFalloff * 3, edge);
                
                // 最终颜色
                half4 finalColor;
                finalColor.rgb = lerp(_LightColor.rgb, i.vectorColor, _IfParticleControl);
                half intensity = lerp(_Density, i.uv.z, _IfParticleControl);
                finalColor.a = screenDepthFP * cameraAtten * i.uv.y * (1 - i.uv.y) * edge * intensity * 10;
                UBPA_APPLY_FOG(i, finalColor);
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}