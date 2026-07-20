// Created By: WangYu  Date: 2022-10-02

using System;
using System.Collections.Generic;
using com.xknight.mt.Lib.Editor.MT.TerrainSampling;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using com.xknight.mt.Lib.Runtime.TriangleNet.Data;
using com.xknight.mt.Lib.Runtime.TriangleNet.Geometry;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;
using TNMesh = com.xknight.mt.Lib.Runtime.TriangleNet.Mesh;

namespace com.xknight.mt.Lib.Editor.MT.Jobs
{
    /// <summary>
    /// 三角测量的工作
    /// </summary>
    public class TriangulateJob : IMTJob
    {
        /// <summary>
        /// 各级lod的扫描器
        /// </summary>
        public UnityTerrainScanner[] scanners;
        /// <summary>
        /// 网格数据
        /// </summary>
        public TriangulateMeshData[] meshDatas;
        /// <summary>
        /// 最小三角形面积
        /// </summary>
        protected float m_minTriangleArea;
        
        /// <summary>
        /// 当前网格的索引
        /// </summary>
        protected int m_curMeshIdx = 0;

        public TriangulateJob(UnityTerrainScanner[] scanners, float minTriangleArea)
        {
            this.scanners = scanners;
            m_minTriangleArea = minTriangleArea;
            
            //每级mesh的数量都是一样的
            var lod0 = this.scanners[0];
            this.meshDatas = new TriangulateMeshData[lod0.Trees.Length];
        }
        
        public bool IsDone => m_curMeshIdx >= meshDatas.Length;
        
        public float Progress => ((float)m_curMeshIdx / meshDatas.Length);
        
        public virtual void Update()
        {
            if (IsDone)
            {
                return;
            }

            //每级mesh的包围盒都是一样的
            Bounds bnd = scanners[0].Trees[m_curMeshIdx].BND;
            meshDatas[m_curMeshIdx] = new TriangulateMeshData(m_curMeshIdx, bnd);
            meshDatas[m_curMeshIdx].lods = new TriangulateMeshData.LOD[scanners.Length];
            
            for (int lod = 0; lod < scanners.Length; lod++)
            {
                var tree = scanners[lod].Trees[m_curMeshIdx];
                
                var lodData = new TriangulateMeshData.LOD();
                lodData.uvMin = tree.UVMin;
                lodData.uvMax = tree.UVMax;

                RunTriangulate(tree.svdLs, lodData, tree.BND);
                meshDatas[m_curMeshIdx].lods[lod] = lodData;
            }
            
            m_curMeshIdx++; //下1个
        }
        
        /// <summary>
        /// 执行细分
        /// </summary>
        protected void RunTriangulate(List<SampleVertexData> svdLs, TriangulateMeshData.LOD lod, Bounds debugBounds)
        {
            if (svdLs.Count < 3)
            {
                m_curMeshIdx++;
                return;
            }

            try
            {
                OnRunTriangulate(svdLs, lod);
            }
            catch (Exception ex)
            {
                MTLogger.LogError($"三角测量发生异常，已跳过 : MeshIdx = {m_curMeshIdx} Bounds = {debugBounds} \n\n {ex}");
            }
        }

        private void OnRunTriangulate(List<SampleVertexData> svdLs, TriangulateMeshData.LOD lod)
        {
            //平面几何图形
            var tGeometry = new InputGeometry();
            for (int i = 0; i < svdLs.Count; i++)
            {
                var svd = svdLs[i];
                tGeometry.AddPoint(svd.position.x, svd.position.z);
            }

            var tnMesh = new TNMesh();
            tnMesh.Triangulate(tGeometry);
            
            int vertCount = tnMesh.Vertices.Count;
            if (vertCount != svdLs.Count)
            {
                MTLogger.LogError("三角测量失败");
            }
            
            lod.vertices = new Vector3[vertCount];
            lod.normals = new Vector3[vertCount];
            lod.uvs = new Vector2[vertCount];
            lod.faces = new int[tnMesh.triangles.Count * 3];
            
            int vIdx = 0;
            foreach (Vertex tVertex in tnMesh.Vertices)
            {
                var svdItem = svdLs[vIdx];
                
                lod.vertices[vIdx] = new Vector3(tVertex.x, svdItem.position.y, tVertex.y);
                lod.normals[vIdx] = svdItem.normal;
                lod.uvs[vIdx] = svdItem.uv;
                
                vIdx++;
            }

            vIdx = 0;
            foreach (Triangle tTriangle in tnMesh.triangles.Values)
            {
                var tPos = new Vector2[]
                {
                    new Vector2(lod.vertices[tTriangle.P0].x, lod.vertices[tTriangle.P0].z),
                    new Vector2(lod.vertices[tTriangle.P1].x, lod.vertices[tTriangle.P1].z),
                    new Vector2(lod.vertices[tTriangle.P2].x, lod.vertices[tTriangle.P2].z)
                };
                //已知3角形的3个点，求三角形的面积，参考公式：
                // abs((x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) / 2.0)
                // abs((x3 - x1) * (y2 - y1) - (x2 - x1) * (y3 - y1)) / 2.0
                float triArea = Mathf.Abs((tPos[2].x - tPos[0].x) * (tPos[1].y - tPos[0].y) - (tPos[1].x - tPos[0].x) * (tPos[2].y - tPos[0].y)) / 2.0f;
                if (triArea < m_minTriangleArea)
                {
                    continue;
                }

                lod.faces[vIdx] = tTriangle.P2;
                lod.faces[vIdx + 1] = tTriangle.P1;
                lod.faces[vIdx + 2] = tTriangle.P0;
                
                vIdx += 3;
            }
        }
        
    }
}