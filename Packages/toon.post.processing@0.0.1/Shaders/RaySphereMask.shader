/*
解析式全屏 Ray-Sphere 遮罩
 */
Shader "XKnight/ToonPostProcessing/RaySphereMask"
{
    Properties
    {
        [Main(Sphere, __, on, off)]
        _Sphere ("球设置", Float) = 1
        
        [Sub(Sphere)] _SphereCenter ("Sphere 的中心点（世界空间）", Vector) = (0, 0, 0, 0)
        [Sub(Sphere)] _SphereRadius ("Sphere 的半径", Float) = 1
        
        [Sub(Sphere)] _PoleThresholdInner ("分界线 - 内进度（溶解带内侧）", Range(0, 1)) = 0
        [Sub(Sphere)] _PoleThresholdOuter ("分界线 - 外进度（真实边界）", Range(0, 1)) = 0
        
        
        [Main(Edge, __, on, off)]
        _Edge ("边设置", Float) = 1
        
        [Sub(Edge)] _EdgeWidth ("过渡带宽度", Float) = 0
        [Sub(Edge)] _EdgePower ("过渡带的集中度（幂）", Range(1, 100)) = 1
        
        
        [Main(DissolveNoise), __, on, off)]
        _DissolveNoise ("消融噪声设置", Float) = 1
        
        [Sub(DissolveNoise)] _DissolveNoiseTex ("消融噪声图", 2D) = "gray" {}
        [Sub(DissolveNoise)] _DissolveNoiseScale ("噪声密度", Float) = 10
        [Sub(DissolveNoise)] _DissolveNoiseDir ("噪声滚动 (xy 方向/速度)", Vector) = (0, 1, 0, 0)
        [Sub(DissolveNoise)] _DissolveAmount ("消融幅度", Range(0, 0.5)) = 0.08
        
        [Sub(DissolveNoise)] _DissolveNoiseSwaySpeed  ("噪声 UV 晃动速度", Float) = 1
        [Sub(DissolveNoise)] _DissolveNoiseSwayAmount ("噪声 UV 晃动幅度", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RaySphereMask"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite Off ZTest Off Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex fullscreen_vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4x4 _SphereWorldToObject;
                float3 _SphereCenter;
                float _SphereRadius;
                
                half _PoleThresholdInner, _PoleThresholdOuter;
                half _EdgeWidth, _EdgePower;
                float4 _DissolveNoiseTex_ST;
                float4 _DissolveNoiseDir;
                half _DissolveNoiseScale, _DissolveAmount;
                half _DissolveNoiseSwaySpeed, _DissolveNoiseSwayAmount;
            CBUFFER_END
            
            TEXTURE2D_X_FLOAT(_CameraCharacterDepthTexture); SAMPLER(sampler_CameraCharacterDepthTexture);
            TEXTURE2D_X(_DissolveNoiseTex); SAMPLER(sampler_DissolveNoiseTex);

            #if SHADER_API_GLES
            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            #else
            struct Attributes
            {
                uint vertexID : SV_VertexID;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            #endif

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                float3 viewRayWS : TEXCOORD1;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings fullscreen_vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
                #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
                #endif

                output.positionCS = pos;
                output.uv = uv;
                
                float4 ndc = float4(uv * 2 - 1, 1, 1);
                // ndc.y *= _ProjectionParams.x;
                
                float4 rayEnd = mul(UNITY_MATRIX_I_VP, ndc);
                output.viewRayWS = rayEnd.xyz / rayEnd.w - _WorldSpaceCameraPos.xyz;

                return output;
            }
            
            #define FAR_DEPTH_THRESHOLD             0.999h
            #define POLE_THRESHOLD_START_OFFSET     -0.01
            #define POLE_THRESHOLD_END_OFFSET       0.1
            
            float ToEffectiveThreshold(half t01)
            {
                return -0.5 + POLE_THRESHOLD_START_OFFSET + t01 * (1.0 + POLE_THRESHOLD_END_OFFSET);
            }
            
            float RaySphereIntersect(float3 ro, float3 rd, float3 center, float radius)
            {
                float3 oc = ro - center;
                float B = dot(oc, rd);
                float C = dot(oc, oc) - radius * radius;
                float disc = B * B - C;
                
                float discSqrt = sqrt(max(0, disc)); // disc < 0 时 sqrt() = 0，无 NaN
                float t1 = -B + discSqrt;
                float t2 = -B - discSqrt;
                
                float inner = lerp(-1, t1, step(0, t1));
                float tHit = lerp(inner, t2, step(0, t2));
                
                return lerp(-1, tHit, step(0, disc));
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // 先确保把角色不会被挡住
                half4 cameraCharacterDepthColor = SAMPLE_TEXTURE2D_X(_CameraCharacterDepthTexture, sampler_CameraCharacterDepthTexture, input.uv);
                half cameraCharacter01Depth = Linear01Depth(cameraCharacterDepthColor.r, _ZBufferParams);
                clip(cameraCharacter01Depth - FAR_DEPTH_THRESHOLD);
                
                float3 ro = _WorldSpaceCameraPos.xyz;
                float3 rd = normalize(input.viewRayWS);

                float t = RaySphereIntersect(ro, rd, _SphereCenter, _SphereRadius);
                half hit = step(0, t); // t < 0 = 0, t >= 0 = 1
                
                float effectiveOuter = ToEffectiveThreshold(_PoleThresholdOuter);
                float effectiveInner = ToEffectiveThreshold(_PoleThresholdInner);
                
                float3 p_world = ro + t * rd;
                float3 p_obj = mul(_SphereWorldToObject, float4(p_world, 1)).xyz;
                
                // 外边界之外：直接裁掉，遮罩为0
                if (p_obj.y > effectiveOuter)
                {
                    return half4(0, 0, 0, 1);
                }
                
                // 内边界之内：完全不溶解，实心部分
                if (p_obj.y < effectiveInner)
                {
                    return half4(hit, 0, 0, 1);
                }
                
                // 在中间地带做溶解
                float band = max(effectiveOuter - effectiveInner, 0.001);
                
                float3 p_obj_n = normalize(p_obj);
                
                // 摇晃一下
                float swayAngle = sin(_Time.y * _DissolveNoiseSwaySpeed) * _DissolveNoiseSwayAmount;
                float csa = cos(swayAngle), ssa = sin(swayAngle);
                float3 p_swayed = p_obj_n;
                p_swayed.xz = float2(csa * p_obj_n.x - ssa * p_obj_n.z, ssa * p_obj_n.x + csa * p_obj_n.z);
                
                float2 uvOct = p_swayed.xy / (abs(p_swayed.x) + abs(p_swayed.y) + abs(p_swayed.z));
                float2 dissolveUV = (uvOct + 1) * 0.5;
                dissolveUV *= _DissolveNoiseScale;
                dissolveUV = dissolveUV * _DissolveNoiseTex_ST.xy + _DissolveNoiseTex_ST.zw;
                dissolveUV += frac(_DissolveNoiseDir.xy * _Time.y);
                
                half noiseVal = SAMPLE_TEXTURE2D_X(_DissolveNoiseTex, sampler_DissolveNoiseTex, dissolveUV).r;
                float edge = effectiveInner + noiseVal * band;
                
                // 软化边界
                half sy = CheapSmoothStep(edge - _EdgeWidth, edge + _EdgeWidth, p_obj.y);
                half maskValue = 1 - pow(sy, _EdgePower);
                
                return half4(maskValue * hit, 0, 0, 1);
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}