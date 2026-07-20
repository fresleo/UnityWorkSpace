// Created By: WangYu  Date: 2023-11-21

using System;
using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.DataContainer;
using com.xknight.mt.Lib.Runtime.MT.Detail;
using com.xknight.mt.Lib.Runtime.MT.Log;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class TMLoader : AbsMTLoader
    {
        //地表细节绘制功能 >>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 开关
        /// </summary>
        public bool useDetailDraw;
        /// <summary>
        /// 绘制距离
        /// </summary>
        public float detailDrawDistance = 64;
        
        /// <summary>
        /// LOD策略
        /// </summary>
        public string lpPath;
        /// <summary>
        /// 地形网格的配置
        /// </summary>
        public string tmcPath;
        
        /// <summary>
        /// 接收阴影
        /// </summary>
        public bool receiveShadow = true;
        
        
        // 显示调试信息
        public bool debugDetail;
        public bool debugTreeWorldBounds;
        public Transform debugPositionContain;
        
        
        private LODPolicy m_lp;
        private TerrainMeshConfig m_tmc;
        
        //父节点
        private Transform m_activeParent;
        private Transform m_deactiveParent;
        
        //虚拟纹理创建器
        private IVTCreator m_vtCreator;
        
        //4叉树
        private TMQuadTreeWrapper m_quadTree;
        //树节点命令
        private MTArray<TMQuadTreeNode> m_activeCmd, m_deactiveCmd;
        
        //高度图
        private MTHeightMap m_heightMap;
        
        //网格池
        private TerrainMeshDataPool m_terrainMeshDataPool;
        //激活的 Patch 渲染器
        private Dictionary<int, PooledTMPatchRender> m_activePatchRenders = new ();
        //细节渲染器
        // private DetailRenderer m_detailRenderer;
        
        //细节投影矩阵
        private Matrix4x4 m_detailProjM;

        
        public LODPolicy LP => m_lp;

        public TerrainMeshConfig TMC => m_tmc;
        

        protected override void OnSetCullCamera()
        {
            //细节投影矩阵
            m_detailProjM = Matrix4x4.Perspective(
                cullCamera.fieldOfView, cullCamera.aspect, cullCamera.nearClipPlane,
                detailDrawDistance);
        }

        protected override void OnStop()
        {
            m_lp = null;
            m_tmc = null;

            ClearParentNode();
            m_activeParent = null;
            m_deactiveParent = null;

            m_vtCreator = null;

            if (m_quadTree != null)
            {
                m_quadTree.Clear();
                m_quadTree = null;
            }
            if (m_activeCmd != null)
            {
                m_activeCmd.Clear();
                m_activeCmd = null;
            }
            if (m_deactiveCmd != null)
            {
                m_deactiveCmd.Clear();
                m_deactiveCmd = null;
            }

            if (m_heightMap != null)
            {
                MTHeightMap.UnregisterMap(m_heightMap);
                m_heightMap = null;
            }

            if (m_terrainMeshDataPool != null)
            {
                m_terrainMeshDataPool.Clear();
                m_terrainMeshDataPool = null;
            }
            
            m_activePatchRenders.Clear();
            PooledTMPatchRender.Clear();

            /*
            if (m_detailRenderer != null)
            {
                m_detailRenderer.Clear();
                m_detailRenderer = null;
            }
            */
        }
        
        // 加载资源
        private bool LoadAsset(out TextAsset treeData, out TextAsset heightMap)
        {
            treeData = null;
            heightMap = null;
            
            m_lp = LoadAssetObject<LODPolicy>(lpPath);
            if (m_lp == null)
            {
                MTLogger.LogError($"{nameof(LODPolicy)} 加载失败: {lpPath}");
                return false;
            }
            
            m_tmc = LoadAssetObject<TerrainMeshConfig>(tmcPath);
            if (m_tmc == null)
            {
                MTLogger.LogError($"{nameof(TerrainMeshConfig)} 加载失败: {tmcPath}");
                return false;
            }

            string tmcSubPath = m_tmc.treeDataPath;
            treeData = LoadAssetObject<TextAsset>(tmcSubPath);
            if (treeData == null)
            {
                MTLogger.LogError($"{nameof(TextAsset)} 加载失败: {tmcSubPath}");
                return false;
            }

            tmcSubPath = m_tmc.heightMapPath;
            heightMap = LoadAssetObject<TextAsset>(tmcSubPath);
            if (heightMap == null)
            {
                MTLogger.LogError($"{nameof(TextAsset)} 加载失败: {tmcSubPath}");
                return false;
            }
            
            return true;
        }
        
        protected override void OnPlay()
        {
            bool result = LoadAsset(out TextAsset treeData, out TextAsset heightMap);
            if (!result)
            {
                gameObject.SetActive(false);
                return;
            }
            
            //创建渲染器根节点
            CreateParentNode();
            
            //vt创建器脚本
            m_vtCreator = FindObjectOfType<AbsVTCreator>();
            
            //4叉树
            m_quadTree = new TMQuadTreeWrapper(treeData.bytes, transform.position);
            //树节点命令
            m_activeCmd = new MTArray<TMQuadTreeNode>(m_quadTree.NodeCount);
            m_deactiveCmd = new MTArray<TMQuadTreeNode>(m_quadTree.NodeCount);
            
            //高度图
            m_heightMap = new MTHeightMap(
                m_quadTree.Bnd, m_tmc.heightmapWorldY, 
                m_tmc.heightmapResolution, m_tmc.heightmapScale, 
                heightMap.bytes);
            
            //网格池
            m_terrainMeshDataPool = new TerrainMeshDataPool(m_tmc, MTAssetLoadMgr.Instance.meshDataLoader);
            
            //细节渲染器
            if (useDetailDraw)
            {
                // m_detailRenderer = new DetailRenderer(m_tmc, m_quadTree.Bnd, receiveShadow);
            }
        }


        protected override bool CanFrameRendering()
        {
            return m_lp != null && m_tmc != null && m_quadTree != null;
        }

        protected override void OnCameraMoves(ScriptableRenderContext context, List<Camera> cameras)
        {
            m_activeCmd.Reset();
            m_deactiveCmd.Reset();
            
            Matrix4x4 w2cm = cullCamera.worldToCameraMatrix;
            //执行4叉树剔除
            m_quadTree.CullQuadtree(
                cullCamera.transform.position, cullCamera.fieldOfView, 
                Screen.height, Screen.width, 
                projM, w2cm,
                m_activeCmd, m_deactiveCmd, 
                m_lp);
            
            //执行命令，把 Patch 的 Mesh 处理好
            for (int i = 0; i < m_activeCmd.Length; i++)
            {
                ActiveMesh(m_activeCmd[i]);
            }
            for (int i = 0; i < m_deactiveCmd.Length; i++)
            {
                DeactiveMesh(m_deactiveCmd[i]);
            }
            
            //更新所有处理激活状态 Patch 的纹理
            if (m_quadTree.ActiveNodes.Length > 0)
            {
                for (int i = 0; i < m_quadTree.ActiveNodes.Length; i++)
                {
                    var node = m_quadTree.ActiveNodes[i];
                    var patch = m_activePatchRenders[node.meshId];
                    patch.UpdatePatchTexture(
                        cullCamera.transform.position, cullCamera.fieldOfView, 
                        Screen.height, Screen.width);
                }
            }
            
            //细节进行视椎体剔除
            if (useDetailDraw)
            {
                // m_detailRenderer.Cull(m_detailProjM, w2cm);
            }
        }
        
        protected override void OnBeginFrameRenderingAfter(ScriptableRenderContext context, List<Camera> cameras)
        {
            //更新细节的渲染
            if (useDetailDraw)
            {
                // m_detailRenderer.OnUpdate(cullCamera);
            }
        }
        
        public override void DisplayInEditor(int lodLv)
        {
            bool result = LoadAsset(out TextAsset treeData, out TextAsset heightMap);
            if (!result)
            {
                gameObject.SetActive(false);
                return;
            }
            
            //创建渲染器根节点
            CreateParentNode();
            
            //4叉树
            m_quadTree = new TMQuadTreeWrapper(treeData.bytes, transform.position);
            
            //网格池
            m_terrainMeshDataPool = new TerrainMeshDataPool(m_tmc, MTAssetLoadMgr.Instance.meshDataLoader);

            //直接激活所有的 Patch
            for (int i = 0; i < m_quadTree.NodeCount; i++)
            {
                var itemNode = m_quadTree[i];
                
                if (itemNode.meshId >= 0 && itemNode.lodLv == lodLv)
                {
                    ActiveMesh(itemNode);
                }
            }
        }

        
        private const string c_NodeName_Active = "Active", c_NodeName_Deactive = "Deactive";
        
        private void ClearParentNode()
        {
            int childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == c_NodeName_Active || child.name == c_NodeName_Deactive)
                {
                    MTRuntimeUtils.DestroyObject(child.gameObject);
                }
            }
        }

        private void CreateParentNode()
        {
            if (m_activeParent == null)
            {
                var apGo = new GameObject(c_NodeName_Active);
                apGo.transform.SetParent(transform);
                apGo.layer = gameObject.layer;
                
                //编辑器，非运行时下才这么做，为了防止策划误操作
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    apGo.hideFlags = HideFlags.HideAndDontSave;
                }
#endif //UNITY_EDITOR
                
                m_activeParent = apGo.transform;
            }
            
            if (m_deactiveParent == null)
            {
                var dpGo = new GameObject(c_NodeName_Deactive);
                dpGo.transform.SetParent(transform);
                dpGo.layer = gameObject.layer;
                
                //编辑器，非运行时下才这么做，为了防止策划误操作
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    dpGo.hideFlags = HideFlags.HideAndDontSave;
                }
