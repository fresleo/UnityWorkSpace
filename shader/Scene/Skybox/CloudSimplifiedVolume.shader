Shader "XKnight/Scene/Cloud/SimplifiedVolume"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorIntensity("ColorIntensity",Range(0,10)) = 1
        _Alpha("_Alpha",Range(0,1)) = 1
        _CloudStepSize("_CloudStepSize",Range(0,0.1)) = 0.005
        _CloudAttenuation("_CloudAttenuation",Range(0,5)) = 5
        _CloudDarkColor("DarkColor",Color) = (0.5,0.5,0.5,0.5)
        [HDR]_CloudBrightColor("_CloudBrightColor",Color) = (1,1,1,1)
        _CloudHaloRadius("_CloudHaloRadius",Range(0,10)) = 2
        _CloudHaloIntensity("_CloudHaloIntensity",Range(0,10)) = 1
        _CloudHaloPower("_CloudHaloPower",Range(0,10)) = 1
        [Toggle(_USE_REVERSE)]_Use_Reverse("_Reverse",Float) = 1
        _MaxNum("_MaxNum",Range(0,8)) = 8
//        [HideInInspector]_MaxNum("_MaxNum",Range(0,8)) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
//        Tags { "Queue"="Transparent"}
        LOD 300
        Pass
        {
            Name "cloud simple xyz"
            Tags {"LightMode" = "MeshSkybox"}
//            Tags {"LightMode" = "UniversalForward"}
            
		    ZWrite Off
		    Blend SrcAlpha OneMinusSrcAlpha
		    Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma shader_feature _ _USE_REVERSE 
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float3  _MainLightDirCustom;
            
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _CloudDarkColor;
            float4 _CloudBrightColor;
            float _CloudHaloRadius;
            float _CloudHaloIntensity;
            float _CloudHaloPower;
            float _ColorIntensity;
            float _ColorIntensityEnd;
            float _Alpha;
            float _CloudStepSize;
            float _CloudAttenuation; 
            float _MaxNum;
            CBUFFER_END
            #define CloudMaxStep 0.5f
            // float _TestAlpha;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float2 toLight : TEXCOORD2;
                float4 pos : SV_POSITION;
            };
            
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                #if UNITY_REVERSED_Z
	                o.pos.z = 0.000001f;
                #else
                    o.pos.z = o.pos.w - 0.00001;
                #endif
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float3 worldBitangent = cross(worldNormal, worldTangent) * v.tangent.w;

                float3x3 worldToTangent = float3x3(worldTangent, worldBitangent, worldNormal);
                float3 worldSunDir = _MainLightDirCustom.xyz;
                float3 tangentSunDir = mul(worldSunDir, worldToTangent); 
                o.toLight = (tangentSunDir.xz);
                o.uv = v.uv;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.viewDir = GetWorldSpaceViewDir(worldPos);
                return o;
            }
            half4 GetCloudColor(float2 uv, half2 lightDir, float stepSize)
            {
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float2 samplerDir = lightDir * stepSize;
                const int c_numSamples = 8;
                for (int i = 1; i < c_numSamples; ++i)
                {
                    result.r += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + i * samplerDir).r;
                }
                return result;
            }
                        
            half4 frag (v2f i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) ;
                
                float2 toLight = clamp(i.toLight.xy,-CloudMaxStep,CloudMaxStep);
                half4 clouds = GetCloudColor(i.uv,toLight,_CloudStepSize);
                #if defined(_USE_REVERSE)
                 clouds.r = _MaxNum - clouds.r;
                #endif
                
                clouds.r = exp2(-_CloudAttenuation * (clouds.r));
                half toLightLength = saturate(1 - length(toLight) / _CloudHaloRadius);
                half haloBright = pow(toLightLength,_CloudHaloPower) * _CloudHaloIntensity;
                half3 brightColor = _CloudBrightColor.rgb * haloBright + _CloudBrightColor.rgb;
                half t = clouds.r * (1.0 + dot(i.viewDir,- _MainLightDirCustom));
                
                // float value = step(_TestAlpha,t);
                // return half4(value.xxx,1.0);
                
                #if defined(_USE_REVERSE)
                // float threshold = smoothstep(0,1,t);
                // threshold = t;
                // color.rgb = lerp(brightColor.rgb, _CloudDarkColor.rgb,threshold);
                // half threshold = saturate(t);
                color.rgb = lerp(_CloudDarkColor.rgb,brightColor.rgb, t);
                #else
                color.rgb = lerp(_CloudDarkColor.rgb,brightColor.rgb, t);
                #endif
                color.rgb *= _ColorIntensity;
                color.a *= _Alpha;
                return color; 
            }
            ENDHLSL
        }


    }
}
