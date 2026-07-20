#if UNITY_EDITOR
// 如果定义了这个宏，则测量 BuildFromSkinMeshRenderer 方法的用时。
// 它是为调试而定义的。
// #define MEASUREMENT_METHOD_BuildFromSkinMeshRenderer
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace AirSticker.Runtime.Core
{
    /// <summary>
    /// 从网格渲染器或蒙皮网格渲染器创建三角形多边形信息。
    /// </summary>
    public class TrianglePolygonsFactory
    {
        private static readonly int VertexCountOfTrianglePolygon = 3;
        private static readonly int MaxWorkingVertexCountForTerrain = 128 * 128; // 16,384
        
        private static readonly int MaxWorkingVertexCount = 65536;
        private static readonly int MaxWorkingTriangleCount = 65536;
        private readonly List<BoneWeight> _workingBoneWeights = new(MaxWorkingVertexCount);
        private readonly List<int> _workingTrianglesForCalcPolygonCount = new(MaxWorkingTriangleCount);

        private NativeArray<int> _workingTriangles = new(MaxWorkingTriangleCount, Allocator.Persistent);
        private NativeArray<Vector3> _workingVertexNormals = new(MaxWorkingVertexCount, Allocator.Persistent);
        private NativeArray<Vector3> _workingVertexPositions = new(MaxWorkingVertexCount, Allocator.Persistent);

        private bool _disposed;
        public static int MaxGeneratedPolygonPerFrame { get; set; } = 100000;
#if MEASUREMENT_METHOD_BuildFromSkinMeshRenderer
        public static float[] Time_BuildFromSkinMeshRenderer { get; set; } = new float[3];
#endif
        
        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _workingBoneWeights.Clear();
            _workingTrianglesForCalcPolygonCount.Clear();

            _workingVertexPositions.Dispose();
            _workingVertexNormals.Dispose();
            _workingTriangles.Dispose();

            _disposed = true;
        }

        /// <summary>
        /// 从接收对象构建
        /// </summary>
        internal IEnumerator BuildFromReceiverObject(
            MeshFilter[] meshFilters, MeshRenderer[] meshRenderers,
            SkinnedMeshRenderer[] skinnedMeshRenderers,
            Terrain[] terrains,
            List<ConvexPolygonInfo> trianglePolygonInfos)
        {
            int listCapacity = 0;
            
            if (meshFilters != null)
            {
                listCapacity += GetNumPolygonsFromMeshFilters(meshFilters);
            }

            if (skinnedMeshRenderers != null)
            {
                listCapacity += GetNumPolygonsFromSkinModelRenderers(skinnedMeshRenderers);
            }

            float terrainMehResolutionScale = 0;
            if (terrains != null)
            {
                int numTerrainMeshPolygon = GetNumPolygonsFromTerrains(terrains, 1.0f);
                terrainMehResolutionScale = Mathf.Sqrt(MaxWorkingVertexCountForTerrain / (float)numTerrainMeshPolygon);
                listCapacity += GetNumPolygonsFromTerrains(terrains, terrainMehResolutionScale);
            }
            
            if (listCapacity > 0)
            {
                trianglePolygonInfos.Capacity = listCapacity;
            }

            if (meshFilters != null && meshRenderers != null)
            {
                yield return BuildFromMeshFilter(meshFilters, meshRenderers, trianglePolygonInfos);
            }
            if (skinnedMeshRenderers != null)
            {
                yield return BuildFromSkinMeshRenderer(skinnedMeshRenderers, trianglePolygonInfos);
            }
            if (terrains != null)
            {
                yield return BuildFromTerrain(terrains, terrainMehResolutionScale, trianglePolygonInfos);
            }
            yield return null;
        }

        private static int GetNumPolygonsFromSkinModelRenderers(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            int numPolygon = 0;

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (!renderer || renderer.sharedMesh == null)
                {
                    return -1;
                }
                if (!renderer.sharedMesh.isReadable)
                {
                    return -1;
                }
                
                var mesh = renderer.sharedMesh;
                numPolygon += mesh.triangles.Length / 3;
            }

            return numPolygon;
        }

        private static int GetNumPolygonsFromTerrain(Terrain terrain, float terrainMehResolutionScale)
        {
            var terrainData = terrain.terrainData;
            int vertexCountX = (int)(terrainData.heightmapResolution * terrainMehResolutionScale);
            int vertexCountY = (int)(terrainData.heightmapResolution * terrainMehResolutionScale);
            return (vertexCountX - 1) * (vertexCountY - 1) * 2;
        }

        private static int GetNumPolygonsFromTerrains(Terrain[] terrains, float terrainMehResolutionScale)
        {
            int numPolygon = 0;
            
            foreach (var terrain in terrains)
            {
                numPolygon += GetNumPolygonsFromTerrain(terrain, terrainMehResolutionScale);
            }
            
            return numPolygon;
        }

        private static int GetNumPolygonsFromMeshFilters(MeshFilter[] meshFilters)
        {
            int numPolygon = 0;

            foreach (var meshFilter in meshFilters)
            {
                if (!meshFilter || meshFilter.sharedMesh == null)
                {
                    return -1;
                }
                if (!meshFilter.sharedMesh.isReadable)
                {
                    return -1;
                }
                
                var numPoly = meshFilter.sharedMesh.triangles.Length / 3;
                numPolygon += numPoly;
            }
            
            return numPolygon;
        }

        private IEnumerator BuildFromMeshFilter(MeshFilter[] meshFilters, MeshRenderer[] meshRenderers,
            List<ConvexPolygonInfo> convexPolygonInfos)
        {
            int numBuildConvexPolygon = GetNumPolygonsFromMeshFilters(meshFilters);
            if (numBuildConvexPolygon < 0) yield break;
            
            var newConvexPolygonInfos = new ConvexPolygonInfo[numBuildConvexPolygon];

            // 计算一些缓冲区的大小并存储多边形的计数。
            int bufferSize = 0;
            var polygonCounts = new List<int>();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var meshFilter = meshFilters[i];
                if (!meshFilter || meshFilter.sharedMesh == null) continue;
                
                var mesh = meshFilter.sharedMesh;
                var subMeshCount = mesh.subMeshCount;
                for (int meshNo = 0; meshNo < subMeshCount; meshNo++)
                {
                    mesh.GetTriangles(_workingTrianglesForCalcPolygonCount, meshNo);
                    var numPoly = _workingTrianglesForCalcPolygonCount.Count / 3;
                    bufferSize += numPoly * VertexCountOfTrianglePolygon;
                    polygonCounts.Add(numPoly);
                }
            }

            // 分配一些缓冲区。
            var positionBuffer = new Vector3[bufferSize];
            var boneWeightBuffer = new BoneWeight[bufferSize];
            var normalBuffer = new Vector3[bufferSize];
            var lineBuffer = new Line[bufferSize];
            var localPositionBuffer = new Vector3[bufferSize];
            var localNormalBuffer = new Vector3[bufferSize];
            
            int startOffsetOfBuffer = 0;
            int rendererNo = 0;
            int newConvexPolygonNo = 0;
            int indexOfPolygonCounts = 0;

            for (int mi = 0; mi < meshFilters.Length; mi++)
            {
                var meshFilter = meshFilters[mi];
                if (!meshFilter || meshFilter.sharedMesh == null) yield break;
                
                var mesh = meshFilter.sharedMesh;
                using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
                var meshData = meshDataArray[0];
                meshData.GetVertices(_workingVertexPositions);
                meshData.GetNormals(_workingVertexNormals);
                
                int subMeshCount = meshData.subMeshCount;
                for (int meshNo = 0; meshNo < subMeshCount; meshNo++)
                {
                    meshData.GetIndices(_workingTriangles, meshNo);
                    var numPoly = polygonCounts[indexOfPolygonCounts++];
                    for (int i = 0; i < numPoly; i++)
                    {
                        if ((newConvexPolygonNo + 1) % MaxGeneratedPolygonPerFrame == 0)
                        {
                            // 每帧处理的最大多边形数为 MaxGeneratedPolygonPerFrame
                            yield return null;
                        }
                        if (!meshFilter || meshFilter.sharedMesh == null)
                        {
                            // 网格过滤器被删除，因此进程被终止。
                            yield break;
                        }
                        
                        int v0_no = _workingTriangles[i * 3];
                        int v1_no = _workingTriangles[i * 3 + 1];
                        int v2_no = _workingTriangles[i * 3 + 2];
                        
                        localPositionBuffer[startOffsetOfBuffer] = _workingVertexPositions[v0_no];
                        localPositionBuffer[startOffsetOfBuffer + 1] = _workingVertexPositions[v1_no];
                        localPositionBuffer[startOffsetOfBuffer + 2] = _workingVertexPositions[v2_no];

                        localNormalBuffer[startOffsetOfBuffer] = _workingVertexNormals[v0_no];
                        localNormalBuffer[startOffsetOfBuffer + 1] = _workingVertexNormals[v1_no];
                        localNormalBuffer[startOffsetOfBuffer + 2] = _workingVertexNormals[v2_no];

                        boneWeightBuffer[startOffsetOfBuffer] = default;
                        boneWeightBuffer[startOffsetOfBuffer + 1] = default;
                        boneWeightBuffer[startOffsetOfBuffer + 2] = default;
                        
                        newConvexPolygonInfos[newConvexPolygonNo] = new ConvexPolygonInfo
                        {
                            ConvexPolygon = new ConvexPolygon(
                                positionBuffer,
                                normalBuffer,
                                boneWeightBuffer,
                                lineBuffer,
                                localPositionBuffer,
                                localNormalBuffer,
                                meshRenderers[rendererNo],
                                startOffsetOfBuffer,
                                VertexCountOfTrianglePolygon,
                                rendererNo,
                                VertexCountOfTrianglePolygon)
                        };
                        
                        newConvexPolygonNo++;
                        startOffsetOfBuffer += VertexCountOfTrianglePolygon;
                    }
                }

                rendererNo++;
            }

            convexPolygonInfos.AddRange(newConvexPolygonInfos);
        }

        private IEnumerator BuildFromSkinMeshRenderer(SkinnedMeshRenderer[] skinnedMeshRenderers,
            List<ConvexPolygonInfo> trianglePolygonInfos)
        {
#if MEASUREMENT_METHOD_BuildFromSkinMeshRenderer
            var sw = new Stopwatch();
            sw.Start();
#endif
            
            int numBuildConvexPolygon = GetNumPolygonsFromSkinModelRenderers(skinnedMeshRenderers);
            if (numBuildConvexPolygon < 0) yield break;

            var newConvexPolygonInfos = new ConvexPolygonInfo[numBuildConvexPolygon];
            var boneWeights = new BoneWeight[3];
            var newConvexPolygonNo = 0;

            // Calculate size of some buffers and store the count of the polygons.
            var bufferSize = 0;
            var polygonCounts = new List<int>();
            for (var rendererNo = 0; rendererNo < skinnedMeshRenderers?.Length; rendererNo++)
            {
                var skinnedMeshRenderer = skinnedMeshRenderers[rendererNo];
                if (!skinnedMeshRenderer || skinnedMeshRenderer.sharedMesh == null)
                    // The skinned mesh renderer is deleted, so skip.
                    continue;
                var mesh = skinnedMeshRenderer.sharedMesh;
                var subMeshCount = mesh.subMeshCount;
                for (var meshNo = 0; meshNo < subMeshCount; meshNo++)
                {
                    mesh.GetTriangles(_workingTrianglesForCalcPolygonCount, meshNo);
                    var numPoly = _workingTrianglesForCalcPolygonCount.Count / 3;
                    bufferSize += numPoly * VertexCountOfTrianglePolygon;
                    polygonCounts.Add(numPoly);
                }
            }

            // Allocate some buffers.
            var positionBuffer = new Vector3[bufferSize];
            var localPositionBuffer = new Vector3[bufferSize];
            var boneWeightBuffer = new BoneWeight[bufferSize];
            var normalBuffer = new Vector3[bufferSize];
            var localNormalBuffer = new Vector3[bufferSize];
            var lineBuffer = new Line[bufferSize];
            var startOffsetOfBuffer = 0;
#if MEASUREMENT_METHOD_BuildFromSkinMeshRenderer
            sw.Stop();
            Time_BuildFromSkinMeshRenderer[0] = sw.ElapsedMilliseconds;
            sw = new Stopwatch();
            sw.Start();
#endif
            var indexOfPolygonCount = 0;
            for (var rendererNo = 0; rendererNo < skinnedMeshRenderers?.Length; rendererNo++)
            {
                var skinnedMeshRenderer = skinnedMeshRenderers[rendererNo];
                if (!skinnedMeshRenderer || skinnedMeshRenderer.sharedMesh == null)
                    // The skinned mesh renderer is deleted, so process is terminated.
                    yield break;
                var mesh = skinnedMeshRenderer.sharedMesh;

                using var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
                var meshData = meshDataArray[0];
                meshData.GetVertices(_workingVertexPositions);
                meshData.GetNormals(_workingVertexNormals);
                var subMeshCount = meshData.subMeshCount;
                mesh.GetBoneWeights(_workingBoneWeights);
                for (var meshNo = 0; meshNo < subMeshCount; meshNo++)
                {
                    meshData.GetIndices(_workingTriangles, meshNo);
                    var numPoly = polygonCounts[indexOfPolygonCount++];
                    for (var i = 0; i < numPoly; i++)
                    {
                        if ((newConvexPolygonNo + 1) % MaxGeneratedPolygonPerFrame == 0)
                            // Maximum number of polygons processed per frame is MaxGeneratedPolygonPerFrame.
                            yield return null;
                        if (!skinnedMeshRenderer || skinnedMeshRenderer.sharedMesh == null)
                            // The skinned mesh renderer is deleted, so process is terminated.
                            yield break;
                        var v0No = _workingTriangles[i * 3];
                        var v1No = _workingTriangles[i * 3 + 1];
                        var v2No = _workingTriangles[i * 3 + 2];

                        // Calculate world matrix.
                        if (skinnedMeshRenderer.rootBone != null)
                        {
                            boneWeights[0] = _workingBoneWeights[v0No];
                            boneWeights[1] = _workingBoneWeights[v1No];
                            boneWeights[2] = _workingBoneWeights[v2No];
                            boneWeightBuffer[startOffsetOfBuffer] = boneWeights[0];
                            boneWeightBuffer[startOffsetOfBuffer + 1] = boneWeights[1];
                            boneWeightBuffer[startOffsetOfBuffer + 2] = boneWeights[2];
                        }
                        else
                        {
                            boneWeightBuffer[startOffsetOfBuffer] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 1] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 2] = default;
                        }

                        localPositionBuffer[startOffsetOfBuffer] = _workingVertexPositions[v0No];
                        localPositionBuffer[startOffsetOfBuffer + 1] = _workingVertexPositions[v1No];
                        localPositionBuffer[startOffsetOfBuffer + 2] = _workingVertexPositions[v2No];

                        localNormalBuffer[startOffsetOfBuffer] = _workingVertexNormals[v0No];
                        localNormalBuffer[startOffsetOfBuffer + 1] = _workingVertexNormals[v1No];
                        localNormalBuffer[startOffsetOfBuffer + 2] = _workingVertexNormals[v2No];


                        newConvexPolygonInfos[newConvexPolygonNo] = new ConvexPolygonInfo
                        {
                            ConvexPolygon = new ConvexPolygon(
                                positionBuffer,
                                normalBuffer,
                                boneWeightBuffer,
                                lineBuffer,
                                localPositionBuffer,
                                localNormalBuffer,
                                skinnedMeshRenderer,
                                startOffsetOfBuffer,
                                3,
                                rendererNo,
                                VertexCountOfTrianglePolygon)
                        };
                        newConvexPolygonNo++;
                        startOffsetOfBuffer += VertexCountOfTrianglePolygon;
                    }
                }
            }

            trianglePolygonInfos.AddRange(newConvexPolygonInfos);
            
