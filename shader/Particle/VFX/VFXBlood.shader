// 模拟血迹表现的特殊特效材质
// Custom Inputs are X = Pan Offset, Y = UV Warp Strength, Z = Gravity(CustomData 输入 X = 偏移，Y = UV 扭曲强度，Z = 重力)
// Specular Alpha 的使用类似于金属度控制。 高值更像电介质，低值更像金属
// 底部的子着色器适用于着色器模型 2.0 和 OpenGL ES 2.0 设备

Shader "XKnight/Particle/Blood"
{
	Properties
	{
		[Main(Base, __, on, off)]
        _Main("基础", Float) = 1
		[Sub(Base)][HDR] _BaseColor ("基色", Color) = (1,1,1,1)
		[Sub(Base)] _LightStr ("灯光强度", float) = 1.0
		[Sub(Base)] _AlphaMin ("Alpha Clip 最小值", Range (-0.01, 1.01)) = 0.1
		[Sub(Base)] _AlphaSoft ("Alpha Clip 柔软度", Range (0,1)) = 0.1
		[Sub(Base)] _EdgeDarken ("边缘 变暗", float) = 1.0
		[Sub(Base)] _ProcMask ("程序遮罩强度", float) = 1.0

		[Main(MaskControls, _, off, off)]
        _MaskControls("遮罩控制", Float) = 0
		[Sub(MaskControls)] _MainTex ("Mask 纹理", 2D) = "white" {}
		[Sub(MaskControls)] _MaskStr ("Mask 强度", float) = 0.7
		[Sub(MaskControls)] _Columns ("Flipbook 列", Int) = 1
		[Sub(MaskControls)] _Rows ("Flipbook 行", Int) = 1
		[Sub(MaskControls)] _ChannelMask ("通道遮罩", Vector) = (1,1,1,0)
		[SubToggle(MaskControls, __)] _FlipU("Flip U 随机", float) = 0
		[SubToggle(MaskControls, __)] _FlipV("Flip V 随机", float) = 0

		[Main(NoiseControls, __, off, off)]
        _NoiseControls("噪声控制", Float) = 0
		[Sub(NoiseControls)] _NoiseTex ("噪声纹理", 2D) = "white" {}
		[Sub(NoiseControls)] _NoiseAlphaStr ("噪音强度", float) = 1.0
		[Sub(NoiseControls)] _ChannelMask2 ("通道遮罩",Vector) = (1,1,1,0)
		[Sub(NoiseControls)] _Randomize ("随机化噪声", float) = 1.0

		[Main(UV Warp Controls, __, off, off)]
		_UVWarpControls("UV扭曲控制", Float) = 0
		[Sub(UV Warp Controls)] _WarpTex ("扭曲纹理", 2D) = "gray" {}
		[Sub(UV Warp Controls)] _WarpStr ("扭曲强度", float) = 0.2

		[Main(VertexPhysics, __, off, off)]
		_VertexPhysics("重力控制(需要CustomData.texcoord0.zw 配合)", Float) = 0
		[Sub(VertexPhysics)] _FallOffset ("重力偏移-texcoord0.w", range(-1,0)) = -0.5
		[Sub(VertexPhysics)] _FallRandomness ("重力随机性-texcoord0.z", float) = 0.25

		[Main(SpecularReflection, _, off, off)]
		_SpecularReflection("高光反射控制", Float) = 0
		[SubToggle(SpecularReflection, __)] _Specular_Reflection("Specular Reflection(高光反射开关)", Float) = 1
		[Sub(SpecularReflection)] [HDR] _SpecularColor ("Reflection 多种颜色", Color) = (1,1,1,0.5)
		[Sub(SpecularReflection)] _ReflectionTex ("Reflection 纹理", 2D) = "black" {}
		[Sub(SpecularReflection)] _ReflectionSat ("Reflection 饱和度", float) = 0.5
		[Sub(SpecularReflection)] [NoScaleOffset] [Normal] _Normal ("Reflection 法线", 2D) = "bump" {}
		[Sub(SpecularReflection)] _FlattenNormal ("Flatten Reflection Normal", float) = 2.0 
	}
	
	// LOD 400
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent" "Queue" = "Transparent"
		}
		LOD 400
		
		Pass 
		{
			Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
            
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ _SPECULAR_REFLECTION_ON
			#pragma multi_compile _ _BLEND_VOLUME_COLOR
			#pragma multi_compile _ _HEIGHT_FOG
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MaterialVolume.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST, _ReflectionTex_ST, _NoiseTex_ST, _WarpTex_ST;
				
				half4 _BaseColor;
				half4 _SpecularColor;									
				half _LightStr;
				half _AlphaMin;
				half _AlphaSoft;
				half _EdgeDarken;
				half _ProcMask;
				half _MaskStr;
				half _Columns;
				half _Rows;
				half4 _ChannelMask;
				half _FlipU;
				half _FlipV;
				half _ReflectionSat;
				half _NoiseAlphaStr;
				half4 _ChannelMask2;
				
				// 这个判断会导致 SRP Batcher 失效
				// #ifdef _SPECULAR_REFLECTION_ON
				half _FlattenNormal;
				// #endif
				
				half _Randomize;
				half _WarpStr;
				half _FallOffset;
				half _FallRandomness;
			CBUFFER_END
			
			half _NoiseColorStr;
			
			sampler2D _MainTex; 
			sampler2D _ReflectionTex;
			sampler2D _NoiseTex;
			sampler2D _WarpTex;
			#ifdef _SPECULAR_REFLECTION_ON
			sampler2D _Normal;
			#endif
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				#ifdef _SPECULAR_REFLECTION_ON
				half4 tangent : TANGENT;							
				#endif
				half3 normal : NORMAL;
				half4 color : COLOR;
				
				float4 texcoord0 : TEXCOORD0; // Z is Random, W is Lifetime
				float3 texcoord1 : TEXCOORD1; // X is Pan Offset, Y is UV Warp Strength, Z is Gravity
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half3 normal : NORMAL;
				half4 color : COLOR;
				
				float4 uv : TEXCOORD0;
				
				float3 positionWS : TEXCOORD1;
				
				float4 vertLight : TEXCOORD2;
				float3 customData : TEXCOORD3; // XY is custom ((panDistanceOffset & warpStrength)), Z is stable random
				
				UBPA_FOG_COORDS(4)
				
				#ifdef _SPECULAR_REFLECTION_ON
				float3 viewDir : TEXCOORD5;						
				#endif
				
				float3x3 tangentToWorld : TEXCOORD6; // 注意：这个必须放在最后
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata v)
			{
				v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float lifetime = v.texcoord0.w;
				lifetime = lifetime * lifetime + (_FallOffset + ((v.texcoord0.z - 0.5) * _FallRandomness)) * lifetime;
				float4 fallPos = lifetime * float4(0, v.texcoord1.z, 0, 0);

				float2 UVflip = round(frac(float2(v.texcoord0.z * 13, v.texcoord0.z * 8))); 	//random 0 or 1 in x and y
				UVflip = UVflip * 2 - 1; 														//random -1 or 1 in x and y
				UVflip = lerp(1, UVflip, float2(_FlipU, _FlipV));
				
				#ifdef SHADER_API_GLES3
				fallPos *= -1.0;
				#endif
				
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz + fallPos;
				o.positionWS = worldPos;

				o.vertex = TransformObjectToHClip(v.vertex) + fallPos;
				o.color = v.color;
				o.color.a *= o.color.a;
				o.color.a += _AlphaMin;
				o.normal = TransformObjectToWorldNormal(v.normal);
				o.customData = float3(v.texcoord1.xy, v.texcoord0.z);

				// o.uv.xy is original UVs, o.uv.zw is randomized and panned //
				o.uv.xy = TRANSFORM_TEX(v.texcoord0.xy * UVflip, _MainTex);
				o.uv.zw = o.uv.xy * half2(_Columns, _Rows) + v.texcoord0.z * half2(3,8) * _Randomize;
				
				#ifdef _SPECULAR_REFLECTION_ON
				// get all the vectors and matricies I need to handle normalmapped reflections //
				float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;								
				float3x3 rotation = float3x3(v.tangent.xyz, binormal, v.normal);							
				o.tangentToWorld = mul((float3x3)unity_ObjectToWorld, transpose(rotation));							
				float3 worldViewDir = normalize (TransformWorldToView(o.positionWS));							
				o.viewDir = worldViewDir;																	
				#endif

				//Do vertex lighting
				float3 shade = SampleSH(float4(o.normal,1));
				shade = max(shade, (unity_AmbientSky + unity_AmbientGround + unity_AmbientEquator) * 0.15);		//Don't go to 0 even if there's no significant lighting data
				o.vertLight.xyz = lerp(1, shade, _LightStr);
				//vertlight.w is currently unused
				
				UBPA_TRANSFER_FOG(o, o.positionWS);
				
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				// Sample The UV Offset
				float4 uvWarp = tex2D(_WarpTex, i.uv.zw * _WarpTex_ST.xy + _WarpTex_ST.zw * (i.customData.x + 1) + (float2(5,8) * i.customData.z) );
				float2 warp = (uvWarp.xy * 2) - 1;
				warp *= _WarpStr * i.customData.y;

				//Sample The Mask
				half4 mask = tex2D(_MainTex, i.uv.xy * _MainTex_ST.xy + warp);
				mask = saturate(lerp(1, mask, _MaskStr));

				//Make And Edge Mask So Nothing Spills Off The Quad
				half2 tempUV = frac(i.uv.xy * half2(_Columns, _Rows)) - 0.5;
				tempUV *= tempUV * 4;
				half edgeMask = saturate(tempUV.x + tempUV.y);
				edgeMask *= edgeMask;
				edgeMask = 1- edgeMask;
				edgeMask = lerp(1.0, edgeMask, _ProcMask);

				mask *= edgeMask;
				half4 col = max(0.001, i.color);
				col.a = saturate(dot(mask, _ChannelMask));

				//Sample The Noise
				half4 noise4 = tex2D(_NoiseTex, i.uv.zw * _NoiseTex_ST.xy + _NoiseTex_ST.zw * i.customData.x + warp);
				half noise = dot(noise4, _ChannelMask2);
				noise = saturate(lerp(1,noise,_NoiseAlphaStr));

				//Alpha Clip
				col.a *= noise;
				half preClipAlpha = col.a;
				half clippedAlpha =  saturate((preClipAlpha * i.color.a - _AlphaMin)/(_AlphaSoft));
				col.a = clippedAlpha;

				//Bring In Base Lighting
				float3 baseLighting = max(0.01,(i.vertLight + 0.2 * dot(i.vertLight, half3(1,1,1))));
				baseLighting = i.vertLight.xyz;
				
				#ifdef _SPECULAR_REFLECTION_ON
				////// Sample The Normals //////
				half3 normalTex = UnpackNormal(tex2D(_Normal, i.uv.zw * _NoiseTex_ST.xy + _NoiseTex_ST.zw * i.customData.x + warp));

				////// Make Normals Steep Near Alpha Edge //////
				normalTex.z = _FlattenNormal * (preClipAlpha + preClipAlpha + col.a - 1) * 0.5;
				normalTex.z = _FlattenNormal * (saturate((preClipAlpha * i.color.a - _AlphaMin)/(_AlphaSoft + 0.2)) - 0.1) * 1.2;
				normalTex = normalize(normalTex);

				////// Transform Normals To World Space //////
				normalTex.xyz = mul(i.tangentToWorld, normalTex.xyz);
				float3 combinedNormals = normalize(i.normal + normalTex);
				float3 viewDir = (combinedNormals + half3(0,1,0)) * 0.5;

				////// Calculate Reflection UVs ///////
				float3 reflectionVector = reflect(-i.viewDir, combinedNormals);
				reflectionVector.x = atan2(reflectionVector.x, reflectionVector.z) * 0.31831;
				reflectionVector = reflectionVector * 0.5;
				float2 reflectionUVs = reflectionVector.xy * _ReflectionTex_ST.xy;
				reflectionUVs += _ReflectionTex_ST.zw * (GET_GLOBAL_TIME + i.customData.z);
				float3 reflectionTex = tex2D(_ReflectionTex, reflectionUVs);

				////// Generate Specular Reflection//////
				float desatReflection = dot(reflectionTex, float3(1,1,1)) * 0.333;
				float3 spec = lerp(desatReflection, reflectionTex, _ReflectionSat);
				float3 spec0 = spec;
				float3 spec1 = spec0 * spec0 * spec0 * spec0;
				spec = clamp(lerp(spec0, spec1, _SpecularColor.w * preClipAlpha),0,10);
				
				float fresnel = 1 - dot(i.viewDir, combinedNormals) * _SpecularColor.w;
				spec *= clamp(fresnel, 0.2,1);
				#endif

				//Find Edge
				half edge = 1 - saturate(preClipAlpha * clippedAlpha);
				edge *= edge;
				edge = 1 - edge;
				edge = edge + lerp(0, noise - 0.5, _NoiseColorStr);

				//Edge Darken
				edge = saturate(lerp(0.71, edge * edge, _EdgeDarken));

				//Edge Alpha
				col.a *= saturate(lerp(1.25, _BaseColor.a , edge));
				
				// #ifndef  _SPECULAR_REFLECTION_ON
				// 	edge *= 2;
				// #endif 

				col.xyz *= lerp(min(col.xyz * col.xyz * col.xyz * 0.3, 1.0), 0.71, edge);  //Make sure this doesn't end up wAAAAAAY over one

				//Tint And Combine Lighting
				col.xyz *= max(0,baseLighting * _BaseColor.xyz);
				
				#ifdef _SPECULAR_REFLECTION_ON
				col.xyz += baseLighting * spec * _SpecularColor.xyz;
				#endif
				
				col = BlendVolumeColorAndAlpha(i.positionWS, col);
				
				UBPA_APPLY_FOG(i, col);

				return col;
			}
			ENDHLSL
		}
	}

	//这是 SHADER MODEL 2.0 和 OPENGL ES 2.0 设备的后备功能
	//由于性能和硬件限制，UV 扭曲、噪点平移、镜面反射、顶点动画和顶点照明均已禁用
	
	// LOD 300
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent" "Queue" = "Transparent"
		}
		LOD 300
		
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma multi_compile _ _BLEND_VOLUME_COLOR
			#pragma multi_compile _ _HEIGHT_FOG
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MaterialVolume.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			
			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST, _NoiseTex_ST;
				
				half4 _BaseColor;
				half _AlphaMin;
				half _AlphaSoft;
				half _EdgeDarken;
				half _ProcMask;
				half _MaskStr;
				half4 _ChannelMask;
				half _Columns;
				half _Rows;
				half _FlipU;
				half _FlipV;
				half _NoiseAlphaStr;
				half4 _ChannelMask2;
				half _Randomize;
			CBUFFER_END
			
			half _NoiseColorStr;
			
			sampler2D _MainTex;
			sampler2D _NoiseTex;
			
			struct appdata_t 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float4 texcoord0 : TEXCOORD0; // Z 是随机的，W 是生命周期的
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
				float4 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				
				UBPA_FOG_COORDS(2)
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = TransformObjectToHClip(v.vertex);

				float2 UVflip = round(frac(float2(v.texcoord0.z * 13, v.texcoord0.z * 8))); 	//random 0 or 1 in x and y
				UVflip = UVflip * 2 - 1; 														//random -1 or 1 in x and y
				UVflip = lerp(1, UVflip, float2(_FlipU, _FlipV));
				
				// o.uv.xy is original UVs, o.uv.zw is randomized and panned
				o.uv.xy = TRANSFORM_TEX(v.texcoord0.xy * UVflip, _MainTex);
				o.uv.zw = o.uv.xy * half2(_Columns, _Rows) + v.texcoord0.z * half2(3,8) * _Randomize;
				o.uv.zw *= _NoiseTex_ST.xy;
				o.uv.zw += _NoiseTex_ST.zw * v.texcoord0.w;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.positionWS = worldPos;
				
				o.color = v.color;
				o.color.a += _AlphaMin;
				
				UBPA_TRANSFER_FOG(o, o.positionWS);

				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				half4 col = i.color;
				//Sample The Mask
				half4 mask = tex2D(_MainTex, i.uv.xy);
				mask = saturate(lerp(1, mask, _MaskStr));

				//Make And Edge Mask So Nothing Spills Off The Quad
				half2 tempUV = frac(i.uv.xy * half2(_Columns, _Rows)) - 0.5;
				tempUV *= tempUV * 4;
				half edgeMask = saturate(tempUV.x + tempUV.y);
				edgeMask *= edgeMask;
				edgeMask = 1- edgeMask;
				edgeMask = lerp(1.0,  edgeMask, _ProcMask);
				mask *= edgeMask;
				col.a *= saturate(dot(mask, _ChannelMask));
				
				//Sample The Noise
				half4 noise4 = tex2D(_NoiseTex, i.uv.zw);
				half noise = dot(noise4, _ChannelMask2);
				noise = saturate(lerp(1,noise,_NoiseAlphaStr));

				//Alpha Clip
				col.a *= noise * i.color.a;
				half preClipAlpha = col.a;
				half clippedAlpha =  saturate((preClipAlpha * i.color.a - _AlphaMin)/(_AlphaSoft));
				col.a = clippedAlpha;
				preClipAlpha = lerp(0.5, (min(preClipAlpha * 0.9 + 0.1,1.0)) * clippedAlpha, _EdgeDarken);
				
				col.xyz *= preClipAlpha * _BaseColor;
				col = BlendVolumeColorAndAlpha(i.positionWS, col);

				UBPA_APPLY_FOG(i, col);
				
				return col;
			}
			ENDHLSL 
		}
	}
	
	CustomEditor "LWGUI.LWGUI"
}
