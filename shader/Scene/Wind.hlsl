#ifndef __WIND__
#define __WIND__

#include "WindCommon.hlsl"

uniform float4 g_SmoothTime;
uniform float4 g_PrevSmoothTime;
uniform float3 g_WindDirection;
uniform float4 g_WindOffset;
uniform float2 g_Wind;
uniform float2 g_Turbulence;
uniform sampler2D g_GustNoise;

float3 GetWindDirection()
{
    return normalize(float3(g_WindDirection.x, 0, g_WindDirection.z));
}

float GetWindSpeed()
{
    return g_Wind.x;
}

float GetWindStrength(float localWindStrength)
{
    return g_Wind.y * localWindStrength;
}

float GetWindVariation( float3 objectPivot, float localWindVariation)
{
    return 1.0 - frac( objectPivot.x * objectPivot.z * 10.0 ) * localWindVariation;
}

float GetSmoothAmbientOffset()
{
    return g_SmoothTime.x;
}

float GetSmoothTurbulenceOffset()
{
    return g_SmoothTime.z;
}

float2 GetSmoothGustOffset()
{
    return g_WindOffset.xy;
}

float GetTurbulenceStrength(float localTurbulenceStrength)
{
    return g_Turbulence.y * localTurbulenceStrength;
}

float2 GetTrunkBendFactor(float4 localTrunkBendFactor)
{
    return localTrunkBendFactor.xy;
}
            
float GetTrunkMask(float3 vertex, float2 uv1, float bendFactor, float baseBendFactor )
{
    float trunkMask = saturate( uv1.x * bendFactor );
    return saturate( trunkMask + saturate( vertex.y ) * baseBendFactor );
}

float4 AmbientFrequency( float3 objectPivot, float3 vertexWorldPosition, float3 windDirection, float phaseOffset, float speed, float time )
{
    float footprint = 3;
    time -= phaseOffset * footprint;
                
    float pivotOffset = length( float3(objectPivot.x, 0, objectPivot.z) );
                
    float scale = 0.5;
    float frequency = pivotOffset * scale - time;
    return FastSin( float4( frequency, frequency*0.5, frequency*0.25, frequency*0.125) * speed );
}

float3 AmbientWind( float3 objectPivot, float3 vertexWorldPosition, float3 windDirection, float phaseOffset, float time )
{
    float4 sine = AmbientFrequency( objectPivot, vertexWorldPosition, windDirection, phaseOffset, 1, time );
    sine.w = abs(sine.w) + 0.5;
    float xz = 1.5 * sine.x * sine.z + sine.w + 1;
    float y = 1 * sine.y * sine.z + sine.w;
    return windDirection * float3(xz, 0, xz) + float3(0, y, 0);
}

float3 Turbulence( float3 objectPivot, float3 vertexWorldPosition, float3 worldNormal, float phaseOffset, float edgeFlutter, float speed, float time )
{
#if defined(_TYPE_TREE_BARK_ON)
    return float3(0, 0, 0);
#else
    time -= phaseOffset;
    float frequency = ( objectPivot.x + objectPivot.y + objectPivot.z ) * 2.5 - time;
                    
    float4 sine = FastSin( float4( (1.65 * frequency) * speed, (2 * 1.65 * frequency) * speed, 0, 0) );
                    
    float x = 1 * sine.x + 1;
    float z = 1 * sine.y + 1;
    float y = (x + z) * 0.5;
                    
#if defined(_TYPE_TREE_LEAVES_ON)
    return worldNormal * float3(x, y, z) * float3(1, .6, 1) * edgeFlutter;
#else
    return worldNormal * float3(x, y, z) * float3(1, 0.35, 1);
#endif
    
#endif
}

float3 SampleGust( float3 objectPivot, float3 vertexWorldPosition, float3 windDirection, float phaseOffset, float edgeFlutter, float lod, float2 windOffset)
{
#if defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
    windOffset -= phaseOffset.xx * 0.05;
    lod = 5;
#else
    windOffset -= phaseOffset.xx * 0.05;
#endif
                
#if defined(_TYPE_TREE_LEAVES_ON)
    float3 vertexOffset = vertexWorldPosition - objectPivot;
    float2 offset = (objectPivot.xz) * 0.02 - windOffset.xy + vertexOffset.xz * 0.0075 * edgeFlutter;
#else
    float2 offset = (objectPivot.xz) * 0.02 - windOffset.xy;
#endif
    float strength = tex2Dlod( g_GustNoise, float4(offset, 0, lod) ).r;
    return strength * windDirection;
}

