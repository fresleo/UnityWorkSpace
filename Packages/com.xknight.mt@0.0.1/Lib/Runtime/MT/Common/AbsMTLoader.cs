// Created By: WangYu  Date: 2023-11-21

using System;
using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public abstract class AbsMTLoader : MonoBehaviour, IMTLoader
    {
        /// <summary>
        /// 剔除相机
        /// </summary>
        public Camera cullCamera;
        
        /// <summary>
        /// 投影矩阵
        /// </summary>
        protected Matrix4x4 projM;
        
        //前1个世界到相机的矩阵
        private Matrix4x4 m_prevW2CM;
        

        protected virtual void OnDisable()
        {
            Stop();
            MTMasterControl.Instance.UnregisterLoader(this);
        }
        
        protected virtual void OnEnable()
        {
            MTMasterControl.Instance.RegisterLoader(this);
        }
        

        public void SetCullCamera(Camera came)
        {
            if (came == null)
            {
                return;
            }
            
            cullCamera = came;
            
            //矩阵
            projM = Matrix4x4.Perspective(
                cullCamera.fieldOfView, cullCamera.aspect, cullCamera.nearClipPlane, 
                cullCamera.farClipPlane);
            m_prevW2CM = Matrix4x4.identity;

            OnSetCullCamera();
        }

        public void Stop()
        {
            Pause();
            OnStop();
        }
        
        public void Pause()
        {
            OnPause();
            RenderPipelineManager.beginContextRendering -= BeginContextRendering;
        }

        public void Play()
        {
            OnPlay();
            RenderPipelineManager.beginContextRendering += BeginContextRendering;
        }

        public virtual void DisplayInEditor(int lodLv)
        {
        }

        
        /// <summary>
        /// 当设置剔除相机时
        /// </summary>
        protected virtual void OnSetCullCamera()
        {
        }

        /// <summary>
        /// 当停止时
        /// </summary>
        protected virtual void OnStop()
        {
        }
        
        /// <summary>
        /// 当暂停时
        /// </summary>
        protected virtual void OnPause()
        {
        }

        /// <summary>
        /// 当播放时
        /// </summary>
        protected virtual void OnPlay()
        {
        }
        
        //每帧都会调用
        private void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (cullCamera == null)
            {
                return;
            }

            if (!CanFrameRendering())
            {
                return;
            }
            
            Matrix4x4 w2cm = cullCamera.worldToCameraMatrix;
            if (m_prevW2CM != w2cm)
            {
                m_prevW2CM = w2cm;
                OnCameraMoves(context, cameras);
            }

            OnBeginFrameRenderingAfter(context, cameras);
        }

        protected virtual bool CanFrameRendering()
        {
            return true;
        }
        
        /// <summary>
        /// 当摄像机移动时
        /// </summary>
        protected abstract void OnCameraMoves(ScriptableRenderContext context, List<Camera> cameras);

        /// <summary>
        /// 开始渲染帧后
        /// 可以在一定程度上代替 Update() 的功能
        /// </summary>
        protected virtual void OnBeginFrameRenderingAfter(ScriptableRenderContext context, List<Camera> cameras)
        {
        }
        
        
        protected TObject LoadAssetObject<TObject>(string path) 
            where TObject : UnityEngine.Object
        {
            return AssetLoadUtils.LoadAssetObject<TObject>(path);
        }
        
    }
}