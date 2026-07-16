#ifndef __TOONPBR_TRANSMISSION__
#define __TOONPBR_TRANSMISSION__

// 光传输
void ApplyTransmission(inout half4 color, 
    in InputDataToon inputData, in half3 BaseColor, in float3 Transmission)
{
    float shadow = _TransmissionShadow;

    #define SUM_LIGHT_TRANSMISSION(Light) \
        float3 atten = Light.color * Light.distanceAttenuation; \
        atten = lerp( atten, atten * Light.shadowAttenuation, shadow ); \
        half3 transmission = max( 0, -dot( inputData.normalWS, Light.direction ) ) * atten * Transmission; \
        color.rgb += BaseColor * transmission;

    Light mainLight = GetMainLight(inputData.shadowCoord);
    
    SUM_LIGHT_TRANSMISSION(mainLight);

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
            SUM_LIGHT_TRANSMISSION( light );
        }
    }
    #endif
    LIGHT_LOOP_BEGIN( pixelLightCount )
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
    {
        SUM_LIGHT_TRANSMISSION( light );
    }
    LIGHT_LOOP_END
    #endif
}

#endif // __TOONPBR_TRANSMISSION__
