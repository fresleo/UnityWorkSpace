Shader "XKnight/Scene/StarClouds"
{
    Properties
    {
        [Main(Settings, _, off, off)]
        _Settings("Settings", Float) = 1
        [Sub(Settings)] _AllCloudsAlpha ("Clouds Alpha--云透明度(不含星星)", Range(0,1)) = 1
        [Sub(Settings)] _IntensityScale ("Intensity Scale--星空整体曝光度(含星星)", Range(0,2)) = 1

        [Main(Cloud, _, off, off)]
        _Main("Main", Float) = 1
        [Tex(Cloud, _Cloud01Color)] _Cloud01Tex ("Cloud 01 Texture--云1 纹理", 2D) = "white" {}
        [Tex(Cloud, _Cloud02Color)] _Cloud02Tex ("Cloud 02 Texture--云2 纹理", 2D) = "white" {}
        [HideInInspector] _Cloud01Color ("Cloud 01 Color--云1 颜色", Color) = (1,1,1,1)
        [HideInInspector] _Cloud02Color ("Cloud 02 Color--云2 颜色", Color) = (1,1,1,1)
        [Sub(Cloud)] _CloudMultiplyer ("Cloud 01 Multiplier--云1 曝光度", Float) = 1
        [Sub(Cloud)] _Cloud02Multipler ("Cloud 02 Multiplier--云2 曝光度", Float) = 1
        [Sub(Cloud)] _CloudOffset ("Cloud 01 Offset--云1 偏移", Float) = 0
        [Sub(Cloud)] _Cloud02Offset ("Cloud 02 Offset--云2 偏移", Float) = 0
        [Sub(Cloud)] _CloudTex01UV1Coord ("Cloud 01 UV1 缩放(XY)/偏移(ZW)", Vector) = (0,0,1,1)
        [Sub(Cloud)] _CloudTex02UV1Coord ("Cloud 02 UV1 缩放(XY)/偏移(ZW)", Vector) = (0,0,1,1)
        [Sub(Cloud)] _CloudTexMix("Cloud Tex Mix--云纹理 混合", Range(0,1)) = 1
        [Tex(Cloud)] _TintColorTex ("Tint Color Textur--云层色调 纹理", 2D) = "white" {}
        [Sub(Cloud)] _TintSpeed ("Tint Speed--云层色调波速", Range(0,1)) = 0
        [Sub(Cloud)] _TintColorTexScale ("Tint Color Scale--色调颜色 比例", Range(0.0, 1.0)) = 1
        [Sub(Cloud)] _TintColorTexUV1Coord ("Tint Color UV1 缩放(XY)/偏移(ZW)", Vector) = (0,0,1,1)

        [Main(Flow, _, off, off)]
        _Flow("Flow", Float) = 1
        [Tex(Flow)] _FlowTex ("Flow Texture--流动 纹理", 2D) = "white" {}
        [Sub(Flow)] _FlowSpeed ("Flow Speed--云层流速", Float) = 0
        [Sub(Flow)] _FlowStrength ("Flow Strength--云层扭曲强度", Float) = 0

        // Stars
        [Main(Star, _, off, off)]
        _Star("Star", Float) = 1
        [Tex(Star)] _StarTex ("Stars Texture--星星 纹理", 2D) = "white" {}
        [Tex(Star)] _ColorPalette ("Color Palette--星星调色 纹理", 2D) = "white" {}
        [Sub(Star)] _Desaturate ("Desaturate--星星去饱和", Range(0,2)) = 1
        [Sub(Star)] _ColorPalletteSpeed ("Color Palette Speed--星星调色板速率", Range(0,5)) = 0
        [Sub(Star)] _StarBrightness1 ("Star Brightness 1--星星亮度 1", Float) = 1
        [Sub(Star)] _StarBrightness2 ("Star Brightness 2--星星亮度 2", Float) = 1
        [Sub(Star)] _StarDepth ("Star Depth--星空深度", Float) = 0
        [Sub(Star)] _StarNoiseTiling ("Star Noise Tiling--星星噪波 平铺值", Vector) = (1,1,0,0)
        [Sub(Star)] _StarScintillationSpeed ("Star Scintillation Speed--星星 闪烁速度", Float) = 0
        [Sub(Star)] _StartOffsetSpeed("Star Offset Speed--星星偏移速度（正值朝下|负值朝上|默认为0）",Float) = 0
        [Sub(Star)] _StarTexUV1Coord ("Star UV1 缩放(XY)/偏移(ZW)", Vector) = (0,0,1,1)
        [Sub(Star)] _StarTexUV2Coord ("Star UV2 缩放(XY)/偏移(ZW)", Vector) = (0,0,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+100"
        }

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined( LOD_FADE_CROSSFADE )
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif
            
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            TEXTURE2D(_Cloud01Tex); SAMPLER(sampler_Cloud01Tex);
            TEXTURE2D(_Cloud02Tex); SAMPLER(sampler_Cloud02Tex);
            TEXTURE2D(_ColorPalette); SAMPLER(sampler_ColorPalette);
            TEXTURE2D(_TintColorTex); SAMPLER(sampler_TintColorTex);
            TEXTURE2D(_StarTex); SAMPLER(sampler_StarTex);
            TEXTURE2D(_FlowTex); SAMPLER(sampler_FlowTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Cloud01Color;
                float4 _CloudTex01UV1Coord;
                float _CloudOffset;
                float _CloudMultiplyer;

                float4 _Cloud02Color;
                float4 _CloudTex02UV1Coord;
                float _Cloud02Offset;
                float _Cloud02Multipler;
                float _CloudTexMix;

                float4 _ColorPalette_ST;
                float _ColorPalletteSpeed;

                float _AllCloudsAlpha;
                float _Desaturate;
                float _TintSpeed;

                float _IntensityScale;

                float4 _TintColorTexUV1Coord;
                float _TintColorTexScale;

                float4 _StarTexUV1Coord;
                float _StarDepth;
                float4 _StarNoiseTiling;
                float _StarScintillationSpeed;
                float _StartOffsetSpeed;
                float _StarBrightness1;
                float _StarBrightness2;

                float _FlowSpeed;
                float _FlowStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                UBPA_FOG_COORDS(1)
                float2 uv2 : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv0 = input.uv0;
                output.uv2 = input.uv2;

                output.positionWS = vertexInput.positionWS;
                output.tangentWS.xyz = normalInput.tangentWS;
                output.positionCS = vertexInput.positionCS;

                UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

                return output;
            }

            // texUV是经过scale和offset变换的结果    
            half4 SampleFlowMap(float2 texUV, TEXTURE2D_PARAM(cloudTexture, cloudSampler))
            {
                float2 flowVal = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, texUV).xy * 2 - 1;

                float dif1 = frac(_Time.y * _FlowSpeed + 0.5) * _FlowStrength;
                float dif2 = frac(_Time.y * _FlowSpeed) * _FlowStrength;

                float lerpVal = abs(frac(_Time.y * _FlowSpeed) * 2 - 1);

                float4 col1 = SAMPLE_TEXTURE2D(cloudTexture, cloudSampler, texUV + flowVal * dif1);
                float4 col2 = SAMPLE_TEXTURE2D(cloudTexture, cloudSampler, texUV + flowVal * dif2);

                return lerp(col2, col1, lerpVal);
            }

            // Fragment shader
            half4 frag(Varyings input) : SV_Target
            {
                #if defined( LOD_FADE_CROSSFADE )
                LODFadeCrossFade(input.positionCS);
                #endif
                
                float2 flowUV2 = input.uv0.xy * _CloudTex02UV1Coord.xy + _CloudTex02UV1Coord.zw;

                half cloud02 = SampleFlowMap(flowUV2, _Cloud02Tex, sampler_Cloud02Tex);

                float2 flowUV4 = input.uv0.xy * _CloudTex01UV1Coord.xy + _CloudTex01UV1Coord.zw;
                half cloud01 = SampleFlowMap(flowUV4, _Cloud01Tex, sampler_Cloud01Tex);

                cloud01 = clamp((cloud01 + _CloudOffset) * _CloudMultiplyer, 0.0f, 2.0f);
                cloud02 = saturate((cloud02 + _Cloud02Offset) * _Cloud02Multipler);

                half3 cloud01Color = cloud01 * _Cloud01Color;
                half3 cloud02Color = cloud02 * _Cloud02Color;

                half3 cloud = cloud01Color + lerp(cloud01Color, cloud02Color - cloud01Color, _CloudTexMix);

                // Tint color
                float2 tintNoiseUV2 = input.uv0.xy * _TintColorTexUV1Coord.xy + _TintColorTexUV1Coord.zw;

                float tintNoise2 = _Time.y * _TintSpeed + tintNoiseUV2.y;
                float3 tintColorLerp = SAMPLE_TEXTURE2D(_TintColorTex, sampler_TintColorTex, float2(tintNoiseUV2.x, tintNoise2)).rgb;

                // Clouds Alpha
                float3 cloudsAlpha = cloud * _AllCloudsAlpha;

                tintColorLerp = lerp(dot(tintColorLerp, float3(0.298999995, 0.587000012, 0.114)), tintColorLerp, _TintColorTexScale);
                cloudsAlpha.rgb = cloudsAlpha.rgb * tintColorLerp;

                // Clouds Alpha Change
                float cloudChange = dot(cloudsAlpha, float3(0.298999995, 0.587000012, 0.114));
                cloudChange = saturate((cloudChange - 0.0399999991) * 10);

                // Stars
                float starUVAnimation = _Time.y * _StartOffsetSpeed;
                _StarTexUV1Coord.w = starUVAnimation;
                float2 starUV2 = input.uv0.xy * _StarTexUV1Coord.xy + _StarTexUV1Coord.zw;

                // 星星深度与切线相关
                float2 starDepth = input.tangentWS.xy * _StarDepth;
                float2 starUVOffset2 = starUV2 * 0.4 + starDepth;

                float starTex3 = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, starUV2).x;
                float starTex4 = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, starUVOffset2).y;

                float starLerp1 = starTex3;
                float starLerp2 = starTex4;

                float starBrightness1 = starLerp1 * _StarBrightness1;
                float starBrightness2 = starLerp2 * _StarBrightness2;
                starLerp1 = starBrightness1 * cloudChange + starBrightness2;

                //Stars
                float2 a = input.uv2.xy * _StarNoiseTiling.xy;
                float b = _Time.y * _StarScintillationSpeed;
                float4 b4 = b * float4(0.4, 0.2, 0.1, 0.5);
                a = a * 2 + b4.zw;
                b4.xy = input.uv2.xy * _StarNoiseTiling.xy + b4.xy;

                float StarScintillation1 = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, b4.xy).z;
                float StarScintillation2 = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, a.xy).z;

                float starsAndColor = StarScintillation1 * StarScintillation2;
                starsAndColor = saturate(starsAndColor * 3);
                starsAndColor = starsAndColor * starLerp1;

                // Color palette 
                float3 paletteUV2;
                paletteUV2.yz = input.uv0.xy * _ColorPalette_ST.xy + _ColorPalette_ST.zw;
                paletteUV2.x = _Time.y * _ColorPalletteSpeed + paletteUV2.y;

                float3 paletteColorLerp = SAMPLE_TEXTURE2D(_ColorPalette, sampler_ColorPalette, paletteUV2.xy).rgb;
                paletteColorLerp = lerp(paletteColorLerp, 1, _Desaturate);

                float3 finalColor = starsAndColor * paletteColorLerp + cloudsAlpha;

                UBPA_APPLY_FOG(input, finalColor);

                // Final Color
                finalColor.rgb *= _IntensityScale;

                return float4(finalColor.xyz, 1.0f);
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}