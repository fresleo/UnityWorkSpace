// Created By: WangYu  Date: 2023-11-30

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.InstancedObject;
using com.xknight.mt.Lib.Runtime.MT.Log;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class IOLoader : AbsMTLoader
    {
        /// <summary>
        /// 暂停渲染，主要用于解决和 FrameDebugger 的冲突
        /// </summary>
        public static bool s_pauseRender;
        
        public string groupConfigPath;
        public MeshFilter[] lodMeshes;
        public MeshRenderer[] lodMaterials;

        public float displayDistance = 1000;

        public float[] lodAndScreenCovers;

        public ShadowCastingMode castShadows = ShadowCastingMode.On;
        public bool receiveShadows = true;

        public bool debugTreeBnd;
        public bool debugIoBnd;
        public bool debugIoRate;


        //组配置
        private IOGroupConfig m_groupConfig;

        //原始的远平面距离
        private float m_originalFarPlane;

        //剔除平面
        private Plane[] m_cullPlanes;

        //平铺模式
        private IOTileData[] m_tileDatas;
        private MTArray<IOTileData> m_visibleDataArray;
        public MTArray<IOTileData> VisibleDataArray => m_visibleDataArray;

        //4叉树模式
        private IOQuadTreeNode[] m_treeNodes;
        private MTArray<IOQuadTreeNode> m_candidateNodeArray, m_visibleNodeArray;
        public MTArray<IOQuadTreeNode> VisibleNodeArray => m_visibleNodeArray;

        //渲染任务队列
        private List<IORenderTask> m_renderTasks = new();

        //最大数据长度
        private int m_maxDataLenght;


        public IOGroupConfig GroupConfig => m_groupConfig;


        protected override void OnStop()
        {
            m_groupConfig = null;

            m_cullPlanes = null;

            m_tileDatas = null;
            if (m_visibleDataArray != null)
            {
                m_visibleDataArray.Clear();
                m_visibleDataArray = null;
            }

            m_treeNodes = null;
            if (m_candidateNodeArray != null)
            {
                m_candidateNodeArray.Clear(item => item?.Clear());
                m_candidateNodeArray = null;
            }

            if (m_visibleNodeArray != null)
            {
                m_visibleNodeArray.Clear(item => item?.Clear());
                m_visibleNodeArray = null;
            }

            foreach (var item in m_renderTasks)
            {
                item.Clear();
            }
            m_renderTasks.Clear();
        }

        private bool LoadAsset(out TextAsset byteData)
        {
            byteData = null;
            
            m_groupConfig = LoadAssetObject<IOGroupConfig>(groupConfigPath);
            if (m_groupConfig == null)
            {
                MTLogger.LogError($"{nameof(IOGroupConfig)} 加载失败: {groupConfigPath}");
                return false;
            }

            byteData = LoadAssetObject<TextAsset>(m_groupConfig.byteDataPath);
            if (byteData == null)
            {
                MTLogger.LogError($"{nameof(TextAsset)} 加载失败: {m_groupConfig.byteDataPath}");
                return false;
            }

            return true;
        }

        private void LoadByteData(TextAsset byteData)
        {
            //读取2进制数据
            MemoryStream stream = new MemoryStream(byteData.bytes);
            
            //数据长度
            int dataLen = MTStreamUtils.ReadInt(stream);
            
            switch (m_groupConfig.dataType)
            {
                //平铺模式
                case IOGroupConfig.EDataType.Flat:
                {
                    m_tileDatas = new IOTileData[dataLen];
                    for (int i = 0; i < dataLen; i++)
                    {
                        m_tileDatas[i] = IOTileDataExt.Deserialize(stream);
                    }

                    m_visibleDataArray = new MTArray<IOTileData>(dataLen);
                }
                    break;

                //树模式
                case IOGroupConfig.EDataType.Tree:
                {
                    m_treeNodes = new IOQuadTreeNode[dataLen];
                    for (int i = 0; i < dataLen; i++)
                    {
                        var node = new IOQuadTreeNode();
                        node.Initialize(-1);
                        node.Deserialize(stream, Vector3.zero);
                        m_treeNodes[i] = node;
                    }

                    m_candidateNodeArray = new MTArray<IOQuadTreeNode>(dataLen);
                    m_visibleNodeArray = new MTArray<IOQuadTreeNode>(dataLen);
                }
                    break;
            }
            stream.Close();

            m_maxDataLenght = dataLen;
        }
        
        protected override void OnPlay()
        {
            bool result = LoadAsset(out TextAsset byteData);
            if (!result)
            {
                gameObject.SetActive(false);
                return;
            }

            LoadByteData(byteData);
            
            m_cullPlanes = new Plane[6];
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

            switch (m_groupConfig.dataType)
            {
                case IOGroupConfig.EDataType.Flat:
                {
                    m_visibleDataArray.Reset();
                    for (int i = 0; i < m_tileDatas.Length; i++)
                    {
                        var item = m_tileDatas[i];
                        if (GeometryUtility.TestPlanesAABB(m_cullPlanes, item.bnd))
                        {
                            m_visibleDataArray.Add(item);
                        }
                    }
                }
                    break;

                case IOGroupConfig.EDataType.Tree:
                {
                    MTRuntimeUtils.CullQuadTreeNode(
                        m_cullPlanes, m_treeNodes,
                        m_candidateNodeArray, m_visibleNodeArray,
                        CheckVisibility);

                    bool CheckVisibility(IOQuadTreeNode curNode)
                    {
                        // 没子节点，说明是叶子节点，加入可见列表
                        return curNode.children.Length == 0;
                    }
                }
                    break;
            }
        }

        protected override void OnBeginFrameRenderingAfter(ScriptableRenderContext context, List<Camera> cameras)
        {
            //清空容器
            foreach (var item in m_renderTasks)
            {
                item.Reset();
            }
            
            switch (m_groupConfig.dataType)
            {
                //平铺模式
                case IOGroupConfig.EDataType.Flat:
                {
                    for (int i = 0; i < m_visibleDataArray.Length; i++)
                    {
                        var item = m_visibleDataArray[i];

                        int lodLv = GetLodLv(item.bnd);
                        var ld = item.lmc;
                        var matrix = item.matrix;

                        IORenderTask task;
                        if (ld.baked)
                        {
                            task = GetRenderTask(lodLv, ld.index);
                            task.lightmapScaleOffsets.Add(ld.scaleOffset);
                        }
                        else
                        {
                            task = GetRenderTask(lodLv, -1);
                        }

                        task.worldMatrixs.Add(matrix);
                    }
                }
                    break;

                //树模式
                case IOGroupConfig.EDataType.Tree:
                {
                    for (int ii = 0; ii < m_visibleNodeArray.Length; ii++)
                    {
                        var item = m_visibleNodeArray[ii];

                        for (int jj = 0; jj < item.holdBounds.Count; jj++)
                        {
                            var bnd = item.holdBounds[jj];
                            var ld = item.holdLightmapDatas[jj];
                            var matrix = item.holdWorldMatrixs[jj];

                            int lodLv = GetLodLv(bnd);

                            IORenderTask task;
                            if (ld.baked)
                            {
                                task = GetRenderTask(lodLv, ld.index);
                                task.lightmapScaleOffsets.Add(ld.scaleOffset);
                            }
                            else
                            {
                                task = GetRenderTask(lodLv, -1);
                            }

                            task.worldMatrixs.Add(matrix);
                        }
                    }
                }
                    break;
            }

            foreach (var task in m_renderTasks)
            {
                //没有能显示的世界矩阵
                if (task.worldMatrixs.Datas == null || task.worldMatrixs.Length == 0)
                {
                    continue;
                }
                
                // 获取渲染资源
                if (!GetMeshAndMaterial(task.lodLv, out var mesh, out var material))
                {
                    continue;
                }

                // 启动 Instancing 功能
                if (!material.enableInstancing)
                {
                    material.enableInstancing = true;
                    if (!material.enableInstancing)
                    {
                        MTLogger.LogError($"无法靠代码帮材质球启用 Instancing 功能： {material.name}");
                        continue;
                    }
                }
                
                // 如果有 lightmap ，组织对它的支持
                bool hasLightmap = LightmapSettings.lightmaps != null
                                   && LightmapSettings.lightmaps.Length > 0
                                   && task.lightmapIndex >= 0
                                   && task.lightmapScaleOffsets.Datas != null
                                   && task.lightmapIndex < LightmapSettings.lightmaps.Length;
                
                Texture2D lightmapColor = null;
                if (hasLightmap)
                {
                    lightmapColor = LightmapSettings.lightmaps[task.lightmapIndex].lightmapColor;
                    if (lightmapColor == null)
                    {
                        hasLightmap = false;
                    }
                }

                // 设置关键字
                if (hasLightmap)
                {
                    material.EnableKeyword("LIGHTMAP_ON");
                }

                RenderParams rp = new RenderParams(material);

                if (hasLightmap)
                {
                    rp.matProps = new MaterialPropertyBlock();

                    // 这里假定使用的还是 URP 的 Lightmap 工作方式，如果 shader 中有特殊定制的话，可能会不适用
                    rp.matProps.SetTexture("unity_Lightmap", lightmapColor);
                    rp.matProps.SetVectorArray("unity_LightmapST", task.lightmapScaleOffsets.Datas);
                }

                if (s_pauseRender)
                {
                    Graphics.RenderMeshInstanced(rp, mesh, 0, task.worldMatrixs.Datas);
                }
            }
        }

        public override void DisplayInEditor(int lodLv)
        {
            bool result = LoadAsset(out TextAsset byteData);
            if (!result)
            {
                gameObject.SetActive(false);
                return;
            }
            
            LoadByteData(byteData);

            switch (m_groupConfig.dataType)
            {
                case IOGroupConfig.EDataType.Flat:
                {
                    m_visibleDataArray.Reset();
                    for (int i = 0; i < m_tileDatas.Length; i++)
                    {
                        var item = m_tileDatas[i];
                        m_visibleDataArray.Add(item);
                    }
                }
                    break;

                case IOGroupConfig.EDataType.Tree:
                {
                    m_visibleNodeArray.Reset();
                    for (int i = 0; i < m_treeNodes.Length; i++)
                    {
                        var item = m_treeNodes[i];
                        if (item.children.Length == 0)
                        {
                            m_visibleNodeArray.Add(item);
                        }
                    }
                }
                    break;
            }
        }


        //判断lod级别
        private int GetLodLv(Bounds bnd)
        {
            float rate = MTRuntimeUtils.ScreenCoverRate(cullCamera, bnd);

            if (lodAndScreenCovers != null)
            {
                for (int lod = 0; lod < lodAndScreenCovers.Length; lod++)
                {
                    if (rate >= lodAndScreenCovers[lod])
                    {
                        return lod;
                    }
                }
            }

            return 0;
        }

        private IORenderTask GetRenderTask(int lodLv, int lightmapIndex)
        {
            foreach (var item in m_renderTasks)
            {
                if (item.lodLv == lodLv &&
                    item.lightmapIndex == lightmapIndex)
                {
                    return item;
                }
            }

            var task = new IORenderTask(m_maxDataLenght)
            {
                lodLv = lodLv,
                lightmapIndex = lightmapIndex
            };
            m_renderTasks.Add(task);

            return task;
        }

        //根据lod级别获取资源
        private bool GetMeshAndMaterial(int lodLv, out Mesh mesh, out Material material)
        {
            mesh = null;
            material = null;
            if (lodLv < 0 || lodLv >= lodMeshes.Length || lodLv >= lodMaterials.Length)
            {
                return false;
            }
            
            if (Application.isPlaying)
            {
                mesh = lodMeshes[lodLv].mesh;
                material = lodMaterials[lodLv].material;
            }
            else
            {
                mesh = lodMeshes[lodLv].sharedMesh;
                material = lodMaterials[lodLv].sharedMaterial;
            }

            return mesh != null && material != null;
        }

        private void OnDrawGizmos()
        {
            if (!this.enabled) return;

            var lastColor = Gizmos.color;
            {
                if (m_groupConfig == null) return;

                //平铺模式
                switch (m_groupConfig.dataType)
                {
                    case IOGroupConfig.EDataType.Flat:
                    {
                        if (debugIoBnd)
                        {
                            if (m_visibleDataArray != null)
                            {
                                Gizmos.color = Color.red;
                                for (int i = 0; i < m_visibleDataArray.Length; i++)
                                {
                                    var item = m_visibleDataArray[i];

                                    //对象的包围盒
                                    Gizmos.DrawWireCube(item.bnd.center, item.bnd.size);
                                }
                            }
                        }
                    }
                        break;

                    case IOGroupConfig.EDataType.Tree:
                    {
                        if (debugTreeBnd || debugIoBnd)
                        {
                            if (m_visibleNodeArray != null)
                            {
                                for (int i = 0; i < m_visibleNodeArray.Length; i++)
                                {
                                    var item = m_visibleNodeArray[i];

                                    //树的包围盒
                                    if (debugTreeBnd)
                                    {
                                        Gizmos.color = Color.green;
                                        Gizmos.DrawWireCube(item.bnd.center, item.bnd.size);
                                    }

                                    if (debugIoBnd)
                                    {
                                        for (int j = 0; j < item.holdBounds.Count; j++)
                                        {
                                            var ioBnd = item.holdBounds[j];

                                            //对象的包围盒
                                            Gizmos.color = Color.red;
                                            Gizmos.DrawWireCube(ioBnd.center, ioBnd.size);
                                        }
                                    }
                                }
                            }
                        }
                    }
                        break;
                }
            }
            Gizmos.color = lastColor;
        }
    }
}