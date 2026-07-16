#if !defined(VFX_PASS_HLSL)
#define VFX_PASS_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MaterialVolume.hlsl"
#include "VFXCore.hlsl"

v2f vert(appdata v)
{
    v2f o = (v2f)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
    float3 worldNormal = normalize(TransformObjectToWorldNormal(v.normal));
    
#ifdef _VERTEXWAVEON_ON //if(_VertexWaveOn)
{
    float attemMaskCustomData = v.uv1.z;
    ApplyVertexWaveWorldSpace(worldPos.xyz, worldNormal, v.color.rgb, v.uv, attemMaskCustomData, v.uv2.y);
}
#endif

    if(_FresnelOn || _FresnelRevertOn || _CameraDistanceAngleFadeOn)//#if defined(_FRESNELON_ON) || defined(_FRESNELREVERTON_ON)
    {
        worldPos.xyz += worldNormal * _OffsetVertexByNormal;
    }

    o.vertex = TransformWorldToHClip(worldPos.xyz);

    o.uv = v.uv; // uv.xy : main uv, zw : custom data.xy
    o.grabPos = ComputeScreenPos(o.vertex);
    o.grabPos.z = -TransformWorldToView(worldPos.xyz).z;
    o.color = v.color;

    
    if(_FullScreenOn)
    {
        float2 remap = o.uv.xy * 2.0f - 1.0f;
        v.vertex.xyz = float3(remap, 0);
#if UNITY_UV_STARTS_AT_TOP
        v.vertex.y = -v.vertex.y;
#endif

#if UNITY_REVERSED_Z
        v.vertex.z = 1;
#endif
        
        o.vertex = v.vertex;
    }

    if(_FresnelOn || _FresnelRevertOn || _CameraDistanceAngleFadeOn)//#if defined(_FRESNELON_ON) || defined(_FRESNELREVERTON_ON)
    {
        float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);
        o.fresnel_customDataZ.x = 1.0f - dot(worldNormal, normalize(viewDir + _OffsetFresnel.xyz));
    }
    //#endif

    // particle custom data (Custom1).zw
    o.fresnel_customDataZ.yz = v.uv1.xy;

    // fresnel alpha, custom2.w
    o.fresnel_customDataZ.w = v.uv2.y;


#ifdef _LIGHTON_ON
    // if(_LightOn)
    {
        TANGENT_SPACE_COMBINE(v.vertex, v.normal, v.tangent, o);
    }
#endif

#ifdef _DECALEFFECTON_ON
    o.center = v.uv2;
    o.size = v.uv3;
#endif

    o.positionWS = worldPos.xyz;
    UBPA_TRANSFER_FOG(o, o.positionWS);
    
    return o;
}

