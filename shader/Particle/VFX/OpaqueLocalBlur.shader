Shader "XKnight/Particle/OpaqueLocalBlur"
{
    Properties
    {
        [OpaqueLocalBlurHint]
        [Space(22)] [Header(Main)] [Space(6)]
        [HDR] _Color ("主颜色", Color) = (1,1,1,1)
        _MaskMap ("遮罩贴图", 2D) = "white" {}
        
        [Space(22)] [Header(Blur)] [Space(6)]
        [Toggle] _Blur ("开启模糊", int) = 0
        _BlurIntensity ("模糊强度", Range(0, 1)) = 0.5
        [Enum(Low, 0, Medium, 1, Height, 2)] _BlurLevel ("模糊精度", Float) = 0
        
        [Space(22)] [Header(Dispersion)] [Space(6)]
        [Toggle(_MAINTEX_DISPERSION_ON)] _MainTexDispersion ("主纹理色散", Float) = 0
        [Toggle] _ParticleControl ("粒子 uv0.zw 控制强度", Float) = 0
        _MainTexHorizontalDispersion ("色散强度-水平", Range(-1, 1)) = 0
        _MainTexVerticalDispersion ("色散强度-垂直", Range(-1, 1)) = 0
        

        [Space(42)] [Header(Manager)] [Space(6)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("剔除模式", Float) = 2
        [LiteToggle] _ZWriteMode ("深度写入", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("深度测试", Float) = 4
        
        // [Space(22)] [Header(Stencil)] [Space(6)]
        // [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 0
        // [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 0
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
        // [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
        // [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
        // [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0
        // _ColorMask ("Color Mask", Float) = 15
    }
    
    HLSLINCLUDE
    
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    
    CBUFFER_START(UnityPerMaterial)
    
    float4 _Color;
    float _BlurIntensity;
    float _BlurLevel;
    
    float _ParticleControl;
    float _MainTexHorizontalDispersion;
    float _MainTexVerticalDispersion;
    
    float4 _MaskMap_ST;
    
    CBUFFER_END
    
    TEXTURE2D_X(_CameraOpaqueTexture);
    SAMPLER(sampler_CameraOpaqueTexture);
    TEXTURE2D(_MaskMap);
    SAMPLER(sampler_MaskMap);
    
    float4 _CameraOpaqueTexture_TexelSize;
    
    
    struct appdata
    {
        float4 vertex  : POSITION;
        float3 normal  : NORMAL;
        float4 color   : COLOR;
        float4 uv      : TEXCOORD0;

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 vertex              : SV_POSITION;
        float4 uv                  : TEXCOORD0;
        float4 color               : TEXCOORD1;
        float3 positionWS          : TEXCOORD2;
        float4 positionSS          : TEXCOORD3;

        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    ENDHLSL

    SubShader
    {
        LOD 400
        
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        // ForwardLit - UniversalForward
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            // Stencil
            // {
            //     Ref [_Stencil]
            //     Comp [_StencilComp]
            //     Pass [_StencilOp]
            //     ReadMask [_StencilReadMask]
            //     WriteMask [_StencilWriteMask]
            //     Fail [_StencilFail]
            //     ZFail [_StencilZFail]
            // }

            // ColorMask [_ColorMask]
            
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            
            Cull [_CullMode]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Pipeline keywords
            // #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _BLUR_ON
            #pragma shader_feature_local_fragment _ _MAINTEX_DISPERSION_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            
            float3 GetOpaqueColor(float2 uv, float2 dispersion)
            {
                float3 color = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv, 0).rgb;
                #ifdef _MAINTEX_DISPERSION_ON
                {
                    float2 uvDispersion = float2(_MainTexHorizontalDispersion, _MainTexVerticalDispersion);
                    uvDispersion = lerp(uvDispersion, dispersion, _ParticleControl) * 0.005;
                    color.r = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + uvDispersion, 0).r;
                    color.b = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv - uvDispersion, 0).b;
                }
                #endif
                return color;
            }
            
            float3 KawaseBlur(float2 uv, float scale, float2 dispersion)
            {
                float2 texelSize = _CameraOpaqueTexture_TexelSize.xy;
                float b = _BlurIntensity * 5 * scale;
        
                float3 color = GetOpaqueColor(uv, dispersion);
                float endDot = 1;
        
                // 是否开启模糊
                #ifdef _BLUR_ON
                {
                    if (_BlurLevel >= 0)
                    {
                        color += GetOpaqueColor(uv + float2(b, b)   * texelSize, dispersion);
                        color += GetOpaqueColor(uv + float2(b, -b)  * texelSize, dispersion);
                        color += GetOpaqueColor(uv + float2(-b, b)  * texelSize, dispersion);
                        color += GetOpaqueColor(uv + float2(-b, -b) * texelSize, dispersion);
                        endDot = 0.2;
                    }
            
                    // 当开启色散，关闭高级的模糊效果
                    #ifndef _MAINTEX_DISPERSION_ON
                    {
                        if (_BlurLevel >= 1)
                        {
                            color += GetOpaqueColor(uv + float2(b, b)   * texelSize * 2, dispersion);
                            color += GetOpaqueColor(uv + float2(b, -b)  * texelSize * 2, dispersion);
                            color += GetOpaqueColor(uv + float2(-b, b)  * texelSize * 2, dispersion);
                            color += GetOpaqueColor(uv + float2(-b, -b) * texelSize * 2, dispersion);
                            endDot = 0.11111111;
                        }
                        if (_BlurLevel >= 2)
                        {
                            color += GetOpaqueColor(uv + float2(b, b)   * texelSize * 3, dispersion);
                            color += GetOpaqueColor(uv + float2(b, -b)  * texelSize * 3, dispersion);
                            color += GetOpaqueColor(uv + float2(-b, b)  * texelSize * 3, dispersion);
                            color += GetOpaqueColor(uv + float2(-b, -b) * texelSize * 3, dispersion);
                            endDot = 0.07692308;
                        }
                    }
                    #endif
                }
                #endif
        
                color *= endDot;
                return color;
            }
            
            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
        
                o.vertex = TransformWorldToHClip(worldPos.xyz);
                o.uv = v.uv;
                o.color = v.color;
                o.positionWS = worldPos.xyz;
                o.positionSS = ComputeScreenPos(o.vertex);

                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.positionSS.xy / i.positionSS.w;
                
                float4 color = _Color;
                float mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy * _MaskMap_ST.xy + _MaskMap_ST.zw).r;
                color.rgb *= KawaseBlur(uv, i.color.a * mask, i.uv.zw) * i.color.rgb;
                color.a *= mask * i.color.a;
                
                return color;
            }
            
            ENDHLSL
        }

    }

    SubShader
    {
        LOD 300
        
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        // ForwardLit - UniversalForward
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            // Stencil
            // {
            //     Ref [_Stencil]
            //     Comp [_StencilComp]
            //     Pass [_StencilOp]
            //     ReadMask [_StencilReadMask]
            //     WriteMask [_StencilWriteMask]
            //     Fail [_StencilFail]
            //     ZFail [_StencilZFail]
            // }

            // ColorMask [_ColorMask]
            
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            
            Cull [_CullMode]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
        
                o.vertex = TransformWorldToHClip(worldPos.xyz);
                o.uv = v.uv;
                o.color = v.color;
                o.positionWS = worldPos.xyz;
                o.positionSS = ComputeScreenPos(o.vertex);

                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                return float4(1,1,1,0);
            }
            
            ENDHLSL
        }

    }



    CustomEditor "LWGUI.LWGUI"
}