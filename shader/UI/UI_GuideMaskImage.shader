Shader "XKnight/UI/UI_GuideMaskImage"
{
    // 该shader用于UGUI的Image
    // 借助顶点的Color属性为像素点赋颜色值，像素点上alpha值不为0时使用Mask Alpha，否则该像素点alpha值为0
    
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //MASK SUPPORT ADD
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        //MASK SUPPORT END
        [HideInInspector] _MaskAlpha ("Mask Alpha", Range(0,1)) = 0.8
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float _MaskAlpha;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = i.color;
                return fixed4(col.rgb, col.a > 0 ? _MaskAlpha : 0);
            }
            ENDCG
        }
    }
}