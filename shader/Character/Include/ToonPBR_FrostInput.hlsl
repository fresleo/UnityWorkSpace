#ifndef __TOONPBR_FROST_INPUT__
#define __TOONPBR_FROST_INPUT__

#define TOONPBR_FROST_TEXTURE \
    TEXTURE2D_X(_FrostTexture); SAMPLER(sampler_FrostTexture); \
    TEXTURE2D_X(_FrostBumpMap); SAMPLER(sampler_FrostBumpMap); \
    TEXTURE2D_X(_IceOverlayMask); SAMPLER(sampler_IceOverlayMask); \
    TEXTURE2D_X(_IcicleMask); SAMPLER(sampler_IcicleMask); \

#define TOONPBR_FROST_CBUFFER \
    float4  _FrostTexture_ST, _FrostBumpMap_ST, _IceOverlayMask_ST, _IcicleMask_ST; \
    \
    half4   _FrostTint; \
    float   _FrostBumpScale; \
    float   _IcicleMaskTile; \
    \
    float   _IceSlider; \
    float   _IceAmount; \
    float   _YMaskTop, _YMaskDown; \
    float   _IcicleLength; \
    float   _yIceMultiplier; \
    \
    float   _FrostEmissionFresnelIntensity; \
    float   _FrostEmissionFresnelPow; \
    \
    float   _TransmissionShadow; \
    float   _TransStrength; \
    float   _TransNormal; \
    float   _TransScattering; \
    float   _TransDirect; \
    float   _TransAmbient; \
    float   _TransShadow; \
    \
    float   _TessValue; \
    float   _TessMin; \
    float   _TessMax; \
    float   _TessEdgeLength; \
    float   _TessMaxDisp; \

#endif // __TOONPBR_FROST_INPUT__
