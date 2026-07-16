Shader "XKnight/UI/UI_Dissolution"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        
        // 溶解功能 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Toggle(_DISSOLUTION_ON)] _EnableDissolution ("启用溶解", Float) = 0
        
        _DissolutionEdgeWidth ("溶解 - 边缘宽度", Range(0, 1)) = 0
        [HDR] _DissolutionEdgeColor ("溶解 - 边缘颜色", Color) = (1, 1, 1, 1)
        
        _DissolutionFadeWidth ("溶解 - 淡出宽度", Range(0, 1)) = 0
        
        _DissolutionNoiseScale ("溶解 - 噪声比例", float) = 50
        
        [Toggle(_DISSOLUTION_CUSTOM_NOISE)] _EnableDissolutionCustomNoise ("溶解 - 自定义噪声", float) = 0
        _DissolutionNoiseTex ("溶解 - 噪声纹理", 2D) = "white" {}
        
        // 整体溶解
        _DissolutionThreshold_AsAWhole ("溶解 - 阈值 - 整体溶解", Range(0, 1)) = 0
        
        // 根据 UV 溶解
        [Toggle(_DISSOLUTION_BY_U)] _EnableDissolutionByU ("溶解 - 根据U", float) = 0
        [Toggle(_DISSOLUTION_BY_V)] _EnableDissolutionByV ("溶解 - 根据V", float) = 0
        
        _DissolutionThreshold_RemapRange ("溶解 - 阈值的重映射范围，为负时就是反转方向", Range(-4, 4)) = 2
        _DissolutionThreshold_ByUV ("溶解 - 阈值 - 根据UV", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        
        // HDR 下需要 alpha 按 One OneMinusSrcAlpha 累积成「真实覆盖率」，以正确叠加到独立 overlay buffer 上
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/DissolutionFunc.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/UI/GammaUtils.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // 溶解功能 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            #pragma multi_compile_local _ _DISSOLUTION_ON
            #pragma multi_compile_local _ _DISSOLUTION_CUSTOM_NOISE
            #pragma multi_compile_local _ _DISSOLUTION_BY_U _DISSOLUTION_BY_V

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4  mask : TEXCOORD2;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            half _IsGammaUI;

            // 溶解功能 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            half _DissolutionEdgeWidth;
            half4 _DissolutionEdgeColor;
            half _DissolutionFadeWidth;
            half _DissolutionNoiseScale;
            
            sampler2D _DissolutionNoiseTex;
            float4 _DissolutionNoiseTex_ST;

            half _DissolutionThreshold_AsAWhole;
            half _DissolutionThreshold_RemapRange, _DissolutionThreshold_ByUV;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if(!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }
                
                OUT.color = v.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = IN.color * (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // 溶解功能 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                DISSOLUTION_FRAG_AS_A_WHOLE(
                    IN.texcoord, _DissolutionNoiseScale, _DissolutionNoiseTex,
                    _DissolutionEdgeWidth, _DissolutionEdgeColor, _DissolutionFadeWidth,
                    _DissolutionThreshold_AsAWhole,
                    color);
                DISSOLUTION_FRAG_BY_UV(
                    IN.texcoord, _DissolutionNoiseScale, _DissolutionNoiseTex,
                    _DissolutionEdgeWidth, _DissolutionEdgeColor,
                    _DissolutionThreshold_RemapRange, _DissolutionThreshold_ByUV,
                    color);

                // 这里的逻辑，主要是为了适应纹理勾选了 "sRGB (Color Texture)" 选项的情况
                color.rgb = lerp(color.rgb, LinearToSRGB(color.rgb), _IsGammaUI);
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"
    CustomEditor "XKnight.ShaderGUI.UI_Dissolution_ShaderGUI"
    
}
