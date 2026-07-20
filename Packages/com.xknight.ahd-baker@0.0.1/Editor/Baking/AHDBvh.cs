/*******************************************************************************
 * File: AHDBvh.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD CPU 烘焙用 BVH 构建。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace XKnight.AHDBaker.Editor
{
    internal struct AHDBvhNode
    {
        public float3 boundsMin;
        public float3 boundsMax;
        public int left;
        public int right;
        public int start;
        public int count;
    }

    internal sealed class AHDBvhBuildResult
    {
        public AHDBvhNode[] nodes = Array.Empty<AHDBvhNode>();
        public int[] triangleIndices = Array.Empty<int>();
    }

    internal static class AHDBvh
    {
        private const int c_LeafTriangleCount = 4;

        public static AHDBvhBuildResult Build(List<AHDOccluderTriangle> triangles)
        {
            AHDBvhBuildResult result = new AHDBvhBuildResult();
            if (triangles == null || triangles.Count == 0)
            {
                return result;
            }

            List<AHDBvhNode> nodes = new List<AHDBvhNode>();
            int[] indices = new int[triangles.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            BuildNode(triangles, indices, 0, indices.Length, nodes);
            result.nodes = nodes.ToArray();
            result.triangleIndices = indices;
            return result;
        }

        private static int BuildNode(
            List<AHDOccluderTriangle> triangles, int[] indices,
            int start, int count,
            List<AHDBvhNode> nodes)
        {
            int nodeIndex = nodes.Count;
            AHDBvhNode node = CreateBoundsNode(triangles, indices, start, count);
            nodes.Add(node);

            if (count <= c_LeafTriangleCount)
            {
                node.left = -1;
                node.right = -1;
                node.start = start;
                node.count = count;
                nodes[nodeIndex] = node;
                return nodeIndex;
            }

            int axis = GetLargestAxis(node.boundsMax - node.boundsMin);
            Array.Sort(indices, start, count, new CentroidComparer(triangles, axis));
            
            int leftCount = count / 2;
            int rightCount = count - leftCount;
            int left = BuildNode(triangles, indices, start, leftCount, nodes);
            int right = BuildNode(triangles, indices, start + leftCount, rightCount, nodes);
            node.left = left;
            node.right = right;
            node.start = -1;
            node.count = 0;
            nodes[nodeIndex] = node;
            
            return nodeIndex;
        }

        private static AHDBvhNode CreateBoundsNode(
            List<AHDOccluderTriangle> triangles, int[] indices,
            int start, int count)
        {
            float3 boundsMin = new float3(float.PositiveInfinity);
            float3 boundsMax = new float3(float.NegativeInfinity);
            for (int i = start; i < start + count; i++)
            {
                AHDOccluderTriangle triangle = triangles[indices[i]];
                boundsMin = math.min(boundsMin, triangle.boundsMin);
                boundsMax = math.max(boundsMax, triangle.boundsMax);
            }

            return new AHDBvhNode
            {
                boundsMin = boundsMin,
                boundsMax = boundsMax,
                left = -1,
                right = -1,
                start = start,
                count = count
            };
        }

        private static int GetLargestAxis(float3 extent)
        {
            if (extent.x >= extent.y && extent.x >= extent.z)
            {
                return 0;
            }

            return extent.y >= extent.z ? 1 : 2;
        }

        private sealed class CentroidComparer : IComparer<int>
        {
            private readonly List<AHDOccluderTriangle> _triangles;
            private readonly int _axis;

            public CentroidComparer(List<AHDOccluderTriangle> triangles, int axis)
            {
                _triangles = triangles;
                _axis = axis;
            }

            public int Compare(int x, int y)
            {
                float left = GetAxis(_triangles[x].centroid, _axis);
                float right = GetAxis(_triangles[y].centroid, _axis);
                return left.CompareTo(right);
            }

            private static float GetAxis(float3 value, int axis)
            {
                if (axis == 0)
                {
                    return value.x;
                }

                return axis == 1 ? value.y : value.z;
            }
        }
    }
}
