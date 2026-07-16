Shader "XKnight/UI/UI_CircleOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _Width("Width", Range(0.1, 0.49)) = 0.45
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
		
		_ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        LOD 100
        
        Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"

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
                float4 worldPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _OutlineColor;
            float  _Width;
CBUFFER_END

            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = v.vertex;
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half t = smoothstep(_Width, 0.5, distance(i.uv, float2(0.5, 0.5)));
                half3 color = _OutlineColor.rgb * t + (1.0 - t) * _MainColor;

                half4 textureColor = tex2D(_MainTex, i.uv);

                half alpha = 1.0 - t;

                half4 mainColor = half4(color * textureColor, alpha * _MainColor.a * textureColor.a);

// required for RectMask2D
// #ifdef UNITY_UI_CLIP_RECT
//                 mainColor.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
// #endif

#ifdef UNITY_UI_ALPHACLIP
                clip (mainColor.a - 0.001);
#endif  
                
                return mainColor;
            }
            
            ENDCG
        }
    }
}