half4 frag(v2f i, half faceId : VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // 开启光照是开启视差的前置条件
    #ifdef _LIGHTON_ON
    TANGENT_SPACE_SPLIT(i);
    if(_ParallaxOn)
    {
        float3 viewDirWS = normalize(GetWorldSpaceViewDir(i.positionWS)); // 世界空间-视角方向
        
        // float3 viewDirTS = TangentViewDir(i.tSpace0, i.tSpace1, i.tSpace2, viewDirWS); // 切线空间-视角方向
        float3 viewDirTS = mul(float3x3(tangent, binormal, normal), viewDirWS); // 切线空间-视角方向
        
        float cosViewAngle = saturate(dot(float3(0,1,0),  viewDirWS));
        float2 parallaxUV = CalculateParallaxOffset(_ParallaxTex, i.uv.xy, viewDirTS, -_Parallax, cosViewAngle);
        
        // UV 滚动
        float2 parallaxTexOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _ParallaxTexOffsetStop);
        float2 parallaxTexOffset = (_ParallaxTex_ST.zw * parallaxTexOffsetScale);
        
        i.uv.xy = parallaxUV * _ParallaxTex_ST.xy + parallaxTexOffset;
    }
    #endif
    
    half4 mainColor = float4(0,0,0,1);
    float4 uvTextures = i.uv;                       // xy:默认uv, zw:粒子自定义数据
    float4 mainUV = i.uv;                           // xy:主纹理uv, zw:无偏移缩放的主纹理uv
    float2 screenUV = i.grabPos.xy / i.grabPos.w;   // 屏幕空间uv

    // 是否使用屏幕空间uv
    if(_MainTexUseScreenUV) 
    {
        mainUV.xy = screenUV;
    }
    // 粒子系统控制主纹理偏移
    mainUV = MainTexOffset(mainUV);
    // 是否只让主纹理使用屏幕空间uv
    if(!_OnlyMainTexUseScreenUV) 
    {
        uvTextures.xy = mainUV.zw;
    }

    float fresnel = i.fresnel_customDataZ.x;
    float dissolveCustomData = i.fresnel_customDataZ.y;
    float dissolveEdgeWidth = i.fresnel_customDataZ.z;
    float fresnelAlpha = lerp(1.0f, i.fresnel_customDataZ.w, _FresnelUseCustomData2W);
    
    // Decal贴花
    #ifdef _DECALEFFECTON_ON //if(_DecalEffectOn)
    {
        ApplyParticleDecal(mainColor.a, mainUV, i);
        mainUV.xy = Unity_TilingAndOffset(mainUV.xy, _MainTex_ST);
    }
    #endif
    
    // 主纹理旋转
    #ifdef _MAINTEXTUREROTATIONUVON_ON
    {
        mainUV.xy = MainTexRotation(mainUV.xy, _MainTexRotationCenter.xy, _MainTexRotationAngle / 360 * 6.283185307);
    }
    #endif
    
    float2 finalUV = mainUV.xy;
    float2 polarUV = PolarUV(mainUV.zw);
    
    float2 uvDistorted = 0;
    #ifdef _DISTORTIONON_ON //if(_DistortionOn)
    {
        float2 distortUV = uvTextures.xy;
        
        #ifdef _DISTORTIONRADIALUVON_ON //if(_DistortionRadialUVOn)
        {
            distortUV = polarUV;
        }
        #endif

        distortUV = distortUV * _DistortTile.xy + frac(_DistortDir.xy * GET_GLOBAL_TIME.xx);
        
        #ifdef _MAINTEXTURERADIALUVON_ON //if(_MainTextureRadialUVOn)
        {
            mainUV.xy = polarUV * _MainTex_ST.xy + (_MainTexOffsetUseCustomData_XY > 0.0f ? uvTextures.zw : _MainTex_ST.zw * lerp(GET_GLOBAL_TIME.xx, 1, _MainTexOffsetStop));
        }
        #endif
        
        uvDistorted = ApplyDistortion(mainUV, distortUV);
        if(_DistortionAffectMainTexture)
        {
            finalUV = mainUV.xy + uvDistorted;
        }
    }
    #else//else
    {
        #ifdef _MAINTEXTURERADIALUVON_ON //if(_MainTextureRadialUVOn)
        {
            finalUV = MainTexOffset(polarUV, uvTextures.zw);
        }
        #endif
    }
    #endif
    
    mainColor = SampleMainTex(finalUV, i, faceId, uvDistorted);

    ApplyMainTexBlend(mainColor, uvTextures.xy);
    
    ApplyMainTexMask(mainColor, uvTextures.xy, uvDistorted);
    
    #ifdef _OFFSETON_ON //if(_OffsetOn)
    {
        float2 offsetUV = uvTextures.xy;
        // 流光的极坐标
        UNITY_BRANCH
        if(_OffsetRadialUVOn)
        {
            offsetUV = polarUV;
        }
        // 流光扰动
        offsetUV += (_DistortionApplyToOffset ? uvDistorted.xy : 0);

        half2 timeScale = lerp(GET_GLOBAL_TIME.xx, half2(1, 1), _OffsetDirTimeInvariant);
        offsetUV = offsetUV * _OffsetTile + timeScale * _OffsetDir;

        float2 maskUV = Unity_TilingAndOffset(uvTextures.xy, _OffsetMaskTex_ST);
        
        ApplyOffset(mainColor, offsetUV, maskUV);
    }
    #endif
    
    //dissolve
    #ifdef _DISSOLVEON_ON //if(_DissolveOn)
    {
        // 溶解图 UV
        float2 dissolveOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveTexOffsetStop);
        float2 dissolveOffset = _DissolveTex_ST.zw * dissolveOffsetScale;
        
        float2 dissolveUV = uvTextures.xy + (_DistortionApplyToDissolve ? uvDistorted.xy : 0);
        dissolveUV = dissolveUV * _DissolveTex_ST.xy + dissolveOffset;
        
        // 溶解方向图 UV
        float2 dissolveDirectionOffsetScale = lerp(GET_GLOBAL_TIME.xx, 1, _DissolveDirectionTexOffsetStop);
        float2 dissolveDirectionOffset = _DissolveDirectionTex_ST.zw * dissolveDirectionOffsetScale;
        
        float2 dissolveDirectionUV = uvTextures.xy * _DissolveDirectionTex_ST.xy + dissolveDirectionOffset;
        
        ApplyDissolve(mainColor, dissolveUV, dissolveDirectionUV, i.color, dissolveCustomData, dissolveEdgeWidth);
    }
    #endif
    
    if(_FresnelOn) //#ifdef _FRESNELON_ON
    {
        ApplyFresnel(mainColor, fresnel, fresnelAlpha);
    }
    //#endif
    
    if(_FresnelRevertOn) //#ifdef _FRESNELREVERTON_ON
    {
        ApplyFresnelRevert(mainColor, fresnel);
    }
    //#endif
    if(_FresnelRevertAlpha)
    {
        ApplyFresnelAlpha(mainColor, fresnel);
    }
    if(_VertexColorCondition){
        ApplyFresnelConditionWithVertexColor(mainColor, i.color);
    }
    #ifdef _LIGHTON_ON //if(_LightOn)
    {
        ApplyLightWithNormalMap(mainColor, i);
    }
    #endif
    
    #ifdef _DEPTHFADINGON_ON //if(_DepthFadingOn)
    {
        ApplySoftParticle(mainColor, i.grabPos);
    }
    #endif
    
    mainColor.rgb = BlendVolumeColor(i.positionWS, mainColor.rgb);
    
    // 因为 ui 有 gamma 矫正，而 spine 也是当 ui 绘制的，所以也需要统一处理一下颜色的 gamma
    mainColor.rgb = lerp(mainColor.rgb, LinearToSRGB(mainColor.rgb), _IsGammaUI);
    
    //摄像机过渡功能
    if (_CameraDistanceAngleFadeOn)
    {
        float angleFadeFactor;
        ApplyViewAngleFade(angleFadeFactor, fresnel, _ViewAngleFading);
        mainColor.a = mainColor.a * angleFadeFactor;
        
        float distanceToCamera = distance(i.positionWS.xyz, _WorldSpaceCameraPos);
        float fadeDistance = saturate(distanceToCamera / _CameraDistanceFading);
        mainColor.a = saturate(mainColor.a) * _Alpha * fadeDistance;
    }
    else
    {
        mainColor.a = saturate(mainColor.a) * _Alpha;
    }
    
    UBPA_APPLY_FOG(i, mainColor);
    
    return mainColor;
}

#endif //VFX_PASS_HLSL