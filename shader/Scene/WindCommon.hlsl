#ifndef __WIND_COMMON__
#define __WIND_COMMON__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct VertexAttributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    half4 color : COLOR;
    
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SurfaceInput
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    float4 shadowCoord : TEXCOORD0;
    #endif
    
    UBPA_FOG_COORDS(1)
    
    float4 positionSS : TEXCOORD2;
    float3 positionWS : TEXCOORD3;
    float3 positionVS :  TEXCOORD4;
    float4 positionNDC : TEXCOORD5;

    float3 normalWS : TEXCOORD6;

    float4 uv0 : TEXCOORD7;
    float4 uv1 : TEXCOORD8;
    
    float objectId : TEXCOORD9;
    half shaderLod : TEXCOORD10;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WindInput
{
    // Global
    float3 direction;
    float speed;
    
    // Per-Object
    float3 objectPivot;
    
    // Per-Vertex
    float phaseOffset;
    float3 normalWS;
    float mask;
    float flutter;
};

float4 TransformHClipToViewPortPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o / o.w;
}

float Remap( float value, float2 remap )
{
    return remap.x + value * (remap.y - remap.x);
}

float4 SmoothCurve( float4 x )
{
    return x * x *( 3.0 - 2.0 * x );
}

float4 TriangleWave( float4 x )
{
    return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}

float4 SmoothTriangleWave( float4 x )
{
    return SmoothCurve( TriangleWave( x ) );
}

float4 FastSin( float4 x )
{
    #ifndef PI
    #define PI 3.14159265
    #endif
    #define DIVIDE_BY_PI 1.0 / (2.0 * PI)
    return (SmoothTriangleWave( x * DIVIDE_BY_PI ) - 0.5) * 2;
}
            
float3 FixStretching( float3 vertex, float3 original, float3 center )
{
    return center + SafeNormalize(vertex - center) * length(original - center);
}

// u是轴
float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
{
    original -= center;
    float C = cos( angle );
    float S = sin( angle );
    float t = 1 - C;
    float m00 = t * u.x * u.x + C;
    float m01 = t * u.x * u.y - S * u.z;
    float m02 = t * u.x * u.z + S * u.y;
    float m10 = t * u.x * u.y + S * u.z;
    float m11 = t * u.y * u.y + C;
    float m12 = t * u.y * u.z - S * u.x;
    float m20 = t * u.x * u.z - S * u.y;
    float m21 = t * u.y * u.z + S * u.x;
    float m22 = t * u.z * u.z + C;
    float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
    return mul( finalMatrix, original ) + center;
}

float3 GetObjectPivot()
{
    return float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
}

float GetVertexMask( float4 vertexColor )
{
    return 1.0f;
    
    #if defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
        return 1.0;
    #else
        return vertexColor.r;
    #endif
}

float GetPhaseOffset( float4 vertexColor, float3 vertexWorldPosition, float3 objectPivot )
{
    return 0.0f;
    
    #if defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
        return vertexColor.r;
    #else
        return 1.0 - vertexColor.g;
    #endif
}

float GetEdgeFlutter( float4 vertexColor )
{
    return 0.0f;
    
    #if defined(_TYPE_TREE_BARK_ON)
        return 0;
    #else
        #if defined(_TYPE_TREE_LEAVES_ON)
            return vertexColor.g;
        #else
            return 1;
        #endif
    #endif
}

float GetHeightMask(float4 vertexColor, float2 uv1)
{
    #if defined(_TYPE_TREE_LEAVES_ON) || defined(_TYPE_TREE_BARK_ON)
    return uv1.y;
    #else
    //return vertexColor.a;
    return uv1.x;
    #endif
}

#endif // __WIND_COMMON__
