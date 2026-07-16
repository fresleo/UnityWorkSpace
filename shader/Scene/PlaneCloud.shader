Shader "XKnight/Scene/PlaneCloud"
{
    Properties
    {
        [Main(Main, _, off, off)]
        _Main("云-透明-溶解", Float) = 1
        //暂不支持序列帧图，因filpbook算法中，除法计算太浪费
        [Tex(Main)] [NoScaleOffset] [MainTexture] _MainTexture("云(单图)", 2D) = "white" {}
        [Sub(Main)] _CloudsDarkColor("云彩暗色", Color) = (0.5, 0.5, 0.5, 1)
        [Sub(Main)] _CloudsBrightColor("云彩亮色", Color) = (1, 1, 1, 1)        
        [Sub(Main)] _Opacity("透明度", Range(0, 2)) = 1
        [Sub(Main)] _Dissolve("溶解度", Range(0, 2.5)) = 0
        
        [Main(Scattering, _, off, off)]
        _Volume("散射", Float) = 1
        [Sub(Scattering)]_SunCloudScattering("太阳->云散射度", Range(0, 1)) = 0.5
        
        [Main(Distortion, _, off, off)]
        _Distortion("扭曲", Float) = 1
        [SubToggle(Distortion, __)] _DistortionAffectMainTexture("扭曲主(云)纹理图", Float) = 1
        [SubToggle(Distortion, __)] _DistortionAffectU("影响 U", Float) = 1
        [SubToggle(Distortion, __)] _DistortionAffectV("影响 V", Float) = 1
        [Tex(Distortion)] _DistortionNoiseTex("扭曲 贴图", 2D) = "white" {} 
        [Sub(Distortion)] _DistortionTiling("扭曲 平铺数", Vector) = (0, 0, 0, 0)
        [Sub(Distortion)] _DistortionDirection("扭曲 方向", Vector) = (0, 1, 0, -1) 
        [Sub(Distortion)] _DistortionIntensity("扭曲 强度", Range(-1, 1)) = 0.5
        
        [Main(Fresnel, _, off, off)]
        _Fresnel("菲尼尔", Float) = 1
        [SubToggle(Fresnel, __)] _FresnelOn("开启轮廓光", Int) = 0
        [Sub(Fresnel)] [HDR] _FresnelColor("边缘颜色", Color) = (1,1,1,1)
        [Sub(Fresnel)] _FresnelScale("菲尼尔缩放", Range(0, 2)) = 1
        [Sub(Fresnel)] _FresnelPower("菲尼尔强度", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            ColorMask RGBA
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Pipeline keywords
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DISTORTIONON_ON
            #pragma shader_feature_local_fragment _ _FRESNELON_ON

            // -------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined( LOD_FADE_CROSSFADE )
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float3 normal   : NORMAL;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 clipPos      : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float fresnel       : TEXCOORD2;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DistortionDirection;
                float2 _DistortionTiling;
                float _DistortionAffectMainTexture;
                float _DistortionAffectU;
                float _DistortionAutoScale;
                float _DistortionIntensity;
                float _DistortionAffectV;
                float _SunCloudScattering;
                float _SkyBlend;
                float _HorizonBlend;
                float _Opacity;
                float _Dissolve;
                float4 _CloudsDarkColor;
                float4 _CloudsBrightColor;
                sampler2D _MainTexture; 
                sampler2D _DistortionNoiseTex;
                float _FresnelOn;
                float _FresnelScale;
                float _FresnelPower;
                float4 _FresnelColor;
            CBUFFER_END

            void ApplyFresnel(inout float4 mainColor, float fresnel, float fresnelAlpha)
            {
                fresnel = _FresnelScale * pow(max(fresnel, 0.001), _FresnelPower);
                half4 fresnelNode = fresnel * _FresnelColor;
                mainColor.rgb += fresnelNode.rgb;
                mainColor.a *= fresnelAlpha;
            }

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv = v.uv;
                v.normal = v.normal;
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);
                o.worldPos = positionWS;
                o.clipPos = positionCS;
                #ifdef _FRESNELON_ON//if(_FresnelOn)
                {
                    float3 viewDir = normalize(_WorldSpaceCameraPos - positionWS);
                    float3 worldNormal = normalize(TransformObjectToWorldNormal(v.normal));                   
                    o.fresnel= 1.0f - dot(worldNormal, viewDir);
                }
                #endif

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                #if defined( LOD_FADE_CROSSFADE )
                LODFadeCrossFade(i.clipPos);
                #endif

                // 计算世界空间下的观察方向
                float3 worldViewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                // 计算扭曲后的纹理坐标
                float2 distortedUV = i.uv * _DistortionTiling + frac(_DistortionDirection.xy * (_Time.x ));
                float4 distortion = tex2D(_DistortionNoiseTex, distortedUV);
                float2 uvOffset = distortion.rg * 0.2 * _DistortionIntensity;
                uvOffset.x *= _DistortionAffectU;
                uvOffset.y *= _DistortionAffectV;
                float2 finalUV = _DistortionAffectMainTexture ? i.uv + uvOffset : i.uv;

                // 采样主纹理并计算云的颜色
                float4 cloudTex = tex2D(_MainTexture, finalUV);
                float4 cloudColor = lerp(_CloudsDarkColor, _CloudsBrightColor, cloudTex.r * cloudTex.r);

                // 获取主光源方向
                Light mainLight = GetMainLight();
                
                // 灯光方向、颜色（世界空间）
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;
                float3 sunColorLinear = FastSRGBToLinear(lightColor.rgb);
                float sunDot = dot(normalize(-lightDir), worldViewDir);
                
                // 云散射
                float4 sunScattering = float4(sunColorLinear * (pow(abs(max(sunDot, 0.0)), 15.0) * _SunCloudScattering), 0.0);
                float4 cloudScattering = sunScattering * clamp(dot(worldViewDir, float3(0, -1, 0)) * 5.0, 0.0, 1.0);
                cloudScattering = cloudScattering * cloudTex.g;

                // 计算最终颜色
                float4 finalColor = cloudColor + cloudScattering;

                // 菲尼尔颜色
                #ifdef _FRESNELON_ON//if(_FresnelOn)
                {
                    ApplyFresnel(finalColor, i.fresnel, 1.0);   
                }
                #endif
                
                // 计算最终透明度
                float alpha = clamp(cloudTex.a * _Opacity - (1.0 - cloudTex.b) * _Dissolve, 0.0, 1.0);

                return half4(finalColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}