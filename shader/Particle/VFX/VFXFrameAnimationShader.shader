Shader "XKnight/Particle/VFXFrameAnimationShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    	_Mask("Mask", 2D) = "white" {}
		[IntRange]_Columns("Columns", Range( 0 , 6)) = 0
		[IntRange]_Rows("Rows", Range( 0 , 6)) = 0
		_Speed("Speed", Float) = 0
    	
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("SrcBlend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend", Float) = 0.0
    }
	
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZTest Off
            Cull Off
        	Blend[_SrcBlend][_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			CBUFFER_START(UnityPerMaterial)
				float _Columns;
				float _Rows;
				float _Speed;
	            float4 _MainTex_ST;
			CBUFFER_END

            sampler2D _MainTex;
            sampler2D _Mask;
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv * 2.0f - 1.0f, .0f, 1.0f);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
#if UNITY_UV_STARTS_AT_TOP
            	i.uv.y = 1.0f - i.uv.y;
#endif
            	
                float2 texCoord18 = i.uv.xy;
				// *** BEGIN Flipbook UV Animation vars ***
				// Total tiles of Flipbook Texture
				float fbtotaltiles13 = _Columns * _Rows;
				// Offsets for cols and rows of Flipbook Texture
				float fbcolsoffset13 = 1.0f / _Columns;
				float fbrowsoffset13 = 1.0f / _Rows;
				// Speed of animation
				float fbspeed13 = GET_GLOBAL_TIME[ 1 ] * _Speed;
				// UV Tiling (col and row offset)
				float2 fbtiling13 = float2(fbcolsoffset13, fbrowsoffset13);
				// UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
				// Calculate current tile linear index
				float fbcurrenttileindex13 = round( fmod( fbspeed13 + 1.0, fbtotaltiles13) );
				fbcurrenttileindex13 += ( fbcurrenttileindex13 < 0) ? fbtotaltiles13 : 0;
				// Obtain Offset X coordinate from current tile linear index
				float fblinearindextox13 = round ( fmod ( fbcurrenttileindex13, _Columns ) );
				// Reverse X animation if speed is negative
				fblinearindextox13 = (_Speed > 0 ? fblinearindextox13 : (int)_Columns - fblinearindextox13);
				// Multiply Offset X by coloffset
				float fboffsetx13 = fblinearindextox13 * fbcolsoffset13;
				// Obtain Offset Y coordinate from current tile linear index
				float fblinearindextoy13 = round( fmod( ( fbcurrenttileindex13 - fblinearindextox13 ) / _Columns, _Rows ) );
				// Reverse Y to get tiles from Top to Bottom and Reverse Y animation if speed is negative
				fblinearindextoy13 = (_Speed <  0 ? fblinearindextoy13 : (int)_Rows - fblinearindextoy13);
				// Multiply Offset Y by rowoffset
				float fboffsety13 = fblinearindextoy13 * fbrowsoffset13;
				// UV Offset
				float2 fboffset13 = float2(fboffsetx13, fboffsety13);
				// Flipbook UV
				half2 fbuv13 = texCoord18 * fbtiling13 + fboffset13;
				// *** END Flipbook UV Animation vars ***
				float4 tex2DNode17 = tex2D( _MainTex, fbuv13 );
            	float mask = tex2D(_Mask, i.uv);

	            return tex2DNode17 * mask;
            }
            ENDCG
        }
    }
}
