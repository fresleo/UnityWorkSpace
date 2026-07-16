Shader "XKnight/Scene/TrafficLightEffect"
{
    Properties
    {
        [HDR]_BaseColor("主要颜色",color) = (1,1,1,1)
        _BaseMap("主贴图", 2D) = "white" {}
    	_AlphaScale("透明度缩放",float) = 1
	    [Toggle]_IfFlash("是否闪烁开关", float) = 0
        _FlashFrequency ("每秒闪烁频率", Range(0.1, 10)) = 1.0 
        _FlashIntensity ("闪烁强度", Range(0, 1)) = 0.5
    }
 
    SubShader
    {
        Tags
        {	"Queue"="Transparent"
        	"RenderType" = "Transparent" 
        	"IgnoreProjector" = "True" 
        	"RenderPipeline" = "UniversalPipeline" 
        }
 		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
        Pass
        {
            Name "TrafficLightEffect"
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
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
            
            struct Attributes
			{
	            float4 vertex : POSITION;
	            float3 normal : NORMAL;
	            float2 uv : TEXCOORD0;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
	            float4 positionCS : SV_POSITION;
	            float3 positionWS : TEXCOORD0;
	            float2 uv : TEXCOORD1;
				UBPA_FOG_COORDS(2)
	            UNITY_VERTEX_INPUT_INSTANCE_ID
			};
 
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _BaseMap_ST;
            half _FlashFrequency;
            half _FlashIntensity;
            half _IfFlash;
            half _AlphaScale;
            CBUFFER_END
            TEXTURE2D (_BaseMap);SAMPLER(sampler_BaseMap);
            
 
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.vertex.xyz);
                output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(input.vertex.xyz);
                UBPA_TRANSFER_FOG(output, output.positionWS);
                return output;
            }
 
            half4 frag(Varyings output) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(output);
                half4 c;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, output.uv.xy);
                half phase = 2 * PI * _FlashFrequency * _Time.y;
                half flash = (sin(phase) + 1) * 0.5; // 范围[0,1]
                half4 color = _BaseColor;
                color.a *= 1.0 - (flash * lerp(0,_FlashIntensity,_IfFlash));
                c = baseMap * color;
                c.a = baseMap.a*color.a*_AlphaScale;
                UBPA_APPLY_FOG(output, c);
                return c;
            }
            ENDHLSL
        }
    }
	CustomEditor "LWGUI.LWGUI"
}