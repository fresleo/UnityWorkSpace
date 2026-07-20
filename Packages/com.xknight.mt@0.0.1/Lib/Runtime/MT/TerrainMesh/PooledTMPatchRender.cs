// Created By: WangYu  Date: 2022-10-20

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 可池化的地形网格 Patch 渲染器
    /// </summary>
    public class PooledTMPatchRender : IVTReceiver
    {
        #region 对象池

        static Queue<PooledTMPatchRender> s_poolQ = new Queue<PooledTMPatchRender>();

        public static void Clear()
        {
            while (s_poolQ.Count > 0)
            {
                s_poolQ.Dequeue().Destroy();
            }
        }

        public static PooledTMPatchRender Pop()
        {
            if (s_poolQ.Count > 0)
            {
                return s_poolQ.Dequeue();
            }

            return new PooledTMPatchRender();
        }

        public static void Push(PooledTMPatchRender item)
        {
            item.OnPushBackPool();

            s_poolQ.Enqueue(item);
        }

        #endregion 对象池

        
        //公共引用
        private TerrainMeshConfig m_tmConfig;
        private IVTCreator m_vtCreator;
        
        //patch 自身的资源
        private GameObject m_go;
        private MeshFilter m_mf;
        private MeshRenderer m_mr;
        private MeshCollider m_mc;
        private Material[] m_bakedVTMats;

        private Material[] m_mixMats, m_bakeDiffuseMats, m_bakeNormalMats;
        
        
        //读进来的2进制数据
        private TerrainMeshData m_tmd;
        
        //世界空间中心点位置
        private Vector3 m_worldCenter = Vector3.zero;
        //直径
        private float m_diameter = 0;

        //当前纹理尺寸
        private int m_textureSize = -1;
        //bake出来的vt纹理
        private IVT[] m_textures;

        //等待回收的命令id
        private long m_waitBackCmdId = 0;
        //最后一个待定的创建命令
        private VTCreateCmd m_lastPendingCreateCmd;
        
        private static readonly int _Diffuse = Shader.PropertyToID("_Diffuse");
        private static readonly int _Normal = Shader.PropertyToID("_Normal");

        /// <summary>
        /// 补丁 GO 的名字
        /// </summary>
        public const string PATCH_GO_NAME = "Patch";

        /// <summary>
        /// go 的 Transform
        /// </summary>
        public Transform transform => m_go.transform;
        
        //构造函数
        private PooledTMPatchRender()
        {
            //创建渲染对象
            m_go = new GameObject(PATCH_GO_NAME);
            m_mf = m_go.AddComponent<MeshFilter>();
            m_mr = m_go.AddComponent<MeshRenderer>();
            m_mc = m_go.AddComponent<MeshCollider>();
            m_mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        private void Destroy()
        {
            OnPushBackPool();
            
            ClearRendererMaterial();
            m_mf = null;
            m_mr = null;
            m_mc = null;
            if (m_go != null)
            {
                UnityEngine.Object.Destroy(m_go);
                m_go = null;
            }
            
            if (m_bakedVTMats != null)
            {
                foreach (var mat in m_bakedVTMats)
                {
                    MTRuntimeUtils.DestroyObject(mat);
                }
                m_bakedVTMats = null;
            }

            m_mixMats = null;
            m_bakeDiffuseMats = null;
            m_bakeNormalMats = null;

            m_tmConfig = null;
            m_vtCreator = null;
        }
        
        //清理渲染器上的材质球
        private void ClearRendererMaterial()
        {
            if(m_mr == null) return;
            
            //非运行时删非 shared 的材质会提示错误的
            if (Application.isPlaying)
            {
                var mats = m_mr.materials;
                if (mats != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var mat = mats[i];
                        if(mat == null) continue;

                        UnityEngine.Object.Destroy(mat);
                    }
                }
            }
        }
        
        //回收回对象池中
        private void OnPushBackPool()
        {
            m_bakedVTMats[0].SetTexture(_Diffuse, null);
            m_bakedVTMats[0].SetTexture(_Normal, null);

            if (m_go != null)
            {
                m_go.SetActive(false);
            }

            if (m_textures != null)
            {
                m_vtCreator.DisposeTextures(m_textures);
                m_textures = null;
            }
            
            if (m_lastPendingCreateCmd != null)
            {
                VTCreateCmd.Push(m_lastPendingCreateCmd);
                m_lastPendingCreateCmd = null;
            }

            m_tmd = null;
            m_textureSize = -1;
            m_waitBackCmdId = 0;
        }
        
        
        /// <summary>
        /// 更新 patch 的 mesh
        /// </summary>
        public void UpdatePatchMesh(TerrainMeshConfig tmConfig, IVTCreator vtCreator, TerrainMeshData tmd, Vector3 position, int layer)
        {
            m_tmConfig = tmConfig;
            m_vtCreator = vtCreator;
            
            m_go.layer = layer;
            m_go.SetActive(true);
            m_go.transform.position = position;

            //设置网格
            m_tmd = tmd;
            m_mf.mesh = m_tmd.mesh;
            m_mc.sharedMesh = m_tmd.mesh;
            
            //准备vt的运行时材质球
            if (m_bakedVTMats == null)
            {
                m_bakedVTMats = new Material[1];

                var bakedVTMat = AssetLoadUtils.LoadAssetObject<Material>(m_tmConfig.bakedVTMatPath);
                if (bakedVTMat != null)
                {
                    m_bakedVTMats[0] = UnityEngine.Object.Instantiate(bakedVTMat);
                }
            }
            
            //默认先把mix材质球换上来
            ClearRendererMaterial();
            {
                int len = m_tmConfig.mixMatPaths.Length;
                if (m_mixMats == null || m_mixMats.Length != len)
                {
                    m_mixMats = new Material[len];
                }
                AssetLoadUtils.LoadAssetObjects(m_tmConfig.mixMatPaths, m_mixMats);
                m_mr.materials = m_mixMats;
            }

            //需要还原 Lightmap 信息
            if (tmConfig.lightmapData.baked)
            {
                m_mr.lightmapIndex = tmConfig.lightmapData.index;
                m_mr.lightmapScaleOffset = tmConfig.lightmapData.scaleOffset;
            }

            m_worldCenter = m_tmd.mesh.bounds.center + position;
            m_diameter = m_tmd.mesh.bounds.size.magnitude;
            
            m_textureSize = -1;
            m_waitBackCmdId = 0;
        }
        
        /// <summary>
        /// 更新 Patch 的 Texture
        /// </summary>
        public void UpdatePatchTexture(Vector3 viewerPos, float fov, float screenH, float screenW)
        {
            //当前纹理的尺寸
            float pixelSize = MTRuntimeUtils.PixelSize(viewerPos, fov, screenH, m_worldCenter, m_diameter);
            //确保纹理尺寸是1个整数，且是2的幂
            int curTexSize = Mathf.NextPowerOfTwo(Mathf.FloorToInt(pixelSize));
            
            RequestTexture(curTexSize);
        }

        //请求新纹理
        private void RequestTexture(int size)
        {
            if (m_textureSize == size)
            {
                return;
            }
            //使用固定尺寸渲染纹理，否则纹理会一直重新创建
            size = Mathf.Clamp(size, 128, 2048);
            if (m_textureSize == size)
            {
                return;
            }
            m_textureSize = size;

            var cmd = VTCreateCmd.Pop();
            cmd.cmdId = VTCreateCmd.GenerateID();
            cmd.size = size;
            cmd.uvMin = m_tmd.uvMin;
            cmd.uvMax = m_tmd.uvMax;
            cmd.receiver = this;

            {
                int len = m_tmConfig.bakeVTDiffuseMatPaths.Length;
                if (m_bakeDiffuseMats == null || m_bakeDiffuseMats.Length != len)
                {
                    m_bakeDiffuseMats = new Material[len];
                }
                AssetLoadUtils.LoadAssetObjects(m_tmConfig.bakeVTDiffuseMatPaths, m_bakeDiffuseMats);
                cmd.bakeDiffuse = m_bakeDiffuseMats;
            }

            {
                int len = m_tmConfig.bakeVTNormalMatPaths.Length;
                if (m_bakeNormalMats == null || m_bakeNormalMats.Length != len)
                {
                    m_bakeNormalMats = new Material[len];
                }
                AssetLoadUtils.LoadAssetObjects(m_tmConfig.bakeVTNormalMatPaths, m_bakeNormalMats);
                cmd.bakeNormal = m_bakeNormalMats;
            }
            
            if (m_waitBackCmdId > 0)
            {
                if (m_lastPendingCreateCmd != null)
                {
                    VTCreateCmd.Push(m_lastPendingCreateCmd);
                }
                m_lastPendingCreateCmd = cmd;
            }
            else
            {
                m_waitBackCmdId = cmd.cmdId;
                m_vtCreator?.AppendCmd(cmd);
            }
        }
        
        long IVTReceiver.WaitCmdId => m_waitBackCmdId;

        void IVTReceiver.OnTextureReady(long cmdId, IVT[] textures)
        {
            if (m_tmd == null || m_waitBackCmdId != cmdId)
            {
                m_vtCreator.DisposeTextures(textures);
                return;
            }

            if (m_textures != null)
            {
                m_vtCreator.DisposeTextures(m_textures);
                m_textures = null;
            }
            m_textures = textures;
            ApplyTextures();

            //替换材质
            ClearRendererMaterial();
            m_mr.materials = m_bakedVTMats;

            m_waitBackCmdId = 0;
            if (m_lastPendingCreateCmd != null)
            {
                m_waitBackCmdId = m_lastPendingCreateCmd.cmdId;
                m_vtCreator.AppendCmd(m_lastPendingCreateCmd);
                m_lastPendingCreateCmd = null;
            }
        }
        
        //应用纹理
        private void ApplyTextures()
        {
            Vector2 size = m_tmd.uvMax - m_tmd.uvMin;
            var scale = new Vector2(1f / size.x, 1f / size.y);
            var offset = -new Vector2(scale.x * m_tmd.uvMin.x, scale.y * m_tmd.uvMin.y);

            if (m_textures.Length > 0)
            {
                m_bakedVTMats[0].SetTexture(_Diffuse, m_textures[0].Tex);
            }

            m_bakedVTMats[0].SetTextureScale(_Diffuse, scale);
            m_bakedVTMats[0].SetTextureOffset(_Diffuse, offset);

            if (m_textures.Length > 1)
            {
                m_bakedVTMats[0].SetTexture(_Normal, m_textures[1].Tex);
            }

            m_bakedVTMats[0].SetTextureScale(_Normal, scale);
            m_bakedVTMats[0].SetTextureOffset(_Normal, offset);
        }
        
    }
}