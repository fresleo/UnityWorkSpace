#ifndef __TOONPBR_TRANSLUCENCY__
#define __TOONPBR_TRANSLUCENCY__

// SSS半透明
void ApplyTranslucency(inout half4 color,
    in InputDataToon inputData, in half3 BaseColor, in float3 Translucency)
{
    float shadow = _TransShadow;
    float normal = _TransNormal;
    float scattering = _TransScattering;
    float direct = _TransDirect;
    float ambient = _TransAmbient;
    float strength = _TransStrength;

    #define SUM_LIGHT_TRANSLUCENCY(Light) \
        float3 atten = Light.color * Light.distanceAttenuation; \
        atten = lerp( atten, atten * Light.shadowAttenuation, shadow ); \
        half3 lightDir = Light.direction + inputData.normalWS * normal; \
        half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering ); \
        half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency; \
        color.rgb += BaseColor * translucency * strength;

    Light mainLight = GetMainLight(inputData.shadowCoord);
    
    SUM_LIGHT_TRANSLUCENCY(mainLight);

    // 对多光源的支持
    #if defined(_ADDITIONAL_LIGHTS)
    uint meshRenderingLayers = GetMeshRenderingLayer();
    uint pixelLightCount = GetAdditionalLightsCount();
    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
        {
            SUM_LIGHT_TRANSLUCENCY( light );
        }
    }
    #endif
    LIGHT_LOOP_BEGIN( pixelLightCount )
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
    {
        SUM_LIGHT_TRANSLUCENCY( light );
    }
    LIGHT_LOOP_END
    #endif
}

#endif // __TOONPBR_TRANSLUCENCY__
