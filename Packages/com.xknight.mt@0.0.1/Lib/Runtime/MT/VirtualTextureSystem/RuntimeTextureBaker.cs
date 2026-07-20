using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem
{
    /// <summary>
    /// 运行时纹理烘焙器
    /// </summary>
    public class RuntimeTextureBaker : IVT
    {
        private static Mesh s_fullScreenMesh;

        /// <summary>
        /// 返回一个全屏网格
        /// 作为<see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/>的参数，用来绘制全屏效果
        /// </summary>
        public static Mesh FullscreenMesh
        {
            get
            {
                if (s_fullScreenMesh != null)
                {
                    return s_fullScreenMesh;
                }

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_fullScreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_fullScreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f)
                });
                s_fullScreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });
                s_fullScreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_fullScreenMesh.UploadMeshData(true);

                return s_fullScreenMesh;
            }
        }

        /// <summary>
        /// 全局rt计数
        /// </summary>
        public static int s_rtCount = 0;
        
        private static readonly int _BakeScaleOffset = Shader.PropertyToID("_BakeScaleOffset");
        
        private int _texSize = 32; //纹理尺寸
        private RenderTexture _rt;
        
        private CommandBuffer _cmdBuffer; //命令缓冲
        private Vector4 _scaleOffset; //缩放偏移
        private Material[] _mats; //烘焙用的材质

        int IVT.Size => _texSize;
        Texture IVT.Tex => _rt;

        public RuntimeTextureBaker(int size)
        {
            _texSize = size;
            
            _cmdBuffer = new CommandBuffer();
            _cmdBuffer.name = "RuntimeTextureBaker";
            _scaleOffset = new Vector4(1, 1, 0, 0);
            
            CreateRT();
        }

        private void CreateRT()
        {
            _rt = new RenderTexture(_texSize, _texSize, 0);
            _rt.wrapMode = TextureWrapMode.Clamp;
            _rt.useMipMap = true;
            _rt.Create();
            _rt.DiscardContents(); //丢弃内容，避免复制操作gpu的内存

            s_rtCount++;
            // MTLogger.Log($"已创建的 rt : {s_rtCount}");
        }
        
        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            if (_rt != null)
            {
                _rt.Release();
                _rt = null;
                s_rtCount--;
            }
            
            if (_cmdBuffer != null)
            {
                _cmdBuffer.Clear();
                _cmdBuffer = null;
            }
            
            _mats = null;
        }
        
        /// <summary>
        /// 重置
        /// </summary>
        public void Reset(Vector2 uvMin, Vector2 uvMax, Material[] mats)
        {
            _scaleOffset.x = uvMax.x - uvMin.x;
            _scaleOffset.y = uvMax.y - uvMin.y;
            _scaleOffset.z = uvMin.x;
            _scaleOffset.w = uvMin.y;
            _mats = mats;
            Validate();
        }

        /// <summary>
        /// 验证 rt 是否被创建出来了
        /// </summary>
        public bool Validate()
        {
            //确保rt被创建
            if (!_rt.IsCreated())
            {
                _rt.Release();
                CreateRT();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 烘焙
        /// </summary>
        public void Bake()
        {
            for (int i = 0; i < _mats.Length; i++)
            {
                _mats[i].SetVector(_BakeScaleOffset, _scaleOffset);
            }

            _rt.DiscardContents();
            
            _cmdBuffer.Clear();
            _cmdBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            _cmdBuffer.SetViewport(new Rect(0, 0, _rt.width, _rt.height));
            _cmdBuffer.SetRenderTarget(_rt, 
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            for (int i = 0; i < _mats.Length; i++)
            {
                _cmdBuffer.DrawMesh(FullscreenMesh, Matrix4x4.identity, _mats[i]);
            }

            Graphics.ExecuteCommandBuffer(_cmdBuffer);
        }
        
    }
}