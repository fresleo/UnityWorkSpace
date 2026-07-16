// =============================================================================
// UI/OutlineEx — UGUI Text 描边 Shader
// 配合 OutlineEx.cs 组件使用，通过顶点数据传递描边参数（颜色、宽度、UV 范围），
// 在 fragment 阶段对周围 8 个方向采样生成描边，避免创建多份网格副本。
//
// 顶点数据通道分配（由 OutlineEx.cs 写入）：
//   TEXCOORD0  — 扩展后的 UV（用于采样字形纹理）
//   TEXCOORD1  — 原始字形 UV 子矩形最小值（用于采样钳制）
//   TEXCOORD2  — 原始字形 UV 子矩形最大值（用于采样钳制）
//   TEXCOORD3  — OutlineColor.rg
//   TANGENT.x  — OutlineRings（采样环数，默认 1）
//   TANGENT.zw — OutlineColor.ba
//   NORMAL.z   — OutlineWidth（描边宽度，单位：texel）
// =============================================================================

Shader "UI/OutlineEx"
{
    Properties
    {
        // [PerRendererData] 标记使 UGUI 可以按 Renderer 自动设置纹理，支持合批
        [PerRendererData] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        // UGUI 标准 Stencil 属性，用于 Mask 组件裁切
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        // UGUI Mask 组件使用的 Stencil 配置
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "OUTLINE"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/UI/GammaUtils.cginc"

            // RectMask2D 裁切 / Alpha 裁切 变体（local 不占用全局关键字空间）
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;                // 字体图集纹理
            fixed4 _Color;                     // Graphic 组件的 Tint 颜色
            fixed4 _TextureSampleAdd;          // UGUI 内部：Text 组件使用时为 (1,1,1,0)，使 alpha-only 纹理可见
            float4 _MainTex_TexelSize;         // (1/width, 1/height, width, height)

            float4 _ClipRect;                  // RectMask2D 裁切矩形（世界空间）
            float _UIMaskSoftnessX;            // RectMask2D 水平软裁切
            float _UIMaskSoftnessY;            // RectMask2D 垂直软裁切
            half _IsGammaUI;
            int _UIVertexColorAlwaysGammaSpace;

            // ---- 顶点输入结构 ----
            // 数据由 OutlineEx.cs 的 ModifyMesh 写入各通道
            struct appdata
            {
                float4 vertex   : POSITION;    // 扩展后的顶点位置
                float4 tangent  : TANGENT;     // .x = OutlineRings, .zw = OutlineColor.ba
                float4 normal   : NORMAL;      // .z  = OutlineWidth
                float2 texcoord : TEXCOORD0;   // 扩展后的 UV
                float2 uv1      : TEXCOORD1;   // 原始字形 UV 最小值
                float2 uv2      : TEXCOORD2;   // 原始字形 UV 最大值
                float2 uv3      : TEXCOORD3;   // OutlineColor.rg
                fixed4 color    : COLOR;       // 顶点颜色
            };

            // ---- 片段输入结构 ----
            struct v2f
            {
                float4 vertex        : SV_POSITION;
                float2 texcoord      : TEXCOORD0;
                float4 uvRect        : TEXCOORD1;   // xy = 原始 UV 最小值, zw = 原始 UV 最大值
                half4  outlineColor  : TEXCOORD2;   // rgba = 描边颜色
                float2 texelOffset   : TEXCOORD3;   // 预计算的纹素偏移 = texelSize * outlineWidth
                float4 mask          : TEXCOORD4;   // RectMask2D 裁切插值数据
                float2 outlineInfo   : TEXCOORD5;   // x = outlineWidth, y = ringCount
                fixed4 color         : COLOR;
            };

            static const float2 _BaseDir[8] = {
                float2( 1.0,       0.0),
                float2( 0.70711,   0.70711),
                float2( 0.0,       1.0),
                float2(-0.70711,   0.70711),
                float2(-1.0,       0.0),
                float2(-0.70711,  -0.70711),
                float2( 0.0,      -1.0),
                float2( 0.70711,  -0.70711)
            };

            v2f vert(appdata IN)
            {
                v2f o;

                float4 vPosition = UnityObjectToClipPos(IN.vertex);
                o.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(IN.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                                0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                o.texelOffset = _MainTex_TexelSize.xy * IN.normal.z;
                o.texcoord = IN.texcoord;

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if (!IsGammaSpace())
                    {
                        IN.color.rgb = UIGammaToLinear(IN.color.rgb);
                    }
                }
                o.color = IN.color * _Color;
                o.uvRect = float4(IN.uv1, IN.uv2);
                o.outlineColor = half4(IN.uv3.x, IN.uv3.y, IN.tangent.z, IN.tangent.w);
                o.outlineInfo = float2(IN.normal.z, max(1, round(IN.tangent.x)));

                return o;
            }

            fixed IsInRect(float2 pPos, float2 pClipRectMin, float2 pClipRectMax)
            {
                pPos = step(pClipRectMin, pPos) * step(pPos, pClipRectMax);
                return pPos.x * pPos.y;
            }

            half3 ApplyGammaUI(half3 color)
            {
                return lerp(color, LinearToSRGB(color), _IsGammaUI);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                color.rgb = ApplyGammaUI(color.rgb);

                if (IN.outlineInfo.x > 0)
                {
                    color.a *= IsInRect(IN.texcoord, IN.uvRect.xy, IN.uvRect.zw);
                    half fillAlpha = color.a;
                    half3 fillColor = color.rgb;

                    half4 val = half4(IN.outlineColor.rgb, 0);
                    val.rgb = ApplyGammaUI(val.rgb);

                    int ringCount = (int)IN.outlineInfo.y;
                    const int MAX_RINGS = 5;

                    [loop]
                    for (int r = 1; r <= MAX_RINGS; r++)
                    {
                        if (r > ringCount) break;

                        float ringScale = (float)r / (float)ringCount;
                        float2 offset = IN.texelOffset * ringScale;
                        float sn, cs;
                        sincos(ringScale * 0.39, sn, cs);

                        [unroll]
                        for (int d = 0; d < 8; d++)
                        {
                            float2 dir = float2(
                                _BaseDir[d].x * cs - _BaseDir[d].y * sn,
                                _BaseDir[d].x * sn + _BaseDir[d].y * cs);
                            float2 pos = IN.texcoord + offset * dir;
                            float a = IsInRect(pos, IN.uvRect.xy, IN.uvRect.zw)
                                    * (tex2D(_MainTex, pos) + _TextureSampleAdd).w
                                    * IN.outlineColor.a;
                            val.w = val.w + a - val.w * a;
                        }
                    }

                    val.w = smoothstep(0.0, 0.65, val.w);
                    val.w *= IN.color.a;

                    half outlineAlpha = val.w * (1.0 - fillAlpha);
                    color.rgb = fillColor * fillAlpha + val.rgb * outlineAlpha;
                    color.a = fillAlpha + outlineAlpha;
                }
                else
                {
                    color.rgb *= color.a;
                }

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                half clipFactor = m.x * m.y;
                color.rgb *= clipFactor;
                color.a *= clipFactor;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }

            ENDCG
        }
    }
}