float3 CombineWind( float3 ambient, float3 gust, float3 turbulence, float3 shiver, float4 strength )
{
    ambient *= strength.x;
    gust *= strength.y;
    turbulence *= strength.z;
    shiver *= strength.w;
                
    // Trees require more displacement for the wind to be visible because the objects are larger.
    // These are magic numbers that give a nice balance between the grass/plants and trees,
    // based on a common tree size.
#if defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
    ambient *= 3;
    gust *= 1;
    turbulence *= 3;
    shiver *= 3;
#endif
                
    float gustLength = length( gust );
    float increaseTurbelenceWithGust = smoothstep(0, 1, gustLength) + 1;
                
    // Calculate the balance between different wind types.
    // If we do it here then we can keep the input parameters in a 0-1 range.
    ambient *= 0.1;
    gust *= 1.5;
    turbulence *= 0.15;
    shiver *= 0.15;

    return ambient + gust + lerp( turbulence * increaseTurbelenceWithGust, shiver * increaseTurbelenceWithGust, gustLength);
}

float3 ComputeWind( WindInput input, float3 positionWS, float windStrength, float turbulenceStrength)
{
#if defined(_TYPE_GRASS_ON) || defined(_TYPE_PLANT_ON)
    input.phaseOffset += dot( input.direction, (positionWS - input.objectPivot) );
    input.phaseOffset += input.mask * 0.3;
#endif
                
    float3 ambient = AmbientWind( input.objectPivot, positionWS, input.direction, input.phaseOffset, GetSmoothAmbientOffset() );
    float3 gust =  SampleGust( input.objectPivot, positionWS, input.direction, input.phaseOffset, input.flutter, 0, GetSmoothGustOffset() );
                
    // Add a bit of a random phase offset to the tree leaves. Phase Offset is calculated
    // per-branch and we don't want to have the same turbulence for the entire branch.
#if defined(_TYPE_TREE_LEAVES_ON)
    input.phaseOffset += dot( input.direction, (positionWS - input.objectPivot) ) * input.flutter;
#endif
                
    float3 turbulence1 = Turbulence( input.objectPivot.xyz, positionWS.xyz, input.normalWS.xyz, input.phaseOffset, input.flutter, 1, GetSmoothTurbulenceOffset(  ) );
    float3 turbulence2 = Turbulence( input.objectPivot.xyz, positionWS.xyz, input.normalWS.xyz, input.phaseOffset, input.flutter, 2, GetSmoothTurbulenceOffset(  ) );
    return CombineWind( ambient, gust, turbulence1, turbulence2, float4(windStrength.xx, turbulenceStrength.xx) );
}

float3 ApplyWind( float3 positionWS, float3 objectPivot, float3 combinedWind, float mask)
{
#if defined(_TYPE_GRASS_ON)
    return FixStretching( positionWS + combinedWind * mask, positionWS, float3( positionWS.x, objectPivot.y, positionWS.z ) ); // TODO: This does not work correctly if the grass is a larger patch and it is rotated. Ideally we would use vertexOS.y transformed into world space instead of objectPivot.y.
#elif defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
    return FixStretching( positionWS + combinedWind * (mask * 4), positionWS, objectPivot);
#else
    return FixStretching( positionWS + combinedWind * (mask * mask), positionWS, objectPivot);
#endif
}

void Wind( WindInput input, inout float3 positionWS, inout float3 normalWS, float localWindStrength, float localTurbulenceStrength)
{
    // Adjust the pivot for grass to use the XZ position of the vertex.
    // This is a decent workaround to get a per-grass-blade pivot until
    // we have proper pivot support.
#ifdef _TYPE_GRASS_ON
    input.objectPivot = float3(positionWS.x, input.objectPivot.y, positionWS.z);
#endif
                
    // Compute wind.
    float3 wind = ComputeWind( input, positionWS, GetWindStrength(localWindStrength), GetTurbulenceStrength(localTurbulenceStrength));
                
    // Apply wind to vertex.
    float3 outputWS = ApplyWind( positionWS, input.objectPivot, wind, input.mask);

    // TODO 看看有没有提升
    // Recalculate normals for grass
#if defined(_TYPE_GRASS_ON)
    float3 delta = outputWS - positionWS;
    normalWS = lerp( normalWS, normalWS + SafeNormalize( delta + float3(0, 0.1, 0) ), length(delta) * 1);
#endif
                
    positionWS = outputWS;
}

