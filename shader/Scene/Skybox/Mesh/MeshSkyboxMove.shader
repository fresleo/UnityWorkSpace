Shader "XKnight/Scene/MeshSkybox/Move"
{
    Properties
    {
    	[Toggle(_TEST)]_Test("_Test",Float) = 1
        [NoScaleOffset]_MainTex ("外层贴图", 2D) = "white" {}
    	_ColorMain("外层颜色",Color) = (1,1,1,1)
    	_AlphaMain("外层透明度",Range(0,3)) = 1
        _WindDir("风的吹向", Vector) = (1,0,0,0)
        _WindSpeed("风的速度", range(0,0.5)) = 0.02
        _WindFlowDistance("风吹的单位距离", range(0,10)) = 0.5
        _WindFlowFallOff("风速的衰减（最高点到最低点）", range(0,10)) = 2
        _HorizonFactor("地平线上的流速", range(0,0.5)) = 0
        [NoScaleOffset]_MainTex2 ("内层贴图", 2D) = "white" {}
    	_ColorMain2("内层颜色",Color) = (1,1,1,1)
    	_AlphaMain2("内层透明度",Range(0,3)) = 1
        _WindSpeed2("内层速度", range(0,0.02)) = 0.005
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        LOD 300
		Cull Off
		ZWrite Off
        Pass
        {
            Name "Sky_Cloud"
            Tags{ "LightMode" = "MeshSkybox" }
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
			#pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature _ _TEST
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float lerpVect : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };
			
			TEXTURE2D(_MainTex);
			SamplerState sampler_MainTex;
			TEXTURE2D(_MainTex2);
			SamplerState sampler_MainTex2;
			
        CBUFFER_START(UnityPerMaterial)
			float4 _ColorMain;
			float4 _ColorMain2;
            float3 _WindDir;
			float _HorizonFactor;
			float _WindSpeed;
            float _WindFlowDistance;
            float _WindFlowFallOff;
            float _WindSpeed2;
			float _AlphaMain;
			float _AlphaMain2;
        CBUFFER_END			
			v2f vert (appdata v)
			{
		        v2f o;
        		o.pos = TransformObjectToHClip(v.vertex.xyz);
        		float3 binormal = cross(normalize(v.normal),normalize(v.tangent.xyz)) * v.tangent.w;
        		float3x3 rotation = float3x3(v.tangent.xyz, binormal, v.normal);
        		float2 tempUV = v.uv - 0.5;
        		float2 skyTangentUV = float2(tempUV.y, -tempUV.x);
        		float skyHMask = dot(skyTangentUV,skyTangentUV) * 4;
        		float3 windDir = mul(rotation,_WindDir).xyz;
        		tempUV = skyTangentUV / (length(skyTangentUV)+ 1e-4);
        		float2 windDis = windDir.xy * _WindFlowDistance;
        		float2 windRes = dot(windDis,tempUV) * tempUV; 
        		float2 tangentMovement = lerp(windDis,windRes,skyHMask);            
        		float2 slowMoveMask = lerp(_HorizonFactor,1.0,pow(max(1-skyHMask,1e-4),_WindFlowFallOff)) * tangentMovement;            
        		float winSpeed = (_WindSpeed * _Time.y);
        		float winFracSpeed = frac(winSpeed);
        		float offsetTime = frac(winSpeed+0.5f);
        		float2 uv0 = (offsetTime - 0.5 ) * slowMoveMask + v.uv; 
        		float2 uv1 = (winFracSpeed -0.5) * slowMoveMask + v.uv;
        		float timeV = winFracSpeed * 2;
        		float lerpVect = min(timeV,2-timeV);
        		o.uv.xy = uv0;
        		o.uv.zw = uv1;
        		o.lerpVect = lerpVect;
        		// 内层贴图
        		float _Cos = cos(_Time.y * _WindSpeed2);
        		float _Sin = sin(_Time.y * _WindSpeed2);
        		float2 centeredUV = v.uv - 0.5;
        		float2 rotatedUV;
        		rotatedUV.x = centeredUV.x * _Cos - centeredUV.y * _Sin;
        		rotatedUV.y = centeredUV.x * _Sin + centeredUV.y * _Cos;
        		rotatedUV += 0.5;
        		o.uv2.xy = rotatedUV;
        		return o;
			}

            half4 frag (v2f i) : SV_Target
            {
                float2 uv0 = i.uv.xy;
                float2 uv1 = i.uv.zw;
                half3 color0 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv0).rgb;
                half3 color1 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv1).rgb;               
                half3 color = lerp(color0,color1,i.lerpVect);
            	color.rgb *= _ColorMain.rgb;
            	half alphaMain = color.r * _AlphaMain;
            	half4 mainResult = half4(color.rgb,alphaMain);
            	//
            	half4 color2 = SAMPLE_TEXTURE2D(_MainTex2,sampler_MainTex2,i.uv2.xy).rgba;
            	color2.rgb *= _ColorMain2.rgb;
            	color2.a *= _AlphaMain2;
            	half4 mainResult2 = color2;
                half4 blendColor = mainResult2 + mainResult * (1- mainResult2.a);
                blendColor.rgb = clamp(blendColor.rgb+1e-4,0.0,1.0);
                return blendColor;
            }
            ENDHLSL
        }
    }
}
