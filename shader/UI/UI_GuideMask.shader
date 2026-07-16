Shader "XKnight/UI/UI_GuideMask"
{
    // 该shader用于UGUI的Image
    // 通过添加MaskTex图片，并设定Mask Rect，则可以在指定范围内使用图片的alpha值作为透明度参考
    // Mask Color用于设定所有像素点的颜色
    
    Properties
    {
        _MaskTex ("MaskTex", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        //MASK SUPPORT ADD
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        //MASK SUPPORT END
        _Color("Mask Color", Color) = (0,0,0,0.8)
        _Range("Mask Rect", Vector) = (0,0,1,1)
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

        //MASK SUPPORT ADD
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
        //MASK SUPPORT END

        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            uniform sampler2D _MaskTex;
            uniform float4 _MaskTex_ST;
            uniform float4 _Color;
            uniform float4 _Range;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(VertexOutput i) : COLOR
            {
                float alpha = i.uv0.x >= _Range.x && i.uv0.x <= _Range.z && i.uv0.y >= _Range.y && i.uv0.y <= _Range.w
                                  ? _Color.a - tex2D(_MaskTex,TRANSFORM_TEX(
                                                         float2((i.uv0.x-_Range.x)/(_Range.z-_Range.x),(i.uv0.y-_Range.y
                                                         )/(_Range.w-_Range.y)), _MaskTex)).a
                                  : _Color.a;
                return fixed4(_Color.rgb, alpha > 1 ? 1 : alpha);
            }
            ENDCG
        }
    }
}