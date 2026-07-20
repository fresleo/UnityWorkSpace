// Created By: WangYu  Date: 2023-11-21

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.StaticObject;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    public class SOLoader : AbsMTLoader
    {
        public string groupConfigPath;
        public GameObject[] prototypes;
        
        public float displayDistance = 1000;
        
        public bool debugTree;
        public bool debugTreeAssetIdxs;


        //组配置
        private SOGroupConfig m_groupConfig;
        
        //父节点
        private Transform m_activeParent;
        private Transform m_deactiveParent;
        
        //原始的远平面距离
        private float m_originalFarPlane;
        //剔除平面
        private Plane[] m_cullPlanes;
        //树的所有节点
        private SOQuadTreeNode[] m_treeNodes;
        //候选，激活，可见
        private MTArray<SOQuadTreeNode> m_candidateArray, m_activeArray, m_visibleArray;
        public MTArray<SOQuadTreeNode> VisibleArray => m_visibleArray;


        public SOGroupConfig GroupConfig => m_groupConfig;
        

        //静态对象的包装
        struct SOWrap
        {
            public GameObject go;
            public MeshRenderer[] mrs;
        }
        //静态对象有定位的包装
        struct SOWrapHadLocation
        {
            public int assetIdx;
            public SOWrap wrap;
        }
        
        //go池
        private Dictionary<int, Queue<SOWrap>> m_soPool = new();
        //激活的go
        private Dictionary<int, SOWrapHadLocation> m_soActivatedDict = new();

        
        protected override void OnStop()
        {
            m_groupConfig = null;
            
            m_activeParent = null;
            m_deactiveParent = null;
            
            m_cullPlanes = null;
            if (m_treeNodes != null)
            {
                for (int i = 0; i < m_treeNodes.Length; i++)
                {
                    var node = m_treeNodes[i];
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
            
            foreach (var iter in m_soPool)
            {
                iter.Value.Clear();
            }
            m_soPool.Clear();
            m_soActivatedDict.Clear();
        }

        protected override void OnPlay()
        {
            m_groupConfig = LoadAssetObject<SOGroupConfig>(groupConfigPath);
            if (m_groupConfig == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            var treeData = LoadAssetObject<TextAsset>(m_groupConfig.treeDataPath);
            if (treeData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            //创建渲染器根节点
            if (m_activeParent == null)
            {
                var apGo = new GameObject("Active");
                apGo.layer = gameObject.layer;
                apGo.transform.SetParent(transform);
                m_activeParent = apGo.transform;
            }
            if (m_deactiveParent == null)
            {
                var dpGo = new GameObject("Deactive");
                dpGo.layer = gameObject.layer;
                dpGo.transform.SetParent(transform);
                m_deactiveParent = dpGo.transform;
            }
            
            m_cullPlanes = new Plane[6];
            
            //读取树的2进制数据
            MemoryStream stream = new MemoryStream(treeData.bytes);
            int treeLen = MTStreamUtils.ReadInt(stream);
            m_treeNodes = new SOQuadTreeNode[treeLen];
            for (int i = 0; i < treeLen; i++)
            {
                var node = new SOQuadTreeNode();
                node.Initialize(-1);
                node.Deserialize(stream, Vector3.zero);
                m_treeNodes[i] = node;
            }
            stream.Close();
            
            m_candidateArray = new MTArray<SOQuadTreeNode>(treeLen);
            m_activeArray = new MTArray<SOQuadTreeNode>(treeLen);
            m_visibleArray = new MTArray<SOQuadTreeNode>(treeLen);
        }


        protected override bool CanFrameRendering()
        {
            return m_groupConfig != null;
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

            bool CheckVisibility(SOQuadTreeNode curNode)
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
        
        private void OnDrawGizmos()
        {
            if (!this.enabled) return;
            
            var lastColor = Gizmos.color;
            {
                //画树的包围盒
                if (debugTree)
                {
                    //只画可见的
                    if (m_visibleArray != null)
                    {
                        for (int i = 0; i < m_visibleArray.Length; i++)
                        {
                            var node = m_visibleArray[i];
                            
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(node.bnd.center, node.bnd.size);
                        }
                    }
                }
            }
            Gizmos.color = lastColor;
        }

        //激活节点
        private void ActiveNode(SOQuadTreeNode node)
        {
            for (int i = 0; i < node.holdGids.Count; i++)
            {
                int assetIdx = node.holdAssetIdxs[i];
                //资源不合法
                if (assetIdx < 0 || assetIdx >= prototypes.Length)
                {
                    continue;
                }

                int gid = node.holdGids[i];
                Matrix4x4 worldMatrix = node.holdWorldMatrixs[i];
                LightmapConfig[] lds = node.holdLightmapDatas[gid];

                //已经激活了
                if (m_soActivatedDict.ContainsKey(gid))
                {
                    continue;
                }
                
                //确保资源池存在
                if (!m_soPool.TryGetValue(assetIdx, out var soQueue))
                {
                    soQueue = new Queue<SOWrap>();
                    m_soPool[assetIdx] = soQueue;
                }
                
                //取出包装
                SOWrap soWrap;
                if (soQueue.Count > 0)
                {
                    soWrap = soQueue.Dequeue();
                }
                else
                {
                    GameObject prefab = prototypes[assetIdx];
                    GameObject clone = Instantiate(prefab);

                    soWrap.go = clone;
                    soWrap.mrs = clone.GetComponentsInChildren<MeshRenderer>();
                }

                //各种显示操作
                soWrap.go.SetActive(true);
                soWrap.go.transform.SetParent(m_activeParent, false);
                SyncWorldMatrix(soWrap, worldMatrix);
                RecoverLightmap(soWrap, lds);

                //记录到激活字典中
                SOWrapHadLocation aso = new SOWrapHadLocation
                {
                    assetIdx = assetIdx,
                    wrap = soWrap
                };
                m_soActivatedDict[gid] = aso;
            }
        }

        //停用节点
        private void DeactiveNode(SOQuadTreeNode node)
        {
            for (int i = 0; i < node.holdGids.Count; i++)
            {
                int gid = node.holdGids[i];
                
                //停用对象
                SOWrapHadLocation aso = m_soActivatedDict[gid];
                m_soActivatedDict.Remove(gid);
                
                aso.wrap.go.SetActive(false);
                aso.wrap.go.transform.SetParent(m_deactiveParent, false);
                
                m_soPool[aso.assetIdx].Enqueue(aso.wrap);
            }
        }
        
        //同步世界矩阵
        private void SyncWorldMatrix(SOWrap wrap, Matrix4x4 worldMatrix)
        {
            wrap.go.transform.position = worldMatrix.GetPosition();
            wrap.go.transform.rotation = worldMatrix.rotation;
            
            Matrix4x4 localMatrix = wrap.go.transform.parent.worldToLocalMatrix * worldMatrix;
            wrap.go.transform.localScale = localMatrix.lossyScale;
        }
        
        //恢复 Lightmap
        private void RecoverLightmap(SOWrap wrap, LightmapConfig[] lds)
        {
            for (int i = 0; i < lds.Length; i++)
            {
                var ld = lds[i];
                if (!ld.baked)
                {
                    continue;
                }

                var mr = wrap.mrs[i];
                
                mr.lightmapIndex = ld.index;
                mr.lightmapScaleOffset = ld.scaleOffset;
            }
        }
        
    }
}