#endif //UNITY_EDITOR
                
                m_deactiveParent = dpGo.transform;
            }
        }
        
        
        //激活网格
        private void ActiveMesh(TMQuadTreeNode node)
        {
            if(m_activePatchRenders.ContainsKey(node.meshId)) return;
            
            PooledTMPatchRender tmPatch = PooledTMPatchRender.Pop();
            tmPatch.transform.SetParent(m_activeParent);
            
            TerrainMeshData tmd = m_terrainMeshDataPool.PopMesh(node.meshId);
            tmPatch.UpdatePatchMesh(m_tmc, m_vtCreator, tmd, transform.position, gameObject.layer);
            m_activePatchRenders.Add(node.meshId, tmPatch);
        }

        //吊销网格
        private void DeactiveMesh(TMQuadTreeNode node)
        {
            if(!m_activePatchRenders.ContainsKey(node.meshId)) return;
            
            PooledTMPatchRender tmPatch = m_activePatchRenders[node.meshId];
            tmPatch.transform.SetParent(m_deactiveParent);
            
            PooledTMPatchRender.Push(tmPatch);
            m_activePatchRenders.Remove(node.meshId);
        }

        
        private void OnDrawGizmos()
        {
            if (!this.enabled) return;

            var lastColor = Gizmos.color;
            {
                if (debugDetail && useDetailDraw)
                {
                    // m_detailRenderer?.DrawDebug();
                }

                if (debugTreeWorldBounds && m_quadTree != null)
                {
                    Bounds worldBounds = m_quadTree.Bnd;
                    
                    Gizmos.color = Color.green;
                    if (debugPositionContain != null)
                    {
                        Vector3 debugPosition = debugPositionContain.position;
                        bool result = worldBounds.Contains(debugPosition);
                        if (result)
                        {
                            Gizmos.color = Color.red;
                        }
                    }
                    Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
                }
            }
            Gizmos.color = lastColor;
        }

    }
}