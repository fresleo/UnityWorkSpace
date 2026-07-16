Shader "XKnight/UI/UI_Gray"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Luminance("Luminance", Range(0.1, 4)) = 1
        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode("Src Mode", Int) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstMode("Dst Mode", Int) = 10
        
		// 该Shader逻辑不会使用模板测试，仅UI Mask时使用
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        
        _ColorMask ("Color Mask", Float) = 15
        _ClipRect("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        LOD 100
        
        Pass
        {
            Name "Default"
            
            Cull Off
		    Blend [_SrcMode] [_DstMode]
		    
		    Stencil
	        {
	            Ref             [_Stencil]
	            Comp            [_StencilComp]
	            Pass            [_StencilOp]
	            ReadMask        [_StencilReadMask]
	            WriteMask       [_StencilWriteMask]
	        }
		    
		    ColorMask [_ColorMask]
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/UI/GammaUtils.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            
            float4 _MainTex_ST;
            float  _Luminance;
            half _IsGammaUI;
            float4 _ClipRect;

            float UnityGet2DClipping (float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float grayscale = Luminance(col.rgb) * _Luminance;
                
                // 为了配合 Gamma 校正的工作，根据纹理是否勾选了 srgb 选项的情况，执行不同的处理逻辑
                grayscale = lerp(grayscale, LinearToSRGB(grayscale), _IsGammaUI);

                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (col.a - 0.001);
                #endif
                
                return half4(grayscale, grayscale, grayscale, col.a);
            }
            ENDCG
        }
    }
}
