// 模拟Buff火焰覆盖全身表现的特殊特效材质
Shader "XKnight/Buff/MeshFire"
{
	Properties
	{
        [Main(Main, __, on, off)]
        _Main ("主要设置", float) = 1
        [Sub(Main)] [HDR] _EmissColor("发光颜色", Color) = (0,0,0,0)
		[Sub(Main)] _EmissIntensity("发光强度", Range( 0 , 30)) = 1
		[Sub(Main)] _Emiss("发光区域", 2D) = "white" {}
		[Sub(Main)] _EmissSpeed("发光 速度", Vector) = (0,0,0,0)
		
		[Main(Ramp, __, on, off)]
        _Ramp ("Ramp设置", float) = 1
		[Sub(Ramp)] [NoScaleOffset]_ColorRamp("Ramp 颜色图", 2D) = "white" {}
		[SubToggle(Ramp, __)] _RampInvert("Ramp 反向", Float) = 1
		
		[Main(Fire, __, on, off)]
        _Fire ("火苗设置", float) = 1
		[Sub(Fire)] _RootFade("火苗根本过渡", Range( 0 , 0.3)) = 0.2
		[Sub(Fire)] _FireScale("火苗比例", Range( -1 , 1)) = 0
		[Sub(Fire)] _FireSize("火苗大小", Range( 0 , 10)) = 0
		
		[Main(Velocity, __, on, off)]
        _Velocity ("速度-力量设置(法向-起始速度-力量设置)", float) = 1
		[Sub(Velocity)] _NormalVelocity("法向量速度", Float) = 1
		[Sub(Velocity)] _StartVelocity("起始速度", Vector) = (0,0,0,0)
		[Sub(Velocity)] _ConstantForce("恒力", Vector) = (0,0,0,0)
		
		[Main(Pos, __, on, off)]
        _Pos ("运行时位置设置-前端传值,不用调参", float) = 0
		[Sub(Pos)] _PrePos("PrePos", Vector) = (0,0,0,0)
		[Sub(Pos)] _CurPos("CurPos", Vector) = (0,0,0,0)
		
		[Main(Motion, __, on, off)]
        _Motion ("运动-噪声-凝聚力(Motion-Noise-Cohesion)设置", float) = 1
		[Sub(Motion)] _MotionNoise("运动噪声", Float) = 0
		[Sub(Motion)] _MotionForce("运动强度", Float) = 1
		[Sub(Motion)] _Cohesion("凝聚", Range( 0 , 1)) = 0
		[Sub(Motion)] _CohesionForce("凝聚力", Float) = 0
		
		[Main(Tubulence, __, on, off)]
        _Tubulence ("湍流(Tubulence)设置", float) = 1
		[Sub(Tubulence)] _TubulenceNoise("湍流噪声", 2D) = "white" {}
		[Sub(Tubulence)] _TubulenceFreq("湍流频率", Float) = 1
		[Sub(Tubulence)] _TubulencePower("湍流强度", Float) = 1
		[Sub(Tubulence)] _TubulenceSpeed("湍流速度", Float) = 1
	}

	SubShader
	{
		Tags
		{	"RenderPipeline"="UniversalPipeline"
			"RenderType"="Transparent"
			"Queue"="Transparent"
			"UniversalMaterialType"="Unlit"
		}
		
		Cull Off
		Pass
		{
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend One One, One OneMinusSrcAlpha
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			#pragma multi_compile_instancing
			
			CBUFFER_START(UnityPerMaterial)
				float4 _PrePos;
				float4 _CurPos;
				float4 _EmissColor;
				float4 _Emiss_ST;
				float3 _StartVelocity;
				float3 _ConstantForce;
				float2 _EmissSpeed;
				float _FireScale;
				float _RampInvert;
				float _EmissIntensity;
				float _TubulencePower;
				float _MotionForce;
				float _TubulenceSpeed;
				float _FireSize;
				float _MotionNoise;
				float _CohesionForce;
				float _Cohesion;
				float _NormalVelocity;
				float _TubulenceFreq;
				float _RootFade;
			CBUFFER_END

			TEXTURE2D_X(_TubulenceNoise); SAMPLER(sampler_TubulenceNoise);
			TEXTURE2D_X(_Emiss); SAMPLER(sampler_Emiss);
			TEXTURE2D_X(_ColorRamp); SAMPLER(sampler_ColorRamp);
			
			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 vertexColor : COLOR;				
				float4 texcoord : TEXCOORD0;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 texcoord4 : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			float3 CalculatePivotPosition(float4 texcoord2, float4 texcoord3)
			{
			    float2 tc2 = texcoord2.xy;
			    float2 tc3 = texcoord3.xy;
			    return float3(-tc2.x, tc3.x, -tc2.y) * 0.01;
			}

			float3 CalculateXVector(float4 color)
			{
			    float3 colorVector = color.rgb * 2.0 - 1.0;
			    return float3(-colorVector.x, colorVector.z, colorVector.y);
			}

			float3 CalculateStartForce(float3 xVector, float texcoordY)
			{
			    return ((xVector * _NormalVelocity) + _StartVelocity) * texcoordY;
			}

			float3 CalculateConstantForce(float3 pivotPosition, float texcoordYY, float texcoord3Y, float motionFactor)
			{
			    float random = texcoord3Y;
			    float sinFactor = sin(_TimeParameters.x + (random * 2.0 * PI));
			    float motionScale = (1.0 - motionFactor) * 0.5 + 0.5;
			    return (sinFactor * pivotPosition + _ConstantForce) * texcoordYY * motionScale * 0.5;
			}

			float3 CalculateCohesionForce(float3 pivotPosition, float texcoordYY)
			{
			    return (-pivotPosition * _Cohesion * _CohesionForce) * texcoordYY * 0.5;
			}

			float3 CalculateMotionForce(float3 pivotPosition, float texcoordYY, float motionFactor, float distance)
			{
				float smoothstepResult = smoothstep(0.0, 0.1, motionFactor);
			    float3 motionVector = SafeNormalize((_PrePos - (_CurPos + float4(pivotPosition * _MotionNoise * smoothstepResult, 0.0))).xyz);
			    return (motionVector * distance).xyz * _MotionForce * texcoordYY * 0.5;
			}

			float3 CalculateTurbulence(float2 texcoord, float texcoord3)
			{
				float turbulenceTimeOffset = fmod(_TimeParameters.x, 2048.0) + texcoord3 * 100.0 * _TubulenceSpeed;
			    float2 turbulenceUV = float2(texcoord.x, texcoord.y + turbulenceTimeOffset);
			    turbulenceUV *= _TubulenceFreq;
			    
			    float3 turbulence = (SAMPLE_TEXTURE2D_LOD(_TubulenceNoise, sampler_TubulenceNoise, turbulenceUV, 0).rgb - 0.5) * _TubulencePower * texcoord.y;
			    return turbulence;
			}

			Varyings vert(Attributes input)
			{
			    Varyings output = (Varyings)0;
			    UNITY_SETUP_INSTANCE_ID(input);
			    UNITY_TRANSFER_INSTANCE_ID(input, output);
			    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			    // Calculate Pivot Position
			    float3 pivotPosition = CalculatePivotPosition(input.texcoord2, input.texcoord3);

				// Calculate Scaling Position
			    float3 scalingPos = lerp(input.positionOS.xyz, pivotPosition, _FireScale);

				// Calculate X Vector
			    float3 xVector = CalculateXVector(input.vertexColor);
				
			    // Calculate Motion Factor
				float distance = length(_PrePos - _CurPos);
			    float motionFactor = clamp(distance, 0.0, 1.0);

				// Calculate Texcoord
				float texcoordYY = input.texcoord.y * input.texcoord.y;
				
			    // Calculate Forces
			    float3 startForce = CalculateStartForce(xVector, input.texcoord.y);
			    float3 constantForce = CalculateConstantForce(pivotPosition, texcoordYY, input.texcoord3.y, motionFactor);
			    float3 cohesionForce = CalculateCohesionForce(pivotPosition, texcoordYY);
			    float3 motionForce = CalculateMotionForce(pivotPosition, texcoordYY, motionFactor, distance);
			    float3 turbulence = CalculateTurbulence(input.texcoord.xy, input.texcoord3.y);
				
			    // Combine all forces for absolute displacement
			    float3 vertexPosition = scalingPos + startForce + constantForce + cohesionForce + motionForce + turbulence;
				
				// Assign the calculated position to the output structure				
			    input.positionOS.xyz = vertexPosition;
				
			    // Calculate vertex positions using the absolute position
			    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
			    
			    // Output position data
			    output.positionWS = vertexInput.positionWS;
			    output.positionCS = vertexInput.positionCS;
			    output.clipPosV = vertexInput.positionCS;
				
				// Pass through texture coordinates
			    output.texcoord4.xy = input.texcoord.xy;
			    output.texcoord4.zw = input.texcoord3.xy;

			    return output;
			}

			half4 frag(Varyings IN) : SV_Target
			{
			    UNITY_SETUP_INSTANCE_ID(IN);
			    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

			    // Calculate UV coordinates
			    float2 uv_Emiss = IN.texcoord4.xy * _Emiss_ST.xy + _Emiss_ST.zw;
			    float2 uv_Main = IN.texcoord4.xy;
			    float2 uv_Random = IN.texcoord4.zw;

			    // Calculate random value
			    float random = uv_Random.y;

			    // Calculate panner for emission
			    float2 panner = uv_Emiss + (random * float2(2, 1)) + _Time.y * _EmissSpeed;

			    // Sample emission texture
			    float emissionSample = SAMPLE_TEXTURE2D(_Emiss, sampler_Emiss, panner).r;

			    // Calculate y-based fade
			    float yFade = uv_Main.y * uv_Main.y;
			    yFade *= yFade; // Equivalent to y^4

			    // Clamp emission value
			    float clampedEmission = saturate(emissionSample - yFade);

			    // Calculate motion factor
			    float motionDistance = distance(_PrePos, _CurPos);
			    float motionFactor = saturate(motionDistance);

			    // Calculate ramp UV
			    float2 rampUV;
			    rampUV.x = uv_Main.y * ((motionFactor * 0.3) + 1.0) * _FireSize;
			    rampUV.x = _RampInvert ? (1.0 - rampUV.x) : rampUV.x;
			    rampUV.y = 0.5;

			    // Sample color ramp
			    float3 rampColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, rampUV).rgb;

			    // Calculate root fade
			    float rootFade = smoothstep(0.0, _RootFade, uv_Main.y);

			    // Combine final color
			    float3 finalColor = clampedEmission * _EmissColor.rgb * _EmissIntensity * rampColor * rootFade;

			    return half4(finalColor, 1);
			}
			ENDHLSL
		}
	}
	CustomEditor "LWGUI.LWGUI"
}
