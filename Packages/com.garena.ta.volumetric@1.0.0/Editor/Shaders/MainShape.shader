Shader "Hidden/MainShape"
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
            Name "MainShape2D"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "./Common.hlsl"
            #include "../../Runtime/Shaders/MathCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;

                return o;
            }
            float _Frequency;
            float _Coverage;
            float _Layer;
            float3 _Size;

            float getWorleyFbm(
                const float3 pointPos,
                float frequency,
                float amplitude,
                const float lacunarity,
                const float gain,
                const int octaveCount
            ) {
                float noise = 0.0;
                for (int i = 0; i < octaveCount; ++i) {
                    noise += amplitude * (1.0 - getTileableWorleyNoise(pointPos.xy, frequency));
                    frequency *= lacunarity;
                    amplitude *= gain;
                }
                return noise;
            }
            float getWorleyFbm(const float3 pointPos) {
                float cellCount = 4.0;
                float4 noise = float4(
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 4.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 16.0)
                    );
                float3 fbm = float3(
                    dot(noise.xyz, float3(0.625, 0.25, 0.125)),
                    dot(noise.yzw, float3(0.625, 0.25, 0.125)),
                    dot(noise.zw, float2(0.75, 0.25))
                    );
                return dot(fbm, float3(0.625, 0.25, 0.125));
            }

            float getPerlinWorley(const float3 pointPos) {
                int octaveCount = 3;
                float frequency = 8.0;
                float perlin = getPerlinNoise(pointPos, frequency, octaveCount);
                perlin = clamp(perlin, 0.0, 1.0);

                float cellCount = 4.0;
                float3 noise = float3(
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 14.0)
                    );
                float fbm = dot(noise, float3(0.625, 0.25, 0.125));
                return remap(perlin, 0.0, 1.0, fbm, 1.0);
            }

            float GetDensity(float2 pos)
            {
                float worley = getWorleyFbm(
                    float3(pos.x, pos.y, 0) + float3(0.5, 0.5, 0.5),
                    _Frequency, // frequency
                    0.4, // amplitude
                    2.0, // lacunarity
                    0.95, // gain
                    4 // octaveCount
                );
                worley = smoothstep(0.8, 1.4, worley);

                return worley;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 outputColor = float4(0, 0, 0, 1);

                float density = GetDensity(i.uv);

                outputColor.rgb = density;
                outputColor.a = density;
                return outputColor;
            }
            ENDHLSL
        }
        Pass
        {
            Name "MainShape3D"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "./Common.hlsl"
            #include "../../Runtime/Shaders/MathCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;

                return o;
            }
            float _Frequency;
            float _Coverage;
            float _Layer;
            float3 _Size;

            float getWorleyFbm(
                const float3 pointPos,
                float frequency,
                float amplitude,
                const float lacunarity,
                const float gain,
                const int octaveCount
            ) {
                float noise = 0.0;
                for (int i = 0; i < octaveCount; ++i) {
                    noise += amplitude * (1.0 - getTileableWorleyNoise(pointPos.xy, frequency));
                    frequency *= lacunarity;
                    amplitude *= gain;
                }
                return noise;
            }
            float getWorleyFbm(const float3 pointPos) {
                float cellCount = 4.0;
                float4 noise = float4(
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 4.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 16.0)
                    );
                float3 fbm = float3(
                    dot(noise.xyz, float3(0.625, 0.25, 0.125)),
                    dot(noise.yzw, float3(0.625, 0.25, 0.125)),
                    dot(noise.zw, float2(0.75, 0.25))
                    );
                return dot(fbm, float3(0.625, 0.25, 0.125));
            }

            float getPerlinWorley(const float3 pointPos) {
                int octaveCount = 3;
                float frequency = 8.0;
                float perlin = getPerlinNoise(pointPos, frequency, octaveCount);
                perlin = clamp(perlin, 0.0, 1.0);

                float cellCount = 4.0;
                float3 noise = float3(
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 2.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 8.0),
                    1.0 - getTileableWorleyNoise(pointPos.xy, cellCount * 14.0)
                    );
                float fbm = dot(noise, float3(0.625, 0.25, 0.125));
                return remap(perlin, 0.0, 1.0, fbm, 1.0);
            }
            float ShapeAlteringFunction(const float heightFraction) {
                //float biased = pow(heightFraction, bias);           // 应用偏差(bias=0.5)
                float biased = sqrt(heightFraction);
                float x = clamp(biased * 2.0 - 1.0, -1.0, 1.0);   // 映射到[-1,1]
                return 1.0 - x * x;                               // 半圆形变换
            }

            float2 SampleWeather(const float height, float weather)
            {
                float heightFraction = height;
                float localWeather = weather;
                //
                float heightScale = ShapeAlteringFunction(heightFraction);

                float factor = 1 - _Coverage * heightScale;

                //float density = remapClamped(localWeather * 0.5 + 0.5, factor, factor + 0.5);
                float density = saturate(localWeather + 1.0 - 2.0 * factor);
                return float2(heightFraction, density);
            }

            float GetDensity(float3 pos)
            {
                float worley = getWorleyFbm(
                    float3(pos.x, pos.z, 0) + float3(0.5, 0.5, 0.5),
                    _Frequency, // frequency
                    0.4, // amplitude
                    2.0, // lacunarity
                    0.95, // gain
                    4 // octaveCount
                );
                worley = smoothstep(0.8, 1.4, worley);
                float2 weather = SampleWeather(pos.y, worley);
                return weather.y;
            }

            float3 CalculateNormal(float3 pos) 
            {
                // 当前体素的 R 值
                float center = GetDensity(pos);

                float3 texelSize = 1.0 / _Size;
                // 相邻体素的 R 值（中心差分）
                float dx = GetDensity(pos + float3(texelSize.x, 0, 0)) - GetDensity(pos - float3(texelSize.x, 0, 0));

                float dy = GetDensity(pos + float3(0, texelSize.y, 0)) - GetDensity(pos - float3(0, texelSize.y, 0));

                float dz = GetDensity(pos + float3(0, 0, texelSize.z)) - GetDensity(pos - float3(0, 0, texelSize.z));

                // 梯度向量
                float3 gradient = float3(dx, dy, dz) * 0.5;

                // 法线（归一化梯度）
                return normalize(gradient);
            }
            float3 CalculateNormalBySobel(float3 pos)
            {
                float3 texelSize = 1.0 / _Size;

                float3 offsetX = float3(texelSize.x, 0, 0);
                float3 offsetY = float3(0, texelSize.y, 0);
                float3 offsetZ = float3(0, 0, texelSize.z);

                // Sobel weights for 3D gradient estimation
                float dx = 0.0;
                float dy = 0.0;
                float dz = 0.0;

                for (int z = -1; z <= 1; ++z)
                    for (int y = -1; y <= 1; ++y)
                        for (int x = -1; x <= 1; ++x)
                        {
                            float3 offset = float3(x, y, z) * texelSize;
                            
                            float density = GetDensity(pos + offset);
                            dx += density * x;
                            dy += density * y;
                            dz += density * z;
                        }

                return normalize(float3(dx, dy, dz));
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 outputColor = float4(0, 0, 0, 1);
                //if (_Layer <= 0 || _Layer >= 1)//
                //    return 0;
                float3 pointPos = float3(i.uv.x, _Layer, i.uv.y);
                
                float density = GetDensity(pointPos);

                //法线方案不太可行，效率粗糙
                //float3 normal = CalculateNormalBySobel(pointPos);
                
                density = pow(density, 0.75);//TODO:
                outputColor.rgb = density;
                outputColor.a = density;
                return outputColor;
            }
            ENDHLSL
        }
    }
}
