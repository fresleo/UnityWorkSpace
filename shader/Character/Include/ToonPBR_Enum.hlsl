#ifndef __TOONPBR_ENUM__
#define __TOONPBR_ENUM__

#define EShadingModel_DefaultLit 0
#define EShadingModel_Hair 1
#define EShadingModel_Face 2
#define EShadingModel_Eye 3
#define EShadingModel_Weapon 4

#define ESurfaceType_Opaque 0
#define ESurfaceType_Transparent 1

#define EShadowType_DoubleShade 0
#define EShadowType_Ramp 1
#define EShadowType_FaceSDFShadow 2
#define EShadowType_Shadow175 3

#define EDissolveType_None 0
#define EDissolveType_Random 1
#define EDissolveType_Direction 2
#define EDissolveType_Mask 3

#define ESpecularType_Default 0
#define ESpecularType_HairSpecularViewNormal 1
#define ESpecularType_HairSpecularTangent 2

#define ESpecularShadingMode_DirectSpecularToon 0
#define ESpecularShadingMode_Anisotropy 1

#define EShadowSource175_VertexColorR 0
#define EShadowSource175_PBRMaskR 1

#endif // __TOONPBR_ENUM__