void Wind_Trunk( float3 vertex, float3 vertexWorldPosition, float3 vertexWithWind, float2 uv1, float3 objectPivot, float3 windDirection, float4 localTrunkBendFactor, float localWindStrength, out float3 vertexOut )
{
    // Additional properties. Either global or baked.
    float2 bendFactor = GetTrunkBendFactor(localTrunkBendFactor);
    float trunkMask = GetTrunkMask( vertex, uv1, bendFactor.x, bendFactor.y );
    float ambientStrength = GetWindStrength(localWindStrength);
    
    // Calculate Ambient Wind
    float4 trunkAmbient = AmbientFrequency( objectPivot, vertexWorldPosition, windDirection, 0, 0.75, GetSmoothAmbientOffset(  ) ) + ambientStrength;
    trunkAmbient *= trunkMask;
    
    // Calculate Gust
    float3 trunkGust = SampleGust( objectPivot, vertexWorldPosition, windDirection, 0, 0, 7, GetSmoothGustOffset(  ));
    trunkGust *= trunkMask;
    
    // Apply
    float gustFrequency = trunkAmbient.w * length(trunkGust);
    float baseFrequency1 = trunkAmbient.x;
    float baseFrequency2 = trunkAmbient.x + trunkAmbient.y;
    float baseFrequency = lerp( baseFrequency1, baseFrequency2, (_SinTime.x + 1) * 0.5 * ambientStrength);
    
    // TODO: Use the "FixStretching" approach?
    vertexOut = RotateAroundAxis( objectPivot, vertexWithWind, normalize( cross( float3(0,1,0) , windDirection ) ),
            (baseFrequency * 0.75 + gustFrequency) * ambientStrength * 0.0375);
}

void Wind(VertexAttributes vertex, inout SurfaceInput surface, inout float3 positionWS, float localWindStrength, float localWindVariation, float localTurbulenceStrength, float4 localTrunkBendFactor = 1)
{
    float3 objectPivot = GetObjectPivot();
    float heightMask = GetHeightMask(vertex.color, vertex.uv1.xy );
    float phaseOffset = GetPhaseOffset( vertex.color, positionWS, objectPivot );
    
    WindInput input;
                
    // Global
    input.direction = GetWindDirection();
    input.speed = GetWindSpeed();
                
    // Per-Object
    input.objectPivot = objectPivot;
                
    // Per-Vertex
    input.phaseOffset = phaseOffset;
    input.normalWS = surface.normalWS;
    float windVariation = GetWindVariation( input.objectPivot, localWindVariation);
    float vertexMask = GetVertexMask( vertex.color );
    input.mask = heightMask * vertexMask * windVariation;
    input.flutter = GetEdgeFlutter( vertex.color );
                
    float3 vertexOut = positionWS;
    float3 normalOut = surface.normalWS;
    Wind( input, vertexOut, normalOut, localWindStrength, localTurbulenceStrength);
                
#if defined(_TYPE_TREE_LEAVES_ON) || defined( _TYPE_TREE_BARK_ON )
    Wind_Trunk( vertex.positionOS.xyz, positionWS.xyz, vertexOut.xyz, vertex.uv1.xy, input.objectPivot, input.direction, localTrunkBendFactor, localWindStrength, vertexOut);
#endif
                
    positionWS = vertexOut;
    surface.normalWS = normalOut;
}

void Wind(float2 uv2, inout float3 normalWS, inout float3 positionWS, float localWindStrength, float localWindVariation, float localTurbulenceStrength, float4 localTrunkBendFactor = 1)
{
    float3 objectPivot = GetObjectPivot();
    float heightMask = uv2.x;
    float phaseOffset = 0;
    
    WindInput input;
                
    // Global
    input.direction = GetWindDirection();
    input.speed = GetWindSpeed();
                
    // Per-Object
    input.objectPivot = objectPivot;
                
    // Per-Vertex
    input.phaseOffset = phaseOffset;
    input.normalWS = normalWS;
    float windVariation = GetWindVariation( input.objectPivot, localWindVariation);
    input.mask = heightMask * windVariation;
    input.flutter = 0;
                
    float3 vertexOut = positionWS;
    float3 normalOut = normalWS;
    Wind( input, vertexOut, normalOut, localWindStrength, localTurbulenceStrength);
                
#if defined(_TYPE_TREE_LEAVES_ON) || defined( _TYPE_TREE_BARK_ON )
    Wind_Trunk( vertex.positionOS.xyz, positionWS.xyz, vertexOut.xyz, vertex.uv1.xy, input.objectPivot, input.direction, localTrunkBendFactor, localWindStrength, vertexOut);
#endif
                
    positionWS = vertexOut;
}

#endif // __WIND__
