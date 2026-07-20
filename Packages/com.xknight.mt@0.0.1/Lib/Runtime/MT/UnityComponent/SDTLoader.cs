// Created By: WangYu  Date: 2024-05-30

using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.OcclusionCulling;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    public class SDTLoader : AbsMTLoader
    {
        /// <summary>
        /// 配置的路径
        /// </summary>
        public string configPath;
        
        /// <summary>
        /// 显示的距离
        /// </summary>
        public float displayDistance = 1000;

        /// <summary>
        /// 剔除器
        /// </summary>
        public DynamicOcclusionCuller culler;


        // 配置文件
        private SpatialDataTagConfig m_config;
        
        // 树的所有节点
        private SDTQuadTreeNode[] m_treeNodes;
        // 功能性节点队列
        private MTArray<SDTQuadTreeNode> m_candidateArray, m_activeArray, m_visibleArray;
        
        // 原始的远平面距离
        private float m_originalFarPlane;
        // 剔除平面
        private Plane[] m_cullPlanes;


        protected override void OnSetCullCamera()
        {
            if (culler != null)
            {
                culler.cullCamera = this.cullCamera;
            }
        }

        protected override void OnStop()
        {
            m_config = null;

            if (m_treeNodes != null)
            {
                foreach (var node in m_treeNodes)
                {
                    node?.Clear();
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

        protected override void OnPlay()
        {
            if (!LoadAsset())
            {
                gameObject.SetActive(false);
                return;
            }
            
            m_cullPlanes = new Plane[6];
        }

        private bool LoadAsset()
        {
            m_config = LoadAssetObject<SpatialDataTagConfig>(configPath);
            if (m_config == null)
            {
                return false;
            }
            
            var treeData = LoadAssetObject<TextAsset>(m_config.treeDataPath);
            if (treeData == null)
            {
                return false;
            }

            //读取树的2进制数据
            MemoryStream stream = new MemoryStream(treeData.bytes);
            int treeLen = MTStreamUtils.ReadInt(stream);
            m_treeNodes = new SDTQuadTreeNode[treeLen];
            for (int i = 0; i < treeLen; i++)
            {
                var node = new SDTQuadTreeNode();
                node.Initialize(-1);
                node.Deserialize(stream, Vector3.zero);
                m_treeNodes[i] = node;
            }
            stream.Close();
            
            m_candidateArray = new MTArray<SDTQuadTreeNode>(treeLen);
            m_activeArray = new MTArray<SDTQuadTreeNode>(treeLen);
            m_visibleArray = new MTArray<SDTQuadTreeNode>(treeLen);
            
            return true;
        }

        protected override bool CanFrameRendering()
        {
            return m_config != null;
        }

        protected override void OnCameraMoves(ScriptableRenderContext context, List<Camera> cameras)
        {
            //创建视锥平面
            m_originalFarPlane = cullCamera.farClipPlane;
            cullCamera.farClipPlane = displayDistance;
            GeometryUtility.CalculateFrustumPlanes(cullCamera, m_cullPlanes);
            cullCamera.farClipPlane = m_originalFarPlane;
            
            MTRuntimeUtils.CullQuadTreeNode(
                m_cullPlanes, m_treeNodes, 
                m_candidateArray, m_visibleArray,
                CheckVisibility);
            
            bool CheckVisibility(SDTQuadTreeNode curNode)
            {
                // 没子节点，说明是叶子节点，加入可见列表
                return curNode.children.Length == 0;
            }
            
            //激活节点
            for (int i = 0; i < m_visibleArray.Length; i++)
            {
                var node = m_visibleArray[i];
                if (!m_activeArray.Contains(node))
                {
                    ActiveNode(node);
                }
            }
            //停用节点
            for (int i = 0; i < m_activeArray.Length; i++)
            {
                var node = m_activeArray[i];
                if (!m_visibleArray.Contains(node))
                {
                    DeactiveNode(node);
                }
            }
            //角色交换，为下一次剔除做准备
            (m_activeArray, m_visibleArray) = (m_visibleArray, m_activeArray);
        }

        private void ActiveNode(SDTQuadTreeNode node)
        {
            for (int i = 0; i < node.holdIds.Count; i++)
            {
                int rid = node.holdIds[i];
                Bounds bounds = node.holdBounds[i];
                Matrix4x4 worldMatrix = node.holdWorldMatrixs[i];

                culler.AddTestData(rid, bounds, worldMatrix);
            }
        }

        private void DeactiveNode(SDTQuadTreeNode node)
        {
            for (int i = 0; i < node.holdIds.Count; i++)
            {
                int rid = node.holdIds[i];
                
                culler.RemoveTestData(rid);
            }
        }
    }
}