#ifndef __UI_GLICHEFFECT_INPUT__
#define __UI_GLICHEFFECT_INPUT__

CBUFFER_START(UnityPerMaterial)
        // sampler2D _MainTex;
        half4 _MainTex_ST;
half4 _Color;
half _ColorLine; 
half _ColorLineSpeed;
half _ColorLineScale;
half _ColorLineIntensity;
//half _Amount;
half _DSSpeed;
half _DSAmplitude;
half _DSIndensity;
half _LineAmount;
half _LineSpeed;
half _LineOffset;
int _HorizonTalToggle;
int _VerticalToggle;
int _HorizonTalToggle2;
int _VerticalToggle2;
half4 _MaskCenter;
half _MaskRadius;
half _NoiseScale;
half _NoiseSpeed;
half _DisturbInstensity;
half _Pixel;

half _FlashSpeed;

half _PixelScale;
half _UseMask;
half _PolkaDotDensity;
//int _UseScreenSpace;
//half _PolkaDotRotation;

half4 _OutlineColor;
half _OutlineWidth;
half _AlphaThreshold;
half4 _MainTex_TexelSize;
CBUFFER_END

half _IsGammaUI;
int _UIVertexColorAlwaysGammaSpace;

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
TEXTURE2D(_FlashMaskTex); SAMPLER(sampler_FlashMaskTex);
TEXTURE2D(_PolkadotMaskTex); SAMPLER(sampler_PolkadotMaskTex);
TEXTURE2D(_PolkadotTex); SAMPLER(sampler_PolkadotTex);
TEXTURE2D(_NoiseTex2); SAMPLER(sampler_NoiseTex2);

#endif
