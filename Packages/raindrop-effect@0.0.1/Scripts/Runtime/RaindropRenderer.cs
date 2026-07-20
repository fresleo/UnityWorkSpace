// Created By: WangYu  Date: 2024-11-18

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaindropEffect
{
    /// <summary>
    /// 渲染参数
    /// </summary>
    [Serializable]
    public class RenderParameters
    {
        /// <summary>
        /// RT 的尺寸
        /// </summary>
        public int renderTextureWidth = 1920, renderTextureHeight = 1080;
        
        /// <summary>
        /// 液滴的生成速率 (每秒)
        /// </summary>
        [Range(0, 1000)] public float dropletsSpawnRate = 450;
        /// <summary>
        /// 液滴的尺寸范围
        /// </summary>
        public Vector2 dropletSizeRange = new(5, 30);
        
        /// <summary>
        /// 折射范围
        /// </summary>
        public Vector2 refraction = new(0.4f, 0.6f);
        /// <summary>
        /// 灯光的位置
        /// </summary>
        public Vector4 lightPosition = new(-1, -1, 1, 0);
        /// <summary>
        /// 雨滴颜色
        /// </summary>
        public Color raindropColor = new(0.2f, 0.2f, 0.2f, 0.2f);
        /// <summary>
        /// Alpha 平滑范围
        /// </summary>
        public Vector2 alphaSmoothRange = new(0.95f, 1.0f);
    }
    
    /// <summary>
    /// 雨滴渲染器
    /// </summary>
    public class RaindropRenderer
    {
        /// <summary>
        /// 渲染数据
        /// </summary>
        public RaindropRendererData rendererData;
        /// <summary>
        /// 渲染参数
        /// </summary>
        public RenderParameters rendParas;
        
        /// <summary>
        /// 背景 RT
        /// </summary>
        public RenderTexture backgroundTex;
        // 如果没传入背景图，会产生1个临时的，并记录一下这个情况
        private bool m_backgroundTexIsTemp;
        
        /// <summary>
        /// 液滴 RT
        /// </summary>
        public RenderTexture dropletTexture;
        /// <summary>
        /// 雨滴 RT
        /// </summary>
        public RenderTexture raindropTex;
        
        private RenderTexture m_raindropEffectTex;
        /// <summary>
        /// 最终的雨滴效果 RT
        /// </summary>
        public RenderTexture GetRaindropEffectTexture()
        {
            return m_raindropEffectTex;
        }

        private Material m_raindropMaterial, m_dropletMaterial, m_raindropEffectMaterial;
        
        private static readonly string s_cmdBufferName = $"{nameof(RaindropRenderer)}_CmdBuffer";
        
        private Matrix4x4 m_projectionMatrix;
        private CommandBuffer m_commandBuffer;
        
        private Mesh m_raindropMesh;
        private ComputeBuffer m_argsBuffer;
        private uint[] m_args = new uint[5] { 0, 0, 0, 0, 0 };
        
        private Matrix4x4[] m_raindropMatrixList = new Matrix4x4[1023];
        private float[] m_raindropSizeList = new float[1023];
        private MaterialPropertyBlock m_raindopMpb;
        
        private bool m_inited;
        

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if(m_inited) return;
            
            InitRTs();
            InitMaterials();
            
            m_commandBuffer = new CommandBuffer();
            m_commandBuffer.name = s_cmdBufferName;
            ResetBuffer();
            
            m_raindropMesh = GenerateQuadMesh(Vector2.zero, Vector2.one);
            m_raindopMpb = new MaterialPropertyBlock();

            m_inited = true;
        }
        
        private void InitRTs()
        {
            int rtWidth = this.rendParas.renderTextureWidth;
            int rtHeight = this.rendParas.renderTextureHeight;
            
            // 确保有1个背景
            if (!backgroundTex)
            {
                backgroundTex = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
                m_backgroundTexIsTemp = true;
            }
            
            dropletTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            raindropTex = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            
            m_raindropEffectTex = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
        }

        private void InitMaterials()
        {
            m_raindropMaterial = this.rendererData.renderResources.raindropMaterial;
            if (m_raindropMaterial.mainTexture == null)
            {
                Debug.LogError("错误: 未分配雨滴纹理！", m_raindropMaterial);
            }

            if (m_dropletMaterial == null)
            {
                m_dropletMaterial = CoreUtils.CreateEngineMaterial(this.rendererData.renderResources.dropletShader);
            }
            m_dropletMaterial.SetTexture(DropletSPID._MainTex, m_raindropMaterial.mainTexture);
            
            if (m_raindropEffectMaterial == null)
            {
                m_raindropEffectMaterial = CoreUtils.CreateEngineMaterial(this.rendererData.renderResources.raindropEffectShader);
            }
            SetMatRT();
        }

        private void SetMatRT()
        {
            m_raindropEffectMaterial.SetTexture(RaindropEffectSPID._MainTex, backgroundTex);
            m_raindropEffectMaterial.SetTexture(RaindropEffectSPID._DropletTex, dropletTexture);
            m_raindropEffectMaterial.SetTexture(RaindropEffectSPID._RaindropTex, raindropTex);
        }
        
        private void ResetBuffer()
        {
            m_projectionMatrix = Matrix4x4.Ortho(0, this.rendParas.renderTextureWidth, 0, this.rendParas.renderTextureHeight, -1, 1);
            m_commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, m_projectionMatrix);
            
            m_argsBuffer?.Release();
            m_argsBuffer = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }
        
        public static Mesh GenerateQuadMesh(Vector2 center, Vector2 size)
        {
            Vector2 halfSize = size * 0.5f;

            Mesh quadMesh = new Mesh();

            quadMesh.vertices = new Vector3[]
            {
                new Vector3(center.x - halfSize.x, center.y - halfSize.y, 0),
                new Vector3(center.x + halfSize.x, center.y - halfSize.y, 0),
                new Vector3(center.x + halfSize.x, center.y + halfSize.y, 0),
                new Vector3(center.x - halfSize.x, center.y + halfSize.y, 0),
            };

            quadMesh.triangles = new int[]
            {
                0, 3, 1,
                1, 3, 2,
            };

            quadMesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            };

            quadMesh.normals = new Vector3[]
            {
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
            };

            quadMesh.name = "QuadMesh";

            return quadMesh;
        }


        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            Graphics.Blit(Texture2D.blackTexture, backgroundTex);
            Graphics.Blit(Texture2D.blackTexture, dropletTexture);
            Graphics.Blit(Texture2D.blackTexture, raindropTex);
            Graphics.Blit(Texture2D.blackTexture, m_raindropEffectTex);
        }
        
        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            m_commandBuffer?.Release();
            m_commandBuffer = null;
            m_argsBuffer?.Release();
            m_argsBuffer = null;
            
            ReleaseRTs();
            
            CoreUtils.Destroy(m_dropletMaterial);
            m_dropletMaterial = null;
            CoreUtils.Destroy(m_raindropEffectMaterial);
            m_raindropEffectMaterial = null;
        }
        
        /// <summary>
        /// 释放 RT
        /// </summary>
        public void ReleaseRTs()
        {
            if (m_backgroundTexIsTemp)
            {
                if(backgroundTex) RenderTexture.ReleaseTemporary(backgroundTex);
                backgroundTex = null;
            }
            m_backgroundTexIsTemp = false;
            
            if (dropletTexture) RenderTexture.ReleaseTemporary(dropletTexture);
            dropletTexture = null;
            
            if(raindropTex) RenderTexture.ReleaseTemporary(raindropTex);
            raindropTex = null;
            
            if(m_raindropEffectTex) RenderTexture.ReleaseTemporary(m_raindropEffectTex);
            m_raindropEffectTex = null;
        }
        
        
        /// <summary>
        /// 重置尺寸
        /// </summary>
        public void Resize()
        {
            ReleaseRTs();
            InitRTs();
            
            SetMatRT();
            ResetBuffer();
        }
        
        /// <summary>
        /// 渲染雨滴
        /// </summary>
        public void RenderRaindrops(List<Raindrop> raindrops, float deltaTime)
        {
            if (this.rendParas == null) return;

            RenderDroplets(deltaTime);
            RenderRaindrops(raindrops);
        }
        
        // 渲染液滴
        private void RenderDroplets(float deltaTime)
        {
            if (!m_dropletMaterial) return;

            m_commandBuffer.name = $"{s_cmdBufferName}_{nameof(RenderDroplets)}";
            m_commandBuffer.Clear();
            m_commandBuffer.SetRenderTarget(dropletTexture);
            m_commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, m_projectionMatrix);

            float seed = UnityEngine.Random.Range(0.0f, 100.0f);
            m_dropletMaterial.SetFloat(DropletSPID._Seed, seed);

            Vector4 spawnRect = new Vector4(0, 0, this.rendParas.renderTextureWidth, this.rendParas.renderTextureHeight);
            m_dropletMaterial.SetVector(DropletSPID._SpawnRect, spawnRect);

            Vector4 sizeRange = new Vector4(this.rendParas.dropletSizeRange.x, this.rendParas.dropletSizeRange.y, 0, 0);
            m_dropletMaterial.SetVector(DropletSPID._SizeRange, sizeRange);
            
            uint meshIndexCount = m_raindropMesh.GetIndexCount(0);
            uint dropletsCount = (uint)(this.rendParas.dropletsSpawnRate * deltaTime);
            m_args[0] = meshIndexCount;
            m_args[1] = dropletsCount;
            m_argsBuffer.SetData(m_args);

            m_commandBuffer.DrawMeshInstancedIndirect(m_raindropMesh, 0, m_dropletMaterial, 0, m_argsBuffer);

            Graphics.ExecuteCommandBuffer(m_commandBuffer);
        }
        
        // 渲染雨滴
        private void RenderRaindrops(List<Raindrop> raindrops)
        {
            if (!m_raindropMaterial) return;
            
            m_commandBuffer.name = $"{s_cmdBufferName}_{nameof(RenderRaindrops)}";
            m_commandBuffer.Clear();
            m_commandBuffer.SetRenderTarget(raindropTex);
            m_commandBuffer.ClearRenderTarget(true, true, Color.clear);
            m_commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, m_projectionMatrix);

            Vector3 raindropPos = Vector3.zero;
            Quaternion raindropRotation = Quaternion.identity;
            Vector3 raindropScale = Vector3.one;
            for (int i = 0; i < raindrops.Count; i++)
            {
                // TODO
                if (i >= m_raindropMatrixList.Length)
                {
                    break;
                }

                Raindrop raindrop = raindrops[i];
                raindropPos.x = raindrop.position[0];
                raindropPos.y = raindrop.position[1];
                raindropRotation = raindrop.rotation;
                raindropScale.x = raindrop.Size.x;
                raindropScale.y = raindrop.Size.y;

                Matrix4x4 model = Matrix4x4.TRS(raindropPos, raindropRotation, raindropScale);
                m_raindropMatrixList[i] = model;
                m_raindropSizeList[i] = (raindrop.Size.x / 100.0f);
            }
            
            m_raindopMpb.SetFloatArray(RaindropSPID._Size, m_raindropSizeList);
            int count = Mathf.Min(raindrops.Count, m_raindropMatrixList.Length);
            
            m_commandBuffer.DrawMeshInstanced(m_raindropMesh, 0, m_raindropMaterial, 0, m_raindropMatrixList, count, m_raindopMpb);
            
            Graphics.ExecuteCommandBuffer(m_commandBuffer);
        }
        
        /// <summary>
        /// 混合雨滴特效
        /// </summary>
        public void BlendRaindropEffect()
        {
            if (!m_raindropEffectMaterial) return;

            m_commandBuffer.name = $"{s_cmdBufferName}_{nameof(BlendRaindropEffect)}";
            m_commandBuffer.Clear();
            
            m_commandBuffer.SetRenderTarget(m_raindropEffectTex);
            m_commandBuffer.ClearRenderTarget(true, true, Color.clear);

            m_raindropEffectMaterial.SetVector(RaindropEffectSPID._Refraction, this.rendParas.refraction);
            m_raindropEffectMaterial.SetVector(RaindropEffectSPID._LightPosition, this.rendParas.lightPosition);
            m_raindropEffectMaterial.SetColor(RaindropEffectSPID._RaindropColor, this.rendParas.raindropColor);
            m_raindropEffectMaterial.SetVector(RaindropEffectSPID._AlphaSmoothRange, this.rendParas.alphaSmoothRange);
            
            // Graphics.Blit(this.backgroundTex, m_raindropEffectTex, m_raindropEffectMaterial);
            m_commandBuffer.Blit(this.backgroundTex, m_raindropEffectTex, m_raindropEffectMaterial);
            
            Graphics.ExecuteCommandBuffer(m_commandBuffer);
        }
        
    }
}