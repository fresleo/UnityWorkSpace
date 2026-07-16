#if !defined(VFX_PASS_LOD1)
#define VFX_PASS_LOD1

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MaterialVolume.hlsl"
#include "VFXCoreLOD1.hlsl"

v2f vert_simple(appdata v)
{
    v2f o = (v2f)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);
    float3 worldNormal = normalize(TransformObjectToWorldNormal(v.normal));
    o.worldPos = worldPos;
    o.vertex = TransformWorldToHClip(worldPos.xyz);
    o.color = v.color;
    o.uv_fresnel.xy = v.uv.xy;
    o.uv_fresnel.z = 1.0f - dot(worldNormal, viewDir);

    o.customData1 = float4(v.uv.zw, v.uv1.xy);
    o.customData2 = float4(v.uv1.zw, v.uv2.zw);

    o.grabPos = ComputeScreenPos(o.vertex);
    o.grabPos.z = -TransformWorldToView(worldPos.xyz).z;

#ifndef _MAINTEXUSESCREENUV_ON    
    o.mainUV = MainTexOffset(float4(o.uv_fresnel.xy, o.customData1.xy));
#endif
    
    return o;
}

float UnityGet2DClipping (float2 position, float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

half4 frag_simple(v2f i, half faceId : VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    
    half4 mainColor = half4(0,0,0,1);
    float4 mainUV = float4(0,0,0,0);
    float2 uvTextures = i.uv_fresnel.xy;

    // 由于使用屏幕坐标作为uv时需要是插值后计算，所以区分两种情况
#if _MAINTEXUSESCREENUV_ON
    i.uv_fresnel.xy = i.grabPos.xy * rcp(i.grabPos.w);
    mainUV = MainTexOffset(float4(i.uv_fresnel.xy, i.customData1.xy));
    if (!_OnlyMainTexUseScreenUV)
    {
        uvTextures = mainUV.zw;
    } 
#else
    mainUV = i.mainUV;
    uvTextures = i.mainUV.zw;
#endif    
    
    float fresnel = i.uv_fresnel.z;
    float2 mainTexScroll = i.customData1.xy;
    float dissolveCustomData = i.customData1.z;
    float colorIntensity = i.customData1.w;
    float2 mainTexMaskScroll = i.customData2.xy;
#ifdef _FRESNELON_ON
    half fresnelAlpha = lerp(1.0, i.customData2.w, _FresnelUseCustomData2W);
#endif
    float2 distortionUV = float2(0, 0);
    
    // 主纹理旋转
    #ifdef _MAINTEXTUREROTATIONUVON_ON
    {
        mainUV.xy = MainTexRotation(mainUV.xy, _MainTexRotationCenter.xy, _MainTexRotationAngle / 360 * 6.283185307);
    }
    #endif

    float2 polarUV = PolarUV(mainUV.zw);
    float2 finalMainUV = mainUV.xy;
    
    #ifdef _DISTORTIONON_ON//if(_DistortionOn)
    {
        float2 distortUV = float2(0, 0);
        
        #ifdef _DISTORTIONRADIALUVON_ON//if(_DistortionRadialUVOn)
        {
            distortUV = polarUV * _DistortTile.xy + frac(_DistortDir.xy * GET_GLOBAL_TIME.xx);
        }
        #else//else
        {
            float scalex = lerp(1.0f, length(unity_ObjectToWorld._11_12_13), _DistortionAutoScale);
            float scaley = lerp(1.0f, length(unity_ObjectToWorld._21_22_23), _DistortionAutoScale);
            
            distortUV.xy = uvTextures * _DistortTile.xy * float2(scalex, scaley)  + frac(_DistortDir.xy * GET_GLOBAL_TIME.xx);
        }
        #endif
        
        #ifdef _MAINTEXTURERADIALUVON_ON// if(_MainTextureRadialUVOn)
        {
            mainUV.xy = polarUV * _MainTex_ST.xy + (_MainTexOffsetUseCustomData_XY > 0.0f ? mainTexScroll : _MainTex_ST.zw * lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop));
        }
        #endif
        
        distortionUV = ApplyDistortion_LOD1(mainUV, distortUV);

        if(_DistortionAffectMainTexture)
            finalMainUV = mainUV.xy + distortionUV;
    }
    #else//else
    {
        #ifdef _MAINTEXTURERADIALUVON_ON//if(_MainTextureRadialUVOn)
        {
            finalMainUV = MainTexOffset(polarUV, mainTexScroll);
        }
        #endif
    }
    #endif

    mainColor = SampleMainTex_LOD1(finalMainUV, i, faceId, colorIntensity);

    ApplyMainTexBlend(mainColor, uvTextures);

    ApplyMainTexMask(mainColor, uvTextures, mainTexMaskScroll, distortionUV);

    //dissolve
    #ifdef _DISSOLVEON_ON//if(_DissolveOn)
    {
        // 溶解图 UV
        float2 dissolveOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveTexOffsetStop);
        float2 dissolveOffset = _DissolveTex_ST.zw * dissolveOffsetScale;
        
        float2 dissolveUV = uvTextures + (_DistortionAffectDissolve ? distortionUV : 0);
        dissolveUV = dissolveUV * _DissolveTex_ST.xy + (_DissolveOffsetByCustomData2xy ? mainTexMaskScroll : dissolveOffset);

        // 溶解方向图 UV
        float2 dissolveDirectionOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveDirectionTexOffsetStop);
        float2 dissolveDirectionOffset = _DissolveDirectionTex_ST.zw * dissolveDirectionOffsetScale;
        
        float2 dissolveDirectionUV = uvTextures * _DissolveDirectionTex_ST.xy + dissolveDirectionOffset;
        
        ApplyDissolve(mainColor, dissolveUV, dissolveDirectionUV, i.color, dissolveCustomData);
    }
    #endif

    #ifdef _FRESNELON_ON//if(_FresnelOn)
    {
        ApplyFresnel(mainColor, fresnel, fresnelAlpha);
    }
    #endif

    #ifdef _FRESNELREVERTON_ON//if(_FresnelRevertOn)
    {
        ApplyFresnelRevert(mainColor, fresnel);
    }
    #endif
    
    mainColor.rgb = BlendVolumeColor(i.worldPos, mainColor.rgb);
    
    // 因为 ui 有 gamma 矫正，而 spine 也是当 ui 绘制的，所以也需要统一处理一下颜色的 gamma
    mainColor.rgb = lerp(mainColor.rgb, LinearToSRGB(mainColor.rgb), _IsGammaUI);
    
    mainColor.a = saturate(mainColor.a) * _Alpha;

    // required for RectMask2D
#ifdef UNITY_UI_CLIP_RECT
    mainColor.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
    clip (mainColor.a - 0.001);
#endif  

    return mainColor;
}

#endif //VFX_PASS_LOD1