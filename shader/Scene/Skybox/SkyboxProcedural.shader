Shader "XKnight/Scene/Skybox/Procedural"
{
    Properties
    {
        _SunSize ("Sun Size", Range(0,1)) = 0.04
        _SunSizeConvergence("Sun Size Convergence", Range(1,10)) = 5
//        [Toggle(_MOON)]_Moon("_Moon",Float) = 0
//        _MoonTex("_MoonTex",2D) = "white"{}
//        _MoonColor("_MoonColor",Color) = (1,1,1,1)
//        _MoonColorIntensity("_MoonColorIntensity",float) = 1
//        _MoonRadius("_MoonRadius",Float) = 1
//        _MoonMaskRadius("_MoonMaskRadius",Float) = 1
        _AtmosphereThickness ("Atmosphere Thickness", Range(0,5)) = 1.0
        _SkyTint ("Sky Tint", Color) = (.5, .5, .5, 1)
        _GroundColor ("Ground", Color) = (.369, .349, .341, 1)
        _Exposure("Exposure", Range(0, 8)) = 1.3
        _ExposurePost("_ExposurePost", Range(0, 8)) = 1
        _ContrastPost("_ContrastPost", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

        Pass
        {
            Name "Skybox No Mesh"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
            float4  _MainLightColorCustom;
            float4  _MainLightDirCustom;
            uniform half _Exposure;     // HDR exposure
            uniform half3 _GroundColor;
            uniform half _SunSize;
            uniform half _SunSizeConvergence;
            uniform half3 _SkyTint;
            uniform half _AtmosphereThickness;
            float _ExposurePost;
            float _ContrastPost;
            // #pragma shader_feature _ _MOON
    #if defined(UNITY_COLORSPACE_GAMMA)
        #define GAMMA 2
        #define COLOR_2_GAMMA(color) color
        #define COLOR_2_LINEAR(color) color*color
        #define LINEAR_2_OUTPUT(color) sqrt(color)
    #else
        #define GAMMA 2.2
        // HACK: to get gfx-tests in Gamma mode to agree until UNITY_ACTIVE_COLORSPACE_IS_GAMMA is working properly
        #define COLOR_2_GAMMA(color) ((unity_ColorSpaceDouble.r>2.0) ? pow(color,1.0/GAMMA) : color)
        #define COLOR_2_LINEAR(color) color
        #define LINEAR_2_LINEAR(color) color
    #endif

            
            
            
        static const float3 kDefaultScatteringWavelength = float3(.65, .57, .475);
        static const float3 kVariableRangeForScatteringWavelength = float3(.15, .15, .15);

        #define OUTER_RADIUS 1.025
        static const float kOuterRadius = OUTER_RADIUS;
        static const float kOuterRadius2 = OUTER_RADIUS*OUTER_RADIUS;
        static const float kInnerRadius = 1.0;
        static const float kInnerRadius2 = 1.0;

        static const float kCameraHeight = 0.0001;

        #define kRAYLEIGH (lerp(0.0, 0.0025, pow(_AtmosphereThickness,2.5)))      // Rayleigh constant
        #define kMIE 0.0010             // Mie constant
        #define kSUN_BRIGHTNESS 20.0    // Sun brightness

        #define kMAX_SCATTER 50.0 // Maximum scattering value, to prevent math overflows on Adrenos

        static const half kHDSundiskIntensityFactor = 15.0;
        static const half kSimpleSundiskIntensityFactor = 27.0;

        static const half kSunScale = 400.0 * kSUN_BRIGHTNESS;
        static const float kKmESun = kMIE * kSUN_BRIGHTNESS;
        static const float kKm4PI = kMIE * 4.0 * 3.14159265;
        static const float kScale = 1.0 / (OUTER_RADIUS - 1.0);
        static const float kScaleDepth = 0.25;
        static const float kScaleOverScaleDepth = (1.0 / (OUTER_RADIUS - 1.0)) / 0.25;
        static const float kSamples = 2.0; // THIS IS UNROLLED MANUALLY, DON'T TOUCH
        #define MIE_G (-0.990)
        #define MIE_G2 0.9801
        #define SKY_GROUND_THRESHOLD 0.02
            
        // Calculates the Rayleigh phase function
        half getRayleighPhase(half eyeCos2)
        {
            return 0.75 + 0.75*eyeCos2;
        }
        half getRayleighPhase(half3 light, half3 ray)
        {
            half eyeCos = dot(light, ray);
            return getRayleighPhase(eyeCos * eyeCos);
        }
        
        // float4x4 LToW;
        // TEXTURE2D(_MoonTex);
        // SAMPLER(sampler_MoonTex);
            // float4 _MoonTex_ST;
            // float _MoonMaskRadius;
            // float _MoonRadius;
            // float3 _MoonLightDirCustom;
            // float _MoonColorIntensity;
            // float3 _MoonColor;
        struct appdata_t
        {
            float4 vertex : POSITION;
            // #if defined(_MOON) 
            // float4  uv         : TEXCOORD1;
            // #endif
        };

        struct v2f
        {
            float4  pos             : SV_POSITION;
            float3  vertex          : TEXCOORD0;
            half3   groundColor     : TEXCOORD1;
            half3   skyColor        : TEXCOORD2;
            half3   sunColor        : TEXCOORD3;
            UBPA_FOG_COORDS(4)
            // #if defined(_MOON) 
            // float4  uv         : TEXCOORD5;
            // float4  worldPos         : TEXCOORD6;
            // #endif
            
        };


        float scale(float inCos)
        {
            float x = 1.0 - inCos;
            return 0.25 * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
        }

        v2f vert (appdata_t v)
        {
            v2f OUT;
            OUT.pos = TransformObjectToHClip(v.vertex.xyz);
            
            float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
            float3 kSkyTintInGammaSpace = pow(_SkyTint,1.0/GAMMA); // convert tint from Linear back to Gamma
            float3 kScatteringWavelength = lerp (
                kDefaultScatteringWavelength-kVariableRangeForScatteringWavelength,
                kDefaultScatteringWavelength+kVariableRangeForScatteringWavelength,
                half3(1,1,1) - kSkyTintInGammaSpace); // using Tint in sRGB gamma allows for more visually linear interpolation and to keep (.5) at (128, gray in sRGB) point
            float3 kInvWavelength = 1.0 / pow(kScatteringWavelength, 4);
            float kKrESun = kRAYLEIGH * kSUN_BRIGHTNESS;
            float kKr4PI = kRAYLEIGH * 4.0 * 3.14159265;
            float3 cameraPos = float3(0,kInnerRadius + kCameraHeight,0);    // The camera's current position
            // Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
            // float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
            float3 eyeRay = TransformObjectToWorldDir(v.vertex.xyz);
            float far = 0.0;
            half3 cIn, cOut;
            // #if defined (_MOON)
            // OUT.uv = v.uv;
            // float3 NormalizeWorldPos = normalize(positionWS);
            // OUT.worldPos.xyz = NormalizeWorldPos;
            // #endif
            
            if(eyeRay.y >= 0.0)
            {
                // Sky
                // Calculate the length of the "atmosphere"
                far = sqrt(kOuterRadius2 + kInnerRadius2 * eyeRay.y * eyeRay.y - kInnerRadius2) - kInnerRadius * eyeRay.y;
                float3 pos = cameraPos + far * eyeRay;
                // Calculate the ray's starting position, then calculate its scattering offset
                float height = kInnerRadius + kCameraHeight;
                float depth = exp(kScaleOverScaleDepth * (-kCameraHeight));
                float startAngle = dot(eyeRay, cameraPos) / height;
                float startOffset = depth*scale(startAngle);
                // Initialize the scattering loop variables
                float sampleLength = far / kSamples;
                float scaledLength = sampleLength * kScale;
                float3 sampleRay = eyeRay * sampleLength;
                float3 samplePoint = cameraPos + sampleRay * 0.5;
                // Now loop through the sample rays
                float3 frontColor = float3(0.0, 0.0, 0.0);
                // Weird workaround: WP8 and desktop FL_9_3 do not like the for loop here
                // (but an almost identical loop is perfectly fine in the ground calculations below)
                // Just unrolling this manually seems to make everything fine again.
//              for(int i=0; i<int(kSamples); i++)
                {
                    float height = length(samplePoint);
                    float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
                    float lightAngle = dot(_MainLightDirCustom.xyz, samplePoint) / height;
                    float cameraAngle = dot(eyeRay, samplePoint) / height;
                    float scatter = (startOffset + depth*(scale(lightAngle) - scale(cameraAngle)));
                    float3 attenuate = exp(-clamp(scatter, 0.0, kMAX_SCATTER) * (kInvWavelength * kKr4PI + kKm4PI));
                    frontColor += attenuate * (depth * scaledLength);
                    samplePoint += sampleRay;
                }
                {
                    float height = length(samplePoint);
                    float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
                    float lightAngle = dot(_MainLightDirCustom.xyz, samplePoint) / height;
                    float cameraAngle = dot(eyeRay, samplePoint) / height;
                    float scatter = (startOffset + depth*(scale(lightAngle) - scale(cameraAngle)));
                    float3 attenuate = exp(-clamp(scatter, 0.0, kMAX_SCATTER) * (kInvWavelength * kKr4PI + kKm4PI));
                    frontColor += attenuate * (depth * scaledLength);
                    samplePoint += sampleRay;
                }
                // Finally, scale the Mie and Rayleigh colors and set up the varying variables for the pixel shader
                cIn = frontColor * (kInvWavelength * kKrESun);
                cOut = frontColor * kKmESun;
            }
            else
            {
                // Ground
                far = (-kCameraHeight) / (min(-0.001, eyeRay.y));
                float3 pos = cameraPos + far * eyeRay;
                // Calculate the ray's starting position, then calculate its scattering offset
                float depth = exp((-kCameraHeight) * (1.0/kScaleDepth));
                float cameraAngle = dot(-eyeRay, pos);
                float lightAngle = dot(_MainLightDirCustom.xyz, pos);
                float cameraScale = scale(cameraAngle);
                float lightScale = scale(lightAngle);
                float cameraOffset = depth*cameraScale;
                float temp = (lightScale + cameraScale);
                // Initialize the scattering loop variables
                float sampleLength = far / kSamples;
                float scaledLength = sampleLength * kScale;
                float3 sampleRay = eyeRay * sampleLength;
                float3 samplePoint = cameraPos + sampleRay * 0.5;
                // Now loop through the sample rays
                float3 frontColor = float3(0.0, 0.0, 0.0);
                float3 attenuate;
//              for(int i=0; i<int(kSamples); i++) // Loop removed because we kept hitting SM2.0 temp variable limits. Doesn't affect the image too much.
                {
                    float height = length(samplePoint);
                    float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
                    float scatter = depth*temp - cameraOffset;
                    attenuate = exp(-clamp(scatter, 0.0, kMAX_SCATTER) * (kInvWavelength * kKr4PI + kKm4PI));
                    frontColor += attenuate * (depth * scaledLength);
                    samplePoint += sampleRay;
                }
                cIn = frontColor * (kInvWavelength * kKrESun + kKmESun);
                cOut = clamp(attenuate, 0.0, 1.0);
            }
            OUT.vertex          = -eyeRay;
            // if we want to calculate color in vprog:
            // 1. in case of linear: multiply by _Exposure in here (even in case of lerp it will be common multiplier, so we can skip mul in fshader)
            // 2. in case of gamma and SKYBOX_COLOR_IN_TARGET_COLOR_SPACE: do sqrt right away instead of doing that in fshader
            OUT.groundColor = _Exposure * (cIn + COLOR_2_LINEAR(_GroundColor) * cOut);
            OUT.skyColor    = _Exposure * (cIn * getRayleighPhase(_MainLightDirCustom.xyz, -eyeRay));
            // half lightColorIntensity = clamp(length(_LightColor0.xyz), 0.25, 1);
            half lightColorIntensity = clamp(length(_MainLightColorCustom.xyz), 0.25, 1);
            OUT.sunColor    = kHDSundiskIntensityFactor * saturate(cOut) * _MainLightColorCustom.xyz / lightColorIntensity;
            UBPA_TRANSFER_FOG(OUT, positionWS);
            return OUT;
        }


        // Calculates the Mie phase function
        half getMiePhase(half eyeCos, half eyeCos2)
        {
            half temp = 1.0 + MIE_G2 - 2.0 * MIE_G * eyeCos;
            temp = pow(temp, pow(_SunSize,0.65) * 10);
            temp = max(temp,1.0e-4); // prevent division by zero, esp. in half precision
            temp = 1.5 * ((1.0 - MIE_G2) / (2.0 + MIE_G2)) * (1.0 + eyeCos2) / temp;
            return temp;
        }
            
        half calcSunAttenuation(half3 lightPos, half3 ray)
        {
            half focusedEyeCos = pow(saturate(dot(lightPos, ray)), _SunSizeConvergence);
            return getMiePhase(-focusedEyeCos, focusedEyeCos * focusedEyeCos);
        }
            
        void ColorGradingContrast(inout float3 colorLinear,float exposture,float contrast)
        {
            exposture = max(exposture, 1.0e-4);
            colorLinear *= exposture;
            float3 colorLog = LinearToLogC(colorLinear);
            colorLog = (colorLog - ACEScc_MIDGRAY) * contrast + ACEScc_MIDGRAY;
            colorLinear = LogCToLinear(colorLog);            
        }    
            

        half4 frag (v2f IN) : SV_Target
        {
            half3 col = half3(0.0, 0.0, 0.0);
        // if y > 1 [eyeRay.y < -SKY_GROUND_THRESHOLD] - ground
        // if y >= 0 and < 1 [eyeRay.y <= 0 and > -SKY_GROUND_THRESHOLD] - horizon
        // if y < 0 [eyeRay.y > 0] - sky
            half3 ray = normalize(IN.vertex.xyz);
            half y = ray.y / SKY_GROUND_THRESHOLD;
            // if we did precalculate color in vprog: just do lerp between them
            col = lerp(IN.skyColor, IN.groundColor, saturate(y));
            if(y < 0.0)
            {
                col += IN.sunColor * calcSunAttenuation(_MainLightDirCustom.xyz, -ray);
            }
            // #if defined(_MOON) 
            //  float MoonDist = distance(IN.uv.xyz,_MoonLightDirCustom);
            //  float moonArea = 1 - clamp((MoonDist * _MoonMaskRadius),0,1);
            //  float  _WorldPosDotUp = dot(IN.worldPos.xyz, float3(0,1,0));
            //  float  _WorldPosDotUpstep = smoothstep(0,0.1,_WorldPosDotUp);
            //  float3 moonUV = mul(IN.uv.xyz,LToW);
            //  moonUV.xy = moonUV.xy * _MoonTex_ST.xy * _MoonRadius + _MoonTex_ST.zw;
            //  half3 moonColor = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, moonUV.xy) * _WorldPosDotUpstep * moonArea;
            //  col += moonColor * _MoonColorIntensity * _MoonColor;
             //return half4(moonColor,1.0);
            // #else
            //     float3 right = normalize(cross(_MoonLightDirCustom, float3(0, 1, 0))); // 改用Up轴防死锁
            //     float3 up = normalize(cross(right, _MoonLightDirCustom));
            //     // 利用点积，求出视线向量在着两个轴上的平面投影距离
            //     float2 billboardUV = float2(dot(right, IN.uv.xyz), dot(up, IN.uv.xyz));
            //     // 缩放并居中
            //     float2 moonUV = billboardUV * _MoonRadius * _MoonTex_ST.xy + _MoonTex_ST.zw + 0.5;
            //     half3 moonColor = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, moonUV.xy);
                // return half4(moonColor,1.0);
            // #endif
            UBPA_APPLY_FOG(IN, col);
            
            ColorGradingContrast(col,_ExposurePost,_ContrastPost);
            return half4(col,1.0);
        }
            ENDHLSL
        }
    }
}
