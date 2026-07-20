/*******************************************************************************
 * File: AHDSceneData.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD 烘焙场景中间数据。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    internal struct AHDMeshTriangle
    {
        public float3 world0;
        public float3 world1;
        public float3 world2;
        public float3 normal0;
        public float3 normal1;
        public float3 normal2;
        public float2 uv0;
        public float2 uv1;
        public float2 uv2;
        public int lightmapIndex;
        public int chartId;
        public int ownerId;
    }

    internal struct AHDOccluderTriangle
    {
        public float3 world0;
        public float3 world1;
        public float3 world2;
        public float3 boundsMin;
        public float3 boundsMax;
        public float3 centroid;
        public int ownerId;
    }

    internal struct AHDLightData
    {
        public int type;
        public float3 positionWS;
        public float3 directionToLightWS;
        public float3 rightWS;
        public float3 upWS;
        public float3 color;
        public float intensity;
        public float range;
        public float spotInnerCos;
        public float spotOuterCos;
        public float sourceRadius;
        public float2 areaSize;
        public int areaShape;
    }

    internal struct AHDTexelData
    {
        public int valid;
        public int chartId;
        public float3 positionWS;
        public float3 normalWS;
        public float luminanceMask;
        public int ownerId;

        // 跨三角形累加缓冲。光栅化阶段按每个覆盖三角形累加，ResolveTexels 解析后写入上面的最终字段。
        public float weightAccum;
        public float3 positionAccum;
        public float3 normalAccum;
        public float luminanceAccum;

        // ResolveTexels 计算出的跨三角形几何一致性（length(sum(normal*weight)) / sum(weight)）。
        // Job 末尾会用它乘到 strength 上，抑制 chart 接缝/UV 重叠区的高光。
        public float crossCoherence;
    }

    internal struct AHDTexelResult
    {
        public float3 directionWS;
        public float strength;
        public float visibleWeight;
        public float occlusionRatio;
        public float confidence;
        public int dominantLightIndex;
    }

    internal sealed class AHDSceneData
    {
        public readonly List<AHDMeshTriangle> receivers = new ();
        public readonly List<AHDOccluderTriangle> occluders = new ();
        public readonly List<AHDLightData> lights = new ();
    }

    internal struct AHDSceneSummary
    {
        public int lightmapCount;
        public int lightCount;
        public int tagExcludedLightCount;
        public int receiverRendererCount;
        public int occluderRendererCount;
        public int receiverTriangleCount;
        public int occluderTriangleCount;
    }

    internal sealed class AHDLightmapWorkset
    {
        public int lightmapIndex;
        public int width;
        public int height;
        public AHDTexelData[] texels;
        public AHDTexelResult[] results;
        public bool[] validMask;
        public Color[] pixels;
        public Color[] strengthDebug;
        public Color[] visibilityDebug;
        public Color[] confidenceDebug;
        public Color[] luminanceDebug;
        public Color[] dominantDebug;
        public Color[] transitionScoreDebug;
        public Color[] directionDiffDebug;
        public Color[] featherWeightDebug;
        public Color[] featherMaskDebug;
        public Color[] featherDeltaDebug;
        public int validTexelCount;
    }
}
