Shader "Hidden/DetailShape"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./Common.hlsl"
            #include "../../Runtime/Shaders/MathCommon.hlsl"
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Layer;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }


            float getPerlinWorley(const float3 pointPos) {
                int octaveCount = 3;
                float frequency = 8.0;
                float perlin = getPerlinNoise(pointPos, frequency, octaveCount);
                perlin = clamp(perlin, 0.0, 1.0);

                float cellCount = 4.0;
                float3 noise = float3(
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 14.0)
                );
                float fbm = dot(noise, float3(0.625, 0.25, 0.125));
                return remap(perlin, 0.0, 1.0, fbm, 1.0);
            }

            float getWorleyFbm(const float3 pointPos) {
                float cellCount = 4.0;
                float4 noise = float4(
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 4.0),
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos, cellCount * 16.0)
                );
                float3 fbm = float3(
                    dot(noise.xyz, float3(0.625, 0.25, 0.125)),
                    dot(noise.yzw, float3(0.625, 0.25, 0.125)),
                    dot(noise.zw, float2(0.75, 0.25))
                );
                return dot(fbm, float3(0.625, 0.25, 0.125));
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float3 pointPos = float3(i.uv, _Layer);
                float perlinWorley = getPerlinWorley(pointPos);
                float worleyFbm = getWorleyFbm(pointPos);
                float4 outputColor = remap(perlinWorley, worleyFbm - 1.0, 1.0);
                outputColor.a = 1;
          
                return outputColor;
            }
            ENDHLSL
        }
    }
}