#if MEASUREMENT_METHOD_BuildFromSkinMeshRenderer
            sw.Stop();
            Time_BuildFromSkinMeshRenderer[1] = sw.ElapsedMilliseconds;
#endif
        }

        private IEnumerator BuildFromTerrain(Terrain[] terrains, float terrainMeshResolutionScale,
            List<ConvexPolygonInfo> convexPolygonInfos)
        {
            int numBuildConvexPolygon = GetNumPolygonsFromTerrains(terrains, terrainMeshResolutionScale);
            if (numBuildConvexPolygon < 0) yield break;
            
            var newConvexPolygonInfos = new ConvexPolygonInfo[numBuildConvexPolygon];
            // Calculate size of some buffers and store the count of the polygons.
            int bufferSize = 0;
            //var polygonCounts = new List<int>();
            for (int i = 0; i < terrains?.Length; i++)
            {
                var terrain = terrains[i];
                if(!terrain) continue;
                
                var numPoly = GetNumPolygonsFromTerrain(terrain, terrainMeshResolutionScale);
                bufferSize += numPoly * VertexCountOfTrianglePolygon;
                //polygonCounts.Add(numPoly);
            }

            // Allocate some buffers.
            var positionBuffer = new Vector3[bufferSize];
            var boneWeightBuffer = new BoneWeight[bufferSize];
            var normalBuffer = new Vector3[bufferSize];
            var lineBuffer = new Line[bufferSize];
            var localPositionBuffer = new Vector3[bufferSize];
            var localNormalBuffer = new Vector3[bufferSize];
            var workingVertexPositions = new Vector3[bufferSize];
            var workingVertexNormals = new Vector3[bufferSize];
            var startOffsetOfBuffer = 0;

            //int rendererNo = 0;
            int newConvexPolygonNo = 0;
            //int indexOfPolygonCounts = 0;

            for (int i = 0; i < terrains?.Length; i++)
            {
                var terrain = terrains[i];
                if (!terrain) yield break; // Terrain is deleted, so process is terminated.
                
                var terrainData = terrain.terrainData;
                var invResolutionScale = 1.0f / terrainMeshResolutionScale;
                var vertexCountW = Math.Max(2, (int)(terrainData.heightmapResolution * terrainMeshResolutionScale));
                var vertexCountH = Math.Max(2, (int)(terrainData.heightmapResolution * terrainMeshResolutionScale));
                var size = terrainData.size;

                // Build vertex buffer.
                var vertexNo = 0;
                for (var y = 0; y < vertexCountH; y++)
                {
                    for (var x = 0; x < vertexCountW; x++)
                    {
                        var normalizedPosition =
                            new Vector2(x / (float)(vertexCountW - 1), y / (float)(vertexCountH - 1));
                        workingVertexNormals[vertexNo] = terrainData.GetInterpolatedNormal(
                            x / (float)(vertexCountW - 1),
                            y / (float)(vertexCountH - 1));
                        float height = terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.y);
                        workingVertexPositions[vertexNo] = new Vector3(size.x * normalizedPosition.x,
                            height, size.z * normalizedPosition.y);
                        vertexNo++;
                    }
                }

                // Build Convex Polygon.
                for (int y = 0; y < vertexCountH - 1; y++)
                {
                    for (int x = 0; x < vertexCountW - 1; x++)
                    {
                        {
                            var v0_no = (y * vertexCountW) + x;
                            var v1_no = ((y + 1) * vertexCountW) + x;
                            var v2_no = (y * vertexCountW) + x + 1;

                            localPositionBuffer[startOffsetOfBuffer] = workingVertexPositions[v0_no];
                            localPositionBuffer[startOffsetOfBuffer + 1] = workingVertexPositions[v1_no];
                            localPositionBuffer[startOffsetOfBuffer + 2] = workingVertexPositions[v2_no];

                            localNormalBuffer[startOffsetOfBuffer] = workingVertexNormals[v0_no];
                            localNormalBuffer[startOffsetOfBuffer + 1] = workingVertexNormals[v1_no];
                            localNormalBuffer[startOffsetOfBuffer + 2] = workingVertexNormals[v2_no];

                            boneWeightBuffer[startOffsetOfBuffer] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 1] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 2] = default;

                            newConvexPolygonInfos[newConvexPolygonNo] = new ConvexPolygonInfo
                            {
                                ConvexPolygon = new ConvexPolygon(
                                    positionBuffer,
                                    normalBuffer,
                                    boneWeightBuffer,
                                    lineBuffer,
                                    localPositionBuffer,
                                    localNormalBuffer,
                                    terrain,
                                    startOffsetOfBuffer,
                                    VertexCountOfTrianglePolygon,
                                    0,
                                    VertexCountOfTrianglePolygon)
                            };
                            newConvexPolygonNo++;
                            startOffsetOfBuffer += VertexCountOfTrianglePolygon;
                        }
                        {
                            var v0_no = ((y + 1) * vertexCountW) + x;
                            var v1_no = ((y + 1) * vertexCountW) + x + 1;
                            var v2_no = (y * vertexCountW) + x + 1;

                            localPositionBuffer[startOffsetOfBuffer] = workingVertexPositions[v0_no];
                            localPositionBuffer[startOffsetOfBuffer + 1] = workingVertexPositions[v1_no];
                            localPositionBuffer[startOffsetOfBuffer + 2] = workingVertexPositions[v2_no];

                            localNormalBuffer[startOffsetOfBuffer] = workingVertexNormals[v0_no];
                            localNormalBuffer[startOffsetOfBuffer + 1] = workingVertexNormals[v1_no];
                            localNormalBuffer[startOffsetOfBuffer + 2] = workingVertexNormals[v2_no];

                            boneWeightBuffer[startOffsetOfBuffer] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 1] = default;
                            boneWeightBuffer[startOffsetOfBuffer + 2] = default;

                            newConvexPolygonInfos[newConvexPolygonNo] = new ConvexPolygonInfo
                            {
                                ConvexPolygon = new ConvexPolygon(
                                    positionBuffer,
                                    normalBuffer,
                                    boneWeightBuffer,
                                    lineBuffer,
                                    localPositionBuffer,
                                    localNormalBuffer,
                                    terrain,
                                    startOffsetOfBuffer,
                                    VertexCountOfTrianglePolygon,
                                    0,
                                    VertexCountOfTrianglePolygon)
                            };
                            newConvexPolygonNo++;
                            startOffsetOfBuffer += VertexCountOfTrianglePolygon;
                        }
                    }
                }
            }

            convexPolygonInfos.AddRange(newConvexPolygonInfos);
        }
        
    }
}