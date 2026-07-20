// 液滴
Shader "Hidden/ReformSim/Droplet"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _Seed ("Random Seed", float) = 100
        _SpawnRect ("Spawn Rect", Vector) = (0, 0, 1920, 1080)
        _SizeRange ("Size Range", Vector) = (10, 30, 0, 0)
    }

    SubShader
    {
        ZWrite Off ZTest Always
        
        //Blend OneMinusDstColor OneMinusSrcColor
        //Blend One OneMinusSrcAlpha
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Seed;
            float4 _SpawnRect;
            float4 _SizeRange;

            float _PHI = 1.61803398874989484820459;

            float gold_noise(in float2 xy, in float seed)
            {
                return frac(tan(distance(xy * _PHI, xy) * seed) * xy.x);
            }

            float2 lerp(float2 a, float2 b, float2 t)
            {
                return a + (b - a) * t;
            }

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                int id = instanceID + 1;

                float2 gn1 = float2(
                    gold_noise(float2(1, id), _Seed + 1.0),
                    gold_noise(float2(id, 1), _Seed + 2.0));
                float2 pos = _SpawnRect.xy + _SpawnRect.zw * gn1;

                float2 size = float2(
                    gold_noise(float2(1, id), _Seed + 3.0),
                    gold_noise(float2(id, 1), _Seed + 4.0));
                size = lerp(float2(_SizeRange.x, _SizeRange.x), float2(_SizeRange.y, _SizeRange.y), size);

                float4x4 modelMatrix = float4x4(
                    size.x, 0.0, 0.0, pos.x,
                    0.0, size.x, 0.0, pos.y,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);
                float4x4 mvp = mul(UNITY_MATRIX_VP, modelMatrix);
                o.vertex = mul(mvp, v.vertex);

                o.uv = v.uv;

                return o;
            }

            sampler2D _MainTex;

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                texColor.rgb *= texColor.a;
                
                half4 color = half4(texColor.rg, 0.0, texColor.a);
                //half4 color = half4(texColor.rg * texColor.a, texColor.a, texColor.a);
                
                return color;
            }
            ENDCG
        }
    }
}