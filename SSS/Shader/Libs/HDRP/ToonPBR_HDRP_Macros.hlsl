#ifndef TOONPBR_HDRP_MACROS
#define TOONPBR_HDRP_MACROS

#define _HDRP_CHARACTER 1

#define TOONPBR_HDRP_POSITION_WS_ABSOLUTE 0

// Rim 的屏幕深度采样依赖 _CameraDepthTexture，暂不接入屏幕深度
#undef _RIM_ON


#undef _HAS_CEL_HAIR_SHADOW_V1
#undef _HAS_CEL_HAIR_SHADOW_V2
#undef _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE

#undef _FROST_ON
#undef _ICE_OVERLAY_MASK_ON

#undef _TRANSMISSION_LIGHT_ON
#undef _TRANSLUCENCY_ON

// MRT 输出（URP 的 ForwardLit 会向 SV_Target1/2/3 输出附加 buffer，
// HDRP ForwardOnly 的 MRT 要通过 Custom Pass + RT 机制来做）
#undef _MRT_BUFFER

// 暂不处理附加光（点光 / 聚光）
#undef _ADDITIONAL_LIGHTS
#undef _ADDITIONAL_LIGHTS_VERTEX

#endif // TOONPBR_HDRP_MACROS
