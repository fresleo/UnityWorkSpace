// Created By: WangYu  Date: 2022-10-18

using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格的4叉树包装器
    /// </summary>
    public class TMQuadTreeWrapper
    {
        //剔除平面
        private Plane[] m_cullPlanes;
        //树的所有节点
        private TMQuadTreeNode[] m_treeNodes;
        //候选，激活，可见
        private MTArray<TMQuadTreeNode> m_candidateArray, m_activeArray, m_visibleArray;
        
        public TMQuadTreeWrapper(byte[] data, Vector3 centerOffset)
        {
            m_cullPlanes = new Plane[6];
            
            MemoryStream stream = new MemoryStream(data);
            int treeLen = MTStreamUtils.ReadInt(stream);
            ReadTreeData(treeLen, stream, centerOffset);
            stream.Close();
            
            m_candidateArray = new MTArray<TMQuadTreeNode>(treeLen);
            m_activeArray = new MTArray<TMQuadTreeNode>(treeLen);
            m_visibleArray = new MTArray<TMQuadTreeNode>(treeLen);
        }

        //读取树的数据
        private void ReadTreeData(int treeLen, Stream stream, Vector3 centerOffset)
        {
            m_treeNodes = new TMQuadTreeNode[treeLen];
            MinCellSize = float.MaxValue;
            
            for (int i = 0; i < treeLen; i++)
            {
                var node = new TMQuadTreeNode();
                node.Initialize(-1);
                node.Deserialize(stream, centerOffset);
                m_treeNodes[i] = node;
                
                var size = Mathf.Min(node.bnd.size.x, node.bnd.size.z);
                if (size < MinCellSize)
                {
                    MinCellSize = size;
                }
            }
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            if (m_treeNodes != null)
            {
                for (int i = 0; i < m_treeNodes.Length; i++)
                {
                    var item = m_treeNodes[i];
                    item?.Clear();
                }
                m_treeNodes = null;
            }

            if (m_candidateArray != null)
            {
                m_candidateArray.Clear(item => item?.Clear());
                m_candidateArray = null;
            }
            if (m_activeArray != null)
            {
                m_activeArray.Clear(item => item?.Clear());
                m_activeArray = null;
            }
            if (m_visibleArray != null)
            {
                m_visibleArray.Clear(item => item?.Clear());
                m_visibleArray = null;
            }
            
            m_cullPlanes = null;
        }

        /// <summary>
        /// 剔除4叉树
        /// </summary>
        public void CullQuadtree(
            Vector3 viewerPos, float fov,
            float screenH, float screenW,
            Matrix4x4 projectionMatrix, Matrix4x4 world2CameraMatrix,
            MTArray<TMQuadTreeNode> activeCmd, MTArray<TMQuadTreeNode> deactiveCmd,
            LODPolicy lodPolicy)
        {
            Matrix4x4 worldToProjectionMatrix = projectionMatrix * world2CameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, m_cullPlanes);

            MTRuntimeUtils.CullQuadTreeNode(
                m_cullPlanes, m_treeNodes, 
                m_candidateArray, m_visibleArray,
                CheckVisibility);

            bool CheckVisibility(TMQuadTreeNode curNode)
            {
                //判断当前节点的lod是否可以显示
                bool stop_child = false;
                if (curNode.meshId >= 0)
                {
                    float pixelSize = curNode.PixelSize(viewerPos, fov, screenH);
                    int lodLv = lodPolicy.GetLODLevel(pixelSize, screenW);
                    if (curNode.lodLv <= lodLv)
                    {
                        stop_child = true; //此级以下全部隐藏
                    }
                }

                return stop_child;
            }

            //没激活的，但是在显示列表里的，准备去显示
            for (int i = 0; i < m_visibleArray.Length; i++)
            {
                var node = m_visibleArray[i];
                if (!m_activeArray.Contains(node))
                {
                    activeCmd.Add(node);
                }
            }
            //已经激活的，但是不显示的，准备隐藏
            for (int i = 0; i < m_activeArray.Length; i++)
            {
                var node = m_activeArray[i];
                if (!m_visibleArray.Contains(node))
                {
                    deactiveCmd.Add(node);
                }
            }
            //角色交换，为下一次剔除做准备
            (m_activeArray, m_visibleArray) = (m_visibleArray, m_activeArray);
        }
        
        
        /// <summary>
        /// 节点总数
        /// </summary>
        public int NodeCount => m_treeNodes.Length;

        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds Bnd => m_treeNodes[0].bnd;

        /// <summary>
        /// 激活的节点
        /// </summary>
        public MTArray<TMQuadTreeNode> ActiveNodes => m_activeArray;

        /// <summary>
        /// 最小单元尺寸
        /// </summary>
        public float MinCellSize { get; private set; }
        
        /// <summary>
        /// 索引4叉树节点
        /// </summary>
        /// <param name="index">索引</param>
        public TMQuadTreeNode this[int index]
        {
            get
            {
                if (index >= 0 && index < m_treeNodes.Length)
                {
                    return m_treeNodes[index];
                }
                return null;
            }
        }
        
    }
}